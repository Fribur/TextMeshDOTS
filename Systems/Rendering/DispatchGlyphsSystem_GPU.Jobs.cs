using System;
using System.Collections.Generic;
using TextMeshDOTS.HarfBuzz;
using TextMeshDOTS.LatiosInterop.Unsafe;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static TextMeshDOTS.DispatchGlyphsSystem;

namespace TextMeshDOTS
{
    public unsafe partial class DispatchGlyphsSystem_GPU
    {
        #region Collect Jobs

        [BurstCompile]
        struct AllocateJob : IJob
        {
            [ReadOnly] public GlyphTable       glyphTable;
            public NativeParallelHashSet<uint> glyphEntryIDsToEncodeSet;

            public void Execute()
            {
                glyphEntryIDsToEncodeSet.Capacity = 3 * math.max(glyphTable.glyphEntries.Length, glyphEntryIDsToEncodeSet.Capacity);
            }
        }

        [BurstCompile]
        struct CaptureRenderGlyphsJob_GPU : IJobChunk
        {
            [ReadOnly] public GlyphTable                            glyphTable;
            [ReadOnly] public BufferTypeHandle<PreviousRenderGlyph> renderGlyphHandle;
            public ComponentTypeHandle<TextShaderIndex>             textShaderIndexHandle;
            public ComponentTypeHandle<ResidentRange>               residentRangeHandle;
            public ComponentTypeHandle<GpuState>                    gpuStateHandle;

            [NativeDisableParallelForRestriction] public NativeStream.Writer renderGlyphCapturesStream;
            public NativeParallelHashSet<uint>.ParallelWriter                glyphEntryIDsToEncodeSet;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                renderGlyphCapturesStream.BeginForEachIndex(unfilteredChunkIndex);

                var shaderPtr        = chunk.GetComponentDataPtrRW(ref textShaderIndexHandle);
                var residentPtr      = (ResidentRange*)chunk.GetRequiredComponentDataPtrRW(ref residentRangeHandle);
                var gpuStates        = (GpuState*)chunk.GetRequiredComponentDataPtrRW(ref gpuStateHandle);
                var glyphBuffers     = chunk.GetBufferAccessor(ref renderGlyphHandle);
                var gpuStateMask     = chunk.GetEnabledMask(ref gpuStateHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndex))
                {
                    gpuStateMask[entityIndex] = false;
                    bool resident             = gpuStates[entityIndex].state == GpuState.State.DynamicPromoteToResident ||
                                                gpuStates[entityIndex].state == GpuState.State.ResidentUncommitted;
                    gpuStates[entityIndex].state = resident ? GpuState.State.Resident : GpuState.State.Dynamic;
                    var glyphs                   = glyphBuffers[entityIndex];

                    foreach (var glyph in glyphs)
                    {
                        var entry = glyphTable.GetEntry(glyph.glyph.glyphEntryId);
                        if (!entry.isInHbGPUAtlas)
                            glyphEntryIDsToEncodeSet.Add(glyph.glyph.glyphEntryId);
                    }

                    renderGlyphCapturesStream.Write(new RenderGlyphCapture
                    {
                        glyphBuffer        = glyphs.Length != 0 ? (RenderGlyph*)glyphs.GetUnsafeReadOnlyPtr() : null,
                        glyphCount         = glyphs.Length,
                        makeResident       = resident,
                        residentRangePtr   = residentPtr + entityIndex,
                        textShaderIndexPtr = shaderPtr != null ? shaderPtr + entityIndex : null,
                    });
                }

