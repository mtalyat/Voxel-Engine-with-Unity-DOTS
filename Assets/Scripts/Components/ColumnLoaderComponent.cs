using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[GenerateAuthoringComponent]
public struct ColumnLoaderComponent : IComponentData
{
    public int Distance;
    public int2 ColumnIndex;
}
