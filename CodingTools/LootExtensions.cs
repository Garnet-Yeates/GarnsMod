using System;
using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace GarnsMod.CodingTools
{
    internal static class LootExtensions
    {
        /// <summary>
        /// Loops through <paramref name="loot"/>'s Get() List and removes any <see cref="IItemDropRule"/> that matches the given predicate. Regardless
        /// of if it matches the predicate or not, it will continue to loop through all the "children" (rules that are chained onto this rule) and see if they match
        /// the predicate. If any child matches the predicate, it will remove the child from the parent and possibly re-attach the child's ChainedRules onto
        /// the parent if <paramref name="reattachChains"/> is set to true. If it is set to true, it will then repeat this same predicate-checking process on the children 
        /// of the child recursively until there are no children left.
        /// </summary>
        public static bool RecursiveRemoveWhere(this ILoot loot, Predicate<IItemDropRule> predicate, bool reattachChains = false, bool stopAtFirst = true)
        {
            bool removedAny = false;

            foreach (IItemDropRule rootRule in loot.Get())
            {
                if (RecursiveRemoveMain(loot, predicate, rootRule, reattachChains, stopAtFirst))
                {
                    removedAny = true;

                    if (stopAtFirst)
                    {
                        return true;
                    }
                }
            }

            return removedAny;
        }

        /// <summary>
        /// Loops through all the "children" (rules that are chained onto this rule) and see if they match the given predicate. If any child matches the predicate, it
        /// will remove the child from the parent and possibly re-attach the child's ChainedRules onto the parent if <paramref name="reattachChains"/> is set to true. 
        /// If it is set to true, it will then repeat this same predicate-checking process on the children  of the child recursively until there are no children left.
        /// </summary>
        public static bool RecursiveRemoveChildren(this IItemDropRule rootRule, Predicate<IItemDropRule> shouldRemove, bool reattachChains = false, bool stopAtFirst = true)
        {
            return RecursiveRemoveMain(null, shouldRemove, rootRule, reattachChains, stopAtFirst);
        }

        // parentRule is null => implies we are on the first iteration
        // parentRule is not null => implies that chainToChild is not null
        // loot is null => implies that this function was called by the IItemDropRule extension, as opposed to the ILoot extension
        /// <summary>
        /// Loops through <paramref name="currRule"/>'s "children" (rules that are chained onto this rule), or the rules inside <paramref name="loot"/>'s Get() if loot is not null, and checks if they match
        /// the predicate. If any child matches the predicate, it will remove the child from the parent and possibly re-attach the child's ChainedRules onto
        /// the parent if <paramref name="reattachChains"/> is set to true. If it is set to true, it will then repeat this same predicate-checking process on the children 
        /// of the child recursively until there are no children left. 
        /// </summary>
        private static bool RecursiveRemoveMain(ILoot loot, Predicate<IItemDropRule> shouldRemove, IItemDropRule currRule, bool reattachChains = false, bool stopAtFirst = true)
        {
            IItemDropRule parentRule = currRule.ParentRule();

            bool canRemove = !(parentRule is null && loot is null);
            if (canRemove && shouldRemove(currRule))
            {
                if (parentRule is not null)
                {
                    currRule.RemoveFromParent(reattachChains);

                    if (reattachChains && !stopAtFirst)
                    {
                        ContinueRecursion(newParent: parentRule); // Don't return here because this could possibly return false. We must return true no matter what if we found one
                    }

                    return true;
                }
                else // (canRemove == true) && (parentRule == null) implies that loot is not null. Being at this else means loot can't be null
                {
                    loot.Remove(currRule);
                    return true;
                }
            }
            else
            {
                return ContinueRecursion(newParent: currRule);
            }

            // Return true if any were removed. Returns immediately after finding one (preventing extra calls to RecursiveMoveMain) if stopAtFirst is set to true
            bool ContinueRecursion(IItemDropRule newParent)
            {
                bool removedAny = false;

                foreach (IItemDropRuleChainAttempt chainAttempt in new List<IItemDropRuleChainAttempt>(currRule.ChainedRules)) // iterate over shallow clone to stop concurrent modification
                {
                    IItemDropRule child = chainAttempt.RuleToChain;
                    child.RegisterParent(newParent, chainAttempt);

                    if (RecursiveRemoveMain(loot, shouldRemove, child, reattachChains))
                    {
                        removedAny = true;

                        if (stopAtFirst)
                        {
                            return true;
                        }
                    }
                }

                return removedAny;
            }
        }

        /// <summary>Recursively loops through this ILoot instance and looks for the first IItemDropRule of type T that matches the given predicate</summary>
        public static T Find<T>(this ILoot loot, Predicate<T> query) where T : IItemDropRule
        {
            foreach (IItemDropRule rootRule in loot.Get())
            {
                if (RecursiveFindMain(query, rootRule) is T result)
                {
                    return result;
                }
            }

            return default;
        }

        /// <summary>Recursively loops through the children (rules that are chained onto this rule) of this rule and looks for the first IItemDropRule of type T that matches the given predicate</summary>
        public static T FindChild<T>(this IItemDropRule root, Predicate<T> query) where T : IItemDropRule
        {
            return RecursiveFindMain(query, root);
        }

        /// <summary>
        /// Checks if currRule is of type T and matches the given predicate. If it does it will be returned. If not, it will recursively call this method
        /// again on the children of currRule
        /// </summary>
        private static T RecursiveFindMain<T>(Predicate<T> query, IItemDropRule currRule) where T : IItemDropRule
        {
            if (currRule is T castedRule && query(castedRule))
            {
                return castedRule;
            }
            else
            {
                foreach (IItemDropRuleChainAttempt chainAttempt in new List<IItemDropRuleChainAttempt>(currRule.ChainedRules)) // iterate over shallow clone to stop concurrent modification
                {
                    IItemDropRule child = chainAttempt.RuleToChain;
                    child.RegisterParent(currRule, chainAttempt);

                    if (RecursiveFindMain(query, child) is T result)
                    {
                        return result;
                    }
                }

                return default; // return null
            }
        }

        /// <summary>
        /// Used to temporarily store data about which IItemDropRule is the "parent" of another IItemDropRule, as well as storing the ChainAttempt 
        /// An IItemDropRule is considered a "child" to another IItemDropRule if the parent's ChainedRules array contains an IItemDropRuleChainAttempt
        /// where the chainAttempt.RuleToChain references the child.
        /// </summary>
        public struct ParentWithChain
        {
            public IItemDropRule Parent { get; set; }
            public IItemDropRuleChainAttempt ChainAttempt { get; set; }
        }

        /// <summary>
        /// Used to temporarily store data about which IItemDropRule is the "parent" of another IItemDropRule. Keys in this dictionary
        /// represent children, and you can get their parent as well as the chain that leads to the child
        /// <see cref="ParentRule(IItemDropRule)"/>
        /// </summary> 
        private static Dictionary<IItemDropRule, ParentWithChain> ParentDictionary = new();

        private static IItemDropRule RegisterParent(this IItemDropRule rule, IItemDropRule parent, IItemDropRuleChainAttempt chainAttempt)
        {
            ParentDictionary[rule] = new ParentWithChain { Parent = parent, ChainAttempt = chainAttempt };
            return rule;
        }

        /// <summary>
        /// Only should be used in the context of RecursiveRemove calls, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls. Mainly used inside Predicates of RecursiveRemove as a means of being more exact about what
        /// rule we are removing
        /// </summary>
        public static IItemDropRule ParentRule(this IItemDropRule rule)
        {
            return ParentDictionary.TryGetValue(rule, out ParentWithChain parentWithChain) ? parentWithChain.Parent : null;
        }

        /// <summary>
        /// Only should be used in the context of RecursiveRemove calls, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls. Mainly used inside Predicates of RecursiveRemove as a means of being more exact about what
        /// rule we are removing
        /// </summary>
        public static IItemDropRule ParentRule(this IItemDropRule rule, int amount = 1)
        {
            for (int i = 0; i < amount; i++)
            {
                rule = rule?.ParentRule();
            }
            return rule;
        }

        /// <summary>Helper method for RecursiveRemoveMain</summary>
        private static void RemoveFromParent(this IItemDropRule rule, bool reattachChains = false)
        {
            if (ParentDictionary.TryGetValue(rule, out ParentWithChain parentWithChain))
            {
                parentWithChain.Parent.ChainedRules.Remove(parentWithChain.ChainAttempt);

                if (reattachChains)
                {
                    parentWithChain.Parent.ChainedRules.AddRange(rule.ChainedRules);
                }
            }
        }
    }
}