using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using NetworkTrungVI;

public class Server : MonoBehaviour {
    [Space(5)]
    [Header("Mo phong mat goi tin & Do cham tre")]
    [Range(0, 100)]
    public int mSimulatedDropPacket = 0;
    public float mSimulatedLatency = 0;
    public float mDisconnectTimeout = 3f;

    [Space(5)]
    [Header("Network setting")]
    public int mPort = 8888;

    private bool first = false;
    //private Transform[] SpawnPoints;
    private NetworkVI mNetworkVI;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mGameobject">Tham so truyen vao la 1 object de spawn</param>
 	public void InBegin (GameObject mGameobject, NetworkVI _inNet) {
        
        mNetworkVI = _inNet;

        NetworkManagerServer.ServerInit(8888, this);
        NetworkManagerServer.mInstance.SetSimulatedLatency(mSimulatedLatency);
        NetworkManagerServer.mInstance.SetSimulatedDropPacket(mSimulatedDropPacket);
        NetworkManagerServer.mInstance.mGameobjectToSpawn = mGameobject;
        NetworkManagerServer.mInstance.SetDisconnected(false);
        NetworkManagerServer.mInstance.SetDisconnectTimeout(mDisconnectTimeout);

        first = false;
        // Khi bắt đầu vào trò chơi hãy tạo 1 gameobject cho người chơi
        GameObject ob = InstanceGameobject(NetworkManagerServer.mInstance.mGameobjectToSpawn);
        NetworkManagerServer.mInstance.SetLocalGameobject(ob);

        TransformSynch sync = ob.GetComponent<TransformSynch>();
        if (sync)
        {
            sync.SetIsLocalPlayer(true);
        }

	}

	
	void Update () {
        if (NetworkManagerServer.mInstance != null)
        {
            //NetworkManagerServer.mInstance.CheckReceiveProcess();

            NetworkManagerServer.mInstance.ProcessIncomingPackets();

            NetworkManagerServer.mInstance.CheckClientsDisconnect();

            NetworkManagerServer.mInstance.SendOutgoingPacket();

            if (!first)
            {
                first = true;
                NetworkManagerServer.mInstance.StartReceivingUseBlockingMode();
            }

            if (NetworkManagerServer.mInstance.GetDisconnected())
            {
                //Destroy(this.gameObject);
                mNetworkVI.GoBackLobbyScene();
            }
        }
        
	}
    /// <summary>
    /// Phuong thuc tao mot gameobject tren server.
    /// </summary>
    /// <param name="inGameobject">Doi tuong can tao.</param>
    /// <returns></returns>
    public GameObject InstanceGameobject(GameObject inGameobject)
    {
        Transform randomTr = mNetworkVI.mSpawnPoints.GetChild(UnityEngine.Random.Range(0, mNetworkVI.mSpawnPoints.childCount));

        GameObject gameOb = (GameObject)Instantiate(inGameobject, randomTr.position, Quaternion.identity);
        
        return NetworkManagerServer.mInstance.RegisterAndReturn(gameOb);
    }

    public void HandleLostClient(ClientProxy _inClientproxy)
    {
        int playerId = _inClientproxy.GetPlayerID();
        // chưa hoàn thành.
        GameObject playerObject = GetPlayerGameobjectByPlayerId(playerId);
        if (playerObject)
        {
            DestroyGameobject(playerObject);
        }
    }

    private GameObject GetPlayerGameobjectByPlayerId(int _playerId)
    {
        GameObject[] gameobjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject child in gameobjects)
        {
            TransformSynch sync = child.GetComponent<TransformSynch>();
            if (sync)
            {
                if (sync.GetIdPlayer() == _playerId)
                {
                    Debug.Log(sync.GetIdPlayer());
                    return child;
                }
            }
        }
        return null;
    }

    public void DestroyGameobject(GameObject inGameobject)
    {
        if (inGameobject)
        {
            NetworkManagerServer.mInstance.UnRegisterGameobject(inGameobject);
            Destroy(inGameobject);
        }
    }
}
