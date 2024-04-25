//using System;
//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Transforms;
//using UnityEngine;
//using UnityEngine.Rendering;
//using Unity.Mathematics;
//using TextMeshDOTS.Rendering.Authoring;

//namespace TextMeshDOTS.Rendering
//{
//    //[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
//    [UpdateInGroup(typeof(PresentationSystemGroup))]
//    [BurstCompile]
//    public unsafe partial class TextMeshDOTSProceduralIndexedSystem : SystemBase
//    {
//        EntityQuery textToRenderQ;

//        BatchRendererGroup _batchRendererGroup;
//        BatchID _batchID;

//        BatchMaterialID _proceduralMaterialID;
//        UnityObjectRef<Material> proceduralMaterial;

//        //GraphicsBuffer to provide indices to the procedural shader
//        GraphicsBuffer _indexBuffer;
//        NativeArray<ushort> _sysmemIndexBuffer;
//        NativeArray<uint> _glyphCountPerInstance;


//        //persistent GraphicsBuffer providing instanced shader property data and NativeArrays to set the data
//        GraphicsBuffer _gpuPersistentInstanceData;
//        NativeArray<float4x4> zero;
//        NativeArray<float3x4> objectToWorld;
//        NativeArray<float3x4> worldToObject;
//        NativeArray<TextShaderIndex> _TextShaderIndex;
//        //corresponding instanced shader property IDs 
//        int objectToWorldID;
//        int worldToObjectID;
//        int textShaderIndexID;

//        //visible Instances GraphicsBuffer and NativeArrays to set the data
//        GraphicsBuffer _gpuVisibleInstances;
//        NativeArray<int> _sysmemVisibleInstances;
//        uint _gpuVisibleInstancesWindow;

//        //Shader properties valid for entire batch uploaded via SetGlobalBuffer
//        GraphicsBuffer _latiosTextBuffer;
//        //corresponding shader property IDs
//        int latiosTextBufferID;

//        bool _initialized;

//        // Some helper constants to make calculations later a bit more convenient.
//        uint _elementsPerDraw;
//        int _itemCount;
//        uint _instancesPerItem = 1;

//        const int renderGlyphSize = 96;
//        const int matrixSize = sizeof(float) * 4 * 4;
//        const int packedMatrixSize = sizeof(float) * 4 * 3;
//        const int textShaderIndexSize = sizeof(uint) * 2;
//        const int float4Size = sizeof(float) * 4;
//        const int Offset = 32; // Offset should be divisible by 64, 48 and 16
//        const int extraBytes = matrixSize + Offset;

//        int BufferSize(int bufferCount) => bufferCount * sizeof(int);
//        private bool UseConstantBuffer => BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer;
//        int kBRGBufferMaxWindowSize => UseConstantBuffer ? BatchRendererGroup.GetConstantBufferMaxWindowSize() : 128 * 1024 * 1024;
//        int kBRGBufferAlignment => UseConstantBuffer ? BatchRendererGroup.GetConstantBufferOffsetAlignment() : 16;

//        protected override void OnCreate()
//        {
//            textToRenderQ = SystemAPI.QueryBuilder()
//                        .WithAll<RenderGlyph>()
//                        .WithAll<LocalToWorld>()
//                        .WithAll<TextShaderIndex>()
//                        .WithAll<TextRenderControl>()
//                        .Build();
//            RequireForUpdate(textToRenderQ);

//            GetShaderPropertyIDs();
//        }
//        protected override void OnUpdate()
//        {
//            var fontBlobReferenceEntities = textToRenderQ.ToEntityArray(Allocator.Temp);
//            var fontBlobReferenceEntity = fontBlobReferenceEntities[0];
//            if (!_initialized)
//            {
//                InitializeBRG(fontBlobReferenceEntity);
//                Debug.Log("Initialzied text BRG: Procedural Index");
//            }
//        }
//        protected override void OnDestroy()
//        {
//            if (_initialized)
//            {
//                _batchRendererGroup.RemoveBatch(_batchID);
//                if (proceduralMaterial) _batchRendererGroup.UnregisterMaterial(_proceduralMaterialID);

