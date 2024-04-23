//using System;
//using Unity.Burst;
//using Unity.Collections.LowLevel.Unsafe;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Transforms;
//using UnityEngine;
//using UnityEngine.Rendering;
//using TextMeshDOTS.Rendering;
//using TextMeshDOTS;

////[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
//[UpdateInGroup(typeof(PresentationSystemGroup))]
//[BurstCompile]
//public unsafe partial class ProceduralIndirectDefaultSystem : SystemBase
//{
//    EntityQuery textToRenderQ;
//    UnityObjectRef<Material> proceduralMaterial;

//    private BatchRendererGroup _batchRendererGroup;
//    private GraphicsBuffer _gpuPersistentInstanceData;
//    private GraphicsBuffer _gpuVisibleInstances;
//    private GraphicsBuffer _gpuIndirectBuffer;
//    private uint _gpuVisibleInstancesWindow;

//    private GraphicsBuffer _gpuPositions;
//    private GraphicsBuffer _gpuNormals;
//    private GraphicsBuffer _gpuTangents;
//    private uint _elementsPerDraw;

//    private BatchID _batchID;
//    private BatchMaterialID _proceduralMaterialID;
//    private int _itemCount;
//    private uint _instancesPerItem;
//    private int _visibleItemCount;
//    private bool _initialized;

//    private NativeArray<Vector4> _sysmemBuffer;
//    private NativeArray<int> _sysmemVisibleInstances;

//    private bool UseConstantBuffer => BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer;

//    protected override void OnCreate()
//    {
//        textToRenderQ = SystemAPI.QueryBuilder()
//            .WithAll<RenderGlyph>()
//            .WithAll<LocalToWorld>()
//            .Build();
//        RequireForUpdate(textToRenderQ);

//    }
//    protected override void OnUpdate()
//    {
//        var fontBlobReferenceEntities = textToRenderQ.ToEntityArray(Allocator.Temp);
//        var fontBlobReferenceEntity = fontBlobReferenceEntities[0];
//        //var fontBlobReference = SystemAPI.GetComponent<FontBlobReference>(fontBlobReferenceEntity);
//        var renderMeshUnmanaged = SystemAPI.GetComponent<TestProceduralMaterial>(fontBlobReferenceEntity);

//        if (!_initialized)
//        {
//            InitializeBRG(ref renderMeshUnmanaged);
//            Debug.Log("Initialzied BRG");
//        }
//    }
//    protected override void OnDestroy()
//    {
//        if (_initialized)
//        {
//            _batchRendererGroup.RemoveBatch(_batchID);
//            if (proceduralMaterial) _batchRendererGroup.UnregisterMaterial(_proceduralMaterialID);

//            _batchRendererGroup.Dispose();
//            _gpuPersistentInstanceData.Dispose();
//            _gpuVisibleInstances.Dispose();
//            _gpuIndirectBuffer.Dispose();
//            _gpuPositions.Dispose();
//            _gpuNormals.Dispose();
//            _gpuTangents.Dispose();
//            _sysmemBuffer.Dispose();
//            _sysmemVisibleInstances.Dispose();
//        }
//    }

//    public JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
//    {
//        if (!_initialized)
//        {
//            return new JobHandle();
//        }

//        BatchCullingOutputDrawCommands drawCommands = new BatchCullingOutputDrawCommands();

//        var filterSettings = new BatchFilterSettings
//        {
//            renderingLayerMask = 1,
//            layer = 0,
//            motionMode = MotionVectorGenerationMode.ForceNoMotion,
//            shadowCastingMode = ShadowCastingMode.On,
//            receiveShadows = true,
//            staticShadowCaster = false,
//            allDepthSorted = false
//        };
//        drawCommands.drawRangeCount = 1;
//        drawCommands.drawRanges = Malloc<BatchDrawRange>(1);

//        // ProceduralIndirect draw range
//        drawCommands.drawRanges[0] = new BatchDrawRange
//        {
//            drawCommandsType = BatchDrawCommandType.ProceduralIndirect,
//            drawCommandsBegin = 0,
//            drawCommandsCount = 5,
//            filterSettings = filterSettings,
//        };

//        drawCommands.visibleInstances = Malloc<int>(_visibleItemCount);
//        for (int i = 0; i < _visibleItemCount; ++i)
//            drawCommands.visibleInstances[i] = _sysmemVisibleInstances[i];

