using GarnsMod.UI.FishingRodUI;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace GarnsMod.UI.AlternatingAmmoUI
{
    internal class AlternatingAmmoUISystem : ModSystem
    {
        private UserInterface _alternatingAmmoInterface;

        public AlternatingAmmoUIState UIState => _alternatingAmmoInterface?.CurrentState as AlternatingAmmoUIState;  

        public override void Load()
        {
            _alternatingAmmoInterface = new UserInterface();
        }

        public override void OnWorldLoad()
        {
            _alternatingAmmoInterface.SetState(new AlternatingAmmoUIState()); // setState calls Activate() on the new ui
        }

        public override void OnWorldUnload()
        {
            _alternatingAmmoInterface.SetState(null);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer($"{nameof(GarnsMod)}: Alternating Ammo UI", DrawAlternatingAmmoUI, InterfaceScaleType.UI));
            }
        }

        // Helper method so ModifyinterfaceLayers isn't as cluttered
        private bool DrawAlternatingAmmoUI()
        {
            _alternatingAmmoInterface.Draw(Main.spriteBatch, new GameTime());
            return true;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            _alternatingAmmoInterface?.Update(gameTime);
        }
    }
}
