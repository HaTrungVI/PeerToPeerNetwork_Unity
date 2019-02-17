using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using NetworkTrungVI;

public class ReplicationManagerServer {
    private List<int> mNetworkIdToRemove = new List<int>();
    private Dictionary<int, ReplicationCommand> mNetworkIdToReplicationCmd = new Dictionary<int, ReplicationCommand>();

    public void ReplicateCreate(int _inNetworkID, int _inDirtupState)
    {
        mNetworkIdToReplicationCmd[_inNetworkID] = new ReplicationCommand(_inDirtupState);
    }
    public void ReplicateDestroy(int _inNetworkID)
    {
        mNetworkIdToReplicationCmd[_inNetworkID].SetDestroy();
    }
    public void AddDirtyState(int _inNetworkID, int _inDirtyState)
    {
        mNetworkIdToReplicationCmd[_inNetworkID].AddDirtyState(_inDirtyState);
    }

    int WriteCreateAction(ref OutputMemoryBitStream _output, int _inNetworkId, int _inDirtyState)
    {
        // chúng ta phải spawn 1 gameoject trước gọi phương thức này.
        GameObject gameOb = NetworkManagerServer.mInstance.GetGameobjectByNetworkId(_inNetworkId);
        TransformSynch sync = gameOb.GetComponent<TransformSynch>();
        if (sync)
        {
            return sync.Write(ref _output, _inDirtyState);
        }
        return 0;
    }
    int WriteUpdateAction(ref OutputMemoryBitStream _output, int _inNetworkId, int _inDirtyState)
    {
        GameObject gameOb = NetworkManagerServer.mInstance.GetGameobjectByNetworkId(_inNetworkId);
        TransformSynch sync = gameOb.GetComponent<TransformSynch>();
        if (sync)
        {
            return sync.Write(ref _output, _inDirtyState);
        }
        return 0;
    }
    int WriteDestroyAction(ref OutputMemoryBitStream _output, int _inNetworkId, int _inDirtyState)
    {
        // không làm gì cả
        return _inDirtyState;
    }

    public void Write(ref OutputMemoryBitStream output)
    {
        //Debug.Log(mNetworkIdToReplicationCmd.Count);
        foreach (var pair in mNetworkIdToReplicationCmd)
        {
            ReplicationCommand replicationCmd = pair.Value;
            //Debug.Log(pair.Key + "   " + replicationCmd.GetAction() + "   " + replicationCmd.HasDirtyState());
            if (replicationCmd.HasDirtyState())
            {
                int networkid = pair.Key;
                output.Write(networkid);                                                                // ghi network id
                ReplicationCommand.ReplicationAction action = replicationCmd.GetAction();
                int intAction = (int)action;
                output.Write(intAction);                                                                  // ghi action
                //Debug.Log(networkid + "  " + action + "   " + intAction);
                int writtenState = 0;
                int dirtyState = replicationCmd.GetDirtyState();

                switch (action)
                {
                    case ReplicationCommand.ReplicationAction.RA_Create:                                // Replication command với chỉ thị tạo 1 đối tượng.
                        {
                            writtenState = WriteCreateAction(ref output, networkid, dirtyState);        // ghi những dữ liệu cần thiết để gửi đi.
                            replicationCmd.SetAction(ReplicationCommand.ReplicationAction.RA_Update);   // sau khi ghi xong chuyến trạng thái của command này sang update.
                            break;
                        }
                    case ReplicationCommand.ReplicationAction.RA_Update:                                // Replication ra lệnh update 1 đối tượng.
                        {
                            writtenState = WriteUpdateAction(ref output, networkid, dirtyState);
                            break;
                        }
                    case ReplicationCommand.ReplicationAction.RA_Destroy:
                        {
                            writtenState = WriteDestroyAction(ref output, networkid, dirtyState);
                            mNetworkIdToRemove.Add(networkid);
                            break;
                        }
                    default: break;
                }
                replicationCmd.ClearDirtyState(writtenState);                                           // Xóa bỏ những bit trạng thái đã được ghi vào luồng dữ liêu
                                                                                                        // và chuẩn bị gửi đi.
            }
        }
        if (mNetworkIdToRemove.Count != 0)
        {
            foreach (int id in mNetworkIdToRemove)
            {
                RemoveFromIdToReplicationCommand(id);
            }
            mNetworkIdToRemove.Clear();
        }
    }

    private void RemoveFromIdToReplicationCommand(int _inNetworkId)
    {
        if (mNetworkIdToReplicationCmd.ContainsKey(_inNetworkId))
        {
            mNetworkIdToReplicationCmd.Remove(_inNetworkId);
        }
    }

}
