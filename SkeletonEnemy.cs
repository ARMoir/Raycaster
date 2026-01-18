using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raycaster;

public class SkeletonEnemy : Enemy
{
    public List<Bone> Bones = new();

    public Vector2 Position => new((float)X, (float)Y);

    public new bool Alive => Bones.Any(b => b.IsCore && !b.Broken);

    public SkeletonEnemy(double x, double y)
    {
        X = x;
        Y = y;
        Build();
    }

    void Build()
    {
        // -------- Spine / Torso --------
        Bones.Add(new Bone { LocalStart = new Vector2(0, -18), LocalEnd = new Vector2(0, -10), IsCore = true, Thickness = 3 });
        Bones.Add(new Bone { LocalStart = new Vector2(0, -10), LocalEnd = new Vector2(0, -2), IsCore = true, Thickness = 3 });
        Bones.Add(new Bone { LocalStart = new Vector2(0, -2), LocalEnd = new Vector2(0, 6), IsCore = true, Thickness = 3 });
        Bones.Add(new Bone { LocalStart = new Vector2(0, 6), LocalEnd = new Vector2(0, 14), IsCore = true, Thickness = 3 });

        // -------- Rib cage / chest -------- (centered on upper torso)
        Bones.Add(new Bone { LocalStart = new Vector2(-6, -10), LocalEnd = new Vector2(6, -10), IsCore = true, Thickness = 2 });
        Bones.Add(new Bone { LocalStart = new Vector2(-5, -6), LocalEnd = new Vector2(5, -6), Thickness = 2 });
        Bones.Add(new Bone { LocalStart = new Vector2(-4, -2), LocalEnd = new Vector2(4, -2), Thickness = 2 });

        // -------- Head -------- (larger, on top)
        Bones.Add(new Bone { LocalStart = new Vector2(-4, -22), LocalEnd = new Vector2(4, -22), IsCore = true, Thickness = 2 });
        Bones.Add(new Bone { LocalStart = new Vector2(0, -22), LocalEnd = new Vector2(0, -18), IsCore = true, Thickness = 2 });

        // -------- Arms -------- (connected to rib cage, natural bend)
        // Left arm (battle stance)
        Bones.Add(new Bone { LocalStart = new Vector2(-6, -8), LocalEnd = new Vector2(-14, -2), Thickness = 3 });
        Bones.Add(new Bone { LocalStart = new Vector2(-14, -2), LocalEnd = new Vector2(-22, 4), Thickness = 3 }); // lower arm
        Bones.Add(new Bone { LocalStart = new Vector2(-22, 4), LocalEnd = new Vector2(-26, 8), Thickness = 4 }); // hand / weapon

        // Right arm (raised claw)
        Bones.Add(new Bone { LocalStart = new Vector2(6, -8), LocalEnd = new Vector2(14, -4), Thickness = 3 });
        Bones.Add(new Bone { LocalStart = new Vector2(14, -4), LocalEnd = new Vector2(20, 0), Thickness = 2 });
        Bones.Add(new Bone { LocalStart = new Vector2(20, 0), LocalEnd = new Vector2(22, 2), Thickness = 1 });

        // -------- Legs -------- (slightly angled outward)
        Bones.Add(new Bone { LocalStart = new Vector2(-3, 14), LocalEnd = new Vector2(-6, 28), Thickness = 3 });
        Bones.Add(new Bone { LocalStart = new Vector2(3, 14), LocalEnd = new Vector2(6, 28), Thickness = 3 });
        Bones.Add(new Bone { LocalStart = new Vector2(-6, 28), LocalEnd = new Vector2(-8, 32), Thickness = 2 }); // left foot
        Bones.Add(new Bone { LocalStart = new Vector2(6, 28), LocalEnd = new Vector2(8, 32), Thickness = 2 }); // right foot
    }

}
