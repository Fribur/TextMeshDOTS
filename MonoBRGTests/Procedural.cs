using System;
using TextMeshDOTS.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public unsafe class Procedural : MonoBehaviour
{
    public Mesh mesh;
    public Material proceduralMaterial;

    BatchRendererGroup _batchRendererGroup;
    GraphicsBuffer _gpuPersistentInstanceData;
    GraphicsBuffer _gpuVisibleInstances;
    uint _gpuVisibleInstancesWindow;

    GraphicsBuffer _gpuPositions;
    GraphicsBuffer _gpuNormals;
    GraphicsBuffer _gpuTangents;
    uint _elementsPerDraw;

    BatchID _batchID;
    BatchMaterialID _proceduralMaterialID;
    int _itemCount;
    int _visibleItemCount;
    bool _initialized;

    NativeArray<float4x4> zero;
    NativeArray<float3x4> objectToWorld;
    NativeArray<float3x4> worldToObject;
    NativeArray<float4> color;
    NativeArray<int> _sysmemVisibleInstances;

    // Some helper constants to make calculations later a bit more convenient.
    const int matrixSize = sizeof(float) * 4 * 4;
    const int packedMatrixSize = sizeof(float) * 4 * 3;
    const int float4Size = sizeof(float) * 4;
    const int Offset = 32;
    const int extraBytes = matrixSize + Offset;
    //private const int kExtraBytes = kSizeOfMatrix * 2;
    const int _instancesPerItem = 2;

    // Offset should be divisible by 64, 48 and 16
    // These can be edited to test nonzero GLES buffer offsets.
    int BufferSize(int bufferCount) => bufferCount * sizeof(int);
    int BufferOffset => 0;
    int kBRGBufferMaxWindowSize => UseConstantBuffer ? BatchRendererGroup.GetConstantBufferMaxWindowSize() : 128 * 1024 * 1024;
    int kBRGBufferAlignment => UseConstantBuffer ? BatchRendererGroup.GetConstantBufferOffsetAlignment() : 16;
    bool UseConstantBuffer => BatchRendererGroup.BufferTarget == BatchBufferTarget.ConstantBuffer;


    public static T* Malloc<T>(int count) where T : unmanaged
    {
        return (T*)UnsafeUtility.Malloc(
            UnsafeUtility.SizeOf<T>() * count,
            UnsafeUtility.AlignOf<T>(),
            Allocator.TempJob);
    }

    public JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
    {
        if (!_initialized)
        {
            return new JobHandle();
        }

        BatchCullingOutputDrawCommands drawCommands = new BatchCullingOutputDrawCommands();

        var filterSettings = new BatchFilterSettings
        {
            renderingLayerMask = 1,
            layer = 0,
            motionMode = MotionVectorGenerationMode.ForceNoMotion,
            shadowCastingMode = ShadowCastingMode.On,
            receiveShadows = true,
            staticShadowCaster = false,
            allDepthSorted = false
        };
        drawCommands.drawRangeCount = 1;
        drawCommands.drawRanges = Malloc<BatchDrawRange>(1);

        // Procedural draw range
        drawCommands.drawRanges[0] = new BatchDrawRange
        {
            drawCommandsType = BatchDrawCommandType.Procedural,
            drawCommandsBegin = 0,
            drawCommandsCount = 5,
            filterSettings = filterSettings,
        };


        drawCommands.visibleInstances = Malloc<int>(_itemCount);
        for (int i = 0; i < _itemCount; ++i)
            drawCommands.visibleInstances[i] = _sysmemVisibleInstances[i];


        drawCommands.visibleInstanceCount = _itemCount;

        //some variables to track drawcommands and resulting offsets into buffer        
        uint drawCallCount = 5;
        uint visibleBaseOffset = 0;

        // Procedural draw command
        drawCommands.proceduralDrawCommandCount = 5;
        drawCommands.proceduralDrawCommands = Malloc<BatchDrawCommandProcedural>(5);
        for (uint i = 0; i < drawCallCount; ++i)
        {
            drawCommands.proceduralDrawCommands[i] = new BatchDrawCommandProcedural
            {
                flags = BatchDrawCommandFlags.None,
                batchID = _batchID,
                materialID = _proceduralMaterialID,
                splitVisibilityMask = 0xff,
                sortingPosition = 0,
                visibleOffset = visibleBaseOffset + i * _instancesPerItem,
                visibleCount = _instancesPerItem,
                topology = MeshTopology.Triangles,
                indexBufferHandle = default,
                baseVertex = 0,
                indexOffsetBytes = 0,
                elementCount = _elementsPerDraw,
            };
        }
        visibleBaseOffset += drawCallCount * _instancesPerItem;

        drawCommands.instanceSortingPositions = null;
        drawCommands.instanceSortingPositionFloatCount = 0;

        cullingOutput.drawCommands[0] = drawCommands;
        return new JobHandle();
    }


    // Start is called before the first frame update
    void Start()
    {
        _batchRendererGroup = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);

        var itemsPerRow = 10;
        _itemCount = 1 * itemsPerRow;

        // Bounds
        Bounds bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
        _batchRendererGroup.SetGlobalBounds(bounds);

        // Register mesh and material
        if (proceduralMaterial) _proceduralMaterialID = _batchRendererGroup.RegisterMaterial(proceduralMaterial);

        // Batch metadata buffer
        int objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
        int worldToObjectID = Shader.PropertyToID("unity_WorldToObject");
        int colorID = Shader.PropertyToID("_BaseColor");
        int positionsID = Shader.PropertyToID("_Positions");
        int normalsID = Shader.PropertyToID("_Normals");
        int tangentsID = Shader.PropertyToID("_Tangents");
        int baseIndexID = Shader.PropertyToID("_BaseIndex");    
        

        uint brgWindowSize = 0;
        zero = new NativeArray<float4x4>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);        
        objectToWorld = new NativeArray<float3x4>(_itemCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        worldToObject = new NativeArray<float3x4>(_itemCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        color = new NativeArray<float4>(_itemCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _sysmemVisibleInstances = new NativeArray<int>(_itemCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        int intCountGpuPersistent = BRGStaticHelper.BufferCountForInstances((packedMatrixSize * 2) + float4Size, _itemCount, extraBytes);
        int intCountGpuVisible = BRGStaticHelper.BufferCountForInstances(sizeof(int), _itemCount, 0);
        if (UseConstantBuffer)
        {
            _gpuPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Constant, intCountGpuPersistent / 4, 16);
            brgWindowSize = (uint)intCountGpuPersistent * 4;
            _gpuVisibleInstances = new GraphicsBuffer(GraphicsBuffer.Target.Constant, sizeof(int) * _itemCount / 4, sizeof(int) * 4);
            _gpuVisibleInstancesWindow = (uint)(sizeof(int) * _itemCount);
        }
        else
        {
            _gpuPersistentInstanceData = new GraphicsBuffer(GraphicsBuffer.Target.Raw, intCountGpuPersistent, sizeof(int));
            _gpuVisibleInstances = new GraphicsBuffer(GraphicsBuffer.Target.Raw, intCountGpuVisible, sizeof(int));
        }

        // 64 bytes of zeroes, so loads from address 0 return zeroes. This is a BatchRendererGroup convention.
        zero[0] = float4x4.zero;

        // Matrices
        for (int i = 0; i < _itemCount; ++i)
        {
            float px = (i % itemsPerRow) * 2.0f;
            float py = -(i / itemsPerRow) * 2.0f;
            float pz = 0.0f;

            var matrix = Matrix4x4.Translate(new Vector3(px, py, pz));
            objectToWorld[i] = BRGStaticHelper.GetPackedMatrix(matrix);
            worldToObject[i] = BRGStaticHelper.GetPackedMatrix(matrix.inverse);
            //objectToWorld[i] = BRGStaticHelper.GetPackedMatrix(px, py, pz); // compute the new current frame matrix
            //worldToObject[i] = BRGStaticHelper.GetPackedInverseMatrix(px, py, pz); // compute the new inverse matrix

            Color col = Color.HSVToRGB(((float)(i / _instancesPerItem) / (float)(_itemCount / _instancesPerItem)) % 1.0f, 1.0f, 1.0f);
            color[i] = new float4(col.r, col.g, col.b, 1.0f);
            _sysmemVisibleInstances[i] = i;
        }

        // In this simple example, the instance data is placed into the buffer like this:
        // Offset | Description
        //      0 | 64 bytes of zeroes, so loads from address 0 return zeroes
        //     64 | 32 uninitialized bytes to make working with SetData easier, otherwise unnecessary
        //     96 | unity_ObjectToWorld, 10 packed float3x4 matrices
        //    576 | unity_WorldToObject, 10 packed float3x4 matrices
        //   1056 | colors, 10 packed float4 color

        // Compute start addresses for the different instanced properties. unity_ObjectToWorld starts
        // at address 96 instead of 64, because the computeBufferStartIndex parameter of SetData
        // is expressed as source array elements, so it is easier to work in multiples of sizeof(PackedMatrix).
        uint byteAddressObjectToWorld = packedMatrixSize * 2;
        uint byteAddressWorldToObject = byteAddressObjectToWorld + (uint)(packedMatrixSize * _itemCount);
        uint byteAddressColor = byteAddressWorldToObject + (uint)(packedMatrixSize * _itemCount);

        // Upload our instance data to the GraphicsBuffer, from where the shader can load them.
        _gpuPersistentInstanceData.SetData(zero, 0, 0, 1);
        _gpuPersistentInstanceData.SetData(objectToWorld, 0, (int)(byteAddressObjectToWorld / packedMatrixSize), objectToWorld.Length);
        _gpuPersistentInstanceData.SetData(worldToObject, 0, (int)(byteAddressWorldToObject / packedMatrixSize), worldToObject.Length);
        _gpuPersistentInstanceData.SetData(color, 0, (int)(byteAddressColor / float4Size), color.Length);

        var batchMetadata = new NativeArray<MetadataValue>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        batchMetadata[0] = new MetadataValue { NameID = objectToWorldID, Value = byteAddressObjectToWorld | 0x80000000 }; // matrices
        batchMetadata[1] = new MetadataValue { NameID = worldToObjectID, Value = byteAddressWorldToObject | 0x80000000 }; // inverse matrices
        batchMetadata[2] = new MetadataValue { NameID = colorID, Value = byteAddressColor | 0x80000000 };// colors

        _gpuVisibleInstances.SetData(_sysmemVisibleInstances);

        #region Set up procedural mesh
        var indices = mesh.GetIndices(0);

        var meshVertices = mesh.vertices;
        var meshNormals = mesh.normals;
        var meshTangents = mesh.tangents;

        var vertexCount = meshVertices.Length;
        var indexCount = indices.Length;
        var target = UseConstantBuffer ? GraphicsBuffer.Target.Constant : GraphicsBuffer.Target.Structured;

        _gpuPositions = new GraphicsBuffer(target, indexCount, 4 * 4);
        _gpuNormals = new GraphicsBuffer(target, indexCount, 4 * 4);
        _gpuTangents = new GraphicsBuffer(target, indexCount, 4 * 4);

        var positions = new Vector4[indexCount];
        var normals = new Vector4[indexCount];
        var tangents = new Vector4[indexCount];

        for (int i = 0; i < indices.Length; ++i)
        {
            var idx = indices[i];
            positions[i] = meshVertices[idx];
            normals[i] = meshNormals[idx];
            tangents[i] = meshTangents[idx];
        }

        _gpuPositions.SetData(positions);
        _gpuNormals.SetData(normals);
        _gpuTangents.SetData(tangents);

        if (UseConstantBuffer)
        {
            Shader.SetGlobalConstantBuffer(positionsID, _gpuPositions, 0, positions.Length * 4 * 4);
            Shader.SetGlobalConstantBuffer(normalsID, _gpuNormals, 0, positions.Length * 4 * 4);
            Shader.SetGlobalConstantBuffer(tangentsID, _gpuTangents, 0, positions.Length * 4 * 4);
        }
        else
        {
            Shader.SetGlobalBuffer(positionsID, _gpuPositions);
            Shader.SetGlobalBuffer(normalsID, _gpuNormals);
            Shader.SetGlobalBuffer(tangentsID, _gpuTangents);
        }
        proceduralMaterial.SetInt(baseIndexID, 0);
        _elementsPerDraw = (uint)indices.Length;
        #endregion



        // Register batch
        _batchID = _batchRendererGroup.AddBatch(batchMetadata, _gpuPersistentInstanceData.bufferHandle, 0, brgWindowSize);

        _initialized = true;
    }



    private void OnDestroy()
    {
        if (_initialized)
        {
            _batchRendererGroup.RemoveBatch(_batchID);
            if (proceduralMaterial) _batchRendererGroup.UnregisterMaterial(_proceduralMaterialID);

            _batchRendererGroup.Dispose();
            _gpuPersistentInstanceData.Dispose();
            _gpuVisibleInstances.Dispose();
            _gpuPositions.Dispose();
            _gpuNormals.Dispose();
            _gpuTangents.Dispose();
            zero.Dispose();
            objectToWorld.Dispose();
            worldToObject.Dispose();
            color.Dispose();
            _sysmemVisibleInstances.Dispose();
        }
    }
}
