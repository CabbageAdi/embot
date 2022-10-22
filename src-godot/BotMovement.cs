using Godot;
using System;

public class BotMovement : RigidBody
{
    [Export] public float Speed;
    
    public override void _Ready()
    {
    }

    private int prevSec = 0;
    
    public override void _Process(float delta)
    {
        //movement
        var v = this.LinearVelocity;
        if (Input.IsKeyPressed((int)KeyList.Up))
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
        
        this.LinearVelocity = v;
        
        //raycasts
        var screenSpace = this.GetWorld().DirectSpaceState;

        var raycastResult = screenSpace.IntersectRay(this.Translation, Vector3.Back * 50);

        var distance = 
            Mathf.Round(((Vector3)raycastResult["position"] - this.Translation).Round().Length()) - 1;

        distance /= 5;

        JavaScript.Eval($"setPin(0, {distance})", true);
    }
}
