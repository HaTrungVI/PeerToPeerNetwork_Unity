using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using NetworkTrungVI;

public class NetworkManagerClient : NetworkManager {
    public const float kTimeBetweenSayHellos = 1f;
    public const float kTimeBetweenSendUpdateState = 0.033f;
    public static NetworkManagerClient mInstance = null;

    private ReplicationManagerClient mReplicationManagerClient = new ReplicationManagerClient();
    private Client mClient;
    private float mTimeReceivedPacket = 0;
    public void SetTimeReceivedPacket(float _inData) { mTimeReceivedPacket = _inData; }
    private float mServerDisconnectTimeout = 3f;
    public void SetServerDisconnectTimeout(float _inData) { mServerDisconnectTimeout = _inData; }
    public void SetClient(Client inClient)
    {
        mClient = inClient;
    }
    public Client GetClient() { return mClient; }
    public enum NetworkClientState
    {
        None = 0,
        SayingHello = 1,
        Welcomed = 2,
        EndGame = 3,
        LeaveGame = 4,
        AppExit = 5
    };
    private NetworkClientState mState;
    private EndPoint mServerEndpoint;
    private string mName = "TrungVI";
    private int mPlayerId;          
    private int mNetworkId;     // network id của local gameobject
    private float mLastTimeHello;
    private float mLastTimeState;

    public void SetNetworkId(int _in) { mNetworkId = _in; }
    public int GetNetworkId() { return mNetworkId; }

    private GameObject mGameobjectForThisClient = null;
    public void SetLocalGameobject(GameObject _inGameob) { mGameobjectForThisClient = _inGameob; }
    public GameObject GetLocalGameobject() { return mGameobjectForThisClient; }

    public void SetServerEndpoint(EndPoint inEp) { mServerEndpoint = inEp; }
    public EndPoint GetServerEndpoint() { return mServerEndpoint; }
    public void SetName(string inName) { mName = inName; }
    public static void StaticInint(EndPoint inServerEndpoint, string inName, Client inClient)
    {
        NetworkManagerClient.mInstance = new NetworkManagerClient();
        //NetworkManagerClient.mInstance.StartReceivePackets();
        
        NetworkManagerClient.mInstance.SetClient(inClient);
        NetworkManagerClient.mInstance.SetServerEndpoint(inServerEndpoint);
        NetworkManagerClient.mInstance.InitForClient(8888);
        NetworkManagerClient.mInstance.SetName(inName);
        NetworkManagerClient.mInstance.mState = NetworkClientState.SayingHello;
    }

    public override void Disconnect()
    {
        mState = NetworkClientState.LeaveGame;
        //HandleDisconect();
    }

    public override void DestroyGameobject()
    {
        if (mGameobjectForThisClient)
        {
            //RemoveGameobjectFromIdGameobjectDic(mGameobjectForThisClient);      // Xóa bỏ local gameobject khỏi từ điển ( networkid -> gameobject)
            //this.mClient.DestroyGameobject(mGameobjectForThisClient);           // Destroy gameobjcet
            //mGameobjectForThisClient = null;
            mState = NetworkClientState.EndGame;                                // Chuyển mState về trạng thái endgame để gửi thông báo cho server.
        }
    }

    public override void RespawnGameobject()
    {
        if (mGameobjectForThisClient == null)
        {
            mState = NetworkClientState.SayingHello;
        }
    }

    public override void ProcessPacket(ref InputMemoryBitStream _input, EndPoint _inFromAddress)
    {
        mServerEndpoint = _inFromAddress;
        mTimeReceivedPacket = Time.time;
        int packetType = 0;
        _input.Read(ref packetType);
        switch (packetType)
        {
            case NetworkManager.kWelcomeCC:
                {
                    HandleWelcomePacket(ref _input);
                    break;
                }
            case NetworkManager.kStateCC:
                {
                    HandleStatePacket(ref _input);
                    break;
                }
            case NetworkManager.kEndLeaveCC:
                {
                    HandleLeaveGamePacket();
                    break;
                }
            default: break;
        }
    }

    public void CheckServerDisconnect()
    {
        float time = Time.time - mTimeReceivedPacket;
        if (time > mServerDisconnectTimeout)
        {
            this.mClient.HandleServerDisconnect();
        }
    }

    private void HandleLeaveGamePacket()
    {
        HandleDisconect();
    }


