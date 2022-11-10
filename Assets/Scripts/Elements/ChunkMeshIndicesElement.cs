using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[InternalBufferCapacity(0)]
public struct ChunkMeshIndicesElement : IBufferElementData
{
    public int Value;

    public static implicit operator int(ChunkMeshIndicesElement i) => i.Value;
    public static implicit operator ChunkMeshIndicesElement(int i) => new ChunkMeshIndicesElement { Value = i };
}
