using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[GenerateAuthoringComponent]
public struct SprintComponent : IComponentData
{
    public bool Value;

    public float Multiplier;

    public float GetMultiplier()
    {
        return Value ? Multiplier : 1.0f;
    }
}
