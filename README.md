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

### Why Use LootExtensions?
#### Suck Me and Cuck Me
![This is an image](https://i.gyazo.com/50a33252786ab59cb10af26d712ef3e7.png)
