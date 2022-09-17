using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace GarnsMod.Tools
{
    internal static class VectorExtensions
    {
        public static Vector2 To(this Vector2 pos1, Vector2 pos2)
        {
            return (pos2 - pos1);
        }

        public static Vector2 From(this Vector2 pos1, Vector2 pos2)
        {
            return (pos1 - pos2);

        }

        public static Vector2 Abs(this Vector2 v1)
        {
            return new(Math.Abs(v1.X), Math.Abs(v1.Y));
        }

        /// <summary>Basically converts x and y into 0, 1, or -1 </summary>
        public static Vector2 Cardinals(this Vector2 v1)
        {
            return new(v1.X > 0 ? 1 : v1.X == 0 ? 0 : -1, v1.Y > 0 ? 1 : v1.Y == 0 ? 0 : -1);
        }

        public static void Deconstruct(this Vector2 vec, out float x, out float y)
        {
            x = vec.X;
            y = vec.Y;
        }

        // Directional Conditionals

        public static bool IsGoingTowardsX(this Entity e, Vector2 position)
        {
            return IsGoingTowardsX(e.velocity, e.position, position.X);
        }

        public static bool IsGoingTowardsX(this Entity e, float x)
        {
            return IsGoingTowardsX(e.velocity, e.position, x);
        }

        private static bool IsGoingTowardsX(Vector2 vec, Vector2 pos2, float x)
        {
            return vec.Cardinals().X == (new Vector2(x, 0) - pos2).Cardinals().X;
        }

        public static bool IsGoingTowardsY(this Entity e, Vector2 position)
        {
            return IsGoingTowardsX(e.velocity, e.position, position.Y);
        }

        public static bool IsGoingTowardsY(this Entity e, float x)
        {
            return IsGoingTowardsY(e.velocity, e.position, x);
        }

        public static bool IsGoingTowardsY(Vector2 vec, Vector2 pos, float y)
        {
            return vec.Cardinals().Y == (new Vector2(0, y) - pos).Cardinals().Y;
        }

        public static bool IsGoingTowardsSky(this Entity e)
        {
            return e.velocity.IsGoingTowardsSky();
        }

        public static bool IsGoingTowardsSky(this Vector2 velocity)
        {
            return velocity.Y < 0;
        }

        public static bool IsGoingTowardsHell(this Entity e)
        {
            return e.velocity.IsGoingTowardsHell();
        }

        public static bool IsGoingTowardsHell(this Vector2 velocity)
        {
            return velocity.Y > 0;
        }

        public static bool IsGoingLeft(this Entity e)
        {
            return e.velocity.IsGoingLeft();
        }

        public static bool IsGoingLeft(this Vector2 velocity)
        {
            return velocity.X < 0;
        }

        public static bool IsGoingRight(this Entity e)
        {
            return e.velocity.IsGoingRight();
        }

        public static bool IsGoingRight(this Vector2 velocity)
        {
            return velocity.X > 0;
        }

        public static bool IsXFasterThan(this Entity e, float absSpeed)
        {
            return e.velocity.IsXFasterThan(absSpeed);
        }

        public static bool IsXFasterThan(this Vector2 velocity, float absSpeed)
        {
            return Math.Abs(velocity.X) > absSpeed;
        }

        public static bool IsYFasterThan(this Entity e, float absSpeed)
        {
            return e.velocity.IsYFasterThan(absSpeed);
        }

        public static bool IsYFasterThan(this in Vector2 velocity, float absSpeed)
        {
            return Math.Abs(velocity.Y) > absSpeed;
        }


        // Directional Conditional Modifiers

        public static void SlowDownIfFasterThan(this ref Vector2 vec, float topSpeed, float slowPercent)
        {
            if (vec.Length() > topSpeed)
            {
                Vector2 slowedDown = vec * (1 - slowPercent);
                vec.X = slowedDown.X;
                vec.Y = slowedDown.Y;
            }
        }

        public static void SlowY(this Entity e, float slowPercent)
        {
            e.velocity.SlowY(slowPercent);
        }

        public static void SlowY(this ref Vector2 vec, float slowPercent)
        {
            vec.SlowYIfFasterThan(0f, slowPercent);
        }

        public static void SlowX(this Entity e, float slowPercent)
        {
            e.velocity.SlowX(slowPercent);
        }

        public static void SlowX(this ref Vector2 vec, float slowPercent)
        {
            vec.SlowXIfFasterThan(0f, slowPercent);
        }

        public static void SlowXIfFasterThan(this Entity e, float topSpeed, float slowPercent)
        {
            e.velocity.SlowXIfFasterThan(topSpeed, slowPercent);
        }

        public static void SlowXIfFasterThan(this ref Vector2 vec, float topSpeed, float slowPercent)
        {
            if (vec.IsXFasterThan(topSpeed))
            {
                vec.X *= (1 - slowPercent);
            }
        }

        public static void SlowYIfFasterThan(this Entity e, float topSpeed, float slowPercent)
        {
            e.velocity.SlowYIfFasterThan(topSpeed, slowPercent);
        }

        public static void SlowYIfFasterThan(this ref Vector2 vec, float topSpeed, float slowPercent)
        {
            if (vec.IsYFasterThan(topSpeed))
            {
                vec.Y *= (1 - slowPercent);
            }
        }

        // Positional Conditional

        public static bool IsXFurtherThan(this Entity e, float amount, float x)
        {
            return e.position.IsXFurtherThan(amount, x);
        }

        public static bool IsXFurtherThan(this Entity e, float amount, Vector2 otherPosition)
        {
            return e.position.IsXFurtherThan(amount, otherPosition);
        }

        public static bool IsXFurtherThan(this Vector2 position, float amount, Vector2 otherPosition)
        {
            return position.IsXFurtherThan(amount, otherPosition.X);
        }

        public static bool IsXFurtherThan(this Vector2 position, float amount, float x)
        {
            return position.GetXDistance(x) > amount;
        }

        public static bool IsXCloserThan(this Entity e, float amount, float x)
        {
            return e.position.IsXCloserThan(amount, x);
        }

        public static bool IsXCloserThan(this Entity e, float amount, Vector2 otherPosition)
        {
            return e.position.IsXCloserThan(amount, otherPosition);
        }

        public static bool IsXCloserThan(this Vector2 position, float amount, Vector2 otherPosition)
        {
            return position.IsXCloserThan(amount, otherPosition.X);
        }

        public static bool IsXCloserThan(this Vector2 position, float amount, float x)
        {
            return position.GetXDistance(x) < amount;
        }

        public static float GetXDistance(this Vector2 position, Vector2 otherPosition)
        {
            return position.GetXDistance(otherPosition.X);
        }

        public static float GetXDistance(this Vector2 position, float x)
        {
            return Math.Abs(x - position.X);
        }

        public static bool IsYFurtherThan(this Entity e, float amount, float y)
        {
            return e.position.IsYFurtherThan(amount, y);
        }

        public static bool IsYFurtherThan(this Entity e, float amount, Vector2 otherPosition)
        {
            return e.position.IsYFurtherThan(amount, otherPosition);
        }

        public static bool IsYFurtherThan(this Vector2 position, float amount, Vector2 otherPosition)
        {
            return position.IsYFurtherThan(amount, otherPosition.Y);
        }

        public static bool IsYFurtherThan(this Vector2 position, float amount, float y)
        {
            return position.GetYDistance(y) > amount;
        }

        public static bool IsYCloserThan(this Entity e, float amount, float y)
        {
            return e.position.IsYCloserThan(amount, y);
        }

        public static bool IsYCloserThan(this Entity e, float amount, Vector2 otherPosition)
        {
            return e.position.IsYCloserThan(amount, otherPosition);
        }

        public static bool IsYCloserThan(this Vector2 position, float amount, Vector2 otherPosition)
        {
            return position.IsYCloserThan(amount, otherPosition.Y);
        }

        public static bool IsYCloserThan(this Vector2 position, float amount, float y)
        {
            return position.GetYDistance(y) < amount;
        }

        public static float GetYDistance(this Vector2 position, Vector2 otherPosition)
        {
            return position.GetYDistance(otherPosition.Y);
        }

        public static float GetYDistance(this Vector2 position, float y)
        {
            return Math.Abs(y - position.Y);
        }

        public static bool IsHigherUpThan(this Entity e, Vector2 position)
        {
            return e.position.IsHigherUpThan(position);
        }

        public static bool IsHigherUpThan(this Entity e, float y)
        {
            return e.position.IsHigherUpThan(y);
        }

        public static bool IsHigherUpThan(this Vector2 position, Vector2 otherPosition)
        {
            return position.Y < otherPosition.Y;
        }

        public static bool IsHigherUpThan(this Vector2 position, float y)
        {
            return position.Y < y;
        }

        public static bool IsLowerDownThan(this Entity e, Vector2 position)
        {
            return e.position.IsLowerDownThan(position);
        }

        public static bool IsLowerDownThan(this Entity e, float y)
        {
            return e.position.IsLowerDownThan(y);
        }

        public static bool IsLowerDownThan(this Vector2 position, Vector2 otherPosition)
        {
            return position.Y > otherPosition.Y;
        }

        public static bool IsLowerDownThan(this Vector2 position, float y)
        {
            return position.Y > y;
        }

        public static bool IsToLeftOf(this Entity e, Vector2 position)
        {
            return e.position.IsToLeftOf(position);
        }

        public static bool IsToLeftOf(this Entity e, float x)
        {
            return e.position.IsToLeftOf(x);
        }

        public static bool IsToLeftOf(this Vector2 position, Vector2 otherPosition)
        {
            return position.X < otherPosition.X;
        }

        public static bool IsToLeftOf(this Vector2 position, float x)
        {
            return position.X < x;
        }

        public static bool IsToRightOf(this Entity e, Vector2 position)
        {
            return e.position.IsToRightOf(position);
        }

        public static bool IsToRightOf(this Entity e, float x)
        {
            return e.position.IsToRightOf(x);
        }

        public static bool IsToRightOf(this Vector2 position, Vector2 otherPosition)
        {
            return position.X > otherPosition.X;
        }

        public static bool IsToRightOf(this Vector2 position, float x)
        {
            return position.X > x;
        }

        // Positional Conditional Modifiers

        public static bool SlowXIfCloserThan(this Entity e, float specifiedDistance, Vector2 otherPos, float slowPercent, float? endSlowPercent = null)
        {
            return SlowXIfCloserThan(ref e.position, ref e.velocity, specifiedDistance, otherPos.X, slowPercent, endSlowPercent);
        }

        public static bool SlowXIfCloserThan(this Entity e, float specifiedDistance, float x, float slowPercent, float? endSlowPercent = null)
        {
            return SlowXIfCloserThan(ref e.position, ref e.velocity, specifiedDistance, x, slowPercent, endSlowPercent);
        }

        private static bool SlowXIfCloserThan(ref Vector2 pos, ref Vector2 vel, float specifiedDistance, float x, float slowPercent, float? endSlowPercent = null)
        {
            float ourDistance = pos.GetXDistance(x);
            if (ourDistance < specifiedDistance)
            {
                if (endSlowPercent is not float endPercent)
                {
                    vel.SlowX(slowPercent);
                    return true;
                }

                float distanceProgress = 1 - ourDistance / specifiedDistance;
                vel.SlowX(MathHelper.Lerp(slowPercent, endPercent, distanceProgress));
                return true;
            }

            return false;
        }


        public static bool SlowYIfCloserThan(this Entity e, float specifiedDistance, Vector2 otherPos, float slowPercent, float? endSlowPercent = null)
        {
            return SlowYIfCloserThan(ref e.position, ref e.velocity, specifiedDistance, otherPos.Y, slowPercent, endSlowPercent);
        }

        public static bool SlowYIfCloserThan(this Entity e, float specifiedDistance, float y, float slowPercent, float? endSlowPercent = null)
        {
            return SlowYIfCloserThan(ref e.position, ref e.velocity, specifiedDistance, y, slowPercent, endSlowPercent);
        }

        private static bool SlowYIfCloserThan(ref Vector2 pos, ref Vector2 vel, float specifiedDistance, float y, float slowPercent, float? endSlowPercent = null)
        {
            float ourDistance = pos.GetYDistance(y);
            if (ourDistance < specifiedDistance)
            {
                if (endSlowPercent is not float endPercent)
                {
                    vel.SlowY(slowPercent);
                    return true;
                }

                float distanceProgress = 1 - ourDistance / specifiedDistance;
                vel.SlowY(Utils.GetLerpValue(slowPercent, endPercent, distanceProgress, true));
                return true;
            }

            return false;
        }
    }
}
