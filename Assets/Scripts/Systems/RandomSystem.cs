using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public partial class RandomSystem : SystemBase
{
    protected override void OnUpdate()
    {
        //base random seed on time so it is "truly" random
        long time = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;

        Entities.ForEach((Entity e, int entityInQueryIndex, ref RandomComponent random) => {
            random.Value = Random.CreateFromIndex((uint)(entityInQueryIndex + time));
        }).ScheduleParallel();
    }
}
