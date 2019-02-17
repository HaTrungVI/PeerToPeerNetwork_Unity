using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using NetworkTrungVI;

public class ReceivedPacket {

    private InputMemoryBitStream mInput;
    private EndPoint mFromAddressEndpoint;
    private float mReceivedTime;

    public ReceivedPacket(ref InputMemoryBitStream _input, EndPoint _inFromAddrEp, float _inTime)
    {
        mInput = _input;
        mFromAddressEndpoint = _inFromAddrEp;
        mReceivedTime = _inTime;
    }
    public InputMemoryBitStream GetInputMemoryStream() { return mInput; }
    public EndPoint GetFromAddressEndpoint() { return mFromAddressEndpoint; }
    public float GetReceivedTime() { return mReceivedTime; }
}
