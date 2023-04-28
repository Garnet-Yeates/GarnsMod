# LootExtensions Overview

## Understanding the Loot Tree
The main use case for LootExtensions stems from the fact that the `ILoot.RemoveWhere` method only targets surface level rules. Not only this, but the default procedures for finding child rules (considering we cannot use RemoveWhere for this) rely on hard-coding and create compatibility issues. To fully understand the
drawbacks of `ILoot.RemoveWhere` and the default system, we must learn what 'surface level' or 'root rules' are, as well as learning what child rules are (both `nested` and `chained` children).

### Surface Level / Root Rules
Surface level rules are the rules that appear directly under the `ILoot` instance. These rules can be directly iterated over / mutated by using the `ILoot.Get()` method and they
can be removed from the loot tree using the `ILoot.RemoveWhere()` method. In the diagram below, the root rules are the 4 rules under the `ILoot` 
(`DropBasedOnMasterMode`, `ItemDropWithConditionRule`, `LeadingConditionRule(NotExpert)`, and `DropBasedOnExpertMode`). If we want to access rules below the root rules, we cannot use `RemoveWhere` and we must manually loop through chained and nested rules to remove them ourselves.

### Chained Rules
Rule-Chaining is the process of attaching another rule (or several rules) onto another rule with an `IItemDropRuleChainAttempt`. Every `IItemDropRule` in 
the tModLoader codebase can have other rules chained to them, because all `IItemDropRule` subclasses have a `ChainedRules` list. There are 3 types of 
`IItemDropRuleChainAttempt` that are used throughout vanilla code / tModLoader: 
- `TryIfFailedRandomRoll` when a rule is executed but fails to roll the drop.
- `TryIfSucceeded` when a rule is executed and succeeds to roll the drop.
- `TryIfDoesntFillConditions` when the conditions are not met for a rule to to be executed in the first place  .

A rule that is chained onto another rule is considered to be the 'chained child' of that rule. Their parent rule is considered to be the 'chained parent'. Chained children are referenced
by an `IItemDropRuleChainAttempt` within their chained parent's `ChainedRules` list.
In the diagram below, there are about 10 different chains attached onto various `IItemDropRule` instances in the loot. View the diagram in fullscreen to get a better look at them.

### Nested Rules
Rule-Nesting is a capability that some `IItemDropRule` implementations have in tModLoader. Any `IItemDropRule` in the codebase that implements `INestedItemDropRule` is
a rule that is able to execute other rules. Examples of `INestedItemDropRule` include (but are not limited to) `DropBasedOnExpertMode`, `OneFromRulesRule`, `SequentialRulesRule`. A rule that is able to be executed by another rule is considered to be nested inside said rule. We refer to these as 'nested child' and we refer to their parent as the 'nested parent'.

