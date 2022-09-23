using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace GarnsMod.CodingTools
{
    internal static class VectorExtensions
    {
        public static Vector2 To(this Vector2 p1, Vector2 p2)
        {
            return p2 - p1;
        }

        public static Vector2 From(this Vector2 p1, Vector2 p2)
        {
            return p1 - p2;
        }

        public static Vector2 CardinalsTo(this Vector2 p1, Vector2 p2)
        {
            return (p1 - p2).Cardinals();
        }

        public static Vector2 Abs(this Vector2 v)
        {
            return new(v.AbsX(), v.AbsY());
        }

        public static float AbsX(this Vector2 v)
        {
            return Math.Abs(v.X);
        }

        public static float AbsY(this Vector2 v)
        {
            return Math.Abs(v.Y);
        }

        public static float CardinalY(this Vector2 v)
        {
            return v.Y.Cardinal();
        }

        public static float CardinalX(this Vector2 v)
        {
            return v.X.Cardinal();
        }

        public static Vector2 Cardinals(this Vector2 v)
        {
            return new(v.CardinalX(), v.CardinalY());
        }

        public static float Cardinal(this float n)
        {
            return n > 0 ? 1 : n == 0 ? 0 : n;
        }

        public static void Deconstruct(this Vector2 v, out float x, out float y)
        {
            x = v.X;
            y = v.Y;
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

        private static bool IsGoingTowardsX(Vector2 myVelocity, Vector2 myPosition, float x)
        {
            return myVelocity.CardinalX() == (x - myPosition.X).Cardinal();
        }

        public static bool IsGoingTowardsY(this Entity e, Vector2 position)
        {
            return IsGoingTowardsX(e.velocity, e.position, position.Y);
        }

        public static bool IsGoingTowardsY(this Entity e, float y)
        {
            return IsGoingTowardsY(e.velocity, e.position, y);
        }

        public static bool IsGoingTowardsY(Vector2 myVelocity, Vector2 myPosition, float y)
        {
            return myVelocity.CardinalY() == (y - myPosition.Y).Cardinal();
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
                vec.X *= 1 - slowPercent;
            }
        }

        public static void CapXSpeed(this Entity e, float maxAbsSpeed)
        {
            e.velocity.CapXSpeed(maxAbsSpeed);
        }

        public static void CapXSpeed(this ref Vector2 vec, float maxAbsSpeed)
        {
            if (vec.X > 0)
            {
                vec.X = Math.Min(vec.X, maxAbsSpeed);
            }
            else
            {
                vec.X = Math.Max(vec.X, -maxAbsSpeed);
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

        public static void SlowYIfFasterThan(this Entity e, float topSpeed, float slowPercent)
        {
            e.velocity.SlowYIfFasterThan(topSpeed, slowPercent);
        }

        public static void SlowYIfFasterThan(this ref Vector2 vec, float topSpeed, float slowPercent)
        {
            if (vec.IsYFasterThan(topSpeed))
            {
                vec.Y *= 1 - slowPercent;
            }
        }

        public static void CapYSpeed(this ref Vector2 vec, float maxSpeed)
        {
            if (vec.Y > 0)
            {
                vec.Y = Math.Min(vec.Y, maxSpeed);
            }
            else
            {
                vec.Y = Math.Max(vec.Y, -maxSpeed);
            }
        }

        public static void CapYSpeed(this Entity e, float maxAbsSpeed)
        {
            e.velocity.CapXSpeed(maxAbsSpeed);
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
                if (endSlowPercent is float endPercent)
                {
                    float distanceProgress = 1 - ourDistance / specifiedDistance;
                    vel.SlowX(MathHelper.Lerp(slowPercent, endPercent, distanceProgress));
                    return true;
                }

                vel.SlowX(slowPercent);
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
                if (endSlowPercent is float endPercent)
                {
                    float distanceProgress = 1 - ourDistance / specifiedDistance;
                    vel.SlowY(Utils.GetLerpValue(slowPercent, endPercent, distanceProgress, true));
                    return true;
                }

                vel.SlowY(slowPercent);
                return true;
            }

            return false;
        }
    }
}
