using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CustomRandom {
    public static CustomRandom mInstance = new CustomRandom();

    private System.Random ran;

    public CustomRandom()
    {
        ran = new System.Random();
    }

    public void Seed(int seed)
    {
        ran = new System.Random(seed);
    }

    public int GetValue(int _low, int _high)
    {
        return ran.Next(_low, _high);
    }
    public double GetValue()
    {
        return ran.NextDouble();
    }
    public int GetValue_v2()
    {
        return ran.Next(Int32.MinValue, Int32.MaxValue);
    }
}
