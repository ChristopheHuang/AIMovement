using System;
using System.Collections.Generic;
using Navigation;
using UnityEngine;
using UnityEngine.Tilemaps;
using Node = UnityEditor.Experimental.GraphView.Node;
using Random = System.Random;

public class Movement : MonoBehaviour {
	
	public enum MoveState
	{
		Normal,    
		Wander,
		Follower,
		Avoid,
		Flee
	}
	
	public List<Vector3> pathToTarget;
	Navigation.Node closestNode = null;
	void FindClosestNodeToSetStartPoint(List<Navigation.Node> nodes)
	{
		closestNode = null;
		float closestDistance = float.MaxValue;

		foreach (var node in nodes)
		{
			float distance = Vector3.Distance(node.Position, transform.position + transform.forward);
			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestNode = node;
			}
		}
	}
	
	Navigation.Node targetNode = null;
	Navigation.Node currentNode = null;
	
	public void FindPathToTarget()
	{
		pathToTarget.Clear();
		TileNavGraph tileNavGraphInstance = TileNavGraph.Instance;
    
		if (tileNavGraphInstance)
		{
			FindClosestNodeToSetStartPoint(tileNavGraphInstance.LNode);
			currentNode = tileNavGraphInstance.GetNode(closestNode.Position);
			targetNode = tileNavGraphInstance.GetNode(targetPos);
        
			List<Navigation.Node> foundPath = AStar.FindPath(currentNode, targetNode, tileNavGraphInstance);
        
			if (foundPath != null && foundPath.Count > 0)
			{
				foreach (var node in foundPath)
				{
					pathToTarget.Add(node.Position);
				}
			}
		}
		
	}
	public MoveState CurrentState = MoveState.Normal;
	
    [SerializeField]
    private float MaxSpeed = 20f;
    private Vector3 velocity = Vector3.zero;
    private float posOffsetY = 0.5f;
    private Vector3 targetPos;
    public Vector3 TargetPos
    {
        get { return targetPos; }
        set { targetPos = value; targetPos.y += posOffsetY; }
    }
    
	private MoveState lastState;

	private void Start()
	{
		currentTimer = 0.0f;
		lastState = CurrentState;
	}

	private void Update ()
	{
	    if (lastState != CurrentState)
	    {
		    FindPathToTarget();
		    lastState = CurrentState;
	    }

	    switch (CurrentState)
	    {
		    case MoveState.Normal:
			    Seek();
			    break;
		    case MoveState.Wander:
			    // Wander();
			    break;
		    case MoveState.Follower:
			    FollowLeader();
			    break;
		    case MoveState.Avoid:
			    Avoid();
			    break;
		    case MoveState.Flee:
			    Flee();
			    break;
	    }
    }
	
	public float maxForce = 5;
	public float mass = 30;
	public float slowDownRadius = 2.0f;
	public float stopRadius = 1.0f;
	private bool isArrivedCurrentPos;
	
	public void Seek()
	{
		if (pathToTarget.Count > 0)
			targetPos = pathToTarget[0];
		
		float distanceToTarget = Vector3.Distance(transform.position, targetPos);
		Vector3 desiredDirection = targetPos - transform.position;
		
		
		float slowingFactor = 1;
		if (distanceToTarget < slowDownRadius)
		{
			slowingFactor = distanceToTarget / slowDownRadius;
			if (pathToTarget.Count > 0)
				pathToTarget.RemoveAt(0);
		}

		Vector3 desiredVelocity = desiredDirection.normalized * MaxSpeed * slowingFactor;
		Vector3 steering = desiredVelocity - velocity;
		steering = Vector3.ClampMagnitude(steering, maxForce);
		steering /= mass;
		velocity = Vector3.ClampMagnitude(velocity + steering, MaxSpeed);

		if (distanceToTarget < stopRadius)
		{
			velocity = Vector3.zero;
		}

		transform.position += velocity * Time.deltaTime;
		
		if (velocity != Vector3.zero) { transform.forward = velocity.normalized; }
		
	}
	
	[Header("Wander Settings")]
	private float currentTimer = 0.0f;
	public float wanderRange = 10.0f;
	
	public void Wander()
	{
		currentTimer = 0.0f;
		// Random patrol point
		targetPos =  new Vector3(UnityEngine.Random.Range(-wanderRange, wanderRange), // Axis x
								 transform.position.y,								  // Axis y 2d movement so always 0
								 UnityEngine.Random.Range(-wanderRange, wanderRange));// Axis z
	}
	