//                _batchRendererGroup.Dispose();
//                _indexBuffer.Dispose();
//                _gpuPersistentInstanceData.Dispose();
//                _gpuVisibleInstances.Dispose();
//                _latiosTextBuffer.Dispose();
//                zero.Dispose();
//                objectToWorld.Dispose();
//                worldToObject.Dispose();
//                _TextShaderIndex.Dispose();
//                _sysmemVisibleInstances.Dispose();
//                _sysmemIndexBuffer.Dispose();
//                _glyphCountPerInstance.Dispose();
//            }
//        }

//        public JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
//        {
//            if (!_initialized)
//            {
//                return new JobHandle();
//            }
//            //initialize filter settings for entire batch
//            var filterSettings = new BatchFilterSettings
//            {
//                renderingLayerMask = 1,
//                layer = 0,
//                motionMode = MotionVectorGenerationMode.ForceNoMotion,
//                shadowCastingMode = ShadowCastingMode.On,
//                receiveShadows = false,
//                staticShadowCaster = false,
//                allDepthSorted = false
//            };

//            //initialize draw commands
//            var drawCommands = new BatchCullingOutputDrawCommands();
//            drawCommands.drawRangeCount = 1;
//            drawCommands.drawRanges = BRGStaticHelper.Malloc<BatchDrawRange>(1);

//            // ProceduralIndirect draw range
//            drawCommands.drawRanges[0] = new BatchDrawRange
//            {
//                drawCommandsType = BatchDrawCommandType.Procedural,
//                drawCommandsBegin = 0,
//                drawCommandsCount = 1,
//                filterSettings = filterSettings,
//            };

//            //define which instances are visible (To-Do: investigate if culling can be done on GPU without further data uploads)
//            drawCommands.visibleInstanceCount = _itemCount;
//            drawCommands.visibleInstances = BRGStaticHelper.Malloc<int>(_itemCount);
//            for (int i = 0; i < _itemCount; ++i)
//                drawCommands.visibleInstances[i] = _sysmemVisibleInstances[i];

//            //some variables to track drawcommands and resulting offsets into buffer
//            uint drawCallCount = 1;
//            uint visibleBaseOffset = 0;

//            // Procedural draw command
//            drawCommands.proceduralDrawCommandCount = 1;
//            drawCommands.proceduralDrawCommands = BRGStaticHelper.Malloc<BatchDrawCommandProcedural>(1);
//            for (uint i = 0; i < drawCallCount; ++i)
//            {
//                drawCommands.proceduralDrawCommands[i] = new BatchDrawCommandProcedural
//                {
//                    flags = BatchDrawCommandFlags.None,
//                    batchID = _batchID,
//                    materialID = _proceduralMaterialID,
//                    splitVisibilityMask = 0xff,
//                    sortingPosition = 0,
//                    visibleOffset = visibleBaseOffset + i * _instancesPerItem,
//                    visibleCount = _instancesPerItem,
//                    topology = MeshTopology.Triangles,
//                    indexBufferHandle = _indexBuffer.bufferHandle,
//                    baseVertex = 0,
//                    indexOffsetBytes = 0,
//                    elementCount = _glyphCountPerInstance[0],
//                };
//            }
//            visibleBaseOffset += drawCallCount * _instancesPerItem;

//            drawCommands.instanceSortingPositions = null;
//            drawCommands.instanceSortingPositionFloatCount = 0;

//            cullingOutput.drawCommands[0] = drawCommands;
//            return new JobHandle();
//        }


//        void InitializeBRG(Entity fontBlobReferenceEntity)
//        {
//            //fetch data for setting the instanced batch
//            var fontBlobReference = SystemAPI.GetComponent<FontBlobReference>(fontBlobReferenceEntity);
//            var textShaderIndex = SystemAPI.GetComponentRW<TextShaderIndex>(fontBlobReferenceEntity);
//            var testProceduralMaterial = SystemAPI.GetComponent<TestProceduralMaterial>(fontBlobReferenceEntity);
//            var localToWorld = SystemAPI.GetComponent<LocalToWorld>(fontBlobReferenceEntity);
//            var renderGlyphs = SystemAPI.GetBuffer<RenderGlyph>(fontBlobReferenceEntity).AsNativeArray();
//            textShaderIndex.ValueRW.firstGlyphIndex = 0;
//            textShaderIndex.ValueRW.glyphCount = (uint)renderGlyphs.Length;

