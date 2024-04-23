//using System;
//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Transforms;
//using UnityEngine;
//using UnityEngine.Rendering;
//using Unity.Mathematics;

//namespace TextMeshDOTS.Rendering
//{
//    //[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
//    [UpdateInGroup(typeof(PresentationSystemGroup))]
//    [BurstCompile]
//    public unsafe partial class ProceduralSystem : SystemBase
//    {
//        EntityQuery textToRenderQ;

//        BatchRendererGroup _batchRendererGroup;
//        BatchID _batchID;

//        BatchMaterialID _proceduralMaterialID;
//        UnityObjectRef<Material> proceduralMaterial;
//        int baseIndexID;

//        //persistent GraphicsBuffer providing instanced shader property data and NativeArrays to set the data
//        GraphicsBuffer _gpuPersistentInstanceData;
//        NativeArray<float4x4> zero;
//        NativeArray<float3x4> objectToWorld;
//        NativeArray<float3x4> matrixPrevious;
//        NativeArray<float3x4> worldToObject;
//        NativeArray<float4> color;
//        //corresponding instanced shader property IDs 
//        int objectToWorldID;
//        int matrixPreviousID;
//        int worldToObjectID;
//        int colorID;

//        //visible Instances GraphicsBuffer and NativeArrays to set the data
//        GraphicsBuffer _gpuVisibleInstances;
//        NativeArray<int> _sysmemVisibleInstances;
//        uint _gpuVisibleInstancesWindow;

//        //Shader properties valid for entire batch uploaded via SetGlobalBuffer
//        GraphicsBuffer _gpuPositions;
//        GraphicsBuffer _gpuNormals;
//        GraphicsBuffer _gpuTangents;
//        //corresponding shader property IDs
//        int positionsID;
//        int normalsID;
//        int tangentsID;

//        bool _initialized;

//        // Some helper constants to make calculations later a bit more convenient.
//        uint _elementsPerDraw;
//        int _itemCount;
//        uint _instancesPerItem = 2;

//        const int matrixSize = sizeof(float) * 4 * 4;
//        const int packedMatrixSize = sizeof(float) * 4 * 3;
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
//                .WithAll<RenderGlyph>()
//                .WithAll<LocalToWorld>()
//                .Build();
//            RequireForUpdate(textToRenderQ);

//            GetShaderPropertyIDs();
//        }
//        protected override void OnUpdate()
//        {
//            var fontBlobReferenceEntities = textToRenderQ.ToEntityArray(Allocator.Temp);
//            var fontBlobReferenceEntity = fontBlobReferenceEntities[0];
//            //var fontBlobReference = SystemAPI.GetComponent<FontBlobReference>(fontBlobReferenceEntity);
//            var testProceduralMaterial = SystemAPI.GetComponent<TestProceduralMaterial>(fontBlobReferenceEntity);

//            if (!_initialized)
//            {
//                InitializeBRG(ref testProceduralMaterial);
//                //Debug.Log("Initialzied BRG");
//            }
//        }
//        protected override void OnDestroy()
//        {
//            if (_initialized)
//            {
//                _batchRendererGroup.RemoveBatch(_batchID);
//                if (proceduralMaterial) _batchRendererGroup.UnregisterMaterial(_proceduralMaterialID);

//                _batchRendererGroup.Dispose();
//                _gpuPersistentInstanceData.Dispose();
//                _gpuVisibleInstances.Dispose();
//                _gpuPositions.Dispose();
//                _gpuNormals.Dispose();
//                _gpuTangents.Dispose();
//                zero.Dispose();
//                objectToWorld.Dispose();
//                matrixPrevious.Dispose();
//                worldToObject.Dispose();
//                color.Dispose();
//                _sysmemVisibleInstances.Dispose();
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
//                receiveShadows = true,
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
//                drawCommandsCount = 5,
//                filterSettings = filterSettings,
//            };

//            //define which instances are visible (To-Do: investigate if culling can be done on GPU without further data uploads)
//            drawCommands.visibleInstanceCount = _itemCount;
//            drawCommands.visibleInstances = BRGStaticHelper.Malloc<int>(_itemCount);
//            for (int i = 0; i < _itemCount; ++i)
//                drawCommands.visibleInstances[i] = _sysmemVisibleInstances[i];

//            //some variables to track drawcommands and resulting offsets into buffer        
//            uint drawCallCount = 5;
//            uint visibleBaseOffset = 0;

