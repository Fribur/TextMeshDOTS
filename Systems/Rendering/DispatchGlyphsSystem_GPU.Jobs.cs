using System;
using System.Threading;
using TextMeshDOTS.HarfBuzz;
using TextMeshDOTS.LatiosInterop.Unsafe;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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

                var shaderPtr    = chunk.GetComponentDataPtrRW(ref textShaderIndexHandle);
                var residentPtr  = (ResidentRange*)chunk.GetRequiredComponentDataPtrRW(ref residentRangeHandle);
                var gpuStates    = (GpuState*)chunk.GetRequiredComponentDataPtrRW(ref gpuStateHandle);
                var glyphBuffers = chunk.GetBufferAccessor(ref renderGlyphHandle);
                var gpuStateMask = chunk.GetEnabledMask(ref gpuStateHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndex))
                {
                    gpuStateMask[entityIndex] = false;
                    bool resident             = gpuStates[entityIndex].state == GpuState.State.DynamicPromoteToResident ||
                                                gpuStates[entityIndex].state == GpuState.State.ResidentUncommitted;
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
                            glyphEntryIDsToEncodeSet.Add(glyph.glyph.glyphEntryId);
                    }
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
            [ReadOnly] [NativeDisableUnsafePtrRestriction] public IntPtr               gpuDrawContext;
            [ReadOnly] public NativeArray<uint>    glyphEntryIDsToEncode;
            public GlyphTable           glyphTable;  // Made RW to store blobOffset
            [ReadOnly] public FontTable            fontTable;

            public NativeArray<byte>               encodedBlobs;
            public NativeArray<uint3>              blobMeta;
            public NativeReference<int>            totalSizeRef;

            public void Execute()
            {
                int runningOffset = 0;

                for (int i = 0; i < glyphEntryIDsToEncode.Length; i++)
                {
                    var glyphEntryID = glyphEntryIDsToEncode[i];
                    var glyphEntry = glyphTable.GetEntry(glyphEntryID);                    

                    // Skip zero-size glyphs
                    if (glyphEntry.width == 0 || glyphEntry.height == 0)
                    {
                        blobMeta[i] = default;
                        continue;
                    }

                    var face = fontTable.faces[glyphEntry.key.faceIndex];
                    var font = fontTable.GetOrCreateFont(glyphEntry.key.faceIndex, 0);

                    if (face.HasVarData && font.currentVariableProfileIndex != glyphEntry.key.variableProfileIndex)
                        font = fontTable.SetVariableProfile(glyphEntry.key.faceIndex, 0, glyphEntry.key.variableProfileIndex);

                    // Set scale for GPU encoding (use font UPem for design units)
                    int upem = (int)face.UnitsPerEM;
                    Harfbuzz.hb_gpu_draw_set_scale(gpuDrawContext, upem, upem);                    

                    // Reset draw context for new glyph
                    Harfbuzz.hb_gpu_draw_reset(gpuDrawContext);

                    // Draw glyph outline into GPU encoder
                    Harfbuzz.hb_gpu_draw_glyph(gpuDrawContext, font.ptr, glyphEntry.key.glyphIndex);

                    // Encode to blob
                    IntPtr blob = Harfbuzz.hb_gpu_draw_encode(gpuDrawContext, out GlyphExtents glyphExtents);                    

                    if (blob == IntPtr.Zero)
                    {
                        blobMeta[i] = default;
                        continue;
                    }

                    // Get blob data and length
                    uint blobLength = Harfbuzz.hb_blob_get_length(blob);
                    byte* blobData = Harfbuzz.hb_blob_get_data(blob, out _);

                    // Store blob data
                    int alignedBlobSize = (int)((blobLength + 7) & ~7);
                    UnsafeUtility.MemCpy(
                        (byte*)encodedBlobs.GetUnsafePtr() + runningOffset,
                        blobData,
                        blobLength
                    );

                    // Store blob offset in glyph entry for later use in WriteRenderGlyphsToGpuJob_GPU
                    ref var entryRW = ref glyphTable.GetEntryRW(glyphEntryID);
                    entryRW.blobOffset = runningOffset;

                    // Store metadata: (arrayIndex, blobOffset, blobLength)
                    // uint x = (uint)glyphEntry.z;
                    // x |= ((uint)glyphEntry.key.format) << 30;
                     uint y = (uint)runningOffset;
                     uint z = blobLength;
                    blobMeta[i] = new uint3(y, y, z);

                    runningOffset += alignedBlobSize;                    

                    // Recycle blob for buffer reuse
                    Harfbuzz.hb_gpu_draw_recycle_blob(gpuDrawContext, blob);
                }

                totalSizeRef.Value = runningOffset;
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
                                    capture.textShaderIndexPtr->glyphCount      = (uint)capture.glyphCount;
                                }
                                capture.residentRangePtr->start = newLocation;
                                capture.residentRangePtr->count = (uint)capture.glyphCount;
                                //UnityEngine.Debug.Log($"Allocated resident range: {capture.residentRangePtr->start}, {capture.residentRangePtr->count}");
                            }
                            else
                            {
                                capture.gpuStart = (int)capture.residentRangePtr->start;
                                //UnityEngine.Debug.Log($"Updated resident range: {capture.residentRangePtr->start}, {capture.residentRangePtr->count}");
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
                if (dynamicCount > 0)
                {
                    GapAllocator.TryAllocate(glyphGpuTable.residentGaps, (uint)dynamicCount, ref residentBufferSize, out var dynamicStart);
                    //UnityEngine.Debug.Log($"Allocated dynamic region: {dynamicStart}, {dynamicCount}");
                    glyphGpuTable.dispatchDynamicGaps.Add(new uint2(dynamicStart, (uint)dynamicCount));
                    for (int i = 0; i < captures.Length; i++)
                    {
                        ref var capture = ref captures.ElementAt(i);
                        if (capture.makeResident)
                            continue;
                        capture.gpuStart = (int)dynamicStart;
                        dynamicStart += (uint)capture.glyphCount;
                        if (capture.textShaderIndexPtr != null)
                        {
                            capture.textShaderIndexPtr->firstGlyphIndex = (uint)capture.gpuStart;
                            capture.textShaderIndexPtr->glyphCount = (uint)capture.glyphCount;
                            //UnityEngine.Debug.Log($"Allocated dynamic range: {capture.textShaderIndexPtr->firstGlyphIndex}, {capture.textShaderIndexPtr->glyphCount}");
                        }
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
    }
}
