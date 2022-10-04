using GarnsMod.Content.Mechanics.AlternatingAmmoMechanic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace GarnsMod.UI.AlternatingAmmoUI
{
    internal class AlternatingAmmoUIState : UIState
    {
        private UIHoverImageButton ModeButton;

        public AlternatingAmmoMode SelectedAlternatingAmmoMode { get; private set; }

        public static readonly int ButtonSize = 76; // Should correspond to the texture size since the draw size is based on the texture size

        private Vector2 Origin { get; set; }

        public void CalculateOrigin()
        {
            Vector2 newOrigin = new(0, Main.screenHeight - ButtonSize);
            if (newOrigin != Origin)
            {
                Origin = newOrigin;
                RefreshButtons();
            }
        }

        public override void Update(GameTime gameTime)
        {
            CalculateOrigin();
        }

        public override void OnInitialize()
        {
            SelectedAlternatingAmmoMode = Main.LocalPlayer.GetModPlayer<AlternatingAmmoPlayer>().Mode;

            ModeButton = new UIHoverImageButton(SelectedAlternatingAmmoMode.TextureAsset, $"Trail Color: {SelectedAlternatingAmmoMode.DisplayName}");
            ModeButton.OnClick += ModeButton_OnClick;
            RefreshButtons();

            Append(ModeButton);
        }

        private void ModeButton_OnClick(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.Item10);
            SelectedAlternatingAmmoMode = ((int)SelectedAlternatingAmmoMode + 1) % AlternatingAmmoMode.Count;
            Main.LocalPlayer.GetModPlayer<AlternatingAmmoPlayer>().Mode = SelectedAlternatingAmmoMode;
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            ModeButton.SetImage(SelectedAlternatingAmmoMode.TextureAsset);
            ModeButton.HoverText = $"Ammo Alternating Mode: {SelectedAlternatingAmmoMode.DisplayName}";
            ModeButton.Width.Set(ButtonSize, 0);
            ModeButton.Height.Set(ButtonSize, 0);
            ModeButton.Left.Set(Origin.X, 0f);
            ModeButton.Top.Set(Origin.Y, 0f);
            ModeButton.Recalculate();
        }
    }
}