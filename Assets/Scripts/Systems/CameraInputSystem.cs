using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class CameraInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        float deltaTime = Time.DeltaTime;
        
        Entities.ForEach((ref CameraComponent camera, in CameraInputComponent input) => {
            camera.Pitch -= mouseY * input.Sensitivity * deltaTime;
            camera.Pitch = math.clamp(camera.Pitch, -math.PI / 2.0f, math.PI / 2.0f);
            camera.Yaw += mouseX * input.Sensitivity * deltaTime;
        }).Schedule();
    }
}
