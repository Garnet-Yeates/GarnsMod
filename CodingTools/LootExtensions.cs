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
                if (RecursiveRemove(loot, predicate, rootRule, reattachChains, stopAtFirst))
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
            return RecursiveRemove(null, shouldRemove, rootRule, reattachChains, stopAtFirst);
        }

        /// <summary>
        /// Main entry point into RecursiveRemoveMain. RecursiveRemoveMain should only ever be called by this method or itself. This method marks the dictionary as in use and will clear
        /// it and unmark after (unless some other call earlier on the method call stack was using it first, in which case it will let that method unmark and clear it [this is so that you
        /// can use RecursiveRemoveMain within RecursiveFind predicates without causing issues with the dictionary being modified during the predicates]) Note: can't think of many use cases
        /// for having RecursiveRemove within RecursiveFind predicates, but there are definitely some. It would be more common to use RecursiveFind within RecursiveRemove
        /// </summary>
        public static bool RecursiveRemove(ILoot loot, Predicate<IItemDropRule> shouldRemove, IItemDropRule rootRule, bool reattachChains = false, bool stopAtFirst = true)
        {
            bool wasInuse = DictionaryInUse;
            DictionaryInUse = true;
            bool result = RecursiveRemoveMain(loot, shouldRemove, rootRule, reattachChains, stopAtFirst);
            if (!wasInuse)
            {
                DictionaryInUse = false;
                ParentDictionary.Clear();
            }
            return result;
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


        /// <summary>
        /// Recursively loops through this ILoot instance looking for any rule of type T that matches the given predicate, returning it. If no match is found, it will
        /// recursively loop through the children of that child and try to find one, so on and so forth
        /// </summary>
        public static bool Has<T>(this ILoot loot, Predicate<T> query) where T : IItemDropRule
        {
            return loot.Find(query) is not null;
        }

        /// <summary>
        /// Recursively loops through this ILoot instance and looks for the first IItemDropRule of type T that matches the given predicate. If no match is found, it will
        /// recursively loop through the children of that child and try to find one, so on and so forth
        /// </summary>
        public static T Find<T>(this ILoot loot, Predicate<T> query) where T : IItemDropRule
        {
            foreach (IItemDropRule rootRule in loot.Get())
            {
                if (RecursiveFind(rootRule, query) is T result)
                {
                    return result;
                }
            }

            return default;
        }

        /// <summary>
        /// Recursively loops through the children (IItemDropRules that are chained onto this rule) of this rule and sees if any are of type T that matches the given predicate. If no match is
        /// found, it will recursively loop through the children of that child and try to find one, so on and so forth
        /// </summary>
        public static bool HasChild<T>(this IItemDropRule root, Predicate<T> query) where T : IItemDropRule
        {
            return root.FindChild(query) is not null;
        }

        /// <summary>
        /// Recursively loops through the children (IItemDropRules that are chained onto this rule) of this rule and sees if any are of type T that matches the given predicate. If no match is
        /// found, it will recursively loop through the children of that child and try to find one, so on and so forth
        /// </summary>
        public static bool HasChild<T>(this IItemDropRule root, int nthChild, Predicate<T> query) where T : IItemDropRule
        {
            return root.FindChild(nthChild, query) is not null;
        }

        /// <summary>
        /// Recursively loops through the children (IItemDropRules that are chained onto this rule) of this rule and looks for the first of type T that matches the given predicate. If no match is
        /// found, it will then recursively loop through the children of that child and try to find one, so on and so forth
        /// </summary>
        public static T FindChild<T>(this IItemDropRule root, Predicate<T> query) where T : IItemDropRule
        {
            return RecursiveFind(root, query);
        }

        /// <summary>
        /// Recursively loops through the children (IItemDropRules that are chained onto this rule) of this rule and looks for the first of type T that matches the given predicate. If no match is
        /// found, it will then recursively loop through the children of that child and try to find one, so on and so forth
        /// </summary>
        public static T FindChild<T>(this IItemDropRule root, int nthChild, Predicate<T> query) where T : IItemDropRule
        {
            return RecursiveFind(root, query, nthChild);
        }

        /// <summary>
        /// Main entry point into RecursiveFindMain. RecursiveFindMain should only ever be called by this method or itself. This method marks the dictionary as in use and will clear
        /// it and unmark after (unless some other call earlier on the method call stack was using it first, in which case it will let that method unmark and clear it [this is so that you
        /// can use RecursiveFind within RecursiveRemoveWhere predicates without causing issues with the dictionary being modified during the predicates])
        /// </summary>
        public static T RecursiveFind<T>(IItemDropRule root, Predicate<T> query, int? nthChild = null) where T : IItemDropRule
        {
            bool wasInUse = DictionaryInUse;
            DictionaryInUse = true;
            T result = RecursiveFindMain(query, root, root, nthChild);
            if (!wasInUse)
            {
                DictionaryInUse = false;
                ParentDictionary.Clear();
            }
            return result;
        }

        /// <summary>
        /// Checks if currRule is of type T and matches the given predicate. If it does it will be returned. If not, it will recursively call this method
        /// again on the children of currRule
        /// </summary>
        private static T RecursiveFindMain<T>(Predicate<T> query, IItemDropRule rootRule, IItemDropRule currRule, int? nthChild = null) where T : IItemDropRule
        {
            // Could not have rootRule param and ParentRule(n) check and instead we could have n param that starts at 0 and goes up on each recursion for efficiency
            if (currRule != rootRule && currRule is T castedRule && query(castedRule) && (nthChild is not int n || castedRule.ParentRule(n) == rootRule))
            {
                return castedRule;
            }
            else
            {
                foreach (IItemDropRuleChainAttempt chainAttempt in new List<IItemDropRuleChainAttempt>(currRule.ChainedRules)) // iterate over shallow clone to stop concurrent modification
                {
                    IItemDropRule child = chainAttempt.RuleToChain;
                    child.RegisterParent(currRule, chainAttempt);

                    if (RecursiveFindMain(query, rootRule, currRule: child, nthChild) is T result)
                    {
                        return result;
                    }
                }

                return default; // return null
            }
        }

        public static void Clear(this ILoot loot)
        {
            loot.RemoveWhere(_ => true);
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

        private static bool DictionaryInUse = false;

        private static IItemDropRule RegisterParent(this IItemDropRule rule, IItemDropRule parent, IItemDropRuleChainAttempt chainAttempt)
        {
            ParentDictionary[rule] = new ParentWithChain { Parent = parent, ChainAttempt = chainAttempt };
            return rule;
        }

        /// <summary>
        /// Only should be used in the context of RecursiveRemove calls, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of RecursiveRemove as a means of being more exact about what
        /// rule we are removing
        /// </summary>
        public static IItemDropRule ParentRule(this IItemDropRule rule)
        {
            if (!DictionaryInUse)
            {
                throw new Exception("IItemDropRule.ParentRule() can only be used in the context of predicates within RecursiveFind (Find<T>, Has<T>, FindChild<T>, HasChild<T>) or RecursiveRemove (RecursiveRemoveWhere, RecursiveRemoveChildren)");
            }

            return ParentDictionary.TryGetValue(rule, out ParentWithChain parentWithChain) ? parentWithChain.Parent : null;
        }

        /// <summary>
        /// Only should be used in the context of RecursiveRemove calls, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of RecursiveRemove as a means of being more exact about what
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