using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;

public partial class ChunkMeshGenerationSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _bufferSystem;

    private Dictionary<Entity, GameObject> gameobjects = new Dictionary<Entity, GameObject>();

    private Material material;

    private ComputeShader _voxelShader;
    private static Dictionary<Entity, MeshBuffer> _meshBuffers = new Dictionary<Entity, MeshBuffer>();

    private ComputeBuffer _voxelColors;

    protected override void OnCreate()
    {
        _bufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        _voxelShader = Resources.Load<ComputeShader>("Shaders/Compute/VoxelCompute");

        Initialize();
    }

    protected override void OnDestroy()
    {
        _voxelColors.Dispose();
    }

    protected override void OnUpdate()
    {
        AddBuffers();
        GenerateMesh();
        DestroyEmptyGameObjects();
    }

    /// <summary>
    /// Add buffers to the chunk so we can store mesh data.
    /// </summary>
    private void AddBuffers()
    {
        var buffer = _bufferSystem.CreateCommandBuffer().AsParallelWriter();

        Entities
            .WithAll<ChunkGenerateMeshTag>()
            .WithNone<ChunkMeshVerticesElement>()
            .ForEach((int entityInQueryIndex, Entity e) =>
            {
                buffer.AddBuffer<ChunkMeshVerticesElement>(entityInQueryIndex, e);
                buffer.AddBuffer<ChunkMeshIndicesElement>(entityInQueryIndex, e);
                buffer.AddBuffer<ChunkMeshColorsElement>(entityInQueryIndex, e);
            }).ScheduleParallel();

        _bufferSystem.AddJobHandleForProducer(Dependency);
    }

    /// <summary>
    /// Generate mesh and store the data into the buffers.
    /// </summary>
    private void GenerateMesh()
    {
        var buffer = _bufferSystem.CreateCommandBuffer().AsParallelWriter();

        Entities
            .WithoutBurst()
            .WithAll<ChunkGenerateMeshTag>()
            .ForEach((int entityInQueryIndex, Entity e, ref DynamicBuffer<ChunkMeshVerticesElement> vertsBuffer, ref DynamicBuffer<ChunkMeshIndicesElement> indicesBuffer, ref DynamicBuffer<ChunkMeshColorsElement> colorsBuffer, in DynamicBuffer<VoxelElement> voxelsBuffer, in ColumnIndexComponent columnIndex, in ChunkPositionComponent chunkPosition) =>
            {
                var voxels = voxelsBuffer.Reinterpret<uint>().AsNativeArray();

                MeshBuffer meshBuffer;
                if (_meshBuffers.TryGetValue(e, out meshBuffer))
                {
                    //there is a mesh buffer
                    if (meshBuffer.Initialized && meshBuffer.IsDone)
                    {
                        //if done, store back in dynamic buffers and move on to render mesh
                        NativeArray<float3> verts = new NativeArray<float3>(meshBuffer.vertices, Allocator.Temp);
                        NativeArray<float4> colors = new NativeArray<float4>(meshBuffer.colors, Allocator.Temp);
                        NativeArray<int> indices = new NativeArray<int>(meshBuffer.indices, Allocator.Temp);

                        vertsBuffer.Clear();
                        indicesBuffer.Clear();
                        colorsBuffer.Clear();

                        vertsBuffer.CopyFrom(verts.Reinterpret<ChunkMeshVerticesElement>());
                        colorsBuffer.CopyFrom(colors.Reinterpret<ChunkMeshColorsElement>());
                        indicesBuffer.CopyFrom(indices.Reinterpret<ChunkMeshIndicesElement>());

                        verts.Dispose();
                        colors.Dispose();
                        indices.Dispose();

                        //dispose and remove from dictionary
                        meshBuffer.Dispose();
                        _meshBuffers.Remove(e);

                        //move on
                        buffer.RemoveComponent<ChunkGenerateMeshTag>(entityInQueryIndex, e);
                        buffer.AddComponent<ChunkRenderTag>(entityInQueryIndex, e);
                    }
                }
                else
                {
                    //not processed yet, so process it
                    meshBuffer = new MeshBuffer();
                    meshBuffer.voxelArray = new ComputeBuffer(voxels.Length, sizeof(uint));
                    meshBuffer.voxelArray.SetData(voxels);
                    meshBuffer.InitializeBuffer();
                    _voxelShader.SetVector("chunkPosition", new Vector4(0, 0, 0, 0));// new Vector4(columnIndex.Value.x * Constants.CHUNK_WIDTH, chunkPosition.Y * Constants.CHUNK_HEIGHT, columnIndex.Value.y * Constants.CHUNK_WIDTH)) ;

                    _meshBuffers[e] = meshBuffer;

                    _voxelShader.SetBuffer(0, "voxelArray", meshBuffer.voxelArray);
                    _voxelShader.SetBuffer(0, "counter", meshBuffer.countBuffer);
                    _voxelShader.SetBuffer(0, "vertexBuffer", meshBuffer.vertexBuffer);
                    _voxelShader.SetBuffer(0, "colorBuffer", meshBuffer.colorBuffer);
                    _voxelShader.SetBuffer(0, "indexBuffer", meshBuffer.indexBuffer);

                    _voxelShader.Dispatch(0, Constants.X_THREADS, Constants.Y_THREADS, Constants.X_THREADS);

                    AsyncGPUReadback.Request(meshBuffer.countBuffer, (callback) =>
                    {
                        _meshBuffers[e].counts = new uint[2] { 0, 0 };
                        _meshBuffers[e].countBuffer.GetData(_meshBuffers[e].counts);
                        int count = (int)_meshBuffers[e].counts[0];

                        _meshBuffers[e].ResizeArrays(count);

                        _meshBuffers[e].vertexBuffer.GetData(_meshBuffers[e].vertices, 0, 0, count);
                        _meshBuffers[e].indexBuffer.GetData(_meshBuffers[e].indices, 0, 0, count);
                        _meshBuffers[e].colorBuffer.GetData(_meshBuffers[e].colors, 0, 0, count);

                        _meshBuffers[e].IsDone = true;
                    });
                }
            }).Run();

        RenderMesh();
    }

    /// <summary>
    /// Take the data from the buffers and put it into the mesh renderer.
    /// Should be called at the end of GenerateMesh().
    /// </summary>
    private void RenderMesh()
    {
        var buffer = _bufferSystem.CreateCommandBuffer();

        Entities
            .WithStructuralChanges()
            .WithAll<ChunkRenderTag>()
            .ForEach((Entity e, in DynamicBuffer<ChunkMeshVerticesElement> vertsBuffer, in DynamicBuffer<ChunkMeshIndicesElement> indicesBuffer, in DynamicBuffer<ChunkMeshColorsElement> colorsBuffer, in ColumnIndexComponent columnIndex, in ChunkPositionComponent chunkPosition) =>
            {
                //only initialize a game object if it is needed
                if(vertsBuffer.Length > 0)
                {
                    //create mesh and set generated values
                    var mesh = new Mesh();

                    //so we can go past 65000 verts or whatever
                    mesh.indexFormat = IndexFormat.UInt32;

                    mesh.SetVertices(vertsBuffer.Reinterpret<float3>().AsNativeArray());
                    mesh.SetIndices(indicesBuffer.Reinterpret<int>().AsNativeArray(), MeshTopology.Triangles, 0);
                    mesh.SetColors(colorsBuffer.Reinterpret<float4>().AsNativeArray().ToArray().Select((float4 f) => { return new Color(f.x, f.y, f.z, f.w); }).ToArray());

                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();

                    var go = new GameObject("Chunk");
                    var filter = go.AddComponent<MeshFilter>();
                    var renderer = go.AddComponent<MeshRenderer>();
                    filter.sharedMesh = mesh;
                    if (material == null)
                    {
                        material = Resources.Load<Material>("Materials/VoxelsMaterial");
                    }
                    renderer.sharedMaterial = material;

                    float3 p = new float3(columnIndex.Value.x, chunkPosition.Y, columnIndex.Value.y);
                    p *= Constants.ChunkSize;
                    go.transform.position = p;

                    //this game object will not change
                    go.isStatic = true;

                    buffer.AddComponent<ChunkIdleTag>(e);

                    buffer.AddComponent<ChunkWithGameObjectTag>(e);
                    gameobjects.Add(e, go);
                }

                //done rendering
                buffer.RemoveComponent<ChunkRenderTag>(e);
            }).Run();

        _bufferSystem.AddJobHandleForProducer(Dependency);
    }

    private void DestroyEmptyGameObjects()
    {
        var buffer = _bufferSystem.CreateCommandBuffer();

        Entities
            .WithoutBurst()
            .WithAll<ChunkWithGameObjectTag>()
            .WithNone<ChunkMeshVerticesElement>()
            .ForEach((Entity e) =>
            {
                GameObject.Destroy(gameobjects[e]);
                gameobjects.Remove(e);
                buffer.RemoveComponent<ChunkWithGameObjectTag>(e);
            }).Run();

        _bufferSystem.AddJobHandleForProducer(Dependency);
    }

    private void Initialize(int count = 256)
    {
        VoxelColor32[] colors = new VoxelColor32[]
        {
            new VoxelColor32
            {
                color = Helper.Color32ToFloat(new Color32(255, 0, 0, 255)),
                metallic = 0.5f,
                smoothness = 0.5f,
            },
            new VoxelColor32
            {
                color = Helper.Color32ToFloat(new Color32(127, 127, 0, 255)),
                metallic = 0.5f,
                smoothness = 0.5f,
            },
            new VoxelColor32
            {
                color = Helper.Color32ToFloat(new Color32(0, 255, 0, 255)),
                metallic = 0.5f,
                smoothness = 0.5f,
            },
            new VoxelColor32
            {
                color = Helper.Color32ToFloat(new Color32(0, 255, 255, 255)),
                metallic = 0.5f,
                smoothness = 0.5f,
            },
            new VoxelColor32
            {
                color = Helper.Color32ToFloat(new Color32(0, 0, 255, 255)),
                metallic = 0.5f,
                smoothness = 0.5f,
            },
            new VoxelColor32
            {
                color = Helper.Color32ToFloat(new Color32(255, 0, 255, 255)),
                metallic = 0.5f,
                smoothness = 0.5f,
            }
        };

        _voxelColors = new ComputeBuffer(colors.Length, sizeof(float) * 3);
        _voxelColors.SetData(colors);

        _voxelShader.SetBuffer(0, "voxelColors", _voxelColors);
        _voxelShader.SetInt("chunkWidth", Constants.CHUNK_WIDTH);
        _voxelShader.SetInt("chunkHeight", Constants.CHUNK_HEIGHT);
    }
}

