using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct ColumnGenerateComponent : IComponentData
{
    public int2 Index;
}
