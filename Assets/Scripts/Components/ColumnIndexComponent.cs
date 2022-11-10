using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct ColumnIndexComponent : IComponentData
{
    public int2 Value;
}
