using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Raycaster
{

    public class Bone
    {
        public Vector2 LocalStart;
        public Vector2 LocalEnd;

        public Vector2 WorldStart;
        public Vector2 WorldEnd;

        public Vector2 Velocity = Vector2.Zero;

        public bool Broken;
        public bool IsCore;
        public float Thickness = 2f;
    }

}
