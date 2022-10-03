using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using GarnsMod.Content.Mechanics;

namespace GarnsMod.UI.AlternatingAmmoUI
{
    internal class AlternatingAmmoUIState : UIState
    {
        private UIHoverImageButton ModeButton;

        public AlternatingAmmoMode SelectedAlternatingAmmoMode { get; private set; }

        public static readonly int ButtonSize = 76; // Should correspond to the texture size since the draw size is based on the texture size

        Vector2 Origin { get; set; }

        public void CalculateOrigin()
        {
            Origin = new Vector2(0, Main.screenHeight - ButtonSize);
            RefreshButtons();
        }

        public override void OnInitialize()
        {
            SelectedAlternatingAmmoMode = Main.LocalPlayer.GetModPlayer<AlternatingAmmoPlayer>().SelectedAlternatingAmmoMode;

            ModeButton = new UIHoverImageButton(SelectedAlternatingAmmoMode.TextureAsset, $"Trail Color: {SelectedAlternatingAmmoMode.DisplayName}");
            ModeButton.OnClick += ModeButton_OnClick;
            RefreshButtons();

            Append(ModeButton);
        }

        private void ModeButton_OnClick(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.Item10);
            SelectedAlternatingAmmoMode = ((int)SelectedAlternatingAmmoMode + 1) % AlternatingAmmoMode.Count;
            Main.LocalPlayer.GetModPlayer<AlternatingAmmoPlayer>().SelectedAlternatingAmmoMode = SelectedAlternatingAmmoMode;
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

    internal readonly struct AlternatingAmmoMode
    {
        internal static List<AlternatingAmmoMode> ammoModes = new();

        public static int Count => ammoModes.Count;

        public static readonly AlternatingAmmoMode Disabled = new("Disabled", ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/UI/AlternatingAmmoUI/AlternatingMode_Disabled"));
        public static readonly AlternatingAmmoMode Alternate = new("Cycle through ammo", ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/UI/AlternatingAmmoUI/AlternatingMode_ByType"));
        public static readonly AlternatingAmmoMode Alternate_PreserveRatio = new("Cycle through ammo, keeping ratios", ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/UI/AlternatingAmmoUI/AlternatingMode_ByRatio"));

        internal int Value { get; }
        internal string DisplayName { get; }
        internal Asset<Texture2D> TextureAsset { get; }

        private AlternatingAmmoMode(string name, Asset<Texture2D> asset)
        {
            Value = Count;
            TextureAsset = asset;
            DisplayName = name;
            ammoModes.Add(this);
        }

        public static explicit operator int(AlternatingAmmoMode m) => m.Value;

        public static implicit operator AlternatingAmmoMode(int i) => ammoModes[i];

        public static bool operator ==(AlternatingAmmoMode m1, AlternatingAmmoMode m2) => m1.Value == m2.Value;
        public static bool operator !=(AlternatingAmmoMode m1, AlternatingAmmoMode m2) => m1.Value != m2.Value;

        public override int GetHashCode() => Value;
    }
}