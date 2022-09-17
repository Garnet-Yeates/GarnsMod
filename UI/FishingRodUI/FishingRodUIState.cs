using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using GarnsMod.Content.Items.Tools;
using static GarnsMod.Content.Items.Tools.GarnsFishingRod;
using GarnsMod.Tools;
using Terraria.ModLoader.UI.Elements;
using Terraria.GameContent.UI.Elements;

namespace GarnsMod.UI.FishingRodUI
{
    internal class FishingRodUIState : UIState
    {
        private UIHoverImageButton TrailColorButton;
        private UIHoverImageButton TrailTypeButton;
        private UIHoverImageButton ShootModeButton;

        public Vector2 Origin { get; private set; }
        public int InventoryIndex { get; private set; }
        public TrailColorMode SelectedTrailColorMode { get; private set; }
        public TrailTypeMode SelectedTrailTypeMode { get; private set; }
        public ShootMode SelectedShootMode { get; private set; }

        public FishingRodUIState(ShootMode shootMode, TrailColorMode trailColorMode, TrailTypeMode trailTypeMode, int inventoryIndex)
        {
            Origin = MainHelpers.MouseScreenWithoutZoom();
            SelectedShootMode = shootMode;
            SelectedTrailColorMode = trailColorMode;
            SelectedTrailTypeMode = trailTypeMode;
            InventoryIndex = inventoryIndex;
        }

        public override void OnInitialize()
        {
            TrailColorButton = new UIHoverImageButton(SelectedTrailColorMode.TextureAsset, $"Trail Color: {SelectedTrailColorMode.Name}");
            TrailColorButton.Width.Set(38, 0);
            TrailColorButton.Height.Set(38, 0);
            TrailColorButton.OnClick += TrailColorButton_OnClick;
            Append(TrailColorButton);

            TrailTypeButton = new UIHoverImageButton(SelectedTrailTypeMode.TextureAsset, $"Trail Type: {SelectedTrailTypeMode.Name}");
            TrailTypeButton.Width.Set(38, 0);
            TrailTypeButton.Height.Set(38, 0);
            TrailTypeButton.OnClick += TrailTypeButton_OnClick;
            Append(TrailTypeButton);

            ShootModeButton = new UIHoverImageButton(SelectedShootMode.TextureAsset, $"Shoot Mode: {SelectedShootMode.Name}");
            ShootModeButton.Width.Set(38, 0);
            ShootModeButton.Height.Set(38, 0);
            ShootModeButton.OnClick += ShootModeButton_OnClick;
            Append(ShootModeButton);

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

        private void ShootModeButton_OnClick(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.Item10);
            SelectedShootMode = ((int)SelectedShootMode + 1) % ShootMode.Count;
            if (Main.player[Main.myPlayer].HeldItem.ModItem is GarnsFishingRod rod)
            {
                rod.shootMode = SelectedShootMode;
            }
            RefreshButtons();
        }

        internal static void Close()
        {
            ModContent.GetInstance<FishingRodUISystem>().CloseFishingRodUI();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime); // don't remove or else the Update() call doesn't propagate meaning UIHoverImageButton won't prevent other actions

            Player myPlayer = Main.player[Main.myPlayer];

            if (myPlayer.HeldItem.ModItem is not GarnsFishingRod || myPlayer.selectedItem != InventoryIndex)
            {
                Close();
            }
        }

        public static readonly int ButtonSize = 38; // Should correspond to the texture size since the draw size is based on the texture size

        // Makes sure that the bu
        private void RefreshButtons()
        {
            float half = ButtonSize / 2f;
            float space = half * 2f;

            TrailColorButton.SetImage(SelectedTrailColorMode.TextureAsset);
            TrailColorButton.HoverText = $"Trail Color: {SelectedTrailColorMode.Name}";
            TrailColorButton.Left.Set(Origin.X - half + space, 0f);
            TrailColorButton.Top.Set(Origin.Y - half, 0f);
            TrailColorButton.Recalculate();

            TrailTypeButton.SetImage(SelectedTrailTypeMode.TextureAsset);
            TrailTypeButton.HoverText = $"Trail Type: {SelectedTrailTypeMode.Name}";
            TrailTypeButton.Left.Set(Origin.X - half - space, 0f);
            TrailTypeButton.Top.Set(Origin.Y - half, 0f);
            TrailTypeButton.Recalculate();

            ShootModeButton.SetImage(SelectedShootMode.TextureAsset);
            ShootModeButton.HoverText = $"Shoot Mode: {SelectedShootMode.Name}";
            ShootModeButton.Left.Set(Origin.X - half, 0f);
            ShootModeButton.Top.Set(Origin.Y - half - space, 0f);
            ShootModeButton.Recalculate();
        }
    }
}