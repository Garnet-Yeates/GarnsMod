using GarnsMod.CodingTools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.Graphics.VertexStrip;

namespace GarnsMod.Content.Shaders
{
    public class ShaderSystem : ModSystem
    {
        public override void Load()
        {
            Ref<Effect> vertexPixelShaderRef = Main.VertexPixelShaderRef;

            GameShaders.Misc["TrailShaderStream"] = new MiscShaderData(vertexPixelShaderRef, "MagicMissile").UseProjectionMatrix(doUse: true);
            GameShaders.Misc["TrailShaderStream"].UseImage0("Images/Extra_" + (short)192);
            GameShaders.Misc["TrailShaderStream"].UseImage1("Images/Extra_" + (short)197);
            GameShaders.Misc["TrailShaderStream"].UseImage2("Images/Extra_" + (short)196);

            GameShaders.Misc["TrailShaderFire"] = new MiscShaderData(vertexPixelShaderRef, "MagicMissile").UseProjectionMatrix(doUse: true);
            GameShaders.Misc["TrailShaderFire"].UseImage0("Images/Extra_" + (short)192);
            GameShaders.Misc["TrailShaderFire"].UseImage1("Images/Extra_" + (short)197);
            GameShaders.Misc["TrailShaderFire"].UseImage2("Images/Extra_" + (short)193);

            GameShaders.Misc["TrailShaderPlain"] = new MiscShaderData(vertexPixelShaderRef, "MagicMissile").UseProjectionMatrix(doUse: true);
            GameShaders.Misc["TrailShaderPlain"].UseImage0("Images/Extra_" + (short)192);
            GameShaders.Misc["TrailShaderPlain"].UseImage1("Images/Extra_" + (short)197);
            GameShaders.Misc["TrailShaderPlain"].UseImage2("Images/Extra_" + (short)197);
        }
    }

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
                col.A = (byte)(col.A * colorAlphaModifier);
                return col;
            }

            // OldPos is top left. We want the trail to be at the center + any additionaly offset they want. We also need to subtract the screen position to get the relative screen loc
            offset -= Main.screenPosition;
            if (padding)
            {
                vertexStrip.PrepareStripWithProceduralPadding(proj.oldPos, proj.oldRot, StripColorFunc, overrideWidthFunction ?? trailType.WidthFunction, offsetForAllPositions: offset, includeBacksides: true);
            }
            else
            {
                vertexStrip.PrepareStrip(proj.oldPos, proj.oldRot, StripColorFunc, overrideWidthFunction ?? trailType.WidthFunction, offsetForAllPositions: offset, includeBacksides: true);
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

        public static readonly TrailType Plain = new("Plain", saturation: 0.0f, opacity: 2.5f, widthFunc: PlainWidthFunction, alphaModifier: 0.45f);
        public static readonly TrailType Stream = new("Stream", saturation: 1.5f, opacity: 4.0f, widthFunc: StreamWidthFunction, alphaModifier: 0.45f);
        public static readonly TrailType Fire = new("Fire", saturation: -1f, opacity: 6.0f, widthFunc: FireWidthFunction, alphaModifier: 0.45f);

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
            ItemID iod;
            if (progress < 0.25f)
            {
                return MathHelper.Lerp(8f, 20f, progress * (1.0f / 0.25f));
            }
            if (progress >= 0.25f && progress < 0.50f)
            {
                return MathHelper.Lerp(20f, 30f, (progress - 0.25f) * (1.0f / 0.25f));
            }

            return MathHelper.Lerp(30, 40f, (progress - 0.5f) * (1.0f / 0.5f));
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

        public static float FireWidthFunction(float progress)
        {
            if (progress < 0.25f)
            {
                return MathHelper.Lerp(0f, 15f, progress * (1.0f / 0.25f));
            }
            if (progress >= 0.25f && progress < 0.50f)
            {
                return MathHelper.Lerp(15f, 30f, (progress - 0.25f) * (1.0f / 0.25f));
            }

            return MathHelper.Lerp(30f, 15f, (progress - 0.5f) * (1.0f / 0.5f));
        }

        public static explicit operator int(TrailType m) => m.Value;

        public static implicit operator TrailType(int i) => typeModes[i];

        public static bool operator ==(TrailType m1, TrailType m2) => m1.Value == m2.Value;
        public static bool operator !=(TrailType m1, TrailType m2) => m1.Value != m2.Value;
        public override int GetHashCode() => Value.GetHashCode();
    }
}

