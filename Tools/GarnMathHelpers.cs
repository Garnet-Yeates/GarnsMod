using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GarnsMod.Tools
{
    internal class GarnMathHelpers
    {
        public static Rectangle RectFrom2Points(Vector2 p1, Vector2 p2)
        {
            int width = (int)Math.Abs(p1.X - p2.X);
            int height = (int)Math.Abs(p1.Y - p2.Y);

            int lefterX = (int)(p1.X < p2.X ? p1.X : p2.X);
            int higherY = (int)(p1.Y < p2.Y ? p1.Y : p2.Y);

            return new(lefterX, higherY, width, height);
        }

    }

}
