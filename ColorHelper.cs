using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GarnsMod
{

    public class ColorHelper
    {
        public static readonly List<Color> RainbowColors = new()
        {
            new(255, 0, 0),
            new(255, 128, 0),
            new(255, 255, 0),
            new(128, 255, 0),
            new(0, 255, 0),
            new(0, 255, 128),
            new(0, 255, 255),
            new(0, 128, 255),
            new(0, 0, 255),
            new(127, 0, 255),
            new(255, 0, 255),
            new(255, 0, 127),
        };

    }

    public class ColorGradient
    {
        private float inc;
        private int n;

        private readonly List<Color> colors = new();

        public static readonly Dictionary<int, ColorGradient> RainbowGradients = InitGradients();

        public static Dictionary<int, ColorGradient> InitGradients()
        {
            Dictionary<int, ColorGradient> dict = new();
            for (int i = 0; i < ColorHelper.RainbowColors.Count; i++)
            {
                dict.Add(i, FromCollectionWithStartIndex(ColorHelper.RainbowColors, i, 2));
            }
            return dict;
        }

        public ColorGradient(IEnumerable<Color> colors = null)
        {
            if (colors is List<Color> c)
            {
                c.ForEach(color => AddColor(color));
            }
        }

        public static ColorGradient FromCollectionWithStartIndex(List<Color> colors, int startIndex, int extraStart = 0)
        {
            ColorGradient grad = new();
            for (int i = 0; i < extraStart; i++)
            {
                grad.AddColor(colors[startIndex]);
            }
            int length = colors.Count;
            for (int currIndex = startIndex, i = 0; i < length; currIndex = (currIndex + 1) % length, i++)
            {
                grad.AddColor(colors[currIndex]);
            }
            return grad;
        }

        public void AddColor(Color c)
        {
            colors.Add(c);
            n = colors.Count;
            if (colors.Count == 1)
            {
                inc = 1;
                return;
            }
            inc = 1f / (n - 1f);
        }

        public Color GetColor(float progress)
        {
            if (float.IsNaN(progress))
            {
                return colors[0];
            }
            if (n == 1)
            {
                return colors[0];
            }

            // 100% would be 1 for progress btw, keep it as a decimal
            int currIndex = (int)(progress / inc);
            int nextIndex = currIndex + 1;
            float p = (progress % inc) / inc; // little p is our progress between currIndex and nextIndex
            if (nextIndex >= n)
            {
                nextIndex = currIndex; // dont think this is even needed but it is incase progress is somehow > 100%
            }
            return (colors[nextIndex].Average(colors[currIndex], p));
        }
    }
}
