using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using NetworkTrungVI;

public class ReplicationManagerClient {
   
    public void ProcessPacket(ref InputMemoryBitStream _input)
    {
        while (_input.GetRemainingBitsCount() >= 32)
        {
            int networkid = -1;
            _input.Read(ref networkid);
            if (networkid == -1) break;     // tránh trường hợp đọc thừa dữ liệu.

            ReplicationCommand.ReplicationAction action;
            int actionInt = -1;
            _input.Read(ref actionInt);
            if (actionInt == -1) break;

            
            action = (ReplicationCommand.ReplicationAction)actionInt;

            switch (action)
            {
                case ReplicationCommand.ReplicationAction.RA_Create:
                    {
                        ProcessCreateAction(ref _input, networkid);
                        break;
                    }
                case ReplicationCommand.ReplicationAction.RA_Update:
                    {
                        ProcessUpdateAction(ref _input, networkid);
                        break;
                    }
                case ReplicationCommand.ReplicationAction.RA_Destroy:
                    {
                        ProcessDestroyAction(ref _input, networkid);
                        break;
                    }
                default: break;
            }
        }
       //Debug.Log(_Count);
    }
    private void ProcessCreateAction(ref InputMemoryBitStream _input, int inNetworkId)
    {
        if (NetworkManagerClient.mInstance.GetDisconnected()) return;
        GameObject mGameobject = NetworkManagerClient.mInstance.GetGameobjectByNetworkId(inNetworkId);
        if (!mGameobject)
        {
            mGameobject = NetworkManagerClient.mInstance.GetClient().InstanceGameobject(NetworkManagerClient.mInstance.mGameobjectToSpawn);
            //Debug.Log("Spawn 1 gameobject    networkID " + inNetworkId);
            TransformSynch _sync = mGameobject.GetComponent<TransformSynch>();
            if (_sync)
            {
                _sync.SetIdNetwork(inNetworkId);
            }
            NetworkManagerClient.mInstance.AddGameobjectToIdGameobjectDic(mGameobject);
        }
        TransformSynch sync = mGameobject.GetComponent<TransformSynch>();
        if (sync)
        {
            if (inNetworkId == NetworkManagerClient.mInstance.GetNetworkId())
            {
                sync.SetIsLocalPlayer(true);
                NetworkManagerClient.mInstance.SetLocalGameobject(mGameobject);
            }
            else sync.SetIsLocalPlayer(false);
            sync.Read(ref _input);
            sync.ResetTransform();
        }
        else
        {
            sync = NetworkManagerClient.mInstance.mGameobjectToSpawn.GetComponent<TransformSynch>();
            if (sync)
            {
                sync.Read(ref _input);
                sync.ResetTransform();
            }
            else Debug.Log("Gameobject khong chua TransformSynch");
        }
    }
    private void ProcessUpdateAction(ref InputMemoryBitStream _input, int inNetworkId)
    {
        GameObject mGameobject = NetworkManagerClient.mInstance.GetGameobjectByNetworkId(inNetworkId);
        if (mGameobject)
        {
            TransformSynch sync = mGameobject.GetComponent<TransformSynch>();
            if (sync)
            {
                sync.Read(ref _input);
            }
        }
    }
    private void ProcessDestroyAction(ref InputMemoryBitStream _input, int inNetworkId)
    {
        GameObject mGameobject = NetworkManagerClient.mInstance.GetGameobjectByNetworkId(inNetworkId);
        if (mGameobject)
        {
            NetworkManagerClient.mInstance.RemoveGameobjectFromIdGameobjectDic(mGameobject);

            if (mGameobject == NetworkManagerClient.mInstance.GetLocalGameobject())
            {
                NetworkManagerClient.mInstance.SetLocalGameobject(null);
            }

            NetworkManagerClient.mInstance.GetClient().DestroyGameobject(mGameobject);
        }
    }


}
