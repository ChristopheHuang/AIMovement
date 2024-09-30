using System;
using UnityEngine;
using System.Collections;
using AOT;

public class Unit : SelectableEntity
{
    public Movement movement;
    
    private Unit leader;
    
    override protected void Awake()
    {
        base.Awake();
        movement = GetComponent<Movement>();
	}

    public void SetTargetPos(Vector3 pos)
    {
        movement.TargetPos = pos;
    }

    public void SwicthState()
    {
        if (movement.CurrentState == Movement.MoveState.Normal)
            movement.CurrentState = Movement.MoveState.Wander;
        else if (movement.CurrentState == Movement.MoveState.Wander)
            movement.CurrentState = Movement.MoveState.Normal;
    }   

    public void SetLeader(Unit newLeader)
    {
        movement.CurrentState = Movement.MoveState.Follower;
        leader = newLeader;
        if (leader)
        {
            SetTargetPos(leader.transform.position);
            movement.leaderUnit = leader;
        }
    }

    public Unit GetLeader()
    {
        return leader;
    }
}
