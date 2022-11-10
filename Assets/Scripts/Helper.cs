using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public static class Helper
{
    public static readonly int3[] Directions3D = new int3[6]
    {
        new int3(0, 0, 1),
        new int3(0, 0, -1),
        new int3(0, 1, 0),
        new int3(0, -1, 0),
        new int3(1, 0, 0),
        new int3(-1, 0, 0)
    };

    public static int3 IndexToPos3D(int index)
    {
        int z = index / (Constants.CHUNK_WIDTH * Constants.CHUNK_HEIGHT);
        int y = (index - z * Constants.CHUNK_WIDTH * Constants.CHUNK_HEIGHT) / Constants.CHUNK_WIDTH;
        int x = index - Constants.CHUNK_WIDTH * (y + Constants.CHUNK_HEIGHT * z);

        return new int3(x, y, z);
    }

    public static int Pos3DToIndex(int x, int y, int z)
    {
        return (z * Constants.CHUNK_HEIGHT + y) * Constants.CHUNK_WIDTH + x;
    }

    public static int Pos3DToIndex(int3 pos) => Pos3DToIndex(pos.x, pos.y, pos.z);

    public static int2 IndexToPos2D(int index)
    {
        int x = index % Constants.CHUNK_WIDTH;
        int z = index / Constants.CHUNK_WIDTH;

        return new int2(x, z);
    }

    public static int Pos2DToIndex(int x, int z)
    {
        return z * Constants.CHUNK_WIDTH + x;
    }

    public static int Pos2DToIndex(int2 pos) => Pos2DToIndex(pos.x, pos.y);

    public static float Color32ToFloat(Color32 c)
    {
        if (c.r == 0)
            c.r = 1;
        if (c.g == 0)
            c.g = 1;
        if (c.b == 0)
            c.b = 1;
        if (c.a == 0)
            c.a = 1;

        return (c.r << 24) | (c.g << 16) | (c.b << 8) | (c.a);
    }
}
