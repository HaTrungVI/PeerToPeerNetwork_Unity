using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScript : MonoBehaviour {
    private float mTime = 0;
    private int mCount = 0;
    public Text mLoadingText;
    void Start()
    {
        mLoadingText.text = "Loading";
    }

    void Update()
    {
        if (Time.time - mTime >= 0.2f)
        {
            mTime = Time.time;
            mCount++;
            if (mCount > 3)
            {
                mCount = 0;
                mLoadingText.text = "Loading";
            }
            else
            {
                mLoadingText.text += ".";
            }
        }
    }
}
