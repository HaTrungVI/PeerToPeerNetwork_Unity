using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetworkTrungVI;


public class NetworkObject : MonoBehaviour 
{
    /// <summary>
    /// để không xảy ra lỗi không mong muốn hãy thiết lập spawn id cho mỗi gameobject khác nhau các giá trị khác nhau.
    /// </summary>
    public int SpawnID = 0;
    protected int mNetworkID = -1;
    protected int mPlayerID = -1;
    public int GetNetWorkId() { return mNetworkID; }
    public void SetNetworkId(int _inID) { mNetworkID = _inID; }
    public void SetPlayerId(int _inID) { mPlayerID = _inID; }
    public int GetPlayerId() { return mPlayerID; }    

    #region nguoi dung tu dinh nghia


    /// <summary>
    /// ghi tất cả các thuộc tính cần kiểm tra tính đồng bộ
    /// </summary>
    /// <param name="output"></param>
    public virtual void WriteForCrc(ref OutputMemoryBitStream output)
    {
        output.Write(mPlayerID);
        output.Write(mNetworkID);
    }
    
    public virtual void ToMove(Vector3 _targetPosition)
    {
        return;
    }
    public virtual void ToAttack(int _inTargetId)
    {
        return;
    }


    #endregion


    /// <summary>
    /// Lấy network id của gameobject này dự trên vị trí click chuột trong thế giới game.
    /// Trả về -1 nếu không tìm thầy Gameobject nào.
    /// Chú ý game object phải chứa Component "NetworkObject".
    /// </summary>
    /// <param name="_inTargetPosition"></param>
    /// <returns></returns>
    public static int TrySelectGameobject(Vector3 _inTargetPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(_inTargetPosition);
        RaycastHit hit;

        if(Physics.Raycast(ray, out hit, 200f))
        {
            GameObject _gameob = hit.transform.gameObject;
            NetworkObject ob = _gameob.GetComponent<NetworkObject>();
            if (ob != null)
            {
                return ob.GetNetWorkId();
            }
        }

        return -1;
    }
}
