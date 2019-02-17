using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class movementPlayer : MonoBehaviour {
    //private NetWorkIdentity mNetworkIdentity;
    private Rigidbody2D _Rigidbody;
    private float hor = 0f;
    private float ver = 0f;
    private TransformSynch sync;
    public float speed;
    //public Color[] RandomColors;
	// Use this for initialization
	void Start () {
        sync = GetComponent<TransformSynch>();
        _Rigidbody = GetComponent<Rigidbody2D>();
        //GetComponent<SpriteRenderer>().color = RandomColors[Random.Range(0, RandomColors.Length)];
       // mNetworkIdentity = GetComponent<NetWorkIdentity>();
	}

    void FixedUpdate()
    {
        if (sync)
        {
            if (!sync.GetIsLocalPlayer()) return;
        }
        //if (!mNetworkIdentity.IsLocalPlayer) return;
        hor = Input.GetAxis("Horizontal");
        ver = Input.GetAxis("Vertical");

        Vector3 pos = new Vector3(hor, ver, 0f) * speed * Time.fixedDeltaTime;
        pos = transform.position + pos;
        _Rigidbody.MovePosition(pos);
    }
}
