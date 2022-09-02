using GarnsMod.Content.Items.Tools;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace GarnsMod.Content.Players
{
    // This ModPlayer is used to make things happen when the player fishes specifically with GarnsFishingRod
    public class GarnsFishingRodPlayer : ModPlayer
    {
        // This is used for multiplicatively increasing fishing skill. Works similar to how time of day/moonphase/weather affects it
        // Pretty sure it is only called on the client who is fishing
        // For our additive increase we do it in GarnFishingRod.HoldItem
        public override void GetFishingLevel(Item fishingRod, Item bait, ref float fishingLevelMultiplier)
        {
            if (fishingRod.ModItem is GarnsFishingRod rod)
            {
                fishingLevelMultiplier += rod.FishingPowerMultIncrease * 0.01f;
            }
        }

        // Only called on the client fishing
        public override void ModifyFishingAttempt(ref FishingAttempt attempt)
        {
            // This if block runs if the player is fishing with Garn's fishing rod
            if (attempt.playerFishingConditions.Pole.ModItem is GarnsFishingRod rod)
            {
                // The pole can fish in lava at [LavaFishingLevel]
                if (rod.level >= GarnsFishingRod.LavaFishingLevel)
                {
                    attempt.CanFishInLava = true;
                }

                // At level [CrateChanceLevel], there is 10% additional chance that the catch will be a crate
                // The "tier" of the crate depends on the rarity, which we don't modify here (see ExampleMod ExampleFishingPlayer.CatchFish comments)
                if (!attempt.crate && rod.level >= GarnsFishingRod.CrateChanceLevel)
                {
                    if (Main.rand.Next(100) < 10)
                    {
                        attempt.crate = true;
                    }
                }
            }
        }

        // Only called on the client who is fishing
        public override bool? CanConsumeBait(Item bait)
        {
            PlayerFishingConditions conditions = Player.GetFishingConditions(); ;

            // This makes it so there is a multiplicative % decrease in bait consumption. We return false or null which means "dont consume" or "let vanilla decide"
            if (conditions.Pole.ModItem is GarnsFishingRod rod)
            {
                return Main.rand.Next(100) < rod.BaitConsumptionReductionPercent ? false : null;
            }

            return null; // Let the default vanilla logic run
        }

        // Only called on the client that is fishing
        public override void ModifyCaughtFish(Item fish)
        {
            if (Player.GetFishingConditions().Pole.ModItem is GarnsFishingRod rod)
            {
                rod.OnCatchFish();
            }
        }
    }
}
