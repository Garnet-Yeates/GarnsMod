﻿using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;
using static Terraria.GameContent.ItemDropRules.Chains;

namespace GarnsMod.CodingTools
{
    internal static class LootExtensions
    {
        /// <summary>
        /// When reattachChains is set to true in any of the recursive removal methods, and a rule <paramref name="ruleToChain"/> is removed, it will reattach the child chains of the 
        /// currRule onto the parent or currRule. This is so currRule's children are not also lost. By default it will use whatever the ChainAttempt is between
        /// currRule => child. Supplying a ChainReattacher function will allow you to create a chain yourself between the parent of currRule and the child of
        /// currRule. The <paramref name="ruleToChain"/> parameter of this delegate is a reference to one of currRule's children (this function will be called for
        /// each of currRule's children)
        /// </summary>
        public delegate IItemDropRuleChainAttempt ChainReattcher(IItemDropRule ruleToChain);

        /// <summary>
        /// A <see cref="LootPredicate{R}"/> is the same as a regular predicate but the type param R must be an <see cref="IItemDropRule"/>.
        /// LootPredicates are used to find a specific rule that matches the supplied set of conditions.<br/>
        /// <br/>
        /// These predicates can be used inside any of the top-level recursive extension methods:<br/><br/>
        /// <see cref="RemoveWhere{R}(ILoot, LootPredicate{R}, int?, bool, ChainReattcher, bool)"/><br/>
        /// <see cref="RemoveChildrenWhere{R}(IItemDropRule, LootPredicate{R}, int?, bool, ChainReattcher, bool)"/><br/>
        /// <see cref="RemoveWhere{C, R}(ILoot, LootPredicate{R}, int?, bool, ChainReattcher bool)"/><br/>
        /// <see cref="RemoveChildrenWhere{C, R}(IItemDropRule, LootPredicate{R}, int?, bool, ChainReattcher bool)"/><br/>
        /// <br/>
        /// <see cref="HasRuleWhere{R}(ILoot, LootPredicate{R}, int?)"/><br/>
        /// <see cref="FindRuleWhere{R}(ILoot, LootPredicate{R}, int?)"/><br/>
        /// <see cref="HasChildWhere{R}(IItemDropRule, LootPredicate{R}, int?)"/><br/>
        /// <see cref="FindChildWhere{R}(IItemDropRule, LootPredicate{R}, int?)"/><br/>
        /// <see cref="HasRuleWhere{C, R}(ILoot, LootPredicate{R}, int?)"/><br/>
        /// <see cref="FindRuleWhere{C, R}(ILoot, LootPredicate{R}, int?)"/><br/>
        /// <see cref="HasChildWhere{C, R}(IItemDropRule, LootPredicate{R}, int?)"/><br/>
        /// <see cref="FindChildWhere{C, R}(IItemDropRule, LootPredicate{R}, int?)"/><br/>
        /// <br/>
        /// There are also special helper extension methods for <see cref="IItemDropRule"/>s that can <i>only</i> be used within these predicates:<br/><br/>
        /// <see cref="ParentRule(IItemDropRule, int)"/><br/>
        /// <see cref="ImmediateParent(IItemDropRule)"/><br/>
        /// <see cref="ChainFromImmediateParent(IItemDropRule)"/><br/>
        /// <see cref="HasParentRuleWhere{R}(IItemDropRule, LootPredicate{R}, int?)"/><br/>
        /// <see cref="FindParentRuleWhere{R}(IItemDropRule, LootPredicate{R}, int?)"/><br/>
        /// <see cref="HasParentRuleWhere{C, R}(IItemDropRule, LootPredicate{R}, int?)"/><br/>
        /// <see cref="FindParentRuleWhere{C, R}(IItemDropRule, LootPredicate{R}, int?)"/><br/>
        /// </summary>
        public delegate bool LootPredicate<R>(R rule) where R : IItemDropRule;

        #region Recursive Removing

        // RECURSIVE REMOVING

