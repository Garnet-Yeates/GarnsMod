using GarnsMod.Content.Players;
using Terraria;
using Terraria.ModLoader;

namespace GarnsMod.Content.InfoDisplays
{
    // This example show how to create new informational display (like Radar, Watches, etc.)
    // Take a look at the ExampleInfoDisplayPlayer at the end of the file to see how to use it
    class MouseScreenInfoDisplay : InfoDisplay
    {
        public override string Texture => "GarnsMod/Content/InfoDisplays/DefaultInfoDisplay";

        public override void SetStaticDefaults()
        {
            // This is the name that will show up when hovering over icon of this info display
            InfoName.SetDefault("Mouse Screen (takes zoom into account)");
        }

        // This dictates whether or not this info display should be active
        public override bool Active()
        {
            return Main.LocalPlayer.accFishFinder;
        }

        // Here we can change the value that will be displayed in the game
        public override string DisplayValue()
        {
            return $"MouseScreen: {Main.MouseScreen.X} {Main.MouseScreen.Y}";
        }
    }


}
