using TextMeshDOTS.HarfBuzz;
using TextMeshDOTS.HarfBuzz.Bitmap;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TextCore;

namespace TextMeshDOTS
{
    public unsafe partial class DispatchGlyphsSystem
    { 
        #region Collect Jobs
        [BurstCompile]
        struct AllocateJob : IJob
        {
            [ReadOnly] public GlyphTable       glyphTable;
            public NativeParallelHashSet<uint> glyphEntryIDsToRasterizeSet;

            public void Execute()
            {
                glyphEntryIDsToRasterizeSet.Capacity = math.max(glyphTable.entries.Length, glyphEntryIDsToRasterizeSet.Capacity);
            }
        }

        [BurstCompile]
        struct CaptureRenderGlyphsJob : IJobChunk
        {
            [ReadOnly] public GlyphTable                            glyphTable;
            [ReadOnly] public BufferTypeHandle<PreviousRenderGlyph> renderGlyphHandle;
            public ComponentTypeHandle<TextShaderIndex>             textShaderIndexHandle;
            public ComponentTypeHandle<ResidentRange>               residentRangeHandle;
            public ComponentTypeHandle<GpuState>                    gpuStateHandle;

            [NativeDisableParallelForRestriction] public NativeStream.Writer renderGlyphCapturesStream;
            public NativeParallelHashSet<uint>.ParallelWriter                glyphEntryIDsToRasterizeSet;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                renderGlyphCapturesStream.BeginForEachIndex(unfilteredChunkIndex);

                var shaderPtr    = chunk.GetComponentDataPtrRW(ref textShaderIndexHandle);
                var residentPtr  = (ResidentRange*)chunk.GetRequiredComponentDataPtrRW(ref residentRangeHandle);
                var gpuStates    = (GpuState*)chunk.GetRequiredComponentDataPtrRW(ref gpuStateHandle);
                var glyphBuffers = chunk.GetBufferAccessor(ref renderGlyphHandle);
                var gpuStateMask = chunk.GetEnabledMask(ref gpuStateHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndex))
                {
                    gpuStateMask[entityIndex]    = false;
                    bool resident                = gpuStates[entityIndex].state == GpuState.State.DynamicPromoteToResident;
                    gpuStates[entityIndex].state = resident ? GpuState.State.Resident : GpuState.State.Dynamic;
                    var glyphs                   = glyphBuffers[entityIndex];
                    renderGlyphCapturesStream.Write(new RenderGlyphCapture
                    {
                        glyphBuffer        = glyphs.Length != 0 ? (RenderGlyph*)glyphs.GetUnsafeReadOnlyPtr() : null,
                        glyphCount         = glyphs.Length,
                        makeResident       = resident,
                        residentRangePtr   = residentPtr + entityIndex,
                        textShaderIndexPtr = shaderPtr != null ? shaderPtr + entityIndex : null,
                    });

                    foreach (var glyph in glyphs)
                    {
                        var entry = glyphTable.GetEntry(glyph.glyph.glyphEntryId);
                        if (!entry.isInAtlas)
                            glyphEntryIDsToRasterizeSet.Add(glyph.glyph.glyphEntryId);
                    }
                }