### Diagram For Reference
Below here is a diagram for Plantera's `NPCLoot` tree. This diagram is based on this vanilla code:
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
![Plantera Loot Tree](https://i.gyazo.com/50a33252786ab59cb10af26d712ef3e7.png)

## Limitations of the Default System
Now that we understand how the loot tree works, we can understand the limitations of the base `RemoveWhere` method. If we wanted to remove the Venus Magnum `CommonDrop` from Plantera (which is at the bottom of the loot tree), the `RemoveWhere` method is not going to cut it. This is because the `RemoveWhere` will only target the 4 top-level root rules to remove them from the `ILoot` itself (it will not look any deeper than that). By default, removing the Venus Magnum from Plantera requires us to hard-code loops to dig through the `ChainedRules` list of several rules (and also dig through some fields within `INestedItemDropRule`, such as `OneFromRulesRule.options`) in order to navigate to the rules that we want to remove. Let's show an example of trying to remove the venus magnum from Plantera:
```cs
if (npc.type == NPCID.Plantera)
{
    foreach (IItemDropRule rootRule in npcLoot.Get(false))
    {
        if (rootRule is LeadingConditionRule notExpert && notExpert.condition is Conditions.NotExpert)
        {
            foreach (IItemDropRuleChainAttempt chainAttempt in notExpert.ChainedRules)
            {
                if (chainAttempt.RuleToChain is LeadingConditionRule firstTime)
                {
                    foreach (IItemDropRuleChainAttempt chainAttempt2 in firstTime.ChainedRules)
                    {
                        if (chainAttempt2.RuleToChain is OneFromRulesRule oneFromRules)
                        {
                            // Use LINQ to make code shorter
                            oneFromRules.options = oneFromRules.options.Where(
                                option => !(option is CommonDrop cd && cd.itemId == ItemID.VenusMagnum)
                            ).ToArray();
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
    // We don't want to lose BoneSword drop, so we tell the algorithm to re-attach the chains
    if (npc.type == NPCID.Skeleton)
    {
        npcLoot.RemoveWhere<CommonDrop>(boneSwordDrop => boneSwordDrop.itemId == ItemID.BoneSword, reattachChains: true);
    }
}
```
Much more readable, in my opinion.

# How to use LootExtensions
## Loot Predicates
A `LootPredicate<R>` is a generic delegate that is called on `IItemDropRule` implementations in the context of the recursive find/removal methods. The parameter of the function is a rule of type `R` and the function should return true or false. `LootPredicates` are executed on all rules that the recursive find/remove methods touch. Returning true in a predicate in the context of a recursive removal method means that rule will be removed. Returning true in the context of a recursive find method will add that rule to the list of found rules. More explanations on recursive removing/finding methods below.

```cs
static bool FindSkull(CommonDrop cd)
{
    return cd.itemId == ItemID.Skull;
}

public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
{
    LootExtensions.LootPredicate<CommonDrop> findSkullDrop;

    // Use an existing method
    findSkullDrop = FindSkull;

    // Or use a shorthand lamba expression
    findSkullDrop = cd => cd.itemId == ItemID.Skull;

    // Plug into a recursive method 
    itemLoot.FindRuleWhere<CommonDrop>(findSkullDrop);

    // We don't even need the <CommonDrop> because it is inferred (since the predicate variable has <CommonDrop>)
    itemLoot.FindRuleWhere(findSkullDrop);

    // These above examples are just to understand that LootPredicates are delegates and can be treated as variables
    // Normally we use shorthand lambda expressions and we inline them in the recursive methods, like below:

    // In this case, it is inferred that the 'cd' parameter is a CommonDrop (since the method has <CommonDrop>)
    itemLoot.FindRuleWhere<CommonDrop>(cd => cd.itemId == ItemID.Skull);

    // In this case, it is inferred that 'ofo' parameter is a OneFromOptionsDropRule (method has <OneFromOptionsDropRule>)
    itemLoot.FindRuleWhere<OneFromOptionsDropRule>(ofo => ofo.ContainsOption(ItemID.Frostbrand));
}
```

### Examples
```cs
public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
{
    // Any rule that isn't a root (top-level) rule will be found
    LootExtensions.LootPredicate<IItemDropRule> findRuleWithParent = rule => rule.HasParentRule();

    // Any CommonDrop that isn't a root (top-level) rule will be found
    LootExtensions.LootPredicate<CommonDrop> findCommonDropWithParent = rule => rule.HasParentRule();

    // Any non top-level, nested CommonDrop will be found
    LootExtensions.LootPredicate<CommonDrop> findNestedCommon = rule => rule.HasParentRule() && rule.IsNested();

    // Any non top-level, chained CommonDrop will be found
    LootExtensions.LootPredicate<CommonDrop> findChainedCommon = rule => rule.HasParentRule() && rule.IsChained();

    // Any OneFromOptionsDropRule containing ItemID.Frostbrand in its itemIds array will be found
    LootExtensions.LootPredicate<OneFromOptionsDropRule> findFrostbrand = rule => rule.ContainsOption(ItemID.Frostbrand);

    // Any OneFromOptionsDropRule will be found
    LootExtensions.LootPredicate<OneFromOptionsDropRule> findOptions = rule => true;
    
    // Then plug these into RecursiveRemove or RecursiveFind methods, such as RemoveRule / HasRule, etc
}
```

## Recursive Removing
The following methods are used for recursive removing:

### ILoot Extensions
```cs
bool RemoveWhere<R>(LootPredicate<R> predicate, bool includeGlobalDrops = false, int? nthChild = null, 
    bool reattachChains = false, bool stopAtFirst = false);
```
This method will recursively dig through the `ILoot` and remove any rules of type `R` that match the given `LootPredicate<R>`. It searches through the root rules, their children (nested and chained), as well as their children, so on and so forth. For example you could do `npcLoot.RemoveWhere<CommonDrop>()` (note how there is no predicate, which defaults to a predicate that returns true no matter what), and it would remove ALL CommonDrops from the loot pool. If you did `npcLoot.RemoveWhere<CommonDrop>(stopAtFirst: true)` it would remove the first CommonDrop found within the loot pool. Returns true if any rules were removed
<br></br>

### IItemDropRule Extensions
```cs
bool RemoveChildrenWhere<R>(LootPredicate<R> predicate, bool includeGlobalDrops = false, int? nthChild = null, 
    bool reattachChains = false, bool stopAtFirst = false);
```
This method will recursively dig through the children of this `IItemDropRule` and remove any rules of type `R` that match the given `LootPredicate<R>`. It searches through this rule's children (nested and chained), their children, as well as their children, so on and so forth. For example you could do `rule.RemoveWhere<CommonDrop>()` and it would remove ALL CommonDrops that descend from this CommonDrop. 
<br></br>

## Recursive Finding

The following extension methods are used for recursive finding:

### ILoot Extensions

These can be called on any `ILoot`, such as `NPCLoot` or `ItemLoot`

```cs
List<R> FindRulesWhere<R>(LootPredicate<R> predicate, bool includeGlobalDrops = false, int? nthChild = null);
```
Finds all `IItemDropRules` of type `R` within the loot that match the `LootPredicate<R>`. If `nthChild` is supplied, it will only find rules that are the the `nthChild` of the loot (i.e if 1 is supplied for nthChild, it will only find root rules). Returns an empty `List<R>` if none are found.
<br></br>

```CS
R FindRuleWhere<R>(LootPredicate<R> predicate, bool includeGlobalDrops = false, int? nthChild = null);
```
Finds the first `IItemDropRule` of type `R` within the loot that matches the `LootPredicate<R>`. If `nthChild` is supplied, it will only return a rule that is the the `nthChild` of the loot. Returns `null` if no rule is found.
<br></br>

```cs
bool TryFindRuleWhere<R>(out R result, LootPredicate<R> predicate, bool includeGlobalDrops = false, int? nthChild = null);
```
Tries to find the first `IItemDropRule` of type `R` within the loot that matches the `LootPredicate<R>`. If `nthChild` is supplied, it will only find a rule that is the the `nthChild` of the loot. Returns `false` if no rule is found. If a rule is found, then `result` `out` parameter is set to the found rule.
<br></br>

```cs
bool HasRuleWhere<R>(LootPredicate<R> predicate, bool includeGlobalDrops = false, int? nthChild = null);
```
Determines if this `ILoot` has a rule of type `R` that matches the `LootPredicate<R>`. If `nthChild` is specified, the rule must be the `nthChild` of the `ILoot`.
<br></br>

### IItemDropRule Extensions

These can be called on any `IItemDropRule`, such as `CommonDrop`, `OneFromOptionsDropRule`, `OneFromRulesRule`, etc

```cs
List<R> FindChildrenWhere<R>(LootPredicate<R> predicate, bool includeGlobalDrops = false, ChainReplacer chainReplacer = null,
    int? nthChild = null);
```
Finds all `IItemDropRules` of type `R` that are below this rule on the loot tree, that match the `LootPredicate<R>`. If `nthChild` is supplied, it will only find rules that are the the `nthChild` of this rule (i.e if 1 is supplied for nthChild, it will only find children directly under this rule). Returns an empty `List<R>` if none are found.
<br></br>

```CS
R FindChildWhere<R>(LootPredicate<R> predicate, bool includeGlobalDrops = false, ChainReplacer chainReplacer = null, 
    int? nthChild = null);
```
Finds the first `IItemDropRule` of type `R` below this rule on the loot tree, that matches the `LootPredicate<R>`. If `nthChild` is supplied, it will only return a rule that is the the `nthChild` of this rule. Returns `null` if no rule is found.
<br></br>

```CS
bool TryFindChildWhere<R>(out R result, LootPredicate<R> predicate, bool includeGlobalDrops = false, 
    ChainReplacer chainReplacer = null, int? nthChild = null);
```  
Tries to find the first `IItemDropRule` of type `R` below this rule on the loot tree, that matches the `LootPredicate<R>`. If `nthChild` is supplied, it will only find a rule that is the the `nthChild` of this rule. Returns `false` if no rule is found. If a rule is found, then `result` `out` parameter is set to the found rule.
<br></br>

```CS
bool HasChildWhere<R>(LootPredicate<R> predicate, bool includeGlobalDrops = false, int? nthChild = null);
```
Determines if this `IItemDropRule` has a rule of type `R` below it on the loot tree, that matches the `LootPredicate<R>`. If `nthChild` is specified, the rule must be the `nthChild` of this rule.
<br></br>

## <P, C, R> Generic Overloads
All of the above methods have a `<P, C, R>` generic overload. These overloads are simply syntax sugar. What they are doing underneath is adding the following expression to the supplied `LootPredicate<R>`
```cs
rule.HasParentRule() && rule.IsChained() && rule.ChainFromImmediateParent() is C && rule.ImmediateParentRule() is P
```

## <N, R> Generic Overloads
All of the above methods have a `<N, R>` generic overload. These overloads are also syntax sugar. What they are doing underneath is adding the following expression to the supplied `LootPredicate<R>`
```cs
rule.HasParentRule() && rule.IsNested() && rule.ImmediateParentRule() is N
```

## When to Use Generic Overloads
The `<P, C, R>` and `<N, R>` generic overloads are not really needed in most cases. In general, you will just use the `<R>` overload and don't need to narrow down the search based on what rule the current rule is Nested/Chained in. However there are situations where you would want to use them. Take this example (from within the same Plantera example)
![Collision Example](https://i.gyazo.com/3bf51d5c3fd08006c9734fb0edb6f911.png)
In this example, we have a situation where there the same `CommonDrop` appears twice in the tree. Using the normal `<R>` overload would make it so that this drop is removed in both places.
```cs
// This would remove the Grenade Launcher in both places
npcLoot.RemoveWhere<CommonDrop>(drop => drop.itemId == ItemID.GrenadeLauncher);

// This would remove the chained Grenade Launcher drop (the left one)
npcLoot.RemoveWhere<LeadingConditionRule, TryIfSucceeded, CommonDrop>(drop => drop.itemId == ItemID.GrenadeLauncher);
// This would also remove the chained one, but it doesn't care about what chain type it is or what it's chained to (better for compatibility if other mods change)
npcLoot.RemoveWhere<IItemDropRule, IItemDropRuleChainAttempt, CommonDrop>(drop => drop.itemId == ItemID.GrenadeLauncher);

// This would remove the nested Grenade Launcher drop (the right one). 
npcLoot.RemoveWhere<OneFromRulesRule, CommonDrop>(drop => drop.itemId == ItemID.GrenadeLauncher);
// This would also remove the nested one, but it doesn't care about what it is nested inside of
npcLoot.RemoveWhere<IItemDropRule, CommonDrop>(drop => drop.itemId == ItemID.GrenadeLauncher);
```