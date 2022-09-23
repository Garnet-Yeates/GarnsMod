using GarnsMod.CodingTools;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using static Terraria.Graphics.VertexStrip;

namespace GarnsMod.Content.Shaders
{
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    // Instanced per projectile per PreDraw() call
    public struct GradientTrailDrawer
    {
        private static readonly VertexStrip vertexStrip = new();

        internal void Draw(Projectile proj, ColorGradient grad, TrailType trailType, Vector2 offset = default, float? overrideSaturation = null, float? overrideOpacity = null, StripHalfWidthFunction overrideWidthFunction = null, int progressModifier = 0, bool padding = true, float? colorAlphaModifier = null)
        {
            MiscShaderData miscShaderData = GameShaders.Misc[$"TrailShader{trailType.ShaderName}"];
            miscShaderData.UseSaturation(overrideSaturation ?? trailType.Saturation);
            miscShaderData.UseOpacity(overrideOpacity ?? trailType.Opacity);
            miscShaderData.Apply();

            colorAlphaModifier ??= trailType.AlphaModifier;

            // Progress is a float between 0 and 1 where 0 is the beginning of the strip right behind the projectile and 1 is the end
            Color StripColorFunc(float progress)
            {
                float usingProgress = GarnMathHelpers.Modulo(progress + progressModifier / 1000f, 1.00f);
                Color col = grad.GetColor(usingProgress);
                col.A = (byte) (col.A * colorAlphaModifier);
                return col;
            }

            // OldPos is top left. We want the trail to be at the center + any additionaly offset they want. We also need to subtract the screen position to get the relative screen loc
            offset -= Main.screenPosition;
            if (padding)
            {
                vertexStrip.PrepareStripWithProceduralPadding(proj.oldPos, proj.oldRot, StripColorFunc, overrideWidthFunction ?? trailType.WidthFunction, offsetForAllPositions: offset, includeBacksides: false);
            }
            else
            {
                vertexStrip.PrepareStrip(proj.oldPos, proj.oldRot, StripColorFunc, overrideWidthFunction ?? trailType.WidthFunction, offsetForAllPositions: offset, includeBacksides: false);

            }
            vertexStrip.DrawTrail();
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }
    }

    internal readonly struct TrailType
    {
        internal static List<TrailType> typeModes = new();

        private int Value { get; }

        public static int Count => typeModes.Count;

        public static readonly TrailType Plain = new("Plain", saturation: 0.0f, opacity: 2.5f, widthFunc: PlainWidthFunction);
        public static readonly TrailType Fire = new("Fire", saturation: -1.25f, opacity: 8.0f, widthFunc: FireWidthFunction);
        public static readonly TrailType Stream = new("Stream", saturation: 1.5f, opacity: 4.0f, widthFunc: StreamWidthFunction);
        public static readonly TrailType Stream2 = new("Stream2", saturation: 0f, opacity: 6.0f, widthFunc: Stream2WidthFunction, alphaModifier: 0.45f);
        
        internal string ShaderName { get; }
        internal float Saturation { get; }
        internal float AlphaModifier { get; }
        internal float Opacity { get; }

        internal StripHalfWidthFunction WidthFunction { get; }

        private TrailType(string shaderName, float saturation, float opacity, StripHalfWidthFunction widthFunc, float alphaModifier = 1f)
        {
            Value = typeModes.Count;
            ShaderName = shaderName;
            Saturation = saturation;
            AlphaModifier = alphaModifier;
            Opacity = opacity;
            WidthFunction = widthFunc;
            typeModes.Add(this);
        }

        public static float PlainWidthFunction(float progress)
        {
            if (progress < 0.2f)
            {
                return MathHelper.Lerp(0f, 15f, progress * (1.0f / 0.2f));
            }
            return MathHelper.Lerp(15f, 75f, (progress - 0.2f) * (1.0f / 0.8f));
        }

        public static float FireWidthFunction(float progress)
        {
            if (progress < 0.25f)
            {
                return MathHelper.Lerp(0f, 14f, progress * (1.0f / 0.25f));
            }
            if (progress >= 0.25f && progress < 0.50f)
            {
                return MathHelper.Lerp(14f, 18f, (progress - 0.25f) * (1.0f / 0.25f));
            }

            return MathHelper.Lerp(18f, 0f, (progress - 0.5f) * (1.0f / 0.5f));
        }
        
        public static float StreamWidthFunction(float progress)
        {
            if (progress < 0.25f)
            {
                return MathHelper.Lerp(5f, 15f, progress * (1.0f / 0.25f));
            }
            if (progress >= 0.25f && progress < 0.50f)
            {
                return MathHelper.Lerp(15f, 20f, (progress - 0.25f) * (1.0f / 0.25f));
            }

            return MathHelper.Lerp(20f, 30f, (progress - 0.5f) * (1.0f / 0.5f));
        }

        public static float Stream2WidthFunction(float progress)
        {
            if (progress < 0.25f)
            {
                return MathHelper.Lerp(10f, 20f, progress * (1.0f / 0.25f));
            }
            if (progress >= 0.25f && progress < 0.50f)
            {
                return MathHelper.Lerp(20f, 30f, (progress - 0.25f) * (1.0f / 0.25f));
            }

            return MathHelper.Lerp(30f, 35f, (progress - 0.5f) * (1.0f / 0.5f));
        }

        public static explicit operator int(TrailType m) => m.Value;

        public static implicit operator TrailType(int i) => typeModes[i];

        public static bool operator ==(TrailType m1, TrailType m2) => m1.Value == m2.Value;
        public static bool operator !=(TrailType m1, TrailType m2) => m1.Value != m2.Value;
        public override int GetHashCode() => Value.GetHashCode();
    }
}

