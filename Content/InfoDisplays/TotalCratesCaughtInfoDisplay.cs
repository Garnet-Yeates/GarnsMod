using GarnsMod.Content.Players;
using Terraria;
using Terraria.ModLoader;

namespace GarnsMod.Content.InfoDisplays
{
    class TotalCratesCaughtInfoDisplay : InfoDisplay
    {
        public override void SetStaticDefaults()
        {
            // This is the name that will show up when hovering over icon of this info display
            InfoName.SetDefault("Total Crates Caught");
        }

        // This dictates whether or not this info display should be active
        public override bool Active()
        {
            return Main.LocalPlayer.accFishFinder;
        }

        // Here we can change the value that will be displayed in the game
        public override string DisplayValue()
        {
            int totalCratesCaught = Main.LocalPlayer.GetModPlayer<GarnsFishingRPGPlayer>().totalCratesCaught;
            string s = totalCratesCaught == 1 ? "" : "s";
            return $"{totalCratesCaught} crate{s} caught";
        }
    }
}
