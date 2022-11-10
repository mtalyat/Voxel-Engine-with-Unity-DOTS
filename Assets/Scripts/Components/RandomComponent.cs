using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

[Serializable]
[GenerateAuthoringComponent]
public struct RandomComponent : IComponentData
{
    public Random Value;
}
