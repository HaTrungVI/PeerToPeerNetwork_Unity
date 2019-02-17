using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiScript : MonoBehaviour {

    [SerializeField]
    private Text mText;

    private PeerToPeerManager mP2p;

    public void SetP2pManager(PeerToPeerManager _p2p) { mP2p = _p2p; }
	void Update () {
        if (NetworkManager_v2.mInstance != null)
        {
            mText.text = NetworkManager_v2.mInstance.GetTurnNumber().ToString();
        }
	}

    public void LeaveGame()
    {
        if (mP2p != null)
        {
            mP2p.Disconnect(false);
        }
    }
}
