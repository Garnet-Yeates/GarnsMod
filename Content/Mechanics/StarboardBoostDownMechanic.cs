using GarnsMod.CodingTools;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GarnsMod.Content.Mechanics
{
    // This class adds the actual downwards boost mechanic to the wings
    internal class StarboardBoostDownPlayer : ModPlayer
    {
        const int StarboardWingID = 45;

        public override void PreUpdateMovement()
        {
            if (Player.wings == StarboardWingID)
            {
                if (Player.controlDown && !Player.controlJump)
                {
                    Player.velocity += new Vector2(0, 1.5f);
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
