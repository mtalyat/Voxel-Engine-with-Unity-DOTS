using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[GenerateAuthoringComponent]
public struct CameraComponent : IComponentData
{
    public float Pitch;
    public float Yaw;
}
