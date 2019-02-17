using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using NetworkTrungVI;

public class TurnData 
{
    private UInt32 mCrc;
    private int mPlayerId;
    private CommandList mCommandList;
    private int mRandomValue;
    public UInt32 GetCRC() { return mCrc; }
    public int GetPlayerID() { return mPlayerId; }
    public CommandList GetCommandList() { return mCommandList; }
    public int GetRandomValue() { return mRandomValue; }

    public TurnData(int _playerid, int _randomValue, CommandList _commandlist, UInt32 _crc)
    {
        this.mPlayerId = _playerid;
        this.mRandomValue = _randomValue;
        this.mCommandList = _commandlist;
        this.mCrc = _crc;
    }
    public TurnData()
    {
        this.mPlayerId = -1;
        this.mRandomValue = -1;
        this.mCommandList = null;
        this.mCrc = 0;
    }

    public void Write(ref OutputMemoryBitStream output)
    {
        output.Write(mPlayerId);
        output.Write(mRandomValue);
        output.Write(mCrc);
        mCommandList.Write(ref output);
    }

    public void Read(ref InputMemoryBitStream input)
    {
        input.Read(ref mPlayerId);
        input.Read(ref mRandomValue);
        input.Read(ref mCrc);
        mCommandList = new CommandList();
        mCommandList.Read(ref input);
    }
}