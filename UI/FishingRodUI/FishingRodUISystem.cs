using Microsoft.Xna.Framework;
using System.Collections.Generic;
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
        private FishingRodUIState FishingRodUI => _fishingRodInterface is null ? null : (FishingRodUIState)_fishingRodInterface.CurrentState;

        public bool IsFishingRodUIOpen => FishingRodUI is not null;

        private UserInterface _fishingRodInterface;

        // On Mod Load
        public override void Load()
        {
            _fishingRodInterface = new UserInterface();
        }

        // On World Unload (just in case it is open)
        public override void OnWorldUnload()
        {
            CloseFishingRodUI();
        }

        public void OpenFishingRodUI(ShootMode shootMode, TrailColorMode trailColorMode, TrailTypeMode trailTypeMode, int hotbarIndex)
        {
            SoundEngine.PlaySound(SoundID.MenuOpen);
            _fishingRodInterface.SetState(new FishingRodUIState(shootMode, trailColorMode, trailTypeMode, hotbarIndex)); // setState calls Activate() on the new ui
        }

        public void CloseFishingRodUI()
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            _fishingRodInterface.SetState(null);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer($"{nameof(GarnsMod)}: Fishing Rod UI", DrawFishingRodInterface, InterfaceScaleType.UI));
            }
        }

        // Helper method so ModifyinterfaceLayers isn't as cluttered
        private bool DrawFishingRodInterface()
        {
            _fishingRodInterface.Draw(Main.spriteBatch, new GameTime());
            return true;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            _fishingRodInterface?.Update(gameTime);
        }
    }
}
