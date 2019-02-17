using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Net;
using System.Net.Sockets;

public class NetworkVI : MonoBehaviour {

    public GameObject mButtonInGame;

    public static bool mIsServer = false;

    public InputField mIpInputField;

    public GameObject mServerObject;
    public GameObject mClientObject;

    [Header("Network scene")]
    public string nameofSceneNetwork;
    [Header("Gameobject to spawn")]
    public GameObject mGameobject;

    private EndPoint mServerEndpoint;

    public Transform mSpawnPoints;


    private string LobbyScene = "";

    private GameObject ui = null;
    private GameObject mServer = null;
    private GameObject mClient = null;

    private bool First = true;

    void Start()
    {
        Application.runInBackground = true;
        DontDestroyOnLoad(this.gameObject);
            
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
    }

    void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if (arg0.name == nameofSceneNetwork)
        {
            ui = (GameObject)Instantiate(mButtonInGame);
            ui.GetComponent<UiInGame>().Setup(this);

            if (NetworkVI.mIsServer)
            {
                //Debug.Log("Join as server!");
                mServer = (GameObject)Instantiate(mServerObject);
                mServer.GetComponent<Server>().InBegin(mGameobject, this);
            }
            else
            {
                //Debug.Log("Join as client!");
                mClient = (GameObject)Instantiate(mClientObject);
                mClient.GetComponent<Client>().InBegin(mServerEndpoint, mGameobject, this, "KhongTen");
            }
        }
        else
        {
            if (ui) Destroy(ui);
            if (mServer) Destroy(mServer);
            if (mClient) Destroy(mClient);
        }
    }

    public void RemoveDelegate()
    {
        SceneManager.sceneLoaded -= SceneManager_sceneLoaded;  
    }

    public void CreateRoom()
    {
        mIsServer = true;
        int mSceneindex = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + nameofSceneNetwork + ".unity");
        LobbyScene = SceneManager.GetActiveScene().name;
        if (mSceneindex >= 0)
        {
            SceneManager.LoadScene(mSceneindex);
        }
        else
        {
            Debug.Log("Xem xet lai ten cua network scene!");
        }
    }

    public void JoinRoom()
    {
        EndPoint ep = null;
        try
        {
            IPAddress addr = IPAddress.Parse(mIpInputField.text);
            ep = (EndPoint)(new IPEndPoint(addr, 8888));
        }
        catch
        {
            Debug.Log("Dia chi IP khong dung!");
            return;
        }
        if (ep == null) return;
        mServerEndpoint = ep;
        mIsServer = false;
        int mSceneindex = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + nameofSceneNetwork + ".unity");
        LobbyScene = SceneManager.GetActiveScene().name;
        if (mSceneindex >= 0)
        {
            SceneManager.LoadScene(mSceneindex);
        }
        else
        {
            Debug.Log("Xem xet lai ten cua network scene!");
        }
    }


    public void LeaveRoom()
    {
        if (LobbyScene == "") return;

        if (mIsServer)
        {
            if (NetworkManagerServer.mInstance != null)
            {
                NetworkManagerServer.mInstance.Disconnect();
            }
        }
        else
        {
            if (NetworkManagerClient.mInstance != null)
            {
                NetworkManagerClient.mInstance.Disconnect();
            }
        }

    }

    public void GoBackLobbyScene()
    {
        if (LobbyScene == "") return;

        int mSceneindex = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + LobbyScene + ".unity");
        if (mSceneindex >= 0)
        {
            this.RemoveDelegate();
            Destroy(this.gameObject);
            SceneManager.LoadScene(mSceneindex);
        }
        else
        {
            Debug.Log("Khong tim thay lobby scene!");
        }

    }

    /// <summary>
    /// Mô phỏng khi game object bị hủy
    /// Hàm này chỉ để thử nghiệm
    /// </summary>
    public void DestroyGameobject()
    {
        if (mIsServer)
        {
            if (NetworkManagerServer.mInstance != null)
            {
                NetworkManagerServer.mInstance.DestroyGameobject();
            }
        }
        else
        {
            if (NetworkManagerClient.mInstance != null)
            {
                NetworkManagerClient.mInstance.DestroyGameobject();
            }
        }
    }

    public void RespawnGameobject()
    {
        if (mIsServer)
        {
            if (NetworkManagerServer.mInstance != null)
            {
                NetworkManagerServer.mInstance.RespawnGameobject();
                //NetworkManagerServer.mInstance.GetSocket().Close();
            }
        }
        else
        {
            if (NetworkManagerClient.mInstance != null)
            {
                NetworkManagerClient.mInstance.RespawnGameobject();
            }
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Quit on " + Time.time);
        if (mIsServer)
        {
            if (NetworkManagerServer.mInstance != null)
            {
                NetworkManagerServer.mInstance.HandleDisconect();
            }
        }
        else
        {
            if (NetworkManagerClient.mInstance != null)
            {
                NetworkManagerClient.mInstance.SendAppExitPacket();
                NetworkManagerClient.mInstance.HandleDisconect();
            }
        }
    }
}
