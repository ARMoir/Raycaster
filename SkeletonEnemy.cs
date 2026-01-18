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
        const float H = 54f;   // -22 → +32
        const float CY = 5f;
        const float W = 54f;

        Vector2 N(float x, float y)
            => new(x / W, (y - CY) / H);

        // =========================
        // SPINE (thick core)
        // =========================
        Bones.Add(new Bone { LocalStart = N(0, -20), LocalEnd = N(0, -14), IsCore = true, Thickness = 0.10f });
        Bones.Add(new Bone { LocalStart = N(0, -14), LocalEnd = N(0, -8), IsCore = true, Thickness = 0.10f });
        Bones.Add(new Bone { LocalStart = N(0, -8), LocalEnd = N(0, -2), IsCore = true, Thickness = 0.10f });
        Bones.Add(new Bone { LocalStart = N(0, -2), LocalEnd = N(0, 6), IsCore = true, Thickness = 0.10f });
        Bones.Add(new Bone { LocalStart = N(0, 6), LocalEnd = N(0, 14), IsCore = true, Thickness = 0.10f });

        // =========================
        // RIB CAGE
        // =========================
        for (int i = 0; i < 4; i++)
        {
            float y = -12 + i * 4;
            float w = 7 - i;

            Bones.Add(new Bone { LocalStart = N(-w, y), LocalEnd = N(w, y), Thickness = 0.065f });
            Bones.Add(new Bone { LocalStart = N(-w, y), LocalEnd = N(-w + 1.5f, y + 3), Thickness = 0.045f });
            Bones.Add(new Bone { LocalStart = N(w, y), LocalEnd = N(w - 1.5f, y + 3), Thickness = 0.045f });
        }

        Bones.Add(new Bone { LocalStart = N(0, -12), LocalEnd = N(0, 0), IsCore = true, Thickness = 0.06f });

        // =========================
        // JACK-O-LANTERN HEAD
        // =========================
        Bones.Add(new Bone { LocalStart = N(-6, -26), LocalEnd = N(6, -26), IsCore = true, Thickness = 0.09f });
        Bones.Add(new Bone { LocalStart = N(-6, -26), LocalEnd = N(-7, -22), Thickness = 0.08f });
        Bones.Add(new Bone { LocalStart = N(6, -26), LocalEnd = N(7, -22), Thickness = 0.08f });
        Bones.Add(new Bone { LocalStart = N(-7, -22), LocalEnd = N(-6, -18), Thickness = 0.07f });
        Bones.Add(new Bone { LocalStart = N(7, -22), LocalEnd = N(6, -18), Thickness = 0.07f });
        Bones.Add(new Bone { LocalStart = N(-5, -18), LocalEnd = N(5, -18), Thickness = 0.07f });

        Bones.Add(new Bone { LocalStart = N(-3, -26), LocalEnd = N(-4, -18), Thickness = 0.05f });
        Bones.Add(new Bone { LocalStart = N(0, -26), LocalEnd = N(0, -18), IsCore = true, Thickness = 0.055f });
        Bones.Add(new Bone { LocalStart = N(3, -26), LocalEnd = N(4, -18), Thickness = 0.05f });

        // Eyes
        Bones.Add(new Bone { LocalStart = N(-4, -22), LocalEnd = N(-2, -20), Thickness = 0.045f });
        Bones.Add(new Bone { LocalStart = N(-2, -20), LocalEnd = N(-1, -22), Thickness = 0.045f });
        Bones.Add(new Bone { LocalStart = N(-4, -22), LocalEnd = N(-1, -22), Thickness = 0.035f });

        Bones.Add(new Bone { LocalStart = N(4, -22), LocalEnd = N(2, -20), Thickness = 0.045f });
        Bones.Add(new Bone { LocalStart = N(2, -20), LocalEnd = N(1, -22), Thickness = 0.045f });
        Bones.Add(new Bone { LocalStart = N(4, -22), LocalEnd = N(1, -22), Thickness = 0.035f });

        // Grin
        Bones.Add(new Bone { LocalStart = N(-4, -19), LocalEnd = N(-2, -17), Thickness = 0.045f });
        Bones.Add(new Bone { LocalStart = N(-2, -17), LocalEnd = N(0, -19), Thickness = 0.045f });
        Bones.Add(new Bone { LocalStart = N(0, -19), LocalEnd = N(2, -17), Thickness = 0.045f });
        Bones.Add(new Bone { LocalStart = N(2, -17), LocalEnd = N(4, -19), Thickness = 0.045f });

        // Neck
        Bones.Add(new Bone { LocalStart = N(0, -18), LocalEnd = N(0, -14), IsCore = true, Thickness = 0.07f });

        // =========================
        // SHOULDERS
        // =========================
        Bones.Add(new Bone { LocalStart = N(-7, -8), LocalEnd = N(7, -8), Thickness = 0.08f });

        // =========================
        // ARMS
        // =========================
        Bones.Add(new Bone { LocalStart = N(-7, -8), LocalEnd = N(-14, -4), Thickness = 0.075f });
        Bones.Add(new Bone { LocalStart = N(-14, -4), LocalEnd = N(-20, 1), Thickness = 0.065f });
        Bones.Add(new Bone { LocalStart = N(-20, 1), LocalEnd = N(-24, 5), Thickness = 0.055f });
        Bones.Add(new Bone { LocalStart = N(-24, 5), LocalEnd = N(-27, 9), Thickness = 0.045f });

        Bones.Add(new Bone { LocalStart = N(7, -8), LocalEnd = N(14, -4), Thickness = 0.075f });
        Bones.Add(new Bone { LocalStart = N(14, -4), LocalEnd = N(20, 0), Thickness = 0.065f });
        Bones.Add(new Bone { LocalStart = N(20, 0), LocalEnd = N(24, 4), Thickness = 0.055f });
        Bones.Add(new Bone { LocalStart = N(24, 4), LocalEnd = N(26, 6), Thickness = 0.045f });

        // =========================
        // PELVIS
        // =========================
        Bones.Add(new Bone { LocalStart = N(-6, 12), LocalEnd = N(6, 12), IsCore = true, Thickness = 0.085f });
        Bones.Add(new Bone { LocalStart = N(-6, 12), LocalEnd = N(-4, 16), Thickness = 0.06f });
        Bones.Add(new Bone { LocalStart = N(6, 12), LocalEnd = N(4, 16), Thickness = 0.06f });

        // =========================
        // LEGS
        // =========================
        Bones.Add(new Bone { LocalStart = N(-4, 16), LocalEnd = N(-7, 24), Thickness = 0.075f });
        Bones.Add(new Bone { LocalStart = N(-7, 24), LocalEnd = N(-8, 30), Thickness = 0.065f });
        Bones.Add(new Bone { LocalStart = N(-8, 30), LocalEnd = N(-10, 33), Thickness = 0.05f });

        Bones.Add(new Bone { LocalStart = N(4, 16), LocalEnd = N(7, 24), Thickness = 0.075f });
        Bones.Add(new Bone { LocalStart = N(7, 24), LocalEnd = N(8, 30), Thickness = 0.065f });
        Bones.Add(new Bone { LocalStart = N(8, 30), LocalEnd = N(10, 33), Thickness = 0.05f });
    }



}
