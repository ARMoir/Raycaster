using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raycaster
{
    public class Player
    {
        public double posX;
        public double posY;
        public double dirX;
        public double dirY;

        public double planeX = 0;
        public double planeY = 0.66;

        public int Health = 100;
        public int Ammo = 30;

        public List<Enemy> enemies = new List<Enemy>
        {
            new Enemy { X = 1.5, Y = 1.5 },
            new Enemy { X = 3.5, Y = 3.5 },
            new Enemy { X = 5.5, Y = 2.5 }
        };
    }

    public class Enemy
    {
        public double X;
        public double Y;
        public bool Alive = true;
    }
}

