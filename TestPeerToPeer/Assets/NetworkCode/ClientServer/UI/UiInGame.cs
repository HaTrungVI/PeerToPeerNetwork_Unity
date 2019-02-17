using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UiInGame : MonoBehaviour {

    private NetworkVI mNetworkVI;
    
    public void Setup(NetworkVI _inNet)
    {
        mNetworkVI = _inNet;
    }

    public void LeaveGame()
    {
        if (mNetworkVI)
        {
            mNetworkVI.LeaveRoom();
        }
    }

    public void DestroyGameobject()
    {
        if (mNetworkVI) mNetworkVI.DestroyGameobject();
    }

    public void RespawnGameobject()
    {
        if (mNetworkVI) mNetworkVI.RespawnGameobject();
    }
}
