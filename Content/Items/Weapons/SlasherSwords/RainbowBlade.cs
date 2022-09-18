using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace GarnsMod.Content.Items.Weapons.SlasherSwords
{
    internal class RainbowBlade : ModItem, ISlasherSword
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Garn's Blade");

            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.damage = 40;
            Item.useTime = 100;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.useTurn = true;

            Item.UseSound = null;

            Item.width = 66;
            Item.height = 66;

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
            float xOff, yOff, angleProgress = SlasherSword.GetAngleProgress(player);

            if (angleProgress < 0.25f)
            {
                xOff = MathHelper.Lerp(-6, -3, angleProgress * (1f / 0.25f));
                yOff = MathHelper.Lerp(2, 2, angleProgress * (1f / 0.25f));
            }
            else if (angleProgress < 0.55f)
            {
                xOff = MathHelper.Lerp(-3, 0, (angleProgress - 0.25f) * (1f / 0.3f));
                yOff = MathHelper.Lerp(2, 0f, (angleProgress - 0.25f) * (1f / 0.3f));
            }
            else
            {
                xOff = MathHelper.Lerp(0, 0, (angleProgress - 0.55f) * (1f / 0.45f));
                yOff = MathHelper.Lerp(0, -3f, (angleProgress - 0.55f) * (1f / 0.45f));
            }

            return new(xOff * player.direction, yOff);
        }

        #endregion
    }
}


