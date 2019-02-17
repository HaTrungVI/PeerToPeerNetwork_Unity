using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using NetworkTrungVI;

public class NetworkManagerServer : NetworkManager {
    public static NetworkManagerServer mInstance = null;
    private Dictionary<int, ClientProxy> mPlayerIdToClientsDic = new Dictionary<int, ClientProxy>();
    private Dictionary<EndPoint, ClientProxy> mEndpointToClientsDic = new Dictionary<EndPoint, ClientProxy>();
    private int mNewPlayerId;   // playerId quản lý các client đang tham gia trong trò chơi
    private int mNewNetworkId;  // networkId quản lý các gameobject đang tồn tại trong trò chơi
    private Server mServer;
    

    private float mDisconnectTimeout;
    public void SetDisconnectTimeout(float _inData)
    {
        mDisconnectTimeout = _inData;
    }
    private GameObject mLocalGameobject;
    public void SetLocalGameobject(GameObject _inGameobject)
    {
        mLocalGameobject = _inGameobject;
    }
    public GameObject GetLocalGameobject() { return mLocalGameobject; }

    public void SetServer(Server inServer)
    {
        mServer = inServer;
    }
    public NetworkManagerServer()
    {
        mNewNetworkId = 1;
        mNewPlayerId = 1;

    }

    public override void Disconnect()
    {
        HandleDisconect();
    }

    public override void HandleConnectionReset(EndPoint _inEndpoint)
    {
        if (mEndpointToClientsDic.ContainsKey(_inEndpoint))
        {
            HandleClientDisconnected(mEndpointToClientsDic[_inEndpoint]);
        }
    }


    public override void DestroyGameobject()
    {
        if (mLocalGameobject)
        {
            this.mServer.DestroyGameobject(mLocalGameobject);
            mLocalGameobject = null;
        }
    }

    public override void RespawnGameobject()
    {
        if (mLocalGameobject == null)
        {
            GameObject ob = this.mServer.InstanceGameobject(NetworkManagerServer.mInstance.mGameobjectToSpawn);
            
            SetLocalGameobject(ob);

            TransformSynch sync = ob.GetComponent<TransformSynch>();
            if (sync)
            {
                sync.SetIsLocalPlayer(true);
            }
        }
    }

    /// <summary>
    /// Khi 1 client nào đó đã out khỏi phòng chơi.
    /// </summary>
    private void HandleClientDisconnected(ClientProxy _clientProxy)
    {
        mEndpointToClientsDic.Remove(_clientProxy.GetEndpoint());
        mPlayerIdToClientsDic.Remove(_clientProxy.GetPlayerID());

        mServer.HandleLostClient(_clientProxy);
    }


    public void CheckClientsDisconnect()
    {
        Queue<ClientProxy> inTemp = new Queue<ClientProxy>();
        foreach (var pair in mEndpointToClientsDic)
        {
            float time = Time.time - pair.Value.GetLastTimeReceivedPacket();
            
            if (time > mDisconnectTimeout)
            {
                Debug.Log(pair.Value.GetPlayerID() + " : " + time);
                inTemp.Enqueue(pair.Value);
            }
        }
        while (inTemp.Count != 0)
        {
            HandleClientDisconnected(inTemp.Dequeue());
        }
    }

    /// <summary>
    /// Tiến trình xử lý 1 gói tin trên server
    /// </summary>
    /// <param name="_input"></param>
    /// <param name="_inFromAddress"></param>
    public override void ProcessPacket(ref NetworkTrungVI.InputMemoryBitStream _input, System.Net.EndPoint _inFromAddress)
    {
        // Nếu chưa tồn tại Endpoint từ Client mới gửi đến trong mEndpointToClientsDic.
        if (!mEndpointToClientsDic.ContainsKey(_inFromAddress))                 
        {
            HandlePacketFromNewClient(ref _input, _inFromAddress);
        }
        else // ngược lại thì xử lý tiếp gói tin.
        {
            ProcessPacket(mEndpointToClientsDic[_inFromAddress], ref _input);
        }
    }



