using Godot;
using Godot.Collections;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public partial class Guard : CharacterBody3D
{
	// General variables
	NavigationAgent3D navigationAgent;
	const float delayTime = 0.5f;
	float delay;
	// Movement variables
	const float deltaAccel = 3.5f;
	const float deltaRotSpeed = 2f;
	const float oppositeThreshold = Mathf.Pi / 6f;
	[Export] public float chaseSpeed = 7f;
	[Export] public float patrolSpeed = 5f;
	float speed;
	// Pathfinding variables
	float time;
	bool targetReached;
	Vector3 playerPosition;
	const float playerDistanceDelta = 0.1f;
	// Chase variables
	[Export] public float persistenceTime = 2f;
	[Export] public float attackRange = 3f;
	float timer;
	bool chasing;
	// Patrol variables
	[Export] public Array<Node3D> patrolPoints;
	[Export] public Dictionary<int, float> overrideWaitTimes;
	[Export] public float patrolPointWaitTime = 0f;
	const float arrivalRange = 2f;
	float waitTime;
	int currentPatrolPoint;
	
	
	public override void _Ready()
	{
		// Add to group
		AddToGroup("Guards");

		// Set up navigation agent
		navigationAgent = GetChild<NavigationAgent3D>(0);
		navigationAgent.VelocityComputed += SafeVelocityCalculated;
		// Set up vision
		Vision vision = GetChild<Vision>(1);
		vision.PlayerSeen += ChasePlayer;
		// Pathfinding
		targetReached = false;
		// Movement
		speed = 0f;
		// Chase
		chasing = false;
		// Patrol
		currentPatrolPoint = 0;
		waitTime = 0f;
		navigationAgent.TargetPosition = patrolPoints[0].GlobalPosition; 
		
		delay = 0f;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (delay > delayTime)
		{
			time = (float) delta;
			// Mangage guard chase state
			ManagePersistence((float) delta);
			// Manage states
			SetVariables();
			if (!chasing)
				Patrol((float) delta);
			// Manage movement and rotation in both chase and patrol state
			SetSpeed((float) delta);
			SetRotation((float) delta);

			IsTargetReached();
		}
		else
		{
			delay += (float) delta;
		}
	}

	//------------------------------------------------------------------------

	public void ChasePlayer(Vector3 position)
	{
		playerPosition = position;
		// Set up persistence timer
		chasing = true;
		timer = 0f;
		// Update target position if player has moved a decent distance
		if (navigationAgent.TargetPosition.DistanceTo(playerPosition) > playerDistanceDelta)
			navigationAgent.TargetPosition = playerPosition;
	}
	
	public void SafeVelocityCalculated(Vector3 safeVelocity)
	{
		Vector3 forward = Vector3.Forward.Rotated(Vector3.Up, Rotation.Y);
		Vector3 safe = safeVelocity.Project(forward);

		float safeSpeed = safe.Length();
		speed = speed + Mathf.Clamp(safeSpeed - speed, - time * deltaAccel, time * deltaAccel);

		Velocity = safe.Normalized() * speed;
		Velocity = safeVelocity;
		MoveAndSlide();
	}

	void TargetReached()
	{
		if (chasing)
		{
			GD.Print(Name + ": Bang");
		}
	}
	//------------------------------------------------------------------------

	void ManagePersistence(float deltaTime)
	{
		if (timer < persistenceTime)
			timer += deltaTime;
		else
			chasing = false;
	}

	void SetVariables()
	{
		if (chasing)
		{
			navigationAgent.TargetDesiredDistance = attackRange;
		}
		else
		{
			navigationAgent.TargetDesiredDistance = arrivalRange;
		}
	}

	void Patrol(float deltaTime)
	{
		if (targetReached && navigationAgent.TargetPosition == patrolPoints[currentPatrolPoint].GlobalPosition)
		{
			navigationAgent.Velocity = navigationAgent.TargetPosition - GlobalPosition;

			waitTime += deltaTime;

			float time = patrolPointWaitTime;

			if (overrideWaitTimes != null && overrideWaitTimes.ContainsKey(currentPatrolPoint))
				time = overrideWaitTimes[currentPatrolPoint];

			if (waitTime > time)
			{
				waitTime = 0f;
				currentPatrolPoint++;

				if (currentPatrolPoint >= patrolPoints.Count)
					currentPatrolPoint = 0;
			}
			else
				SetRotationAngle(deltaTime, patrolPoints[currentPatrolPoint].Rotation.Y, false);
		}
		else
		{
			navigationAgent.TargetPosition = patrolPoints[currentPatrolPoint].GlobalPosition;
			SetRotationVector3(deltaTime, navigationAgent.GetNextPathPosition());
		}
	}

	void SetSpeed(float deltaTime)
	{
		Vector3 forward = Vector3.Forward.Rotated(Vector3.Up, Rotation.Y);
		Vector3 targetPosition = navigationAgent.TargetPosition;

		float targetSpeed;
		float deltaSpeed = deltaAccel * deltaTime;
		
		// Set target speed
		if (targetReached || IsOppositeDirection())
		{
			targetSpeed = 0f;
		}
		else if (chasing)
		{
			targetSpeed = chaseSpeed;
		}
		else
		{
			targetSpeed = patrolSpeed;
		}
		// Set speed
		//speed += Mathf.Clamp(targetSpeed - speed, -RampUpSpeed(deltaSpeed, GlobalPosition.DistanceTo(targetPosition)), deltaSpeed);
		if (targetReached)
		{
			speed += Mathf.Clamp(targetSpeed - speed, -deltaSpeed * (3f + (navigationAgent.TargetDesiredDistance + GlobalPosition.DistanceTo(targetPosition))), deltaSpeed);
		}
		else
			speed += Mathf.Clamp(targetSpeed - speed, -deltaSpeed, deltaSpeed);
		navigationAgent.Velocity = forward * speed;
	}

	bool IsOppositeDirection()
	{
		Vector3 forward = Vector3.Forward.Rotated(Vector3.Up, Rotation.Y);
		float angle = forward.AngleTo(GlobalPosition.DirectionTo(navigationAgent.GetNextPathPosition()));
		return angle > oppositeThreshold;
	}

	void IsTargetReached()
	{
		if (navigationAgent.DistanceToTarget() <= navigationAgent.TargetDesiredDistance)
		{
			targetReached = true;
			TargetReached();
		}
		else
			targetReached = false;
	}

	float RampUpSpeed(float deltaSpeed, float distance)
	{
		float x = Mathf.Clamp(distance - 1f, 0f, distance) / navigationAgent.TargetDesiredDistance * 0.25f;

		return Mathf.Clamp(deltaSpeed / x, deltaSpeed, Mathf.Inf);
	}

	void SetRotation(float deltaTime)
	{
		if (chasing)
			SetRotationVector3(deltaTime, navigationAgent.GetNextPathPosition());
	}

	void SetRotationVector3(float deltaTime, Vector3 target)
	{
		Vector3 forward = Vector3.Forward.Rotated(Vector3.Up, Rotation.Y);
		Vector3 direction = GlobalPosition.DirectionTo(target);
		direction = new Vector3(direction.X, 0f, direction.Z).Normalized();

		float angleDifference = Mathf.Sign(Mathf.Sign(forward.Cross(direction).Y) * 2f + 1f) * forward.AngleTo(direction);
		float deltaAngle = deltaRotSpeed * deltaTime;
		
		RotateY(Mathf.Clamp(angleDifference, -deltaAngle, deltaAngle));
	}

	void SetRotationAngle(float deltaTime, float target, bool deg)
	{
		float targetAngle = target;

		// If angle deg, turn into radian
		if (deg)
			targetAngle = Mathf.DegToRad(targetAngle);

		float angleDifference = Mathf.AngleDifference(Rotation.Y, targetAngle);
		float deltaAngle = deltaRotSpeed * deltaTime;

		RotateY(Mathf.Clamp(angleDifference, -deltaAngle, deltaAngle));
	}

	float RampUpRotSpeed(float deltaAngle, float absDifference)
	{
		GD.Print(Mathf.Pi / 6f + " " + Mathf.Clamp(absDifference % (Mathf.Pi * 2f), 1f, Mathf.Inf) + " " + absDifference % 2);
		return deltaAngle * Mathf.Clamp(absDifference % (Mathf.Pi * 2f), 1f, Mathf.Inf);
	}
}
