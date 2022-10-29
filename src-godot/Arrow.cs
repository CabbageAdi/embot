using Godot;
using System;
using Embot;

public class Arrow : RigidBody
{
    [Export] public float Speed;
    
    public override void _Ready()
    {
        this.Connect(Signal.BodyEntered.GetName(), this, nameof(Connected));
    }

    public override void _Process(float delta)
    {
        this.LinearVelocity = Vector3.Forward.Rotated(Vector3.Up, this.GlobalRotation.y) * delta * Speed;
    }

    public void Connected(Node body)
    {
        this.QueueFree();
    }
}