                renderGlyphCapturesStream.EndForEachIndex();
            }
        }

        [BurstCompile]
        struct CopyGlyphEntryIDsJob : IJob
        {
            [ReadOnly] public NativeParallelHashSet<uint> glyphEntryIDsToEncodeSet;
            public NativeList<uint>                       glyphEntryIDsToEncode;

            public void Execute()
            {
                var count = glyphEntryIDsToEncodeSet.Count();
                glyphEntryIDsToEncode.Capacity = count;
                foreach (var glyphEntryID in glyphEntryIDsToEncodeSet)
                {
                    glyphEntryIDsToEncode.AddNoResize(glyphEntryID);
                }
            }
        }

        #endregion

        #region Write Jobs

        [BurstCompile]
        struct EncodeGlyphsToGpuBlobsJob : IJob
        {
            [ReadOnly] public NativeArray<uint>    glyphEntryIDsToEncode;
            public GlyphTable                      glyphTable;
            public GlyphGpuTable                   glyphGpuTable;
            [ReadOnly] public FontTable            fontTable;

            public NativeList<byte>  encodedBlobsTemp;
            public NativeList<uint3> blobMeta;

            const uint kGcThresholdBytes = 268435456u; // 256 MB

            public void Execute()
            {
                var gpuDrawContext = Harfbuzz.hb_gpu_draw_create_or_fail(); //assume most glyphs are monochrome, so just create
                IntPtr gpuPaintContext = IntPtr.Zero;
                IntPtr blob;

                int encodedBlobsOffset     = 0;
                var hbGpuAtlasSize       = glyphGpuTable.bufferSizeHbGpuAtlas.Value;

                // Run GC before allocating new blobs if buffer is over threshold
                if (hbGpuAtlasSize > kGcThresholdBytes)
                {
                    hbGpuAtlasSize = CollectHbGpuAtlasGarbage(hbGpuAtlasSize);
                    glyphGpuTable.bufferSizeHbGpuAtlas.Value = hbGpuAtlasSize;
                }

                for (int i = 0; i < glyphEntryIDsToEncode.Length; i++)
                {
                    var glyphEntryID = glyphEntryIDsToEncode[i];
                    var glyphEntry = glyphTable.GetEntry(glyphEntryID);

                    if (glyphEntry.width == 0 || glyphEntry.height == 0)
                        continue;

                    var face = fontTable.faces[glyphEntry.key.faceIndex];
                    var font = fontTable.GetOrCreateFont(glyphEntry.key.faceIndex, 0);

                    if (face.HasVarData && font.currentVariableProfileIndex != glyphEntry.key.variableProfileIndex)
                        font = fontTable.SetVariableProfile(glyphEntry.key.faceIndex, 0, glyphEntry.key.variableProfileIndex);

                    // note: for best quality slug rendering, do NOT scale font at all, and do not use hb_gpu_draw_set_scale
                    // otherwise ensure font object is scaled identically in all jobs doing so, and set hb_gpu_draw_set_scale to upem                    

                    if (Hint.Unlikely(face.hasColor))
                    {
                        if (gpuPaintContext == IntPtr.Zero)
                            gpuPaintContext = Harfbuzz.hb_gpu_paint_create_or_fail();
                        Harfbuzz.hb_gpu_paint_glyph(gpuPaintContext, font.ptr, glyphEntry.key.glyphIndex);
                        blob = Harfbuzz.hb_gpu_paint_encode(gpuPaintContext, out _);
                    }
                    else
                    {
                        Harfbuzz.hb_gpu_draw_glyph(gpuDrawContext, font.ptr, glyphEntry.key.glyphIndex);
                        blob = Harfbuzz.hb_gpu_draw_encode(gpuDrawContext, out _);
                    }

                    if (blob == IntPtr.Zero)
                        continue;

                    //get blob and store
                    byte* blobData = Harfbuzz.hb_blob_get_data(blob, out uint blobLength);
                    int alignedBlobSize = (int)((blobLength + 7) & ~7);
                    GapAllocator.TryAllocate(glyphGpuTable.hbGpuAtlasGaps, (uint)alignedBlobSize, ref hbGpuAtlasSize, out var blobOffset);
                    encodedBlobsTemp.AddRange(blobData, (int)blobLength);
                    int padding = alignedBlobSize - (int)blobLength;
                    for (int p = 0; p < padding; p++) // pad to alignment boundary
                        encodedBlobsTemp.Add(0);

                    ref var entryRW = ref glyphTable.GetEntryRW(glyphEntryID);
                    entryRW.blobOffset = (int)blobOffset;
                    entryRW.blobAlignedSize = alignedBlobSize;

                    blobMeta.Add(new uint3((uint)encodedBlobsOffset, blobOffset, (uint)alignedBlobSize));
                    encodedBlobsOffset += alignedBlobSize;

                    if (Hint.Unlikely(face.hasColor))
                    {
                        Harfbuzz.hb_gpu_paint_recycle_blob(gpuPaintContext, blob);
                        Harfbuzz.hb_gpu_paint_reset(gpuPaintContext);
                    }
                    else
                    {
                        Harfbuzz.hb_gpu_draw_recycle_blob(gpuDrawContext, blob);
                        Harfbuzz.hb_gpu_draw_reset(gpuDrawContext);
                    }
                }

                glyphGpuTable.bufferSizeHbGpuAtlas.Value = hbGpuAtlasSize;

                Harfbuzz.hb_gpu_draw_destroy(gpuDrawContext);
                if (gpuPaintContext != IntPtr.Zero)
                    Harfbuzz.hb_gpu_paint_destroy(gpuPaintContext);                       
            }

            uint CollectHbGpuAtlasGarbage(uint currentSize)
            {
                Debug.Log("Collect garbage in HbGPUAtlas");
                // Free all zero-refcount glyph blobs into hbGpuAtlasGaps, then coalesce
                foreach (var glyphEntryID in glyphGpuTable.hbGpuAtlasGcCandidates)
                {
                    ref var entry = ref glyphTable.GetEntryRW(glyphEntryID);
                    if (entry.refCount > 0 || !entry.isInHbGPUAtlas)
                        continue;

                    glyphGpuTable.hbGpuAtlasGaps.Add(new uint2((uint)entry.blobOffset, (uint)entry.blobAlignedSize));
                    entry.blobOffset = -1;
                    entry.blobAlignedSize = 0;
                }
                glyphGpuTable.hbGpuAtlasGcCandidates.Clear();

                // sort by start offset before coalescing, so TryAllocate is deterministic
                glyphGpuTable.hbGpuAtlasGaps.Sort(new GapStartOffsetComparer());

                return GapAllocator.CoalesceGaps(glyphGpuTable.hbGpuAtlasGaps, currentSize);
            }
            struct GapStartOffsetComparer : IComparer<uint2>
            {
                public int Compare(uint2 a, uint2 b)
                {
                    return a.x.CompareTo(b.x);
                }
            }
        }      

  
        [BurstCompile]
        struct WriteRenderGlyphsToGpuJob_GPU : IJobFor
        {
            [ReadOnly] public GlyphTable                                          glyphTable;
            [ReadOnly] public NativeArray<RenderGlyphCapture>                     captures;
            [NativeDisableParallelForRestriction] public NativeArray<RenderGlyph> uploadArray;
            public NativeArray<uint3>                                             uploadMetaArray;

            public void Execute(int index)
            {
                var capture = captures[index];
                for (int i = 0; i < capture.glyphCount; i++)
                {
                    var glyph = capture.glyphBuffer[i];
                    var entry = glyphTable.GetEntry(glyph.glyphEntryId);

                    // For GPU blob approach, we store the blob offset (in bytes) in arrayIndex
                    // This is first texel index, a plain integer to access the RGBAI16 buffer
                    // because there is no I16 in HLSL, we use not StructuredBuffer<int4>, but StructuredBuffer<int2>
                    // and then decode the 4 I16 components via bit shifts
                    glyph.arrayIndex = (uint)(entry.blobOffset / 8);

                    uploadArray[capture.writeStart + i] = glyph; 
                }
                uploadMetaArray[index] = new uint3((uint)capture.writeStart, (uint)capture.gpuStart, (uint)capture.glyphCount);
            }
        }

        #endregion

        [BurstCompile]
        struct AssignShaderIndicesJob : IJob
        {
            [ReadOnly] public NativeStream        renderGlyphCapturesStream;
            public NativeList<RenderGlyphCapture> captures;
            public GlyphTable                     glyphTable;
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
                        var capture        = reader.Read<RenderGlyphCapture>();
                        capture.writeStart = writeBufferOffset;
                        writeBufferOffset += capture.glyphCount;

                        if (capture.makeResident)
                        {
                            // Allocate _tmdGlyphs buffer space (glyph indices)
                            if (capture.residentRangePtr->glyphCount != capture.glyphCount)
                            {
                                GapAllocator.TryAllocate(glyphGpuTable.residentGaps, (uint)capture.glyphCount, ref residentBufferSize, out var newLocation);
                                capture.gpuStart = (int)newLocation;
                                if (capture.textShaderIndexPtr != null)
                                {
                                    capture.textShaderIndexPtr->firstGlyphIndex = newLocation;
                                    capture.textShaderIndexPtr->glyphCount      = (uint)capture.glyphCount;
                                }
                                capture.residentRangePtr->firstGlyphIndex = newLocation;
                                capture.residentRangePtr->glyphCount      = (uint)capture.glyphCount;
                            }
                            else
                            {
                                capture.gpuStart = (int)capture.residentRangePtr->firstGlyphIndex;
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

                // Allocate dynamic region for _tmdGlyphs
                if (dynamicCount > 0)
                {
                    GapAllocator.TryAllocate(glyphGpuTable.residentGaps, (uint)dynamicCount, ref residentBufferSize, out var dynamicStart);
                    glyphGpuTable.dispatchDynamicGaps.Add(new uint2(dynamicStart, (uint)dynamicCount));
                    for (int i = 0; i < captures.Length; i++)
                    {
                        ref var capture = ref captures.ElementAt(i);
                        if (capture.makeResident)
                            continue;
                        capture.gpuStart = (int)dynamicStart;
                        dynamicStart    += (uint)capture.glyphCount;
                        if (capture.textShaderIndexPtr != null)
                        {
                            capture.textShaderIndexPtr->firstGlyphIndex = (uint)capture.gpuStart;
                            capture.textShaderIndexPtr->glyphCount      = (uint)capture.glyphCount;
                        }
                    }
                }

                glyphGpuTable.bufferSize.Value = residentBufferSize;

                // Remove empty captures from upload list.
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
    }
}
