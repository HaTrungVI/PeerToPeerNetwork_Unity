using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using NetworkTrungVI;
using System.Threading;

/// <summary>
/// client server
/// </summary>
public class NetworkManager {

    public const int kHelloCC = 101;
    public const int kWelcomeCC = 102;
    public const int kStateCC = 103;
    public const int kUpdateStateCC = 104;
    public const int kDestroyGameobjcetCC = 105;
    public const int kLeaveGameCC = 106;
    public const int kEndLeaveCC = 107;
    public const int kEmptyCC = 108;
    public const int kAppExitCC = 109;

    protected bool Disconnected = false;
    public bool GetDisconnected() { return Disconnected; }
    public void SetDisconnected(bool _inData) { Disconnected = _inData; }

    // so packet co the nhan trong 1 Frame;
    public int kMaxPacketsPerFrameCount = 10;

    public GameObject mGameobjectToSpawn;

    private float CurrentTime = 0;
    
    // Neu khong can mo phong thi khong nen set gia tri cho cac gia tri mo phong
    private int mSimulatedDropPacket = 0;
    private float mSimulatedLatency = 0;

    private byte[] mData = new byte[1020];

    #region test
    private Queue<ReceivedPacket> mTempQueued = new Queue<ReceivedPacket>();
    private bool mCanTransmit = true;
    #endregion

    public void SetSimulatedLatency(float _inParam)
    {
        mSimulatedLatency = _inParam;
    }
    public void SetSimulatedDropPacket(int _inParam)
    {
        mSimulatedDropPacket = _inParam;
    }

    protected Dictionary<int, GameObject> mNetworkIdToGameobjectDic = new Dictionary<int,GameObject>();
    public Socket GetSocket() { return mSocket; }
    private Socket mSocket = null;
    private int mPort;
    private Queue<ReceivedPacket> mPacketQueue = new Queue<ReceivedPacket>();

    public virtual void ProcessPacket(ref InputMemoryBitStream _input, EndPoint _inFromAddress) { return; }
    public virtual void HandleConnectionReset(EndPoint _inEndpoint) { return; }
    public virtual void DestroyGameobject() { return; }
    public virtual void RespawnGameobject() { return; }
    public virtual void Disconnect() { return; }

    ~NetworkManager()
    {
        HandleDisconect();
    }

    /// <summary>
    /// Thuong duoc su dung cho server
    /// </summary>
    /// <param name="_inPort"></param>
    /// <returns></returns>
    public bool Init(int _inPort)
    {
        mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPAddress IpAddr = IPAddress.Any;
        IPEndPoint IpEndp = new IPEndPoint(IpAddr, _inPort);
        try
        {
            mSocket.Bind(IpEndp);
        }
        catch
        {
            Debug.Log("Loi Bind Socket!");
            return false;
        }

        if (mSocket == null) return false;

        try
        {
            mSocket.Blocking = true;
        }
        catch
        {
            Debug.Log("Khong the thiet lap socket ve trang thai Non-Blocking!");
            return false;
        }
        
        mPort = _inPort;
        Debug.Log("server da duoc tao tai cong: " + _inPort);
        //StartReceivingUseBlockingMode();
        return true;
    }

    private bool first = false;
    public void CheckReceiveProcess()
    {
        if (!Disconnected && mSocket != null)
        {
            if (!mSocket.Connected && !first)
            {
                
                Debug.Log("Start");
                first = true;
                StartReceivePackets();
            }
            if(mSocket.Connected) 
            {
                first = false;
            }
        }
    }


    public void HandleDisconect()
    {
        if (!Disconnected)
        {
            Debug.Log("close socket");
            Disconnected = true;
            mSocket.Close();
            mSocket = null;
        }
    }

