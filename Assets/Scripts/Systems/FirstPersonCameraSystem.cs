using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(CameraInputSystem))]
public partial class FirstPersonCameraSystem : SystemBase
{
    protected override void OnCreate()
    {
        //hide and lock cursor
        UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
    }

    protected override void OnUpdate()
    {
        Entities
            .WithAll<FirstPersonCameraTag>()
            .ForEach((ref Rotation rotation, in CameraComponent camera) => {
                //rotate camera up/down
                rotation.Value = quaternion.EulerXYZ(camera.Pitch, camera.Yaw, 0.0f);
        }).Schedule();
    }
}
