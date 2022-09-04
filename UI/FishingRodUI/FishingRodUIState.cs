using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using GarnsMod.Content.Items.Tools;
using static GarnsMod.Content.Items.Tools.GarnsFishingRod;

namespace GarnsMod.UI.FishingRodUI
{
    internal class FishingRodUIState : UIState
    {
        private UIHoverImageButton TrailColorButton;
        private UIHoverImageButton TrailTypeButton;

        public bool Visible { get; private set; }
        public Vector2 Origin { get; private set; }
        public int HotbarNum { get; private set; }
        public TrailColorMode SelectedTrailColorMode { get; private set; }
        public TrailTypeMode SelectedTrailTypeMode { get; private set; }
        public ShootMode SelectedShootMode { get; private set; }

        public override void OnInitialize()
        {
            Origin = new(-100, -100);
            Visible = false;

            TrailColorButton = new UIHoverImageButton(TrailColorMode.SingleColor.TextureAsset, TrailColorMode.SingleColor.Name);
            TrailColorButton.Width.Set(38, 0);
            TrailColorButton.Height.Set(38, 0);
            TrailColorButton.OnClick += TrailColorButton_OnClick;
            SelectedTrailColorMode = (TrailColorMode)0;
            Append(TrailColorButton);

            TrailTypeButton = new UIHoverImageButton(TrailTypeMode.Plain.TextureAsset, TrailTypeMode.Plain.Name);
            TrailTypeButton.Width.Set(38, 0);
            TrailTypeButton.Height.Set(38, 0);
            TrailTypeButton.OnClick += TrailTypeButton_OnClick;
            SelectedTrailTypeMode = (TrailTypeMode)0;
            Append(TrailTypeButton);

            RefreshButtons();
        }

        private void TrailColorButton_OnClick(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.Item10);
            SelectedTrailColorMode = ((int)SelectedTrailColorMode + 1) % TrailColorMode.Count;
            if (Main.player[Main.myPlayer].HeldItem.ModItem is GarnsFishingRod rod)
            {
                rod.trailColorMode = SelectedTrailColorMode;
            }
            RefreshButtons();
        }

        private void TrailTypeButton_OnClick(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.Item10);
            SelectedTrailTypeMode = ((int)SelectedTrailTypeMode + 1) % TrailTypeMode.Count;
            if (Main.player[Main.myPlayer].HeldItem.ModItem is GarnsFishingRod rod)
            {
                rod.trailTypeMode = SelectedTrailTypeMode;
            }
            RefreshButtons();
        }

        internal void SetVisible(ShootMode shootMode, TrailColorMode colorMode, TrailTypeMode typeMode, int hotbarOffset)
        {
            SoundEngine.PlaySound(SoundID.MenuOpen);
            HotbarNum = hotbarOffset;
            SelectedTrailColorMode = colorMode;
            SelectedTrailTypeMode = typeMode;
            SelectedShootMode = shootMode;
            Origin = Main.MouseScreen;
            Visible = true;
            RefreshButtons(); // Refresh buttons so their texture changes and they move to the new origin
        }

        internal void SetInvisible()
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            Visible = false;
            Origin = new(-1000, -1000);
            RefreshButtons(); // Refresh buttons so they move to the new origin
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime); // don't remove.

            Player myPlayer = Main.player[Main.myPlayer];

            if (Visible && (myPlayer.HeldItem.ModItem is not GarnsFishingRod || myPlayer.selectedItem != HotbarNum))
            {
                SetInvisible();
            }
        }

        public static readonly int ButtonSize = 38; // Should correspond to the texture size since the draw size is based on the texture size

        private void RefreshButtons()
        {
            float half = ButtonSize / 2f;
            float space = half * 2f;

            if (TrailColorButton is not null)
            {
                TrailColorButton.SetImage(SelectedTrailColorMode.TextureAsset);
                TrailColorButton.HoverText = $"Trail Color: {SelectedTrailColorMode.Name}";

                TrailColorButton.Left.Set(Origin.X - half + space, 0f);
                TrailColorButton.Top.Set(Origin.Y - half, 0f);

                TrailColorButton.Recalculate();
            }

            if (TrailTypeButton is not null)
            {
                TrailTypeButton.SetImage(SelectedTrailTypeMode.TextureAsset);
                TrailTypeButton.HoverText = $"Trail Type: {SelectedTrailTypeMode.Name}";

                TrailTypeButton.Left.Set(Origin.X - half - space, 0f);
                TrailTypeButton.Top.Set(Origin.Y - half, 0f);

                TrailTypeButton.Recalculate();
            }
        }
    }
}