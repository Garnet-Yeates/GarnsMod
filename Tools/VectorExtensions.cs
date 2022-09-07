using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GarnsMod.Tools
{
    internal static class VectorExtensions
    {
        public static Vector2 CardinalsTo(this Vector2 v1, Vector2 v2)
        {
            return (v2 - v1).Cardinals();
        }

        public static Vector2 Abs(this Vector2 v1)
        {
            return new(Math.Abs(v1.X), Math.Abs(v1.Y));
        }

        /// <summary>
        /// Basically converts x and y into 0, 1, or -1
        /// </summary>
        public static Vector2 Cardinals(this Vector2 v1)
        {
            return new(v1.X > 0 ? 1 : v1.X == 0 ? 0 : -1, v1.Y > 0 ? 1 : v1.Y == 0 ? 0 : -1);
        }

        public static void Deconstruct(this Vector2 vec, out float x, out float y)
        {
            DeconstructVector(vec, out x, out y);
        }

        public static void DeconstructVector(Vector2 vec, out float x, out float y)
        {
            x = vec.X;
            y = vec.Y;
        }
    }
}
