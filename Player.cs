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
    }
}

