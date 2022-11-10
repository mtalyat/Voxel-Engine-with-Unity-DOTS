using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct ColumnSharedIndexComponent : ISharedComponentData, IEquatable<ColumnSharedIndexComponent>
{
    public int2 Value;

    public bool Equals(ColumnSharedIndexComponent other)
    {
        return Value.x == other.Value.x && Value.y == other.Value.y;
    }

    public override int GetHashCode()
    {
        ulong x = (ulong)Value.x;
        ulong y = (ulong)Value.y;

        x = (x | (x << 16)) & 0x0000FFFF0000FFFF;
        x = (x | (x << 8)) & 0x00FF00FF00FF00FF;
        x = (x | (x << 4)) & 0x0F0F0F0F0F0F0F0F;
        x = (x | (x << 2)) & 0x3333333333333333;
        x = (x | (x << 1)) & 0x5555555555555555;

        y = (y | (y << 16)) & 0x0000FFFF0000FFFF;
        y = (y | (y << 8)) & 0x00FF00FF00FF00FF;
        y = (y | (y << 4)) & 0x0F0F0F0F0F0F0F0F;
        y = (y | (y << 2)) & 0x3333333333333333;
        y = (y | (y << 1)) & 0x5555555555555555;

        int v = (int)(x | (y << 1));

        return v;
    }
}