#region Avoid settings
	[Header("Avoid Settings")]
	public Vector3 obstaclePos;
	private Vector3 avoidanceDirection;
	Vector3 avoidPos = Vector3.zero;
	public float avoidDistance = 5;
	public float rayDistance = 2.0f;
	
	public void Avoid()
	{
		Vector3 directionToObstacle = obstaclePos - transform.position;
		Vector3 forwardDirection = transform.forward;
		Vector3 crossProduct = Vector3.Cross(forwardDirection, directionToObstacle);


		if (crossProduct.y > 0 && avoidPos == Vector3.zero) // Means obstacle is on unit's right side
		{
			avoidPos = obstaclePos - transform.right * avoidDistance;
		}
		else if (crossProduct.y < 0 && avoidPos == Vector3.zero) // Means obstacle is on unit's left side
		{
			avoidPos = obstaclePos + transform.right * avoidDistance;
		}
		
		Vector3 desiredDirection = (avoidPos - transform.position).normalized;
		Vector3 desiredVelocity = desiredDirection * MaxSpeed;
		Vector3 steering = desiredVelocity - velocity;
		steering = Vector3.ClampMagnitude(steering, maxForce); 
		steering /= mass;
		velocity = Vector3.ClampMagnitude(velocity + steering, MaxSpeed);
		
		transform.position += velocity * Time.deltaTime;
		if (velocity != Vector3.zero)
		{
			transform.forward = velocity.normalized; 
		}

		float distanceToAvoidPos = Vector3.Distance(transform.position, avoidPos);
		if (distanceToAvoidPos < 2)
		{
			avoidPos = Vector3.zero;
			CurrentState = lastState;
		}
	}
#endregion Avoid settings
	
	public Vector3 fleePos;

	public void Flee()
	{
		float distanceToFleePos = Vector3.Distance(transform.position, fleePos);
		
		if (distanceToFleePos < 5)
		{
			Vector3 fleeDirection = Vector3.zero;
			if (fleePos != Vector3.zero)
			{
				fleeDirection = transform.position - fleePos;
			}

			Vector3 desiredDirection = (fleeDirection - transform.position).normalized;
			Vector3 desiredVelocity = desiredDirection * MaxSpeed * 10;
			Vector3 steering = desiredVelocity - velocity;
			steering = Vector3.ClampMagnitude(steering, maxForce);
			steering /= mass;
			velocity = Vector3.ClampMagnitude(velocity + steering, MaxSpeed * 10);
		}
	}
	