    /// <summary>
    /// Su dung cho client
    /// </summary>
    /// <param name="_inPort"></param>
    /// <returns></returns>
    public bool InitForClient(int _inPort)
    {
        mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        Disconnected = false;
        if (mSocket == null) return false;
        try
        {
            mSocket.Blocking = true;
        }
        catch
        {
            Debug.Log("Khong the thiet lap socket ve trang thai Non-Blocking!");
            return false;
        }

        //if (NetworkManagerClient.mInstance != null)
        //{
        //    if (NetworkManagerClient.mInstance.GetServerEndpoint() != null)
        //    {
        //        mSocket.BeginConnect(NetworkManagerClient.mInstance.GetServerEndpoint(), new AsyncCallback(ClientConnectCallBack), null);
        //    }
        //}
        mSocket.Connect(NetworkManagerClient.mInstance.GetServerEndpoint());
        mPort = _inPort;
        Debug.Log("Cong ket noi: " + mPort);
        //StartReceivingUseBlockingMode();
        return true;
    }

    private void ClientConnectCallBack(IAsyncResult ar)
    {
        mSocket.EndConnect(ar);
    }

    

    /// <summary>
    /// Su dung non blocking mode de nhan packet
    /// </summary>
    public void StartReceivePackets()
    {
        try
        {
            IPAddress IpAddr = IPAddress.Any;
            IPEndPoint IpEndp = new IPEndPoint(IpAddr, mPort);
            EndPoint Endp = (EndPoint)IpEndp;

            mData = new byte[1020];
            //if (Disconnected) return;
            Debug.Log("Receiving");
            mSocket.BeginReceiveFrom(mData, 0, mData.Length, SocketFlags.None, ref Endp, new AsyncCallback(ReceiveFrom_CallBack), null);
        }
        catch (ObjectDisposedException ex)
        {
            Debug.Log("The Socket has been closed.");
        }
    }



