using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[InternalBufferCapacity(0)]
public struct ChunkMeshVerticesElement : IBufferElementData
{
    public float3 Value;

    public static implicit operator float3(ChunkMeshVerticesElement i) => i.Value;
    public static implicit operator ChunkMeshVerticesElement(float3 f) => new ChunkMeshVerticesElement { Value = f };
}
