using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using cakeslice;

public class PlayerController : NetworkObject 
{

    public enum PlayerState
    {
        Move,
        Attack,
        None
    };
    private PlayerState mState;
    public GameObject UI;
    public NavMeshAgent Agent;
    private Camera cam;
    public Animator mAnimator;
    public Outline mOutline;
    public float WalkSpeed = 1.5f;
    public float RunSpeed = 4f;
    public float mDistanceToWalk = 4f;

    private float mSpeed;
    private Vector3 targetPosition = Vector3.zero;

    private GameObject mTargetOb;
    void Start()
    {
        mState = PlayerState.None;
        
        GameObject _CameraParent = GameObject.FindGameObjectWithTag("Cam");
        cam = _CameraParent.transform.GetChild(0).GetComponent<Camera>();
        mSpeed = WalkSpeed;
        targetPosition = transform.position;
        mOutline.enabled = false;
    }
    
    void Update()
    {
        RightClickAction();
        Move();
    }

    void Move()
    {
        Agent.SetDestination(targetPosition);

        if (Vector3.SqrMagnitude(transform.position - targetPosition) < mDistanceToWalk) mSpeed = WalkSpeed;
        else mSpeed = RunSpeed;

        Agent.speed = Mathf.Lerp(Agent.speed, mSpeed, Time.deltaTime * 10f);

        mAnimator.SetFloat("Speed", Agent.velocity.magnitude);

        if (mState == PlayerState.Attack && mTargetOb != null)
        {
            float distance = Vector3.SqrMagnitude(transform.position - mTargetOb.transform.position);
            if (distance <= 1.7 * 1.7)
            {
                mAnimator.SetBool("Attack", true);
                Quaternion rot = Quaternion.LookRotation(mTargetOb.transform.position - transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.deltaTime * 10f);
                Agent.updateRotation = false;
            }
            else
            {
                mAnimator.SetBool("Attack", false);
                mTargetOb = null;
                Agent.updateRotation = true;
                mState = PlayerState.None;
            }
        }
        else
        {
            Agent.updateRotation = true;
            mAnimator.SetBool("Attack", false);
        }
    }

    private void RightClickAction()
    {
        if (InputManager.mIntance != null)
        {
            InputManager.mIntance.SetSelectedNetworkId(mNetworkID);
        }

        Vector3 targetv3 = Vector3.zero;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Input.GetMouseButtonDown(1))
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 500f))
            {
                targetv3 = hit.point;
                GameObject gameob = hit.transform.root.gameObject;
                NetworkObject netob = gameob.GetComponent<NetworkObject>();
                if (InputManager.mIntance != null && netob == null)
                {
                    InputManager.mIntance.HandleMouseClick(targetv3, InputManager.RIGHT_MOUSE_BUTTON, InputManager.ActionType.Move);
                }
                else if (InputManager.mIntance != null && netob != null)
                {
                    int targetid = netob.GetNetWorkId();
                    InputManager.mIntance.HandleMouseClick(targetv3, InputManager.RIGHT_MOUSE_BUTTON, InputManager.ActionType.Attack, targetid);
                }
            }
        }
        
    }

    public override void ToMove(Vector3 _targetPosition)
    {
        Agent.stoppingDistance = 0f;
        mState = PlayerState.Move;
        targetPosition = _targetPosition;
        mTargetOb = null;
    }

    public override void ToAttack(int _inTargetId)
    {
        GameObject targetOb = NetworkManager_v2.mInstance.GetGameobjectByNetworkId(_inTargetId);
        if (targetOb != null)
        {
            mState = PlayerState.Attack;
            mTargetOb = targetOb;
            Agent.stoppingDistance = 1.7f;
            targetPosition = targetOb.transform.position;
        }
    }

    public override void WriteForCrc(ref NetworkTrungVI.OutputMemoryBitStream output)
    {
        base.WriteForCrc(ref output);

    }

    private void LeftClickAction()
    {
        
    }
}
