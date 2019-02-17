using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// class test mẫu do người dùng tự định nghĩa
public class CustomP2pManager : PeerToPeerManager {

    private Camera mCamera = null;
    private cakeslice.Outline mLastOutline =  null;

    
    public override void SpawnGameobjects()
    {
        if (InputManager.mIntance != null && mSpawnPositon.Length > 0 && mSpawnIdToGameobject.Count > 0)
        {
            StartCoroutine(WaitForSpawn());
        }
        
    }
    
    IEnumerator WaitForSpawn()
    {
        yield return new WaitForSeconds(0.2f);
        // để thực hiện Instantiate 1 gameobject hãy gọi tới HandleSpawnGameobject
        InputManager.mIntance.HandleSpawnGameobject(mSpawnPositon[Random.Range(0, mSpawnPositon.Length)].position, 0);
        SetupFuntion();
    }

    void SetupFuntion()
    {
        GameObject _CameraParent = GameObject.FindGameObjectWithTag("Cam");
        mCamera = _CameraParent.transform.GetChild(0).GetComponent<Camera>();
        GameObject ui = GameObject.FindGameObjectWithTag("UI");
        ui.GetComponent<UiScript>().SetP2pManager(this);
    }

    void Update()
    {
        bool outob = true;
        if (NetworkManager_v2.mInstance != null)
        {
            if (NetworkManager_v2.mInstance.mState == NetworkManager_v2.NetworkState.Playing && mCamera != null)
            {
                Ray ray = mCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 400f))
                {
                    GameObject gameob = hit.transform.root.gameObject;
                    PlayerController pl = gameob.GetComponent<PlayerController>();
                    if (pl != null)
                    {
                        if (pl.GetPlayerId() != NetworkManager_v2.mInstance.GetPlayerID())
                        {
                            pl.mOutline.enabled = true;
                            mLastOutline = pl.mOutline;
                            outob = false;
                        }
                    }
                }                
            }
        }

        if (outob && mLastOutline != null)
        {
            mLastOutline.enabled = false;
        }


    }

    
}
