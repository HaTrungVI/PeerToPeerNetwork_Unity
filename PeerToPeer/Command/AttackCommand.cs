using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetworkTrungVI;

public class AttackCommand : Command
{
    protected int mTargetNetworkID;
    public void SetTargetNetworkID(int _inID) { mTargetNetworkID = _inID; }
    public AttackCommand() { mCommandType = CommandType.CM_ATTACK; }

    public static AttackCommand StaticCreate(int _inMyNetworkID, int _inTargetNetworkID)
    {
        AttackCommand re = null;
        GameObject gameob = NetworkManager_v2.mInstance.GetGameobjectByNetworkId(_inMyNetworkID);
        GameObject targetob = NetworkManager_v2.mInstance.GetGameobjectByNetworkId(_inTargetNetworkID);
        int playerid = NetworkManager_v2.mInstance.GetPlayerID();
        if (gameob != null && targetob != null)
        {
            NetworkObject netob = gameob.GetComponent<NetworkObject>();
            NetworkObject netTarget = targetob.GetComponent<NetworkObject>();
            if (netob != null && netTarget != null)
            {
                if (netTarget.GetPlayerId() != netob.GetPlayerId() && playerid == netob.GetPlayerId())
                {
                    
                    re = new AttackCommand();
                    re.SetNetworkID(_inMyNetworkID);
                    re.SetPlayerID(playerid);
                    re.SetTargetNetworkID(_inTargetNetworkID);
                }
            }
        }
        return re;
    }

    public override void Read(ref InputMemoryBitStream input)
    {
        input.Read(ref mTargetNetworkID);
    }

    public override void Write(ref OutputMemoryBitStream output)
    {
        base.Write(ref output);
        output.Write(mTargetNetworkID);
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
                    _netOb.ToAttack(mTargetNetworkID);
                }
            }
        }
    }
}