                renderGlyphCapturesStream.EndForEachIndex();
            }
        }

        [BurstCompile]
        struct AssignShaderIndicesJob : IJob
        {
            [ReadOnly] public NativeStream        renderGlyphCapturesStream;
            public NativeList<RenderGlyphCapture> captures;
            public GlyphGpuTable                  glyphGpuTable;

            public void Execute()
            {
                int captureCount  = renderGlyphCapturesStream.Count();
                captures.Capacity = captureCount;

                int writeBufferOffset  = 0;
                int dynamicCount       = 0;
                var residentBufferSize = glyphGpuTable.bufferSize.Value;

                for (int stream = 0; stream < renderGlyphCapturesStream.ForEachCount; stream++)
                {
                    var reader = renderGlyphCapturesStream.AsReader();
                    for (int i = reader.BeginForEachIndex(stream); i > 0; i--)
                    {
                        var capture         = reader.Read<RenderGlyphCapture>();
                        capture.writeStart  = writeBufferOffset;
                        writeBufferOffset  += capture.glyphCount;
                        if (capture.makeResident)
                        {
                            if (capture.residentRangePtr->count != capture.glyphCount)
                            {
                                GapAllocator.TryAllocate(glyphGpuTable.residentGaps, (uint)capture.glyphCount, ref residentBufferSize, out var newLocation);
                                capture.gpuStart = (int)newLocation;
                                if (capture.textShaderIndexPtr != null)
                                {
                                    capture.textShaderIndexPtr->firstGlyphIndex = newLocation;
                                    capture.textShaderIndexPtr->glyphCount = (uint)capture.glyphCount;
                                }
                                capture.residentRangePtr->start = newLocation;
                                capture.residentRangePtr->count = (uint)capture.glyphCount;
                                //UnityEngine.Debug.Log($"Allocated resident range: {capture.residentRangePtr->start}, {capture.residentRangePtr->count}");
                            }
                        }
                        else
                        {
                            dynamicCount += capture.glyphCount;
                        }
                        captures.AddNoResize(capture);
                    }
                    reader.EndForEachIndex();
                }
                GapAllocator.TryAllocate(glyphGpuTable.residentGaps, (uint)dynamicCount, ref residentBufferSize, out var dynamicStart);
                glyphGpuTable.dispatchDynamicGaps.Add(new uint2(dynamicStart, (uint)dynamicCount));
                for (int i = 0; i < captures.Length; i++)
                {
                    ref var capture = ref captures.ElementAt(i);
                    if (capture.makeResident)
                        continue;
                    capture.gpuStart  = (int)dynamicStart;
                    dynamicStart     += (uint)capture.glyphCount;
                    if (capture.textShaderIndexPtr != null)
                    {
                        capture.textShaderIndexPtr->firstGlyphIndex = (uint)capture.gpuStart;
                        capture.textShaderIndexPtr->glyphCount      = (uint)capture.glyphCount;
                        //UnityEngine.Debug.Log($"Allocated dynamic range: {capture.textShaderIndexPtr->firstGlyphIndex}, {capture.textShaderIndexPtr->glyphCount}");
                    }
                }

                glyphGpuTable.bufferSize.Value = residentBufferSize;

                // Remove empty buffers from upload list.
                int dstIndex = 0;
                for (int i = 0; i < captures.Length; i++)
                {
                    if (captures[i].glyphCount != 0)
                    {
                        captures[dstIndex] = captures[i];
                        dstIndex++;
                    }
                }
                captures.Length = dstIndex;
            }
        }

        [BurstCompile]
        struct AllocateGlyphsInAtlasJob : IJob
        {
            [ReadOnly] public NativeParallelHashSet<uint> glyphEntryIDsToRasterizeSet;
            public NativeList<uint>                       glyphEntryIDsToRasterize;
            public NativeList<uint>                       atlasDirtyIDs;
            public GlyphTable                             glyphTable;
            public AtlasTable                             atlasTable;

            public void Execute()
            {
                var count                         = glyphEntryIDsToRasterizeSet.Count();
                glyphEntryIDsToRasterize.Capacity = count;
                foreach (var glyph in glyphEntryIDsToRasterizeSet)
                    glyphEntryIDsToRasterize.AddNoResize(glyph);
                glyphEntryIDsToRasterize.Sort();

                UnsafeHashSet<uint> dirtyAtlasIDSet = new UnsafeHashSet<uint>(32, Allocator.Temp);

                foreach (var glyph in glyphEntryIDsToRasterize)
                {
                    ref var glyphEntry = ref glyphTable.GetEntryRW(glyph);
                    var doublePadding = 2 * glyphEntry.padding;
                    atlasTable.Allocate(glyph, (short)(glyphEntry.width + doublePadding), (short)(glyphEntry.height + doublePadding), out glyphEntry.x, out glyphEntry.y, out glyphEntry.z);
                    uint id  = (uint)glyphEntry.z;
                    id      |= glyph & 0xc0000000;
                    dirtyAtlasIDSet.Add(id);
                }

                atlasDirtyIDs.Capacity = dirtyAtlasIDSet.Count;
                foreach (var id in dirtyAtlasIDSet)
                    atlasDirtyIDs.AddNoResize(id);
                atlasDirtyIDs.Sort();
            }
        }
        #endregion

        #region Write Jobs
        [BurstCompile]
        struct RasterizeJob : IJobFor
        {
            [ReadOnly] public NativeArray<uint>                                                           glyphEntryIDsToRasterize;
            [ReadOnly] public GlyphTable                                                                  glyphTable;
            [ReadOnly] public FontTable                                                                   fontTable;
            [NativeDisableParallelForRestriction] public NativeArray<TextureAtlasArray<byte>.AtlasPtr>    sdf8Ptrs;
            [NativeDisableParallelForRestriction] public NativeArray<TextureAtlasArray<ushort>.AtlasPtr>  sdf16Ptrs;
            [NativeDisableParallelForRestriction] public NativeArray<TextureAtlasArray<Color32>.AtlasPtr> bitmapPtrs;

            [NativeDisableUnsafePtrRestriction] public DrawDelegates  drawDelegates;
            [NativeDisableUnsafePtrRestriction] public PaintDelegates paintDelegates;

            [NativeDisableContainerSafetyRestriction] DrawData drawData;
            [NativeSetThreadIndex] int                         threadIndex;

            public void Execute(int glyphIndex)
            {
                var glyphEntry = glyphTable.GetEntry(glyphEntryIDsToRasterize[glyphIndex]);

                // If the glyph doesn't have any real size, then there's nothing to rasterize.
                if (glyphEntry.width == 0 || glyphEntry.height == 0)
                    return;

                var face         = fontTable.faces[glyphEntry.key.faceIndex];
                var font         = fontTable.GetOrCreateFont(glyphEntry.key.faceIndex, threadIndex);
                var samplingSize = glyphEntry.key.textureSize.GetSamplingSize();
                font.SetScale(samplingSize, samplingSize);
                var maxDeviation = BezierMath.GetMaxDeviation(font.GetScale().x);
                if (!drawData.edges.IsCreated)
                    drawData = new DrawData(256, 16, maxDeviation, Allocator.Temp);
                drawData.Clear();
                drawData.maxDeviation = maxDeviation;

                if (glyphEntry.key.format == RenderFormat.SDF8)
                {
                    font.DrawGlyph(glyphEntry.key.glyphIndex, drawDelegates, ref drawData);
                    var sdf8TextureSlice = GetSdf8TextureSlice(glyphEntry.z);
                    var doublePadding = 2*glyphEntry.padding;
                    var atlasRect        = new GlyphRect(glyphEntry.x, glyphEntry.y, glyphEntry.width + doublePadding, glyphEntry.height + doublePadding);
                    //Debug.Log($"new: {atlasRect.x} {atlasRect.y} {atlasRect.width} {atlasRect.height}");
                    SDF_SPMD.SDFGenerateSubDivisionLineEdges(face.sdfOrientation,
                                                             ref drawData,
                                                             sdf8TextureSlice,
                                                             atlasRect,
                                                             glyphEntry.padding,
                                                             kTextureDimension,
                                                             kTextureDimension,
                                                             glyphEntry.padding);
                }
                else if (glyphEntry.key.format == RenderFormat.Bitmap8888)
                {
                    PaintData paintData     = default;
                    paintData.drawDelegates = drawDelegates;
                    paintData.clipGlyph     = drawData;
                    paintData.Clear();
                    font.PaintGlyph(glyphEntry.key.glyphIndex, ref paintData, paintDelegates, 0, new ColorARGB(255, 0, 0, 0));
                    if (paintData.paintSurface.Length > 0)
                    {
                        var bitmapTextureSlice = GetBitmapTextureSlice(glyphEntry.z);
                        for (int y = 0; y < glyphEntry.height; y++)
                        {
                            for (int x = 0; x < glyphEntry.width; x++)
                            {
                                var argb                     = paintData.paintSurface[y * glyphEntry.width + x];
                                var dstY                     = y + glyphEntry.y;
                                var dstX                     = x + glyphEntry.x;
                                var dstIndex                 = dstY * kTextureDimension + dstX;
                                bitmapTextureSlice[dstIndex] = new Color32(argb.r, argb.g, argb.b, argb.a);
                            }
                        }
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError("SDF16 is not supported yet.");
                }
            }

            unsafe NativeArray<byte> GetSdf8TextureSlice(short z)
            {
                foreach (var ptr in sdf8Ptrs)
                {
                    if (ptr.atlasIndex == z)
                    {
                        return CollectionHelper.ConvertExistingDataToNativeArray<byte>(ptr.ptr, ptr.dimension * ptr.dimension, Allocator.None, true);
                    }
                }
                return default;
            }

            unsafe NativeArray<Color32> GetBitmapTextureSlice(short z)
            {
                foreach (var ptr in bitmapPtrs)
                {
                    if (ptr.atlasIndex == z)
                    {
                        return CollectionHelper.ConvertExistingDataToNativeArray<Color32>(ptr.ptr, ptr.dimension * ptr.dimension, Allocator.None, true);
                    }
                }
                return default;
            }
        }

        [BurstCompile]
        struct WriteRenderGlyphsToGpuJob : IJobFor
        {
            [ReadOnly] public GlyphTable                                          glyphTable;
            [ReadOnly] public NativeArray<RenderGlyphCapture>                     captures;
            [NativeDisableParallelForRestriction] public NativeArray<RenderGlyph> uploadArray;
            public NativeArray<uint3>                                             uploadMetaArray;

            public void Execute(int index)
            {
                const float kTextureResolutionFloatInverse = 1f / kTextureDimension;
                var         capture                        = captures[index];
                for (int i = 0; i < capture.glyphCount; i++)
                {
                    var glyph = capture.glyphBuffer[i];
                    var entry = glyphTable.GetEntry(glyph.glyphEntryId);

                    glyph.arrayIndex = (uint)entry.z;
                    // Todo: Currently we are overwriting these values because glyph generation doesn't need to augment these.
                    // Should we change that there? Or should we change the RenderGlyph comment?

                    glyph.blUVA = new float2(entry.x, entry.y) * kTextureResolutionFloatInverse;
                    glyph.trUVA = glyph.blUVA + (new float2(entry.width, entry.height) + entry.padding * 2) * kTextureResolutionFloatInverse;

                    // Debug:
                    //if (i < 5 && entry.key.format == RenderFormat.SDF8)
                    //{
                    //    UnityEngine.Debug.Log($"x: {entry.x}, y: {entry.y}, width: {entry.width}, height: {entry.height}, arrayIndex: {entry.z}, blUVA: {glyph.blUVA}, trUVA: {glyph.trUVA}");
                    //}
                    capture.glyphBuffer[i] = glyph;

                    uploadArray[capture.writeStart + i] = glyph;
                }
                uploadMetaArray[index] = new uint3((uint)capture.writeStart, (uint)capture.gpuStart, (uint)capture.glyphCount);
            }
        }
        #endregion
    }
}

