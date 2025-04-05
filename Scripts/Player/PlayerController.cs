using Godot;
using System;

public partial class PlayerController : CharacterBody3D
{
	// General reference variables
	CameraManager playerCamera;
	CollisionShape3D playerCollision;
	StaticBody3D hideBox;
	// Camera control related variables
	[Export] public Vector3 normalCameraOffset = new Vector3(0f, 10f, 6f);
	Vector3 currentTarget;
	const float deltaCameraAccel = 5f;
	// Player movement variables
	[Export] public float sprintSpeed = 8f;
	[Export] public float runSpeed = 5.0f;
	[Export] public float walkSpeed = 2.5f;
	float speed;
	Vector3 direction;
	const float deltaAccel = 10f;
	const float deltaRotAccel = 5f;
	// Player state variables
	public bool sprinting;
	public bool running;
	public bool walking;
	public bool crouching;
	public bool hiding;
	// Player model / collision variables
	[Export] public float crouchScale = 0.65f;
	const float deltaScaleAccel = 5f;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    public override void _Ready()
    {
		playerCamera = GetNode<CameraManager>("/root/" + GetTree().Root.GetChild(0).Name + "/Game Manager/Player Camera");
		hideBox = GetChild<StaticBody3D>(0);
		playerCollision = GetChild<CollisionShape3D>(1);
		currentTarget = GlobalPosition;
        speed = 0f;
    }

    void PlayerStateSynchronizer()
	{
		// Manage walking / running / sprinting
		if (Input.IsActionPressed("Walk"))
		{
			walking = true;
			running = false;
			sprinting = false;
		}
		else if (Input.IsActionPressed("Sprint"))
		{
			walking = false;
			running = false;
			sprinting = true;
		}
		else
		{
			walking = false;
			running = true;
			sprinting = false;
		}

		// Manage crouching / sliding
		if (Input.IsActionPressed("Crouch"))
		{
			hiding = true;
			crouching = true;
		}
		else
		{
			hiding = false;
			crouching = false;
		}

		// Manage hiding
		if (hiding && !GetChildren().Contains(hideBox))
		{
			AddChild(hideBox);
		}
		else if (!hiding && GetChildren().Contains(hideBox))
		{
			RemoveChild(hideBox);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Set player in correct states
		PlayerStateSynchronizer();
		SetScale((float) delta);

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		// Get the input direction and handle the movement/deceleration.
		SetDirection((float) delta);
		SetSpeed((float) delta);
		//TODO: Model rotation
		if (direction != Vector3.Zero)
		{
			velocity.X = Mathf.MoveToward(Velocity.X, direction.X * speed, speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, direction.Z * speed, speed);
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, speed);
		}

		Velocity = velocity;
		MoveAndSlide();

		// Perch camera in correct position
		// TODO: Add smoothing to camera
		SetCamera((float) delta);
	}

	void SetSpeed(float deltaTime)
	{
		float difference;
		float value;
		int sign;

		if (crouching)
		{
			difference = speed - walkSpeed;
		}
		else
		{
			if (sprinting)
			{
				difference = speed - sprintSpeed;
			}
			else if (walking)
			{
				difference = speed - walkSpeed;
			}
			else if (running)
			{
				difference = speed - runSpeed;
			}
			else
			{
				difference = 0f;
			}
		}

		value = Mathf.Clamp(Mathf.Abs(difference), 0f, deltaAccel * deltaTime);
		sign = Mathf.Sign(difference);

		speed -= sign * value;
	}

	void SetDirection(float deltaTime)
	{
		Vector2 inputDir2D = Input.GetVector("Left", "Right", "Up", "Down").Rotated(-playerCamera.Rotation.Y);
		Vector3 inputDir3D = new Vector3(inputDir2D.X, 0, inputDir2D.Y);
		
		direction = direction.MoveToward(inputDir3D, deltaRotAccel * deltaTime);
	}

	void SetScale(float deltaTime)
	{
		//TODO: use playerCollision.Scale when model is available
		if (crouching)
		{
			Scale = Scale.MoveToward(new Vector3(1f, crouchScale, 1f), deltaScaleAccel * deltaTime);
			if (!IsOnFloor())
				GlobalPosition -= Vector3.Up * deltaScaleAccel * deltaTime / 2;
		}
		else
		{
			Scale = Scale.MoveToward(Vector3.One, deltaScaleAccel * deltaTime);
		}
	}

	void SetCamera(float deltaTime)
	{
		//? Check gamefeel and see if camera smoothing is essential
		// float distance = (GlobalPosition - currentTarget).Length();
		// currentTarget = currentTarget.MoveToward(GlobalPosition, deltaTime * deltaCameraAccel);
		playerCamera.PerchCamera(GlobalPosition, normalCameraOffset);
	}
}
