using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnCommand : Command
{
    public SpawnCommand() { mCommandType = CommandType.CM_SPAWN; }
    protected Vector3 mPosition;
    protected int mSpawnID;

    public void SetPosition(Vector3 _indata) { mPosition = _indata; }
    public void SetSpawnID(int _indata) { mSpawnID = _indata; }

    public static SpawnCommand StaticCreate(Vector3 _vt, int _id)
    {
        SpawnCommand re = null;
        int playerid = NetworkManager_v2.mInstance.GetPlayerID();
        re = new SpawnCommand();
        re.SetPlayerID(playerid);
        re.SetSpawnID(_id);
        re.SetPosition(_vt);

        return re;
    }

    public override void Write(ref NetworkTrungVI.OutputMemoryBitStream output)
    {
        base.Write(ref output);
        output.Write(mPosition);
        output.Write(mSpawnID);
    }
    public override void Read(ref NetworkTrungVI.InputMemoryBitStream input)
    {
        input.Read(ref mPosition);
        input.Read(ref mSpawnID);
    }
    public override void ProcessCommand()
    {
        NetworkManager_v2.mInstance.GetPeerToPeerManager().InstantiateGameobject(mPlayerID, mPosition, mSpawnID);
    }
}
