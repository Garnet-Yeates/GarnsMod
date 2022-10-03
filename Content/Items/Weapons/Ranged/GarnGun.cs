using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace GarnsMod.Content.Items.Weapons.Ranged
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
            Item.width = 62;
            Item.height = 32;
            Item.scale = 0.75f;
            Item.rare = ItemRarityID.Green;

            // Use Properties
            Item.useTime = BaseUseTime;
            Item.useAnimation = BaseUseAnimation;
            Item.reuseDelay = BaseReuseDelay;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;

            // Weapon Properties
            Item.DamageType = DamageClass.Ranged;
            Item.damage = 20;
            Item.knockBack = 0f;
            Item.noMelee = true;

            // Gun Properties
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = BaseShootSpeed;
            Item.useAmmo = AmmoID.Bullet;
        }

        // The amount of time it takes to fully charge the weapon, in seconds
        public const float ChargeTime = 30f; // 25 seconds

        public static int ChargeTimeTicks => (int)(ChargeTime * 60);

        private float ChargeProgress => currentCharge / (float)ChargeTimeTicks;

        // Goes up every tick the item is being used
        private int currentCharge = 0;

        // If chargeTimeout hits 0, we lose our charge progress. Goes down every tick
        private int chargeTimeout = Grace;

        // This is what charge timeout is set to when you stop using the item. You are given 'grace' ticks to use the item again before it times out
        private const int Grace = 20;

        public const int BaseUseAnimation = 24;
        public const int ChargedUseAnimation = 24;

        public const int BaseUseTime = 8;
        public const int ChargedUseTime = 5;

        public const int BaseReuseDelay = 24;
        public const int ChargedReuseDelay = 12;

        public const float BaseShootSpeed = 5f;
        public const float ChargedShootSpeed = 15f;


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
            return true;
        }

        public override void UpdateInventory(Player player)
        {
            if (--chargeTimeout == 0)
            {
                currentCharge = 0;
                Item.useTime = BaseUseTime;
                Item.reuseDelay = BaseReuseDelay;
                Item.shootSpeed = BaseShootSpeed;
                Item.useAnimation = BaseUseAnimation;
            }
        }

        public override bool? UseItem(Player player)
        {
            Main.NewText($"Charge: {currentCharge} ticks {currentCharge * 100 / ChargeTimeTicks}%");
            Item.useTime = (int)Math.Round(BaseUseTime - (BaseUseTime - ChargedUseTime) * ChargeProgress);
            Item.reuseDelay = (int)Math.Round(BaseReuseDelay - (BaseReuseDelay - ChargedReuseDelay) * ChargeProgress);
            Item.useAnimation = (int)Math.Round(BaseUseAnimation - (BaseUseAnimation - ChargedUseAnimation) * ChargeProgress);
            Item.shootSpeed = BaseShootSpeed - (BaseShootSpeed - ChargedShootSpeed) * ChargeProgress;

            return null;
        }

        public override void UseItemFrame(Player player)
        {
            // Make this client sided, doesn't need to be synced

            chargeTimeout = Grace;
            currentCharge++;
            if (currentCharge > ChargeTimeTicks)
            {
                currentCharge = ChargeTimeTicks;
            }
        }
    }

    class GarnGunGlobalAmmunition : GlobalItem
    {
        // Represents the available ammunitions that the GarnGun should be able to use, ordered by priority
        public static readonly List<int> AmmoPriorityList = new() { AmmoID.Bullet, AmmoID.Arrow };

        public override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            return lateInstantiation && AmmoPriorityList.Contains(item.ammo);
        }

        // Reject using lower priority ammos if the user has a higher priority ammo in their inventory
        public override bool? CanBeChosenAsAmmo(Item ammoType, Item weapon, Player player)
        {
            return null;
            if (weapon.type == ModContent.ItemType<GarnGun>())
            {
                foreach (int prioritizeAmmoType in AmmoPriorityList)
                {
                    if (ammoType.ammo == prioritizeAmmoType)
                    {
                        return true;
                    }
                    // Don't let it advance to the next iteration to try a lower prio ammo if we have a higher prio one in inventory
                    else if (player.inventory.Any(item => item.ammo == prioritizeAmmoType))
                    {
                        return false;
                    }

                    // If ammo.type isn't prioritizeAmmoType, and they don't have prioritizeAmmoType in their inventory, try the next highest priority
                    // AKA continue to the next iteration of this foreach
                }

                return false;
            }

            return null;
        }
    }


}