    /// <summary>
    /// Xử lý gói tin khi client vừa join
    /// </summary>
    /// <param name="_input"></param>
    /// <param name="_inFromEndpoint"></param>
    private void HandlePacketFromNewClient(ref InputMemoryBitStream _input, EndPoint _inFromEndpoint)
    {
        int packetType = 0;
        _input.Read(ref packetType);
        if (packetType == NetworkManager.kHelloCC)         // Nếu packet do client gửi tới có tiêu đề kHelloCC
        {
            string name = "Client";
            _input.Read(ref name);
            // tạo 1 client proxy với Endpoint vừa đến, và tên mới nhận được.
            // Còn id player phải do server cấp phát. để tạo được sự đồng bộ vói các local player khác.
            ClientProxy nCl = new ClientProxy(_inFromEndpoint, name, GetNewIdPlayer());
            mEndpointToClientsDic[_inFromEndpoint] = nCl;
            mPlayerIdToClientsDic[nCl.GetPlayerID()] = nCl;

            // tới đây server có thể spawn 1 gameobject mới tượng trưng cho client vừa join.
            //
            
            
            //
            // gửi 1 packet phản hồi cho client mới join.
            SendWelcomePaket(nCl);

            // với clientProxy vừa tạo chúng ta cần thêm tất cả những client đã tồn tại
            // vào trong ReplicationManager của nó. Nhằm đảm bảo có thể cập nhật đủ trạng thái của tất cả client khác cho Client vừa tham gia.
            // @@
            foreach (var pair in mNetworkIdToGameobjectDic)
            {
                TransformSynch sync = pair.Value.GetComponent<TransformSynch>();
                if (sync != null)
                {
                    nCl.GetReplicationManagerServer().ReplicateCreate(pair.Key, sync.GetBitState());
                }
                else Debug.Log("Gameobject khong co TransformSynch de dong bo!");
            }
        }
    }

    private void SendWelcomePaket(ClientProxy _inCl)
    {
        GameObject gObject = mServer.InstanceGameobject(mGameobjectToSpawn);
        TransformSynch _sync = gObject.GetComponent<TransformSynch>();
        int idnet = -1;
        if (_sync)
        {
            _sync.SetIdPlayer(_inCl.GetPlayerID());
            _sync.SetIsLocalPlayer(false);
            idnet = _sync.GetIdNetwork();
        }

        OutputMemoryBitStream pack = new OutputMemoryBitStream(); //
        pack.Write(NetworkManager.kWelcomeCC);              // 
        pack.Write(_inCl.GetPlayerID());                    // Gửi cho client vừa rồi playerId mà server vừa tạo.
        pack.Write(idnet);
        SendPacket( pack, _inCl.GetEndpoint());
    }

    /// <summary>
    /// Xử lý những packet từ client đã tồn tại trong game.
    /// </summary>
    /// <param name="_inClientProxy"></param>
    /// <param name="_input"></param>
    private void ProcessPacket(ClientProxy _inClientProxy, ref InputMemoryBitStream _input)
    {
        //Debug.Log(_inClientProxy.GetPlayerID() + " received");
        _inClientProxy.UpdateLastPacketTime();              // Cập nhật thời gian nhận của packet cuối cùng nhận được.

        int packetType = 0;
        _input.Read(ref packetType);
        switch (packetType)
        {
            case NetworkManager.kHelloCC:                   // Trường hợp này xảy ra vd khi người chơi bị chết và muốn hồi sinh thì gửi cho server packet kHelloCC
                {
                    SendWelcomePaket(_inClientProxy);
                    break;
                }
            case NetworkManager.kUpdateStateCC:
                {
                    HandleUpdateStatePacket(_inClientProxy, ref _input);
                    break;
                }
            case NetworkManager.kDestroyGameobjcetCC:
                {
                    HandleDestroyGameobjectPacket(_inClientProxy, ref _input);
                    break;
                }
            case NetworkManager.kLeaveGameCC:
                {
                    HandleLeaveGamePacket(_inClientProxy, ref _input);
                    break;
                }
            case NetworkManager.kAppExitCC:
                {
                    HandleAppExitPacket(_inClientProxy, ref _input);
                    break;
                }
            case NetworkManager.kEmptyCC:
                {
                    break;
                }
            default: break;
        }
    }
    private void HandleAppExitPacket(ClientProxy _inClientProxy, ref InputMemoryBitStream _input)
    {
        int id_network = -1;
        _input.Read(ref id_network);
        int playerid = _inClientProxy.GetPlayerID();
        EndPoint endpoint = _inClientProxy.GetEndpoint();

        mEndpointToClientsDic.Remove(endpoint);
        mPlayerIdToClientsDic.Remove(playerid);

        if (id_network > 0)
        {
            GameObject gameobToDestroy = GetGameobjectByNetworkId(id_network);
            this.mServer.DestroyGameobject(gameobToDestroy);
        }
    }

    private void HandleDestroyGameobjectPacket(ClientProxy _inClientProxy, ref InputMemoryBitStream _input)
    {
        int id_network = -1;
        _input.Read(ref id_network);
        if(id_network > 0)
        {
            GameObject gameobToDestroy = GetGameobjectByNetworkId(id_network);
            this.mServer.DestroyGameobject(gameobToDestroy);
        }
    }

