using System;
using System.Linq;
using System.Numerics;

public static class SkeletonCombat
{
    public static void Update(SkeletonEnemy s, float dt)
    {
        foreach (var b in s.Bones)
        {
            if (!b.Broken) continue;

            b.WorldStart += b.Velocity * dt;
            b.WorldEnd += b.Velocity * dt;
            b.Velocity *= 0.96f;
        }
    }

    public static void AxeHit(SkeletonEnemy s, Vector2 dir)
    {
        var intact = s.Bones.Where(b => !b.Broken).ToList();
        if (intact.Count == 0) return;

        var bone = intact[Random.Shared.Next(intact.Count)];
        bone.Broken = true;
        bone.WorldStart = bone.LocalStart + s.Position;
        bone.WorldEnd = bone.LocalEnd + s.Position;
        bone.Velocity = dir * Random.Shared.Next(120, 220);
    }
}