    private void ReceiveFrom_CallBack(IAsyncResult ar)
    {
        try
        {
            IPAddress IpAddr = IPAddress.Any;
            IPEndPoint IpEndp = new IPEndPoint(IpAddr, mPort);
            EndPoint Endp = (EndPoint)IpEndp;

            int _byteCount = mSocket.EndReceiveFrom(ar, ref Endp);

            if (_byteCount > 0)
            {
                //Debug.Log("OK "+ _byteCount);
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
            //mCanTransmit = true;
            StartReceivePackets();
            //test = true;
        }
        catch (ObjectDisposedException ex)
        {
            Debug.Log("The Socket has been closed.");
        }
    }

    public void ProcessIncomingPackets()
    {
        CurrentTime = Time.time;
        //ReadIncomingProcessIntoQueue();         // đưa packet vừa tới vào hàng đợi xử lý
        ProcessQueuedPackets();                 // Xử lý tất cả packets trong hàng đợi.
    }



    /// <summary>
    /// Khong su dung
    /// </summary>
    void ReadIncomingProcessIntoQueue()
    {
        IPAddress IpAddr = IPAddress.Any;
        IPEndPoint IpEndp = new IPEndPoint(IpAddr, mPort);
        EndPoint Endp = (EndPoint)IpEndp;

        int ReceivedPacketsCount = 0;
        int ReceivedBytesCount = 0;

        while (ReceivedPacketsCount < kMaxPacketsPerFrameCount)
        {
            byte[] data = new byte[1020];
            
            int BytesCount = mSocket.ReceiveFrom(data, ref Endp);
            if (BytesCount == 0)
            {
                break;
            }
            else if (BytesCount < 0)
            {
                Debug.Log("Loi nhan du lieu!");
                break;
            }
            else
            {
                ++ReceivedPacketsCount;
                ReceivedBytesCount += BytesCount;
                InputMemoryBitStream input = new InputMemoryBitStream(data, BytesCount * 8);

                // gia lap mat goi tin
                if (UnityEngine.Random.Range(0, 100) > mSimulatedDropPacket)
                {
                    // thoi gian nhan = tg hien tai + tg tre cua 1 packet.
                    float receivedTime = Time.time + mSimulatedLatency;
                    ReceivedPacket packet = new ReceivedPacket(ref input, Endp, receivedTime);
                    mPacketQueue.Enqueue(packet);
                }
                else
                {
                    Debug.Log("1 packet da bi mat!");
                }
            }
        }
        if (ReceivedBytesCount > 0)
        {
            // do something
        }
    }

    /// <summary>
    /// Khong su dung.
    /// </summary>
    void TransmitQueuedPackets()
    {
        while (mCanTransmit && mTempQueued.Count != 0)
        {
            mPacketQueue.Enqueue(mTempQueued.Dequeue());
        }
    }



    void ProcessQueuedPackets()
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



    public void AddGameobjectToIdGameobjectDic(GameObject _inGameobject)
    {
        TransformSynch sync = _inGameobject.GetComponent<TransformSynch>();
        if (sync)
        {
            mNetworkIdToGameobjectDic.Add(sync.GetIdNetwork(), _inGameobject);
        }
    }



    public void RemoveGameobjectFromIdGameobjectDic(GameObject _inGameobject)
    {
        TransformSynch sync = _inGameobject.GetComponent<TransformSynch>();
        if (sync)
        {
            mNetworkIdToGameobjectDic.Remove(sync.GetIdNetwork());
        }
    }



    public GameObject GetGameobjectByNetworkId(int _inNetworkId)
    {
        if (mNetworkIdToGameobjectDic.ContainsKey(_inNetworkId)) return mNetworkIdToGameobjectDic[_inNetworkId];
        else return null;
    }


    /// <summary>
    /// send packet khi socket đang ở trạng thái non blocking
    /// </summary>
    /// <param name="output"></param>
    /// <param name="_inEndpoint"></param>
    public void SendPacket1(ref OutputMemoryBitStream output, EndPoint _inEndpoint)
    {
        if (mSocket == null)
        {
            Debug.Log("socket null");
            return;
        }
        if (_inEndpoint == null)
        {
            Debug.Log("server endpoint null");
            return;
        }
        if (Disconnected) return;
        try
        {
            //Debug.Log(((IPEndPoint)_inEndpoint).Address.ToString());
            mSocket.BeginSendTo(output.GetBuffer(), 0, output.GetByteLength(), SocketFlags.None, _inEndpoint, new AsyncCallback(SendTo_CallBack), null);
        }
        catch (ObjectDisposedException ex)
        {
            Debug.Log("The Socket has been closed.");
            return;
        }
    }




    private void SendTo_CallBack(IAsyncResult ar)
    {
        try
        {
            int bytesSent = mSocket.EndSendTo(ar);
        }
        catch (ObjectDisposedException ex)
        {
            Debug.Log("The Socket has been closed.");
            return;
        }
    }

    #region blocking mode

    public void StartReceivingUseBlockingMode()
    {
        Thread thread = new Thread(ReceivingThread);
        thread.IsBackground = true;
        thread.Start();
    }

    private void ReceivingThread()
    {
        IPAddress IpAddr = IPAddress.Any;
        IPEndPoint IpEndp = new IPEndPoint(IpAddr, mPort);
        EndPoint Endp = (EndPoint)IpEndp;
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

    /// <summary>
    /// send packet khi packet đang ở trạng thái blocking
    /// </summary>
    /// <param name="output"></param>
    /// <param name="_inEndpoint"></param>
    public void SendPacket(OutputMemoryBitStream output, EndPoint _inEndpoint)
    {
        Thread thread = new Thread(() => SendThread(output, _inEndpoint));
        thread.IsBackground = true;
        thread.Start();
    }

    private void SendThread(OutputMemoryBitStream output, EndPoint _inEndpoint)
    {
        byte[] data = new byte[output.GetByteLength()];
        Array.Copy(output.GetBuffer(), data, output.GetByteLength());

        mSocket.SendTo(data, _inEndpoint);
    }

    #endregion


}
