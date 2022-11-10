using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

[UpdateAfter(typeof(RandomSystem))]
public partial class ChunkGenerationSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _bufferSystem;

    private ComputeShader _noiseShader;
    private static Dictionary<Entity, NoiseBuffer> _noiseBuffers =  new Dictionary<Entity, NoiseBuffer>();

    protected override void OnCreate()
    {
        _bufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        _noiseShader = Resources.Load<ComputeShader>("Shaders/Compute/HeightmapNoise");

        //initialize shader for generating noise
        Initialize();
    }

    protected override void OnUpdate()
    {
        var buffer = _bufferSystem.CreateCommandBuffer().AsParallelWriter();

        //initialize chunk data
        Entities
            .WithAll<ChunkGenerateTag>()
            .WithNone<VoxelElement>()
            .ForEach((int entityInQueryIndex, Entity e) =>
            {
                buffer.AddBuffer<VoxelElement>(entityInQueryIndex, e);
            }).ScheduleParallel();

        Entities
            .WithoutBurst()
            .WithAll<ChunkGenerateTag>()
            .WithAll<VoxelElement>()
            .ForEach((int entityInQueryIndex, Entity e, ref DynamicBuffer<VoxelElement> voxelsBuffer, in ColumnIndexComponent columnIndex, in ChunkPositionComponent chunkPosition) =>
            {
                voxelsBuffer.ResizeUninitialized(Constants.CHUNK_VOLUME);
                
                var voxels = voxelsBuffer.Reinterpret<uint>().AsNativeArray();

                NoiseBuffer noiseBuffer;
                if(_noiseBuffers.TryGetValue(e, out noiseBuffer))
                {
                    //already started compute shader for this entity
                    //if the data is done, then get the data back and move on
                    if(noiseBuffer.Initialized && noiseBuffer.IsDone)
                    {
                        //get data
                        NativeArray<uint> temp = new NativeArray<uint>(noiseBuffer.voxelsArray, Allocator.Temp);
                        voxelsBuffer.CopyFrom(temp.Reinterpret<VoxelElement>());
                        temp.Dispose();

                        //dispose and remove from dictionary
                        noiseBuffer.Dispose();
                        _noiseBuffers.Remove(e);

                        //move on to mesh generation
                        buffer.RemoveComponent<ChunkGenerateTag>(entityInQueryIndex, e);
                        buffer.AddComponent<ChunkGenerateMeshTag>(entityInQueryIndex, e);
                    }
                } else
                {
                    //making a new one
                    noiseBuffer = new NoiseBuffer();
                    noiseBuffer.voxelsArray = voxels.ToArray();
                    _noiseBuffers[e] = noiseBuffer;
                    noiseBuffer.InitializeBuffer();

                    _noiseShader.SetBuffer(0, "voxelArray", noiseBuffer.noiseBuffer);
                    _noiseShader.SetBuffer(0, "count", noiseBuffer.countBuffer);

                    _noiseShader.SetVector("chunkPosition", new Vector4(columnIndex.Value.x * Constants.CHUNK_WIDTH, chunkPosition.Y * Constants.CHUNK_HEIGHT, columnIndex.Value.y * Constants.CHUNK_WIDTH));
                    _noiseShader.SetVector("seedOffset", Vector4.zero);

                    _noiseShader.Dispatch(0, Constants.X_THREADS, Constants.Y_THREADS, Constants.X_THREADS);

                    AsyncGPUReadback.Request(noiseBuffer.noiseBuffer, (callback) =>
                    {
                        //after GPU is done with the data, we want to do stuff

                        //copy back to nativearray
                        //voxels.CopyFrom(callback.GetData<uint>(0));
                        //voxelsBuffer.CopyFrom(callback.GetData<uint>(0).Reinterpret<VoxelElement>());
                        callback.GetData<uint>(0).CopyTo(_noiseBuffers[e].voxelsArray);
                        _noiseBuffers[e].IsDone = true;
                    });
                }
            }).Run();

        _bufferSystem.AddJobHandleForProducer(Dependency);
    }

    private void Initialize(int count = 256)
    {
        _noiseShader.SetInt("chunkWidth", Constants.CHUNK_WIDTH);
        _noiseShader.SetInt("chunkHeight", Constants.CHUNK_HEIGHT);

        _noiseShader.SetBool("generateCaves", false);//for now
        _noiseShader.SetBool("forceFloor", false);//for now

        _noiseShader.SetInt("maxHeight", 32);
        _noiseShader.SetInt("oceanHeight", 1);

        _noiseShader.SetFloat("noiseScale", 0.004f);
        _noiseShader.SetFloat("caveScale", 0.01f);
        _noiseShader.SetFloat("caveThreshold", 0.8f);

        _noiseShader.SetInt("surfaceVoxelID", (1 << 8) | 0);
        _noiseShader.SetInt("subSurfaceVoxelID", (1 << 8) | 1);

        //for (int i = 0; i < count; i++)
        //{
        //    CreateNewNoiseBuffer();
        //}
    }

    //#region Pooling
    //public NoiseBuffer GetNoiseBuffer()
    //{
    //    if (_availableNoiseBuffers.Count > 0)
    //        return _availableNoiseBuffers.Dequeue();
    //    else
    //    {
    //        return CreateNewNoiseBuffer(false);
    //    }
    //}

    //public NoiseBuffer CreateNewNoiseBuffer(bool enqueue = true)
    //{
    //    NoiseBuffer buffer = new NoiseBuffer();
    //    buffer.InitializeBuffer();
    //    _noiseBuffers.Add(buffer);

    //    if (enqueue)
    //        _availableNoiseBuffers.Enqueue(buffer);

    //    return buffer;
    //}

    //public void ClearAndRequeueBuffer(NoiseBuffer buffer)
    //{
    //    ClearVoxelData(buffer);
    //    _availableNoiseBuffers.Enqueue(buffer);
    //}
    //#endregion
    #region Compute Helpers

    //public void GenerateVoxelData(NativeArray<VoxelElement> voxelElements)
    //{
    //    _noiseShader.SetBuffer(0, "voxelArray", cont.data.noiseBuffer);
    //    _noiseShader.SetBuffer(0, "count", cont.data.countBuffer);

    //    _noiseShader.SetVector("chunkPosition", cont.containerPosition);
    //    _noiseShader.SetVector("seedOffset", Vector3.zero);

    //    _noiseShader.Dispatch(0, xThreads, yThreads, xThreads);

    //    AsyncGPUReadback.Request(cont.data.noiseBuffer, (callback) =>
    //    {
    //        callback.GetData<Voxel>(0).CopyTo(WorldManager.Instance.container.data.voxelArray.array);
    //        WorldManager.Instance.container.RenderMesh();
    //    });
    //}

    private void ClearVoxelData(NoiseBuffer buffer)
    {
        buffer.countBuffer.SetData(new int[] { 0 });
        _noiseShader.SetBuffer(1, "voxelArray", buffer.noiseBuffer);
        _noiseShader.Dispatch(1, Constants.X_THREADS, Constants.Y_THREADS, Constants.X_THREADS);
    }
    #endregion
}

public class NoiseBuffer
{
    public ComputeBuffer noiseBuffer;
    public ComputeBuffer countBuffer;
    public bool Initialized;
    public bool IsDone;
    public uint[] voxelsArray;

    public void InitializeBuffer()
    {
        countBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Counter);
        countBuffer.SetCounterValue(0);
        countBuffer.SetData(new uint[] { 0 });

        voxelsArray = new uint[Constants.CHUNK_VOLUME];
        noiseBuffer = new ComputeBuffer(voxelsArray.Length, 4);
        noiseBuffer.SetData(voxelsArray);

        Initialized = true;
        IsDone = false;
    }

    public void Dispose()
    {
        countBuffer?.Dispose();
        noiseBuffer?.Dispose();

        Initialized = false;
        IsDone = false;
    }

    public uint this[int index]
    {
        get
        {
            return voxelsArray[index];
        }

        set
        {
            voxelsArray[index] = value;
        }
    }
}