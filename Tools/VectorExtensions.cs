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
            x = vec.X;
            y = vec.Y;
        }

        public static void SlowDownIfFasterThan(this ref Vector2 vec, float topSpeed, float slowPercent)
        {
            if (vec.Length() > topSpeed)
            {
                Vector2 slowedDown = vec * (1 - slowPercent);
                vec.X = slowedDown.X;
                vec.Y = slowedDown.Y;
            }
        }

        public static void SlowY(this ref Vector2 vec, float slowPercent)
        {
            vec.SlowYIfFasterThan(0f, slowPercent);
        }
        public static void SlowX(this ref Vector2 vec, float slowPercent)
        {
            vec.SlowXIfFasterThan(0f, slowPercent);
        }

        public static void SlowXIfFasterThan(this ref Vector2 vec, float topSpeed, float slowPercent)
        {
            if (vec.Abs().X > topSpeed)
            {
                vec.X *= (1 - slowPercent);
            }
        }

        public static void SlowYIfFasterThan(this ref Vector2 vec, float topSpeed, float slowPercent)
        {
            if (vec.Abs().Y > topSpeed)
            {
                vec.Y *= (1 - slowPercent);
            }
        }

        public static bool IsGoingTowardsX(this Vector2 vec, float x)
        {
            return vec.Cardinals().X == (new Vector2(x, 0) - vec).Cardinals().X;
        }

        public static bool IsGoingTowardsY(this Vector2 vec, float y)
        {
            return vec.Cardinals().Y == (new Vector2(0, y) - vec).Cardinals().Y;
        }

        public static bool IsXFurtherThan(this Vector2 position, float amount, float x)
        {
            float xDist = Math.Abs(x - position.X);
            return xDist > amount;
        }

        public static bool IsYFurtherThan(this Vector2 position, float amount, float y)
        {
            float yDist = Math.Abs(y - position.Y);
            return yDist > amount;
        }

        public static bool IsXCloserThan(this Vector2 position, float amount, float x)
        {
            float xDist = Math.Abs(x - position.X);
            return xDist < amount;
        }

        public static bool IsYCloserThan(this Vector2 position, float amount, float y)
        {
            float yDist = Math.Abs(y - position.Y);
            return yDist < amount;
        }

        public static bool IsGoingTowardsSky(this Vector2 velocity)
        {
            return velocity.Y < 0;
        }

        public static bool IsGoingTowardsHell(this Vector2 velocity)
        {
            return velocity.Y > 0;
        }

        public static bool IsGoingLeft(this Vector2 velocity)
        {
            return velocity.X < 0;
        }

        public static bool IsGoingRight(this Vector2 velocity)
        {
            return velocity.X > 0;
        }

        public static bool IsHigherUpThan(this Vector2 position, float y)
        {
            return position.Y < y;
        }

        public static bool IsLowerDownThan(this Vector2 position, float y)
        {
            return position.Y > y;
        }

        public static bool IsToLeftOf(this Vector2 position, float x)
        {
            return position.X < x;
        }

        public static bool IsToRightOf(this Vector2 position, float x)
        {
            return position.X > x;
        }
    }
}
