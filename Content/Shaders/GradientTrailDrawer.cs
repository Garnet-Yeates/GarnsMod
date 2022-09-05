using GarnsMod.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
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

        internal void Draw(Projectile proj, ColorGradient grad, TrailType trailType, Vector2 offset = default, float? overrideSaturation = null, float? overrideOpacity = null, StripHalfWidthFunction overrideWidthFunction = null, int progressModifier = 0)
        {
            MiscShaderData miscShaderData = GameShaders.Misc[$"TrailShader{trailType.ShaderName}"];
            miscShaderData.UseSaturation(overrideSaturation ?? trailType.Saturation);
            miscShaderData.UseOpacity(overrideOpacity ?? trailType.Opacity);
            miscShaderData.Apply();

            Color StripColorFunc(float progress) => grad.GetColor(Modulo(progress + progressModifier / 1000f, 1.00f));
  
            // OldPos is top left. We want the trail to be at the center + any additionaly offset they want. We also need to subtract the screen position to get the relative screen loc
            offset += proj.Size / 2 - Main.screenPosition;
            vertexStrip.PrepareStripWithProceduralPadding(proj.oldPos, proj.oldRot, StripColorFunc, overrideWidthFunction ?? trailType.WidthFunction, offsetForAllPositions: offset, includeBacksides: false);
            vertexStrip.DrawTrail();
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }


        // C# % works strangely for negative numbers, this makes it work like modulo
        private static float Modulo(float a, float b)
        {
            return a - b * (float) Math.Floor(a / b);
        }
    }

    internal readonly struct TrailType
    {
        internal static List<TrailType> typeModes = new();
        private int Value { get; }

        public static int Count => typeModes.Count;

        public static readonly TrailType Plain = new("Plain", saturation: 0.0f, opacity: 2.8f, PlainWidthFunction);
        public static readonly TrailType Fire = new("Fire", saturation: -1.25f, opacity: 8.0f, FireWidthFunction);
        public static readonly TrailType Stream = new("Stream", saturation: 1.5f, opacity: 2.5f, StreamWidthFunction);
        
        
        public static readonly TrailType NightglowBG = new("Plain", saturation: 0.0f, opacity: 0.3f, NightGlowBGWidthFunction);
        public static readonly TrailType NightglowFG = new("Plain", saturation: 0.0f, opacity: 1.25f, NightGlowFGWidthFunction);

        internal string ShaderName { get; }
        internal float Saturation { get; }
        internal float Opacity { get; }

        internal StripHalfWidthFunction WidthFunction { get; }

        private TrailType(string shaderName, float saturation, float opacity, StripHalfWidthFunction widthFunc)
        {
            Value = typeModes.Count;
            ShaderName = shaderName;
            Saturation = saturation;
            Opacity = opacity;
            WidthFunction = widthFunc;
            typeModes.Add(this);
        }

        public static float PlainWidthFunction(float progress)
        {
            if (progress < 0.1f)
            {
                return MathHelper.Lerp(0f, 15f, progress * (1.0f / 0.1f));
            }
            return MathHelper.Lerp(15f, 50f, (progress - 0.1f) * (1.0f / 0.8f));
        }

        public static float FireWidthFunction(float progress)
        {
            if (progress < 0.25f)
            {
                return MathHelper.Lerp(0f, 12.5f, progress * (1.0f / 0.25f));
            }
            if (progress >= 0.25f && progress < 0.50f)
            {
                return MathHelper.Lerp(12.5f, 20f, (progress - 0.25f) * (1.0f / 0.25f));
            }

            return MathHelper.Lerp(20f, 10f, (progress - 0.5f) * (1.0f / 0.5f));
        }
        
        public static float StreamWidthFunction(float progress)
        {
            if (progress < 0.25f)
            {
                return MathHelper.Lerp(0f, 20f, progress * (1.0f / 0.25f));
            }
            if (progress >= 0.25f && progress < 0.50f)
            {
                return MathHelper.Lerp(20f, 45f, (progress - 0.25f) * (1.0f / 0.25f));
            }

            return MathHelper.Lerp(45f, 15f, (progress - 0.5f) * (1.0f / 0.5f));
        }

        public static float NightGlowBGWidthFunction(float progress) => MathHelper.Lerp(15f, 120f, progress);
        public static float NightGlowFGWidthFunction(float progress) => MathHelper.Lerp(0f, 30f, progress);


        public static explicit operator int(TrailType m) => m.Value;

        public static implicit operator TrailType(int i) => typeModes[i];

        public static bool operator ==(TrailType m1, TrailType m2) => m1.Value == m2.Value;
        public static bool operator !=(TrailType m1, TrailType m2) => m1.Value != m2.Value;
        public override int GetHashCode() => Value.GetHashCode();
    }
}

