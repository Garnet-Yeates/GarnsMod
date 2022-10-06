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
    // It does some slightly advanced engine hacky-logic to control exactly which calls to ItemLoader.CanChooseAmmo are affected. This make it so that when ItemSlots are drawn,
    // the displayed ammunition count shows [stynger bolt counts + bullet counts] instead of [bullet counts], with a weird [stynger bolt count] for one frame after a stynger bolt is shot
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

        // Allow alt function use
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        // This class does a lot of "engine dancing" if you will. CanChooseAmmo hooks are called in many different parts of the engine (One to display ammo counts, one to decide if item can be used (ItemCheck_CheckCanUse),
        // and one to pick the ammo that is actually shot (PickAmmo, inside ItemCheck_Shoot) as well as in many other places (seeing it a lot in Projectile.cs). We use withinShootLogic flag to figure out where we are
        // in the engine when these hooks are called. This allows us to make CanChooseAmmo behave differently based on where it is called (i.e for ammo counts: display combinations of ammos, for shooting/using item logic:
        // restrict: to using one type of ammo / item based on alt function. Set to true at the beginning of ItemUse_CheckItemUse and will be set to false when Shoot() hook is called (or timeout if shoot hook is never called)
        // 'withinShootLogic' means that we are somewhere in ItemCheck_CheckCanUse or somewhere in ItemCheck_Shoot (ItemCheck_Shoot is procedurally called a bit after ItemCheck_CheckCanUse)
        private bool withinShootLogic;

        // withinShootLogic should naturally be set to false when Shoot() is called (end of shoot logic). However, after the initial CanUseItem where it is set to true, there are things
        // that can happen along the way that cause Shoot() to not be called. Mainly it could be a different CanUseItem() hook returning false at the beginning of
        // ItemCheck_CheckCanUse, or it could be the CanShoot() hook returning false at the beginning of ItemCheck_Shoot. It could also be from the item not having the ammo required to shoot which would also cause
        // ItemCheck_Shoot to fail. Either way we do not want to be considered to be 'inside' shoot logic for a long period, so this is to ensure of that. (if we stay 'inside' shoot logic, the item slot will display
        // the counts for the main ammo if altFunction0 was used last, or they will display the count for the alternate ammo if altFunction2 was used last. We want it to display the 'combined' ammunitions.
        // This also comes with an amazing side effect of showing [0] when you run out of either main ammo or alt ammo and are trying to shoot said ammo, instead of showing [mainAmmo + altAmmo]
        private int shootLogicTimeout = 0;

        // For all intents and purposes, this is exactly equivalent to 'player.altFunctionUse == 2'. We set it at the beginning of item use, and it times out shortly after item use (similar to how withinShootLogic times out)
        // We want to keep 'usingAltFunction' true for a little bit after shoot logic ends if it was initially true (which is why it times out with withinShootLogic, as opposed to letting vanilla set it back to 0 immediately after item use).
        // This is so if shoot logic ends pre-emptively (some time after CanUseItem hook, but before Shoot hook), during the time that withinShootLogic has not timed out yet, it temporarily shows us the ammo count of
        // [alt ammo] as opposed to showing [main ammo] <= (Normally player.altFunctionUse would always be 0 inside my CanChooseAmmo during the time period where withinShootLogic is still true due to shoot logic ending early,
        // but we save it for a bit)
        private bool usingAltFunction;

        // We want to be 'withinShootLogic' for  when:
        // ItemCheck_CheckCanUse is called in tModloader source
        // ItemCheck_Shoot is called in tModloader source. 
        public override bool CanUseItem(Player player)
        {
            withinShootLogic = true;
            usingAltFunction = player.altFunctionUse == 2;
            shootLogicTimeout = 25;
            return true;
        }

        public override void UpdateInventory(Player player)
        {
            // These two flags are set at the very beginning of item use, and are naturally set to false at the very end of the shoot logic (when Shoot hook is called). 
            // However it is possible that shoot logic ended early without Shoot hook being called. This is why we have them time out here. If they never timed out, it would
            // mess with the displayed ammunition counts because it *thinks* we're in "shoot logic" but we're really in "display ammo counts logic"
            if (--shootLogicTimeout == 0)
            {
                usingAltFunction = false;
                withinShootLogic = false;
            }
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
            if (!usingAltFunction)
            {
                return ammoItem.ammo == AmmoID.Bullet;
            }

            // if altfunctionuse is 2, we only allow stynger bolts
            return ammoItem.type == ItemID.StyngerBolt;
        }

        // We reset our withinShootLogic and usingAltFunction here normally (this is where I consider the "shoot logic" to be over). It is not guaranteed that
        // Shoot() will be called every time ItemCheck_CheckCanUse is called (i.e if they have no ammo left based on the alt function), so in that case it times
        // out after 25 ticks
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            withinShootLogic = false;
            usingAltFunction = false;
            return true;
        }
    }
}
