//using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using NetworkTrungVI;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

/// <summary>
/// peer to peer
/// </summary>
public class NetworkManager_v2
{
    public const float mMaxTimeToStart = 3f;
    // các trạng thái mà master peer phản hồi lại cho 1 peer khi 1 peer muồn join room.
    public const int kWelcomeCC = 100;
    public const int kNotMasterPeer = 101;
    public const int kNotJoinableCC = 102;
    // 
    public const int kIntroCC = 103;
    public const int kHellocCC = 104;
    public const int kStartCC = 105;
    //
    public const int kTurnCC = 106;
    public const int kOutCC = 107;

    public enum NetworkState
    {
        None = 0,       //
        Hello = 1,      // Trạng thái mà peer chưa tham gia trò chơi, và đang cố gắng kết nối tới master peer để tham gia trò chơi.
        Lobby = 2,      // Đang trong phòng đợi
        Starting = 3,   // Tất cả người chơi đã sẵn sàng và đang chuẩn bị vào trò chơi.
        Playing = 4,    // Đang trong quá trình chơi game
        Delay = 5       // delay
    };
    public NetworkState mState = NetworkState.None;
    // 1 sub turn ~33 ms => 1 full turn ~100ms
    public static NetworkManager_v2 mInstance;
    
    public const int kSubTurnPerTurn = 3;
    private PeerToPeerManager mPeerToPeerManager = null;
    public PeerToPeerManager GetPeerToPeerManager() { return mPeerToPeerManager; }

    private bool mIsBlockingMode = true;
    private int mNewNetWorkID = 1;
    private int mPlayerId = 0;
    public int GetPlayerID() { return mPlayerId; }
    private int mPlayerCount = 0;
    public int GetPLayerCount() { return mPlayerCount; }
    private int mMaxPlayerCount = 8;
    private bool mIsMasterPeer = false;
    private int mHighestPlayerId = 0;
    private string mName = "Com ";
    private EndPoint mMasterPeerEndP;

    private int mSimulatedDropPacket = 0;
    private float mSimulatedLatency = 0f;

    private byte[] mData;

    private Dictionary<int, string> mPlayerIdToName = new Dictionary<int, string>();
    public Dictionary<int, string> GetPlayersList() { return mPlayerIdToName; }

    private Dictionary<int, EndPoint> mPlayerIdToEndpointDic= new Dictionary<int,EndPoint>();
    private Dictionary<EndPoint, int> mEndpointToPlayerIdDic= new Dictionary<EndPoint,int>();
    private Dictionary<int, GameObject> mNetworkIdToGameobjectDic = new Dictionary<int, GameObject>(); // quản lý tất cả gameobject tồn tại trong trò chơi


    //private List<Dictionary<int, TurnData>> mTurnData = new List<Dictionary<int, TurnData>>();
    private Dictionary<int, Dictionary<int, TurnData>> mTurnData = new Dictionary<int, Dictionary<int, TurnData>>();
    public int GetTurnNumber() { return mTurnNumber; }
    private int mTurnNumber = -2;
    private int mSubTurnNumber = 0;
    private Socket mSocket = null;
    private float CurrentTime = 0;
    private Queue<ReceivedPacket> mPacketQueue = new Queue<ReceivedPacket>();
    private bool Disconnected = false;
    private float mTimeToStart = -1f;
    
    // thoi gian de gui 1 hello packet
    private float mTimeHelloPacket = 0;
    private float mMaxTimeHelloPacket = 1f;
    public void SetMaxTimeHelloPacket(float mInTime) { mMaxTimeHelloPacket = mInTime; }

    // delay
    private bool mLockForDelay = false;

    private float _time = 0f;
    private float _deltatime = 0f;
    public bool GetLockForDelay() { return mLockForDelay; }

    public void SetSimulation(int _drop, float _latency)
    {
        mSimulatedDropPacket = _drop;
        mSimulatedLatency = _latency;
    }
    public NetworkManager_v2()
    {
        CurrentTime = Time.time;
        mTimeHelloPacket = Time.time;
        Disconnected = false;
        InputManager.StaticInit();
    }

