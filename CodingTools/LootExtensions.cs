using System;
using System.Collections.Generic;
using System.Linq;
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
        /// A <see cref="LootPredicate{R}"/> is the same as a regular query but the type param R must be an <see cref="IItemDropRule"/>.
        /// LootPredicates are used to find a specific rule that matches the supplied set of conditions.<br/>
        /// <br/>
        /// These predicates can be used inside any of the top-level recursive extension methods:<br/><br/>
        /// <see cref="RemoveWhere{R}(ILoot, LootPredicate{R}, bool, int?, bool, ChainReattcher, bool)"/><br/>
        /// <see cref="RemoveChildrenWhere{R}(IItemDropRule, LootPredicate{R}, int?, bool, ChainReattcher, bool)"/><br/>
        /// <see cref="RemoveWhere{C, R}(ILoot, LootPredicate{R}, bool, int?, bool, ChainReattcher bool)"/><br/>
        /// <see cref="RemoveChildrenWhere{C, R}(IItemDropRule, LootPredicate{R}, int?, bool, ChainReattcher bool)"/><br/>
        /// <see cref="RemoveWhere{N, R}(ILoot, LootPredicate{R}, bool, bool, int?)"/><br/>
        /// <see cref="RemoveChildrenWhere{N, R}(IItemDropRule, LootPredicate{R}, bool, int?)"/><br/>
        /// <br/>
        /// <see cref="HasRuleWhere{R}(ILoot, LootPredicate{R}, bool, int?)"/><br/>
        /// <see cref="FindRuleWhere{R}(ILoot, LootPredicate{R}, bool, ChainReattcher, int?)"/><br/>
        /// <see cref="HasChildWhere{R}(IItemDropRule, LootPredicate{R}, int?)"/><br/>
        /// <see cref="FindChildWhere{R}(IItemDropRule, LootPredicate{R}, ChainReattcher, int?)"/><br/>
        /// <see cref="HasRuleWhere{C, R}(ILoot, LootPredicate{R}, bool, int?)"/><br/>
        /// <see cref="FindRuleWhere{C, R}(ILoot, LootPredicate{R}, bool, ChainReattcher, int?)"/><br/>
        /// <see cref="HasChildWhere{C, R}(IItemDropRule, LootPredicate{R}, int?)"/><br/>
        /// <see cref="FindChildWhere{C, R}(IItemDropRule, LootPredicate{R}, ChainReattcher, int?)"/><br/>
        /// <see cref="HasRuleWhere{N, R}(ILoot, LootPredicate{R}, bool, int?, int)"/><br/>
        /// <see cref="FindRuleWhere{N, R}(ILoot, LootPredicate{R}, bool, int?)"/><br/>
        /// <see cref="HasChildWhere{N, R}(IItemDropRule, LootPredicate{R}, int?, int)"/><br/>
        /// <see cref="FindChildWhere{N, R}(IItemDropRule, LootPredicate{R}, int?)"/><br/>
        /// <br/>
        /// There are also special helper extension methods for <see cref="IItemDropRule"/>s that can <i>only</i> be used within these predicates:<br/><br/>
        /// <see cref="ParentRule(IItemDropRule, int)"/><br/>
        /// <see cref="ImmediateParent(IItemDropRule)"/><br/>
        /// <see cref="ChainFromImmediateParent(IItemDropRule)"/><br/>
        /// <see cref="IsChained(IItemDropRule)"/><br/>
        /// <see cref="IsNested(IItemDropRule)"/><br/>
        /// <see cref="HasParentRuleWhere{R}(IItemDropRule, LootPredicate{R}, int?)"/><br/>
        /// <see cref="FindParentRuleWhere{R}(IItemDropRule, LootPredicate{R}, int?)"/><br/>
        /// <see cref="HasParentRuleWhere{C, R}(IItemDropRule, LootPredicate{R}, int?)"/><br/>
        /// <see cref="FindParentRuleWhere{C, R}(IItemDropRule, LootPredicate{R}, int?)"/><br/>
        /// <see cref="HasParentRuleWhere{N, R}(IItemDropRule, LootPredicate{R}, int?, int)"/><br/>
        /// <see cref="FindParentRuleWhere{N, R}(IItemDropRule, LootPredicate{R}, int?, int)"/><br/>
        /// </summary>
        public delegate bool LootPredicate<R>(R rule) where R : class, IItemDropRule;

        #region Recursive Removing 

        #region General Recursive Removing

        // RECURSIVE REMOVING

        /// <summary>
        /// Performs the following processes/checks on each <see cref="IItemDropRule"/> "<paramref name="currRule"/>" within <paramref name="loot"/>.Get()  <br/><br/>
        ///
        /// <code>if (<paramref name="currRule"/> is of type <typeparamref name="R"/>, and <paramref name="currRule"/> matches the <paramref name="query"/>)</code>
        /// 
        /// Then it will remove currRule from this loot pool, and possibly re-attach currRule's children onto currRule's parent if <paramref name="reattachChains"/> is set to true.
        /// If <paramref name="reattachChains"/> is true but the rule has no parent (i.e it is directly inside this ILoot, as opposed to being the child of a rule inside this ILoot), the chains can't be reattached (they don't exist). If 
        /// <paramref name="reattachChains"/> is set to false (or there are no chains) it will terminate here because this means the loot chained after this rule is lost no matter what, so there is no reason to make modifications to this rule's children. 
        /// If <paramref name="reattachChains"/> is true, and <paramref name="stopAtFirst"/> is set to false, it will then repeat this same query-checking process on the children of the child (nested and chained children) recursively until it
        /// runs out of children to operate on. <br/><br/>
        /// 
        /// If <paramref name="nthChild"/> is supplied, then the above if check will fail unless <paramref name="currRule"/> is the nth descendent of the <see cref="IItemDropRule"/> or <see cref="ILoot"/> that this extension method originated from.<br/><br/>
        ///
        /// If <paramref name="chainReattacher"/> function is supplied, and reattachChains is set to true, it will re-attach the chains using the function provided as opposed to the default re-attaching mechanism (which is to add currRule's ChainedRules to the 
        /// parent that currRule is removed from). The rule being chained is the param of the function and it should return some type of <see cref="IItemDropRuleChainAttempt"/> where ChainAttempt.RuleToChain is set to the parameter.
        ///
        /// <code>else</code>Then it will also repeat this process recursively on the children of the child (nested and chained children). The rule chained/nested onto currRule will be the new currRule on the next call and n goes up by 1 on the next call<br/><br/>
        ///
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary>
        public static bool RemoveWhere<R>(this ILoot loot, LootPredicate<R> query = null, bool includeGlobalDrops = false, int? nthChild = null, bool reattachChains = false, ChainReattcher chainReattacher = null, bool stopAtFirst = true) where R : class, IItemDropRule
        {
            bool removedAny = false;

            foreach (IItemDropRule rootRule in loot.Get(includeGlobalDrops))
            {
                if (RecursiveRemoveEntryPoint(loot, query, rootRule, reattachChains, chainReattacher, stopAtFirst, 1, nthChild))
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
        /// <code>if (<paramref name="currRule"/> is of type <typeparamref name="R"/>, and <paramref name="currRule"/> matches the <paramref name="query"/>)</code>
        /// 
        /// Then it will remove currRule from this loot pool, and possibly re-attach currRule's children onto currRule's parent if <paramref name="reattachChains"/> is set to true.
        /// If <paramref name="reattachChains"/> is set to false it will terminate here because this means the loot chained after this rule is lost no matter what, so there is no reason to make modifications to this rule's children. 
        /// If <paramref name="reattachChains"/> is true, and <paramref name="stopAtFirst"/> is set to false, it will then repeat this same query-checking process on the children of the child (nested and chained children) recursively until it
        /// runs out of children to operate on. <br/><br/>
        /// 
        /// If <paramref name="nthChild"/> is supplied, then the above if check will fail unless <paramref name="currRule"/> is the nth descendent of the <see cref="IItemDropRule"/> or <see cref="ILoot"/> that this extension method originated from.<br/><br/>
        ///
        /// If <paramref name="chainReattacher"/> function is supplied, and reattachChains is set to true, it will re-attach the chains using the function provided as opposed to the default re-attaching mechanism (which is to add currRule's ChainedRules to the 
        /// parent that currRule is removed from). The rule being chained is the param of the function and it should return some type of <see cref="IItemDropRuleChainAttempt"/> where ChainAttempt.RuleToChain is set to the parameter.
        ///
        /// <code>else</code>Then it will also repeat this process recursively on the children of the child (nested and chained children). The rule chained/nested onto currRule will be the new currRule on the next call and n goes up by 1 on the next call<br/><br/>
        /// 
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary>
        public static bool RemoveChildrenWhere<R>(this IItemDropRule rootRule, LootPredicate<R> query = null, int? nthChild = null, bool reattachChains = false, ChainReattcher chainReattacher = null, bool stopAtFirst = true) where R : class, IItemDropRule
        {
            return RecursiveRemoveEntryPoint(null, query, rootRule, reattachChains, chainReattacher, stopAtFirst, 0, nthChild);
        }

        /// <summary>
        /// Main entry point into RecursiveRemoveMain. RecursiveRemoveMain should only ever be called by this method or itself. This method marks the dictionary as in use and will clear
        /// it and unmark after (unless some other call earlier on the method call stack was using it first, in which case it will let that method unmark and clear it [this is so that you
        /// can use RecursiveRemoveMain within RecursiveFind predicates without causing issues with the dictionary being modified during the predicates]) Note: can't think of many use cases
        /// for having RecursiveRemove within RecursiveFind predicates, but there are definitely some. It would be more common to use RecursiveFind within RecursiveRemove
        /// </summary>
        private static bool RecursiveRemoveEntryPoint<R>(ILoot loot, LootPredicate<R> query, IItemDropRule rootRule, bool reattachChains, ChainReattcher chainReattacher, bool stopAtFirst, int n, int? nthChild) where R : class, IItemDropRule
        {
            UseDictionary();
            query ??= (_) => true;
            bool result = RecursiveRemoveMain(loot, query, rootRule, reattachChains, chainReattacher, stopAtFirst, n, nthChild);
            StopUsingDictionary();
            return result;
        }

        #endregion

        // The following methods are syntax sugar and apply the following 3 checks to your query automatically: rule => rule.HasParentRule() && rule.IsChained() && rule.ChainFromImmediateParent() is C
        #region Chained Rule Overloads

        /// <summary>
        /// Performs the following processes/checks on each child rule, "<paramref name="currRule"/>" of this <paramref name="rootRule"/><br/><br/>
        ///
        /// <code>if (<paramref name="currRule"/> was chained onto its parent via an <see cref="IItemDropRuleChainAttempt"/> of type <typeparamref name="C"/>, and <paramref name="currRule"/> is of type <typeparamref name="R"/>, and <paramref name="currRule"/> matches the <paramref name="query"/>)</code>
        /// Then it will remove currRule from this loot pool, and possibly re-attach currRule's children onto currRule's parent if <paramref name="reattachChains"/> is set to true.
        /// If <paramref name="reattachChains"/> is true but the rule has no parent (i.e it is directly inside this ILoot, as opposed to being the child of a rule inside this ILoot), the chains can't be reattached (they don't exist). If 
        /// <paramref name="reattachChains"/> is set to false (or there are no chains) it will terminate here because this means the loot chained after this rule is lost no matter what, so there is no reason to make modifications to this rule's children. 
        /// If <paramref name="reattachChains"/> is true, and <paramref name="stopAtFirst"/> is set to false, it will then repeat this same query-checking process on the children of the child (nested and chained children) recursively until it
        /// runs out of children to operate on. <br/><br/>
        /// 
        /// If <paramref name="nthChild"/> is supplied, then the above if check will fail unless <paramref name="currRule"/> is the nth descendent of the <see cref="IItemDropRule"/> or <see cref="ILoot"/> that this extension method originated from.<br/><br/>
        ///
        /// If <paramref name="chainReattacher"/> function is supplied, and reattachChains is set to true, it will re-attach the chains using the function provided as opposed to the default re-attaching mechanism (which is to add currRule's ChainedRules to the 
        /// parent that currRule is removed from). The rule being chained is the param of the function and it should return some type of <see cref="IItemDropRuleChainAttempt"/> where ChainAttempt.RuleToChain is set to the parameter.
        ///
        /// <code>else</code>Then it will also repeat this process recursively on the children of the child (nested and chained children). The rule chained/nested onto currRule will be the new currRule on the next call and n goes up by 1 on the next call<br/><br/>
        /// 
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>
        /// </summary>
        public static bool RemoveWhere<C, R>(this ILoot loot, LootPredicate<R> query = null, bool includeGlobalDrops = false, int? nthChild = null, bool reattachChains = false, ChainReattcher chainReattacher = null, bool stopAtFirst = true) where C : class, IItemDropRuleChainAttempt where R : class, IItemDropRule
        {
            bool removedAny = false;

            foreach (IItemDropRule rootRule in loot.Get(includeGlobalDrops))
            {
                if (RecursiveRemoveEntryPoint<C, R>(loot, query, rootRule, reattachChains, chainReattacher, stopAtFirst, 1, nthChild))
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
        /// Performs the following processes/checks on each child rule, "<paramref name="currRule"/>" of this <paramref name="rootRule"/><br/><br/>
        ///
        /// <code>if (<paramref name="currRule"/> was chained onto its parent via an <see cref="IItemDropRuleChainAttempt"/> of type <typeparamref name="C"/>, and <paramref name="currRule"/> is of type <typeparamref name="R"/>, and <paramref name="currRule"/> matches the <paramref name="query"/>)</code>
        /// Then it will remove currRule from this loot pool, and possibly re-attach currRule's children onto currRule's parent if <paramref name="reattachChains"/> is set to true.
        /// If <paramref name="reattachChains"/> is set to false it will terminate here because this means the loot chained after this rule is lost no matter what, so there is no reason to make modifications to this rule's children. 
        /// If <paramref name="reattachChains"/> is true, and <paramref name="stopAtFirst"/> is set to false, it will then repeat this same query-checking process on the children of the child (nested and chained children) recursively until it
        /// runs out of children to operate on. <br/><br/>
        /// 
        /// If <paramref name="nthChild"/> is supplied, then the above if check will fail unless <paramref name="currRule"/> is the nth descendent of the <see cref="IItemDropRule"/> or <see cref="ILoot"/> that this extension method originated from.<br/><br/>
        ///
        /// If <paramref name="chainReattacher"/> function is supplied, and reattachChains is set to true, it will re-attach the chains using the function provided as opposed to the default re-attaching mechanism (which is to add currRule's ChainedRules to the 
        /// parent that currRule is removed from). The rule being chained is the param of the function and it should return some type of <see cref="IItemDropRuleChainAttempt"/> where ChainAttempt.RuleToChain is set to the parameter.
        ///
        /// <code>else</code>Then it will also repeat this process recursively on the children of the child (nested and chained children). The rule chained/nested onto currRule will be the new currRule on the next call and n goes up by 1 on the next call<br/><br/>
        /// 
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary>
        public static bool RemoveChildrenWhere<C, R>(this IItemDropRule rootRule, LootPredicate<R> query = null, int? nthChild = null, bool reattachChains = false, ChainReattcher chainReattacher = null, bool stopAtFirst = true) where C : class, IItemDropRuleChainAttempt where R : class, IItemDropRule
        {
            return RecursiveRemoveEntryPoint<C, R>(null, query, rootRule, reattachChains, chainReattacher, stopAtFirst, 0, nthChild);
        }

        /// <summary>
        /// Main entry point into RecursiveRemoveMain. RecursiveRemoveMain should only ever be called by this method or itself. This method marks the dictionary as in use and will clear
        /// it and unmark after (unless some other call earlier on the method call stack was using it first, in which case it will let that method unmark and clear it [this is so that you
        /// can use RecursiveRemoveMain within RecursiveFind predicates without causing issues with the dictionary being modified during the predicates]) Note: can't think of many use cases
        /// for having RecursiveRemove within RecursiveFind predicates, but there are definitely some. It would be more common to use RecursiveFind within RecursiveRemove
        /// </summary>
        private static bool RecursiveRemoveEntryPoint<C, R>(ILoot loot, LootPredicate<R> query, IItemDropRule rootRule, bool reattachChains, ChainReattcher chainReattacher, bool stopAtFirst, int n, int? nthChild) where C : class, IItemDropRuleChainAttempt where R : class, IItemDropRule
        {
            UseDictionary();
            query ??= (_) => true;
            bool result = RecursiveRemoveMain<R>(loot, rule => rule.HasParentRule() && rule.IsChained() && rule.ChainFromImmediateParent() is C && query(rule), rootRule, reattachChains, chainReattacher, stopAtFirst, n, nthChild);
            StopUsingDictionary();
            return result;
        }

        #endregion

        // The following methods are syntax sugar and apply the following 3 checks to your query automatically: rule => rule.HasParentRule() && rule.IsNested() && rule.ImmediateParent() is N
        #region Nested Rule Overloads

        /// <summary>
        /// Performs the following processes/checks on each rule, "<paramref name="currRule"/>" of this <paramref name="loot"/><br/><br/>
        ///
        /// <code>if (<paramref name="currRule"/> is nested inside of an <see cref="INestedItemDropRule"/> of type <typeparamref name="N"/>, and <paramref name="currRule"/> is of type <typeparamref name="R"/>, and <paramref name="currRule"/> matches the <paramref name="query"/>)</code>
        ///
        /// Then it will remove currRule from this loot pool. The function will stop on the first removed rule if <paramref name="stopAtFirst"/> is true. <br/><br/>
        /// 
        /// If <paramref name="nthChild"/> is supplied, then the above if check will fail unless <paramref name="currRule"/> is the nth descendent of the <see cref="IItemDropRule"/> or <see cref="ILoot"/> that this extension method originated from.<br/><br/>
        ///
        /// <code>else</code>Then it will also repeat this process recursively on the children of the child (nested and chained children). The rule chained/nested onto currRule will be the new currRule on the next call and n goes up by 1 on the next call<br/><br/>
        /// 
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary>
        public static bool RemoveWhere<N, R>(this ILoot loot, LootPredicate<R> query = null, bool includeGlobalDrops = false, bool stopAtFirst = true, int? nthChild = null) where N : class, IItemDropRule, INestedItemDropRule where R : class, IItemDropRule
        {
            foreach (IItemDropRule rootRule in loot.Get(includeGlobalDrops))
            {
                if (RecursiveRemoveEntryPoint<N, R>(loot, query, rootRule, stopAtFirst, 1, nthChild))
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Performs the following processes/checks on each child rule, "<paramref name="currRule"/>" of this <paramref name="rootRule"/><br/><br/>
        ///
        /// <code>if (<paramref name="currRule"/> is nested inside of an <see cref="INestedItemDropRule"/> of type <typeparamref name="N"/>, and <paramref name="currRule"/> is of type <typeparamref name="R"/>, and <paramref name="currRule"/> matches the <paramref name="query"/>)</code>
        ///
        /// Then it will remove currRule from this loot pool. The function will stop on the first removed rule if <paramref name="stopAtFirst"/> is true <br/><br/>
        /// 
        /// If <paramref name="nthChild"/> is supplied, then the above if check will fail unless <paramref name="currRule"/> is the nth descendent of the <see cref="IItemDropRule"/> or <see cref="ILoot"/> that this extension method originated from.<br/><br/>
        ///
        /// <code>else</code>Then it will also repeat this process recursively on the children of the child (nested and chained children). The rule chained/nested onto currRule will be the new currRule on the next call and n goes up by 1 on the next call<br/><br/>
        /// 
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary>
        public static bool RemoveChildrenWhere<N, R>(this IItemDropRule rootRule, LootPredicate<R> query = null, bool stopAtFirst = true, int? nthChild = null) where N : class, IItemDropRule, INestedItemDropRule where R : class, IItemDropRule
        {
            return RecursiveRemoveEntryPoint<N, R>(null, query, rootRule, stopAtFirst, 0, nthChild);
        }

        /// <summary>
        /// Main entry point into RecursiveRemoveMain. RecursiveRemoveMain should only ever be called by this method or itself. This method marks the dictionary as in use and will clear
        /// it and unmark after (unless some other call earlier on the method call stack was using it first, in which case it will let that method unmark and clear it [this is so that you
        /// can use RecursiveRemoveMain within RecursiveFind predicates without causing issues with the dictionary being modified during the predicates]) Note: can't think of many use cases
        /// for having RecursiveRemove within RecursiveFind predicates, but there are definitely some. It would be more common to use RecursiveFind within RecursiveRemove
        /// </summary>
        private static bool RecursiveRemoveEntryPoint<N, R>(ILoot loot, LootPredicate<R> query, IItemDropRule rootRule, bool stopAtFirst, int n, int? nthChild) where N : class, IItemDropRule, INestedItemDropRule where R : class, IItemDropRule
        {
            UseDictionary();
            query ??= (_) => true;
            bool result = RecursiveRemoveMain<R>(loot, rule => rule.HasParentRule() && rule.IsNested() && rule.ImmediateParent() is N && query(rule), rootRule, false, null, stopAtFirst, n, nthChild);
            StopUsingDictionary();
            return result;
        }

        #endregion

        // Main function
        #region Main Recursive Removal Method (all other recursive removal functions use this in some way)

        /// <summary>
        /// Performs the following processes/checks on <paramref name="currRule"/>:<br/><br/>
        ///
        /// <code>if (<paramref name="currRule"/> is of type <typeparamref name="R"/>, and <paramref name="currRule"/> matches the <paramref name="query"/>)</code>
        /// Then it will remove currRule from this loot pool / its parent, and possibly re-attach currRule's children onto currRule's parent if <paramref name="reattachChains"/> is set to true.
        /// If <paramref name="reattachChains"/> is true but the rule's parent is the ILoot that this extension method originated from, then the children cannot be reattached since the parent isn't an IItemDropRule (there'd be no rule to chain it onto). If 
        /// <paramref name="reattachChains"/> is set to false it will terminate here because this means the loot chained after this rule is lost no matter what, so there is no reason to make modifications to this rule's children. 
        /// If <paramref name="reattachChains"/> is true, and <paramref name="stopAtFirst"/> is set to false, it will then repeat this same query-checking process on the children of the child (nested and chained children) (both nested and chained rules)
        /// recursively until it runs out of children to operate on. <br/><br/>
        /// 
        /// If <paramref name="nthChild"/> is supplied, then the above if check will fail unless <paramref name="currRule"/> is the nth descendent of the <see cref="IItemDropRule"/> or <see cref="ILoot"/> that this extension method originated from.<br/><br/>
        ///
        /// If <paramref name="chainReattacher"/> function is supplied, and reattachChains is set to true, it will re-attach the chains using the function provided as opposed to the default re-attaching mechanism (which is to add currRule's ChainedRules to the 
        /// parent that currRule is removed from). The rule being chained is the param of the function and it should return some type of <see cref="IItemDropRuleChainAttempt"/> where ChainAttempt.RuleToChain is set to the parameter.
        ///
        /// <code>else</code>Then it will also repeat this process recursively on the children of the child (nested and chained children). The rule chained/nested onto currRule will be the new currRule on the next call and n goes up by 1 on the next call<br/><br/>
        /// 
        /// </summary>
        private static bool RecursiveRemoveMain<R>(ILoot loot, LootPredicate<R> query, IItemDropRule currRule, bool reattachChains, ChainReattcher chainReattacher, bool stopAtFirst, int n, int? nthChild) where R : class, IItemDropRule
        {
            bool canRemove = n != 0; // If main entry called on IItemDropRule, this will be false on the first iteration to prevent it from being removed (because we only want to remove its children and we also have no ILoot reference to remove it from)

            if (canRemove && currRule is R castedRule && query(castedRule) && (nthChild is null || nthChild == n))
            {
                if (currRule.ImmediateParent() is IItemDropRule parentRule)
                {
                    RemoveFromParent(currRule);

                    if (!currRule.IsNested() && reattachChains && !stopAtFirst)
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

            // Return true if any were removed. Returns immediately after finding one (preventing extra calls to RecursiveRemoveMain) if stopAtFirst is set to true
            bool ContinueRecursion(IItemDropRule newParent)
            {
                if (nthChild is not null && (n + 1) > nthChild) // Stop trying to search if they specified that it must be nth child (maxN) and we are gonna be further than maxN next iteration
                {
                    return false;
                }

                bool removedAny = false;

                // Try nested rules first
                if (currRule is INestedItemDropRule ruleThatExecutesOthers)
                {
                    foreach (IItemDropRule nestedRule in GetRulesNestedInsideThisRule(ruleThatExecutesOthers))
                    {
                        nestedRule.RegisterAsNestedChild(ruleThatExecutesOthers);

                        removedAny |= RecursiveRemoveMain(loot, query, nestedRule, reattachChains, chainReattacher, stopAtFirst, n + 1, nthChild);

                        if (removedAny && stopAtFirst)
                        {
                            return true;
                        }
                    }
                }

                // Then chained rules
                foreach (IItemDropRuleChainAttempt chainAttempt in new List<IItemDropRuleChainAttempt>(currRule.ChainedRules)) // iterate over shallow clone to stop concurrent modification
                {
                    IItemDropRule child = chainAttempt.RuleToChain;
                    child.RegisterAsChainedChild(newParent, chainAttempt);

                    removedAny |= RecursiveRemoveMain(loot, query, child, reattachChains, chainReattacher, stopAtFirst, n + 1, nthChild);

                    if (removedAny && stopAtFirst)
                    {
                        return true;
                    }
                }

                return removedAny;
            }

            void RemoveFromParent(IItemDropRule removing)
            {
                // Down here it is implied that the entry exists in the dictionary
                ParentChildRelationship relationship = ParentDictionary[removing];

                // If the rule is chained to a parent
                if (relationship.ChainedParent is not null)
                {
                    IItemDropRule parentOfRemoving = relationship.ChainedParent;
                    parentOfRemoving.ChainedRules.Remove(relationship.ChainAttempt);

                    if (reattachChains)
                    {
                        foreach (IItemDropRuleChainAttempt chainAttempt in removing.ChainedRules)
                        {
                            parentOfRemoving.ChainedRules.Add(chainReattacher is null ? chainAttempt : chainReattacher(chainAttempt.RuleToChain));
                        }
                    }

                }

                // If the rule is nested inside of a parent
                else
                {
                    RemoveNestedRuleFromParent(removing, (INestedItemDropRule)relationship.NestedParent);
                }
            }
        }

        private static IItemDropRule[] GetRulesNestedInsideThisRule(INestedItemDropRule rule)
        {
            IItemDropRule[] children = null;
            if (rule is OneFromRulesRule ofr)
            {
                children = ofr.options;
            }
            else if (rule is FewFromRulesRule ffr)
            {
                children = ffr.options;
            }
            else if (rule is SequentialRulesRule srr)
            {
                children = srr.rules;
            }
            else if (rule is SequentialRulesNotScalingWithLuckRule srrnl)
            {
                children = srrnl.rules;
            }
            else if (rule is AlwaysAtleastOneSuccessDropRule aos)
            {
                children = aos.rules;
            }
            else if (rule is DropBasedOnExpertMode dbem)
            {
                children = new IItemDropRule[] { dbem.ruleForExpertMode, dbem.ruleForNormalMode };
            }
            else if (rule is DropBasedOnMasterMode dbmm)
            {
                children = new IItemDropRule[] { dbmm.ruleForMasterMode, dbmm.ruleForDefault };
            }

            return children;
        }

        private static void RemoveNestedRuleFromParent(IItemDropRule rule, INestedItemDropRule parent)
        {
            if (parent is OneFromRulesRule ofr)
            {
                ofr.RemoveOption(rule);
            }
            else if (parent is FewFromRulesRule ffr)
            {
                ffr.RemoveOption(rule);
            }
            else if (parent is SequentialRulesRule srr)
            {
                srr.RemoveOption(rule);
            }
            else if (parent is SequentialRulesNotScalingWithLuckRule srrnl)
            {
                srrnl.RemoveOption(rule);
            }
            else if (parent is AlwaysAtleastOneSuccessDropRule aos)
            {
                aos.RemoveOption(rule);
            }
            else if (parent is DropBasedOnExpertMode dbem)
            {
                if (dbem.ruleForExpertMode == rule)
                {
                    dbem.ruleForExpertMode = ItemDropRule.DropNothing();

                }
                if (dbem.ruleForNormalMode == rule)
                {
                    dbem.ruleForNormalMode = ItemDropRule.DropNothing();
                }
            }
            else if (parent is DropBasedOnMasterMode dbmm)
            {
                if (dbmm.ruleForMasterMode == rule)
                {
                    dbmm.ruleForMasterMode = ItemDropRule.DropNothing();
                }
                if (dbmm.ruleForDefault == rule)
                {
                    dbmm.ruleForDefault = ItemDropRule.DropNothing();
                }
            }
        }

        #endregion

        #endregion

        #region Recursive Finding

        #region General Recursive Finding

        /// <summary>
        /// Recursively loops through this <see cref="ILoot"/> instance and sees if there is any <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that matches the given <paramref name="query"/>. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return true. If no match is found, it will recursively loop through the children of that child (both nested and chained children) and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return false.<br/><br/>
        /// 
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary>
        public static bool HasRuleWhere<R>(this ILoot loot, LootPredicate<R> query = null, bool includeGlobalDrops = false, int? nthChild = null) where R : class, IItemDropRule
        {
            return loot.FindRuleWhere(query, includeGlobalDrops, null, nthChild) is not null;
        }

        /// <summary>
        /// Recursively loops through this <see cref="ILoot"/> instance and looks for the first <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that matches the given <paramref name="query"/>. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return it. If no match is found, it will recursively loop through the children of that child (both nested and chained children) and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return null.<br/><br/>
        ///
        /// If <paramref name="chainReplacer"/> function is supplied, the found rule's chain from its parent will be replaced with the chain that the function returns. The found rule will be the param of the function and it should return some type of 
        /// <see cref="IItemDropRuleChainAttempt"/> where ChainAttempt.RuleToChain is set to the parameter.<br/><br/>
        ///        
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary> 
        public static R FindRuleWhere<R>(this ILoot loot, LootPredicate<R> query = null, bool includeGlobalDrops = false, ChainReattcher chainReplacer = null, int? nthChild = null) where R : class, IItemDropRule
        {
            foreach (IItemDropRule rootRule in loot.Get(includeGlobalDrops))
            {
                if (RecursiveFindEntryPoint(rootRule, query, chainReplacer, 1, nthChild) is R result)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Recursively loops through this <see cref="IItemDropRule"/>'s children and sees if there is any child <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that matches the given <paramref name="query"/>. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return true. If no match is found, it will recursively loop through the children of that child (both nested and chained children) and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return false.<br/><br/>
        ///        
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary> 
        public static bool HasChildWhere<R>(this IItemDropRule root, LootPredicate<R> query = null, int? nthChild = null) where R : class, IItemDropRule
        {
            return root.FindChildWhere(query, null, nthChild) is not null;
        }

        /// <summary>
        /// Recursively loops through this <see cref="IItemDropRule"/>'s children and looks for the first child <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that matches the given <paramref name="query"/>. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return it. If no match is found, it will recursively loop through the children of that child (both nested and chained children) and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return null.<br/><br/>
        ///
        /// If <paramref name="chainReplacer"/> function is supplied, the found rule's chain from its parent will be replaced with the chain that the function returns. The found rule will be the param of the function and it should return some type of 
        /// <see cref="IItemDropRuleChainAttempt"/> where ChainAttempt.RuleToChain is set to the parameter.<br/><br/>
        ///        
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary> 
        public static R FindChildWhere<R>(this IItemDropRule root, LootPredicate<R> query = null, ChainReattcher chainReplacer = null, int? nthChild = null) where R : class, IItemDropRule
        {
            return RecursiveFindEntryPoint(root, query, chainReplacer, 0, nthChild);
        }

        /// <summary>
        /// Main entry point into RecursiveFindMain. RecursiveFindMain should only ever be called by this method or itself. This method marks the dictionary as in use and will clear
        /// it and unmark after (unless some other call earlier on the method call stack was using it first, in which case it will let that method unmark and clear it [this is so that you
        /// can use RecursiveFind within RecursiveRemoveWhere predicates without causing issues with the dictionary being modified during the predicates])
        /// </summary>
        private static R RecursiveFindEntryPoint<R>(IItemDropRule root, LootPredicate<R> query, ChainReattcher chainReplacer, int n, int? nthChild) where R : class, IItemDropRule
        {
            UseDictionary();
            query ??= (_) => true;
            R result = RecursiveFindMain(query, null, chainReplacer, root, n, nthChild);
            StopUsingDictionary();
            return result;
        }

        #endregion

        // The following methods are syntax sugar and apply the following 3 checks to your query automatically: rule => rule.HasParentRule() && rule.IsChained() && rule.ChainFromImmediateParent() is C
        #region Chained Rule Overloads

        /// <summary>
        /// Recursively loops through this <see cref="ILoot"/> instance and sees if there is any <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that is chained onto its parent via an <see cref="IItemDropRuleChainAttempt"/> of type <typeparamref name="C"/> that matches the given <paramref name="query"/>. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return true. If no match is found, it will recursively loop through the children of that child (both nested and chained children) and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return false.<br/><br/>
        ///        
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary> 
        public static bool HasRuleWhere<C, R>(this ILoot loot, LootPredicate<R> query = null, bool includeGlobalDrops = false, int? nthChild = null) where C : class, IItemDropRuleChainAttempt where R : class, IItemDropRule
        {
            return loot.FindRuleWhere<C, R>(query, includeGlobalDrops, null, nthChild) is not null;
        }

        /// <summary>
        /// Recursively loops through this <see cref="ILoot"/> instance and looks for the first <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that is chained onto its parent via an <see cref="IItemDropRuleChainAttempt"/> of type <typeparamref name="C"/> that matches the given <paramref name="query"/>. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return it. If no match is found, it will recursively loop through the children of that child (both nested and chained children) and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return null.<br/><br/>
        ///
        /// If <paramref name="chainReplacer"/> function is supplied, the found rule's chain from its parent will be replaced with the chain that the function returns. The found rule will be the param of the function and it should return some type of 
        /// <see cref="IItemDropRuleChainAttempt"/> where ChainAttempt.RuleToChain is set to the parameter.<br/><br/>
        ///        
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary> 
        public static R FindRuleWhere<C, R>(this ILoot loot, LootPredicate<R> query = null, bool includeGlobalDrops = false, ChainReattcher chainReplacer = null, int? nthChild = null) where C : class, IItemDropRuleChainAttempt where R : class, IItemDropRule
        {
            foreach (IItemDropRule rootRule in loot.Get(includeGlobalDrops))
            {
                if (RecursiveFindEntryPoint<C, R>(rootRule, query, chainReplacer, 1, nthChild) is R result)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Recursively loops through this <see cref="IItemDropRule"/>'s children and sees if there is any child <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that is chained onto its parent via an <see cref="IItemDropRuleChainAttempt"/> of type <typeparamref name="C"/> that matches the given <paramref name="query"/>. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return true. If no match is found, it will recursively loop through the children of that child (both nested and chained children) and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return false.<br/><br/>
        ///        
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary> 
        public static bool HasChildWhere<C, R>(this IItemDropRule root, LootPredicate<R> query = null, int? nthChild = null) where C : class, IItemDropRuleChainAttempt where R : class, IItemDropRule
        {
            return root.FindChildWhere<C, R>(query, null, nthChild) is not null;
        }

        /// <summary>
        /// Recursively loops through this <see cref="IItemDropRule"/>'s children and looks for the first child <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that is chained onto its parent via an <see cref="IItemDropRuleChainAttempt"/> of type <typeparamref name="C"/> that matches the given <paramref name="query"/>. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return it. If no match is found, it will recursively loop through the children of that child (both nested and chained children) and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return null.<br/><br/>
        ///
        /// If <paramref name="chainReplacer"/> function is supplied, the found rule's chain from its parent will be replaced with the chain that the function returns. The found rule will be the param of the function and it should return some type of 
        /// <see cref="IItemDropRuleChainAttempt"/> where ChainAttempt.RuleToChain is set to the parameter.<br/><br/>
        ///        
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary> 
        public static R FindChildWhere<C, R>(this IItemDropRule root, LootPredicate<R> query = null, ChainReattcher chainReplacer = null, int? nthChild = null) where C : class, IItemDropRuleChainAttempt where R : class, IItemDropRule
        {
            return RecursiveFindEntryPoint<C, R>(root, query, chainReplacer, 0, nthChild);
        }

        /// <summary>
        /// Main entry point into RecursiveFindMain. RecursiveFindMain should only ever be called by this method or itself. This method marks the dictionary as in use and will clear
        /// it and unmark after (unless some other call earlier on the method call stack was using it first, in which case it will let that method unmark and clear it [this is so that you
        /// can use RecursiveFind within RecursiveRemoveWhere predicates without causing issues with the dictionary being modified during the predicates])
        /// </summary>
        public static R RecursiveFindEntryPoint<C, R>(IItemDropRule root, LootPredicate<R> query, ChainReattcher chainReplacer, int n, int? nthChild) where C : class, IItemDropRuleChainAttempt where R : class, IItemDropRule
        {
            UseDictionary();
            query ??= (_) => true;
            R result = RecursiveFindMain<R>(rule => rule.HasParentRule() && rule.IsChained() && rule.ChainFromImmediateParent() is C && query(rule), null, chainReplacer, root, n, nthChild);
            StopUsingDictionary();
            return result;
        }

        #endregion

        // The following methods are syntax sugar and apply the following 3 checks to your query automatically: rule => rule.HasParentRule() && rule.IsNested() && rule.ImmediateParent() is N
        #region Nested Rule Overloads

        /// <summary>
        /// Recursively loops through this <see cref="ILoot"/> instance and sees if there is any <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that is directly nested inside an <see cref="INestedItemDropRule"/> of type <typeparamref name="N"/> that matches the given <paramref name="query"/>. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return true. If no match is found, it will recursively loop through the children of that child (both nested and chained children) and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return false.<br/><br/>
        ///        
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary> 
        public static bool HasRuleWhere<N, R>(this ILoot loot, LootPredicate<R> query = null, bool includeGlobalDrops = false, int? nthChild = null, int _ = 0) where N : class, IItemDropRule, INestedItemDropRule where R : class, IItemDropRule
        {
            return loot.FindRuleWhere<N, R>(query, includeGlobalDrops, nthChild) is not null;
        }

        /// <summary>
        /// Recursively loops through this <see cref="ILoot"/> instance and sees if there is any <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that is directly nested inside an <see cref="INestedItemDropRule"/> of type <typeparamref name="N"/> that matches the given <paramref name="query"/>. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return it. If no match is found, it will recursively loop through the children of that child (both nested and chained children) and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return null.<br/><br/>
        ///        
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary> 
        public static R FindRuleWhere<N, R>(this ILoot loot, LootPredicate<R> query = null, bool includeGlobalDrops = false, int? nthChild = null) where N : class, IItemDropRule, INestedItemDropRule where R : class, IItemDropRule
        {
            foreach (IItemDropRule rootRule in loot.Get(includeGlobalDrops))
            {
                if (RecursiveFindEntryPoint<N, R>(rootRule, query, 1, nthChild) is R result)
                {
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Recursively loops through this <see cref="IItemDropRule"/>'s children and sees if there is any child <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that is directly nested inside an <see cref="INestedItemDropRule"/> of type <typeparamref name="N"/> that matches the given <paramref name="query"/>. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return true. If no match is found, it will recursively loop through the children of that child (both nested and chained children) and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return false.<br/><br/>
        ///        
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary> 
        public static bool HasChildWhere<N, R>(this IItemDropRule root, LootPredicate<R> query = null, int? nthChild = null, int _ = 0) where N : class, IItemDropRule, INestedItemDropRule where R : class, IItemDropRule
        {
            return root.FindChildWhere<N, R>(query, nthChild) is not null;
        }

        /// <summary>
        /// Recursively loops through this <see cref="IItemDropRule"/>'s children and sees if there is any child <see cref="IItemDropRule"/> of type <typeparamref name="R"/> that is directly nested inside an <see cref="INestedItemDropRule"/> of type <typeparamref name="N"/> that matches the given <paramref name="query"/>. If <paramref name="nthChild"/> is specified, then the rule must be the nth child of the loot pool.
        /// If a match is found it will return it. If no match is found, it will recursively loop through the children of that child (both nested and chained children) and try to find one, so on and so forth.
        /// If nothing was found after searching all children recursively, it will return null.<br/><br/>
        ///        
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary> 
        public static R FindChildWhere<N, R>(this IItemDropRule root, LootPredicate<R> query = null, int? nthChild = null) where N : class, IItemDropRule, INestedItemDropRule where R : class, IItemDropRule
        {
            return RecursiveFindEntryPoint<N, R>(root, query, 0, nthChild);
        }

        /// <summary>
        /// Main entry point into RecursiveFindMain. RecursiveFindMain should only ever be called by this method or itself. This method marks the dictionary as in use and will clear
        /// it and unmark after (unless some other call earlier on the method call stack was using it first, in which case it will let that method unmark and clear it [this is so that you
        /// can use RecursiveFind within RecursiveRemoveWhere predicates without causing issues with the dictionary being modified during the predicates])
        /// </summary>
        private static R RecursiveFindEntryPoint<N, R>(IItemDropRule root, LootPredicate<R> query, int n, int? nthChild) where N : class, IItemDropRule, INestedItemDropRule where R : class, IItemDropRule
        {
            UseDictionary();
            query ??= (_) => true;
            R result = RecursiveFindMain<R>(rule => rule.HasParentRule() && rule.IsNested() && rule.ImmediateParent() is N && query(rule), null, null, root, n, nthChild);
            StopUsingDictionary();
            return result;
        }

        #endregion

        #region Main Recursive Find Method

        /// <summary>
        /// Checks if currRule is of type <typeparamref name="R"/> and matches the given <paramref name="query"/>. If it does it will be returned. If not, it will recursively call this method
        /// again on the children of currRule. If nthChild is specified, then the currRule must also be the nth child of whatever ILoot or IItemDropRule that this
        /// recursive call originated with. If nothing was found after searching all children recursively, it will return null.<br/><br/>
        ///
        /// If <paramref name="chainReplacer"/> function is supplied, the found rule's chain from its parent will be replaced with the chain that the function returns. The found rule will be the param of the function and it should return some type of 
        /// <see cref="IItemDropRuleChainAttempt"/> where ChainAttempt.RuleToChain is set to the parameter.<br/><br/>
        ///        
        /// Note: A rule (a) is considered the chained parent to another rule (b) if (b) is an <see cref="IItemDropRuleChainAttempt"/>.RuleToChain in (a)'s <see cref="IItemDropRule.ChainedRules"/> array<br/>
        /// Note: A rule (a) is considered the nested parent to another rule (b) if (a) is an <see cref="INestedItemDropRule"/> that executes (b). Examples of <see cref="INestedItemDropRule"/> include (not limited to) <see cref="OneFromRulesRule"/>, <see cref="DropBasedOnExpertMode"/>, <see cref="SequentialRulesRule"/>        
        /// </summary> 
        private static R RecursiveFindMain<R>(LootPredicate<R> query, int? indexOfChainToCurrRule, ChainReattcher chainReplacer, IItemDropRule currRule, int n, int? nthChild) where R : class, IItemDropRule
        {
            // n == 0 means this RecursiveFindMain call should not check currRule at all. This is used when an EntryPoint is first called on an IItemDropRule (not an ILoot) so that the rule
            // itself isn't queried and returned if it happens to match the <paramref name="query"/> (because we only want to query its children). n will be initially set to 1 when called on ILoot entry, 0 when called on IItemDropRule entry

            // n must not be 0, currRule must be of type T and match the <paramref name="query"/>. Also if nthChild is specified, n must be == nthChild
            if (n != 0 && currRule is R castedRule && query(castedRule) && (nthChild is null || nthChild == n))
            {
                if (chainReplacer is not null && indexOfChainToCurrRule is int i) // indexOfChainToCurrRule not null <==> parent rule is not null, and vice versa if any are null
                {
                    currRule.ImmediateParent().ChainedRules[i] = chainReplacer(currRule);
                }
                return castedRule;
            }
            else
            {
                if (nthChild is not null && (n + 1) > nthChild) // Stop trying to search if they specified that it must be nth child and we are gonna be further than that specified n next iteration
                {
                    return null;
                }

                // Try nested rules first
                if (currRule is INestedItemDropRule ruleThatExecutesOthers)
                {
                    foreach (IItemDropRule nestedRule in GetRulesNestedInsideThisRule(ruleThatExecutesOthers))
                    {
                        nestedRule.RegisterAsNestedChild(ruleThatExecutesOthers);
                        if (RecursiveFindMain(query, null, chainReplacer, nestedRule, n + 1, nthChild) is R result)
                        {
                            return result;
                        }
                    }
                }

                // Then try chained rules
                for (int i = 0; i < currRule.ChainedRules.Count; i++)
                {
                    IItemDropRuleChainAttempt chainAttempt = currRule.ChainedRules[i];
                    IItemDropRule child = chainAttempt.RuleToChain;
                    child.RegisterAsChainedChild(currRule, chainAttempt);

                    if (RecursiveFindMain(query, i, chainReplacer, child, n + 1, nthChild) is R result)
                    {
                        return result;
                    }
                }

                return null;
            }
        }

        #endregion

        #endregion

        // These methods can only be called inside predicates
        #region Recursive Parent Finding

        // Special recursive methods to be used inside LootPredicates within Child Finding / Child Removing methods. Used to help narrow down search

        /// <summary>
        /// Recursively looks at <see cref="IItemDropRule"/>'s parent <see cref="IItemDropRule"/> and sees if it is of type <typeparamref name="R"/>, and if it matches the given <paramref name="query"/>. If <paramref name="nthParent"/> is specified, then the rule must be the nth parent of the rule this was originally called on.
        /// If a match is found it will return true. If no match is found, it will recursively look at the parent of the last queried parent and do the same check, so on and so forth.
        /// If nothing was found after searching all parents recursively, it will return false.<br/><br/>
        /// 
        /// Can only be used in the context of RemoveWhere/FindWhere/Has recursive predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying
        /// </summary>
        public static bool HasParentRuleWhere<R>(this IItemDropRule rule, LootPredicate<R> pred, int? nthParent = null) where R : class, IItemDropRule
        {
            return rule.FindParentRuleWhere(pred, nthParent) is not null;
        }

        /// <summary>
        /// Recursively looks at <see cref="IItemDropRule"/>'s parent <see cref="IItemDropRule"/> and sees if it is of type <typeparamref name="R"/>, and if it matches the given <paramref name="query"/>. If <paramref name="nthParent"/> is specified, then the rule must be the nth parent of the rule this was originally called on.
        /// If a match is found it will return it. If no match is found, it will recursively look at the parent of the last queried parent and do the same check, so on and so forth.
        /// If nothing was found after searching all parents recursively, it will return null.<br/><br/>
        /// 
        /// Can only be used in the context of RemoveWhere/FindWhere/Has recursive predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying        
        /// </summary>
        public static R FindParentRuleWhere<R>(this IItemDropRule rule, LootPredicate<R> pred, int? nthParent = null) where R : class, IItemDropRule
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
            return null;
        }

        /// <summary>
        /// Recursively looks at <see cref="IItemDropRule"/>'s parent <see cref="IItemDropRule"/> and sees if it matches the given <paramref name="query"/>. If <paramref name="nthParent"/> is specified, then the rule must be the nth parent of the rule this was originally called on.
        /// If a match is found it will return true. If no match is found, it will recursively look at the parent of the last queried parent and do the same check, so on and so forth.
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
        /// Recursively looks at <see cref="IItemDropRule"/>'s parent <see cref="IItemDropRule"/> and sees if it matches the given <paramref name="query"/>. If <paramref name="nthParent"/> is specified, then the rule must be the nth parent of the rule this was originally called on.
        /// If a match is found it will return it. If no match is found, it will recursively look at the parent of the last queried parent and do the same check, so on and so forth.
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
        /// Recursively looks at <see cref="IItemDropRule"/>'s parent <see cref="IItemDropRule"/> and sees if it is of type <typeparamref name="R"/>, is chained onto its parent via an <see cref="IItemDropRuleChainAttempt"/> of type <typeparamref name="C"/>, and if it matches the given <paramref name="query"/>. If <paramref name="nthParent"/> is specified, then the rule must be the nth parent of the rule this was originally called on.
        /// If a match is found it will return true. If no match is found, it will recursively look at the parent of the last queried parent and do the same check, so on and so forth.
        /// If nothing was found after searching all parents recursively, it will return false.<br/><br/>
        /// </summary>
        /// 
        /// Can only be used in the context of RemoveWhere/FindWhere/Has recursive predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying
        public static bool HasParentRuleWhere<C, R>(this IItemDropRule rule, LootPredicate<R> pred, int? nthParent = null) where C : class, IItemDropRuleChainAttempt where R : class, IItemDropRule
        {
            return rule.FindParentRuleWhere<C, R>(pred, nthParent) is not null;
        }

        /// <summary>
        /// Recursively looks at <see cref="IItemDropRule"/>'s parent <see cref="IItemDropRule"/> and sees if it is of type <typeparamref name="R"/>, is chained onto its parent via an <see cref="IItemDropRuleChainAttempt"/> of type <typeparamref name="C"/>, and if it matches the given <paramref name="query"/>. If <paramref name="nthParent"/> is specified, then the rule must be the nth parent of the rule this was originally called on.
        /// If a match is found it will return it. If no match is found, it will recursively look at the parent of the last queried parent and do the same check, so on and so forth.
        /// If nothing was found after searching all parents recursively, it will return null.<br/><br/>
        /// 
        /// Can only be used in the context of RemoveWhere/FindWhere/Has recursive predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying        
        /// </summary>
        public static R FindParentRuleWhere<C, R>(this IItemDropRule rule, LootPredicate<R> pred, int? nthParent = null) where C : class, IItemDropRuleChainAttempt where R : class, IItemDropRule
        {
            return rule.FindParentRuleWhere<R>(rule => rule.HasParentRule() && rule.IsChained() && rule.ChainFromImmediateParent() is C && pred(rule), nthParent);
        }

        /// <summary>
        /// Recursively looks at <see cref="IItemDropRule"/>'s parent <see cref="IItemDropRule"/> and sees if it is of type <typeparamref name="R"/>, is chained onto its parent via an <see cref="IItemDropRuleChainAttempt"/> of type <typeparamref name="N"/>, and if it matches the given <paramref name="query"/>. If <paramref name="nthParent"/> is specified, then the rule must be the nth parent of the rule this was originally called on.
        /// If a match is found it will return true. If no match is found, it will recursively look at the parent of the last queried parent and do the same check, so on and so forth.
        /// If nothing was found after searching all parents recursively, it will return false.<br/><br/>
        /// </summary>
        /// 
        /// Can only be used in the context of RemoveWhere/FindWhere/Has recursive predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying
        public static bool HasParentRuleWhere<N, R>(this IItemDropRule rule, LootPredicate<R> pred, int? nthParent = null, int _ = 0) where N : class, IItemDropRule, INestedItemDropRule where R : class, IItemDropRule
        {
            return rule.FindParentRuleWhere<N, R>(pred, nthParent) is not null;
        }

        /// <summary>
        /// Recursively looks at <see cref="IItemDropRule"/>'s parent <see cref="IItemDropRule"/> and sees if it is of type <typeparamref name="R"/>, is directly nested inside an <see cref="INestedItemDropRule"/> of type <typeparamref name="N"/>, and if it matches the given <paramref name="query"/>. If <paramref name="nthParent"/> is specified, then the rule must be the nth parent of the rule this was originally called on.
        /// If a match is found it will return it. If no match is found, it will recursively look at the parent of the last queried parent and do the same check, so on and so forth.
        /// If nothing was found after searching all parents recursively, it will return null.<br/><br/>
        /// 
        /// Can only be used in the context of RemoveWhere/FindWhere/Has recursive predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying        
        /// </summary>
        public static R FindParentRuleWhere<N, R>(this IItemDropRule rule, LootPredicate<R> pred, int? nthParent = null, int _ = 0) where N : class, IItemDropRule, INestedItemDropRule where R : class, IItemDropRule
        {
            return rule.FindParentRuleWhere<R>(rule => rule.HasParentRule() && rule.IsNested() && rule.ImmediateParent() is N && pred(rule), nthParent);
        }

        #endregion

        #region Data Structures and Helpers

        // DATA STRUCTURES USED WITHIN RECURSIVE CALLS FOR PARENT TRACKING

        /// <summary>
        /// Used to temporarily store data about which IItemDropRule is the "parent" of another IItemDropRule, as well as storing the ChainAttempt 
        /// An IItemDropRule is considered a "child" to another IItemDropRule if the parent's ChainedRules array contains an IItemDropRuleChainAttempt
        /// where the chainAttempt.RuleToChain references the child.
        /// </summary>
        public struct ParentChildRelationship
        {
            // These two will be set if the child is chained to its parent (NestedParent will be null)
            public IItemDropRule ChainedParent { get; set; }
            public IItemDropRuleChainAttempt ChainAttempt { get; set; }

            // Or they will be null and NestedParent won't be 
            public IItemDropRule NestedParent { get; set; }

        }

        /// <summary>
        /// Used to temporarily store data about which IItemDropRule is the "parent" of another IItemDropRule. Keys in this dictionary
        /// represent children, and you can get their parent as well as the chain that leads to the child. This dictionary is built up
        /// on-the-fly during RecursiveRemoveMain and RecursiveFindMain, and is cleared after recursive methods are done using it, so it will only be
        /// accurate during recursion. This means that IItemDropRule.ParentRule(n) extension will only be valid during recursion and should only be used in
        /// predicates plugged into my recursive functions <br/>
        /// See <see cref="ParentRule(IItemDropRule, int)"/>, as well as <see cref="DictionaryPopulated"/>
        /// </summary> 
        private readonly static Dictionary<IItemDropRule, ParentChildRelationship> ParentDictionary = new();

        /// <summary>
        /// Used to determine if the dictionary is being used, and to control whether or not the dictionary should be cleared 
        /// (if a recursive function earlier on the stack used it first, then later calls that use the dictionary won't be responsible for
        /// clearing it and it will be left up to the earlier function). See
        /// <see cref="RecursiveFindEntryPoint{R}(IItemDropRule, LootPredicate{R}, int, int?)"/> and 
        /// <see cref="RecursiveRemoveEntryPoint{R}(ILoot, LootPredicate{R}, IItemDropRule, bool, ChainReattcher, bool, int, int?)"/>
        /// to see this behavior. It is also used in 
        /// <see cref="ParentRule(IItemDropRule, int)"/> to make sure it is being called in the correct context (Dictionary MUST bee in use by a 
        /// recursive method to be able to use ParentRule(n). This basically means that ParentRule(n) can only be used within Predicates of my extension methods)
        /// </summary>
        private static readonly Stack<object> DictionaryUseStack = new();

        private static bool DictionaryPopulated => DictionaryUseStack.Any();

        private static void UseDictionary()
        {
            DictionaryUseStack.Push(null);
        }

        private static void StopUsingDictionary()
        {
            DictionaryUseStack.Pop(); // If exception is thrown here it means StopUsingDictionary() was called without calling UseDictionary() (my coding error)
            if (!DictionaryUseStack.Any())
            {
                ParentDictionary.Clear();
            }
        }

        private static void RegisterAsChainedChild(this IItemDropRule rule, IItemDropRule parent, IItemDropRuleChainAttempt chainAttempt)
        {
            ParentDictionary[rule] = new ParentChildRelationship { ChainedParent = parent, ChainAttempt = chainAttempt };
        }

        private static void RegisterAsNestedChild(this IItemDropRule rule, INestedItemDropRule parent)
        {
            ParentDictionary[rule] = new ParentChildRelationship { NestedParent = (IItemDropRule)parent };
        }

        /// <summary>
        /// Finds the immediate parent of this rule. This is equivalent to <see cref="ParentRule(IItemDropRule, int)"/> where nthChild is 1. <br/><br/>
        /// 
        /// Can only be used in the context of RemoveWhere/FindWhere/Has recursive predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying<br/><br/>
        /// 
        /// Returns null if no parent is found
        /// </summary>
        public static IItemDropRule ImmediateParent(this IItemDropRule rule)
        {
            if (!DictionaryPopulated)
            {
                throw new Exception("IItemDropRule parent referencing can only be done in the context of predicates within the Find, Has, and Remove extensions (and their generic overloads)");
            }

            return ParentDictionary.TryGetValue(rule, out ParentChildRelationship parentWithChain) ? (parentWithChain.ChainedParent ?? parentWithChain.NestedParent) : null;
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
            {
                throw new Exception("nth parent must be greater than 0");
            }

            for (int i = 0; i < nthParent; i++)
            {
                rule = rule.ImmediateParent();

                if (rule is null)
                {
                    return null;
                }
            }

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

        /// <summary>
        /// Determines if this rule is Chained onto its parent via an IItemDropRuleChainAttempt. A false return means this rule is nested inside of an INestedItemDropRule.<br/><br/>
        /// 
        /// Can only be used in the context of RemoveWhere/FindWhere/Has recursive predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying<br/><br/>
        /// Throws <see cref="NullReferenceException"></see> if the rule has no parent. This is to ensure that a false return means the rule is Nested
        /// </summary>
        public static bool IsChained(this IItemDropRule rule)
        {
            if (rule.ImmediateParent() is null)
            {
                throw new NullReferenceException("Cannot check if the current rule is chained/nested to its parent as it has no parent. Make sure rule.HasParentRule() returns true");
            }

            return ParentDictionary[rule].ChainAttempt is not null;
        }

        public static IItemDropRuleChainAttempt ChainFromImmediateParent(this IItemDropRule rule)
        {
            if (rule.ImmediateParent() is null)
            {
                throw new NullReferenceException("Cannot get chain from immediate parent as there is no parent. Make sure rule.HasParentRule() returns true");
            }

            if (!rule.IsChained())
            {
                throw new Exception("Cannot get the chain from the immediate parent of this rule as this rule is not chained to its parent (it is nested inside its parent)");
            }

            return ParentDictionary[rule].ChainAttempt;
        }

        /// <summary>
        /// Determines if this rule is Nested inside an INestedItemDropRule. A false return means this rule is chained to its parent via an IItemDropRuleChain attempt, as opposed to being nested inside a parent.<br/><br/>
        /// 
        /// Can only be used in the context of RemoveWhere/FindWhere/Has recursive predicates, as it uses a Dictionary to find the parent, and the dictionary is only populated
        /// (accurate) during these calls and is cleared after. Mainly used inside Predicates of Remove/Find as a means of being more exact about what
        /// rule we are querying<br/><br/>
        /// Throws <see cref="NullReferenceException"></see> if the rule has no parent. This is to ensure that a false return means the rule is Chained as opposed to nested
        /// </summary>
        public static bool IsNested(this IItemDropRule rule)
        {
            return !rule.IsChained();
        }

        #endregion

        #region Other ILoot Extensions

        /// <summary>Syntax sugar to clear this ILoot. What it does is calls RemoveWhere(_ => true) </summary>
        public static void Clear(this ILoot loot)
        {
            loot.RemoveWhere(_ => true);
        }

        #endregion

        #region Other IItemDropRule Extensions

        public static IItemDropRule MultipleOnSuccess(this IItemDropRule parent, bool hideLootReport, params IItemDropRule[] rulesToExecute)
        {
            foreach (IItemDropRule rule in rulesToExecute)
            {
                rule.OnSuccess(rule, hideLootReport);
            }
            return parent;
        }

        public static IItemDropRule MultipleOnFailure(this IItemDropRule parent, bool hideLootReport, params IItemDropRule[] rulesToExecute)
        {
            foreach (IItemDropRule rule in rulesToExecute)
            {
                rule.OnFailedRoll(rule, hideLootReport);
            }
            return parent;
        }

        public static IItemDropRule MultipleOnFailedConditions(this IItemDropRule parent, bool hideLootReport, params IItemDropRule[] rulesToExecute)
        {
            foreach (IItemDropRule rule in rulesToExecute)
            {
                rule.OnFailedRoll(rule, hideLootReport);
            }
            return parent;
        }

        #endregion

        #region OneFromOptionsDropRule Extensions

        public static void AddOption(this OneFromOptionsDropRule rule, int option)
        {
            List<int> asList = rule.dropIds.ToList();
            asList.Add(option);
            rule.dropIds = asList.ToArray();
        }

        public static bool RemoveOption(this OneFromOptionsDropRule rule, int removing)
        {
            return rule.RemoveMultipleOptions(removing);
        }

        public static bool RemoveMultipleOptions(this OneFromOptionsDropRule rule, params int[] removing)
        {
            return rule.FilterOptions(option => !removing.Contains(option));
        }

        public static bool ContainsOption(this OneFromOptionsDropRule rule, int option)
        {
            return rule.dropIds.Contains(option);
        }

        public static bool FilterOptions(this OneFromOptionsDropRule rule, Predicate<int> query)
        {
            bool anyFiltered = false;
            List<int> newDropIds = new();
            foreach (int dropId in rule.dropIds)
            {
                if (query(dropId))
                {
                    newDropIds.Add(dropId);
                }
                else
                {
                    anyFiltered = true;
                }
            }
            rule.dropIds = newDropIds.ToArray();
            return anyFiltered;
        }


        #endregion

        #region OneFromOptionsNotScaledWithLuckDropRule Extensions

        public static void AddOption(this OneFromOptionsNotScaledWithLuckDropRule rule, int option)
        {
            List<int> asList = rule.dropIds.ToList();
            asList.Add(option);
            rule.dropIds = asList.ToArray();
        }

        public static bool RemoveOption(this OneFromOptionsNotScaledWithLuckDropRule rule, int removing)
        {
            return rule.RemoveMultipleOptions(removing);
        }

        public static bool RemoveMultipleOptions(this OneFromOptionsNotScaledWithLuckDropRule rule, params int[] removing)
        {
            return rule.FilterOptions(option => !removing.Contains(option));
        }

        public static bool ContainsOption(this OneFromOptionsNotScaledWithLuckDropRule rule, int option)
        {
            return rule.dropIds.Contains(option);
        }

        public static bool FilterOptions(this OneFromOptionsNotScaledWithLuckDropRule rule, Predicate<int> query)
        {
            bool anyFiltered = false;
            List<int> newDropIds = new();
            foreach (int dropId in rule.dropIds)
            {
                if (query(dropId))
                {
                    newDropIds.Add(dropId);
                }
                else
                {
                    anyFiltered = true;
                }
            }
            rule.dropIds = newDropIds.ToArray();
            return anyFiltered;
        }

        #endregion

        #region FewFromOptionsDropRule Extensions

        public static void AddOption(this FewFromOptionsDropRule rule, int option)
        {
            List<int> asList = rule.dropIds.ToList();
            asList.Add(option);
            rule.dropIds = asList.ToArray();
        }

        public static bool RemoveOption(this FewFromOptionsDropRule rule, int removing)
        {
            return rule.RemoveMultipleOptions(removing);
        }

        public static bool RemoveMultipleOptions(this FewFromOptionsDropRule rule, params int[] removing)
        {
            return rule.FilterOptions(option => !removing.Contains(option));
        }

        public static bool ContainsOption(this FewFromOptionsDropRule rule, int option)
        {
            return rule.dropIds.Contains(option);
        }

        public static bool FilterOptions(this FewFromOptionsDropRule rule, Predicate<int> query)
        {
            bool anyFiltered = false;
            List<int> newDropIds = new();
            foreach (int dropId in rule.dropIds)
            {
                if (query(dropId))
                {
                    newDropIds.Add(dropId);
                }
                else
                {
                    anyFiltered = true;
                }
            }
            rule.dropIds = newDropIds.ToArray();
            return anyFiltered;
        }

        #endregion

        #region FewFromOptionsNotScaledWithLuckDropRule Extensions

        public static void AddOption(this FewFromOptionsNotScaledWithLuckDropRule rule, int option)
        {
            List<int> asList = rule.dropIds.ToList();
            asList.Add(option);
            rule.dropIds = asList.ToArray();
        }

        public static bool RemoveOption(this FewFromOptionsNotScaledWithLuckDropRule rule, int removing)
        {
            return rule.RemoveMultipleOptions(removing);
        }

        public static bool RemoveMultipleOptions(this FewFromOptionsNotScaledWithLuckDropRule rule, params int[] removing)
        {
            return rule.FilterOptions(option => !removing.Contains(option));
        }

        public static bool ContainsOption(this FewFromOptionsNotScaledWithLuckDropRule rule, int option)
        {
            return rule.dropIds.Contains(option);
        }

        public static bool FilterOptions(this FewFromOptionsNotScaledWithLuckDropRule rule, Predicate<int> query)
        {
            bool anyFiltered = false;
            List<int> newDropIds = new();
            foreach (int dropId in rule.dropIds)
            {
                if (query(dropId))
                {
                    newDropIds.Add(dropId);

                }
                else
                {
                    anyFiltered = true;
                }
            }
            rule.dropIds = newDropIds.ToArray();
            return anyFiltered;
        }

        #endregion

        #region OneFromRulesRule Extensions

        public static void AddOption(this OneFromRulesRule oneFromRulesRule, IItemDropRule option)
        {
            List<IItemDropRule> asList = oneFromRulesRule.options.ToList();
            asList.Add(option);
            oneFromRulesRule.options = asList.ToArray();
        }

        public static bool RemoveOption(this OneFromRulesRule oneFromRulesRule, IItemDropRule removing)
        {
            return oneFromRulesRule.RemoveMultipleOptions(removing);
        }

        public static bool RemoveMultipleOptions(this OneFromRulesRule oneFromRulesRule, params IItemDropRule[] removing)
        {
            return oneFromRulesRule.FilterOptions(option => !removing.Contains(option));
        }

        public static bool RemoveOption(this OneFromRulesRule oneFromRulesRule, Predicate<IItemDropRule> query)
        {
            return oneFromRulesRule.FilterOptions(rule => !query(rule));
        }

        public static bool ContainsOption(this OneFromRulesRule oneFromRulesRule, IItemDropRule ruleOption)
        {
            return oneFromRulesRule.options.Contains(ruleOption);
        }

        public static bool FilterOptions(this OneFromRulesRule oneFromRulesRule, Predicate<IItemDropRule> query)
        {
            bool anyFiltered = false;
            List<IItemDropRule> newOptions = new();
            foreach (IItemDropRule rule in oneFromRulesRule.options)
            {
                if (query(rule))
                {
                    newOptions.Add(rule);
                }
                else
                {
                    anyFiltered = true;
                }
            }
            oneFromRulesRule.options = newOptions.ToArray();
            return anyFiltered;
        }

        #endregion

        #region FewFromRulesRule Extensions

        public static void AddOption(this FewFromRulesRule oneFromRulesRule, IItemDropRule option)
        {
            List<IItemDropRule> asList = oneFromRulesRule.options.ToList();
            asList.Add(option);
            oneFromRulesRule.options = asList.ToArray();
        }

        public static bool RemoveOption(this FewFromRulesRule oneFromRulesRule, IItemDropRule removing)
        {
            return oneFromRulesRule.RemoveMultipleOptions(removing);
        }

        public static bool RemoveMultipleOptions(this FewFromRulesRule oneFromRulesRule, params IItemDropRule[] removing)
        {
            return oneFromRulesRule.FilterOptions(option => !removing.Contains(option));
        }

        public static bool RemoveOption(this FewFromRulesRule oneFromRulesRule, Predicate<IItemDropRule> query)
        {
            return oneFromRulesRule.FilterOptions(rule => !query(rule));
        }

        public static bool ContainsOption(this FewFromRulesRule oneFromRulesRule, IItemDropRule ruleOption)
        {
            return oneFromRulesRule.options.Contains(ruleOption);
        }

        public static bool FilterOptions(this FewFromRulesRule oneFromRulesRule, Predicate<IItemDropRule> query)
        {
            bool anyFiltered = false;
            List<IItemDropRule> newOptions = new();
            foreach (IItemDropRule rule in oneFromRulesRule.options)
            {
                if (query(rule))
                {
                    newOptions.Add(rule);
                }
                else
                {
                    anyFiltered = true;
                }
            }
            oneFromRulesRule.options = newOptions.ToArray();
            return anyFiltered;
        }

        #endregion

        #region SequentialRulesRule Extensions

        public static void AddOption(this SequentialRulesRule sequentialRulesRule, IItemDropRule option)
        {
            List<IItemDropRule> asList = sequentialRulesRule.rules.ToList();
            asList.Add(option);
            sequentialRulesRule.rules = asList.ToArray();
        }

        public static bool RemoveOption(this SequentialRulesRule sequentialRulesRule, IItemDropRule removing)
        {
            return sequentialRulesRule.RemoveMultipleOptions(removing);
        }

        public static bool RemoveMultipleOptions(this SequentialRulesRule sequentialRulesRule, params IItemDropRule[] removing)
        {
            return sequentialRulesRule.FilterOptions(option => !removing.Contains(option));
        }

        public static bool RemoveOption(this SequentialRulesRule sequentialRulesRule, Predicate<IItemDropRule> query)
        {
            return sequentialRulesRule.FilterOptions(rule => !query(rule));
        }

        public static bool ContainsOption(this SequentialRulesRule sequentialRulesRule, IItemDropRule ruleOption)
        {
            return sequentialRulesRule.rules.Contains(ruleOption);
        }

        public static bool FilterOptions(this SequentialRulesRule sequentialRulesRule, Predicate<IItemDropRule> query)
        {
            bool anyFiltered = false;
            List<IItemDropRule> newRules = new();
            foreach (IItemDropRule rule in sequentialRulesRule.rules)
            {
                if (query(rule))
                {
                    newRules.Add(rule);
                }
                else
                {
                    anyFiltered = true;
                }
            }
            sequentialRulesRule.rules = newRules.ToArray();
            return anyFiltered;
        }

        #endregion

        #region SequentialRulesNotScalingWithLuckRule Extensions

        public static void AddOption(this SequentialRulesNotScalingWithLuckRule sequentialRulesRule, IItemDropRule option)
        {
            List<IItemDropRule> asList = sequentialRulesRule.rules.ToList();
            asList.Add(option);
            sequentialRulesRule.rules = asList.ToArray();
        }

        public static bool RemoveOption(this SequentialRulesNotScalingWithLuckRule sequentialRulesRule, IItemDropRule removing)
        {
            return sequentialRulesRule.RemoveMultipleOptions(removing);
        }

        public static bool RemoveMultipleOptions(this SequentialRulesNotScalingWithLuckRule sequentialRulesRule, params IItemDropRule[] removing)
        {
            return sequentialRulesRule.FilterOptions(option => !removing.Contains(option));
        }

        public static bool RemoveOption(this SequentialRulesNotScalingWithLuckRule sequentialRulesRule, Predicate<IItemDropRule> query)
        {
            return sequentialRulesRule.FilterOptions(rule => !query(rule));
        }

        public static bool ContainsOption(this SequentialRulesNotScalingWithLuckRule sequentialRulesRule, IItemDropRule ruleOption)
        {
            return sequentialRulesRule.rules.Contains(ruleOption);
        }

        public static bool FilterOptions(this SequentialRulesNotScalingWithLuckRule sequentialRulesRule, Predicate<IItemDropRule> query)
        {
            bool anyFiltered = false;
            List<IItemDropRule> newRules = new();
            foreach (IItemDropRule rule in sequentialRulesRule.rules)
            {
                if (query(rule))
                {
                    newRules.Add(rule);
                }
                else
                {
                    anyFiltered = true;
                }
            }
            sequentialRulesRule.rules = newRules.ToArray();
            return anyFiltered;
        }

        #endregion

        #region AlwaysAtLeastOneSuccessDropRule Extensions

        public static void AddOption(this AlwaysAtleastOneSuccessDropRule alwaysOneSuccessRule, IItemDropRule option)
        {
            List<IItemDropRule> asList = alwaysOneSuccessRule.rules.ToList();
            asList.Add(option);
            alwaysOneSuccessRule.rules = asList.ToArray();
        }

        public static bool RemoveOption(this AlwaysAtleastOneSuccessDropRule alwaysOneSuccessRule, IItemDropRule removing)
        {
            return alwaysOneSuccessRule.RemoveMultipleOptions(removing);
        }

        public static bool RemoveMultipleOptions(this AlwaysAtleastOneSuccessDropRule alwaysOneSuccessRule, params IItemDropRule[] removing)
        {
            return alwaysOneSuccessRule.FilterOptions(option => !removing.Contains(option));
        }

        public static bool RemoveOption(this AlwaysAtleastOneSuccessDropRule alwaysOneSuccessRule, Predicate<IItemDropRule> query)
        {
            return alwaysOneSuccessRule.FilterOptions(rule => !query(rule));
        }

        public static bool ContainsOption(this AlwaysAtleastOneSuccessDropRule alwaysOneSuccessRule, IItemDropRule ruleOption)
        {
            return alwaysOneSuccessRule.rules.Contains(ruleOption);
        }

        public static bool FilterOptions(this AlwaysAtleastOneSuccessDropRule alwaysOneSuccessRule, Predicate<IItemDropRule> query)
        {
            bool anyFiltered = false;
            List<IItemDropRule> newRules = new();
            foreach (IItemDropRule rule in alwaysOneSuccessRule.rules)
            {
                if (query(rule))
                {
                    newRules.Add(rule);
                }
                else
                {
                    anyFiltered = true;
                }
            }
            alwaysOneSuccessRule.rules = newRules.ToArray();
            return anyFiltered;
        }

        #endregion
    }
}