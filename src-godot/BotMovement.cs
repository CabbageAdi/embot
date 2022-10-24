using Godot;
using System;

public class BotMovement : RigidBody
{
    [Export] public float Speed;
    [Export] public float AngularSpeed;

    public float MaxDist = 50;
    
    public override void _Ready()
    {
    }

    private float prevDistF = 0;
    private float prevDistL = 0;
    private float prevDistR = 0;

    public override void _Process(float delta)
    {
        //movement
        var v = this.LinearVelocity;
        if (Input.IsKeyPressed((int)KeyList.Up) || (JavaScript.Eval($"pinVal(0)") as int?) == 1)
        {
            v.z = -Speed;
        }
        if (Input.IsKeyPressed((int)KeyList.Down))
        {
            v.z = Speed;
        }
        if (Input.IsKeyPressed((int)KeyList.Left))
        {
            v.x = -Speed;
        }
        if (Input.IsKeyPressed((int)KeyList.Right))
        {
            v.x = Speed;
        }
        var av = this.AngularVelocity;
        if (Input.IsKeyPressed((int)KeyList.X))
        {
            av.y = -AngularSpeed;
        }
        else
        {
            av.y = 0;
        }
        
        this.LinearVelocity = v;
        this.AngularVelocity = av;
        
        //distances
        var screenSpace = this.GetWorld().DirectSpaceState;

        var cos = Mathf.Cos(this.Rotation.y);
        var sin = Mathf.Sin(this.Rotation.y);
        var forward = screenSpace.IntersectRay(this.Translation, this.Translation + Vector3.Forward * MaxDist * cos + Vector3.Left * MaxDist * sin);
        var left = screenSpace.IntersectRay(this.Translation, this.Translation + Vector3.Left * MaxDist * cos + Vector3.Back * MaxDist * sin);
        var right = screenSpace.IntersectRay(this.Translation, this.Translation + Vector3.Right * MaxDist * cos + Vector3.Forward * MaxDist * sin);

        var fdist = 
            Mathf.Round(((Vector3)forward["position"] - this.Translation).Round().Length()) - 2;
        var ldist = 
            Mathf.Round(((Vector3)left["position"] - this.Translation).Round().Length()) - 1;
        var rdist = 
            Mathf.Round(((Vector3)right["position"] - this.Translation).Round().Length()) - 1;

        fdist /= 10;
        ldist /= 10;
        rdist /= 10;

        if (fdist != prevDistF)
        {
            JavaScript.Eval($"setPin(0, {fdist})", true);
        }
        if (ldist != prevDistL)
        {
            JavaScript.Eval($"setPin(1, {ldist})", true);
        }
        if (rdist != prevDistR)
        {
            JavaScript.Eval($"setPin(22 {rdist})", true);
        }
        GD.Print($"{fdist}, {ldist}, {rdist}, {this.RotationDegrees.y}");

        prevDistF = fdist;
        prevDistL = ldist;
        prevDistR = rdist;

        //sections
        var down = screenSpace.IntersectRay(this.Translation, this.Translation + Vector3.Down * 50);
        var sectionName = ((StaticBody)down["collider"]).Name;
        GD.Print(sectionName);
    }
}

