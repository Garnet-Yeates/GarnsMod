using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using static GarnsMod.Content.Items.Tools.GarnsFishingRod;

namespace GarnsMod.UI.FishingRodUI
{
    internal class FishingRodUISystem : ModSystem
    {
        private FishingRodUIState FishingRodUI => _fishingRodInterface is null ? null : (FishingRodUIState) _fishingRodInterface.CurrentState;
        
        public bool IsFishingRodUIOpen => FishingRodUI is not null;

        
        private UserInterface _fishingRodInterface;

        public override void Load()
        {
            _fishingRodInterface = new UserInterface();
        }

        public override void Unload()
        {
            CloseFishingRodUI();
        }

        public void OpenFishingRodUI(ShootMode shootMode, TrailColorMode trailColorMode, TrailTypeMode trailTypeMode, int inventoryIndex)
        {
            SoundEngine.PlaySound(SoundID.MenuOpen);
            FishingRodUIState ui = new(shootMode, trailColorMode, trailTypeMode, inventoryIndex);
       //     ui.Activate(); might not be needed
            _fishingRodInterface.SetState(ui);
        }

        public void CloseFishingRodUI()
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            _fishingRodInterface.SetState(null);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            _fishingRodInterface?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "GarnsMod: Fishing Rod UI",
                    delegate
                    {
                        _fishingRodInterface.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

    }
}
