# LootExtensions

## Understanding ILoot and how LootExtensions helps modify it
The main use case for LootExtensions stems from the fact that the `ILoot.RemoveWhere` method only targets surface level rules. Not only this, but the default procedures for finding child rules (considering we cannot use RemoveWhere for this) rely on hard-coding and create compatibility issues. To fully understand the
drawbacks of `ILoot.RemoveWhere` and the default system, we must learn what 'surface level' or 'root rules' are, as well as learning what child rules are (both `nested` and `chained` children).

### Surface Level / Root Rules
Surface level rules are the rules that appear directly under the `ILoot` instance. These rules can be iterated over / mutated by using the `ILoot.Get()` method and they
can be removed from the loot tree using the `ILoot.RemoveWhere()` method. In the diagram below, the root rules are the 4 rules under the `ILoot` 
(`DropBasedOnMaserMode`, `ItemDropWithConditionRule`, `LeadingConditionRule(NotExpert)`, and `DropBasedOnExpertMode`).

### Chained Rules
Rule-Chaining is the process of attaching another rule (or several rules) onto another rule with an `IItemDropRuleChainAttempt`. Every `IItemDropRule` in 
the tModLoader codebase can have other rules chained to them, because all `IItemDropRule` subclasses have a `ChainedRules` list. There are 3 types of 
`IItemDropRuleChainAttempt` that are used throughout vanilla code / tModLoader: 
- `TryIfFailedRandomRoll` when a rule is executed but fails to roll the drop.
- `TryIfSucceeded` when a rule is executed and succeeds to roll the drop.
- `TryIfDoesntFillConditions` when the conditions are not met for a rule to to be executed in the first place  .

A rule that is chained onto another rule is considered to be the 'chained child' of that rule. Chained children are referenced
by an `IItemDropRuleChainAttempt` within their chained parent's `ChainedRules` list.
In the diagram below, there are about 10 different chains attached onto various `IItemDropRule` instances in the loot. View the diagram in fullscreen to get a better look at them.

### Nested Rules
Rule-Nesting is a capability that some `IItemDropRule` implementations have in tModLoader. Any `IItemDropRule` in the codebase that implements `INestedItemDropRule` is
a rule that is able to execute other rules. Examples of `INestedItemDropRule` include (but are not limited to) `DropBasedOnExpertMode`, `OneFromRulesRule`, `SequentialRulesRule`. A rule that is able to be executed by another rule is considered to be nested inside said rule. We refer to these as 'nested child' and we refer to their parent as the 'nested parent'.

