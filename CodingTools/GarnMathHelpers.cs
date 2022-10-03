using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GarnsMod.CodingTools
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

        // C# % works strangely for negative numbers, this makes it work like modulo
        public static float Modulo(float a, float b)
        {
            return a - b * (float)Math.Floor(a / b);
        }

        public static int Modulo(int a, int b)
        {
            return a - b * (int)Math.Floor((float)a / b);
        }

    }

}
