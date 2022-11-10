using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[InternalBufferCapacity(0)]
public struct VoxelElement : IBufferElementData
{
    /*
     * Byte breakdown:
     * [1]       [2]       [3]       [4]
     * [00000000][00000000][00000000][00000000]
     * 
     * [1] Empty
     * [2] Empty
     * [3] Type
     * [4] Color
     */
    public uint Data;

    public static implicit operator uint(VoxelElement e) => e.Data;
    public static implicit operator VoxelElement(uint i) => new VoxelElement { Data = i };

    public void SetVoxelColor(byte color)
    {
        Data = (Data & Constants.BLOCK_EMPTY_INT_FOUR) | color;
    }

    public byte GetVoxelColor()
    {
        return (byte)(Data & Constants.BLOCK_FULL_INT);
    }

    public void SetVoxelType(byte type)
    {
        Data = (Data & Constants.BLOCK_EMPTY_INT_THREE) | ((uint)type << 8);
    }

    public byte GetVoxelType()
    {
        return (byte)((Data >> 8) & Constants.BLOCK_FULL_INT);
    }
}
