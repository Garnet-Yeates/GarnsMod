using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;

namespace GarnsMod.Content.RandomStuff
{
    // This is vanilla code copied verbatim so that we can just call ResolveRule recursive logic on any rules/chains that we create
    internal class CustomItemDropResolver
    {
        /* This is simply here to show how vanilla does it. Normally TryDropping is called on a DropAttempt info when an NPC dies or a bag is opened.
            * How this works is that the DropAttemptInfo either has an NPC set or an item set (depending on if an NPC died or a bagwas opened). It will look
            * thru the rules database to find out what rules to use for the given NPC or item (npc takes prio),
            * then call ResolveRule() on them. What I really care about is the ResolveRule function as well as its helper functions. Simply creating a rule and 
            * calling rule.TryDroppingItem will not work for chained rules and it will also not care about the CanDrop method (i.e conditional rules).
            * By copying this vanilla logic, we can full-functionally just create drop rules and drop stuff based on these awesome rules/conditions/chains, WITHOUT needing 
            * an actual an npc or item, Read GetDropAttemptInfo for more info on the whole process and what to avoid/look out for
            * 
        public void TryDropping(DropAttemptInfo info)
        {
            List<IItemDropRule> rulesForNPCID = ((info.npc != null) ? this._database.GetRulesForNPCID(info.npc.netID) : this._database.GetRulesForItemID(info.item));
            for (int i = 0; i < rulesForNPCID.Count; i++)
            {
                this.ResolveRule(rulesForNPCID[i], info);
            }
        }*/

        /**<summary>
            * When CommonCode.DropItem is called via the Rule being successful and dropping the item, it checks if DropAttemptInfo.npc exists. <br/><br/>
            * 
            * If it doesn't, it calls CommonCode.DropItem (different overload than the one I mentioned above) and drops the item with player.GetItemSource_OpenItem 
            *   where opened item is dirt block. In this case the item is dropped on top of the player <br/><br/>
            *   
            * If it does exist, it calls CommonCode._DropItemFromNPC which makes the drop source npc.ItemSource_Loot and the item is dropped within the hitbox of
            *   the NPC. It also calls CommonCode.ModifyDropFromNPC (for changing things like dropped item color if slime is killed) but this won't do anything if NPC is type 0
            *   so we don't have to worry about that <br/><br/>
            *   
            * Choosing to input the 'hitbox' param will tell my code to trick CommonCode into thinking that this dropped from an NPC of type 0. This allows us to choose
            * where the item drops instead of making it drop on the player <br/><br/>
            * 
            * Keep in mind that this means we shouldn't use rule conditions that require the npc to have x amount of health or value, etc or it won't work properly. I doubt that
            * would ever be needed anyways. Also keep in mind that when using ResolveRule (below) you are not calling it on MP_Client netmode
            * </summary>
            */
        public static DropAttemptInfo GetDropAttemptInfo(Rectangle? hitbox = null)
        {
            int itemDummy = ItemID.DirtBlock;
            NPC npcDummy = null;
            if (hitbox is Rectangle box)
            {
                npcDummy = new()
                {
                    Hitbox = box,
                };
            }
            return new() { npc = npcDummy, item = itemDummy, rng = Main.rand, IsExpertMode = Main.expertMode, IsMasterMode = Main.masterMode, IsInSimulation = false, player = Main.LocalPlayer };
        }

        // Verbatim vanilla code
        public static ItemDropAttemptResult ResolveRule(IItemDropRule rule, DropAttemptInfo info)
        {
            if (!rule.CanDrop(info))
            {
                ItemDropAttemptResult itemDropAttemptResult = default(ItemDropAttemptResult);
                itemDropAttemptResult.State = ItemDropAttemptResultState.DoesntFillConditions;
                ItemDropAttemptResult itemDropAttemptResult2 = itemDropAttemptResult;
                ResolveRuleChains(rule, info, itemDropAttemptResult2);
                return itemDropAttemptResult2;
            }
            ItemDropAttemptResult itemDropAttemptResult3 = (rule as INestedItemDropRule)?.TryDroppingItem(info, ResolveRule) ?? rule.TryDroppingItem(info);
            ResolveRuleChains(rule, info, itemDropAttemptResult3);
            return itemDropAttemptResult3;
        }

        // Verbatim vanilla code
        private static void ResolveRuleChains(IItemDropRule rule, DropAttemptInfo info, ItemDropAttemptResult parentResult)
        {
            ResolveRuleChains(ref info, ref parentResult, rule.ChainedRules);
        }

        // Verbatim vanilla code
        private static void ResolveRuleChains(ref DropAttemptInfo info, ref ItemDropAttemptResult parentResult, List<IItemDropRuleChainAttempt> ruleChains)
        {
            if (ruleChains == null)
            {
                return;
            }
            for (int i = 0; i < ruleChains.Count; i++)
            {
                IItemDropRuleChainAttempt itemDropRuleChainAttempt = ruleChains[i];
                if (itemDropRuleChainAttempt.CanChainIntoRule(parentResult))
                {
                    ResolveRule(itemDropRuleChainAttempt.RuleToChain, info);
                }
            }
        }
    }
}