#region Follower settings
	public enum Format
	{
		Cube,
		Circle,
		Line
	}
	
	[Header("Follow Settings")]
	public Format format = Format.Circle;
	public int followCount = 0;
	public Unit leaderUnit;
	public int followNumber;
	public float followDistance = 3.0f;
	public float rowDistance = 2.0f;
	public List<Node> banNodes;
	
	public void FollowLeader()
	{
		switch (leaderUnit.movement.format)
		{
			case Format.Cube:
				if (leaderUnit.movement.velocity != Vector3.zero)
				{
					int rowIndex = followNumber / 5;
					int positionInRow = followNumber % 5;
					Vector3 newTargetPos = leaderUnit.transform.position - leaderUnit.transform.forward * (rowIndex + 1) * rowDistance;
					
					if (TileNavGraph.Instance.IsPosValid(newTargetPos) &&
                        TileNavGraph.Instance.IsNodeWalkable(TileNavGraph.Instance.GetNode(newTargetPos)))
					targetPos = TileNavGraph.Instance.GetNode(newTargetPos).Position;
					// targetPos = leaderUnit.transform.position - leaderUnit.transform.forward * (rowIndex + 1) * rowDistance;

					if (positionInRow % 2 == 0)
					{
						Vector3 newTargetPosition = targetPos +
						                            leaderUnit.transform.right * (positionInRow * 0.5f) *
						                            followDistance * 0.5f;
						
						if (TileNavGraph.Instance.IsPosValid(newTargetPosition) &&
                            TileNavGraph.Instance.IsNodeWalkable(TileNavGraph.Instance.GetNode(newTargetPosition)))
						targetPos = TileNavGraph.Instance.GetNode(newTargetPosition).Position;
						// targetPos += leaderUnit.transform.right * (positionInRow * 0.5f) * followDistance * 0.5f;
					}
					else
					{
						Vector3 newTargetPosition = targetPos -
						                            leaderUnit.transform.right * (positionInRow * 0.5f) *
						                            followDistance * 0.5f;
						
						if (TileNavGraph.Instance.IsPosValid(newTargetPosition) &&
                            TileNavGraph.Instance.IsNodeWalkable(TileNavGraph.Instance.GetNode(newTargetPosition)))
						targetPos = TileNavGraph.Instance.GetNode(newTargetPosition).Position;
						// targetPos -= leaderUnit.transform.right * ((positionInRow + 1) * 0.5f) * followDistance * 0.5f;
					}
				}
				break;
			case Format.Circle:
				if (leaderUnit.movement.velocity != Vector3.zero)
				{
					int totalUnits = leaderUnit.movement.followCount;

					float angleStep = 360.0f / totalUnits;
					float currentAngle = followNumber * angleStep;

					float radians = currentAngle * Mathf.Deg2Rad;

					Vector3 newTargetPos = leaderUnit.transform.position + new Vector3(
						Mathf.Cos(radians) * followDistance,
						0,
						Mathf.Sin(radians) * followDistance
					);
					
					if (TileNavGraph.Instance.IsPosValid(newTargetPos) && 
					    TileNavGraph.Instance.IsNodeWalkable(TileNavGraph.Instance.GetNode(newTargetPos)))
					targetPos = TileNavGraph.Instance.GetNode(newTargetPos).Position;
					// targetPos = leaderUnit.transform.position + new Vector3(
					// 	Mathf.Cos(radians) * followDistance,
					// 	0,
					// 	Mathf.Sin(radians) * followDistance
					// );
				}
				break;
			case Format.Line:
				// TODO
				
				break;
		}
		
		float distanceToTarget = Vector3.Distance(transform.position, targetPos);
		Vector3 desiredDirection = targetPos - transform.position;
		
		float slowingFactor = 1;
		if (distanceToTarget < slowDownRadius)
		{
			slowingFactor = distanceToTarget / slowDownRadius;
			if (pathToTarget.Count > 0)
				pathToTarget.RemoveAt(0);
		}

		Vector3 desiredVelocity = desiredDirection.normalized * MaxSpeed * slowingFactor;
		Vector3 steering = desiredVelocity - velocity;
		steering = Vector3.ClampMagnitude(steering, maxForce);
		steering /= mass;
		velocity = Vector3.ClampMagnitude(velocity + steering, MaxSpeed);

		if (distanceToTarget < stopRadius)
		{
			velocity = Vector3.zero;
		}
		
		transform.position += velocity * Time.deltaTime;
		if (velocity != Vector3.zero) { transform.forward = velocity.normalized; }
	}
#endregion Follower settings
	
	public void Stop()
	{
		velocity = Vector3.zero;
	}
	
    private void OnDrawGizmos()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawRay(transform.position, velocity);
		if (pathToTarget.Count > 0)
			foreach (var point in pathToTarget)
			{
				Gizmos.color = Color.magenta;
				Gizmos.DrawCube(point, Vector3.one);
			}
	}
}
