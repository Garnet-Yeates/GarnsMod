using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace GarnsMod.Content.Items.Weapons
{
    public class GarnGun : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("This is a modded gun.");
            DisplayName.SetDefault("Garngun");
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            // Common Properties
            Item.width = 62; // Hitbox width of the item.
            Item.height = 32; // Hitbox height of the item.
            Item.scale = 0.75f;
            Item.rare = ItemRarityID.Green; // The color that the item's name will be in-game.

            // Use Properties
            Item.useTime = BaseUseTime; // The item's use time in ticks (60 ticks == 1 second.)
            Item.useAnimation = BaseUseAnimation; // The length of the item's use animation in ticks (60 ticks == 1 second.)
            Item.reuseDelay = BaseReuseDelay;
            Item.useStyle = ItemUseStyleID.Shoot; // How you use the item (swinging, holding out, etc.)
            Item.autoReuse = true; // Whether or not you can hold click to automatically use it again.

            // Weapon Properties
            Item.DamageType = DamageClass.Ranged; // Sets the damage type to ranged.
            Item.damage = 20; // Sets the item's damage. Note that projectiles shot by this weapon will use its and the used ammunition's damage added together.
            Item.knockBack = 0f; // Sets the item's knockback. Note that projectiles shot by this weapon will use its and the used ammunition's knockback added together.
            Item.noMelee = true; // So the item's animation doesn't do damage.

            // Gun Properties
            Item.shoot = ProjectileID.PurificationPowder; // For some reason, all the guns in the vanilla source have this.
            Item.shootSpeed = BaseShootSpeed; // The speed of the projectile (measured in pixels per frame.)
            Item.useAmmo = AmmoID.Bullet; // The "ammo Id" of the ammo item that this weapon uses. Ammo IDs are magic numbers that usually correspond to the item id of one item that most commonly represent the ammo type.
        }

        // The amount of time it takes to fully charge the weapon, in seconds
        public static readonly float ChargeTime = 30f; // 25 seconds

        public static int ChargeTimeTicks => (int)(ChargeTime * 60);

        private float ChargeProgress => charge / (float)ChargeTimeTicks;

        private int charge = 0;

        // If chargeTimeout hits 0, we lose our charge progress
        private int chargeTimeout = 3;

        public static readonly int BaseUseAnimation = 24;
        public static readonly int ChargedUseAnimation = 24;

        public static readonly int BaseUseTime = 8;
        public static readonly int ChargedUseTime = 5;

        public static readonly int BaseReuseDelay = 24;
        public static readonly int ChargedReuseDelay = 12;

        public static readonly float BaseShootSpeed = 5f;
        public static readonly float ChargedShootSpeed = 15f;


        // This method lets you adjust position of the gun in the player's hands. Play with these values until it looks good with your graphics.
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(2f, -2f);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
         //   type = ProjectileID.Cultist;
            position = Main.MouseWorld;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Main.NewText(velocity.Length());
            return true;
        }

        public override void UpdateInventory(Player player)
        {
            if (--chargeTimeout == 0)
            {
                Main.NewText("Charge lost");
                charge = 0;
                Item.useTime = BaseUseTime;
                Item.reuseDelay = BaseReuseDelay;
                Item.shootSpeed = BaseShootSpeed;
                Item.useAnimation = BaseUseAnimation;
            }
        }

        public override bool? UseItem(Player player)
        {
            // Make this client sided, doesn't need to be synced
            if (Main.myPlayer == player.whoAmI)
            {
                Main.NewText($"Charge: {charge} ticks {charge * 100 / ChargeTimeTicks}%");
                Item.useTime = (int)Math.Round(BaseUseTime - (BaseUseTime - ChargedUseTime) * ChargeProgress);
                Item.reuseDelay = (int)Math.Round(BaseReuseDelay - (BaseReuseDelay - ChargedReuseDelay) * ChargeProgress);
                Item.useAnimation = (int)Math.Round(BaseUseAnimation - (BaseUseAnimation - ChargedUseAnimation) * ChargeProgress);
                Item.shootSpeed = BaseShootSpeed - (BaseShootSpeed - ChargedShootSpeed) * ChargeProgress;
            }

            return null;
        }

        public override void UseItemFrame(Player player)
        {
            // Make this client sided, doesn't need to be synced
            if (Main.myPlayer == player.whoAmI)
            {
                chargeTimeout = 3;
                charge++;
                if (charge > ChargeTimeTicks)
                {
                    charge = ChargeTimeTicks;
                }
            }
        }
    }
}
