using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetworkTrungVI;

public class MoveCommand : Command 
{

    public MoveCommand() { mCommandType = CommandType.CM_MOVE; }

    protected Vector3 mTarget;
    public void SetTargetPosition(Vector3 _pos) { mTarget = _pos; }
    public static MoveCommand StaticCreate(int _inNetworkID, Vector3 _inTarget)
    {
        MoveCommand re = null;
        GameObject gameob = NetworkManager_v2.mInstance.GetGameobjectByNetworkId(_inNetworkID);
        int playerid = NetworkManager_v2.mInstance.GetPlayerID();
        if (gameob != null)
        {
            NetworkObject netob = gameob.GetComponent<NetworkObject>();
            if (netob != null)
            {
                if (playerid == netob.GetPlayerId())
                {
                    re = new MoveCommand();
                    re.SetNetworkID(_inNetworkID);
                    re.SetPlayerID(playerid);
                    re.SetTargetPosition(_inTarget);
                }
            }
        }
        return re;
    }

    public override void Read(ref InputMemoryBitStream input)
    {
        input.Read(ref mTarget);
    }

    public override void Write(ref OutputMemoryBitStream output)
    {
        base.Write(ref output);
        output.Write(mTarget);
    }

    public override void ProcessCommand()
    {
        GameObject _gameob = NetworkManager_v2.mInstance.GetGameobjectByNetworkId(mNetworkID);
        if (_gameob != null)
        {
            NetworkObject _netOb = _gameob.GetComponent<NetworkObject>();
            if (_netOb != null)
            {
                if (_netOb.GetPlayerId() == mPlayerID)
                {
                    _netOb.ToMove(mTarget);
                }
            }
        }
    }
}