        /// <summary>
        /// Performs the following processes/checks on each <see cref="IItemDropRule"/> "<paramref name="currRule"/>" within <paramref name="loot"/>.Get()  <br/><br/>
        ///
        /// <code>if (<paramref name="currRule"/> is of type <typeparamref name="R"/>, and <paramref name="currRule"/> matches the predicate)</code>
        /// Then it will remove currRule from this loot pool, and possibly re-attach currRule's children onto currRule's parent if <paramref name="reattachChains"/> is set to true.
        /// If <paramref name="reattachChains"/> is true but the rule has no parent (i.e it is directly inside this ILoot, as opposed to being the child of a rule directly inside this ILoot), the chains can't be reattached. If 
        /// <paramref name="reattachChains"/> is set to false it will terminate here because this means the loot chained after this rule is lost, so there is no reason to make modifications to this rule's children. 
        /// If <paramref name="reattachChains"/> is true, and <paramref name="stopAtFirst"/> is set to false, it will then repeat this same predicate-checking process on the children of the child recursively until it
        /// runs out of children to operate on. <br/><br/>
        /// 
        /// If <paramref name="nthChild"/> is supplied, then the above if check will fail unless <paramref name="currRule"/> is the nth descendent of the <see cref="IItemDropRule"/> or <see cref="ILoot"/> that this extension method originated from
        /// 
        /// <code>else</code>Then it will also repeat this process recursively on the children of the child. The rule chained onto currRule will be the new currRule on the next call and n goes up by 1 on the next call<br/><br/>
        ///
        /// Note: A rule (a) is considered the parent to another rule (b) if (b) exists in (a)'s <see cref="IItemDropRule.ChainedRules"/> array
        /// </summary>
        public static bool RemoveWhere<R>(this ILoot loot, LootPredicate<R> predicate, int? nthChild = null, bool reattachChains = false, ChainReattcher chainReattacher = null, bool stopAtFirst = true) where R : IItemDropRule
        {
            bool removedAny = false;

            foreach (IItemDropRule rootRule in loot.Get())
            {
                if (RecursiveRemoveEntry(loot, predicate, rootRule, reattachChains, chainReattacher, stopAtFirst, 1, nthChild))
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
        /// Performs the following processes/checks on each child rule of this <paramref name="rootRule"/><br/><br/>
        ///
        /// <code>if (<paramref name="currRule"/> is of type <typeparamref name="R"/>, and <paramref name="currRule"/> matches the predicate)</code>
        /// Then it will remove currRule from this loot pool, and possibly re-attach currRule's children onto currRule's parent if <paramref name="reattachChains"/> is set to true.
        /// If <paramref name="reattachChains"/> is set to false it will terminate here because this means the loot chained after this rule is lost, so there is no reason to make modifications to this rule's children. 
        /// If <paramref name="reattachChains"/> is true, and <paramref name="stopAtFirst"/> is set to false, it will then repeat this same predicate-checking process on the children of the child recursively until it
        /// runs out of children to operate on. <br/><br/>
        /// 
        /// If <paramref name="nthChild"/> is supplied, then the above if check will fail unless <paramref name="currRule"/> is the nth descendent of the <see cref="IItemDropRule"/> or <see cref="ILoot"/> that this extension method originated from
        /// 
        /// <code>else</code>Then it will also repeat this process recursively on the children of the child. The rule chained onto currRule will be the new currRule on the next call and n goes up by 1 on the next call<br/><br/>
        /// 
        /// Note: A rule (a) is considered the parent to another rule (b) if (b) exists in (a)'s <see cref="IItemDropRule.ChainedRules"/> array
        /// </summary>
        public static bool RemoveChildrenWhere<R>(this IItemDropRule rootRule, LootPredicate<R> predicate, int? nthChild = null, bool reattachChains = false, ChainReattcher chainReattacher = null, bool stopAtFirst = true) where R : IItemDropRule
        {
            return RecursiveRemoveEntry(null, predicate, rootRule, reattachChains, chainReattacher, stopAtFirst, 0, nthChild);
        }

        /// <summary>
        /// Main entry point into RecursiveRemoveMain. RecursiveRemoveMain should only ever be called by this method or itself. This method marks the dictionary as in use and will clear
        /// it and unmark after (unless some other call earlier on the method call stack was using it first, in which case it will let that method unmark and clear it [this is so that you
        /// can use RecursiveRemoveMain within RecursiveFind predicates without causing issues with the dictionary being modified during the predicates]) Note: can't think of many use cases
        /// for having RecursiveRemove within RecursiveFind predicates, but there are definitely some. It would be more common to use RecursiveFind within RecursiveRemove
        /// </summary>
        public static bool RecursiveRemoveEntry<R>(ILoot loot, LootPredicate<R> predicate, IItemDropRule rootRule, bool reattachChains, ChainReattcher chainReattacher, bool stopAtFirst, int n, int? nthChild) where R : IItemDropRule
        {
            bool wasInuse = DictionaryInUse;
            DictionaryInUse = true;
            bool result = RecursiveRemoveMain(loot, predicate, rootRule, reattachChains, chainReattacher, stopAtFirst, n, nthChild);
            if (!wasInuse)
            {
                DictionaryInUse = false;
                ParentDictionary.Clear();
            }
            return result;
        }

        // EVERYTHING DOWN HERE IS AN OVERLOAD OF THE STUFF ABOVE WHERE YOU CAN ALSO CHECK FOR A SPECIFIC CHAIN TYPE AS WELL AS SPECIFIC RULE TYPE. BASICALLY JUST SYNTAX SUGAR

        /// <summary>
        /// Performs the following processes/checks on each <see cref="IItemDropRule"/> "<paramref name="currRule"/>" within <paramref name="loot"/>.Get()  <br/><br/>
        ///
        /// <code>if (<paramref name="currRule"/> was chained onto its parent via an <see cref="IItemDropRuleChainAttempt"/> of type <typeparamref name="C"/>, and <paramref name="currRule"/> is of type <typeparamref name="R"/>, and <paramref name="currRule"/> matches the predicate)</code>
        /// Then it will remove currRule from this loot pool, and possibly re-attach currRule's children onto currRule's parent if <paramref name="reattachChains"/> is set to true.
        /// If <paramref name="reattachChains"/> is true but the rule has no parent (i.e it is directly inside this ILoot, as opposed to being the child of a rule directly inside this ILoot), the chains can't be reattached. If 
        /// <paramref name="reattachChains"/> is set to false it will terminate here because this means the loot chained after this rule is lost, so there is no reason to make modifications to this rule's children. 
        /// If <paramref name="reattachChains"/> is true, and <paramref name="stopAtFirst"/> is set to false, it will then repeat this same predicate-checking process on the children of the child recursively until it
        /// runs out of children to operate on. <br/><br/>
        /// 
        /// If <paramref name="nthChild"/> is supplied, then the above if check will fail unless <paramref name="currRule"/> is the nth descendent of the <see cref="IItemDropRule"/> or <see cref="ILoot"/> that this extension method originated from
        /// 
        /// <code>else</code>Then it will also repeat this process recursively on the children of the child. The rule chained onto currRule will be the new currRule on the next call and n goes up by 1 on the next call<br/><br/>
        /// 
        /// Note: A rule (a) is considered the parent to another rule (b) if (b) exists in (a)'s <see cref="IItemDropRule.ChainedRules"/> array
        /// </summary>
        public static bool RemoveWhere<C, R>(this ILoot loot, LootPredicate<R> predicate, int? nthChild = null, bool reattachChains = false, ChainReattcher chainReattacher = null, bool stopAtFirst = true) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            bool removedAny = false;

            foreach (IItemDropRule rootRule in loot.Get())
            {
                if (RecursiveRemoveEntry<C, R>(loot, predicate, rootRule, reattachChains, chainReattacher, stopAtFirst, 1, nthChild))
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
        /// Performs the following processes/checks on each child rule of this <paramref name="rootRule"/><br/><br/>
        ///
        /// <code>if (<paramref name="rootRule"/> was chained onto its parent via an <see cref="IItemDropRuleChainAttempt"/> of type <typeparamref name="C"/>, and <paramref name="currRule"/> is of type <typeparamref name="R"/>, and <paramref name="currRule"/> matches the predicate)</code>
        /// Then it will remove currRule from this loot pool, and possibly re-attach currRule's children onto currRule's parent if <paramref name="reattachChains"/> is set to true.
        /// If <paramref name="reattachChains"/> is set to false it will terminate here because this means the loot chained after this rule is lost, so there is no reason to make modifications to this rule's children. 
        /// If <paramref name="reattachChains"/> is true, and <paramref name="stopAtFirst"/> is set to false, it will then repeat this same predicate-checking process on the children of the child recursively until it
        /// runs out of children to operate on. <br/><br/>
        /// 
        /// If <paramref name="nthChild"/> is supplied, then the above if check will fail unless <paramref name="currRule"/> is the nth descendent of the <see cref="IItemDropRule"/> or <see cref="ILoot"/> that this extension method originated from
        /// 
        /// <code>else</code>Then it will also repeat this process recursively on the children of the child. The rule chained onto currRule will be the new currRule on the next call and n goes up by 1 on the next call<br/><br/>
        /// 
        /// Note: A rule (a) is considered the parent to another rule (b) if (b) exists in (a)'s <see cref="IItemDropRule.ChainedRules"/> array
        /// </summary>
        public static bool RemoveChildrenWhere<C, R>(this IItemDropRule rootRule, LootPredicate<R> predicate, int? nthChild = null, bool reattachChains = false, ChainReattcher chainReattacher = null, bool stopAtFirst = true) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            return RecursiveRemoveEntry<C, R>(null, predicate, rootRule, reattachChains, chainReattacher, stopAtFirst, 0, nthChild);
        }

        /// <summary>
        /// Main entry point into RecursiveRemoveMain. RecursiveRemoveMain should only ever be called by this method or itself. This method marks the dictionary as in use and will clear
        /// it and unmark after (unless some other call earlier on the method call stack was using it first, in which case it will let that method unmark and clear it [this is so that you
        /// can use RecursiveRemoveMain within RecursiveFind predicates without causing issues with the dictionary being modified during the predicates]) Note: can't think of many use cases
        /// for having RecursiveRemove within RecursiveFind predicates, but there are definitely some. It would be more common to use RecursiveFind within RecursiveRemove
        /// </summary>
        public static bool RecursiveRemoveEntry<C, R>(ILoot loot, LootPredicate<R> predicate, IItemDropRule rootRule, bool reattachChains, ChainReattcher chainReattacher, bool stopAtFirst, int n, int? nthChild) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            bool wasInuse = DictionaryInUse;
            DictionaryInUse = true;
            bool result = RecursiveRemoveMain<R>(loot, rule => rule.ChainFromImmediateParent() is C && predicate(rule), rootRule, reattachChains, chainReattacher, stopAtFirst, n, nthChild);
            if (!wasInuse)
            {
                DictionaryInUse = false;
                ParentDictionary.Clear();
            }
            return result;
        }

        /// <summary>
        /// Performs the following processes/checks on <paramref name="currRule"/>:: <br/><br/>
        ///
        /// <code>if (<paramref name="currRule"/> is of type <typeparamref name="R"/>, and <paramref name="currRule"/> matches the predicate)</code>
        /// Then it will remove currRule from this loot pool / its parent, and possibly re-attach currRule's children onto currRule's parent if <paramref name="reattachChains"/> is set to true.
        /// If <paramref name="reattachChains"/> is true but the rule's parent is the ILoot that this extension method originated from, then the children cannot be reattached since the parent isn't an IItemDropRule (there'd be no rule to chain it onto). If 
        /// <paramref name="reattachChains"/> is set to false it will terminate here because this means the loot chained after this rule is lost, so there is no reason to make modifications to this rule's children. 
        /// If <paramref name="reattachChains"/> is true, and <paramref name="stopAtFirst"/> is set to false, it will then repeat this same predicate-checking process on the children of the child recursively until it
        /// runs out of children to operate on. <br/><br/>
        /// 
        /// If <paramref name="nthChild"/> is supplied, then the above if check will fail unless <paramref name="currRule"/> is the nth descendent of the <see cref="IItemDropRule"/> or <see cref="ILoot"/> that this extension method originated from
        /// 
        /// <code>else</code>Then it will also repeat this process recursively on the children of the child. The rule chained onto currRule will be the new currRule on the next call and n goes up by 1 on the next call<br/><br/>
        /// 
        /// </summary>
        private static bool RecursiveRemoveMain<R>(ILoot loot, LootPredicate<R> predicate, IItemDropRule currRule, bool reattachChains, ChainReattcher chainReattacher, bool stopAtFirst, int n, int? nthChild) where R : IItemDropRule
        {
            bool canRemove = n != 0; // If main entry called on IItemDropRule, this will be false on the first iteration to prevent it from being removed (because we only want to remove its children and we also have no ILoot reference to remove it from)

            if (canRemove && currRule is R castedRule && predicate(castedRule) && (nthChild is null || nthChild == n))
            {
                if (currRule.ImmediateParent() is IItemDropRule parentRule)
                {
                    currRule.RemoveFromParent(reattachChains, chainReattacher);

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
                int nextN = n + 1;
                if (nthChild is not null && nextN > nthChild) // Stop trying to search if they specified that it must be nth child (maxN) and we are gonna be further than maxN next iteration
                {
                    return false;
                }

                bool removedAny = false;

                foreach (IItemDropRuleChainAttempt chainAttempt in new List<IItemDropRuleChainAttempt>(currRule.ChainedRules)) // iterate over shallow clone to stop concurrent modification
                {
                    IItemDropRule child = chainAttempt.RuleToChain;
                    child.SetParent(newParent, chainAttempt);

                    if (RecursiveRemoveMain(loot, predicate, child, reattachChains, chainReattacher, stopAtFirst, nextN, nthChild))
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

        #endregion

        #region Recursive Child Finding

        /// <summary>
        /// Recursively loops through this <see cref="ILoot"/> instance and sees if there is any <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that matches the given predicate. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return true. If no match is found, it will recursively loop through the children of that child and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return false. 
        /// </summary>
        public static bool HasRuleWhere<R>(this ILoot loot, LootPredicate<R> query, int? nthChild = null) where R : IItemDropRule
        {
            return loot.FindRuleWhere(query, nthChild) is not null;
        }

        /// <summary>
        /// Recursively loops through this <see cref="ILoot"/> instance and looks for the first <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that matches the given predicate. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return it. If no match is found, it will recursively loop through the children of that child and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return null.
        /// </summary>
        public static R FindRuleWhere<R>(this ILoot loot, LootPredicate<R> query, int? nthChild = null) where R : IItemDropRule
        {
            foreach (IItemDropRule rootRule in loot.Get())
                if (RecursiveFindEntry(rootRule, query, 1, nthChild) is R result)
                    return result;
            return default;
        }

        /// <summary>
        /// Recursively loops through this <see cref="IItemDropRule"/>'s children and sees if there is any child <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that matches the given predicate. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return true. If no match is found, it will recursively loop through the children of that child and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return false. 
        /// </summary>
        public static bool HasChildWhere<R>(this IItemDropRule root, LootPredicate<R> query, int? nthChild = null) where R : IItemDropRule
        {
            return root.FindChildWhere(query, nthChild) is not null;
        }

        /// <summary>
        /// Recursively loops through this <see cref="IItemDropRule"/>'s children and looks for the first child <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that matches the given predicate. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return it. If no match is found, it will recursively loop through the children of that child and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return null.
        /// </summary>
        public static R FindChildWhere<R>(this IItemDropRule root, LootPredicate<R> query, int? nthChild = null) where R : IItemDropRule
        {
            return RecursiveFindEntry(root, query, 0, nthChild);
        }

        /// <summary>
        /// Main entry point into RecursiveFindMain. RecursiveFindMain should only ever be called by this method or itself. This method marks the dictionary as in use and will clear
        /// it and unmark after (unless some other call earlier on the method call stack was using it first, in which case it will let that method unmark and clear it [this is so that you
        /// can use RecursiveFind within RecursiveRemoveWhere predicates without causing issues with the dictionary being modified during the predicates])
        /// </summary>
        public static R RecursiveFindEntry<R>(IItemDropRule root, LootPredicate<R> query, int n, int? nthChild = null) where R : IItemDropRule
        {
            bool wasInUse = DictionaryInUse;
            DictionaryInUse = true;
            R result = RecursiveFindMain(query, null, root, n, nthChild);
            if (!wasInUse)
            {
                DictionaryInUse = false;
                ParentDictionary.Clear();
            }
            return result;
        }

        // EVERYTHING HERE IS AN OVERLOAD OF THE STUFF ABOVE

        /// <summary>
        /// Recursively loops through this <see cref="ILoot"/> instance and sees if there is any <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that is chained onto its parent via an <see cref="IItemDropRuleChainAttempt"/> of type <typeparamref name="C"/> that matches the given predicate. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return true. If no match is found, it will recursively loop through the children of that child and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return false. 
        /// </summary>
        public static bool HasRuleWhere<C, R>(this ILoot loot, LootPredicate<R> query, int? nthChild = null) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            return loot.FindRuleWhere<C, R>(query, nthChild) is not null;
        }

        /// <summary>
        /// Recursively loops through this <see cref="ILoot"/> instance and looks for the first <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that is chained onto its parent via an <see cref="IItemDropRuleChainAttempt"/> of type <typeparamref name="C"/> that matches the given predicate. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return it. If no match is found, it will recursively loop through the children of that child and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return null.
        /// </summary>
        public static R FindRuleWhere<C, R>(this ILoot loot, LootPredicate<R> query, int? nthChild = null) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            foreach (IItemDropRule rootRule in loot.Get())
                if (RecursiveFindEntry<C, R>(rootRule, query, 1, nthChild) is R result)
                    return result;
            return default;
        }

        /// <summary>
        /// Recursively loops through this <see cref="IItemDropRule"/>'s children and sees if there is any child <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that is chained onto its parent via an <see cref="IItemDropRuleChainAttempt"/> of type <typeparamref name="C"/> that matches the given predicate. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return true. If no match is found, it will recursively loop through the children of that child and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return false. 
        /// </summary>
        public static bool HasChildWhere<C, R>(this IItemDropRule root, LootPredicate<R> query, int? nthChild = null) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            return root.FindChildWhere<C, R>(query, nthChild) is not null;
        }

        /// <summary>
        /// Recursively loops through this <see cref="IItemDropRule"/>'s children and looks for the first child <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that is chained onto its parent via an <see cref="IItemDropRuleChainAttempt"/> of type <typeparamref name="C"/> that matches the given predicate. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return it. If no match is found, it will recursively loop through the children of that child and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return null.
        /// </summary>
        public static R FindChildWhere<C, R>(this IItemDropRule root, LootPredicate<R> query, int? nthChild = null) where C : IItemDropRuleChainAttempt where R : IItemDropRule
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
            R result = RecursiveFindMain<R>(rule => rule.ChainFromImmediateParent() is C && query(rule), null, root, n, nthChild);
            if (!wasInUse)
            {
                DictionaryInUse = false;
                ParentDictionary.Clear();
            }
            return result;
        }

        /// <summary>
        /// Checks if currRule is of type <typeparamref name="R"/> and matches the given predicate. If it does it will be returned. If not, it will recursively call this method
        /// again on the children of currRule. If nthChild is specified, then the currRule must also be the nth child of whatever ILoot or IItemDropRule that this
        /// recursive call originated with.
        /// </summary>
        private static R RecursiveFindMain<R>(LootPredicate<R> query, IItemDropRuleChainAttempt chainToCurrRule, IItemDropRule currRule, int n, int? nthChild) where R : IItemDropRule
        {
            // n == 0 means this RecursiveFindMain call should not check currRule at all. This is used when FindChildren is first called on an IItemDropRule so that the rule
            // itself isn't queried and returned if it happens to match the predicate (because we only want to query its children). n will be initially set to 1 when called on ILoot entry, 0 when called on IItemDropRule entry
            
            // n must not be 0, currRule must be of type T and match the predicate, and nthChild must not be specified (or if it is specified, n must be == nthChild)
            if (n != 0 && currRule is R castedRule && query(castedRule) && (nthChild is null || nthChild == n))
            {
                return castedRule;
            }
            else
            {
                int nextN = n + 1;
                if (nthChild is not null && nextN > nthChild) // Stop trying to search if they specified that it must be nth child (maxN) and we are gonna be further than maxN next iteration
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

        #endregion

        #region Recursive Parent Finding

        // Special recursive methods to be used inside LootPredicates within Child Finding / Child Removing methods. Used to help narrow down search

        /// <summary>
        /// Recursively looks at <see cref="IItemDropRule"/>'s parent <see cref="IItemDropRule"/> and sees if it is of type <typeparamref name="R"/>, and if it matches the given predicate. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return true. If no match is found, it will recursively look at the parent of the parent and do the same check, so on and so forth.
        /// If nothing was found after searching all parents recursively, it will return false.<br/><br/>
        /// 
        /// Can only be used in the context of RemoveWhere/FindWhere/Has recursive predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying
        /// </summary>
        public static bool HasParentRuleWhere<R>(this IItemDropRule rule, LootPredicate<R> pred, int? nthParent = null) where R : IItemDropRule
        {
            return rule.FindParentRuleWhere(pred, nthParent) is not null;
        }

        /// <summary>
        /// Recursively looks at <see cref="IItemDropRule"/>'s parent <see cref="IItemDropRule"/> and sees if it is of type <typeparamref name="R"/>, and if it matches the given predicate. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return it. If no match is found, it will recursively look at the parent of the parent and do the same check, so on and so forth.
        /// If nothing was found after searching all parents recursively, it will return null.<br/><br/>
        /// 
        /// Can only be used in the context of RemoveWhere/FindWhere/Has recursive predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying        
        /// </summary>
        public static R FindParentRuleWhere<R>(this IItemDropRule rule, LootPredicate<R> pred, int? nthParent = null) where R : IItemDropRule
        {
            IItemDropRule currParent = rule.ImmediateParent();
            int n = 1;
            while (currParent is not null)
            {
                if (currParent is R castedParent && pred(castedParent) && (nthParent is null || nthParent == n))
                {
                    return castedParent;
                }

                n++;
                currParent = currParent.ImmediateParent();

            }
            return default;
        }

        /// <summary>
        /// Recursively looks at <see cref="IItemDropRule"/>'s parent <see cref="IItemDropRule"/> and sees if it matches the given predicate. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return true. If no match is found, it will recursively look at the parent of the parent and do the same check, so on and so forth.
        /// If nothing was found after searching all parents recursively, it will return false.<br/><br/>
        /// 
        /// Can only be used in the context of RemoveWhere/FindWhere/Has recursive predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying
        /// </summary>
        public static bool HasParentRuleWhere(this IItemDropRule rule, LootPredicate<IItemDropRule> pred, int? nthParent = null)
        {
            return rule.FindParentRuleWhere(pred, nthParent) is not null;
        }

        /// <summary>
        /// Recursively looks at <see cref="IItemDropRule"/>'s parent <see cref="IItemDropRule"/> and sees if it matches the given predicate. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return it. If no match is found, it will recursively look at the parent of the parent and do the same check, so on and so forth.
        /// If nothing was found after searching all parents recursively, it will return null.<br/><br/>
        /// 
        /// Can only be used in the context of RemoveWhere/FindWhere/Has recursive predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying        
        /// </summary>
        public static IItemDropRule FindParentRuleWhere(this IItemDropRule rule, LootPredicate<IItemDropRule> pred, int? nthParent = null)
        {
            return rule.FindParentRuleWhere<IItemDropRule>(pred, nthParent);
        }

        /// <summary>
        /// Recursively looks at <see cref="IItemDropRule"/>'s parent <see cref="IItemDropRule"/> and sees if it is of type <typeparamref name="R"/>, is chained onto its parent via an <see cref="IItemDropRuleChainAttempt"/> of type <typeparamref name="C"/>, and if it matches the given predicate. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return it. If no match is found, it will recursively look at the parent of the parent and do the same check, so on and so forth.
        /// If nothing was found after searching all parents recursively, it will return null.<br/><br/>
        /// </summary>
        /// 
        /// Can only be used in the context of RemoveWhere/FindWhere/Has recursive predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying
        public static bool HasParentRuleWhere<C, R>(this IItemDropRule rule, LootPredicate<R> pred, int? nthParent = null) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            return rule.FindParentRuleWhere<C, R>(pred, nthParent) is not null;
        }

        /// <summary>
        /// Recursively looks at <see cref="IItemDropRule"/>'s parent <see cref="IItemDropRule"/> and sees if it is of type <typeparamref name="R"/>, is chained onto its parent via an <see cref="IItemDropRuleChainAttempt"/> of type <typeparamref name="C"/>, and if it matches the given predicate. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return true. If no match is found, it will recursively look at the parent of the parent and do the same check, so on and so forth.
        /// If nothing was found after searching all parents recursively, it will return false.<br/><br/>
        /// 
        /// Can only be used in the context of RemoveWhere/FindWhere/Has recursive predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying        
        /// </summary>
        public static R FindParentRuleWhere<C, R>(this IItemDropRule rule, LootPredicate<R> pred, int? nthParent = null) where C : IItemDropRuleChainAttempt where R : IItemDropRule
        {
            return rule.FindParentRuleWhere<R>(rule => rule.ChainFromImmediateParent() is C && pred(rule), nthParent);
        }

        #endregion

        #region Data Structures and Helpers

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

        public static IItemDropRuleChainAttempt ChainFromImmediateParent(this IItemDropRule rule)
        {
            return ParentDictionary.TryGetValue(rule, out ParentWithChain parentWithChain) ? parentWithChain.ChainAttempt : null;
        }

        /// <summary>
        /// Finds the immediate parent of this rule. This is equivalent to <see cref="ParentRule(IItemDropRule, int)"/> where nthChild is 1. <br/><br/>
        /// 
        /// Can only be used in the context of RemoveWhere/FindWhere/Has recursive predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying
        /// </summary>
        public static IItemDropRule ImmediateParent(this IItemDropRule rule)
        {
            if (!DictionaryInUse)
                throw new Exception("IItemDropRule.ParentRule() can only be used in the context of predicates within RecursiveFind (Find<T>, Has<T>, FindChild<T>, HasChild<T>) or RecursiveRemove (RecursiveRemoveWhere, RecursiveRemoveChildren)");

            return ParentDictionary.TryGetValue(rule, out ParentWithChain parentWithChain) ? parentWithChain.Parent : null;
        }

        /// <summary>
        /// Returns the nth parent of this IItemDropRule. n being 1 means the direct parent of this rule, n being 2 means the parent of ParentRule(1), etc<br/><br/>
        /// 
        /// Can only be used in the context of Remove calls, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove as a means of being more exact about what
        /// rule we are removing
        /// </summary>
        public static IItemDropRule ParentRule(this IItemDropRule rule, int nthParent)
        {
            if (nthParent == 0)
                throw new Exception("nth parent must be greater than 0");

            for (int i = 0; i < nthParent; i++)
                rule = rule?.ImmediateParent();

            return rule;
        }

        /// <summary>
        /// Returns true if this rule has an <paramref name="nthParent"/>. nthParent = 1 is immediate parent, nthParent = 2 is immediate parent of immediate parent<br/><br/>
        /// 
        /// Can only be used in the context of RemoveWhere/FindWhere/Has recursive predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying
        /// </summary>
        public static bool HasParentRule(this IItemDropRule rule, int nthParent = 1)
        {
            return rule.ParentRule(nthParent) is not null;
        }

        /// <summary>Helper method for RecursiveRemoveMain. Removes <paramref name="removing"/> from the ChainedRules of its parent. If reattachChains
        /// is set to true it will attach the chains between <paramref name="removing"/> and its children onto the parent of <paramref name="removing"/> so
        /// that removing's children aren't lost. Furthermore, if <paramref name="reattcher"/> is supplied, you will be able to specify what type of chain
        /// to reattach per-child-of-removing</summary>
        private static void RemoveFromParent(this IItemDropRule removing, bool reattachChains = false, ChainReattcher reattcher = null)
        {
            if (ParentDictionary.TryGetValue(removing, out ParentWithChain parentWithChain))
            {
                IItemDropRule parentOfRemoving = parentWithChain.Parent;
                parentOfRemoving.ChainedRules.Remove(parentWithChain.ChainAttempt);

                if (reattachChains)
                    foreach (IItemDropRuleChainAttempt chainAttempt in removing.ChainedRules)
                        parentOfRemoving.ChainedRules.Add(reattcher is null ? chainAttempt : reattcher(chainAttempt.RuleToChain));
            }
        }

        #endregion

        #region Other ILoot Extensions

        /// <summary>Syntax sugar to clear this ILoot. What it does is calls RemoveWhere(_ => true) </summary>
        public static void Clear(this ILoot loot)
        {
            loot.RemoveWhere(_ => true);
        }

        #endregion

        #region LeadingConditionRule Extensions

        public static IItemDropRule OnConditionsMet(this LeadingConditionRule leadingCondition, params IItemDropRule[] rulesToExecute)
        {
            foreach (IItemDropRule rule in rulesToExecute)
            {
                leadingCondition.OnSuccess(rule);
            }
            return leadingCondition;
        }

        #endregion

        #region OneFromOptionsDropRule Extensions

        public static void AddOption(this OneFromOptionsDropRule oneFromOptionsRule, int option)
        {
            List<int> asList = oneFromOptionsRule.dropIds.ToList();
            asList.Add(option);
            oneFromOptionsRule.dropIds = asList.ToArray();
        }

        public static bool RemoveOption(this OneFromOptionsDropRule oneFromOptionsRule, int removing)
        {
            return oneFromOptionsRule.FilterOptions(option => option != removing);
        }

        public static bool ContainsOption(this OneFromOptionsDropRule oneFromOptionsRule, int option)
        {
            return oneFromOptionsRule.dropIds.Contains(option);
        }

        public static bool FilterOptions(this OneFromOptionsDropRule oneFromOptionsRule, Predicate<int> predicate)
        {
            bool anyFiltered = false;
            List<int> newDropIds = new();
            foreach (int dropId in oneFromOptionsRule.dropIds)
            {
                if (predicate(dropId))
                    newDropIds.Add(dropId);       
                else
                    anyFiltered = true;
            }
            oneFromOptionsRule.dropIds = newDropIds.ToArray();
            return anyFiltered;
        }


        #endregion

        #region OneFromOptionsNotScaledWithLuckDropRule Extensions

        public static void AddOption(this OneFromOptionsNotScaledWithLuckDropRule oneFromOptionsRule, int option)
        {
            List<int> asList = oneFromOptionsRule.dropIds.ToList();
            asList.Add(option);
            oneFromOptionsRule.dropIds = asList.ToArray();
        }

        public static bool RemoveOption(this OneFromOptionsNotScaledWithLuckDropRule oneFromOptionsRule, int removing)
        {
            return oneFromOptionsRule.FilterOptions(option => option != removing);
        }

        public static bool ContainsOption(this OneFromOptionsNotScaledWithLuckDropRule oneFromOptionsRule, int option)
        {
            return oneFromOptionsRule.dropIds.Contains(option);
        }

        public static bool FilterOptions(this OneFromOptionsNotScaledWithLuckDropRule oneFromOptionsRule, Predicate<int> predicate)
        {
            bool anyFiltered = false;
            List<int> newDropIds = new();
            foreach (int dropId in oneFromOptionsRule.dropIds)
            {
                if (predicate(dropId))
                    newDropIds.Add(dropId);
                else
                    anyFiltered = true;
            }
            oneFromOptionsRule.dropIds = newDropIds.ToArray();
            return anyFiltered;
        }

        #endregion

        #region OneFromRulesRule Extensions

        public static void AddRule(this OneFromRulesRule oneFromRulesRule, IItemDropRule option)
        {
            List<IItemDropRule> asList = oneFromRulesRule.options.ToList();
            asList.Add(option);
            oneFromRulesRule.options = asList.ToArray();
        }

        public static bool RemoveRule(this OneFromRulesRule oneFromRulesRule, IItemDropRule removing)
        {
            return oneFromRulesRule.FilterRules(rule => rule != removing);
        }

        public static bool RemoveRule(this OneFromRulesRule oneFromRulesRule, Predicate<IItemDropRule> query)
        {
            return oneFromRulesRule.FilterRules(rule => !query(rule));
        }

        public static bool ContainsRule(this OneFromRulesRule oneFromRulesRule, IItemDropRule ruleOption)
        {
            return oneFromRulesRule.options.Contains(ruleOption);
        }

        public static bool FilterRules(this OneFromRulesRule oneFromRulesRule, Predicate<IItemDropRule> predicate)
        {
            bool anyFiltered = false;
            List<IItemDropRule> newOptions = new();
            foreach (IItemDropRule rule in oneFromRulesRule.options)
            {
                if (predicate(rule))
                    newOptions.Add(rule);
                else
                    anyFiltered = true;
            }
            oneFromRulesRule.options = newOptions.ToArray();
            return anyFiltered;
        }

        #endregion

        #region SequentialRulesRule Extensions

        public static void AddRule(this SequentialRulesRule sequentialRulesRule, IItemDropRule option)
        {
            List<IItemDropRule> asList = sequentialRulesRule.rules.ToList();
            asList.Add(option);
            sequentialRulesRule.rules = asList.ToArray();
        }

        public static bool RemoveRule(this SequentialRulesRule sequentialRulesRule, IItemDropRule removing)
        {
            return sequentialRulesRule.FilterRules(rule => rule != removing);
        }

        public static bool RemoveRule(this SequentialRulesRule sequentialRulesRule, Predicate<IItemDropRule> query)
        {
            return sequentialRulesRule.FilterRules(rule => !query(rule));
        }

        public static bool ContainsRule(this SequentialRulesRule sequentialRulesRule, IItemDropRule ruleOption)
        {
            return sequentialRulesRule.rules.Contains(ruleOption);
        }

        public static bool FilterRules(this SequentialRulesRule sequentialRulesRule, Predicate<IItemDropRule> predicate)
        {
            bool anyFiltered = false;
            List<IItemDropRule> newRules = new();
            foreach (IItemDropRule rule in sequentialRulesRule.rules)
            {
                if (predicate(rule))
                    newRules.Add(rule);
                else
                    anyFiltered = true;
            }
            sequentialRulesRule.rules = newRules.ToArray();
            return anyFiltered;
        }

        #endregion

        #region SequentialRulesNotScalingWithLuckRule Extensions

        public static void AddRule(this SequentialRulesNotScalingWithLuckRule sequentialRulesRule, IItemDropRule option)
        {
            List<IItemDropRule> asList = sequentialRulesRule.rules.ToList();
            asList.Add(option);
            sequentialRulesRule.rules = asList.ToArray();
        }

        public static bool RemoveRule(this SequentialRulesNotScalingWithLuckRule sequentialRulesRule, IItemDropRule removing)
        {
            return sequentialRulesRule.FilterRules(rule => rule != removing);
        }

        public static bool RemoveRule(this SequentialRulesNotScalingWithLuckRule sequentialRulesRule, Predicate<IItemDropRule> query)
        {
            return sequentialRulesRule.FilterRules(rule => !query(rule));
        }

        public static bool ContainsRule(this SequentialRulesNotScalingWithLuckRule sequentialRulesRule, IItemDropRule ruleOption)
        {
            return sequentialRulesRule.rules.Contains(ruleOption);
        }

        public static bool FilterRules(this SequentialRulesNotScalingWithLuckRule sequentialRulesRule, Predicate<IItemDropRule> predicate)
        {
            bool anyFiltered = false;
            List<IItemDropRule> newRules = new();
            foreach (IItemDropRule rule in sequentialRulesRule.rules)
            {
                if (predicate(rule))
                    newRules.Add(rule);
                else
                    anyFiltered = true;
            }
            sequentialRulesRule.rules = newRules.ToArray();
            return anyFiltered;
        }

        #endregion
    }
}