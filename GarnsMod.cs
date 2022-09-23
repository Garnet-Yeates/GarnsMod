using IL.Terraria.DataStructures;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace GarnsMod
{
    public partial class GarnsMod : Mod
    {
        public override void Load()
        {
            Ref<Effect> vertexPixelShaderRef = Main.VertexPixelShaderRef;
            // Fire is like Rainbow rod but the tail end of it is like magic missile (plain middle, firey tail)
            GameShaders.Misc["TrailShaderFire"] = new MiscShaderData(vertexPixelShaderRef, "MagicMissile").UseProjectionMatrix(doUse: true);
            GameShaders.Misc["TrailShaderFire"].UseImage0("Images/Extra_" + (short)195);
            GameShaders.Misc["TrailShaderFire"].UseImage1("Images/Extra_" + (short)197);
            GameShaders.Misc["TrailShaderFire"].UseImage2("Images/Extra_" + (short)193);

            // Stream uses same shaders as Rainbow Rod (plain middle, streamy end)
            GameShaders.Misc["TrailShaderStream"] = new MiscShaderData(vertexPixelShaderRef, "MagicMissile").UseProjectionMatrix(doUse: true);
            GameShaders.Misc["TrailShaderStream"].UseImage0("Images/Extra_" + (short)195);
            GameShaders.Misc["TrailShaderStream"].UseImage1("Images/Extra_" + (short)197);
            GameShaders.Misc["TrailShaderStream"].UseImage2("Images/Extra_" + (short)196);

            // Stream2 uses same shaders as Magic Missile (jagged middle, firey tail)
            GameShaders.Misc["TrailShaderStream2"] = new MiscShaderData(vertexPixelShaderRef, "MagicMissile").UseProjectionMatrix(doUse: true);
            GameShaders.Misc["TrailShaderStream2"].UseImage0("Images/Extra_" + (short)192);
            GameShaders.Misc["TrailShaderStream2"].UseImage1("Images/Extra_" + (short)194);
            GameShaders.Misc["TrailShaderStream2"].UseImage2("Images/Extra_" + (short)193);

            // Plain is like Rainbow Rod but the tail is the same as the middle (plain middle, plain end)
            GameShaders.Misc["TrailShaderPlain"] = new MiscShaderData(vertexPixelShaderRef, "MagicMissile").UseProjectionMatrix(doUse: true);
            GameShaders.Misc["TrailShaderPlain"].UseImage0("Images/Extra_" + (short)195);
            GameShaders.Misc["TrailShaderPlain"].UseImage1("Images/Extra_" + (short)197);
            GameShaders.Misc["TrailShaderPlain"].UseImage2("Images/Extra_" + (short)197);
        }

        public override void Unload()
        {
            // The Unload() methods can be used for unloading/disposing/clearing special objects, unsubscribing from events, or for undoing some of your mod's actions.
            // Be sure to always write unloading code when there is a chance of some of your mod's objects being kept present inside the vanilla assembly.
            // The most common reason for that to happen comes from using events, NOT counting On.* and IL.* code-injection namespaces.
            // If you subscribe to an event - be sure to eventually unsubscribe from it.

            // NOTE: When writing unload code - be sure use 'defensive programming'. Or, in other words, you should always assume that everything in the mod you're unloading might've not even been initialized yet.
            // NOTE: There is rarely a need to null-out values of static fields, since TML aims to completely dispose mod assemblies in-between mod reloads.
        }

    }
}