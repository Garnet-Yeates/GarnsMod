using KokoLib;
using Microsoft.Xna.Framework;
using System;
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
            Item.width = 50;
            Item.height = 30;
            Item.scale = 0.75f;
            Item.rare = ItemRarityID.Green;

            // Use Properties
            Item.useTime = 6;
            Item.useAnimation = 6;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;

            // Weapon Properties
            Item.DamageType = DamageClass.Ranged;
            Item.damage = 20;
            Item.knockBack = 0f;
            Item.noMelee = true;
            Item.UseSound = SoundID.Item40;

            // Gun Properties
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 8f;
            Item.useAmmo = AmmoID.Bullet;
        }

        public const float ChargeMax = 30 * 60; // 30 seconds

        private float ChargeProgress => currentCharge / ChargeMax;

        // Goes up every tick the item is being used
        private int currentCharge = 0;

        // If chargeTimeout hits 0, we lose our charge progress. Goes down every tick
        private int chargeTimeout = Grace;

        // This is what charge timeout is set to when you stop using the item. You are given 'grace' ticks to use the item again before it times out
        private const int Grace = 20;

        public const float ChargedUseSpeedMultiplier = 2;
        public const float ChargedVelocityMultiplier = 1.5f;

        // This method lets you adjust position of the gun in the player's hands. Play with these values until it looks good with your graphics.
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(0f, 0f);
        }

        public override void UpdateInventory(Player player)
        {
            if (--chargeTimeout == 0)
            {
                currentCharge = 0;
            }
        }

        // Every tick we use the item, our charge goes up by 1 and we reset chargeTimeout to grace
        public override void UseItemFrame(Player player)
        {
            chargeTimeout = Grace;
            currentCharge++;
            if (currentCharge > ChargeMax)
            {
                currentCharge = (int)ChargeMax;
            }
        }

        public override float UseSpeedMultiplier(Player player)
        {
            return 1f + ChargeProgress * (ChargedUseSpeedMultiplier - 1f);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            velocity *= 1f + ChargeProgress * (ChargedVelocityMultiplier - 1f);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Update source and give context before we plug it into Projectile.NewProjectile

            if (ContentSamples.ItemsByType[source.AmmoItemIdUsed].ammo == AmmoID.Arrow)
            {
                source = new(source.Entity, source.Item, source.AmmoItemIdUsed, "GarnGunArrow");
                velocity *= 2;
            }

            float spread = MathHelper.ToRadians(5) / 2; // Total arc size, in degrees
            int numShot = 5;
            for (int i = 0; i < numShot; i++)
            {
                float rot = Utils.AngleLerp(-spread, spread, (float)i / numShot);
                Projectile.NewProjectile(source, position, velocity.RotatedBy(rot), type, damage, knockback, player.whoAmI);
            }

            return false;
        }

        public override bool? CanChooseAmmo(Item ammoItem, Player player)
        {
            int[] alsoAcceptAmmoTypes = { AmmoID.Arrow };

            Item weapon = Item; // The Item that this ModItem is attached to is the weapon
            return ammoItem.ammo == weapon.useAmmo || alsoAcceptAmmoTypes.Contains(ammoItem.ammo);
        }
    }

    public class GarnGunSpawnedProjectile : GlobalProjectile
    {
        // Unlike ModProjectile.OnSpawn, GlobalProjectile.OnSpawn is called on all clients (not just owner)
        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (source is EntitySource_ItemUse_WithAmmo { Context: "GarnGunArrow" })
            {
                if (projectile.extraUpdates < 1)
                {
                    projectile.extraUpdates = 1;
                    Net<IExtraUpdateSyncer>.Proxy.SyncExtraUpdates(projectile, projectile.extraUpdates);
                }

            }
        }

        public interface IExtraUpdateSyncer
        {
            void SyncExtraUpdates(Projectile p, int extraUpdates);

            private class GarnsFishingRPGPlayerHandler : ModHandler<IExtraUpdateSyncer>, IExtraUpdateSyncer
            {
                public override IExtraUpdateSyncer Handler => this;

                public void SyncExtraUpdates(Projectile p, int extraUpdates)
                {
                    p.extraUpdates = extraUpdates;
                }
            }
        }
    }



    // This class makes it so that:
    // - Left clicking shoots bullet
    // - Right clicking shoots stynger bolt
    //
    // It does some slightly advanced engine logic to make it so that when ItemSlots are drawn, the displayed ammunition count shows [stynger bolt counts + bullet counts] instead of [bullet counts],
    // with a weird [stynger bolt count] for one frame after a stynger bolt is shot
    public class GarnGunHackyLogic : ModItem
    {
        public override string Texture => $"{nameof(GarnsMod)}/Content/Items/Weapons/Ranged/GarnGun";

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("This is a modded gun.");
            DisplayName.SetDefault("Test");
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            // Common Properties
            Item.width = 50;
            Item.height = 30;
            Item.scale = 0.75f;
            Item.rare = ItemRarityID.Green;

            // Use Properties
            Item.useTime = 6;
            Item.useAnimation = 6;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;

            // Weapon Properties
            Item.DamageType = DamageClass.Ranged;
            Item.damage = 20;
            Item.knockBack = 0f;
            Item.noMelee = true;
            Item.UseSound = SoundID.Item40;

            // Gun Properties
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 8f;
            Item.useAmmo = AmmoID.Bullet;
        }

        // Allow right-click use
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        // This class does a lot of "engine dancing" if you will. CanChooseAmmo hooks are called in many different parts of the engine (One to display ammo counts, one to decide if item can be used (ItemCheck_CheckCanUse),
        // and one to pick the ammo that is actually shot (PickAmmo, inside ItemCheck_Shoot) as well as in many other places (seeing it a lot in Projectile.cs). We use withinShootLogic flag to figure out where we are
        // in the engine when these hooks are called. This allows us to make ammo counts display combinations of ammos, while making shoot logic restricted to using one type of ammo based on alt function. 
        private bool withinShootLogic;

        // CanShoot is the first hook called within ItemCheck_Shoot
        public override bool CanShoot(Player player)
        {
            withinShootLogic = true;
            return true;
        }

        // So when ItemCheck_CheckCanUse happens, it is outside of shoot logic (happens before ItemCheck_Shoot), meaning if the player has stynger bolt OR bullet in their inventory, it will allow the item to be used
        // HOWEVER it is not guaranteed that it will shoot anything when it's used (what if they have stynger bolts in their inv, but 0 bullets, and they left click to shoot a bullet? What will
        //    happen is that ItemCheck_CheckCanUse will allow it to be used, but once we are in the shoot logic, we narrow down what can actually be shot depending on alt function so it might shoot nothing.
        //    this creates a visual issue of the gun being held out trying to be used, but ItemCheck_Shoot determines it can't be shot
        // Having this check here actually makes sure the item won't be used if it is going to shoot nothing. We are actually using ItemLoader which is what tModLoader uses to
        public override bool CanUseItem(Player player)
        {
            withinShootLogic = true;
            bool found = false;
            for (int i = 0; i < 58; i++)
                if (player.inventory[i].type != ItemID.None && player.inventory[i].ammo != 0)
                    found |= ItemLoader.CanChooseAmmo(Item, player.inventory[i], player);
            withinShootLogic = false;
            return found;
        }


        public override bool? CanChooseAmmo(Item ammoItem, Player player)
        {
            // CanChooseAmmo is used within shoot logic and outside. Outside of shoot logic it is used to display ammunition counts. We want it to always show ammunition counts for BOTH ammos, but DURING the shoot
            // WithinShootLogic is used to determine if this hook is being called within the "actual shooting" logic or within the "show ammo counts / check if theyre able to use the item" logic.

            // If we do altFunction check outside of shoot logic it will cause visual issues (basically, it will ONLY display the left click (altFunc ==0) ammunition, because alt function
            // will always be 0 when it is using this hook to display ammunition counts)
            // So when we are outside of shoot logic, we allow both (so it displays both)
            if (!withinShootLogic)
            {
                // here we allow both, so the gun is able to be used
                // This will get called before trying to use the item (this is how the engine checks if the item is actually able to be used. It makes sure it can find ammo based on CanChooseAmmo)
                return ammoItem.ammo == AmmoID.Bullet || ammoItem.type == ItemID.StyngerBolt;
            }

            // Down here we are within shoot logic

            // if altFunctionUse is 0, we only allow bullets
            if (player.altFunctionUse == 0)
            {
                return ammoItem.ammo == AmmoID.Bullet;
            }

            // if altfunctionuse is 2, we only allow stynger bolts
            return ammoItem.type == ItemID.StyngerBolt;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            withinShootLogic = false;
            return true;
        }
    }
}
