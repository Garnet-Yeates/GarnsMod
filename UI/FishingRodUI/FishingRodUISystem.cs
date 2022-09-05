using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using static GarnsMod.Content.Items.Tools.GarnsFishingRod;

namespace GarnsMod.UI.FishingRodUI
{
    internal class FishingRodUISystem : ModSystem
    {
        private FishingRodUIState FishingRodUI => _fishingRodUI is null ? null : (FishingRodUIState) _fishingRodUI.CurrentState;
        public bool IsFishingRodUIOpen => FishingRodUI is not null;

        private UserInterface _fishingRodUI;

        public override void Load()
        {
            _fishingRodUI = new UserInterface();
        }

        public override void Unload()
        {
            CloseFishingRodUI();
            _fishingRodUI = null;
        }

        public void OpenFishingRodUI(ShootMode shootMode, TrailColorMode trailColorMode, TrailTypeMode trailTypeMode, int inventoryIndex)
        {
            SoundEngine.PlaySound(SoundID.MenuOpen);
            FishingRodUIState ui = new(shootMode, trailColorMode, trailTypeMode, inventoryIndex);
            ui.Activate();
            _fishingRodUI.SetState(ui);
        }

        public void CloseFishingRodUI()
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            _fishingRodUI.SetState(null);
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
                        _fishingRodUI.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

    }
}
