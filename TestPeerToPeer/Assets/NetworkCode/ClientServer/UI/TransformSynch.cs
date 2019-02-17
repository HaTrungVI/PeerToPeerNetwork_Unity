using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetworkTrungVI;

public class TransformSynch : MonoBehaviour
{
    #region Region for synchronize
    /// <summary>
    /// Muon dong bo them nua thi them vao day.
    /// Sau này có thể thêm đồng bộ animator qua các biến.
    /// </summary>
    public enum ReplicationState
    {
        Position = 1 << 0,
        Rotation = 1 << 1,
    }
    public bool PositionSync = false;
    public bool RotationSync = false;
    private int BitState = 0;
    #endregion
    private bool IsLocalPlayer = false;
    public void SetIsLocalPlayer(bool _inData) { IsLocalPlayer = _inData; }
    public bool GetIsLocalPlayer() { return IsLocalPlayer; }

    public enum SyncMode
    {
        None=0,
        Lerp
    }
    public SyncMode SynchronizeMode;
    public float LerpSpeed=10f;

    private int mIdNetwork;
    private int mIdPlayer;

    private float mMovementSpeed = 0f;
    private Vector3 mLastPosition = Vector3.zero;
    private Vector3 mLastV3 = Vector3.zero;
    private Quaternion mLastQua = Quaternion.identity;

    private Vector3 newPosition = Vector3.zero;
    private Quaternion newRotation = Quaternion.identity;
    private int mDirtyState = 0;

    void Awake()
    {
        if (PositionSync) BitState |= (1 << 0);
        if (RotationSync) BitState |= (1 << 1);
    }

    void Start () 
    {
        mLastPosition = transform.position;
        mLastPosition = transform.position;
        newPosition = transform.position;
        newRotation = transform.rotation;
        mLastQua = transform.rotation;
	}
	

	void Update () 
    {
        SetDirtyState();
        if (!IsLocalPlayer)
        {
            if (SynchronizeMode == SyncMode.Lerp)
            {
                if (PositionSync) transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * LerpSpeed);
                if (RotationSync) transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * LerpSpeed);
            }
            else if (SynchronizeMode == SyncMode.None)
            {
                if (PositionSync) transform.position = newPosition;
                if (RotationSync) transform.rotation = newRotation;
            }
        }
        float delta = (transform.position - mLastPosition).magnitude;
        mMovementSpeed = delta / Time.deltaTime;
	}

    void SetDirtyState()
    {
        // neu nguoi choi dong vai tro la server
        if (NetworkVI.mIsServer)
        {
            if (transform.position != mLastV3)
            {
                int bit = 1 << 0;
                NetworkManagerServer.mInstance.SetDirtyState(mIdNetwork, bit);
            }
            if (transform.rotation != mLastQua)
            {
                int bit = 1 << 1;
                NetworkManagerServer.mInstance.SetDirtyState(mIdNetwork, bit);
            }
            mLastV3 = transform.position;
            mLastQua = transform.rotation;
        }
    }

    public bool HasUpdateState()
    {
        bool _return = (mLastV3 != transform.position) || (mLastQua != transform.rotation);
        mLastV3 = transform.position;
        mLastQua = transform.rotation;
        return _return;
    }

    //public int HasUpdateState()
    //{
    //    int bits = 0;
        
    //    return bits;
    //}


    public int GetBitState() { return BitState; }
    public int GetIdNetwork() { return mIdNetwork; }
    public void SetIdNetwork(int _inIdNetwork) { mIdNetwork = _inIdNetwork; }
    public int GetIdPlayer() { return mIdPlayer; }
    public void SetIdPlayer(int _inIdPlayer) { mIdPlayer = _inIdPlayer; }

    public int Write(ref OutputMemoryBitStream output, int _inDirtyState)
    {
        int bitsWritten = 0;
        if ((_inDirtyState & (1 << 0)) != 0)            // sync position
        {
            output.Write(true);
            output.Write(transform.position);
            bitsWritten |= (1 << 0);                    // (1<<0) tuong ung voi trang thai sync position
        }
        else output.Write(false);


        if ((_inDirtyState & (1 << 1)) != 0)            // sync rotation
        {
            output.Write(true);
            output.Write(transform.rotation);
            bitsWritten |= (1 << 1);                    // (1<<1) tuong uong voi trang thai sync rotaion
        }
        else output.Write(false);

        return bitsWritten;
    }
    public void Write(ref OutputMemoryBitStream output)
    {
        if (PositionSync)
        {
            output.Write(true);
            output.Write(transform.position);
        }
        if (RotationSync)
        {
            output.Write(true);
            output.Write(transform.rotation);
        }
    }
    public void Read(ref InputMemoryBitStream input)
    {
        if (PositionSync)
        {
            bool canRead = false;
            input.Read(ref canRead);
            if(canRead) input.Read(ref newPosition);
        }
        if (RotationSync)
        {
            bool canRead = false;
            input.Read(ref canRead);
            if(canRead) input.Read(ref newRotation);
        }
    }

    public void ResetTransform()
    {
        if (PositionSync) transform.position = newPosition;
        if (RotationSync) transform.rotation = newRotation;
    }
}
