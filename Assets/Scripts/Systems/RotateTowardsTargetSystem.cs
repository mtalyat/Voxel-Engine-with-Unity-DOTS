using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public partial class RotateTowardsTargetSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref RotateComponent rotate, in Translation translation, in TargetComponent target) =>
        {
            //get translation of target
            ComponentDataFromEntity<Translation> translations = GetComponentDataFromEntity<Translation>(true);
            if (!translations.HasComponent(target.Value))
            {
                //no translation on target
                return;
            }

            Translation targetTranslation = translations[target.Value];

            rotate.TargetDirection = targetTranslation.Value - translation.Value;
        }).Schedule();
    }
}