//            _batchRendererGroup = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);

//            var itemsPerRow = 1;
//            _itemCount = 1 * itemsPerRow;

//            // Bounds
//            Bounds bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
//            _batchRendererGroup.SetGlobalBounds(bounds);

//            // Register procedural material
//            proceduralMaterial = testProceduralMaterial.material;
//            _proceduralMaterialID = _batchRendererGroup.RegisterMaterial(proceduralMaterial);


//            //initialize GraphicsBuffer and Nativearrays used to set the buffer data
//            uint brgWindowSize = 0;
//            zero = new NativeArray<float4x4>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);
//            objectToWorld = new NativeArray<float3x4>(_itemCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
//            worldToObject = new NativeArray<float3x4>(_itemCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
//            _TextShaderIndex = new NativeArray<TextShaderIndex>(_itemCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
//            _sysmemVisibleInstances = new NativeArray<int>(_itemCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);

//            int intCountGpuPersistent = BRGStaticHelper.BufferCountForInstances((packedMatrixSize * 2) + textShaderIndexSize, _itemCount, extraBytes);
//            int intCountGpuVisible = BRGStaticHelper.BufferCountForInstances(sizeof(int), _itemCount, 0);
//            if (UseConstantBuffer)
//            {
//                _gpuPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Constant, intCountGpuPersistent / 4, 16);
//                brgWindowSize = (uint)intCountGpuPersistent * 4;
//                _gpuVisibleInstances = new GraphicsBuffer(GraphicsBuffer.Target.Constant, sizeof(int) * _itemCount / 4, sizeof(int) * 4);
//                _gpuVisibleInstancesWindow = (uint)(sizeof(int) * _itemCount);
//            }
//            else
//            {
//                _gpuPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, intCountGpuPersistent, sizeof(int));
//                _gpuVisibleInstances = new GraphicsBuffer(GraphicsBuffer.Target.Raw, intCountGpuVisible, sizeof(int));
//            }

//            #region instanced properties persistent GraphicsBuffer 
//            //prepare data for upload persistent GraphicsBuffer (providing instanced shader property data)
//            // Offset | Description (the following info is stored in Metadata buffer)
//            //      0 | 64 bytes of zeroes, so loads from address 0 return zeroes
//            //     64 | 32 uninitialized bytes to make working with SetData easier, otherwise unnecessary
//            //     96 | start placing instanced shader properties here

//            // (1) 64 bytes of zeroes, so loads from address 0 return zeroes. This is a BatchRendererGroup convention.
//            zero[0] = float4x4.zero;

//            // (2) set objectToWorld, matrixPrevious, worldToObject, textShaderIndex, visibleInstances
//            for (int i = 0; i < _itemCount; ++i)
//            {
//                objectToWorld[i] = BRGStaticHelper.GetPackedMatrix(localToWorld.Value);                 // compute the new current frame matrix
//                worldToObject[i] = BRGStaticHelper.GetPackedMatrix(math.inverse(localToWorld.Value));   // compute the new inverse matrix
//                _TextShaderIndex[i] = textShaderIndex.ValueRO;
//                _sysmemVisibleInstances[i] = i;
//            }

//            //Compute start addresses for the different instanced properties. unity_ObjectToWorld starts at
//            //address 96 instead of 64, because the computeBufferStartIndex parameter of SetData is expressed
//            //in units of "source array elements" so it is easier to work in multiples of sizeof(PackedMatrix) or sizeof(float4).
//            uint byteAddressObjectToWorld = packedMatrixSize * 2;                                               //2*48 = 64 + 32 = 96
//            uint byteAddressWorldToObject = byteAddressObjectToWorld + (uint)(packedMatrixSize * _itemCount);   //576
//            uint byteAddressTextGlyphBase = byteAddressWorldToObject + (uint)(packedMatrixSize * _itemCount);   //1056

