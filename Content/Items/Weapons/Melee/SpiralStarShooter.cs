using GarnsMod.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static GarnsMod.CodingTools.ColorGradient;

namespace GarnsMod.Content.Items.Weapons.Melee
{
    internal class SpiralStarShooter : ModItem
    {
        public override string Texture => $"{nameof(GarnsMod)}/Content/Items/Weapons/Melee/NorthernStarSword";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spiral Starsword");
            Tooltip.SetDefault("Fires fast stars that move in a helix. \nDNA, bitch");
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 42;

            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 20;
            Item.useAnimation = 12;
            Item.autoReuse = true;
            Item.DamageType = DamageClass.Melee;

            Item.damage = 120;
            Item.knockBack = 6;
            Item.crit = 6;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Pink;
            Item.UseSound = SoundID.Item1;

            Item.shoot = ModContent.ProjectileType<RainbowSpiralStar>();
            Item.shootSpeed = 20f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int randomIndex = Main.rand.Next(RainbowColors.Count);
            Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, player.whoAmI, 0.25f, randomIndex);
            Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, player.whoAmI, 0.75f, (randomIndex + 1) % RainbowColors.Count);
            return false;
        }
    }
}
