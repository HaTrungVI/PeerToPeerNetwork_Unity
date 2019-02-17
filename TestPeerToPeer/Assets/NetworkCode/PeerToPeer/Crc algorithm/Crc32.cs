using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System;

/// <summary>
/// Nếu có thời gian các bạn nên tự tìm hiểu thuật toán
/// và viết lại class này cho chắc.
/// </summary>
public class Crc32 : HashAlgorithm
{

    public const UInt32 crc32polyreverse = 0xEDB88320;

    private UInt32 hash;
    
    private static bool first = true;
    private static UInt32[] mTable = new UInt32[256];

    public static UInt32 Compute(byte indata)
    {
        UInt32 remainder = indata ^ 0xFFFFFFFF;

        for (int i = 0; i < 8; i++)
        {
            if ((remainder & 1) != 0)
            {
                remainder = (remainder >> 1) ^ crc32polyreverse;
            }
            else
            {
                remainder >>= 1;
            }
        }
        remainder ^= 0xFFFFFFFF;
        return remainder;
    }

    public static UInt32 Compute(byte[] data)
    {
        return CalculateHash(InitTable(), data, 0, data.Length);
    }

    public static UInt32 Compute(byte[] data, int offset, int length)
    {
        return CalculateHash(InitTable(), data, offset, length);
    }

    private static UInt32 CalculateHash(UInt32[] table, byte[] data, int offset, int length)
    {
        UInt32 remainder = 0xFFFFFFFF;
        for (int i = offset; i < offset + length; i++)
        {
            remainder = table[(remainder & 0xFF) ^ data[i]] ^ (remainder >> 8);
        }
        remainder ^= 0xFFFFFFFF;
        return remainder;
    }

    private static UInt32[] InitTable()
    {
        if (!first) return mTable;

        UInt32 remainder;
        first = false;
        for (UInt32 i = 0; i < 256; i++)
        {
            remainder = i;
            for (int j = 0; j < 8; j++)
            {
                if ((remainder & 1) != 0)
                {
                    remainder = (remainder >> 1) ^ crc32polyreverse;
                }
                else
                {
                    remainder >>= 1;
                }
            }
            mTable[i] = remainder;
        }
        return mTable;
    }

    public override int HashSize
    {
        get
        {
            return 32;
        }
    }

    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        hash = CalculateHash(mTable, array, ibStart, cbSize);
    }

    public override void Initialize()
    {
        hash = 0xFFFFFFFF;
    }

    protected override byte[] HashFinal()
    {
        var d = Uint32ToBigEndianBytes(hash);
        HashValue = d;
        return d;
    }

    private static byte[] Uint32ToBigEndianBytes(UInt32 data)
    {
        var m = BitConverter.GetBytes(data);

        if (BitConverter.IsLittleEndian) Array.Reverse(m);

        return m;
    }
}
