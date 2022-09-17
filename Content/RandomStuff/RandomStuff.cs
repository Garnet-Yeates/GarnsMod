using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent.Achievements;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static Terraria.GameContent.ItemDropRules.Chains;

namespace GarnsMod.Content.RandomStuff
{
    internal class TileTest : GlobalTile
    {
        public override bool Drop(int tileX, int tileY, int type)
        {
            // Return true if it isn't shadow orb/crimson heart as we don't want to modify logic for other tile such as life crystal
            if (type != TileID.ShadowOrbs)
            {
                return true;
            }

            // When you break the tile you 'initiate' the breaking, but WorldGen is actually destroying the 4 segmentsof this tile.
            // This whole method gets called 5 times but only one of those times is the time where YOU initiated the breaking.
            // WorldGen will set destroyObject to true and do the actual deleting of the tile. This basically makes sure this is called once and you
            // don't get 5x the intended effects
            if (WorldGen.destroyObject)
            {
                return false;
            }

            // I think shadow orb and crimson heart use the same texture just different animation frames so they use this to check which one it is (shadow or crim)
            // 16px with 2 px of padding makes 18x18px frames. Pixels from 0 to 17 are for the left side of the shadow orb. 18 to 35 are the right side of it
            // 36 to 53 are left side of crimson heart... etc. They use this to figure out whether a crimson heart or shadow orb is being smashed
            short frameX = Main.tile[tileX, tileY].TileFrameX;

            bool isCrimsonHeart = frameX >= 36;

            // This code adjusts the tileX and Y that we broke to make it the top left of the tile
            int x = ((Main.tile[tileX, tileY].TileFrameX != 0 && Main.tile[tileX, tileY].TileFrameX != 36) ? (tileX - 1) : tileX);
            int y = ((Main.tile[tileX, tileY].TileFrameY != 0) ? (tileY - 1) : tileY);

            if (isCrimsonHeart) // CRIMSON HEART LOGIC
            {
                // TODO either replace one of the choices, add another choice, or both, or add an additional drop on top of the choice. Or all 3. You see where I'm going with this
                List<IItemDropRule> rulesToTry = new()
                {
                    new CommonDrop(ItemID.CrimtaneOre, 1, 20, 60),
                    new CoinsRule(Item.sellPrice(0, 0, 80, 0), true),
                    new OneFromRulesRule
                    (
                        chanceDenominator: 1, chanceNumerator: 1,
                        new CommonDrop(ItemID.SoulofFright, 1, 5, 10),
                        new CommonDrop(ItemID.SoulofMight, 1, 5, 10),
                        new CommonDrop(ItemID.SoulofSight, 1, 5, 10),
                        new CommonDrop(ItemID.SoulofLight, 1, 5, 10),
                        new CommonDrop(ItemID.SoulofNight, 1, 5, 10),
                        new CommonDrop(ItemID.SoulofFlight, 1, 5, 10)
                    )
                };
                rulesToTry.ForEach(rule => CustomItemDropResolver.ResolveRule(rule, CustomItemDropResolver.GetDropAttemptInfo(new Rectangle(x * 16, y * 16, 32, 32))));
            }
            else // SHADOW ORB LOGIC
            {
                // TODO either replace one of the choices, add another choice, or both, or add an additional drop on top of the choice. Or all 3. You see where I'm going with this

                // Example of me using this. Make sure it is never called on mp client netmode
                List<IItemDropRule> rulesToTry = new()
                {
                    new CommonDrop(ItemID.DemoniteOre, 1, 20, 60),
                    new CoinsRule(Item.sellPrice(0, 0, 80, 0), true),
                    new OneFromRulesRule
                    (
                        chanceDenominator: 1, chanceNumerator: 1,
                        new CommonDrop(ItemID.SoulofFright, 1, 5, 10),
                        new CommonDrop(ItemID.SoulofMight, 1, 5, 10),
                        new CommonDrop(ItemID.SoulofSight, 1, 5, 10),
                        new CommonDrop(ItemID.SoulofLight, 1, 5, 10),
                        new CommonDrop(ItemID.SoulofNight, 1, 5, 10),
                        new CommonDrop(ItemID.SoulofFlight, 1, 5, 10)
                    )
                };
                rulesToTry.ForEach(rule => CustomItemDropResolver.ResolveRule(rule, CustomItemDropResolver.GetDropAttemptInfo(new Rectangle(x*16, y*16, 32, 32))));
            }

            // Has to do with spawing the corresponding boss and putting text in the chat. If you forget this part then the bosses wont spawn
            WorldGen.shadowOrbSmashed = true;
            WorldGen.shadowOrbCount++;
            if (WorldGen.shadowOrbCount >= 3)
            {
                // They wrote this statement in a confusing way where they distributed a negation on the right side but didnt on the left side, so removed the distribution completely
                // It is just making sure that the corresponding boss isn't already alive. If it is, nothing happens
                if (!(NPC.AnyNPCs(NPCID.BrainofCthulhu) && isCrimsonHeart) && !(NPC.AnyNPCs(NPCID.EaterofWorldsHead) && !isCrimsonHeart))
                {
                    WorldGen.shadowOrbCount = 0;
                    float num5 = x * 16;
                    float num6 = y * 16;
                    float num7 = -1f;
                    int plr = 0;
                    for (int num8 = 0; num8 < 255; num8++)
                    {
                        float num9 = Math.Abs(Main.player[num8].position.X - num5) + Math.Abs(Main.player[num8].position.Y - num6);
                        if (num9 < num7 || num7 == -1f)
                        {
                            plr = num8;
                            num7 = num9;
                        }
                    }
                    if (isCrimsonHeart)
                    {
                        NPC.SpawnOnPlayer(plr, 266);
                    }
                    else
                    {
                        NPC.SpawnOnPlayer(plr, 13);
                    }
                }
            }
            else
            {
                LocalizedText localizedText = Lang.misc[10];
                if (WorldGen.shadowOrbCount == 2)
                {
                    localizedText = Lang.misc[11];
                }
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    Main.NewText(localizedText.ToString(), 50, byte.MaxValue, 130);
                }
                else if (Main.netMode == NetmodeID.Server)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromKey(localizedText.Key), new Color(50, 255, 130));
                }
            }
            AchievementsHelper.NotifyProgressionEvent(7); return false; // Return false so that the vanilla logic doesn't run
        }
    }

    class MyNPCLoot : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == NPCID.IceMimic)
            {
                foreach (var rule in npcLoot.Get())
                {
                    if (rule is CommonDrop commonDrop)
                    {
                        foreach (var chainedRule in commonDrop.ChainedRules)
                        {
                            if (chainedRule is TryIfFailedRandomRoll tryIfFailedRoll && tryIfFailedRoll.RuleToChain is OneFromOptionsDropRule ofoDrop && ofoDrop.dropIds.Contains(ItemID.Frostbrand))
                            {
                                List<int> dropIds = ofoDrop.dropIds.ToList();
                                dropIds.Remove(ItemID.Frostbrand);
                                ofoDrop.dropIds = dropIds.ToArray();
                            }
                        }
                    }
                }
            }
        }
    }


    
}