//        drawCommands.visibleInstanceCount = _visibleItemCount;


//        //some variables to track drawcommands and resulting offsets into buffer        
//        uint _gpuIndirectBufferBaseOffset = 0;
//        uint indirectDrawArgsSize = GraphicsBuffer.IndirectDrawArgs.size;
//        uint drawCallCount = 5;
//        uint visibleBaseOffset = 0;


//        // ProceduralIndirect draw command
//        drawCommands.proceduralIndirectDrawCommandCount = 5;
//        drawCommands.proceduralIndirectDrawCommands = Malloc<BatchDrawCommandProceduralIndirect>(5);
//        for (uint i = 0; i < drawCallCount; ++i)
//        {
//            drawCommands.proceduralIndirectDrawCommands[i] = new BatchDrawCommandProceduralIndirect
//            {
//                flags = BatchDrawCommandFlags.None,
//                batchID = _batchID,
//                materialID = _proceduralMaterialID,
//                splitVisibilityMask = 0xff,
//                sortingPosition = 0,
//                visibleOffset = visibleBaseOffset + i * _instancesPerItem,
//                topology = MeshTopology.Triangles,
//                indexBufferHandle = default,
//                visibleInstancesBufferHandle = _gpuVisibleInstances.bufferHandle,
//                visibleInstancesBufferWindowOffset = 0,
//                visibleInstancesBufferWindowSizeBytes = _gpuVisibleInstancesWindow,
//                indirectArgsBufferHandle = _gpuIndirectBuffer.bufferHandle,
//                indirectArgsBufferOffset = _gpuIndirectBufferBaseOffset + i * indirectDrawArgsSize,
//            };
//        }
//        visibleBaseOffset += drawCallCount * _instancesPerItem;
//        _gpuIndirectBufferBaseOffset += drawCallCount * indirectDrawArgsSize;

//        drawCommands.instanceSortingPositions = null;
//        drawCommands.instanceSortingPositionFloatCount = 0;

//        cullingOutput.drawCommands[0] = drawCommands;
//        return new JobHandle();
//    }


//    void InitializeBRG(ref TestProceduralMaterial testProceduralMaterial)
//    {
//        uint kBRGBufferMaxWindowSize = 128 * 1024 * 1024;
//        uint kBRGBufferAlignment = 16;
//        if (UseConstantBuffer)
//        {
//            kBRGBufferMaxWindowSize = (uint)(BatchRendererGroup.GetConstantBufferMaxWindowSize());
//            kBRGBufferAlignment = (uint)(BatchRendererGroup.GetConstantBufferOffsetAlignment());
//        }

//        _batchRendererGroup = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);

//        var itemsPerRow = 10;
//        _itemCount = 1 * itemsPerRow;
//        _visibleItemCount = _itemCount;
//        _instancesPerItem = 2;

//        // Bounds
//        Bounds bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
//        _batchRendererGroup.SetGlobalBounds(bounds);

//        // Register procedural material
//        proceduralMaterial = testProceduralMaterial.material;
//        _proceduralMaterialID = _batchRendererGroup.RegisterMaterial(proceduralMaterial);

//        // Batch metadata buffer
//        int objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
//        int matrixPreviousID = Shader.PropertyToID("unity_MatrixPreviousM");
//        int worldToObjectID = Shader.PropertyToID("unity_WorldToObject");
//        int colorID = Shader.PropertyToID("_BaseColor");
//        int positionsID = Shader.PropertyToID("_Positions");
//        int normalsID = Shader.PropertyToID("_Normals");
//        int tangentsID = Shader.PropertyToID("_Tangents");
//        int baseIndexID = Shader.PropertyToID("_BaseIndex");

//        // Generate a grid of objects...
//        int bigDataBufferVector4Count = 4 + _itemCount * (3 * 3 + 1);      // 4xfloat4 zero + per instance = { 3x mat4x3, 1x float4 color }
//        uint brgWindowSize = 0;
//        _sysmemBuffer = new NativeArray<Vector4>(bigDataBufferVector4Count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
//        if (UseConstantBuffer)
//        {
//            _gpuPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Constant, (int)bigDataBufferVector4Count * 16 / (4 * 4), 4 * 4);
//            brgWindowSize = (uint)bigDataBufferVector4Count * 16;
//        }
//        else
//        {
//            _gpuPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, (int)bigDataBufferVector4Count * 16 / 4, 4);
//        }

