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
    [Export] public Vector3 StartPos = new Vector3(0, 1, 0);
    [Export] public float[] SectionStartRotations;
    [Export] public Vector2[] SectionStartPoints;

    public MeshInstance Top;
    public CollisionShape TopShape;
    public CollisionShape BottomShape;
    public Label SectionTimeLabel;

    public float MaxDist = 50;
    public bool Up = false;
    public int Section = 0;
    public List<float> Times = new List<float>();
    public List<int> FailedSections = new List<int>();

    public float StartTime = 0;
    public bool Start = false;

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
    private float prevRot = 0;

    private int prevSec = -1;

    public override void _Process(float delta)
    {
        float newSpeed = 0;
        for (var i = 3; i < 11; i++){
            var pin = PinVal(i);
            if (pin){
                newSpeed += Mathf.Pow(2, (i - 3));
            }
        }
        var speed = (newSpeed / 255) * Speed;
        var angularSpeed = (newSpeed / 255) * AngularSpeed;

        if (!OS.HasFeature("JavaScript"))
        {
            speed = Speed;
            angularSpeed = AngularSpeed;
        }

        //movement
        var v = Vector3.Zero;
        if (PinVal(0) && !PinVal(1) && !PinVal(2))
        {
            v += Vector3.Forward.Rotated(Vector3.Up, this.Rotation.y) * speed;
        }
        else if (PinVal(0) && PinVal(1) && PinVal(2))
        {
            v += Vector3.Back.Rotated(Vector3.Up, this.Rotation.y) * speed;
        }
        else
        {
            v.z = 0;
        }

        var av = this.AngularVelocity;
        if (PinVal(0) && !PinVal(1) && PinVal(2))
        {
            av.y = -angularSpeed;
        }
        else if (PinVal(0) && PinVal(1) && !PinVal(2))
        {
            av.y = angularSpeed;
        }
        else
        {
            av.y = 0;
        }

        Up = PinVal(13) && Sections[Section].ToString().Contains("Blo");

        if (Up)
        {
            Top.Translation = new Vector3(Top.Translation.x, 1.566f, Top.Translation.z);
            TopShape.Translation = new Vector3(Top.Translation.x, 1.566f, Top.Translation.z);
        }
        else
        {
            Top.Translation = new Vector3(Top.Translation.x, 0.066f, Top.Translation.z);
            TopShape.Translation = new Vector3(Top.Translation.x, 0.066f, Top.Translation.z);
        }

        this.LinearVelocity = v;
        this.AngularVelocity = av;

        //distances
        var screenSpace = this.GetWorld().DirectSpaceState;

        var h = Up ? 1.5f : 0;
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
            Mathf.Round((forward - this.Translation).Length()) - 2;
        var ldist =
            Mathf.Round((left - this.Translation).Length()) - 1;
        var rdist =
            Mathf.Round((right - this.Translation).Length()) - 1;

        fdist /= 10;
        ldist /= 10;
        rdist /= 10;

        if (forward == Vector3.One * 1000 || fdist > 5) fdist = 5;
        if (left == Vector3.One * 1000 || ldist > 5) ldist = 5;
        if (right == Vector3.One * 1000 || rdist > 5) rdist = 5;

        if (fdist != prevDistF)
        {
            SetPin(0, fdist);
        }

        if (ldist != prevDistL)
        {
            SetPin(1, ldist);
        }

        if (rdist != prevDistR)
        {
            SetPin(2, rdist);
        }

        var rot = Mathf.Round(this.RotationDegrees.y);
        if (rot < 0)
        {
            rot = 360 + rot;
        }
        if (prevRot != rot)
        {
            SetPin(3, rot * (5f / 1023f));
        }

        // GD.Print($"{fdist}, {ldist}, {rdist}, {rot:00}");

        prevDistF = fdist;
        prevDistL = ldist;
        prevDistR = rdist;

        //sections
        var down = screenSpace.IntersectRay(this.Translation, this.Translation + Vector3.Down * 50,
            new Godot.Collections.Array(this));
        if (down.Count == 0) return;
        var sectionName = ((StaticBody)down["collider"]).Name;

        if (sectionName.Contains("Arr"))
        {
            SetPin(11, 1);
            SetPin(12, 0);
        }
        else if (sectionName.Contains("Blo"))
        {
            SetPin(11, 0);
            SetPin(12, 1);
        }
        else
        {
            SetPin(11, 0);
            SetPin(12, 0);
        }

        Section = Array.IndexOf(Sections, Sections.First(s => s.ToString().Contains(sectionName)));

        //timing
        
        //start run
        if (PinVal(14) && !Start)
        {
            StartTime = Time.GetTicksMsec();
            Start = true;
            this.Rotation = Vector3.Zero;
            Section = 0;
            Times = new List<float>();
            prevSec = -1;
            FailedSections = new List<int>();
            this.Translation = StartPos;
        }

        if (!PinVal(14) && Start)
        {
            this.Translation = StartPos;
            this.Rotation = Vector3.Zero;

            Times[Section] = Time.GetTicksMsec();
            
            var labelText = "";
            for (var i = 0; i < Times.Count; i++)
            {
                var t = Times[i];
                labelText += TimeSpan.FromMilliseconds(t - (i > 0 ? Times[i - 1] : StartTime)).ToString(@"mm\:ss\:ff");
                if (FailedSections.Contains(i))
                {
                    labelText += " (failed)";
                }
                labelText += "\n";
            }

            SectionTimeLabel.Text = labelText;

            Times = new List<float>();
            FailedSections = new List<int>();
            Section = 0;
            prevSec = -1;
            StartTime = 0;
            Start = false;
        }

        if (Start)
        {
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
                    labelText += TimeSpan.FromMilliseconds(Times[i] - (i > 0 ? Times[i - 1] : StartTime)).ToString(@"mm\:ss\:ff");
                    if (FailedSections.Contains(i))
                    {
                        labelText += " (failed)";
                    }
                }

                labelText += "\n";
            }

            SectionTimeLabel.Text = labelText;
        }

        if (sectionName.Contains("End"))
        {
            JavaScript.Eval($"setPinOut(14, 0)", true);
        }
    }

    public void Collision(Node body)
    {
        if (body.Name.Contains("Arrow") || body.Name.Contains("Block"))
        {
            FailedSections.Add(Section);
            Section++;
            this.Translation = new Vector3(SectionStartPoints[Section].x, StartPos.y, SectionStartPoints[Section].y);
            this.RotationDegrees = new Vector3(0, SectionStartRotations[Section], 0);
        }
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
            return Vector3.One * 1000;
        }
    }

    private bool PinVal(int pin)
    {
        return (JavaScript.Eval($"pinVal({pin})", true) as float?) == 1;
    }

    private void SetPin(int pin, float value)
    {
        JavaScript.Eval($"setPin({pin}, {value})", true);
    }
}