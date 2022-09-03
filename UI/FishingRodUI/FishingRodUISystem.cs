using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace GarnsMod.UI.FishingRodUI
{
    internal class FishingRodUISystem : ModSystem
    {
        internal static FishingRodUIState FishingRodUI;
        private UserInterface _fishingRodUI;

        public override void Load()
        {
            FishingRodUI = new FishingRodUIState();
            FishingRodUI.Activate();
            _fishingRodUI = new UserInterface();
            _fishingRodUI.SetState(FishingRodUI);

        }

        public override void UpdateUI(GameTime gameTime)
        {
            _fishingRodUI?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "YourMod: A Description",
                    delegate
                    {
                        if (FishingRodUI.Visible)
                        {
                            _fishingRodUI.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

    }
}