### Diagram For Reference
Below here is a diagram for Plantera's `NPCLoot` tree. This diagram is based on this vanilla tModLoader code:
```cs
private void RegisterBoss_Plantera()
{
    short type = 262;
    this.RegisterToNPC(type, ItemDropRule.BossBag(3328)); // Treasure Bag
    this.RegisterToNPC(type, ItemDropRule.MasterModeCommonDrop(4934)); // Plantera Trophy
    this.RegisterToNPC(type, ItemDropRule.MasterModeDropOnAllPlayers(4806, this._masterModeDropRng)); // Plantera Pet
    LeadingConditionRule notExpert = new LeadingConditionRule(new Conditions.NotExpert());
    this.RegisterToNPC(type, notExpert);
    LeadingConditionRule firstTimeKilling = new LeadingConditionRule(new Conditions.FirstTimeKillingPlantera());
    notExpert.OnSuccess(firstTimeKilling);
    notExpert.OnSuccess(ItemDropRule.Common(2109, 7)); // PlanteraMask
    notExpert.OnSuccess(ItemDropRule.Common(1141)); // TempleKey
    notExpert.OnSuccess(ItemDropRule.Common(1182, 20)); // Seedling
    notExpert.OnSuccess(ItemDropRule.Common(1305, 50)); // The Axe
    notExpert.OnSuccess(ItemDropRule.Common(1157, 4)); // Pygmy Staff
    notExpert.OnSuccess(ItemDropRule.Common(3021, 10)); // Pygmy Necklace
    IItemDropRule itemDropRule = ItemDropRule.Common(758); // Grenade Launcher
    firstTimeKilling.OnSuccess(itemDropRule, hideLootReport: true);
    itemDropRule.OnSuccess(ItemDropRule.Common(771, 1, 50, 150), hideLootReport: true);
    firstTimeKilling.OnFailedConditions(new OneFromRulesRule(1, 
        itemDropRule, // Grenade Launcher (same reference as the one executed on success of firstTimeKilling)
        ItemDropRule.Common(1255), // Venus Magnum
        ItemDropRule.Common(788),  // Nettle Burst
        ItemDropRule.Common(1178), // Leaf Blower
        ItemDropRule.Common(1259), // Flower Pow
        ItemDropRule.Common(1155), // Wasp Gun
        ItemDropRule.Common(3018))); // Seedler
}
```
![This is an image](https://i.gyazo.com/50a33252786ab59cb10af26d712ef3e7.png)

## Limitations of the Default System
Now that we understand how the loot tree works, we can understand the limitations of the base `RemoveWhere` method. If we wanted to remove the Venus Magnum `CommonDrop` from Plantera (which is at the bottom of the loot tree), the `RemoveWhere` method is not going to cut it. This is because the `RemoveWhere` will only target the 4 top-level root rules to remove them from the `ILoot` itself (it will not look any deeper than that). By default, removing the Venus Magnum from Plantera requires us to hard-code loops to dig through the `ChainedRules` list of several rules (and also dig through some fileds within `INestedItemDropRule`, such as `OneFromRulesRule.options`) in order to navigate to the rules that we want to remove. Let's show an example of trying to remove the venus magnum from Plantera:
```cs
if (npc.type == NPCID.Plantera)
{
    foreach (IItemDropRule rootRule in npcLoot.Get(false))
    {
        if (rootRule is LeadingConditionRule notExpert && notExpert.condition is Conditions.NotExpert)
        {
            foreach (IItemDropRuleChainAttempt chainAttempt in notExpert.ChainedRules)
            {
                if (chainAttempt is TryIfSucceeded && chainAttempt.RuleToChain is LeadingConditionRule firstTime && firstTime.condition is Conditions.FirstTimeKillingPlantera)
                {
                    foreach (IItemDropRuleChainAttempt chainAttempt2 in firstTime.ChainedRules)
                    {
                        if (chainAttempt2 is TryIfDoesntFillConditions && chainAttempt2.RuleToChain is OneFromRulesRule oneFromRules)
                        {
                            // Use LINQ to make code shorter
                            oneFromRules.options = oneFromRules.options.Where(option => !(option is CommonDrop cd && cd.itemId == ItemID.VenusMagnum)).ToArray();
                        }
                    }
                }
            }
        }
    }
}
```
Not only is this code a bit messy, it also comes with potential compatibility issues:
- Due to hard-coding, the structure of the loot tree matters. What I mean by this, is if any other mod makes a change to the loot tree to add or remove rules between any of the rules we are iterating through, then the hard-coded loops will fail to find it. An example of this could be a different mod adding another `LeadingConditionRule` between `firstTimeKillingPlantera` and the `OneFromRulesRule` (maybe `secondTimeKillingPlantera`?). If they were to do this, our for-loops would fail to account for the possibility of another `LeadingConditionRule` coming before our `OneFromRulesRule` that we are looking for. Essentially, any changes to the parent-child structure that would move rules down or up a level on the loot tree would never be accounted for. This is why tModLoader recommends not removing rules (only mutating) for compatibility.  

## Use Cases of LootExtensions
Now that we have a solid understanding of the limitations behind the default system, we can learn about how LootExtensions can be used to overcome these issues.
### The Power of Recursion
The main principle of the LootExtensions system is that it uses recursion for its `FindRuleWhere<R>` and `RemoveWhere<R>` methods (and their overloads). The first benefit of this is code simplicity. Instead of having to hard-code nested loops, we can let the recursive methods of LootExtensions do their magic of finding rules (both chained and nested) within the loot tree. Another amazing benefit to this is code compatibility. The issue described above in the default system is completely solved by using recursion because recursive methods are dynamic in nature. The structure of the loot tree will never matter, as long as the rule we are searching for matches our supplied Type (`<R>`) and `LootPredicate` (more on these below in the section showing how to use the LootExtensions system).

### The Power of Code Simplicity
Let's take a look at a bunch of examples using the default system vs using LootExtensions to make modifications to vanilla loot trees. Note that the `&& false` within the default system `if` statements are to make it so the default code doesn't run. Notice how most of the LootExtensions examples are one line of code.
```cs
public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
{
    // Remove PossessedHatchet from Golem
    // Vanilla Loot Tree:
    // LeadingConditionRule(NotExpert)
    //     TryIfSucceeded Chain => OneFromRulesRule containing Possessed Hatchet
    //         Nested IItemDropRule Option => CommonDrop(PossessedHatchet)
    if (npc.type == NPCID.Golem)
    {
        // DEFAULT TMODLOADER
        // Use hard-coded loops to iterate and find a OneFromRulesRule whose options contains a possessed hatchet
        // Due to hard-coding, the structure of the loot tree matters. The CommonDrop must be the direct nested child of the OneFromRulesRule, which must be the direct chained
        // child of LeadingConditionRule, chained with a TryIfSucceeded chain attempt, and the LeadingConditionRule must be the direct child of the loot itself
        // After finding it, remove the PossessedHatchet CommonDrop from being nested inside of the OneFromRulesRule
        foreach (IItemDropRule rootRule in npcLoot.Get(false))
        {
            if (false && rootRule is LeadingConditionRule leadingCondition && leadingCondition.condition is Conditions.NotExpert)
            {
                foreach (IItemDropRuleChainAttempt chainAttempt in leadingCondition.ChainedRules)
                {
                    if (chainAttempt is TryIfSucceeded successChain && successChain.RuleToChain is OneFromRulesRule oneFromRules)
                    {
                        foreach (IItemDropRule nestedRule in oneFromRules.options)
                        {
                            if (nestedRule is CommonDrop cd && cd.itemId == ItemID.PossessedHatchet)
                            {
                                // Use LINQ to remove with one line of code
                                oneFromRules.options = oneFromRules.options.Where(option => !(option is CommonDrop cd && cd.itemId == ItemID.PossessedHatchet)).ToArray();
                            }
                        }
                    }
                }
            }
        }

        // EXTENSIONS
        // Find and remove any CommonDrop in the loot tree (including CommonDrops nested inside other rules like this one). The CommonDrop's ItemID must be PossessedHatchet in order to be removed.
        // The structure of the loot tree doesn't matter
        npcLoot.RemoveWhere<CommonDrop>(cd => cd.itemId == ItemID.PossessedHatchet);
    }

    // Remove venus magnum from plantera
    // Vanilla loot tree (showing only relevant branches):
    // LeadingConditionRule(NotExpert)
    //     TryIfSucceeded Chain => LeadingConditionRule(FirstTimeKillingPlantera)
    //         TryIfFailedRandomRoll Chain => OneFromRulesRule containing venus magnum
    //             Nested IItemDropRule Option => CommonDrop(ItemID.VenusMagnum) 
    if (npc.type == NPCID.Plantera)
    {
        // DEFAULT TMODLOADER
        // Use hard-coded loops to iterate and find a OneFromRulesRule whose options contains a venus magnum
        // Due to hard coding, the structure of the loot tree matters
        foreach (IItemDropRule rootRule in npcLoot.Get(false))
        {
            if (false && rootRule is LeadingConditionRule notExpert && notExpert.condition is Conditions.NotExpert)
            {
                foreach (IItemDropRuleChainAttempt chainAttempt in notExpert.ChainedRules)
                {
                    if (chainAttempt is TryIfSucceeded && chainAttempt.RuleToChain is LeadingConditionRule firstTime && firstTime.condition is Conditions.FirstTimeKillingPlantera)
                    {
                        foreach (IItemDropRuleChainAttempt chainAttempt2 in firstTime.ChainedRules)
                        {
                            if (chainAttempt2 is TryIfDoesntFillConditions && chainAttempt2.RuleToChain is OneFromRulesRule oneFromRules)
                            {
                                // Use LINQ to make code shorter
                                oneFromRules.options = oneFromRules.options.Where(option => !(option is CommonDrop cd && cd.itemId == ItemID.VenusMagnum)).ToArray();
                            }
                        }
                    }
                }
            }
        }

        // EXTENSIONS
        // Find and remove any CommonDrop(s) from the loot tree. The CommonDrop's ItemID must be VenusMagnum in order to be removed.
        // The structure of the loot tree doesn't matter
        npcLoot.RemoveWhere<CommonDrop>(cd => cd.itemId == ItemID.VenusMagnum);
    }

    // Remove BookOfSkulls from Skeletron.
    // Vanilla Loot Tree:
    // ItemDropWithConditionRule
    //     TryIfFailedRandomRoll Chain => CommonDrop(SkeletronHand)
    //         TryIfFailedRandomRollChain => CommonDrop(BookOfSkulls) 
    if (npc.type == NPCID.SkeletronHead)
    {
        // DEFAULT TMODLOADER
        // Use hard-coded loops to find it
        foreach (IItemDropRule rule in npcLoot.Get())
        {
            if (false && rule is ItemDropWithConditionRule skeletronMaskRule && skeletronMaskRule.itemId == ItemID.SkeletronMask && skeletronMaskRule.condition is Conditions.NotExpert)
            {
                foreach (IItemDropRuleChainAttempt maskChain in skeletronMaskRule.ChainedRules)
                {
                    if (maskChain is TryIfFailedRandomRoll && maskChain.RuleToChain is CommonDrop skeletronHandRule && skeletronHandRule.itemId == ItemID.SkeletronHand)
                    {
                        IEnumerable<IItemDropRuleChainAttempt> withoutBookOfSkulls = skeletronHandRule.ChainedRules.Where(attempt => !(attempt.RuleToChain is CommonDrop bookOfSkullsDrop && bookOfSkullsDrop.itemId == ItemID.BookofSkulls));
                        skeletronHandRule.ChainedRules.Clear();
                        skeletronHandRule.ChainedRules.AddRange(withoutBookOfSkulls);
                    }
                }
            }
        }

        // EXTENSIONS
        // Any BookOfSkulls CommonDrop anywhere within the loot will be removed. Doesn't care about if it has a parent or if/how it is chained to its parent.
        npcLoot.RemoveWhere<CommonDrop>(bookofSkullsDrop => bookofSkullsDrop.itemId == ItemID.BookofSkulls);
    }

    // Remove BoneSword from Skeleton and re-attach its children to the parent it was removed from (so we don't lose Skull drop)
    // Vanilla Loot Tree:
    // CommonDrop(AncientIronHelmet)
    //     TryIfFailedRandomRoll Chain => CommonDrop(AncientGoldHelmet)
    //         TryIfFailedRandomRoll Chain => CommonDrop(BoneSword) <= This is what we want to remove, but without losing Skull drop too
    //             TryIfFailedRandomRoll Chain => CommonDrop(Skull)
    if (npc.type == NPCID.Skeleton)
    {
        // DEFAULT TMODLOADER
        // Use hard-coded loops to remove BoneSwordDrop from Skeleton NPC, then re-attach BoneSword's chained rules 
        // onto AncientGoldHelmetDrop so that the things chained after BoneSword (Skull Drop) are not lost
        foreach (IItemDropRule rule in npcLoot.Get())
        {
            if (false && rule is CommonDrop ancientIronDrop && ancientIronDrop.itemId == ItemID.AncientIronHelmet)
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
                            }
                        }
                    }
                }
            }
        }

        // EXTENSIONS 
        // Any BoneSword CommonDrop anywhere within the loot will be removed. Doesn't care about if it has a parent or if/how it is chained to its parent.
        // Chains are automatically re-attached due to 'reattachChains' param being set to true (false by default)
        npcLoot.RemoveWhere<CommonDrop>(boneSwordDrop => boneSwordDrop.itemId == ItemID.BoneSword, reattachChains: true);
    }
}
```
If we remove the explanation comments and condense the code down to just using the extensions, we get this:
```cs
public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
{
    // Remove PossessedHatchet from Golem
    if (npc.type == NPCID.Golem)
    {
        npcLoot.RemoveWhere<CommonDrop>(cd => cd.itemId == ItemID.PossessedHatchet);
    }

    // Remove venus magnum from Plantera
    if (npc.type == NPCID.Plantera)
    {
        npcLoot.RemoveWhere<CommonDrop>(cd => cd.itemId == ItemID.VenusMagnum);
    }

    // Remove BookOfSkulls from Skeletron
    if (npc.type == NPCID.SkeletronHead)
    {
        npcLoot.RemoveWhere<CommonDrop>(bookofSkullsDrop => bookofSkullsDrop.itemId == ItemID.BookofSkulls);
    }

    // Remove Bone Sword from Skeleton.
    // BoneSword has SkullDrop chained after it, and we don't want to lose that drop, so we tell the algorithm to re-attach the chains
    if (npc.type == NPCID.Skeleton)
    {
        npcLoot.RemoveWhere<CommonDrop>(boneSwordDrop => boneSwordDrop.itemId == ItemID.BoneSword, reattachChains: true);
    }
}
```
Much more readable, in my opinion.

## How to use LootExtensions
### Loot Predicates
A `LootPredicate<R>` is a generic delegate that is called on `IItemDropRule` implementations in the context of the recursive find/removal methods. The parameter of the function is a rule of type `R` and the function should return true or false. `LootPredicates` are executed on all rules that the recursive find/remove methods touch. Returning true in a predicate in the context of a recursive removal method means that rule will be removed. Returning true in the context of a recursive find method will add that rule to the list of found rules. More explanations on recursive removing/finding methods below.

### Recursive Removing
The following methods are used for recursive removing:
- `void ILoot.RemoveWhere<R>(LootPredicate<R> predicate, bool includeGlobalDrops = false, int? nthChild = null, bool reattachChains = false, bool stopAtFirst = false)`. This method will recursively dig through the `ILoot` and remove any rules of type `R` that match the given `LootPredicate<R>`. It searches through the root rules, their children (nested and chained), as well as their children, so on and so forth. For example you could do `npcLoot.RemoveWhere<CommonDrop>()` (note how there is no predicate, which defaults to a predicate that returns true no matter what), and it would remove ALL CommonDrops from the loot pool. If you did `npcLoot.RemoveWhere<CommonDrop>(stopAtFirst: true)` it would remove the first CommonDrop found within the loot pool.
- `IItemDropRule.RemoveChildrenWhere<R>` same as the one above, but it is called on an `IItemDropRule` within the `ILoot` instead of on the `ILoot` itself.
- There is also a `<P, C, R>` generic overload that is used to be more specific. It stands for ParentType, ChainType, RuleType (not the COVID test we all had to take). For example you could do `RemoveWhere<CommonDrop, TryIfFailedRandomRoll, ItemDropWithConditionRule>` which would only remove an `ItemDropWithConditionRule` that is chained to a `CommonDrop` with a `TryIfFailedRandomRoll` chain. The `ItemDropWithConditionRule` must match the `LootPredicate<ItemDropWithConditionRule>`
- There is also a `<N, R>` generic overload that is used to be more specific. It stands for NestedParentType, RuleType. This would only check for a rule of type `R` that is nested inside of a rule of type `N`. `R` must match the `LootPredicate<R>`.

### Recursive Finding
The following methods are used for recursive finding:
- `List<R> ILoot.FindRulesWhere<R>(LootPredicate<R> predicate, bool includeGlobalDrops = false, ChainReplacer chainReplacer = null, int? nthChild = null)`
- `R ILoot.FindRuleWhere<R>(LootPredicate<R> predicate, bool includeGlobalDrops = false, ChainReplacer chainReplacer = null, int? nthChild = null)`
- `bool ILoot.TryFindRuleWhere<R>(out R result, LootPredicate<R> predicate, bool includeGlobalDrops = false, ChainReplacer chainReplacer = null, int? nthChild = null)`
- `bool ILoot.HasRuleWhere<R>(LootPredicate<R> predicate, bool includeGlobalDrops = false, int? nthChild = null)`
- `List<R> IItemDropRule.FindChildrenWhere<R>(LootPredicate<R> predicate, bool includeGlobalDrops = false, ChainReplacer chainReplacer = null, int? nthChild = null)`
- `R IItemDropRule.FindChildWhere<R>(LootPredicate<R> predicate, bool includeGlobalDrops = false, ChainReplacer chainReplacer = null, int? nthChild = null)`
- `bool IItemDropRule.TryFindChildWhere<R>(out R result, LootPredicate<R> predicate, bool includeGlobalDrops = false, ChainReplacer chainReplacer = null, int? nthChild = null)`
- `bool IItemDropRule.HasChildWhere<R>(LootPredicate<R> predicate, bool includeGlobalDrops = false, int? nthChild = null)`
- I omit the descriptions of these because they work very similar to the removal methods except they are for finding. Note there are `<P, C, R>` and `<N, R>` overloads for these as well.
