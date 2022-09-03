using GarnsMod.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using static GarnsMod.Content.Items.Tools.GarnsFishingRod;
using static Terraria.Graphics.VertexStrip;

namespace GarnsMod.Content.Shaders;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct GradientTrailDrawer
{
    private static VertexStrip vertexStrip = new VertexStrip();

    internal void Draw(Projectile proj, ColorGradient grad, TrailTypeMode typeMode, Vector2 offset = default)
    {
        MiscShaderData miscShaderData = GameShaders.Misc[$"TrailShader{typeMode.Name}"];
        miscShaderData.UseSaturation(-2.8f);
        miscShaderData.UseOpacity(2f);
        miscShaderData.Apply();

        Color StripColorFunc(float progress) => grad.GetColor(progress);

        vertexStrip.PrepareStripWithProceduralPadding(proj.oldPos, proj.oldRot, StripColorFunc, GetStripWidthFunc(typeMode), -Main.screenPosition + proj.Size + offset, includeBacksides: false);
        vertexStrip.DrawTrail();
        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
    }

    internal void Draw(Projectile proj, Color beginning, Color end, TrailTypeMode typeMode, Vector2 offset = default)
    {
        Draw(proj, new ColorGradient(new List<Color> { beginning, end }), typeMode, offset);
    }

    internal static StripHalfWidthFunction GetStripWidthFunc(TrailTypeMode typeMode)
    {
        if (typeMode == TrailTypeMode.Plain)
        {
            return (progress) => MathHelper.Lerp(10f, 40f, progress);
        }
        if (typeMode == TrailTypeMode.Fire)
        {
            return (progress) => MathHelper.Lerp(10f, 35f, progress);
        }

        return (progress) => MathHelper.Lerp(10f, 60f, progress);
    }
}
