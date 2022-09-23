using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace GarnsMod.Content.Items.Weapons.Melee.SlasherSwords
{
    internal class GarnBlade : ModItem, ISlasherSword
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Garn's Blade");

            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }
        public override string Texture => $"{nameof(GarnsMod)}/Content/Items/Weapons/Melee/NorthernStarSword";

        public override void SetDefaults()
        {
            Item.damage = 40;
            Item.useTime = 100;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.useTurn = true;

            Item.UseSound = null;

            Item.width = 26;
            Item.height = 42;

            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 6;
            Item.crit = 12;

            Item.value = Item.buyPrice(gold: 10);
            Item.rare = ItemRarityID.Pink;
        }

        #region SlasherOverrides

        public float Offset => 0.25f;

        public bool CanResetImmunity { get; set; }

        public bool CanHitNPCYet { get; set; }

        public float HandRotationOffset => -15f;

        public ISlasherSword SlasherSword => this;

        public override bool? CanHitNPC(Player player, NPC target)
        {
            if (!SlasherSword.CanHitNPC(player))
            {
                return false;
            }

            return null;
        }

        public override void UseItemFrame(Player player)
        {
            SlasherSword.SlasherUseItemFrame(player);
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            SlasherSword.UseStyle(player);
        }

        public override void UseItemHitbox(Player player, ref Rectangle hitbox, ref bool noHitbox)
        {
            SlasherSword.UseItemHitbox(player, ref hitbox);
        }

        public Vector2 GetItemLocationOffset(Player player)
        {
            float angleProgress = SlasherSword.GetAngleProgress(player);
            Vector2 itemLocationOffset = new(0, MathHelper.Lerp(0, -4f, angleProgress * (1f / 0.25f)));
            if (angleProgress > 0.25) itemLocationOffset = new(MathHelper.Lerp(0, -6 * player.direction, (angleProgress - 0.25f) * (1f / 0.75f)), -4f);

            return itemLocationOffset;
        }

        #endregion
    }
}