struct VoxelColor32
{
    public float color;
    public float metallic;
    public float smoothness;
}

public class MeshBuffer
{
    public ComputeBuffer voxelArray;
    public ComputeBuffer vertexBuffer;
    public ComputeBuffer colorBuffer;
    public ComputeBuffer indexBuffer;
    public ComputeBuffer countBuffer;

    public float3[] vertices;
    public float4[] colors;
    public int[] indices;
    public uint[] counts;

    public bool Initialized;
    public bool IsDone;

    public void InitializeBuffer()
    {
        if (Initialized)
            return;

        countBuffer = new ComputeBuffer(2, 4, ComputeBufferType.Counter);
        countBuffer.SetCounterValue(0);
        counts = new uint[2] { 0, 0 };
        countBuffer.SetData(counts);

        int maxTris = Constants.CHUNK_VOLUME / 4;
        //width*height*width*faces*tris

        vertexBuffer ??= new ComputeBuffer(maxTris * 3, 12);
        colorBuffer ??= new ComputeBuffer(maxTris * 3, 16);
        indexBuffer ??= new ComputeBuffer(maxTris * 3, 12);

        Initialized = true;
        IsDone = false;
    }

    public void ResizeArrays(int count)
    {
        vertices = new float3[count];
        colors = new float4[count];
        indices = new int[count];
    }

    public void Dispose()
    {
        voxelArray?.Dispose();
        vertexBuffer?.Dispose();
        colorBuffer?.Dispose();
        indexBuffer?.Dispose();
        countBuffer?.Dispose();

        Initialized = false;
        IsDone = false;
    }
}