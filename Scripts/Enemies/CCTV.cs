using Godot;
using System;

public partial class CCTV : Node3D
{
	// CCTV rotation variables
	Vector3 rotationVector;
	[Export] public float tilt;
	[Export] public float from;
	[Export] public float to;
	[Export] public float rotationSpeed;
	[Export] public float waitTime;
	bool fromTo;
	float timeWaited;
	const float tolerance = 0.1f;
	// FollowPlayer variables
	[Export] public float lookAtPatience = 5f;
	float absentPlayerTime;

	
	public override void _Ready()
	{
		rotationVector = new Vector3(Mathf.DegToRad(tilt), Mathf.DegToRad(from), 0f);
		Rotation = rotationVector;

		Vision visionNode = GetChild<Vision>(0);
		visionNode.PlayerSeen += LookAtPlayer;

		absentPlayerTime = 0f;
	}

	
	public override void _PhysicsProcess(double delta)
	{
		// Rotate camera
		if (absentPlayerTime >= lookAtPatience)
			RotationManager((float) delta);
		else
			absentPlayerTime += (float) delta;
		// Update rotation
		Rotation = rotationVector;
	}

	void RotationManager(float deltaTime)
	{
		float deltaY;
		float deltaX;
		float deltaZ;
		float targetY;
		float targetX = Mathf.DegToRad(tilt);
		float speed = rotationSpeed * deltaTime;

		// Set target
		if (fromTo)
		{
			targetY = from;
		}
		else
		{
			targetY = to;
		}
		targetY = Mathf.DegToRad(targetY);

		// Calculate deltas
		deltaY = Mathf.AngleDifference(rotationVector.Y, targetY);
		deltaX = Mathf.AngleDifference(rotationVector.X, targetX);
		deltaZ = Mathf.AngleDifference(rotationVector.Z, 0f);

		// Rotate roationVector
		rotationVector += new Vector3(Mathf.Clamp(deltaX, -speed, speed), Mathf.Clamp(deltaY, -speed, speed), Mathf.Clamp(deltaZ, -speed, speed));

		// Wait if target reached
		if (Mathf.Abs(deltaY) < tolerance)
		{
			timeWaited += deltaTime;

			if (timeWaited >= waitTime)
			{
				fromTo = !fromTo;
				timeWaited = 0f;
			}
		}
	}

	void LookAtPlayer(Vector3 playerPosition)
	{
		// Reset absentPlayerTime
		absentPlayerTime = 0f;

		// Look towards player
		Transform = Transform.LookingAt(playerPosition);

		// Update rotation
		rotationVector = new Vector3(Rotation.X, Rotation.Y, 0f);
		Rotation = rotationVector;
	}
}
