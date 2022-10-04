using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace GarnsMod.Content.RandomStuff
{
    internal static class ItemDropResolveExtensions
    {
        /// <summary>
        /// Can be called on any NetMode but keep in mind that some rules and their respective CommonCode methods are meant to only be called on SP/Server netmodes. <br/><br/>
        /// If resolving rules from client code, make sure to do a whoAmI check when needed to make sure that the rules are only resolved on ONE client. 
        /// If you fail to do this extra items will spawn depending on how many people are connected<br/><br/>
        /// If resolving rules from server code, make sure that the DropAttemptInfo either has a hitbox set, or a (non server aka not whoAmI = 255) player specified. If you want an
        /// explanation on why, read the exception thrown in CustomItemDropResolver.CreateDropAttemptInfo()
        /// </summary>
        public static void ResolveRules(this List<IItemDropRule> rulesToExecute, DropAttemptInfo info)
        {
            ResolveMultipleRules(rulesToExecute, info);
        }

        /// <summary>
        /// Can be called on any NetMode but keep in mind that some rules and their respective CommonCode methods are meant to only be called on SP/Server netmodes. <br/><br/>
        /// If resolving rules from client code, make sure to do a whoAmI check when needed to make sure that the rules are only resolved on ONE client. 
        /// If you fail to do this extra items will spawn depending on how many people are connected<br/><br/>
        /// If resolving rules from server code, make sure that the DropAttemptInfo either has a hitbox set, or a (non server aka not whoAmI = 255) player specified. If you want an
        /// explanation on why, read the exception thrown in CustomItemDropResolver.CreateDropAttemptInfo()
        /// </summary>
        public static void ResolveMultipleRules(List<IItemDropRule> rulesToExecute, DropAttemptInfo info)
        {
            foreach (IItemDropRule rule in rulesToExecute)
            {
                CustomItemDropResolver.ResolveRule(rule, info);
            }
        }
    }

    // This is vanilla code copied verbatim so that we can just call ResolveRule recursive logic on any rules/chains that we create
    internal class CustomItemDropResolver : ModSystem
    {
        #region CommonCode Class Explanation
        /*
         * CommonCode class explanation
         * 
         * CommonCode contains methods for dropping items. These methods can be called on any netmode (although there are a couple that are only meant to be called on
         * SP/Server) and it will spawn the item in using the correct process for whatever netmode this instance if on. My deduction of why it is called CommonCode is
         * because the methods can be called on any netmodes so it is "common" between netmodes
         *          
         * IItemDropRules (used in NPC loot as well as Item loot) all use CommonCode methods for dropping their items. The cool thing about IItemDropRules is that they
         * can be executed on a local client instance OR on singleplayer OR on the server and it will work (although again, there are a few that are only meant to be called
         * on server/sp). Another cool thing about them is that they can be used for both NPC drops (executed on server/sp) and Loot Bag drops (executed on mp client)
         * 
         * Item Loot => SP/MP
         * NPC Loot => SP/Server
         * 
         * The general rule of thumb is, IItemDropRules that are used ONLY for NPCLoot and are NOT used for ItemLoot MUST be called on SP/Server only. Why? This is because
         * NPC.NPCLoot() is only called on server/sp netmodes so the methods that these rules use in CommonCode were intended for server/sp usage only because these rules wouldn't
         * be executed in MP anyways since they don't show up in Item loot.
         * 
         * The examples I've found thus far of IItemDropRules that should only be called on SP/Server (aka IItemDropRules that are only used for NPCLoot and NOT item loot)
         * are:
         * 
         * IItemDropRule => DropLocalPerClientAndResetsNPCMoneyTo0. 
         * CommonCode => CommonCode.DropItemLocalPerClientAndSetNPCMoneyTo0()
         * 
         * If you execute this rule locally on one multiplayer client (i.e Main.myPlayer == Player.whoAmI), it will cause it to drop the item just once, and all players will be able to see it
         * If you execute this globally on all multiplayer clients (which you shouldn't be doing anyways... If you're going to be executing rules from MPClient, only do it on one client),
         * the item will indeed drop on all clients, but clients will be able to see other client's items
         * 
         * IItemDropRule => DropPerPlayerOnThePlayer
         * CommonCode => CommonCode.DropItemForEachInteractingPlayerOnThePlayer()
         * 
         * If you execute this rule locally on one client, it will only drop one item (won't be for each player)
         * If you execute this rule globally on all clients (which again, you should never do if executing from client code), it will actually work properly. But don't do this.
         * 
         * If you intend to execute rules on client code, the reason why you should only be executing rules on the client locally is because the client spawns the item locally and syncs it to the 
         * other players. If you execute rules on client code, but you accidentally execute the rules on ALL client's code as opposed to one client, then it will spawn the item in n times where n 
         * is the number of players on the server. If 8 people are on, then 8x the intended amount of items will spawn if you forget to spawn it LOCALLY
         */
        #endregion

        #region How And When Rules Are Normally Obtained/Resolved

        // This explains how rules are normally obtained and resolved in vanilla in the first place:

        /* For rule obtaining:
         * - ItemDropResolver.TryDropping is called on a DropAttempt info when an NPC dies or a bag is opened.
         * - The DropAttemptInfo either has an NPC set or an item set (depending on if an NPC died or a bag was opened). 
         * - When this method is called, It will look thru the rules database to find out what rules to obtain for the given NPC or item based on the DropAttemptInfo
         * - After obtaining these rules, it will call ResolveRule() on them.
         * 
         * What I really care about is the ResolveRule function, as well as its helper functions. Simply creating a rule and calling rule.TryDroppingItem will not suffice because it won't resolve recursively
         * for ChainedRules as well as INestedItemDropRules. It will also not care about CanDrop (i.e conditional rules). What we really need in order to execute rules recursively is this (normally private)
         * ResolveRule() recursive entry point.
         * 
         * By hooking into this (normally private) vanilla logic with reflection, we can fully-functionally just create drop rules and drop stuff based on these awesome rules/conditions/chains, WITHOUT needing 
         * an actual an npc or item, Read GetDropAttemptInfo as well as the CommonCode explanation for more info on the whole process and what to avoid/look out for
         * 
         * Another awesome thing is that 95% of IItemDropRules are meant to be able to be executed on all netmodes (see CommonCode explanation above), so we can use these functions I made virtually anywehre
         *         
         * Vanilla reference for TryDropping (inside ItemDropResolver):
         * 
         * public void TryDropping(DropAttemptInfo info)
         * {
         *     List<IItemDropRule> rulesForNPCID = ((info.npc != null) ? this._database.GetRulesForNPCID(info.npc.netID) : this._database.GetRulesForItemID(info.item));
         *     for (int i = 0; i < rulesForNPCID.Count; i++)
         *     {
         *         this.ResolveRule(rulesForNPCID[i], info);
         *     }
         * }
         *
         */

        #endregion

        #region What Happens After TryDroppingItem Is Successful For A Given Rule

        // This explains what happens (what methods are called and when) after a rule is resolved successfully and decides it should drop an item. It explains how it uses CommonCode
        // to drop the items, It also explains  how the DropAttemptInfo has an effect on how the item is dropped (i.e it behaves slightly differently if it thinks its dropping from an
        // npc vs dropping from an item)

        /*
         * When CommonCode.DropItem is called via the Rule being successful and dropping the item, it checks if DropAttemptInfo.npc exists. <br/><br/>
         * 
         * If it doesn't, it calls CommonCode.DropItem (different overload than the one I mentioned above) and drops the item with player.GetItemSource_OpenItem 
         *   where opened item is dirt block. In this case the item is dropped on top of the player <br/><br/>
         *   
         * If it does exist, it calls CommonCode._DropItemFromNPC which makes the drop source npc.ItemSource_Loot and the item is dropped within the hitbox of
         *   the NPC. It also calls CommonCode.ModifyDropFromNPC (for changing things like dropped item color if slime is killed) but this won't do anything if NPC is type 0
         *   so we don't have to worry about that <br/><br/>
         */

        #endregion

        /// <summary>
        /// Choosing to input the 'hitbox' param will tell my code to trick CommonCode into thinking that this dropped from an NPC of type 0. This allows us to choose
        /// where the item drops instead of making it drop on the player specified in the DropAttemptInfo. Keep in mind that this means we shouldn't use rule conditions 
        /// that require the npc to have x amount of health or value, etc or it won't work properly. If called on server netmode, either hitbox OR player must be specified or
        /// else it won't know where to drop the item. Usually player being specified and hitbox not being specified means we are dropping the item on the player (like item loot).
        /// Hitbox being specified and player being unspecified is for dropping items from npc (like npc loot). Dropping from NPC takes precedence over dropping on player
        /// (i.e npc loot takes precedence over item loot), so if hitbox and player are both specified it will use hitbox
        /// </summary>
        public static DropAttemptInfo CreateDropAttemptInfo(Rectangle? hitbox = null, Player player = null)
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

            if (Main.netMode == NetmodeID.Server && hitbox is null && player is null)
            {
                throw new Exception("If custom executing IItemDropRule on the Server, you must either specify a hitbox (to trick vanilla into thinking we're dropping from an NPC so player hitbox isn't" +
                    "needed) OR you must specify a specific player, because if this is called on the server the player will default to the 'server' player which doesn't have an accurate hitbox/location " +
                    "to drop the item at");
            }

            return new DropAttemptInfo
            {
                npc = npcDummy,
                item = itemDummy,
                rng = Main.rand,
                IsExpertMode = Main.expertMode,
                IsMasterMode = Main.masterMode,
                IsInSimulation = false,
                player = player ?? Main.LocalPlayer
            };
        }


        public delegate ItemDropAttemptResult RuleResolver(IItemDropRule rule, DropAttemptInfo info);

        /// <summary>
        /// Can be called on any NetMode but keep in mind that some rules and their respective CommonCode methods are meant to only be called on SP/Server netmodes. <br/><br/>
        /// If resolving rules from client code, make sure to do a whoAmI check when needed to make sure that the rules are only resolved on ONE client. 
        /// If you fail to do this extra items will spawn depending on how many people are connected<br/><br/>
        /// If resolving rules from server code, make sure that the DropAttemptInfo either has a hitbox set, or a (non server aka not whoAmI = 255) player specified. If you want an
        /// explanation on why, read the exception thrown in CustomItemDropResolver.CreateDropAttemptInfo()
        /// </summary>
        public static RuleResolver ResolveRule;

        public override void SetStaticDefaults()
        {
            ItemDropResolver resolver = Main.ItemDropSolver;
            ResolveRule = resolver.GetType()
                .GetMethod("ResolveRule", BindingFlags.NonPublic | BindingFlags.Instance)
                .CreateDelegate<RuleResolver>(resolver);
        }
    }
}
