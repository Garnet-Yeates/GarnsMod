using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using static Terraria.Graphics.VertexStrip;

namespace GarnsMod;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct MyShaderDrawer
{
    private static VertexStrip _vertexStrip = new VertexStrip();

    public void DrawGradient(Projectile proj, ColorGradient grad, Vector2 offset = default)
    {
        MiscShaderData miscShaderData = GameShaders.Misc["TrailShaderFirey"];
        miscShaderData.UseSaturation(-2.8f);
        miscShaderData.UseOpacity(2f);
        miscShaderData.Apply();

        Color stripColorFunc(float progress) => grad.GetColor(progress);
        float stripWidthFunc(float progress) => MathHelper.Lerp(10f, 25f, progress);

        MyShaderDrawer._vertexStrip.PrepareStripWithProceduralPadding(GetOldPosArrayWithOffset(proj, offset), proj.oldRot, stripColorFunc, stripWidthFunc, -Main.screenPosition + proj.Size / 2f, includeBacksides: true);
        MyShaderDrawer._vertexStrip.DrawTrail();
        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
    }

    public void Draw(Projectile proj, Color beginning, Color end, Vector2 offset = default)
    {
        // 190, 197, 193
 
        MiscShaderData miscShaderData = GameShaders.Misc["TrailShaderPlain"];
        miscShaderData.UseSaturation(-2.8f);
        miscShaderData.UseOpacity(2f);
        miscShaderData.Apply();

        Color stripColorFunc(float progress) => Color.Lerp(beginning, end, progress);
        float stripWidthFunc(float progress) => MathHelper.Lerp(15f, 45f, progress);

        MyShaderDrawer._vertexStrip.PrepareStripWithProceduralPadding(GetOldPosArrayWithOffset(proj, offset), proj.oldRot, stripColorFunc, stripWidthFunc, -Main.screenPosition + proj.Size / 2f, includeBacksides: true);
        MyShaderDrawer._vertexStrip.DrawTrail();
        Main.pixelShader.CurrentTechnique.Passes[0].Apply();
    }

    public static Vector2[] GetOldPosArrayWithOffset(Projectile proj, Vector2 offset = default)
    {
        Vector2[] oldPos = proj.oldPos;
        if (offset != new Vector2(0, 0))
        {
            oldPos = new Vector2[oldPos.Length];
            for (int i = 0; i < proj.oldPos.Length; i++)
            {
                oldPos[i] = proj.oldPos[i];
                if (proj.oldPos[i] != new Vector2(0, 0))
                {
                    oldPos[i] += offset;

                }
            }
        }
        return oldPos;
    }
}
