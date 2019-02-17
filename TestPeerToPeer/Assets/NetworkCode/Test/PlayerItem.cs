using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerItem : MonoBehaviour
{
    [SerializeField]
    private Text mName;
    [SerializeField]
    private Text mID;

    private int mPlayerID = -1;
    public int GetPlayerID() { return mPlayerID; }

    public Color _ReadyColor;
    public Color _UnReadyColor;

    public void Setup(int playerid, string name)
    {
        mName.text = name;
        mID.text = playerid.ToString();
        mPlayerID = playerid;
    }

    public void Ready()
    {
        this.GetComponent<Image>().color = _ReadyColor;
    }

    public void UnReady()
    {
        this.GetComponent<Image>().color = _UnReadyColor;
    }
    
}