//            //Debug.Log($"offsets: {byteAddressObjectToWorld} {byteAddressWorldToObject} {byteAddressTextGlyphBase}");

//            //Upload instanced shader property data to the persistent GraphicsBuffer
//            _gpuPersistentInstanceData.SetData(zero, 0, 0, 1);
//            _gpuPersistentInstanceData.SetData(objectToWorld, 0, (int)(byteAddressObjectToWorld / packedMatrixSize), objectToWorld.Length);
//            _gpuPersistentInstanceData.SetData(worldToObject, 0, (int)(byteAddressWorldToObject / packedMatrixSize), worldToObject.Length);
//            _gpuPersistentInstanceData.SetData(_TextShaderIndex, 0, (int)(byteAddressTextGlyphBase / textShaderIndexSize), _TextShaderIndex.Length);

//            //setup batchMetatData that informes shader about (1) propertyID, (2) offset of position where property data starts and
//            //(3) if the property is instanced or not (0x80000000 means data is instanced, 0 means it not and shader should pull this data from global buffer)
//            var batchMetadata = new NativeArray<MetadataValue>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
//            batchMetadata[0] = new MetadataValue { NameID = objectToWorldID, Value = byteAddressObjectToWorld | 0x80000000 };       // matrices, 
//            batchMetadata[1] = new MetadataValue { NameID = worldToObjectID, Value = byteAddressWorldToObject | 0x80000000 };       // inverse matrices
//            batchMetadata[2] = new MetadataValue { NameID = textShaderIndexID, Value = byteAddressTextGlyphBase | 0x80000000 }; // textShaderIndexID
//            #endregion

//            //set visible instances GraphicsBuffer
//            _gpuVisibleInstances.SetData(_sysmemVisibleInstances);

//            #region global shader properties
//            //setup shader properties that are uploaded into global buffer providing data valid for all instances
//            var quadCount = renderGlyphs.Length;
//            var indexCount = (uint)quadCount * 6;
//            var target = UseConstantBuffer ? GraphicsBuffer.Target.Constant : GraphicsBuffer.Target.Raw;

//            _latiosTextBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, quadCount, 96);
//            _latiosTextBuffer.SetData(renderGlyphs);

//            //_sysmemIndexBuffer = new NativeArray<ushort>(indexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
//            //LatiosTextBackendBakingUtility.BuildIndexBuffer(ref _sysmemIndexBuffer);
//            //_indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, indexCount, 2);
//            //_indexBuffer.SetData(_sysmemIndexBuffer);

//            _sysmemIndexBuffer = new NativeArray<ushort>(65535, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
//            LatiosTextBackendBakingUtility.BuildIndexBuffer(ref _sysmemIndexBuffer);
//            _indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, 65535, 2);
//            _indexBuffer.SetData(_sysmemIndexBuffer);

//            _glyphCountPerInstance = new NativeArray<uint>(_itemCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
//            _glyphCountPerInstance[0] = indexCount;

//            if (UseConstantBuffer)
//            {
//                Shader.SetGlobalConstantBuffer(latiosTextBufferID, _latiosTextBuffer, 0, quadCount * 4 * 4);
//            }
//            else
//            {
//                Shader.SetGlobalBuffer(latiosTextBufferID, _latiosTextBuffer);
//            }
//            _elementsPerDraw = (uint)indexCount;
//            #endregion

//            // Register batch
//            _batchID = _batchRendererGroup.AddBatch(batchMetadata, _gpuPersistentInstanceData.bufferHandle, 0, brgWindowSize);

//            _initialized = true;
//        }

//        void GetShaderPropertyIDs()
//        {
//            objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");   //instanced property (information about via MetaData buffer)
//            worldToObjectID = Shader.PropertyToID("unity_WorldToObject");   //instanced property (information about via MetaData buffer)
//            textShaderIndexID = Shader.PropertyToID("_TextShaderIndex");//instanced property (information about via MetaData buffer)
//            latiosTextBufferID = Shader.PropertyToID("_latiosTextBuffer");                //global property
//        }
//    }
//}
