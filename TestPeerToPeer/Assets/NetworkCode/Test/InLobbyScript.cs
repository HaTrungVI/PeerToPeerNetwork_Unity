using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InLobbyScript : MonoBehaviour {

    //[SerializeField]
    //private PeerToPeerManager mPeerToPeerManager;
    [SerializeField]
    private Transform mContent;
    [SerializeField]
    private GameObject PlayerItem;

    private int mPlayerCount = -1;
    private bool first = true;
    public void SetFirst() { first = true; }
    void Start()
    {
        mPlayerCount = -1;
    }
    
	void Update () {
        if (NetworkManager_v2.mInstance == null) return;

        if (mPlayerCount != NetworkManager_v2.mInstance.GetPLayerCount() || first)
        {
            first = false;
            mPlayerCount = NetworkManager_v2.mInstance.GetPLayerCount();
            foreach (Transform child in mContent)
            {
                Destroy(child.gameObject);
            }

            Dictionary<int, string> PlayersList = NetworkManager_v2.mInstance.GetPlayersList();

            foreach (var pair in PlayersList.OrderBy(key => key.Key))
            {
                GameObject plItem = Instantiate(PlayerItem);

                plItem.transform.SetParent(mContent);

                plItem.GetComponent<PlayerItem>().Setup(pair.Key, pair.Value);
            }
        }
	}
}