    private void HandleWelcomePacket(ref InputMemoryBitStream _input)
    {
        if (mState == NetworkClientState.SayingHello)
        {
            int _id = 0, _idnetwork = -1 ;
            _input.Read(ref _id);
            _input.Read(ref _idnetwork);
            mPlayerId = _id;
            mNetworkId = _idnetwork;
            mState = NetworkClientState.Welcomed;
            //Debug.Log("Nguoi choi: " + mName + " ID: " + mPlayerId);
        }
    }
    private void HandleStatePacket(ref InputMemoryBitStream _input)
    {
        if (mState == NetworkClientState.Welcomed || mState == NetworkClientState.EndGame)
        {
            mReplicationManagerClient.ProcessPacket(ref _input);
        }
    }

    #region Send Process
    public void SendOutgoingPacket()
    {
        switch (mState)
        {
            case NetworkClientState.SayingHello:
                {
                    UpdateSendHelloPacket();
                    break;
                }
            case NetworkClientState.Welcomed:
                {
                    UpdateSendStatePacket();
                    break;
                }
            case NetworkClientState.EndGame:
                {
                    HandleSendEndGamePacket();
                    break;
                }
            case NetworkClientState.LeaveGame:
                {
                    HandleSendLeaveGamePacket();
                    break;
                }
            case NetworkClientState.AppExit:
                {
                    HandleSendAppExitPacket();
                    break;
                }
            default: break;
        }
    }

    private void UpdateSendHelloPacket()
    {
        float time = Time.time;
        if (time - mLastTimeHello > kTimeBetweenSayHellos)
        {
            Debug.Log("Say hello");
            SendHelloPacket();
            mLastTimeHello = time;
        }
    }


    private void SendHelloPacket()
    {
        OutputMemoryBitStream packet = new OutputMemoryBitStream();
        packet.Write(NetworkManager.kHelloCC);
        packet.Write(mName);
        SendPacket(packet, mServerEndpoint);
    }


    private void UpdateSendStatePacket()
    {
        float time = Time.time;
        if (time - mLastTimeState > kTimeBetweenSendUpdateState)
        {
            SendStatePacket();
            mLastTimeState = time;
        }
    }


    private void SendStatePacket()
    {
        if (mGameobjectForThisClient != null)
        {
            TransformSynch sync = mGameobjectForThisClient.GetComponent<TransformSynch>();
            if (sync != null)
            {
                // Nếu gameobject đã hoặc đang chuyển trạng thái. Ví dụ như di chuyển hoặc quay, thì tiến hành gửi...
                // ... gói cập nhật trạng thái.
                if (sync.HasUpdateState())
                {
                    //Debug.Log("OK");
                    OutputMemoryBitStream packet = new OutputMemoryBitStream();
                    packet.Write(NetworkManager.kUpdateStateCC);
                    packet.Write(sync.GetIdNetwork());
                    sync.Write(ref packet);

                    SendPacket( packet, mServerEndpoint);
                }
                else SendEmptyPacket();

            }
            else SendEmptyPacket();
        }
        else
        {
            SendEmptyPacket();
        }
    }


    private void SendEmptyPacket()
    {
        OutputMemoryBitStream packet = new OutputMemoryBitStream();
        packet.Write(NetworkManager.kEmptyCC);
        
        SendPacket( packet, mServerEndpoint);
    }

    /// <summary>
    /// EndGame ví dụ là khi nhân vật của người chơi bị chết và cần 1 
    /// khoảng thời gian hồi sinh lại...
    /// Chứ không phải là kết thúc game @@
    /// Ở trạng thái này, tuy đã destroy nhưng client (server) vẫn có thể nhận (gửi) packet
    /// và tiếp tục cập nhật trang thái thế giới.
    /// </summary>
    private void HandleSendEndGamePacket()
    {
        OutputMemoryBitStream packet = new OutputMemoryBitStream();
        packet.Write(NetworkManager.kDestroyGameobjcetCC);
        packet.Write(mNetworkId);

        SendPacket( packet, mServerEndpoint);
    }

    /// <summary>
    /// LeaveGame là khi 1 người chơi nào đó rời khỏi phòng.
    /// Ở trạng thái này client (server) không thể nhận (gửi) bất cứ packet nào khác
    /// và không thể tiếp tục cập nhật trạng thái thế giới.
    /// </summary>
    private void HandleSendLeaveGamePacket()
    {
        OutputMemoryBitStream packet = new OutputMemoryBitStream();
        packet.Write(NetworkManager.kLeaveGameCC);
        packet.Write(mNetworkId);

        SendPacket( packet, mServerEndpoint);
    }

    public void SendAppExitPacket()
    {
        mState = NetworkClientState.AppExit;
        HandleSendAppExitPacket();
    }
    private void HandleSendAppExitPacket()
    {
        OutputMemoryBitStream packet = new OutputMemoryBitStream();
        packet.Write(NetworkManager.kAppExitCC);
        packet.Write(mNetworkId);

        SendPacket(packet, mServerEndpoint);
    }

    #endregion
}
