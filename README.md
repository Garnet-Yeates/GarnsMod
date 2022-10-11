# LootExtensions

## Understanding ILoot and how LootExtensions helps modify it
The main use case for LootExtensions stems from the fact that the `ILoot.RemoveWhere` method only targets surface level rules. To fully understand the
drawbacks of `ILoot.RemoveWhere`, we must learn what 'surface level' or 'root rules' are, as well as learning what child rules are (both `nested` and `chained` children)

### Surface Level / Root Rules
Surface level rules are the rules that appear directly under the `ILoot` instance. These rules can be iterated over / mutated by using the `ILoot.Get()` method and they
can be removed from the loot tree using the `ILoot.RemoveWhere()` method. In the diagram below, the root rules are the 4 rules under the `ILoot` 
(`DropBasedOnMaserMode`, `ItemDropWithConditionRule`, `LeadingConditionRule(NotExpert)`, and `DropBasedOnExpertMode`).

### Chained Rules
Rule-Chaining is the process of attaching another rule (or several rules) onto another rule with an `IItemDropRuleChainAttempt`. Every `IItemDropRule` in 
the tModLoader codebase can have other rules chained to them, because all `IItemDropRule` subclasses have a `ChainedRules` list. There are 3 types of 
`IItemDropRuleChainAttempt` that are used throughout vanilla code / tModLoader: 
- `TryIfFailedRandomRoll` when a rule is executed but fails to roll the drop
- `TryIfSucceeded` when a rule is executed and succeeds to roll the drop
- `TryIfDoesntFillConditions` when the conditions are not met for a rule to to be executed in the first place  

A rule that is chained onto another rule is considered to be the 'chained child' of that rule. Chained children are referenced
by an `IItemDropRuleChainAttempt` within their chained parent's `ChainedRules` list.
In the diagram below, there are about 10 different chains attached onto various `IItemDropRule` instances in the loot. View the diagram in fullscreen to get a better look at them.

### Nested Rules
Rule-Nesting is a capability that some `IItemDropRule` implementations have in tModLoader. Any `IItemDropRule` in the codebase that implements `INestedItemDropRule` is
a rule that is able to execute other rules. Examples of `INestedItemDropRule` include (but are not limited to) `DropBasedOnExpertMode`, `OneFromRulesRule`, `SequentialRulesRule`. A rule that is able to be executed by another rule is considered to be nested inside said rule. We refer to these as 'nested child' and we refer to their parent as the 'nested parent'

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
- Due to hard-coding, the structure of the loot tree matters. What I mean by this, is if any other mod makes a change to the loot tree to add or remove rules between any of the rules we are iterating through, then the hard-coded loops will fail to find it. An example of this could be a different mod adding another `LeadingConditionRule` between `firstTimeKillingPlantera` and the `OneFromRulesRule` (maybe `secondTimeKillingPlantera`?). If they were to do this, our for-loops would fail to account for the possibility of another `LeadingConditionRule` coming before our `OneFromRulesRule` that we are looking for. Essentially, any changes to the parent-child structure that would move rules down or up a level on the loot tree would never be accounted for. This is why tModLoader recommends not removing rules (only mutating) for compatibility  

## Use Cases of LootExtensions
Now that we have a solid understanding of the limitations behind the default system, we can learn about how LootExtensions can be used to overcome these issues.
### The Power of Recursion
The main principle of the LootExtensions system is that it uses recursion for its `FindRuleWhere<R>` and `RemoveWhere<R>` methods (and their overloads). The first benefit of this is code simplicity. Instead of having to hard-code nested loops, we can let the recursive methods of LootExtensions do their magic of finding rules (both chained and nested) within the loot tree. Another amazing benefit to this is code compatibility. The issue described above in the default system is completely solved by using recursion because recursive methods are dynamic in nature. The structure of the loot tree will never matter, as long as the rule we are searching for matches our supplied Type (<R>) and `LootPredicate` (more on these below in the section showing how to use the LootExtensions system)
