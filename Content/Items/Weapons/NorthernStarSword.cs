using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarnsMod.Content.Projectiles;
using GarnsMod.Tools;
using log4net.Core;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using static GarnsMod.Tools.ColorGradient;
using Terraria.ID;
using Terraria.ModLoader;

namespace GarnsMod.Content.Items.Weapons
{
    internal class NorthernStarSword : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Northern Starsword"); // The English name of the projectile
            Tooltip.SetDefault("Fires Northern Lights that descend to deal 10x damage");
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

            Item.shoot = ModContent.ProjectileType<NorthernStar>(); // ID of the projectiles the sword will shoot
            Item.shootSpeed = 15f; // Speed of the projectiles the sword will shoot // used to be 15
        }

        public static readonly List<Color> StarColors = new() { RainbowColors[5], RainbowColors[6], RainbowColors[7] };

        public static Dictionary<int, ColorGradient> NorthStarColorGradients = InitStarColorGradients();

        // Doesn't need to be synced as it affects calls to Shoot() which is client-sided
        private byte currentColor;

        public static Dictionary<int, ColorGradient> InitStarColorGradients()
        {
            Dictionary<int, ColorGradient> dict = new();
            for (int i = 0; i < StarColors.Count; i++)
            {
                dict.Add(i, FromCollectionWithStartIndex(StarColors, i, extraStart: 1, extraLoops: 0));
            }
            return dict;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int amount = 1;

            // Spread starts at MinSpread and scales up to MaxSpread depending on fishing rod level
            float spread = 12;

            Vector2 current = velocity.RotatedBy(MathHelper.ToRadians(spread / 2));

            float increment = MathHelper.ToRadians(-spread / (amount - 1));

            for (int i = 0; i < amount; ++i)
            {
                // Generate new bobbers
                Vector2 vel = current;

                NorthernStar p = (NorthernStar) Projectile.NewProjectileDirect(source, position, vel, type, damage, knockback, player.whoAmI).ModProjectile;
                p.starColorIndex = currentColor;
                NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p.Projectile.whoAmI);

                current = current.RotatedBy(increment);

            }

            currentColor = (byte) ((currentColor + 1) % StarColors.Count);

            return false;
        }
    }
}
