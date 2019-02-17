using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;

public class Client : MonoBehaviour {

    public float mServerDisconnectTimeout = 3f;

    private NetworkVI mNetworkVI;
    private EndPoint mServerEndpoint;
    private string mName = "TrungVI";

    private bool first = false;

    public void SetEndpointServer(EndPoint inEP) { mServerEndpoint = inEP; }
    public void SetName(string inName) { mName = inName; }
	public void InBegin (EndPoint inEP, GameObject _inGameobject , NetworkVI _inVI, string inName = "name") 
    {
        mNetworkVI = _inVI;
        mServerEndpoint = inEP;
        mName = inName;
        NetworkManagerClient.StaticInint(mServerEndpoint, mName, this);
        NetworkManagerClient.mInstance.mGameobjectToSpawn = _inGameobject;
        NetworkManagerClient.mInstance.SetDisconnected(false);
        NetworkManagerClient.mInstance.SetServerDisconnectTimeout(mServerDisconnectTimeout);
        NetworkManagerClient.mInstance.SetTimeReceivedPacket(Time.time);
        first = false;
	}
	
	void Update () {
        if (NetworkManagerClient.mInstance != null)
        {
           // NetworkManagerClient.mInstance.CheckReceiveProcess();

            NetworkManagerClient.mInstance.ProcessIncomingPackets();

            NetworkManagerClient.mInstance.CheckServerDisconnect();

            NetworkManagerClient.mInstance.SendOutgoingPacket();

            if (!first)
            {
                first = true;
                NetworkManagerClient.mInstance.StartReceivingUseBlockingMode();
            }

            if (NetworkManagerClient.mInstance.GetDisconnected())
            {
                //Destroy(this.gameObject);
                mNetworkVI.GoBackLobbyScene();
            }
        }
	}

    public void HandleServerDisconnect()
    {
        Destroy(this.gameObject);
        mNetworkVI.GoBackLobbyScene();
    }

    public GameObject InstanceGameobject(GameObject inGameobject)
    {
        return (GameObject)Instantiate(inGameobject);
    }
    public void DestroyGameobject(GameObject inGameobject)
    {
        if (inGameobject)
        {
            Destroy(inGameobject);
        }
    }
}
