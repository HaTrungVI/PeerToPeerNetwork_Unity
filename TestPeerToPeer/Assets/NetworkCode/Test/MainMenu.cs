using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;

public class MainMenu : MonoBehaviour 
{

    [SerializeField]
    private PeerToPeerManager mPeerToPeerManager;
    [SerializeField]
    private InputField mIpAddressText;
    [SerializeField]
    private InputField mNameText;

    [SerializeField]
    GameObject mLoading;
    [SerializeField]
    GameObject mLobby;
    [SerializeField]
    GameObject mMain;
    [SerializeField]
    GameObject mStartGame;
    float _time = 0f;
    void Start()
    {
        
        mMain.SetActive(true);
        mLoading.SetActive(false);
        mLobby.SetActive(false);
    }

    void Update()
    {
        if (NetworkManager_v2.mInstance == null)
        {
            mMain.SetActive(true);
            mLoading.SetActive(false);
            mLobby.SetActive(false);
            return;
        }
        if (NetworkManager_v2.mInstance.mState == NetworkManager_v2.NetworkState.Lobby)
        {
            mLoading.SetActive(false);
            mLobby.SetActive(true);
            mLobby.GetComponent<InLobbyScript>().SetFirst();
            mMain.SetActive(false);
        }
        else if (NetworkManager_v2.mInstance.mState == NetworkManager_v2.NetworkState.Playing)
        {
            mPeerToPeerManager.LoadPlayScene();
        }
        else if (NetworkManager_v2.mInstance.mState == NetworkManager_v2.NetworkState.Starting)
        {
            mLoading.SetActive(true);
            mLobby.SetActive(false);
            mMain.SetActive(false);
        }
    }

    public void CreateRoom()
    {
        mPeerToPeerManager.CreateRoom(mNameText.text);
        
        mMain.SetActive(false);
        mLoading.SetActive(true);
        
    }

    public void JoinRoom()
    {
        try
        {
            IPAddress address = IPAddress.Parse(mIpAddressText.text);
            EndPoint IpEndpointMP = (EndPoint)(new IPEndPoint(address, mPeerToPeerManager.Port));
            
            mPeerToPeerManager.JoinRoom(mNameText.text, IpEndpointMP);
            mMain.SetActive(false);
            mLoading.SetActive(true);
        }
        catch
        {
            Debug.Log("Nhap lai di chi IP!");
            return;
        }
    }

    public void DisconnectOnLobby()
    {
        if (mPeerToPeerManager != null)
        {
            mPeerToPeerManager.Disconnect(true);
        }
    }

    public void StartGame()
    {
        if (mPeerToPeerManager != null)
        {
            mPeerToPeerManager.TryStartGame();
        }
    }
}
