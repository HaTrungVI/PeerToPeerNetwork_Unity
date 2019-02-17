using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
/// <summary>
/// Hỗ trợ xử lý Actions mà bắt buộc phải được sử dụng trong các hàm Update hay Start của Unity.
/// Ví dụ như khi sử dụng Instantiate phải bắt buộc sử dụng trong Update hoắc Start.
/// </summary>
public class Dispatcher : MonoBehaviour {

    private Queue<Action> mActions = new Queue<Action>();
	void Update () {
        lock (mActions)
        {
            while (mActions.Count != 0)
            {
                mActions.Dequeue().Invoke();
            }
        }
	}

    public void Invoke(Action _inAction)
    {
        lock (mActions)
        {
            mActions.Enqueue(_inAction);
        }
    }
}
