using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine.SceneManagement;

public class PeerToPeerManager : MonoBehaviour 
{

    public string mNamePlayScene;
    public GameObject[] mGameobjectsToSpawn;
    public Transform[] mSpawnPositon;
    public int Port=8888;
    public float mFrameTime = 0.033f;

    private string mNameLobbyScene = "";
    private bool mKeepRunning = true;
    public static Dictionary<int, GameObject> mSpawnIdToGameobject = new Dictionary<int, GameObject>();

    private bool mInLobby = false;
    public bool GetInLobby() { return mInLobby; }
    public void SetInLobby(bool _il) { mInLobby = _il; }

	private bool Setup (bool _IsMasterPeer, string _name, EndPoint _inMasterPeerEndpoint = null) 
    {
        bool re = true;
        if (_IsMasterPeer)
        {
            re = NetworkManager_v2.StaticInitAsMasterPeer(8888, _name, this);
        }
        else
        {
            if (_inMasterPeerEndpoint == null)
            {
                Debug.Log("Dia chi cua master peer bang Null!");
                return false;
            }
            re = NetworkManager_v2.StaticInitAsPeer(_inMasterPeerEndpoint, _name, this);
        }


        mSpawnIdToGameobject = new Dictionary<int, GameObject>();
        foreach (GameObject child in mGameobjectsToSpawn)
        {
            NetworkObject netob = child.GetComponent<NetworkObject>();
            if (netob != null)
            {
                try
                {
                    mSpawnIdToGameobject.Add(netob.SpawnID, child);
                }
                catch (ArgumentException)
                {
                    return false;
                    Debug.LogWarning("Để không xảy ra lỗi không mong muốn hãy thiết lập spawn id cho mỗi gameobject khác nhau các giá trị khác nhau!");
                }
            }
        }
        mKeepRunning = true;
        StartCoroutine(Frame());
        return re;
	}

    /// <summary>
    /// Gọi khi muốn tạo 1 phòng chơi
    /// </summary>
    /// <param name="_name">Tên người chơi</param>
    public bool CreateRoom(string _name)
    {
        string name = (_name == "") ? "player" : _name;
        return Setup(true, name);
    }

    /// <summary>
    /// Gọi khi muốn tham gia 1 phòng chơi
    /// </summary>
    /// <param name="_name">Tên người chơi</param>
    /// <param name="_MasterPeerEp">Địa chỉ IP (Endpoint) của chủ phòng</param>
    public bool JoinRoom(string _name, EndPoint _MasterPeerEp)
    {
        string name = (_name == "") ? "player" : _name;
        return Setup(false, name, _MasterPeerEp);
    }

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    IEnumerator Frame()
    {
        // 1 frame ~= 33 ms. tương ứng với 30 fps
        // Để hoàn thành 1 turn data cần kSubTurnPerTurn = 3 frame.
        // => cứ trong 100 ms sẽ hoàn thành 1 turn data để gửi đi.
        while (mKeepRunning)
        {
            float mTime = Time.time;

            DoFrame();
            //
            if (NetworkManager_v2.mInstance != null)
            {
                if (NetworkManager_v2.mInstance.delay_game)
                {
                    NetworkManager_v2.mInstance.delay_game = false;
                    yield return new WaitForSeconds(0.1f);
                    NetworkManager_v2.mInstance.ProcessCommands();
                }
            }

            mTime = Time.time - mTime;
            mTime = mFrameTime - mTime;
            if (mTime < 0f) mTime = 0f;
            yield return new WaitForSeconds(mTime);
        }
    }

    private void DoFrame()
    {
        if (NetworkManager_v2.mInstance != null)
        {
            if (NetworkManager_v2.mInstance.mState != NetworkManager_v2.NetworkState.Delay)
            {
                NetworkManager_v2.mInstance.ProcessIncomingPackets();
                NetworkManager_v2.mInstance.SendOutgoingPackets();
            }
            else
            {
                NetworkManager_v2.mInstance.ProcessIncomingPackets();
            }
        }
    }

    protected void RegisterGameobject(GameObject _inGameob)
    {
        if (NetworkManager_v2.mInstance != null)
        {
            NetworkManager_v2.mInstance.RegisterGameobject(_inGameob);
        }
    }

    public void InstantiateGameobject(int playerid, Vector3 _position, int _spawnid)
    {
        
        GameObject gameobToSpw = mSpawnIdToGameobject[_spawnid];
        if (gameobToSpw == null) return;

        GameObject gameob = Instantiate(gameobToSpw, _position, Quaternion.identity);
        RegisterGameobject(gameob);

        NetworkObject netob = gameob.GetComponent<NetworkObject>();
        if (netob != null)
        {
            netob.SetPlayerId(playerid);
        }
        
    }

    public void LoadPlayScene()
    {
        int mSceneindex = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + mNamePlayScene + ".unity");

        if (mSceneindex >= 0)
        {
            mNameLobbyScene = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(mSceneindex);
            SpawnGameobjects();
        }
    }

    public void LoadLobbyScene()
    {
        if (mNameLobbyScene == "") return;

        int mSceneindex = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + mNameLobbyScene + ".unity");

        if (mSceneindex >= 0)
        {
            SceneManager.LoadScene(mSceneindex);
            Destroy(this.gameObject);
        }

    }

    /// <summary>
    /// Khi đang trong lobby(phòng đợi) gọi khi muốn bắt đầu trò chơi.
    /// </summary>
    /// <returns></returns>
    public bool TryStartGame()
    {
        if (NetworkManager_v2.mInstance != null && mKeepRunning)
        {
            return NetworkManager_v2.mInstance.TryStartGame();
        }
        else return false;
    }

    private void OnApplicationQuit()
    {
        if (NetworkManager_v2.mInstance != null)
        {
            NetworkManager_v2.mInstance.HandleDisconect();
        }
        
    }

    /// <summary>
    /// Gọi khi muốn rời khỏi phòng.
    /// </summary>
    /// <param name="_inLobby">True nếu bạn đạng trong lobby(Phòng chờ), False nếu bạn đang trong trò chơi</param>
    public void Disconnect(bool _inLobby)
    {
        mKeepRunning = false;
        if (NetworkManager_v2.mInstance != null)
        {
            NetworkManager_v2.mInstance.SendDisconnectPacket();
            StartCoroutine(wait(_inLobby));
        }
    }

    IEnumerator wait(bool _inLobby)
    {
        yield return new WaitForSeconds(0.5f);
        NetworkManager_v2.mInstance.HandleDisconect();
        NetworkManager_v2.mInstance = null;
        if (!_inLobby) LoadLobbyScene();
        
    }

    /// <summary>
    /// Sử dụng hàm này khi muốn Instantiate 1 hay nhiều gameobject
    /// </summary>
    public virtual void SpawnGameobjects() { return; }


}
