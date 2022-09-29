using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace GarnsMod.CodingTools
{
    internal static class LootExtensions
    {
        public delegate bool LootPredicate<R>(R rule) where R : IItemDropRule;


        /// <summary>
        /// Loops through <paramref name="loot"/>'s Get() List and removes any <see cref="IItemDropRule"/> that matches the given predicate. Regardless
        /// of if it matches the predicate or not, it will continue to loop through all the "children" (rules that are chained onto this rule) and see if they match
        /// the predicate. If any child matches the predicate, it will remove the child from the parent and possibly re-attach the child's ChainedRules onto
        /// the parent if <paramref name="reattachChains"/> is set to true. If it is set to true, it will then repeat this same predicate-checking process on the children 
        /// of the child recursively until there are no children left.
        /// </summary>
        public static bool RemoveWhere<R>(this ILoot loot, LootPredicate<R> predicate, bool reattachChains = false, bool stopAtFirst = true) where R : IItemDropRule
        {
            bool removedAny = false;

            foreach (IItemDropRule rootRule in loot.Get())
            {
                if (RecursiveRemoveEntry(loot, predicate, rootRule, reattachChains, stopAtFirst))
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
        public static bool RemoveChildren<R>(this IItemDropRule rootRule, LootPredicate<R> predicate, bool reattachChains = false, bool stopAtFirst = true) where R : IItemDropRule
        {
            return RecursiveRemoveEntry(null, predicate, rootRule, reattachChains, stopAtFirst);
        }

        /// <summary>
        /// Main entry point into RecursiveRemoveMain. RecursiveRemoveMain should only ever be called by this method or itself. This method marks the dictionary as in use and will clear
        /// it and unmark after (unless some other call earlier on the method call stack was using it first, in which case it will let that method unmark and clear it [this is so that you
        /// can use RecursiveRemoveMain within RecursiveFind predicates without causing issues with the dictionary being modified during the predicates]) Note: can't think of many use cases
        /// for having RecursiveRemove within RecursiveFind predicates, but there are definitely some. It would be more common to use RecursiveFind within RecursiveRemove
        /// </summary>
        public static bool RecursiveRemoveEntry<R>(ILoot loot, LootPredicate<R> predicate, IItemDropRule rootRule, bool reattachChains = false, bool stopAtFirst = true) where R : IItemDropRule
        {
            bool wasInuse = DictionaryInUse;
            DictionaryInUse = true;
            bool result = RecursiveRemoveMain(loot, predicate, null, rootRule, reattachChains, stopAtFirst);
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
        private static bool RecursiveRemoveMain<T>(ILoot loot, LootPredicate<T> shouldRemove, IItemDropRuleChainAttempt chainToCurrRule, IItemDropRule currRule, bool reattachChains = false, bool stopAtFirst = true) where T : IItemDropRule
        {
            IItemDropRule parentRule = currRule.ParentRule();

            bool canRemove = !(parentRule is null && loot is null); // If main entry called on IItemDropRule, this will be false on the first iteration to prevent it from being removed (because we only want to remove its children)

            if (canRemove && currRule is T castedRule && shouldRemove(castedRule))
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
                    child.SetParent(newParent, chainAttempt);

                    if (RecursiveRemoveMain(loot, shouldRemove, chainAttempt, child, reattachChains, stopAtFirst))
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
        /// Recursively loops through this ILoot instance and sees if there is any IItemDropRule of type T that matches the given predicate. If a match is found
        /// it will return true. If no match is found, it will recursively loop through the children of that child and try to find one, so on and so forth
        /// If nothing was found after searching all children recursively, it will return false
        /// </summary>
        public static bool Has<T>(this ILoot loot, LootPredicate<T> query) where T : IItemDropRule
        {
            return loot.Find(query) is not null;
        }

        /// <summary>
        /// Recursively loops through this ILoot instance and looks for the first IItemDropRule of type T that matches the given predicate. If a match is found
        /// it will return it. If no match is found, it will recursively loop through the children of that child and try to find one, so on and so forth
        /// If nothing was found after searching all children recursively, it will return null
        /// </summary>
        public static T Find<T>(this ILoot loot, LootPredicate<T> query) where T : IItemDropRule
        {
            foreach (IItemDropRule rootRule in loot.Get())
                if (RecursiveFindEntry(rootRule, query, 1, null) is T result)
                    return result;
            return default;
        }


        /// <summary>
        /// Recursively loops through the children (IItemDropRules that are chained onto this rule) of this rule and sees if any are of type T that matches the given predicate. 
        /// If a match is found it will return true. If no match is found, it will recursively loop through the children of that child and try to find one, so on and so forth
        /// If nothing was found after searching all children recursively, it will return false.
        /// </summary>
        public static bool HasChild<T>(this IItemDropRule root, LootPredicate<T> query) where T : IItemDropRule
        {
            return root.FindChild(query) is not null;
        }

        /// <summary>
        /// Recursively loops through the children (IItemDropRules that are chained onto this rule) of this rule and looks for the first of type T that matches the given predicate. If a match is found
        /// it will return true. If no match is found, it will then recursively loop through the children of that child and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return false.
        /// </summary>
        public static T FindChild<T>(this IItemDropRule root, LootPredicate<T> query) where T : IItemDropRule
        {
            return RecursiveFindEntry(root, query, 0, null);
        }


        /// <summary>
        /// Recursively loops through this ILoot instance and sees if there is any IItemDropRule of type T that matches the given predicate and is also the nth child
        /// of this ILoot. If a match is found it will return true. If no match is found, it will recursively loop through the children of that child and try to find one,
        /// so on and so forth. If nothing was found after searching all children recursively, it will return false. It will terminate and return false before continuing
        /// onto the next recursive call if it realizes that the next n > nthChild since current n increases with each recursive call
        /// </summary>
        public static bool HasNthChild<T>(this ILoot loot, int nthChild, LootPredicate<T> query) where T : IItemDropRule
        {
            return loot.FindNthChild(nthChild, query) is not null;
        }

        /// <summary>
        /// Recursively loops through this ILoot instance and looks for the first IItemDropRule of type T that matches the given predicate and is also the nth child
        /// of this ILoot. If a match is found it will return it. If no match is found, it will recursively loop through the children of that child and try to find one,
        /// so on and so forth. If nothing was found after searching all children recursively, it will return null. It will terminate and return null before continuing
        /// onto the next recursive call if it realizes that the next n > nthChild since current n increases with each recursive call
        /// </summary>
        public static T FindNthChild<T>(this ILoot loot, int nthChild, LootPredicate<T> query) where T : IItemDropRule
        {
            foreach (IItemDropRule rootRule in loot.Get())
                if (RecursiveFindEntry(rootRule, query, 1, nthChild) is T result)
                    return result;
            return default;
        }

        /// <summary>
        /// Recursively loops through the children (IItemDropRules that are chained onto this rule) of this rule and sees if any are of type T that matches the given predicate. 
        /// It also must be the nth child of root IItemDropRule. If a match is found it will return true. If no match is found, it will recursively loop through the children 
        /// of that child and try to find one, so on and so forth If nothing was found after searching all children recursively, it will return false. It will terminate and
        /// return false before continuing onto the next recursive call if it realizes that the next n > nthChild, since current n increases with each recursive call
        /// </summary>
        public static bool HasNthChild<T>(this IItemDropRule root, int nthChild, LootPredicate<T> query) where T : IItemDropRule
        {
            return root.FindNthChild(nthChild, query) is not null;
        }

        /// <summary>
        /// Recursively loops through the children (IItemDropRules that are chained onto this rule) of this rule and looks for the first of type T that matches the given predicate. 
        /// It also must be the nth child of root IItemDropRule. If a match is found it is returned. If no match is found, it will then recursively loop through the children of that 
        /// child and try to find one, so on and so forth. If nothing was found after searching all children recursively, it will return null. It will terminate and return null before continuing
        /// onto the next recursive call if it realizes that the next n > nthChild, since current n increases with each recursive call
        /// </summary>
        public static T FindNthChild<T>(this IItemDropRule root, int nthChild, LootPredicate<T> query) where T : IItemDropRule
        {
            return RecursiveFindEntry(root, query, 0, nthChild);
        }

        /// <summary>
        /// Main entry point into RecursiveFindMain. RecursiveFindMain should only ever be called by this method or itself. This method marks the dictionary as in use and will clear
        /// it and unmark after (unless some other call earlier on the method call stack was using it first, in which case it will let that method unmark and clear it [this is so that you
        /// can use RecursiveFind within RecursiveRemoveWhere predicates without causing issues with the dictionary being modified during the predicates])
        /// </summary>
        public static T RecursiveFindEntry<T>(IItemDropRule root, LootPredicate<T> query, int n, int? nthChild = null) where T : IItemDropRule
        {
            bool wasInUse = DictionaryInUse;
            DictionaryInUse = true;
            T result = RecursiveFindMain(query, null, root, n, nthChild);
            if (!wasInUse)
            {
                DictionaryInUse = false;
                ParentDictionary.Clear();
            }
            return result;
        }

        /// <summary>
        /// Checks if currRule is of type T and matches the given predicate. If it does it will be returned. If not, it will recursively call this method
        /// again on the children of currRule. If nthChild is specified, then the currRule must also be the nth child of whatever ILoot or IItemDropRule that this
        /// recursive call originated with.
        /// </summary>
        private static T RecursiveFindMain<T>(LootPredicate<T> query, IItemDropRuleChainAttempt chainToCurrRule, IItemDropRule currRule, int n, int? nthChild) where T : IItemDropRule
        {
            // n == 0 means this RecursiveFindMain call should not check currRule at all. This is used when FindChildren is first called on an IItemDropRule so that the rule
            // itself isn't queried and returned if it happens to match the predicate (because we only want to query its children). n will be initially set to 1 when called on ILoot entry, 0 when called on IItemDropRule entry
            
            // n must not be 0, currRule must be of type T and match the predicate, and nthChild must not be specified (or if it is specified, n must be == nthChild)
            if (n != 0 && currRule is T castedRule && query(castedRule) && (nthChild is not int nth || nth == n))
            {
                return castedRule;
            }
            else
            {
                int nextN = n + 1;
                if (nthChild is int maxN && nextN > maxN) // Stop trying to search if they specified that it must be nth child (maxN) and we are gonna be further than maxN next iteration
                {
                    return default;
                }

                foreach (IItemDropRuleChainAttempt chainAttempt in new List<IItemDropRuleChainAttempt>(currRule.ChainedRules)) // iterate over shallow clone to stop concurrent modification
                {
                    IItemDropRule child = chainAttempt.RuleToChain;
                    child.SetParent(currRule, chainAttempt);

                    if (RecursiveFindMain(query, chainToCurrRule, currRule: child, nextN, nthChild) is T result)
                    {
                        return result;
                    }
                }

                return default; // return null
            }
        }






        // EVERYTHING DOWN HERE IS AN OVERLOAD OF THE STUFF ABOVE WHERE YOU CAN ALSO CHECK FOR A SPECIFIC CHAIN TYPE AS WELL AS SPECIFIC RULE TYPE. BASICALLY JUST SYNTAX SUGAR





        delegate bool LootPredicate<C, R>(R rule) where C : IItemDropRuleChainAttempt where R : IItemDropRule;

        /// <summary>
        /// Loops through <paramref name="loot"/>'s Get() List and removes any <see cref="IItemDropRule"/> that matches the given predicate. Regardless
        /// of if it matches the predicate or not, it will continue to loop through all the "children" (rules that are chained onto this rule) and see if they match
        /// the predicate. If any child matches the predicate, it will remove the child from the parent and possibly re-attach the child's ChainedRules onto
        /// the parent if <paramref name="reattachChains"/> is set to true. If it is set to true, it will then repeat this same predicate-checking process on the children 
        /// of the child recursively until there are no children left.
        /// </summary>
        public static bool RemoveWhere<C, R>(this ILoot loot, LootPredicate<R> predicate, bool reattachChains = false, bool stopAtFirst = true) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            bool removedAny = false;

            foreach (IItemDropRule rootRule in loot.Get())
            {
                if (RecursiveRemoveEntry<C, R>(loot, predicate, rootRule, reattachChains, stopAtFirst))
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
        public static bool RemoveChildren<C, R>(this IItemDropRule rootRule, LootPredicate<R> predicate, bool reattachChains = false, bool stopAtFirst = true) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            return RecursiveRemoveEntry(null, predicate, rootRule, reattachChains, stopAtFirst);
        }

        /// <summary>
        /// Main entry point into RecursiveRemoveMain. RecursiveRemoveMain should only ever be called by this method or itself. This method marks the dictionary as in use and will clear
        /// it and unmark after (unless some other call earlier on the method call stack was using it first, in which case it will let that method unmark and clear it [this is so that you
        /// can use RecursiveRemoveMain within RecursiveFind predicates without causing issues with the dictionary being modified during the predicates]) Note: can't think of many use cases
        /// for having RecursiveRemove within RecursiveFind predicates, but there are definitely some. It would be more common to use RecursiveFind within RecursiveRemove
        /// </summary>
        public static bool RecursiveRemoveEntry<C, R>(ILoot loot, LootPredicate<R> predicate, IItemDropRule rootRule, bool reattachChains = false, bool stopAtFirst = true) where C : IItemDropRuleChainAttempt where R : IItemDropRule
{
            bool wasInuse = DictionaryInUse;
            DictionaryInUse = true;
            bool result = RecursiveRemoveMain<C, R>(loot, predicate, rootRule, reattachChains, stopAtFirst);
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
        private static bool RecursiveRemoveMain<C, R>(ILoot loot, LootPredicate<R> shouldRemove, IItemDropRule currRule, bool reattachChains = false, bool stopAtFirst = true) where C : IItemDropRuleChainAttempt where R : IItemDropRule
{
            IItemDropRule parentRule = currRule.ParentRule();

            bool canRemove = !(parentRule is null && loot is null); // If main entry called on IItemDropRule, this will be false on the first iteration to prevent it from being removed (because we only want to remove its children)

            if (canRemove && currRule.ChainFromParent() is C && currRule is R castedRule && shouldRemove(castedRule))
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
                    child.SetParent(newParent, chainAttempt);

                    if (RecursiveRemoveMain<C, R>(loot, shouldRemove, child, reattachChains, stopAtFirst))
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
        /// Recursively loops through this ILoot instance and sees if there is any IItemDropRule of type T that matches the given predicate. If a match is found
        /// it will return true. If no match is found, it will recursively loop through the children of that child and try to find one, so on and so forth
        /// If nothing was found after searching all children recursively, it will return false
        /// </summary>
        public static bool Has<C, R>(this ILoot loot, LootPredicate<R> query) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            return loot.Find<C, R>(query) is not null;
        }

        /// <summary>
        /// Recursively loops through this ILoot instance and looks for the first IItemDropRule of type T that matches the given predicate. If a match is found
        /// it will return it. If no match is found, it will recursively loop through the children of that child and try to find one, so on and so forth
        /// If nothing was found after searching all children recursively, it will return null
        /// </summary>
        public static R Find<C, R>(this ILoot loot, LootPredicate<R> query) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            foreach (IItemDropRule rootRule in loot.Get())
                if (RecursiveFindEntry<C, R>(rootRule, query, 1, null) is R result)
                    return result;
            return default;
        }


        /// <summary>
        /// Recursively loops through the children (IItemDropRules that are chained onto this rule) of this rule and sees if any are of type T that matches the given predicate. 
        /// If a match is found it will return true. If no match is found, it will recursively loop through the children of that child and try to find one, so on and so forth
        /// If nothing was found after searching all children recursively, it will return false.
        /// </summary>
        public static bool HasChild<C, R>(this IItemDropRule root, LootPredicate<R> query) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            return root.FindChild<C, R>(query) is not null;
        }

        /// <summary>
        /// Recursively loops through the children (IItemDropRules that are chained onto this rule) of this rule and looks for the first of type T that matches the given predicate. If a match is found
        /// it will return true. If no match is found, it will then recursively loop through the children of that child and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return false.
        /// </summary>
        public static R FindChild<C, R>(this IItemDropRule root, LootPredicate<R> query) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            return RecursiveFindEntry<C, R>(root, query, 0, null);
        }


        /// <summary>
        /// Recursively loops through this ILoot instance and sees if there is any IItemDropRule of type T that matches the given predicate and is also the nth child
        /// of this ILoot. If a match is found it will return true. If no match is found, it will recursively loop through the children of that child and try to find one,
        /// so on and so forth. If nothing was found after searching all children recursively, it will return false. It will terminate and return false before continuing
        /// onto the next recursive call if it realizes that the next n > nthChild since current n increases with each recursive call
        /// </summary>
        public static bool HasNthChild<C, R>(this ILoot loot, int nthChild, LootPredicate<R> query) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            return loot.FindNthChild<C, R>(nthChild, query) is not null;
        }

        /// <summary>
        /// Recursively loops through this ILoot instance and looks for the first IItemDropRule of type T that matches the given predicate and is also the nth child
        /// of this ILoot. If a match is found it will return it. If no match is found, it will recursively loop through the children of that child and try to find one,
        /// so on and so forth. If nothing was found after searching all children recursively, it will return null. It will terminate and return null before continuing
        /// onto the next recursive call if it realizes that the next n > nthChild since current n increases with each recursive call
        /// </summary>
        public static R FindNthChild<C, R>(this ILoot loot, int nthChild, LootPredicate<R> query) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            foreach (IItemDropRule rootRule in loot.Get())
                if (RecursiveFindEntry<C, R>(rootRule, query, 1, nthChild) is R result)
                    return result;
            return default;
        }

        /// <summary>
        /// Recursively loops through the children (IItemDropRules that are chained onto this rule) of this rule and sees if any are of type T that matches the given predicate. 
        /// It also must be the nth child of root IItemDropRule. If a match is found it will return true. If no match is found, it will recursively loop through the children 
        /// of that child and try to find one, so on and so forth If nothing was found after searching all children recursively, it will return false. It will terminate and
        /// return false before continuing onto the next recursive call if it realizes that the next n > nthChild, since current n increases with each recursive call
        /// </summary>
        public static bool HasNthChild<C, R>(this IItemDropRule root, int nthChild, LootPredicate<R> query) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            return root.FindNthChild<C, R>(nthChild, query) is not null;
        }

        /// <summary>
        /// Recursively loops through the children (IItemDropRules that are chained onto this rule) of this rule and looks for the first of type T that matches the given predicate. 
        /// It also must be the nth child of root IItemDropRule. If a match is found it is returned. If no match is found, it will then recursively loop through the children of that 
        /// child and try to find one, so on and so forth. If nothing was found after searching all children recursively, it will return null. It will terminate and return null before continuing
        /// onto the next recursive call if it realizes that the next n > nthChild, since current n increases with each recursive call
        /// </summary>
        public static R FindNthChild<C, R>(this IItemDropRule root, int nthChild, LootPredicate<R> query) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            return RecursiveFindEntry<C, R>(root, query, 0, nthChild);
        }

        /// <summary>
        /// Main entry point into RecursiveFindMain. RecursiveFindMain should only ever be called by this method or itself. This method marks the dictionary as in use and will clear
        /// it and unmark after (unless some other call earlier on the method call stack was using it first, in which case it will let that method unmark and clear it [this is so that you
        /// can use RecursiveFind within RecursiveRemoveWhere predicates without causing issues with the dictionary being modified during the predicates])
        /// </summary>
        public static R RecursiveFindEntry<C, R>(IItemDropRule root, LootPredicate<R> query, int n, int? nthChild = null) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            bool wasInUse = DictionaryInUse;
            DictionaryInUse = true;
            R result = RecursiveFindMain<C, R>(query, null, root, n, nthChild);
            if (!wasInUse)
            {
                DictionaryInUse = false;
                ParentDictionary.Clear();
            }
            return result;
        }

        /// <summary>
        /// Checks if currRule is of type T and matches the given predicate. If it does it will be returned. If not, it will recursively call this method
        /// again on the children of currRule. If nthChild is specified, then the currRule must also be the nth child of whatever ILoot or IItemDropRule that this
        /// recursive call originated with.
        /// </summary>
        private static R RecursiveFindMain<C, R>(LootPredicate<R> query, IItemDropRuleChainAttempt chainToCurrRule, IItemDropRule currRule, int n, int? nthChild) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            // n == 0 means this RecursiveFindMain call should not check currRule at all. This is used when FindChildren is first called on an IItemDropRule so that the rule
            // itself isn't queried and returned if it happens to match the predicate (because we only want to query its children). n will be initially set to 1 when called on ILoot entry, 0 when called on IItemDropRule entry

            // n must not be 0, currRule must be of type T and match the predicate, and nthChild must not be specified (or if it is specified, n must be == nthChild)
            if (n != 0 && currRule.ChainFromParent() is C && currRule is R castedRule && query(castedRule) && (nthChild is not int nth || nth == n))
            {
                return castedRule;
            }
            else
            {
                int nextN = n + 1;
                if (nthChild is int maxN && nextN > maxN) // Stop trying to search if they specified that it must be nth child (maxN) and we are gonna be further than maxN next iteration
                {
                    return default;
                }

                foreach (IItemDropRuleChainAttempt chainAttempt in new List<IItemDropRuleChainAttempt>(currRule.ChainedRules)) // iterate over shallow clone to stop concurrent modification
                {
                    IItemDropRule child = chainAttempt.RuleToChain;
                    child.SetParent(currRule, chainAttempt);

                    if (RecursiveFindMain(query, chainToCurrRule, currRule: child, nextN, nthChild) is R result)
                    {
                        return result;
                    }
                }

                return default; // return null
            }
        }


        // OTHER EXTENSIONS


        /// <summary>Syntax sugar to clear this ILoot. What it does is calls RemoveWhere(_ => true) </summary>
        public static void Clear(this ILoot loot)
        {
            loot.RemoveWhere(_ => true);
        }



        // DATA STRUCTURES USED WITHIN RECURSIVE CALLS FOR PARENT TRACKING

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
        /// represent children, and you can get their parent as well as the chain that leads to the child. This dictionary is built up
        /// on-the-fly during RecursiveRemoveMain and RecursiveFindMain, and is cleared after recursive methods are done using it, so it will only be
        /// accurate during recursion. This means that IItemDropRule.ParentRule(n) extension will only be valid during recursion and should only be used in
        /// predicates plugged into my recursive functions <br/>
        /// See <see cref="ParentRule(IItemDropRule, int)"/>, as well as <see cref="DictionaryInUse"/>
        /// </summary> 
        private readonly static Dictionary<IItemDropRule, ParentWithChain> ParentDictionary = new();

        /// <summary>
        /// Used to control whether or not the dictionary should be cleared (if a recursive function earlier on the stack marked it as used first,
        /// then later calls won't be responsible for un marking it and clearing it and it will be left up to the earlier function). See
        /// <see cref="RecursiveFind{T}(IItemDropRule, LootPredicate{R}, int, int?)"/> and 
        /// <see cref="RecursiveRemove(ILoot, LootPredicate{IItemDropRule}, IItemDropRule, bool, bool)"/>
        /// to see this behavior. It is also used in 
        /// <see cref="ParentRule(IItemDropRule, int)"/> to make sure it is being called in the correct context (Dictionary MUST bee in use by a 
        /// recursive method to be able to use ParentRule(n). This basically means that ParentRule(n) can only be used within Predicates of my extension methods)
        /// </summary>
        private static bool DictionaryInUse = false;

        private static IItemDropRule SetParent(this IItemDropRule rule, IItemDropRule parent, IItemDropRuleChainAttempt chainAttempt)
        {
            ParentDictionary[rule] = new ParentWithChain { Parent = parent, ChainAttempt = chainAttempt };
            return rule;
        }

        public static IItemDropRuleChainAttempt ChainFromParent(this IItemDropRule rule)
        {
            return ParentDictionary.TryGetValue(rule, out ParentWithChain parentWithChain) ? parentWithChain.ChainAttempt : null;
        }

        /// <summary>
        /// Only should be used in the context of Remove/Find predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying
        /// </summary>
        private static IItemDropRule ParentRule(this IItemDropRule rule)
        {
            if (!DictionaryInUse)
                throw new Exception("IItemDropRule.ParentRule() can only be used in the context of predicates within RecursiveFind (Find<T>, Has<T>, FindChild<T>, HasChild<T>) or RecursiveRemove (RecursiveRemoveWhere, RecursiveRemoveChildren)");

            return ParentDictionary.TryGetValue(rule, out ParentWithChain parentWithChain) ? parentWithChain.Parent : null;
        }

        /// <summary>
        /// Returns the nth parent of this IItemDropRule. n being 1 means the direct parent of this rule, n being 2 means the parent of ParentRule(1), etc
        /// Only should be used in the context of Remove calls, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove as a means of being more exact about what
        /// rule we are removing
        /// </summary>
        public static IItemDropRule ParentRule(this IItemDropRule rule, int nthParent)
        {
            if (nthParent == 0)
                throw new Exception("nth parent must be greater than 1");

            for (int i = 0; i < nthParent; i++)
                rule = rule?.ParentRule();

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