    private void HandleLeaveGamePacket(ClientProxy _inClientProxy,ref InputMemoryBitStream _input)
    {
        int id_network = -1;
        _input.Read(ref id_network);
        int playerid = _inClientProxy.GetPlayerID();
        EndPoint endpoint = _inClientProxy.GetEndpoint();

        mEndpointToClientsDic.Remove(endpoint);
        mPlayerIdToClientsDic.Remove(playerid);

        if (id_network > 0)
        {
            GameObject gameobToDestroy = GetGameobjectByNetworkId(id_network);
            this.mServer.DestroyGameobject(gameobToDestroy); 
        }

        SendOutgoingLeavePacket(endpoint);
    }

    private void SendOutgoingLeavePacket(EndPoint inEndpoint)
    {
        OutputMemoryBitStream output = new OutputMemoryBitStream();
        output.Write(kEndLeaveCC);
        SendPacket( output, inEndpoint);
    }

    /// <summary>
    /// Update trang thai gameobject cua cac clients da join
    /// </summary>
    /// <param name="_inClientProxy"></param>
    /// <param name="_input"></param>
    private void HandleUpdateStatePacket(ClientProxy _inClientProxy,ref InputMemoryBitStream _input)
    {
        // Viết xong chức năng gửi của client quay lại viết chức năng này.
        int networkId = -1;
        _input.Read(ref networkId);
        if (networkId > -1)
        {
            GameObject mGameobject = mNetworkIdToGameobjectDic[networkId];
            if (mGameobject)
            {
                TransformSynch sync = mGameobject.GetComponent<TransformSynch>();
                if (sync)
                {
                    sync.Read(ref _input);
                }
            }
        }
    }


    public static void ServerInit(int _inPort, Server inServer)
    {
        NetworkManagerServer.mInstance = new NetworkManagerServer();
        //NetworkManagerServer.mInstance.StartReceivePackets();
        NetworkManagerServer.mInstance.Init(_inPort);
        NetworkManagerServer.mInstance.SetServer(inServer);
    }


    /// <summary>
    /// tạm thời gửi tới tất cả client.
    /// </summary>
    public void SendOutgoingPacket()
    {
        foreach (var pair in mEndpointToClientsDic)
        {
            SendStatePacketToClient(pair.Value);
        }
    }

    void SendStatePacketToClient(ClientProxy _inClientProxy)
    {
        OutputMemoryBitStream output = new OutputMemoryBitStream();
        output.Write(NetworkManager.kStateCC);
        //

        // lấy trạng thái của thế giới trong ReplicationManager gửi cho client cần gửi.
        _inClientProxy.GetReplicationManagerServer().Write(ref output);
        SendPacket( output, _inClientProxy.GetEndpoint());
    }

    private int GetNewIdPlayer()
    {
        int re = mNewPlayerId++;
        return re;
    }
    private int GetNewIdNetwork()
    {
        int re = mNewNetworkId++;
        return re;
    }



    public GameObject RegisterAndReturn(GameObject _inGameobject)
    {
        RegisterGameobject(_inGameobject);
        return _inGameobject;
    }



    public void RegisterGameobject(GameObject _inGameobject)
    {
        int NewIdNetwork = GetNewIdNetwork();
        mNetworkIdToGameobjectDic[NewIdNetwork] = _inGameobject;
        int inDirtyState = 0;
        TransformSynch sync = _inGameobject.GetComponent<TransformSynch>();
        if (sync)
        {
            inDirtyState = sync.GetBitState();
            sync.SetIdNetwork(NewIdNetwork);
        }

        foreach (var pair in mEndpointToClientsDic)
        {
            pair.Value.GetReplicationManagerServer().ReplicateCreate(NewIdNetwork, inDirtyState);
        }
    }

    public void UnRegisterGameobject(GameObject _inGameobject)
    {
        TransformSynch sync = _inGameobject.GetComponent<TransformSynch>();
        if (sync)
        {
            int idNetwork = sync.GetIdNetwork();
            if(mNetworkIdToGameobjectDic.ContainsKey(idNetwork)) mNetworkIdToGameobjectDic.Remove(idNetwork);
            foreach (var pair in mEndpointToClientsDic)
            {
                pair.Value.GetReplicationManagerServer().ReplicateDestroy(idNetwork);
            }
        }
    }

    public void SetDirtyState(int _inNetworkId, int _inDirtyState)
    {
        foreach (var pair in mEndpointToClientsDic)
        {
            pair.Value.GetReplicationManagerServer().AddDirtyState(_inNetworkId, _inDirtyState);
        }
    }
}