//        // 64 bytes of zeroes, so loads from address 0 return zeroes. This is a BatchRendererGroup convention.
//        int positionOffset = 4;
//        _sysmemBuffer[0] = new Vector4(0, 0, 0, 0);
//        _sysmemBuffer[1] = new Vector4(0, 0, 0, 0);
//        _sysmemBuffer[2] = new Vector4(0, 0, 0, 0);
//        _sysmemBuffer[3] = new Vector4(0, 0, 0, 0);

//        // Matrices
//        var itemCountOffset = _itemCount * 3; // one packed matrix
//        for (int i = 0; i < _itemCount; ++i)
//        {
//            /*
//             *  mat4x3 packed like this:
//             *
//                    float4x4(
//                            p1.x, p1.w, p2.z, p3.y,
//                            p1.y, p2.x, p2.w, p3.z,
//                            p1.z, p2.y, p3.x, p3.w,
//                            0.0, 0.0, 0.0, 1.0
//                        );
//            */

//            float px = (i % itemsPerRow) * 2.0f;
//            float py = -(i / itemsPerRow) * 2.0f;
//            float pz = 0.0f;

//            // compute the new current frame matrix
//            _sysmemBuffer[positionOffset + i * 3 + 0] = new Vector4(1, 0, 0, 0);
//            _sysmemBuffer[positionOffset + i * 3 + 1] = new Vector4(1, 0, 0, 0);
//            _sysmemBuffer[positionOffset + i * 3 + 2] = new Vector4(1, px, py, pz);

//            // we set the same matrix for the previous matrix
//            _sysmemBuffer[positionOffset + i * 3 + 0 + itemCountOffset] = _sysmemBuffer[positionOffset + i * 3 + 0];
//            _sysmemBuffer[positionOffset + i * 3 + 1 + itemCountOffset] = _sysmemBuffer[positionOffset + i * 3 + 1];
//            _sysmemBuffer[positionOffset + i * 3 + 2 + itemCountOffset] = _sysmemBuffer[positionOffset + i * 3 + 2];

//            // compute the new inverse matrix
//            _sysmemBuffer[positionOffset + i * 3 + 0 + itemCountOffset * 2] = new Vector4(1, 0, 0, 0);
//            _sysmemBuffer[positionOffset + i * 3 + 1 + itemCountOffset * 2] = new Vector4(1, 0, 0, 0);
//            _sysmemBuffer[positionOffset + i * 3 + 2 + itemCountOffset * 2] = new Vector4(1, -px, -py, -pz);
//        }

//        // Colors
//        int colorOffset = positionOffset + itemCountOffset * 3;
//        for (int i = 0; i < _itemCount; i++)
//        {
//            //Color col = Color.HSVToRGB(((float)(i / itemsPerRow) / (float)(_itemCount / itemsPerRow)) % 1.0f, 1.0f, 1.0f);
//            Color col = Color.HSVToRGB(((float)(i / _instancesPerItem) / (float)(_itemCount / _instancesPerItem)) % 1.0f, 1.0f, 1.0f);

//            // write colors right after the 4x3 matrices
//            _sysmemBuffer[colorOffset + i] = new Vector4(col.r, col.g, col.b, 1.0f);
//        }
//        _gpuPersistentInstanceData.SetData(_sysmemBuffer);

//        // GPU side visible instances
//        _sysmemVisibleInstances = new NativeArray<int>(_visibleItemCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
//        if (UseConstantBuffer)
//        {
//            _gpuVisibleInstances = new GraphicsBuffer(GraphicsBuffer.Target.Constant, sizeof(int) * _visibleItemCount / 4, sizeof(int) * 4);
//            _gpuVisibleInstancesWindow = (uint)(sizeof(int) * _visibleItemCount);
//        }
//        else
//        {
//            _gpuVisibleInstances = new GraphicsBuffer(GraphicsBuffer.Target.Raw, sizeof(int) * _visibleItemCount, sizeof(int));
//        }

//        for (int i = 0; i < _visibleItemCount; ++i)
//            _sysmemVisibleInstances[i] = i;

//        _gpuVisibleInstances.SetData(_sysmemVisibleInstances);

