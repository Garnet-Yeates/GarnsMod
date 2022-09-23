using GarnsMod.Content.Players;
using Terraria;
using Terraria.ModLoader;

namespace GarnsMod.Content.InfoDisplays
{
    class TotalCratesCaughtInfoDisplay : InfoDisplay
    {
        public override void SetStaticDefaults()
        {
            InfoName.SetDefault("Total Crates Caught");
        }

        public override bool Active()
        {
            return Main.LocalPlayer.accFishFinder;
        }

        public override string DisplayValue()
        {
            int totalCratesCaught = Main.LocalPlayer.GetModPlayer<GarnsFishingRPGPlayer>().totalCratesCaught;
            string s = totalCratesCaught == 1 ? "" : "s";
            return $"{totalCratesCaught} crate{s} caught";
        }
    }
}