    public static bool StaticInitAsMasterPeer(int _inPort, string _inName, PeerToPeerManager _p2p)
    {
        mInstance = new NetworkManager_v2();
        return mInstance.InitAsMasterPeer(_inPort, _inName, _p2p);
    }
    public static bool StaticInitAsPeer(EndPoint _addrMasterPeer, string _inName, PeerToPeerManager _p2p)
    {
        mInstance = new NetworkManager_v2();
        return mInstance.InitAsPeer(_addrMasterPeer, _inName, _p2p);
    }

    public bool InitAsMasterPeer(int _inPort, string _inName, PeerToPeerManager _p2p)
    {
        if (!InitSocket(_inPort, false))
        {
            return false;
        }
        mPeerToPeerManager = _p2p;
        mPeerToPeerManager.SetInLobby(true);
        mPlayerId = 1;
        mIsMasterPeer = true;
        mHighestPlayerId = mPlayerId;
        mName = _inName;
        mPlayerCount = 1;

        mState = NetworkState.Lobby;

        mPlayerIdToName.Add(mPlayerId, mName);
        return true;
    }
    public bool InitAsPeer(EndPoint _addrMasterPeer, string _inName, PeerToPeerManager _p2p)
    {
        if (!InitSocket(0, false))
        {
            return false;
        }
        mIsMasterPeer = false;
        mPeerToPeerManager = _p2p;
        mName = _inName;
        mMasterPeerEndP = _addrMasterPeer;
        mState = NetworkState.Hello;
        return true;
    }

    public bool InitSocket(int _inPort, bool _isBlockingMode = true)
    {
        mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, _inPort);
        EndPoint endPoint= (EndPoint)ipEndPoint;
        try
        {
            if (mSocket != null)
            {
                mSocket.Bind(endPoint);
                mSocket.Blocking = _isBlockingMode;
                mIsBlockingMode = _isBlockingMode;
                StartReceivingPacket();
            }
            else return false;
        }
        catch
        {
            return false;
        }
        return true;
    }

    public void ProcessIncomingPackets()
    {
        CurrentTime = Time.time;
        ProcessQueuedPackets();
    }

    private void ProcessQueuedPackets()
    {
        lock (mPacketQueue)
        {
            while (mPacketQueue.Count != 0)
            {
                ReceivedPacket packet = mPacketQueue.Peek();
                if (Time.time > packet.GetReceivedTime())
                {
                    InputMemoryBitStream input = packet.GetInputMemoryStream();
                    ProcessPacket(ref input, packet.GetFromAddressEndpoint());
                    mPacketQueue.Dequeue();
                }
                else break;
            }
        }
    }
    private void StartReceivingPacket()
    {
        if (mIsBlockingMode)
        {
            StartReceivingPacket_blocking();
        }
        else StartReceivingPacket_nonblocking();
    }

    public void HandleDisconect()
    {
        mSocket.Close();
        mSocket = null;
    }

    #region Send packet
    private void SendPacket(OutputMemoryBitStream output, EndPoint to)
    {
        if (mIsBlockingMode) Send_blocking(output, to);
        else Send_nonblocking(output, to);
    }
    private void Send_blocking(OutputMemoryBitStream output, EndPoint to)
    {
        Thread thread = new Thread(() =>
        {
            this.Send_thread(output, to);
        });
        thread.IsBackground = true;
        thread.Start();
    }
    private void Send_thread(OutputMemoryBitStream output, EndPoint to)
    {
        byte[] newData = new byte[output.GetByteLength()];
        Array.Copy(output.GetBuffer(), newData, output.GetByteLength());
        mSocket.SendTo(newData, to);
    }
    private void Send_nonblocking(OutputMemoryBitStream output, EndPoint to)
    {
        if (mSocket == null) return;
        if (Disconnected) return;
        try
        {
            mSocket.BeginSendTo(output.GetBuffer(), 0, output.GetByteLength(), SocketFlags.None, to, new AsyncCallback(sendto_callback), null);
        }
        catch (ObjectDisposedException ex)
        {
            Debug.Log("socket has been closed");
            return;
        }
    }

    private void sendto_callback(IAsyncResult ar)
    {
        int b = mSocket.EndSendTo(ar);
    }

    public void SendDisconnectPacket()
    {
        OutputMemoryBitStream output = new OutputMemoryBitStream();
        output.Write(kOutCC);
        output.Write(mTurnNumber+2);
        output.Write(mPlayerId);
        foreach (var pair in mPlayerIdToEndpointDic)
        {
            SendPacket(output, pair.Value);
        }
        
    }

