using Raycaster;
using System;
using System.Linq;
using System.Numerics;

public class SkeletonCombat
{

    public void DamageSkeleton(SkeletonEnemy skeleton)
    {
        // Prefer core bones first
        var targetBones = skeleton.Bones
            .Where(b => !b.Broken)
            .OrderByDescending(b => b.IsCore)
            .ToList();

        if (targetBones.Count == 0)
            return;

        Random rng = new Random();
        int breaks = rng.Next(5, 10);

        foreach (var bone in targetBones.Take(breaks))
            bone.Broken = true;
    }

}
