using GarnsMod.CodingTools;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

// This .cs file acts like its own folder (has it's on sub-namespace under Mechanics namespace)
namespace GarnsMod.Content.Mechanics.StarboardBoostDownMechanic
{
    // This class adds the actual downwards boost mechanic to the wings
    internal class StarboardBoostDownPlayer : ModPlayer
    {
        const int StarboardWingID = 45;

        public override void PreUpdateMovement()
        {
            if (Player.wings == StarboardWingID)
            {
                int x = (int)Player.Center.X / 16;
                int y = (int)Player.Center.Y / 16 + 2;

                if (Main.tile[x - 1, y].TileType == 0 && Main.tile[x, y].TileType == 0 && Main.tile[x + 1, y].TileType == 0)
                {
                    if (Player.TryingToHoverDown && !Player.controlJump)
                    {
                        Player.velocity += new Vector2(0, 2f);
                    }
                }

            }
        }
    }

    // This class safely changes the tooltip of the starboard item to display the mechanic
    internal class CelestialStarboard : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            return item.type == ItemID.LongRainbowTrailWings;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.InsertAfter("Hold UP to boost faster!", Mod, "wingStat", "Hold DOWN to fall faster!");
        }
    }
}