//        // Set up procedural mesh
//        var mesh = testProceduralMaterial.mesh.Value;
//        var indices = mesh.GetIndices(0);
//        var meshVertices = mesh.vertices;
//        var meshNormals = mesh.normals;
//        var meshTangents = mesh.tangents;


//        var vertexCount = meshVertices.Length;
//        var indexCount = indices.Length;
//        var target = UseConstantBuffer ? GraphicsBuffer.Target.Constant : GraphicsBuffer.Target.Structured;

//        _gpuPositions = new GraphicsBuffer(target, indexCount, 4 * 4);
//        _gpuNormals = new GraphicsBuffer(target, indexCount, 4 * 4);
//        _gpuTangents = new GraphicsBuffer(target, indexCount, 4 * 4);

//        var positions = new Vector4[indexCount];
//        var normals = new Vector4[indexCount];
//        var tangents = new Vector4[indexCount];

//        for (int i = 0; i < indices.Length; ++i)
//        {
//            var idx = indices[i];
//            positions[i] = meshVertices[idx];
//            normals[i] = meshNormals[idx];
//            tangents[i] = meshTangents[idx];
//        }

//        _gpuPositions.SetData(positions);
//        _gpuNormals.SetData(normals);
//        _gpuTangents.SetData(tangents);

//        if (UseConstantBuffer)
//        {
//            Shader.SetGlobalConstantBuffer(positionsID, _gpuPositions, 0, positions.Length * 4 * 4);
//            Shader.SetGlobalConstantBuffer(normalsID, _gpuNormals, 0, positions.Length * 4 * 4);
//            Shader.SetGlobalConstantBuffer(tangentsID, _gpuTangents, 0, positions.Length * 4 * 4);
//        }
//        else
//        {
//            Shader.SetGlobalBuffer(positionsID, _gpuPositions);
//            Shader.SetGlobalBuffer(normalsID, _gpuNormals);
//            Shader.SetGlobalBuffer(tangentsID, _gpuTangents);
//        }
//        proceduralMaterial.Value.SetInt(baseIndexID, 0);

//        _elementsPerDraw = (uint)indices.Length;

//        // Indirect buffer
//        _gpuIndirectBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 5, GraphicsBuffer.IndirectDrawArgs.size);
//        var indirectData = new GraphicsBuffer.IndirectDrawArgs[5];
//        for (uint i = 0; i < 5; ++i)
//        {
//            indirectData[i] = new GraphicsBuffer.IndirectDrawArgs
//            {
//                vertexCountPerInstance = _elementsPerDraw,
//                instanceCount = _instancesPerItem,
//                startVertex = 0,
//                startInstance = 0,
//            };
//        }
//        _gpuIndirectBuffer.SetData(indirectData);

//        var batchMetadata = new NativeArray<MetadataValue>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
//        batchMetadata[0] = CreateMetadataValue(objectToWorldID, 64, true);       // matrices
//        batchMetadata[1] = CreateMetadataValue(matrixPreviousID, 64 + _itemCount * UnsafeUtility.SizeOf<Vector4>() * 3, true); // previous matrices
//        batchMetadata[2] = CreateMetadataValue(worldToObjectID, 64 + _itemCount * UnsafeUtility.SizeOf<Vector4>() * 3 * 2, true); // inverse matrices
//        batchMetadata[3] = CreateMetadataValue(colorID, 64 + _itemCount * UnsafeUtility.SizeOf<Vector4>() * 3 * 3, true); // colors

//        // Register batch
//        _batchID = _batchRendererGroup.AddBatch(batchMetadata, _gpuPersistentInstanceData.bufferHandle, 0, brgWindowSize);

//        _initialized = true;
//    }
//    // Helper function to allocate BRG buffers during the BRG callback function
//    public static T* Malloc<T>(int count) where T : unmanaged
//    {
//        return (T*)UnsafeUtility.Malloc(
//            UnsafeUtility.SizeOf<T>() * count,
//            UnsafeUtility.AlignOf<T>(),
//            Allocator.TempJob);
//    }
//    static MetadataValue CreateMetadataValue(int nameID, int gpuAddress, bool isOverridden)
//    {
//        const uint kIsOverriddenBit = 0x80000000;
//        return new MetadataValue
//        {
//            NameID = nameID,
//            Value = (uint)gpuAddress | (isOverridden ? kIsOverriddenBit : 0),
//        };
//    }
//}
