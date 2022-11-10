using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public partial class DestroySystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _bufferSystem;

    protected override void OnCreate()
    {
        _bufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var buffer = _bufferSystem.CreateCommandBuffer().AsParallelWriter();

        //destory all entities with the destroy tag
        Entities
            .WithAll<DestroyTag>()
            .ForEach((int entityInQueryIndex, Entity e) => {
                buffer.DestroyEntity(entityInQueryIndex, e);
        }).ScheduleParallel();

        _bufferSystem.AddJobHandleForProducer(Dependency);
    }
}