//            // Procedural draw command
//            drawCommands.proceduralDrawCommandCount = 5;
//            drawCommands.proceduralDrawCommands = BRGStaticHelper.Malloc<BatchDrawCommandProcedural>(5);
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
//                    indexBufferHandle = default,
//                    baseVertex = 0,
//                    indexOffsetBytes = 0,
//                    elementCount = _elementsPerDraw,
//                };
//            }
//            visibleBaseOffset += drawCallCount * _instancesPerItem;

//            drawCommands.instanceSortingPositions = null;
//            drawCommands.instanceSortingPositionFloatCount = 0;

//            cullingOutput.drawCommands[0] = drawCommands;
//            return new JobHandle();
//        }


//        void InitializeBRG(ref TestProceduralMaterial testProceduralMaterial)
//        {
//            _batchRendererGroup = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);

//            var itemsPerRow = 10;
//            _itemCount = 1 * itemsPerRow;

//            // Bounds
//            Bounds bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
//            _batchRendererGroup.SetGlobalBounds(bounds);

//            // Register procedural material
//            proceduralMaterial = testProceduralMaterial.material;
//            _proceduralMaterialID = _batchRendererGroup.RegisterMaterial(proceduralMaterial);


//            //initialize GrpahicsBuffer and Nativearrays used to set the buffer data
//            uint brgWindowSize = 0;
//            zero = new NativeArray<float4x4>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);
//            objectToWorld = new NativeArray<float3x4>(_itemCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
//            matrixPrevious = new NativeArray<float3x4>(_itemCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
//            worldToObject = new NativeArray<float3x4>(_itemCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
//            color = new NativeArray<float4>(_itemCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
//            _sysmemVisibleInstances = new NativeArray<int>(_itemCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);

//            int intCountGpuPersistent = BRGStaticHelper.BufferCountForInstances((packedMatrixSize * 3) + float4Size, _itemCount, extraBytes);
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
//            //     96 | unity_ObjectToWorld, 10 packed float3x4 matrices
//            //    576 | unity_MatrixPreviousM, 10 packed float3x4 matrices
//            //    1056| unity_WorldToObject, 10 packed float3x4 matrices
//            //    1536| colors, 10 packed float4 color

//            // (1) 64 bytes of zeroes, so loads from address 0 return zeroes. This is a BatchRendererGroup convention.
//            zero[0] = float4x4.zero;

//            // (2) set objectToWorld, matrixPrevious, worldToObject, color, visibleInstances
//            for (int i = 0; i < _itemCount; ++i)
//            {
//                float px = (i % itemsPerRow) * 2.0f;
//                float py = -(i / itemsPerRow) * 2.0f;
//                float pz = 0.0f;

//                objectToWorld[i] = BRGStaticHelper.GetPackedMatrix(px, py, pz); // compute the new current frame matrix
//                matrixPrevious[i] = objectToWorld[i]; // we set the same matrix for the previous matrix
//                worldToObject[i] = BRGStaticHelper.GetPackedInverseMatrix(px, py, pz); // compute the new inverse matrix

//                Color col = Color.HSVToRGB(((float)(i / _instancesPerItem) / (float)(_itemCount / _instancesPerItem)) % 1.0f, 1.0f, 1.0f);
//                color[i] = new float4(col.r, col.g, col.b, 1.0f);
//                _sysmemVisibleInstances[i] = i;
//            }

//            //Compute start addresses for the different instanced properties. unity_ObjectToWorld starts at
//            //address 96 instead of 64, because the computeBufferStartIndex parameter of SetData is expressed
//            //in units of "source array elements" so it is easier to work in multiples of sizeof(PackedMatrix) or sizeof(float4).
//            uint byteAddressObjectToWorld = packedMatrixSize * 2;                                               //2*48 = 64 + 32 = 96
//            uint byteAddressMatrixPrevious = byteAddressObjectToWorld + (uint)(packedMatrixSize * _itemCount);  //576
//            uint byteAddressWorldToObject = byteAddressMatrixPrevious + (uint)(packedMatrixSize * _itemCount);  //1056
//            uint byteAddressColor = byteAddressWorldToObject + (uint)(packedMatrixSize * _itemCount);           //1536

//            //Upload our instance instanced shader property data to the persistent GraphicsBuffer
//            _gpuPersistentInstanceData.SetData(zero, 0, 0, 1);
//            _gpuPersistentInstanceData.SetData(objectToWorld, 0, (int)(byteAddressObjectToWorld / packedMatrixSize), objectToWorld.Length);
//            _gpuPersistentInstanceData.SetData(matrixPrevious, 0, (int)(byteAddressMatrixPrevious / packedMatrixSize), matrixPrevious.Length);
//            _gpuPersistentInstanceData.SetData(worldToObject, 0, (int)(byteAddressWorldToObject / packedMatrixSize), worldToObject.Length);
//            _gpuPersistentInstanceData.SetData(color, 0, (int)(byteAddressColor / float4Size), color.Length);

