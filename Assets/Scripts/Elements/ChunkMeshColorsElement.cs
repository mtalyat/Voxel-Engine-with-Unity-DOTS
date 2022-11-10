using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[InternalBufferCapacity(0)]
public struct ChunkMeshColorsElement : IBufferElementData
{
    public float4 Value;

    public static implicit operator float4(ChunkMeshColorsElement i) => i.Value;
    public static implicit operator ChunkMeshColorsElement(float4 f) => new ChunkMeshColorsElement { Value = f };
}
