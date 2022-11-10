using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[GenerateAuthoringComponent]
public struct MovementInputComponent : IComponentData
{
    public float Right;
    public float Forward;
    public float Up;
}
