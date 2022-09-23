using GarnsMod.Content.Players;
using Terraria;
using Terraria.ModLoader;

namespace GarnsMod.Content.InfoDisplays
{
    class TotalFishCaughtInfoDisplay : InfoDisplay
    {
        public override void SetStaticDefaults()
        {
            InfoName.SetDefault("Total Fish Caught");
        }

        public override bool Active()
        {
            return Main.LocalPlayer.accFishFinder;
        }

        public override string DisplayValue()
        {
            int totalCratesCaught = Main.LocalPlayer.GetModPlayer<GarnsFishingRPGPlayer>().totalFishCaught;
            return $"{totalCratesCaught} fish caught";
        }
    }


}
