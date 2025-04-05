using Godot;
using System;

public partial class CameraManager : Camera3D
{
	public override void _Ready()
	{
	}

	
	public override void _Process(double delta)
	{
	}

	public void PerchCamera(Vector3 target, Vector3 offset)
	{
		GlobalPosition = target + offset;
		LookAt(target);
	}
}
