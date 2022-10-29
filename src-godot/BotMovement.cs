using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Embot;

public class BotMovement : RigidBody
{
    [Export] public float Speed;
    [Export] public float AngularSpeed;
    [Export] public NodePath[] Sections;
    [Export] public NodePath SectionTimePath;

    public MeshInstance Top;
    public CollisionShape TopShape;
    public CollisionShape BottomShape;
    public Label SectionTimeLabel;
    
    public float MaxDist = 50;
    public bool Height = false;
    public int Section = 0;
    public List<float> Times = new List<float>();
    
    public override void _Ready()
    {
        this.Connect(Signal.BodyEntered.GetName(), this, nameof(Collision));

        Top = this.GetNode<MeshInstance>(this.GetPath().ToString() + "/Top");
        TopShape = this.GetNode<CollisionShape>(this.GetPath().ToString() + "/TopShape");
        BottomShape = this.GetNode<CollisionShape>(this.GetPath().ToString() + "/BottomShape");
        SectionTimeLabel = this.GetNode<Label>(SectionTimePath);
    }

    private float prevDistF = 0;
    private float prevDistL = 0;
    private float prevDistR = 0;

    private int prevSec = -1;
    public override void _Process(float delta)
    {
        //movement
        var v = Vector3.Zero;
        if (Input.IsKeyPressed((int)KeyList.Up) || JavaScript.Eval($"pinVal(0)") as int? == 1)
        {
            v += Vector3.Forward.Rotated(Vector3.Up, this.Rotation.y) * Speed;
        }
        else if (Input.IsKeyPressed((int)KeyList.Down))
        {
            v += Vector3.Back.Rotated(Vector3.Up, this.Rotation.y) * Speed;
        }
        else
        {
            v.z = 0;
        }

        var av = this.AngularVelocity;
        if (Input.IsKeyPressed((int)KeyList.X))
        {
            av.y = -AngularSpeed;
        }
        else if (Input.IsKeyPressed((int)KeyList.Z))
        {
            av.y = AngularSpeed;
        }
        else
        {
            av.y = 0;
        }

        Height = (Input.IsKeyPressed((int)KeyList.C) || (JavaScript.Eval($"pinVal(5)") as int?) == 1);

        if (Height)
        {
            Top.Translation = new Vector3(Top.Translation.x, 3, Top.Translation.z);
            TopShape.Translation = new Vector3(Top.Translation.x, 3, Top.Translation.z);
        }
        else
        {
            Top.Translation = new Vector3(Top.Translation.x, 0, Top.Translation.z);
            TopShape.Translation = new Vector3(Top.Translation.x, 0, Top.Translation.z);
        }

        this.LinearVelocity = v;
        this.AngularVelocity = av;

        //distances
        var screenSpace = this.GetWorld().DirectSpaceState;

        var h = Height ? 5 : 0;
        var f1 = Raycast(new Vector3(0, h, -1.6f), Vector3.Forward);
        var f2 = Raycast(new Vector3(1, h, -1.6f), Vector3.Forward);
        var f3 = Raycast(new Vector3(-1, h, -1.6f), Vector3.Forward);
        
        var forward = new List<Vector3> { f1, f2, f3 }.OrderBy(r => r.Length()).First();

        var l1 = Raycast(new Vector3(-1, h, 0), Vector3.Left);
        var l2 = Raycast(new Vector3(-1, h, 1.7f), Vector3.Left);
        var l3 = Raycast(new Vector3(-1, h, -1.7f), Vector3.Left);

        var left = new List<Vector3> { l1, l2, l3 }.OrderBy(r => r.Length()).First();
        
        var r1 = Raycast(new Vector3(1, h, 0), Vector3.Right);
        var r2 = Raycast(new Vector3(1, h, 1.7f), Vector3.Right);
        var r3 = Raycast(new Vector3(1, h, -1.7f), Vector3.Right);

        var right = new List<Vector3> { r1, r2, r3 }.OrderBy(r => r.Length()).First();

        var fdist =
            Mathf.Round((forward - this.Translation).Length()) - 1;
        var ldist =
            Mathf.Round((left - this.Translation).Length()) - 1;
        var rdist =
            Mathf.Round((right - this.Translation).Length()) - 1;

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
            JavaScript.Eval($"setPin(2, {rdist})", true);
        }

        // GD.Print($"{fdist}, {ldist}, {rdist}, {this.RotationDegrees.y}");

        prevDistF = fdist;
        prevDistL = ldist;
        prevDistR = rdist;

        //sections
        var down = screenSpace.IntersectRay(this.Translation, this.Translation + Vector3.Down * 50, new Godot.Collections.Array(this));
        if (down.Count == 0) return;
        var sectionName = ((StaticBody)down["collider"]).Name;
        GD.Print(sectionName);
        
        if (sectionName.Contains("Arrow"))
        {
            JavaScript.Eval("setPin(11, 1)");
        }
        else
        {
            JavaScript.Eval("setPin(11, 0)");
        }
        
        Section = Array.IndexOf(Sections, Sections.First(s => s.ToString().Contains(sectionName)));
        
        if (Section > prevSec)
        {
            if (prevSec != -1) 
                Times[prevSec] = Time.GetTicksMsec();
            
            Times.Add(Time.GetTicksMsec());
            prevSec = Section;
        }

        if (prevSec >= Section && Times.Count > Section)
        {
            Section = prevSec;
        }
        
        var labelText = "";
        for (int i = 0; i < Times.Count; i++)
        {
            if (i == Section)
            {
                labelText += TimeSpan.FromMilliseconds(Time.GetTicksMsec() - Times[i]).ToString(@"mm\:ss\:ff");
            }
            else
            {
                labelText += TimeSpan.FromMilliseconds(Times[i]).ToString(@"mm\:ss\:ff");
            }
            labelText += "\n";
        }
        
        SectionTimeLabel.Text = labelText;
    }

    private Vector3 Raycast(Vector3 startOffset, Vector3 direction)
    {
        var screenSpace = this.GetWorld().DirectSpaceState;
        var start = this.Translation + startOffset.Rotated(Vector3.Up, this.Rotation.y);
        var ray = screenSpace.IntersectRay(start, start + direction.Rotated(Vector3.Up, this.Rotation.y) * MaxDist);
        if (ray.Count > 0)
        {
            return (Vector3)ray["position"];
        }
        else
        {
            return Vector3.Inf;
        }
    }

    public void Collision(Node body)
    {
        if (body.Name.Contains("Arrow"))
        {
            GD.Print("death");
            Section++;
        }
    }
}

