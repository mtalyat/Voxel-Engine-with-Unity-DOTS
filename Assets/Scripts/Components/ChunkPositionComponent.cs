using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct ChunkPositionComponent : IComponentData
{
    public int Y;
}
