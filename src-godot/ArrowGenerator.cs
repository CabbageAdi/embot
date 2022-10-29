using Godot;
using System;

public class ArrowGenerator : MeshInstance
{
    [Export] public float Delay;
    [Export] public PackedScene ArrowScene;
    
    public override void _Ready()
    {
        
    }

    private float _prevTime = 0;

    public override void _Process(float delta)
    {
        if (Time.GetTicksMsec() >= _prevTime + Delay)
        {
            GD.Print("generated");
            var arrow = (RigidBody)ArrowScene.Instance();
            arrow.Translation = Vector3.Forward * 4;
            this.AddChild(arrow);
            _prevTime = Time.GetTicksMsec();
        }
    }
}
