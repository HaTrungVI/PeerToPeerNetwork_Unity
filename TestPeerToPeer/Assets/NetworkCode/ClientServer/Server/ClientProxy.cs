using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;

public class ClientProxy {
    private int mPlayerId;  // id của client
    //private int mNetworkId; // id gameobject mà client đang đó đang điều khiển.
    private string mName = "Client";
    private EndPoint mEndpoint;
    private float mLastPacketFromThisClientTime = 0;

    public void UpdateLastPacketTime()
    {
        mLastPacketFromThisClientTime = Time.time;
    }
    public ClientProxy(EndPoint _inEndpoint, string _inName, int _inPlayerId)
    {
        mEndpoint = _inEndpoint;
        mName = _inName;
        mPlayerId = _inPlayerId;
        mLastPacketFromThisClientTime = Time.time;
    }

    private ReplicationManagerServer mReplicationManagerServer = new ReplicationManagerServer();

    public ReplicationManagerServer GetReplicationManagerServer() { return mReplicationManagerServer; }
    public int GetPlayerID() { return mPlayerId; }
    public string GetName() { return mName; }
    public EndPoint GetEndpoint() { return mEndpoint;}
    public float GetLastTimeReceivedPacket() { return mLastPacketFromThisClientTime; }
    //public int GetNetworkID() { return mNetworkId; }
    //public void SetNetworkID(int _indata) { mNetworkId = _indata; }


}
