using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace GarnsMod.Tools
{
    public class ColorGradient
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

        private float inc;
        private int n;

        private readonly List<Color> colors = new();

        public static readonly Dictionary<int, ColorGradient> FullRainbowGradientsWithExtraStart = InitFullGradientsWithExtraStart();
        public static readonly Dictionary<int, ColorGradient> FullRainbowGradients = InitFullGradients();
        public static readonly Dictionary<int, Dictionary<int, ColorGradient>> PartialRainbowGradients = InitPartialGradients();

        public static Dictionary<int, ColorGradient> InitFullGradientsWithExtraStart()
        {
            Dictionary<int, ColorGradient> dict = new();
            for (int i = 0; i < RainbowColors.Count; i++)
            {
                dict.Add(i, FromCollectionWithStartIndex(RainbowColors, i, extraStart: 3, extraLoops: 1 ));
            }
            return dict;
        }

        public static Dictionary<int, ColorGradient> InitFullGradients()
        {
            Dictionary<int, ColorGradient> dict = new();
            for (int i = 0; i < RainbowColors.Count; i++)
            {
                dict.Add(i, FromCollectionWithStartIndex(RainbowColors, i, extraStart: 0, extraLoops: 1));
            }
            return dict;
        }

        public static Dictionary<int, Dictionary<int, ColorGradient>> InitPartialGradients()
        {
            static List<Color> GetRainbowColorSubset(int upto)
            {
                List<Color> subset = new();
                for (int i = 0; i <= upto; i++)
                {
                    subset.Add(RainbowColors[i]);
                }
                return subset;
            }

            Dictionary<int, Dictionary<int, ColorGradient>> partialGradients = new();
            for (int sub = 0; sub < RainbowColors.Count; sub++)
            {
                Dictionary<int, ColorGradient> dict = new();
                List<Color> subset = GetRainbowColorSubset(sub);
                for (int i = 0; i < subset.Count; i++)
                {
                    int extraStart = sub switch // sub is numFishingLines - 1
                    {
                        0 => 0,
                        1 => 0,
                        2 => 1,
                        3 => 1,
                        4 => 1,
                        5 => 4,
                        _ => sub / 2 + 2
                    };
                    if (sub < 5)
                    {
                        dict.Add(i, FromCollectionWithStartIndex(subset, i, extraStart: extraStart, extraLoops: 0));
                    }
                    else
                    {
                        dict.Add(i, FromCollectionWithStartIndexLoopBack(subset, i, extraStart: extraStart));
                    }
                }
                partialGradients.Add(sub, dict);
            }
            return partialGradients;
        }

        public ColorGradient(List<Color> colors = null)
        {
            if (colors is List<Color> c)
            {
                c.ForEach(color => AddColor(color));
            }
        }

        public static ColorGradient FromCollectionWithStartIndex(List<Color> colors, int startIndex, int extraStart = 0, int extraLoops = 0)
        {
            ColorGradient grad = new();
            for (int i = 0; i < extraStart; i++)
            {
                grad.AddColor(colors[startIndex]);
            }
            int length = colors.Count;
            for (int currIndex = startIndex, i = 0; i < length + extraLoops; currIndex = (currIndex + 1) % length, i++)
            {
                grad.AddColor(colors[currIndex]);
            }
            return grad;
        }

        public static ColorGradient FromCollectionWithStartIndexLoopBack(List<Color> colors, int startIndex, int extraStart = 0)
        {
            ColorGradient grad = new();
            int direction = startIndex >= colors.Count / 2 ? -1 : 1;

            for (int i = 0; i < extraStart; i++)
            {
                grad.AddColor(colors[startIndex]);
            }

            if (direction < 0)
            {
                for (int i = startIndex; i >= 0; i--)
                {
                    grad.AddColor(colors[i]);
                }
                for (int i = 0; i < colors.Count; i++)
                {
                    grad.AddColor(colors[i]);
                }
            }
            else
            {
                for (int i = startIndex; i < colors.Count; i++)
                {
                    grad.AddColor(colors[i]);
                }
                for (int i = colors.Count - 1; i >= 0; i--)
                {
                    grad.AddColor(colors[i]);

                }
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
            inc = (float) Math.Round(1f / (n - 1f), 4);
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
            float p = progress % inc / inc; // little p is our progress between currIndex and nextIndex
            if (nextIndex >= n)
            {
                nextIndex = currIndex; // if we are on the last color of the gradient next will be out of bounds
            }

            return Color.Lerp(colors[currIndex], colors[nextIndex], p);
        }
    }
}
