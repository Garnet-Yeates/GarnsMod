using GarnsMod.CodingTools;
using GarnsMod.Content.Items.Tools;
using GarnsMod.Content.Items.Weapons.Melee;
using GarnsMod.Content.Items.Weapons.Melee.SlasherSwords;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
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
        // Not called on Multiplayer Clients
        public override bool Drop(int tileX, int tileY, int type)
        {
            List<IItemDropRule> rulesToExecute = new()
            {
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

            // This code adjusts the tileX and Y that we broke to make it the top left of the multitile
            tileX = (Main.tile[tileX, tileY].TileFrameX != 0 && Main.tile[tileX, tileY].TileFrameX != 36) ? (tileX - 1) : tileX;
            tileY = (Main.tile[tileX, tileY].TileFrameY != 0) ? (tileY - 1) : tileY;

            Rectangle dropArea = new(tileX * 16, tileY * 16, 32, 32);

            if (isCrimsonHeart) // CRIMSON HEART LOGIC
            {
                rulesToExecute.Add(new CommonDrop(ItemID.CrimtaneOre, 1, 20, 60));
            }
            else // SHADOW ORB LOGIC
            {
                rulesToExecute.Add(new CommonDrop(ItemID.DemoniteOre, 1, 20, 60));
            }

            rulesToExecute.ResolveRules(CustomItemDropResolver.CreateDropAttemptInfo(dropArea));

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
                    float x = tileX * 16;
                    float y = tileY * 16;
                    float num7 = -1f;
                    int plr = 0;
                    for (int i = 0; i < 255; i++)
                    {
                        float num9 = Math.Abs(Main.player[i].position.X - x) + Math.Abs(Main.player[i].position.Y - y);
                        if (num9 < num7 || num7 == -1f)
                        {
                            plr = i;
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

    class ExtensionUseCases : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // Example 1
            //
            // Remove Book of Skulls from Skeletron. Book of skulls is chained onto a CommonDrop(skeletronHandId) chained onto a ItemDropWithCondition(skeletronMaskId)
            // Book of skulls has no further chains after it
            //
            // Normal way of removing a rule that is nested very far down. We must hardcode for-loops to find the exact rule and remove it from its parent rule
            // This is pretty tedious to do do.
            //
            // Not only is it tedious, there are also potential compatibility issues:
            //     What if another mod that was loaded before this mod removes the skeletron mask rule, and moved the skeletron hand rule up one level? (so the
            //     skeletron hand rule is now at the top level, directly under NpcLoot.Get()). If this were the case, then the code below would fail to find
            //     the SkeletronHandRule to remove the book of skulls from, because it is looking for the SkeletronHandRule chained to a SkeletronMaskRule but there
            //     is no longer a SkeletronMaskRule in the loot (so the SkeletronHandRule has no chains, it is a direct child of loot.Get())
            //
            //     Essentially, if the [parent => child => child => ...] chained rule structure changes, these hard-coded loops will fail to find the drop to remove/remove from 
            if (npc.type == NPCID.SkeletronHead && false)
            {
                foreach (IItemDropRule rule in npcLoot.Get())
                {
                    if (rule is ItemDropWithConditionRule skeletronMaskRule && skeletronMaskRule.itemId == ItemID.SkeletronMask && skeletronMaskRule.condition is Conditions.NotExpert)
                    {
                        foreach (IItemDropRuleChainAttempt maskChain in skeletronMaskRule.ChainedRules)
                        {
                            if (maskChain.RuleToChain is CommonDrop skeletronHandRule && skeletronHandRule.itemId == ItemID.SkeletronHand)
                            {
                                IEnumerable<IItemDropRuleChainAttempt> withoutBookOfSkulls = skeletronHandRule.ChainedRules.Where(attempt => !(attempt.RuleToChain is CommonDrop bookOfSkullsDrop && bookOfSkullsDrop.itemId == ItemID.BookofSkulls));
                                skeletronHandRule.ChainedRules.Clear();
                                skeletronHandRule.ChainedRules.AddRange(withoutBookOfSkulls);
                            }
                        }
                    }
                }
            }
            // EXTENSION WAY
            //
            // Any BookOfSkulls CommonDrop anywhere within the loot will be removed. Doesn't care about if it has a parent or if/how it is chained to its parent. Chains are automatically re-attached
            if (npc.type == NPCID.SkeletronHead && true)
            {
                npcLoot.RemoveWhere<CommonDrop>(bookofSkullsDrop => bookofSkullsDrop.itemId == ItemID.BookofSkulls, reattachChains: true);
            }

            // Example 2
            // 
            // NORMAL WAY
            // 
            // Remove Bone Sword from Skeleton. BoneSword is chained onto a CommonDrop(ancientGoldDrop) chained onto a CommonDrop(ancientIronDrop).
            // BoneSword has SkullDrop chained after it, and we don't want to lose that drop, so we have to re-attach the chains
            //
            // This is another example of doing a simple removal that unfortunately needs a lot of hard-coded nested statements
            // Note that ALL this code is trying to achieve is simply remove BoneSwordDrop from Skeleton NPC (BoneSwordDrop is chained onto AncientGoldHelmet drop),
            // then re-attach BoneSwordDrop's ChainedRules onto AncientGoldHelmetDrop so that the things chained after BoneSword (Skull Drop) are not lost
            if (npc.type == NPCID.Skeleton && false)
            {
                foreach (IItemDropRule rule in npcLoot.Get())
                {
                    if (rule is CommonDrop ancientIronDrop && ancientIronDrop.itemId == ItemID.AncientIronHelmet)
                    {
                        foreach (IItemDropRuleChainAttempt chainFromAncientIron in ancientIronDrop.ChainedRules)
                        {
                            if (chainFromAncientIron.RuleToChain is CommonDrop ancientGoldDrop && ancientGoldDrop.itemId == ItemID.AncientGoldHelmet)
                            {
                                foreach (IItemDropRuleChainAttempt chainFromAncientGold in new List<IItemDropRuleChainAttempt>(ancientGoldDrop.ChainedRules))
                                {
                                    if (chainFromAncientGold.RuleToChain is CommonDrop boneSwordRule && boneSwordRule.itemId == ItemID.BoneSword)
                                    {
                                        ancientGoldDrop.ChainedRules.Remove(chainFromAncientGold);

                                        ancientGoldDrop.ChainedRules.AddRange(boneSwordRule.ChainedRules);

                                        break; // Stop at first
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Example 2
            //
            // EXTENSION WAY
            //
            // Any BoneSword CommonDrop anywhere within the loot will be removed. Doesn't care about if it has a parent or if/how it is chained to its parent. Chains are automatically re-attached
            if (npc.type == NPCID.Skeleton && false)
            {
                npcLoot.RemoveWhere<CommonDrop>(boneSwordDrop => boneSwordDrop.itemId == ItemID.BoneSword, reattachChains: true);
            }
        }
    };


    class NPCLootExtensionTest : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // SIMPLIFYING STUFF FROM EXAMPLEMOD

            // EXAMPLEMOD VERSION
            // Editing an existing drop rule, but for a boss
            // In addition to this code, we also do similar code in Common/GlobalItems/BossBagLoot.cs to edit the boss bag loot. Remember to do both if your edits should affect boss bags as well.
            if (npc.type == NPCID.QueenBee && false)
            {
                foreach (var rule in npcLoot.Get())
                {
                    if (rule is DropBasedOnExpertMode dropBasedOnExpertMode && dropBasedOnExpertMode.ruleForNormalMode is OneFromOptionsNotScaledWithLuckDropRule oneFromOptionsDrop && oneFromOptionsDrop.dropIds.Contains(ItemID.BeeGun))
                    {
                        var original = oneFromOptionsDrop.dropIds.ToList();
                        original.Add(ModContent.ItemType<NorthernStarSword>());
                        oneFromOptionsDrop.dropIds = original.ToArray();
                    }
                }
            }
            // MY VERSION: The ?. are there because it is possible for FindRuleWhere to return null
            if (npc.type == NPCID.QueenBee)
            {
                DropBasedOnExpertMode dboe = npcLoot.FindRuleWhere<DropBasedOnExpertMode>(dboe => dboe.ruleForNormalMode is OneFromOptionsNotScaledWithLuckDropRule ofo && ofo.ContainsOption(ItemID.BeeGun));
                OneFromOptionsNotScaledWithLuckDropRule ofo = dboe?.ruleForNormalMode as OneFromOptionsNotScaledWithLuckDropRule;
                ofo?.AddOption(ModContent.ItemType<NorthernStarSword>());
            }

            if (false)
            {
                // VANILLA BOSS LEADING CONDITION CLUTTER. THIS IS UGLY
                LeadingConditionRule leadingConditionRule = new(new Conditions.NotExpert());
                leadingConditionRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<NorthernStarSword>(), 7));
                leadingConditionRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<RainbowBlade>()));
                leadingConditionRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<GarnsFishingRod>(), 20));

                // This looks much more readable (in my opinion)
                npcLoot.Add(new LeadingConditionRule(new Conditions.NotExpert()).OnConditionsMet(
                    ItemDropRule.Common(ModContent.ItemType<NorthernStarSword>(), 7),
                    ItemDropRule.Common(ModContent.ItemType<RainbowBlade>()),
                    ItemDropRule.Common(ModContent.ItemType<GarnsFishingRod>(), 20)
                ));
            }

            // OTHER RANDOM EXAMPLES

            // Remove bone sword from skeleton
            if (npc.type == NPCID.Skeleton)
            {
                // SPECIFIC EXAMPLE
                // Goal: Remove BoneSword CommonDrop from being chained onto AncientGoldHelmet CommonDrop, which is chained onto AncientIron CommonDrop
                // Constraints:
                //     The BoneSword drop must a CommonDrop chained to its parent via a TryIfFailedRandomRoll chain. Its itemId must be BoneSword
                //     The BoneSword's immediate parent (parent 1) must be a CommonDrop chained to its parent via TryIfFailedRandomRoll, and it must drop AncientGoldHelmet
                //     The BoneSword's second parent (parent 2) (aka parent 1's immediate parent) must be a CommonDrop that drops AncientIronHelmet (no chain constraint!!)
                // Chains will be re-attached so further loot (Skull drop) is not lost
                npcLoot.RemoveWhere<TryIfFailedRandomRoll, CommonDrop>(
                    boneSwordDrop =>
                        boneSwordDrop.itemId == ItemID.BoneSword &&
                        boneSwordDrop.HasParentRuleWhere<TryIfFailedRandomRoll, CommonDrop>(ancientGoldDrop => ancientGoldDrop.itemId == ItemID.AncientGoldHelmet, nthParent: 1) &&
                        boneSwordDrop.HasParentRuleWhere<CommonDrop>(ancientIronDrop => ancientIronDrop.itemId == ItemID.AncientIronHelmet, nthParent: 2),
                    reattachChains: true
                );

                // UNSPECIFIC EXAMPLE (MUCH SIMPLER CODE): Any BoneSword CommonDrop anywhere within the loot will be removed. Doesn't care about if it has a parent or if/how it is chained to its parent
                npcLoot.RemoveWhere<CommonDrop>(boneSwordDrop => boneSwordDrop.itemId == ItemID.BoneSword, reattachChains: true);
            }

            // Skeletron Boss: Remove book of skulls drop rule from being failure chained onto SkeletronHandRule, which is failure chained onto SkeletronMaskRule 
            if (npc.type == NPCID.SkeletronHead)
            {
                // SPECIFIC EXAMPLE (similar to specific example for Skeleton so I'm not going to re-write the constraints)
                npcLoot.RemoveWhere<TryIfFailedRandomRoll, CommonDrop>(
                    bookOfSkullsDrop =>
                        bookOfSkullsDrop.itemId == ItemID.BookofSkulls &&
                        bookOfSkullsDrop.HasParentRuleWhere<TryIfFailedRandomRoll, CommonDrop>(skeletronHandRule => skeletronHandRule.itemId == ItemID.SkeletronHand, nthParent: 1) &&
                        bookOfSkullsDrop.HasParentRuleWhere<CommonDrop>(skeletronMaskRule => skeletronMaskRule.itemId == ItemID.SkeletronMask, nthParent: 2),
                    reattachChains: true
                );

                // UNSPECIFIC EXAMPLE (MUCH SIMPLER CODE): Any BookofSkulls CommonDrop within the loot will be removed. Doesn't care about if it has a parent or if/how it is chained to its parent
                npcLoot.RemoveWhere<CommonDrop>(bookofSkullsDrop => bookofSkullsDrop.itemId == ItemID.BookofSkulls, reattachChains: true);
            }

            // Remove FrostBrand from Ice Mimic options
            // Recursively find a OneFromOptionsDropRule within the loot containining a FrostBrand
            if (npc.type == NPCID.IceMimic)
            {
                OneFromOptionsDropRule iceMimicOptions;

                // SPECIFIC FIND EXAMPLE: Will only find a OneFromOptionsDropRule that is chained onto a ToySled CommonDrop via TryIfFailedRandomRoll chain
                iceMimicOptions = npcLoot.FindRuleWhere<TryIfFailedRandomRoll, OneFromOptionsDropRule>(rule =>
                    rule.ContainsOption(ItemID.Frostbrand) &&
                    rule.HasParentRuleWhere<CommonDrop>(parentRule => parentRule.itemId == ItemID.ToySled, nthParent: 1));

                // UNSPECIFIC FIND EXAMPLE: Any OneFromOptionsDropRule containing a FrostBrand will be found. Doesn't care about if it has a parent or if/how it is chained to its parent
                iceMimicOptions = npcLoot.FindRuleWhere<OneFromOptionsDropRule>(rule => rule.ContainsOption(ItemID.Frostbrand));

                // Don't forget the ?. before accessing stuff after using FindRuleWhere<R>, incase FindRuleWhere returns null! (what if another mod already removed frostbrand?)
                iceMimicOptions?.FilterOptions(itemId => itemId != ItemID.Frostbrand);
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

                // THIS ONE SHOWS JUST HOW SPECIFIC YOU CAN GET. THIS RULE MUST MATCH THE EXACT GEOMETRY OF THE FIRST BRANCH

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
