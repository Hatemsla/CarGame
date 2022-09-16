using System;
using UnityEngine;

public class ControllerInfomation
{
    public int X { get; set; }
    public int Y { get; set; }
    public int R { get; set; }
    public bool FingerState { get; set; }
    public bool PalmState { get; set; }
    public bool RState { get; set; }

    public ControllerInfomation(int x, int y, int r, bool fingerState, bool palmState, bool rState)
    {
        X = x;
        Y = y;
        R = r;
        FingerState = fingerState;
        PalmState = palmState;
        RState = rState;
    }

    public override string ToString()
    {
        return $"{X} {Y} {R} {FingerState} {PalmState} {RState}";
    }
}