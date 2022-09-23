using GarnsMod.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using static GarnsMod.CodingTools.ColorGradient;
using Terraria.ModLoader;

namespace GarnsMod.Content.Items.Weapons.Melee.SlasherSwords
{
    internal class RainbowBlade : ModItem, ISlasherSword
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Garn's Rainbow Blade");

            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.damage = 40;
            Item.useTime = 52;
            Item.useAnimation = 26;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.useTurn = true;

            Item.shoot = ModContent.ProjectileType<RainbowSpiralStar>();

            Item.UseSound = null;

            Item.width = 66;
            Item.height = 66;
            Item.shootSpeed = 20f;

            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 6;
            Item.crit = 12;

            Item.value = Item.buyPrice(gold: 10);
            Item.rare = ItemRarityID.Pink;
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            if (!Main.dedServ)
            {
                Color col = Main.hslToRgb(Main.GlobalTimeWrappedHourly % 1f, 1f, 0.5f);

                for (int i = 0; i < 1; i++)
                {
                    Dust rainbowDust = Dust.NewDustDirect(hitbox.TopLeft(), hitbox.Width, hitbox.Height, DustID.RainbowTorch, 0f, 0f, 0, col, 0.75f + Main.rand.NextFloat() * 0.5f);
                    rainbowDust.noGravity = true;
                }
                Lighting.AddLight(hitbox.Center(), col.ToVector3());
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int randomIndex = Main.rand.Next(RainbowColors.Count);
            Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, player.whoAmI, 0.25f, randomIndex);
            Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, player.whoAmI, 0.75f, (randomIndex + 1) % RainbowColors.Count);
            return false;
        }

        #region SlasherOverrides

        public float Offset => 0f;

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

        public override bool CanUseItem(Player player)
        {
            return base.CanUseItem(player);
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


