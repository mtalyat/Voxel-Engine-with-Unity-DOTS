using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public partial class ColumnGenerationSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _bufferSystem;

    private EntityQuery _columnQuery;

    private EntityArchetype _chunkArchetype;

    protected override void OnCreate()
    {
        _bufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        _chunkArchetype = EntityManager.CreateArchetype(typeof(VoxelElement));
    }

    protected override void OnUpdate()
    {
        InitializeColumns();

        GenerateChunks();

        AssignChunkSharedIndices();
    }

    private void InitializeColumns()
    {
        var buffer = _bufferSystem.CreateCommandBuffer();

        Entities
            .WithoutBurst()
            .WithNone<ColumnIndexComponent>()
            .ForEach((int entityInQueryIndex, Entity e, in ColumnGenerateComponent generation) =>
            {
                int2 i = generation.Index;

                buffer.AddComponent<ColumnIndexComponent>(e, new ColumnIndexComponent { Value = i});
                buffer.AddSharedComponent<ColumnSharedIndexComponent>(e, new ColumnSharedIndexComponent { Value = i });
                buffer.AddBuffer<LinkedEntityGroup>(e);
                buffer.AddComponent<ColumnTag>(e);
            }).Run();
        _bufferSystem.AddJobHandleForProducer(Dependency);
    }

    private void GenerateChunks()
    {
        var buffer = _bufferSystem.CreateCommandBuffer().AsParallelWriter();

        var chunkArchetype = _chunkArchetype;

        Entities
            .WithStoreEntityQueryInField(ref _columnQuery)
            .WithAll<ColumnGenerateComponent>()
            .ForEach((int entityInQueryIndex, Entity e, ref DynamicBuffer<LinkedEntityGroup> linkedEntities, in ColumnIndexComponent columnIndex) =>
            {
                //heightmap stuff

                //create a buffer of children
                var playbackBuffer = buffer.SetBuffer<LinkedEntityGroup>(entityInQueryIndex, e);

                //if the column already has children, add them to the buffer
                if(linkedEntities.Length > 0)
                {
                    playbackBuffer.AddRange(linkedEntities.AsNativeArray());
                }

                //generate chunks that are at the ground level or lower
                for (int y = 0; y < Constants.COLUMN_HEIGHT; y++)
                {
                    var chunkEntity = buffer.CreateEntity(entityInQueryIndex, chunkArchetype);

                    buffer.AddComponent<ColumnIndexComponent>(entityInQueryIndex, chunkEntity, columnIndex);
                    buffer.AddComponent<ChunkPositionComponent>(entityInQueryIndex, chunkEntity, new ChunkPositionComponent { Y = y });
                    buffer.AddComponent<ChunkTag>(entityInQueryIndex, chunkEntity);
                    buffer.AddComponent<ChunkGenerateTag>(entityInQueryIndex, chunkEntity);

                    playbackBuffer.Add(chunkEntity);
                }

                buffer.RemoveComponent<ColumnGenerateComponent>(entityInQueryIndex, e);
            }).ScheduleParallel();

        _bufferSystem.AddJobHandleForProducer(Dependency);
    }

    private void AssignChunkSharedIndices()
    {
        var buffer = _bufferSystem.CreateCommandBuffer();

        Entities
            .WithoutBurst()
            .WithAll<ChunkTag>()
            .WithNone<ColumnSharedIndexComponent>()
            .ForEach((Entity e, in ColumnIndexComponent columnIndex) =>
            {
                int2 index = columnIndex.Value;

                buffer.AddSharedComponent<ColumnSharedIndexComponent>(e, new ColumnSharedIndexComponent { Value = index});
            }).Run();
    }
}
