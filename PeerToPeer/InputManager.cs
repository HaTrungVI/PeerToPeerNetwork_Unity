using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager 
{
    public const int LEFT_MOUSE_BUTTON = 0;
    public const int RIGHT_MOUSE_BUTTON = 1;

    public enum ActionType
    {
        Attack = 0,
        Move = 1,
        None = 2,
    };

    private CommandList mCommandList;
    private int mSelectedNetowrkId;

    /// <summary>
    /// Nếu bạn muốn bỏ qua thao tác chọn đối tướng rồi mới di chuyển thì hãy dùng
    /// hàm này để set Selected Netowrk Id
    /// </summary>
    /// <param name="_indata"></param>
    public void SetSelectedNetworkId(int _indata)
    {
        mSelectedNetowrkId = _indata;
    }
    public InputManager()
    {
        mCommandList = new CommandList();
    }

    public static InputManager mIntance;
    public static void StaticInit()
    {
        mIntance = new InputManager();
    }

    public void NewCommandList()
    {
        mCommandList = new CommandList();
    }
    public CommandList GetCommandList() { return mCommandList; }
    public void ClearCommandList() { mCommandList.Clear(); }

    /// <summary>
    /// Thêm lệnh điều khiển bằng chuột và danh sách lệnh.
    /// </summary>
    /// <param name="_inPosition"> Tọa độ mục tiêu </param>
    /// <param name="_inMouseButton"> left mouse = 0, right mouse = 1 </param>
    public void HandleMouseClick(Vector3 _inPosition, int _inMouseButton, ActionType _inAction, int _targetId = -1)
    {
        switch (_inMouseButton)
        {
            case LEFT_MOUSE_BUTTON:
                {
                    mSelectedNetowrkId = NetworkObject.TrySelectGameobject(_inPosition);
                    break;
                }
            case RIGHT_MOUSE_BUTTON:
                {
                    //RightClickCommand(_inPosition);
                    RightClickCommandv2(_inPosition, _inAction, _targetId);
                    break;
                }
            default: break;
        }
    }

    private void RightClickTest()
    {
        Command cmd = TestCommand.StaticCreate();
        mCommandList.AddCommand(cmd);
    }

    private void RightClickCommand(Vector3 _inTargerPosition)
    {
        if (mSelectedNetowrkId > -1)
        {
            int targetId = NetworkObject.TrySelectGameobject(_inTargerPosition);

            Command cmd = null;

            if (targetId > -1)
            {
                cmd = AttackCommand.StaticCreate(mSelectedNetowrkId, targetId);
            }
            if (cmd == null)
            {
                cmd = MoveCommand.StaticCreate(mSelectedNetowrkId, _inTargerPosition);
            }

            if (cmd != null) mCommandList.AddCommand(cmd);
        }
    }

    private void RightClickCommandv2(Vector3 _inTargetPosition, ActionType _inAction, int _targetId)
    {
        if (mSelectedNetowrkId > -1)
        {

            Command cmd = null;
            if (_inAction == ActionType.Attack && _targetId > -1)
            {
                cmd = AttackCommand.StaticCreate(mSelectedNetowrkId, _targetId);
            }
            else if (_inAction == ActionType.Move)
            {
                cmd = MoveCommand.StaticCreate(mSelectedNetowrkId, _inTargetPosition);
            }

            if (cmd != null) mCommandList.AddCommand(cmd);
        }
    }

    public void HandleInput(KeyCode _inKey)
    {
        switch (_inKey)
        {
            case KeyCode.Return:
                {
                    // start game

                    break;
                }
        }
    }

    public void HandleSpawnGameobject(Vector3 _position, int _objectid)
    {
        Command cmd = null;
        cmd = SpawnCommand.StaticCreate(_position, _objectid);
        mCommandList.AddCommand(cmd);
    }
}
