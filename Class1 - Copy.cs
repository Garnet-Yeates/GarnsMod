using Microsoft.Xna.Framework;

namespace GarnsMod
{
    internal static class Extensions
    {
        public static Vector3 Average(this Vector3 vector, Vector3 other, float myWeight = 0.5f)
        {
            return AverageVectors(vector, other, myWeight);
        }

        public static Color Average(this Color color, Color other, float myWeight = 0.5f)
        {
            return new Color(AverageVectors(color.ToVector3(), other.ToVector3(), myWeight));
        }

        public static Vector3 AverageVectors(Vector3 first, Vector3 second, float myWeight = 0.5f)
        {
            return first * myWeight + second * (1f - myWeight);
        }
    }
}
