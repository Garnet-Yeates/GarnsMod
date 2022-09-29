using GarnsMod.CodingTools;
using Humanizer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
            int x = (Main.tile[tileX, tileY].TileFrameX != 0 && Main.tile[tileX, tileY].TileFrameX != 36) ? (tileX - 1) : tileX;
            int y = (Main.tile[tileX, tileY].TileFrameY != 0) ? (tileY - 1) : tileY;

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
                rulesToTry.ForEach(rule => CustomItemDropResolver.ResolveRule(rule, CustomItemDropResolver.CreateDropAttemptInfo(new Rectangle(x * 16, y * 16, 32, 32))));
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
                rulesToTry.ForEach(rule => CustomItemDropResolver.ResolveRule(rule, CustomItemDropResolver.CreateDropAttemptInfo(new Rectangle(x * 16, y * 16, 32, 32))));
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


    class NPCLootExtensionTest : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // Goal: Remove BoneSword CommonDrop from being chained onto AncientGoldHelmet CommonDrop, which is chained onto AncientIron CommonDrop
            // Constraints:
            //     The BoneSword drop must a CommonDrop chained to its parent via a TryIfFailedRandomRoll chain. Its itemId must be BoneSword
            //     The BoneSword's immediate parent (parent 1) must be a CommonDrop chained to its parent via TryIfFailedRandomRoll, and it must drop AncientGoldHelmet
            //     The BoneSword's second parent (parent 2) (aka parent 1's immediate parent) must be a CommonDrop that drops AncientIronHelmet (no chain constraint!!)
            // Chains will be re-attached so further loot (Skull drop) is not lost
            if (npc.type == NPCID.Skeleton)
            {
                npcLoot.RemoveWhere<TryIfFailedRandomRoll, CommonDrop>(
                    boneSwordDrop =>
                        boneSwordDrop.itemId == ItemID.BoneSword &&
                        boneSwordDrop.HasParentRuleWhere<TryIfFailedRandomRoll, CommonDrop>(ancientGoldDrop => ancientGoldDrop.itemId == ItemID.AncientGoldHelmet, nthParent: 1) &&
                        boneSwordDrop.HasParentRuleWhere<CommonDrop>(ancientIronDrop => ancientIronDrop.itemId == ItemID.AncientIronHelmet, nthParent: 2),
                    reattachChains: true
                );
            }

            // Skeletron Boss: Remove book of skulls drop rule from being failure chained onto SkeletronHandRule, which is failure chained onto SkeletronMaskRule 
            if (npc.type == NPCID.SkeletronHead)
            {
                npcLoot.RemoveWhere<CommonDrop>(
                    bookOfSkullsDrop =>
                        bookOfSkullsDrop.itemId == ItemID.BookofSkulls &&
                        bookOfSkullsDrop.HasParentRuleWhere<TryIfFailedRandomRoll, CommonDrop>(skeletronHandRule => skeletronHandRule.itemId == ItemID.SkeletronHand, nthParent: 1) &&
                        bookOfSkullsDrop.HasParentRuleWhere<CommonDrop>(skeletronMaskRule => skeletronMaskRule.itemId == ItemID.SkeletronMask, nthParent: 2),
                    reattachChains: true
                );
            }



            // Remove FrostBrand from Ice Mimic options
            if (npc.type == NPCID.IceMimic)
            {
                OneFromOptionsDropRule iceMimicOptions = npcLoot.FindRuleWhere<TryIfFailedRandomRoll, OneFromOptionsDropRule>(
                    rule => rule.HasParentRuleWhere<CommonDrop>(toySledDrop => toySledDrop.itemId == ItemID.ToySled, nthParent: 1)
                );
                iceMimicOptions.dropIds = iceMimicOptions.dropIds.Where(itemId => itemId != ItemID.Frostbrand).ToArray();
            }


            // TONS OF EXAMPLES OF USING THE EXTENSIONS IN HERE

            if (npc.type == NPCID.GreenSlime)
            {
                // FIRST BRANCH
                // Hellstone chained onto Obsidian chained onto Gold chained onto Silver chained onto Iron chained onto Copper
                npcLoot.Add(ItemDropRule.Common(ItemID.CopperOre, 2))
                    .OnSuccess(ItemDropRule.Common(ItemID.IronOre, 2))
                        .OnSuccess(ItemDropRule.Common(ItemID.SilverOre, 2))
                            .OnSuccess(ItemDropRule.Common(ItemID.GoldOre, 2))
                                .OnSuccess(ItemDropRule.Common(ItemID.Obsidian, 2))
                                    .OnSuccess(ItemDropRule.Common(ItemID.Hellstone, 2));

                // SECOND BRANCH
                // Silver chained onto Gold chained onto Silver chained onto Iron chained onto Silver chained onto Copper
                npcLoot.Add(ItemDropRule.Common(ItemID.CopperOre, 4, 2, 2))
                    .OnFailedRoll(ItemDropRule.Common(ItemID.SilverOre, 4, 2, 2))
                        .OnFailedRoll(ItemDropRule.Common(ItemID.IronOre, 4, 2, 2))
                            .OnFailedRoll(ItemDropRule.Common(ItemID.SilverOre, 4, 2, 2))
                                .OnFailedRoll(ItemDropRule.Common(ItemID.GoldOre, 4, 2, 2))
                                    .OnFailedRoll(ItemDropRule.Common(ItemID.SilverOre, 4, 2, 2));


                // Remove all CommonDrop(Silver) in the loot tree
                if (false)
                {
                    npcLoot.RemoveWhere<CommonDrop>(
                        silverDrop => silverDrop.itemId == ItemID.SilverOre,
                        stopAtFirst: false,
                        reattachChains: true
                    );
                }

                // Remove all CommonDrop(Silver) that is chained to IronOre
                if (false)
                {
                    npcLoot.RemoveWhere<CommonDrop>(
                        silverDrop =>
                            silverDrop.itemId == ItemID.SilverOre &&
                            silverDrop.HasParentRuleWhere<CommonDrop>(ironDrop => ironDrop.itemId == ItemID.IronOre, nthParent: 1),
                        stopAtFirst: false,
                        reattachChains: true
                    ); ;
                }

                // Remove all CommonDrop(Silver) that is chained to IronOre via TryIfSucceeded chain
                if (false)
                {
                    npcLoot.RemoveWhere<TryIfSucceeded, CommonDrop>(
                        silverDrop =>
                            silverDrop.itemId == ItemID.SilverOre &&
                            silverDrop.HasParentRuleWhere<CommonDrop>(ironDrop => ironDrop.itemId == ItemID.IronOre, nthParent: 1),
                        stopAtFirst: false,
                        reattachChains: true
                    );
                }


                // Remove all CommonDrop(Silver) that is chained to IronOre. The IronOre must be chained to its parent via a TryIfFailedRandomRoll chain
                if (false)
                {
                    npcLoot.RemoveWhere<CommonDrop>(
                        silverDrop =>
                            silverDrop.itemId == ItemID.SilverOre &&
                            silverDrop.HasParentRuleWhere<TryIfFailedRandomRoll, CommonDrop>(ironDrop => ironDrop.itemId == ItemID.IronOre, nthParent: 1),
                        stopAtFirst: false,
                        reattachChains: true
                    );
                }

                // THIS ONE SHOWS JUST HOW SPECIFIC YOU CAN GET. THIS MATCHES THE EXACT GEOMETRY OF THE FIRST BRANCH. ***Checking the children isn't rlly necessary***

                // Change false to true to test
                // Removes the first SilverOre CommonDrop who is chained from its parent via a TryIfFailedRandomRoll, whose parent is an IronOre CommonDrop chained via a TryIfFailedRandomRoll, whose parent is a CopperOre CommonDrop
                // the SilverOre CommonDrop must also have a GoldOre CommonDrop in its ChainedRules chained by TryIfFailedRandomRoll, that has an ObsidianOre CommonDrop in its ChainedRules chained by TryIfFailedRandomRoll,
                // that has a HellstoneOre CommonDrop in its ChainedRules chained by TryIfFailedRandomRoll. Super specific.
                // Chains will be reattached after removing, meaning that the GoldOre CommonDrop will now be chained onto the parent of the removed CommonDrop
                // if it exists (in this case IronOre)
                if (false)
                {
                    npcLoot.RemoveWhere<TryIfFailedRandomRoll, CommonDrop>(
                        cd =>
                            cd.HasParentRuleWhere<CommonDrop>(copperDrop => copperDrop.itemId == ItemID.CopperOre, nthParent: 2) &&
                            cd.HasParentRuleWhere<TryIfFailedRandomRoll, CommonDrop>(ironDrop => ironDrop.itemId == ItemID.IronOre, nthParent: 1) &&
                            cd.itemId == ItemID.SilverOre &&
                            cd.HasChildWhere<TryIfFailedRandomRoll, CommonDrop>(goldDrop => goldDrop.itemId == ItemID.GoldOre, nthChild: 1) &&
                            cd.HasChildWhere<TryIfFailedRandomRoll, CommonDrop>(obsidianDrop => obsidianDrop.itemId == ItemID.Obsidian, nthChild: 2) &&
                            cd.HasChildWhere<TryIfFailedRandomRoll, CommonDrop>(hellstoneDrop => hellstoneDrop.itemId == ItemID.Hellstone, nthChild: 3),
                        reattachChains: true, // if this was false, Gold, Obsidian, and Hellstone drops would be lost too
                        stopAtFirst: true // stopAtFirst is true by default, just showing it here so you knowit exists
                    ); ;
                }
                
            }
        }
    }
}