//            //setup batchMetatData that informes shader about (1) propertyID, (2) offset of position where property data starts and
//            //(3) if the property is instanced or not (0x80000000 means data is instanced, 0 means it not and shader should pull this data from global buffer)
//            var batchMetadata = new NativeArray<MetadataValue>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
//            batchMetadata[0] = new MetadataValue { NameID = objectToWorldID, Value = byteAddressObjectToWorld | 0x80000000 }; // matrices, 
//            batchMetadata[1] = new MetadataValue { NameID = matrixPreviousID, Value = byteAddressMatrixPrevious | 0x80000000 }; // previous matrices
//            batchMetadata[2] = new MetadataValue { NameID = worldToObjectID, Value = byteAddressWorldToObject | 0x80000000 }; // inverse matrices
//            batchMetadata[3] = new MetadataValue { NameID = colorID, Value = byteAddressColor | 0x80000000 };// colors
//            #endregion

//            //set visible instances GraphicsBuffer
//            _gpuVisibleInstances.SetData(_sysmemVisibleInstances);

//            #region global shader properties
//            //setup shader properties that are uploaded into global buffer providing data valid for all instances
//            var mesh = testProceduralMaterial.mesh.Value;
//            var indices = mesh.GetIndices(0);

//            var meshVertices = mesh.vertices;
//            var meshNormals = mesh.normals;
//            var meshTangents = mesh.tangents;

//            var vertexCount = meshVertices.Length;
//            var indexCount = indices.Length;
//            var target = UseConstantBuffer ? GraphicsBuffer.Target.Constant : GraphicsBuffer.Target.Structured;

//            _gpuPositions = new GraphicsBuffer(target, indexCount, 4 * 4);
//            _gpuNormals = new GraphicsBuffer(target, indexCount, 4 * 4);
//            _gpuTangents = new GraphicsBuffer(target, indexCount, 4 * 4);

//            var positions = new Vector4[indexCount];
//            var normals = new Vector4[indexCount];
//            var tangents = new Vector4[indexCount];

//            for (int i = 0; i < indices.Length; ++i)
//            {
//                var idx = indices[i];
//                positions[i] = meshVertices[idx];
//                normals[i] = meshNormals[idx];
//                tangents[i] = meshTangents[idx];
//            }

//            _gpuPositions.SetData(positions);
//            _gpuNormals.SetData(normals);
//            _gpuTangents.SetData(tangents);

//            if (UseConstantBuffer)
//            {
//                Shader.SetGlobalConstantBuffer(positionsID, _gpuPositions, 0, positions.Length * 4 * 4);
//                Shader.SetGlobalConstantBuffer(normalsID, _gpuNormals, 0, positions.Length * 4 * 4);
//                Shader.SetGlobalConstantBuffer(tangentsID, _gpuTangents, 0, positions.Length * 4 * 4);
//            }
//            else
//            {
//                Shader.SetGlobalBuffer(positionsID, _gpuPositions);
//                Shader.SetGlobalBuffer(normalsID, _gpuNormals);
//                Shader.SetGlobalBuffer(tangentsID, _gpuTangents);
//            }
//            proceduralMaterial.Value.SetInt(baseIndexID, 0);
//            _elementsPerDraw = (uint)indices.Length;
//            #endregion

//            // Register batch
//            _batchID = _batchRendererGroup.AddBatch(batchMetadata, _gpuPersistentInstanceData.bufferHandle, 0, brgWindowSize);

//            _initialized = true;
//        }

//        void GetShaderPropertyIDs()
//        {
//            objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");   //instanced property (information about via MetaData buffer)
//            matrixPreviousID = Shader.PropertyToID("unity_MatrixPreviousM");//instanced property (information about via MetaData buffer)
//            worldToObjectID = Shader.PropertyToID("unity_WorldToObject");   //instanced property (information about via MetaData buffer)
//            colorID = Shader.PropertyToID("_BaseColor");                    //instanced property (information about via MetaData buffer)
//            positionsID = Shader.PropertyToID("_Positions");                //global property
//            normalsID = Shader.PropertyToID("_Normals");                    //global property
//            tangentsID = Shader.PropertyToID("_Tangents");                  //global property
//            baseIndexID = Shader.PropertyToID("_BaseIndex");                //material property
//        }
//    }
//}
