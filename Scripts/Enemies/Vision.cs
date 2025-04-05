using Godot;
using Godot.Collections;
using System;

public partial class Vision : Area3D
{
	// General variables
	[Signal] public delegate void PlayerSeenEventHandler(Vector3 playerPosition);
	// Vision variables
	Array<Node3D> visionResults;
	Array<Node3D> focusVisionResults;
	Node3D parent;
	// Player vision variables
	const float awarenessGrowth = 10f;
	const float maxAwareness = 20f;
	float awareness;
	bool playerSeen;

	
	public override void _Ready()
	{
		parent = GetParent<Node3D>();
		visionResults = new();
		focusVisionResults = new();

		playerSeen = false;
	}

	
	public override void _PhysicsProcess(double delta)
	{
		// Emit raycasts to all objects that enter vision cone, updating vision results
		ManageVision();
		// Check is player is in the arrays
		IsPlayerFound((float) delta);
	}

	void ManageVision()
	{
		int idx = 0;
		int focusIdx = 0;
		var results = GetOverlappingBodies();
		var focusResults = results.Duplicate();

		while (idx < results.Count || focusIdx < focusResults.Count)
		{
			// If raycast returns true: object is not seen
			// Unfocused
			if (idx < results.Count)
			{
				if (!RayCast(results[idx].GlobalPosition, false))
				{
					results.RemoveAt(idx);
				}
				else
				{
					idx++;
				}
			}
			// Focused
			if (focusIdx < focusResults.Count)
			{
				if (!RayCast(focusResults[focusIdx].GlobalPosition, true))
				{
					focusResults.RemoveAt(focusIdx);
				}
				else
				{
					focusIdx++;
				}
			}
		}

		visionResults = results;
		focusVisionResults = focusResults;
	}

	bool RayCast(Vector3 target, bool focus)
	{
		PhysicsDirectSpaceState3D space = GetWorld3D().DirectSpaceState;
		PhysicsRayQueryParameters3D ray;

		if (focus)
			ray = PhysicsRayQueryParameters3D.Create(parent.GlobalPosition, target, 0b00000000_00000000_00000000_00001000);
		else
			ray = PhysicsRayQueryParameters3D.Create(parent.GlobalPosition, target, 0b00000000_00000000_00000000_00001100);

		var result = space.IntersectRay(ray);

		return result.Count == 0; // True: collision
	}

	void IsPlayerFound(float deltaTime)
	{
		bool playerInResults = false;
		Array<Node3D> results;
		Vector3 playerPosition = GlobalPosition + Vector3.Up * (maxAwareness + 10f);
		float deltaAwareness = awarenessGrowth * deltaTime;

		// Set array depending on focus
		if (playerSeen)
			results = focusVisionResults;
		else
			results = visionResults;

		// Look for player
		foreach (Node3D node in results)
		{
			if (node.Name == "Player")
			{
				playerInResults = true;
				playerPosition = node.GlobalPosition;
				break;
			}
		}

		// Increase awareness if player is in vision, otherwise decrease
		if (playerInResults || playerSeen)
		{
			if (awareness < maxAwareness)
				awareness += deltaAwareness;
			else
				awareness = maxAwareness;
		}
		else
		{
			if (awareness > 0f)
				awareness -= deltaAwareness;
			else
				awareness = 0f;
		}

		// Set playerSeen variable
		if (awareness >= parent.GlobalPosition.DistanceTo(playerPosition))
		{
			EmitSignal(SignalName.PlayerSeen, playerPosition);
			GetTree().CallGroup("Guards", "ChasePlayer", playerPosition);
			playerSeen = true;
		}
		else
		{
			playerSeen = false;
		}
	}
}
