using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetworkTrungVI;

/// <summary>
/// trong 1 trò chơi RTS, các peer gửi cho nhau các gói command( lệnh điều khiển từ người dùng)
/// để thực hiện đồng bộ hóa trò chơi.
/// </summary>
public class Command
{
    public enum CommandType
    {
        CM_INVALID = 0,
        CM_ATTACK = 1,
        CM_MOVE = 2,
        CM_ROTATE = 3,
        CM_SPAWN = 4
    };

    protected int mNetworkID = 0;
    protected int mPlayerID = 0;
    protected CommandType mCommandType = CommandType.CM_INVALID;
    public Command()
    {
        mNetworkID = 0;
        mPlayerID = 0;
        mCommandType = CommandType.CM_INVALID;
    }

    public void SetPlayerID(int _inData) { mPlayerID = _inData; }
    public int GetPlayerID() { return mPlayerID; }
    public void SetNetworkID(int _inData) { mNetworkID = _inData; }
    public int GetNetworkID() { return mNetworkID; }

    public virtual void Read(ref InputMemoryBitStream input) { return; }
    public virtual void Write(ref OutputMemoryBitStream output)
    {
        int type = (int)mCommandType;
        output.Write(type);
        output.Write(mNetworkID);
        output.Write(mPlayerID);
    }
    public virtual void ProcessCommand() { return; } // được thực hiện khi 1 lệnh được thực thi.

    public static Command StaticReadAndCreate(ref InputMemoryBitStream input)
    {
        Command returnval = new Command();
        int type = 0;
        input.Read(ref type);
        int networkid = 0;
        input.Read(ref networkid);
        int playerid = 0;
        input.Read(ref playerid);
        CommandType mtype = (CommandType)type;

        switch (mtype)
        {
            case CommandType.CM_ATTACK:
                {
                    returnval = new AttackCommand();
                    returnval.SetNetworkID(networkid);
                    returnval.SetPlayerID(playerid);
                    returnval.Read(ref input);
                    break;
                }
            case CommandType.CM_MOVE:
                {
                    returnval = new MoveCommand();
                    returnval.SetNetworkID(networkid);
                    returnval.SetPlayerID(playerid);
                    returnval.Read(ref input);
                    break;
                }
            case CommandType.CM_SPAWN:
                {
                    returnval = new SpawnCommand();
                    returnval.SetNetworkID(networkid);
                    returnval.SetPlayerID(playerid);
                    returnval.Read(ref input);
                    break;
                }
            case CommandType.CM_ROTATE:
                {
                    break;
                }
            default: break;

        }

        return returnval;
    }

}