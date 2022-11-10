using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public static class Constants
{
    public const int CHUNK_HEIGHT = 32;
    public const int CHUNK_WIDTH = 32;
    public static int3 ChunkSize => new int3(CHUNK_WIDTH, CHUNK_HEIGHT, CHUNK_WIDTH);
    public const int CHUNK_VOLUME = CHUNK_WIDTH * CHUNK_WIDTH * CHUNK_HEIGHT;
    public const int CHUNK_VOLUME_PADDED = (CHUNK_WIDTH + 2) * (CHUNK_WIDTH + 2) * (CHUNK_HEIGHT + 2);

    public const int COLUMN_HEIGHT = 8;

    public const int TOTAL_VOXEL_HEIGHT = CHUNK_HEIGHT * COLUMN_HEIGHT;

    public const int X_THREADS = CHUNK_WIDTH / 8 + 1;
    public const int Y_THREADS = CHUNK_HEIGHT / 8;

    public const ushort BLOCK_FULL_SHORT = 0b0000000011111111;

    public const uint BLOCK_FULL_INT = 0b00000000000000000000000011111111;

    public const uint BLOCK_EMPTY_INT_ONE = 0b00000000111111111111111111111111;
    public const uint BLOCK_EMPTY_INT_TWO = 0b11111111000000001111111111111111;
    public const uint BLOCK_EMPTY_INT_THREE = 0b11111111111111110000000011111111;
    public const uint BLOCK_EMPTY_INT_FOUR = 0b11111111111111111111111100000000;
}
