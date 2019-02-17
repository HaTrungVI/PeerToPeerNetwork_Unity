using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
/// <summary>
/// Mỗi người chơi đều sở hữa 1 danh sách Replication command tương ứng với danh sách người chơi đang tham gia trò chơi.
/// Lóp này như 1 lớp "ra lệnh" cho các lớp khác( ví dụ như các lớp chứa quá trình tuần tự dữ liệu) là gameobject sẽ được đồng bộ như thế nào.
/// </summary>
public class ReplicationCommand {
    public enum ReplicationAction
    {
        RA_Create = 0,
        RA_Update = 1,
        RA_Destroy = 2,
        RA_RPC = 3,
        RA_Max = 4
    };
    private ReplicationAction mAction;      // cac trang thai de dong bo hoa 1 game object.
    private int mDirtyState;                // co the hieu day la nhung trang thai ma chung ta muon update. Duoc ghi duoi dang cac bit
                                            // vi du nhu position, hoac quaternion,...
    public ReplicationCommand(int _inDirtyState)
    {
        mAction = ReplicationAction.RA_Create;
        mDirtyState = _inDirtyState;
    }
    public void SetAction(ReplicationAction _inAction) { mAction = _inAction; }
    public ReplicationAction GetAction() { return mAction; }

    public bool HasDirtyState() { return (mAction == ReplicationAction.RA_Destroy) || (mDirtyState != 0); }

    /// <summary>
    /// Xoa bo nhung bit 1 giong nhat cua _inDirtyState va mDirtyState.
    /// </summary>
    /// <param name="_inDirtyState"></param>
    public void ClearDirtyState(int _inDirtyState)
    {
        mDirtyState &= ~_inDirtyState;

        if (mAction == ReplicationAction.RA_Destroy)
        {
            mAction = ReplicationAction.RA_Update;
        }
    }

    public int GetDirtyState() { return mDirtyState; }
    public void AddDirtyState(int _inDirtyState)
    {
        mDirtyState |= _inDirtyState;
    }
    public void HandleCreateAckd()
    {
        if (mAction == ReplicationAction.RA_Create) mAction = ReplicationAction.RA_Update;
    }

    public void SetDestroy() { mAction = ReplicationAction.RA_Destroy; }
}
   