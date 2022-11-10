using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(InputMovementSystem))]
[UpdateAfter(typeof(CameraInputSystem))]
public partial class FirstPersonMovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        Entities
            .WithAll<FirstPersonCameraTag>()
            .ForEach((ref Translation translation, in MovementInputComponent input, in MovementSpeedComponent speed, in CameraComponent camera, in SprintComponent sprint) => {
                //move based on yaw and input
                float cos = math.cos(camera.Yaw);
                float sin = math.sin(camera.Yaw);

                float forward = input.Forward * speed.Value * deltaTime * cos + -input.Right * speed.Value * deltaTime * sin;
                float right = input.Forward * speed.Value * deltaTime * sin + input.Right * speed.Value * deltaTime * cos;
                float up = input.Up * speed.Value * deltaTime;

                //apply values
                translation.Value += new float3(right, up, forward) * sprint.GetMultiplier();
        }).Schedule();
    }
}