#endregion

    #region blocking
    private void StartReceivingPacket_blocking()
    {
        Thread thread = new Thread(this.ReceivingLoop);
        thread.IsBackground = true;
        thread.Start();
    }

    private void ReceivingLoop()
    {
        EndPoint Endp = null;
        while (!Disconnected)
        {

            mData = new byte[1020];
            Debug.Log("Dang doi!");
            int _byteCount = mSocket.ReceiveFrom(mData, ref Endp);
            if (_byteCount > 0)
            {
                InputMemoryBitStream input = new InputMemoryBitStream(mData, _byteCount * 8);
                System.Random rand = new System.Random();
                int nextInt = rand.Next(0, 100);
                // Mô phỏng mất gói tin
                if (nextInt >= mSimulatedDropPacket)
                {
                    // Mô phỏng độ trễ
                    float receivedTime = CurrentTime + mSimulatedLatency;
                    ReceivedPacket packet = new ReceivedPacket(ref input, Endp, receivedTime);
                    // tránh tranh chấp dữ liệu
                    lock (mPacketQueue)
                    {
                        mPacketQueue.Enqueue(packet);
                    }
                }
            }

        }
        Debug.Log("Disconnected!");
    }

    #endregion

    #region Non blocking
    private void StartReceivingPacket_nonblocking()
    {
        try
        {
            EndPoint endp = (EndPoint)(new IPEndPoint(IPAddress.Any, 0));
            mData = new byte[1020];
            mSocket.BeginReceiveFrom(mData, 0, mData.Length, SocketFlags.None, ref endp, new AsyncCallback(ReceiveFromCallback), null);
        }
        catch (ObjectDisposedException ex)
        {
            Debug.Log("Socket has been closed!");
            return;
        }
    }

    private void ReceiveFromCallback(IAsyncResult ar)
    {
        try
        {
            EndPoint endp = (EndPoint)(new IPEndPoint(IPAddress.Any, 0));
            int _byteCount = mSocket.EndReceiveFrom(ar, ref endp);
            if (_byteCount > 0 && endp != null)
            {
                InputMemoryBitStream input = new InputMemoryBitStream(mData, _byteCount * 8);
                System.Random rand = new System.Random();
                int nextInt = rand.Next(0, 100);
                if (nextInt >= mSimulatedDropPacket)
                {
                    // Mô phỏng độ trễ
                    float receivedTime = CurrentTime + mSimulatedLatency;
                    ReceivedPacket packet = new ReceivedPacket(ref input, endp, receivedTime);
                    // tránh tranh chấp dữ liệu
                    lock (mPacketQueue)
                    {
                        mPacketQueue.Enqueue(packet);
                    }
                }
            }

            StartReceivingPacket_nonblocking();
        }
        catch(ObjectDisposedException ex)
        {
            Debug.Log("end receive");
            return;
        }
    }
    #endregion

    #region Process packets (quá trình nhận và xử lý dữ liệu)

    private void ProcessPacket(ref InputMemoryBitStream input, EndPoint _inEndpoint)
    {
        switch (mState)
        {
            case NetworkState.Hello:
                {
                    ProcessPacketHello(ref input, _inEndpoint);
                    break;
                }
            case NetworkState.Lobby:
                {
                    ProcessPacketLobby(ref input, _inEndpoint);
                    break;
                }
            case NetworkState.Delay:
                {
                    ProcessPacketDelay(ref input, _inEndpoint);
                    break;
                }
            case NetworkState.Playing:
                {
                    ProcessPacketPlaying(ref input, _inEndpoint);
                    break;
                }
            default: break;
        }
    }

    private void ProcessPacketHello(ref InputMemoryBitStream input, EndPoint _inEndPoint)
    {
        int type = 0;
        input.Read(ref type);
        switch (type)
        {
            case kNotMasterPeer:
                {
                    HandleNotMasterPeer(ref input);
                    break;
                }
            case kWelcomeCC:
                {
                    mMasterPeerEndP = _inEndPoint;
                    HandleWelcomePacket(ref input);
                    break;
                }
            case kNotJoinableCC:        // trường hợp này xảy ra khi game đã bắt đầu hoặc phòng đã đầy, nên người chơi không thể join.
                {
                    break;
                }
            default: break;
        }
    }


    /// <summary>
    /// Xảy ra khi chúng ta gửi hello packet tới 1 peer mà không phải master
    /// Gói nhận về sẽ chứa 1 địa chỉ ip của master peer
    /// </summary>
    /// <param name="input"></param>
    private void HandleNotMasterPeer(ref InputMemoryBitStream input)
    {
        ReadIPv4(ref input);            // đọc địa chỉ của master peer và lưu lại
        UpdateSayingHello(true);        // gứi lại hello packet cho master peer ngay bây giờ.
    }

    /// <summary>
    /// Trường hợp xảy ra khi được chấp nhận kết nối từ master peer
    /// </summary>
    /// <param name="input"></param>
    private void HandleWelcomePacket(ref InputMemoryBitStream input)
    {
        // đọc player id được cấp phát
        int playerid = 0, masterPeerPlayerid = 0;
        input.Read(ref playerid);
        UpdateHighestPlayerId(playerid);
        mPlayerId = playerid;

        mPlayerIdToName.Add(mPlayerId, mName);

        // bây giờ đọc player id của master peer.
        input.Read(ref masterPeerPlayerid);
        UpdateHighestPlayerId(masterPeerPlayerid);
        mPlayerIdToEndpointDic.Add(masterPeerPlayerid, mMasterPeerEndP);
        mEndpointToPlayerIdDic.Add(mMasterPeerEndP, masterPeerPlayerid);

        // tiếp theo đọc thông tin của tất cả người chơi còn lại.
        int playerCount = 0;
        input.Read(ref playerCount);
        mPlayerCount = playerCount;
        EndPoint Endp = null;
        for (int i = 0; i < playerCount - 1; i++)
        {
            input.Read(ref playerid);
            UpdateHighestPlayerId(playerid);

            ReadIPv4(ref input, ref Endp);
            mPlayerIdToEndpointDic.Add(playerid, Endp);
            mEndpointToPlayerIdDic.Add(Endp, playerid);
        }

        // Đọc tên của tất cả người chơi đã tham gia trò chơi
        string name = " ";
        for (int i = 0; i < playerCount; i++)
        {
            input.Read(ref playerid);
            input.Read(ref name);
            mPlayerIdToName.Add(playerid, name);
        }

        mPlayerCount++;

        // Gửi 1 gói thông báo cho các player khác là tôi đã tham gia trò chơi
        OutputMemoryBitStream output = new OutputMemoryBitStream();
        output.Write(kIntroCC);
        output.Write(mPlayerId);
        output.Write(mName);

        foreach (var pair in mPlayerIdToEndpointDic)
        {
            SendPacket(output, pair.Value);
        }

        mPeerToPeerManager.SetInLobby(true);
        // đã vào phòng đợi để sẵn sàng tham gia trò chơi
        mState = NetworkState.Lobby;
    }

    /// <summary>
    /// xử lý gói tin nhận được nếu người chơi đang trong phòng đợi
    /// </summary>
    /// <param name="input"></param>
    /// <param name="_inEndPoint"></param>
    private void ProcessPacketLobby(ref InputMemoryBitStream input, EndPoint _inEndPoint)
    {
        int packetType = 0;
        input.Read(ref packetType);
        switch (packetType)
        {
            case kHellocCC:
                {
                    HandleHelloPacket(ref input, _inEndPoint);
                    break;
                }
            case kIntroCC:
                {
                    HandleIntroPacket(ref input, _inEndPoint);
                    break;
                }
            case kStartCC:
                {
                    HandleStartPacket(ref input, _inEndPoint);
                    break;
                }
            case kOutCC:
                {
                    HandleDisconnectPacket(ref input, _inEndPoint);
                    break;
                }
            default: break;
        }
    }

    private void HandleHelloPacket(ref InputMemoryBitStream input, EndPoint _inEndpoint)
    {
        if (mEndpointToPlayerIdDic.ContainsKey(_inEndpoint))
        {
            return;
        }

        if (mPlayerCount >= 4)
        {
            OutputMemoryBitStream output = new OutputMemoryBitStream();
            output.Write(kNotJoinableCC);
            SendPacket(output, _inEndpoint);
            return;
        }

        if (mIsMasterPeer)
        {
            string name = " ";
            input.Read(ref name);
            // Nếu máy nhận được là 1 master peer=> cấp phát player id cho máy gửi để nó có thêm tham gia trò chơi
            OutputMemoryBitStream output = new OutputMemoryBitStream();
            output.Write(kWelcomeCC);
            mHighestPlayerId++;
            output.Write(mHighestPlayerId);

            // ghi thông tin của master peer(người chơi chính = chủ phòng chơi) và các player khác đã tham gia
            output.Write(mPlayerId);

            output.Write(mPlayerCount);
            // ghi player id + endpoint (ip address) của tất cả người chơi đã tham gia cho người chơi mới
            foreach (var pair in mPlayerIdToEndpointDic)
            {
                output.Write(pair.Key);
                WriteIPv4(ref output, pair.Value);
            }
            // gừi tên của tất cá người chơi cho người chơi mới
            foreach (var pair in mPlayerIdToName)
            {
                output.Write(pair.Key);
                output.Write(pair.Value);
            }

            SendPacket(output, _inEndpoint);

            // lưu thông tin người chơi mới.
            mPlayerCount++;
            mPlayerIdToName.Add(mHighestPlayerId, name);
            mPlayerIdToEndpointDic.Add(mHighestPlayerId, _inEndpoint);
            mEndpointToPlayerIdDic.Add(_inEndpoint, mHighestPlayerId);
        }
        else
        {
            // nếu người nhận được không phải là master peer
            // tiến hành gửi lại địa chỉ của master peer thực sự cho người gửi
            OutputMemoryBitStream output = new OutputMemoryBitStream();
            output.Write(kNotMasterPeer);
            WriteIPv4(ref output);
            SendPacket(output, _inEndpoint);
        }
    }

    private void HandleIntroPacket(ref InputMemoryBitStream input, EndPoint _inEndpoint)
    {
        if (!mIsMasterPeer)
        {
            int playerid = 0;
            string name = " ";

            input.Read(ref playerid);
            input.Read(ref name);

            mPlayerCount++;
            UpdateHighestPlayerId(mPlayerId);
            mPlayerIdToName.Add(playerid, name);
            mEndpointToPlayerIdDic.Add(_inEndpoint, playerid);
            mPlayerIdToEndpointDic.Add(playerid, _inEndpoint);
        }
    }

    private void HandleStartPacket(ref InputMemoryBitStream input, EndPoint _inEndpoint)
    {
        if (mMasterPeerEndP.Equals(_inEndpoint))
        {
            int seed = 0;
            input.Read(ref seed);
            CustomRandom.mInstance.Seed(seed);

            mState = NetworkState.Starting;
            // Ở đây chính xác là phải trừ đi 1/2 RTT (round time trip), tức là bỏ qua khoảng thời gian gửi
            // nhưng mình lại trừ tạm đi 1 khoảng là delta time.
            mTimeToStart = mMaxTimeToStart - _deltatime;
        }
        
    }

    private void ProcessPacketDelay(ref InputMemoryBitStream input, EndPoint _inEndPoint)
    {
        // nếu người chơi trong trang thái delay,
        // tiếp tục nhập các turn packet đến, và kiểm đã nhận đủ các packet tất cả người chơi trong turn hiện tại hay chưa
        // nếu rồi thì chuyển sang turn mới
        int packetType = -1;
        input.Read(ref packetType);
        if (packetType == kTurnCC)
        {
            HandleTurnPacket(ref input, _inEndPoint);
            TryAdvanceTurn();
        }
    }

    private void ProcessPacketPlaying(ref InputMemoryBitStream input, EndPoint _inEndPoint)
    {
        int packetType = 0;
        input.Read(ref packetType);
        switch (packetType)
        {
            case kTurnCC:
                {
                    HandleTurnPacket(ref input, _inEndPoint);
                    break;
                }
            case kOutCC:
                {
                    HandleDisconnectPacket(ref input, _inEndPoint);
                    break;
                }
            default: break;
        }
    }

    private void HandleTurnPacket(ref InputMemoryBitStream input, EndPoint _inEndpoint)
    {
        if (mEndpointToPlayerIdDic.ContainsKey(_inEndpoint))
        {
            int id = mEndpointToPlayerIdDic[_inEndpoint];
            int playerid = -1;
            int turnNum = -1;
            input.Read(ref turnNum);
            input.Read(ref playerid);

            if (id != playerid)
            {
                return;
            }
            TurnData data = new TurnData();
            data.Read(ref input);
            //Debug.Log(turnNum + " " + playerid + " " + data.GetCommandList().GetCount());
            if (mTurnData.ContainsKey(turnNum))
            {
                try
                {
                    mTurnData[turnNum].Add(playerid, data);
                }
                catch (ArgumentException ex)
                {
                    mTurnData[turnNum][playerid] = data;
                }
            }
            else
            {
                mTurnData.Add(turnNum, new Dictionary<int, TurnData>());
                mTurnData[turnNum].Add(playerid, data);
            }
            //try
            //{
            //    mTurnData[turnNum].Add(playerid, data);
            //}
            //catch (ArgumentException)
            //{
            //    mTurnData[turnNum][playerid] = data;
            //}
        }
    }

    
    private void HandleDisconnectPacket(ref InputMemoryBitStream input, EndPoint _inEndpoint)
    {
        if (mEndpointToPlayerIdDic.ContainsKey(_inEndpoint))
        {
            int playerid = mEndpointToPlayerIdDic[_inEndpoint];
            mEndpointToPlayerIdDic.Remove(_inEndpoint);
            mPlayerIdToEndpointDic.Remove(playerid);
            mPlayerIdToName.Remove(playerid);
            mPlayerCount--;
            int mTurnDisconnect = -1;
            input.Read(ref mTurnDisconnect);
            int id = -1;
            input.Read(ref id);

            if (id != -1)
            {
                for (int i = mTurnNumber + 1; i <= mTurnDisconnect; i++)
                {
                    if (mTurnData.ContainsKey(i))
                    {
                        if (mTurnData[i].ContainsKey(id))
                        {
                            mTurnData[i].Remove(id);
                        }
                    }
                    
                }
            }
        }
    }
    #endregion
    
    #region Send process (quá trình gửi các gói update của mỗi peer)
    
    public void SendOutgoingPackets()
    {
        CheckDeltaTime();
        switch (mState)
        {
            case NetworkState.Hello:
                {
                    UpdateSayingHello(false);
                    break;
                }
            case NetworkState.Starting:
                {
                    UpdateStarting();
                    break;
                }
            case NetworkState.Playing:
                {
                    UpdateSendTurnPacket();
                    break;
                }
            default: break;
        }
    }

    void CheckDeltaTime()
    {
        _deltatime = Time.time - _time;
        _time = Time.time;
    }

    private void UpdateSayingHello(bool now)
    {
        float time = Time.time;
        if (now || time - mTimeHelloPacket > mMaxTimeHelloPacket)
        {
            SendHelloPacket();
            mTimeHelloPacket = time;
        }
    }

    private void SendHelloPacket()
    {
        OutputMemoryBitStream output = new OutputMemoryBitStream();
        output.Write(kHellocCC);
        output.Write(mName);
        SendPacket(output, mMasterPeerEndP);
    }
    
    private void UpdateStarting()
    {
        mTimeToStart -= _deltatime;
        
        if (mTimeToStart <= 0f)
        {
            EnterGame();
        }
    }

    private void EnterGame()
    {
        mState = NetworkState.Playing;
        //
        //...
        //mPeerToPeerManager.StartGameSceneTest.SetActive(true);
    }

    private void UpdateSendTurnPacket()
    {
        mSubTurnNumber++;
        if (mSubTurnNumber == kSubTurnPerTurn)
        {
            TurnData data = new TurnData(mPlayerId, CustomRandom.mInstance.GetValue_v2(), InputManager.mIntance.GetCommandList(), ComputeCRC());

            OutputMemoryBitStream output = new OutputMemoryBitStream();
            output.Write(kTurnCC);
            output.Write(mTurnNumber + 2);
            output.Write(mPlayerId);
            data.Write(ref output);

            foreach (var pair in mPlayerIdToEndpointDic)
            {
                SendPacket(output, pair.Value);
            }
            if (mTurnData.ContainsKey(mTurnNumber + 2))
            {
                try
                {
                    mTurnData[mTurnNumber + 2].Add(mPlayerId, data);
                }
                catch (ArgumentException ex)
                {
                    mTurnData[mTurnNumber + 2][mPlayerId] = data;
                }
            }
            else
            {
                mTurnData.Add(mTurnNumber + 2, new Dictionary<int, TurnData>());
                mTurnData[mTurnNumber + 2].Add(mPlayerId, data);
            }
            //Debug.Log("send " + (mTurnNumber + 2) + " " + mPlayerId);
            //Debug.Log((mTurnNumber + 2) + " " + data.GetCommandList().GetCount());
            InputManager.mIntance.NewCommandList();

            if (mTurnNumber >= 0)
            {
                TryAdvanceTurn();
            }
            else
            {
                mTurnNumber++;
                mSubTurnNumber = 0;
            }
        }
    }

    /// <summary>
    /// Không nên tự ý sử dụng thuộc tính này để tránh bị lỗi.
    /// </summary>
    public bool delay_game = false;
    private void TryAdvanceTurn()
    {
        //Debug.Log("try " + (mTurnNumber+1) + " " + mTurnData[mTurnNumber + 1].Count + " " +mPlayerCount);
        //if (mTurnDisconnect != -1)
        //{
        //    Debug.Log(mTurnDisconnect + " " + (mTurnNumber + 1) + " " + mPlayerCount + " " + mTurnData[mTurnNumber + 1].Count + " " + mTurnData[mTurnNumber+2].Count);
        //}        
        if (mTurnData[mTurnNumber + 1].Count == mPlayerCount)
        {
            if (mState == NetworkState.Delay)
            {
                // trong trạng thái delay các thao tác của người chơi sẽ bị bỏ qua
                // điều này có thể thấy trong các trò chơi thực tế, vd như trong game AOE khi bị lag người chơi sẽ bị "đứng"
                // và các thao tác điều khiển sẽ không được thực thi.
                InputManager.mIntance.ClearCommandList();
                mState = NetworkState.Playing;
                // Đợi 100 ms (thời gian hoàn thành 1 turn) để cho người chơi chậm hơn có thể đuổi kịp turn hiện tại.
                // Theo như mình tính được với cách làm này thời gian hoàn thành 1 turn trong khoảng [0.09ms, 0.15ms] 
                //Delay_Game(100f);
                delay_game = true;
                return;
            }

            ProcessCommands();
        }
        else
        {
            // đợi tới khi nào nhận đử dữ liệu của turn hiên tại của tất cả người chơi.
            Debug.Log("delay");
            mState = NetworkState.Delay;
        }
    }

    public void ProcessCommands()
    {
        mTurnNumber++;
        mSubTurnNumber = 0;

        if (CheckSync(mTurnData[mTurnNumber]))
        {
            foreach (var pair in mTurnData[mTurnNumber])
            {
                //Debug.Log(mTurnNumber + " " + pair.Value.GetCommandList().GetCount() + " player id " + pair.Key);
                pair.Value.GetCommandList().ProcessCommands(pair.Key);
            }
        }
        else
        {
            Debug.Log("Loi dong bo tro choi!");
            return;
        }
    }

    #endregion


    public bool TryStartGame()
    {
        if (mIsMasterPeer && mState == NetworkState.Lobby)
        {
            OutputMemoryBitStream output = new OutputMemoryBitStream();
            output.Write(kStartCC);

            int seed = CustomRandom.mInstance.GetValue_v2();
            CustomRandom.mInstance.Seed(seed);
            output.Write(seed);

            foreach (var pair in mPlayerIdToEndpointDic)
            {
                SendPacket(output, pair.Value);
            }

            mTimeToStart = mMaxTimeToStart;
            mState = NetworkState.Starting;
            return true;
        }
        else return false;
    }

    /// <summary>
    /// Kiểm tra đồng bộ hóa bằng cách:
    /// + Kiểm tra sự đồng nhất của crc: mỗi số crc được sinh ra bằng cách ghi liên tiếp các trạng thái cần đồng
    /// bộ vào 1 mảng byte. Sau đó dựa vào mảng byte vừa tạo phát sinh số crc băng thuật toán crc32
    /// + Kiểm tra sự đồng nhất của random value: tại mỗi turn data của mỗi người chơi khác nhau sẽ sinh các số
    /// giống nhau, nếu seed được truyền vào giống nhau.
    /// </summary>
    /// <param name="_inTurnDataDic"></param>
    /// <returns></returns>
    private bool CheckSync(Dictionary<int, TurnData> _inTurnDataDic)
    {
        bool first = true;
        int mRandomValue = 0;
        UInt32 mCrc = 0;
        foreach (var pair in _inTurnDataDic)
        {
            if (first)
            {
                mRandomValue = pair.Value.GetRandomValue();
                mCrc = pair.Value.GetCRC();
                first = false;
            }
            else
            {
                if (pair.Value.GetCRC() != mCrc)
                {
                    // nếu trạng thái của thế giới game trên mỗi peer đồng nhất
                    // số crc sẽ giống nhau, và ngược lại.
                    return false;
                }
                if (pair.Value.GetRandomValue() != mRandomValue)
                {
                    return false;
                }
            }
        }
        return true;
    }

    
    private void Delay_Game(float _miliSecond)
    {
        mLockForDelay = true;
        float second = _miliSecond / 1000f;
        float mtime = Time.time;
        while (Time.time - mtime < second) { }
        mLockForDelay = false;
    }

    private void UpdateHighestPlayerId(int _inPlayerId)
    {
        mHighestPlayerId = Mathf.Max(mHighestPlayerId, _inPlayerId);
    }

    #region read & write ip address
    /// <summary>
    /// đọc địa chỉ master peer
    /// </summary>
    /// <param name="input"></param>
    private void ReadIPv4(ref InputMemoryBitStream input)
    {
        string ip = "";
        int port = 0;
        input.Read(ref ip);
        input.Read(ref port);
        try
        {
            mMasterPeerEndP = (EndPoint)(new IPEndPoint(IPAddress.Parse(ip), port));
        }
        catch
        {
            Debug.Log("error");
            return;
        }
    }

    /// <summary>
    /// đọc địa chỉ ip
    /// </summary>
    /// <param name="input"></param>
    /// <param name="_inEndpoint"></param>
    private void ReadIPv4(ref InputMemoryBitStream input, ref EndPoint _inEndpoint)
    {
        string ip = "";
        int port = 0;
        input.Read(ref ip);
        input.Read(ref port);
        try
        {
            _inEndpoint = (EndPoint)(new IPEndPoint(IPAddress.Parse(ip), port));
        }
        catch
        {
            Debug.Log("error");
            return;
        }
    }

    private void WriteIPv4(ref OutputMemoryBitStream output)
    {
        int port = ((IPEndPoint)mMasterPeerEndP).Port;
        string ip = ((IPEndPoint)mMasterPeerEndP).Address.ToString();
        output.Write(ip);
        output.Write(port);
    }

    private void WriteIPv4(ref OutputMemoryBitStream output, EndPoint _inEndpoint)
    {
        int port = ((IPEndPoint)_inEndpoint).Port;
        string ip = ((IPEndPoint)_inEndpoint).Address.ToString();
        output.Write(ip);
        output.Write(port);
    }
    #endregion

    /// <summary>
    /// tính crc với mục đích kiểm tra sự đồng bộ hóa
    /// </summary>
    private UInt32 ComputeCRC()
    {
        OutputMemoryBitStream output = new OutputMemoryBitStream();

        foreach (var pair in mNetworkIdToGameobjectDic)
        {
            NetworkObject mObject= pair.Value.GetComponent<NetworkObject>();
            if (mObject != null)
            {
                mObject.WriteForCrc(ref output);
            }
        }

        UInt32 re = Crc32.Compute(output.GetBuffer(), 0, output.GetByteLength());

        return re;
        
    }

    #region network id -> gameobject
    public GameObject GetGameobjectByNetworkId(int _inNetworkId)
    {
        if (mNetworkIdToGameobjectDic.ContainsKey(_inNetworkId))
        {
            return mNetworkIdToGameobjectDic[_inNetworkId];
        }
        else return null;
    }

    public void RegisterGameobject(GameObject _inGameobject)
    {
        int newNetID = GetNewNetworkID();
        NetworkObject netob = _inGameobject.GetComponent<NetworkObject>();
        if (netob != null)
        {
            netob.SetNetworkId(newNetID);
        }
        mNetworkIdToGameobjectDic.Add(newNetID, _inGameobject);
    }

    public void UnRegisterGameobject(GameObject _inGameobject)
    {
        NetworkObject netob = _inGameobject.GetComponent<NetworkObject>();
        if (netob != null)
        {
            int netid = netob.GetNetWorkId();
            if (mNetworkIdToGameobjectDic.ContainsKey(netid)) mNetworkIdToGameobjectDic.Remove(netid);
        }
    }

    public int GetNewNetworkID()
    {
        return mNewNetWorkID++;
    }
    
    #endregion
}
