using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[GenerateAuthoringComponent]
public struct RotateComponent : IComponentData
{
    public float3 TargetDirection;

    public float Speed;
}
