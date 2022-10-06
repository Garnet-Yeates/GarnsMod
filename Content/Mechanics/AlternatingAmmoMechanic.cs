using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

// This .cs file acts like its own folder (has it's on sub-namespace under Mechanics namespace)
namespace GarnsMod.Content.Mechanics.AlternatingAmmoMechanic
{
    internal static class AlternatingAmmoMechanic
    {
        public static class Sets
        {
            public static bool[] NonAmmoAlternatingItems { get; private set; }

            public static void SetStaticDefaults()
            {
                // Base array capacity off some other random ItemID set array
                NonAmmoAlternatingItems = new bool[ItemID.Sets.IsAMaterial.Length];
            }
        }

    }

    internal class AlternatingAmmoSystem : ModSystem
    {
        public override void SetStaticDefaults() => AlternatingAmmoMechanic.Sets.SetStaticDefaults();
    }


    internal class ZZZ : ModPlayer
    {
        public override bool CanShoot(Item item)
        {
            return true;
        }
    }


    // Creates a client-sided timer (never synced) as a means of rotating through ammo types
    internal class AlternatingAmmoPlayer : ModPlayer
    {
        public AlternatingAmmoMode Mode { get; set; }

        public bool AlternatingDisabled => Mode == AlternatingAmmoMode.Disabled;

        // All of the ammo item id's that are available to be used for the current weapon. Calculated right after the weapon shoots. Used to restrict the type of ammo we can use next shot
        public int[] AmmoPool { get; private set; }

        // Increments by one after the Pool is recalculated (every time a weapon is shot). This is what 'cycles' through the pool to 'alternate' our current ammo
        public int CurrPoolIndex = 0;


        // We set AmmoPool to null at the very beginning of the ItemCheck_CheckCanUse vanilla method. ItemCheck_CheckCanUse happens before ItemCheck_Shoot and in the context of guns it is
        // used to make sure the gun has the ammo required to be able to be used. Setting it to null here instead of in CanShoot ensures that any issue with AmmoPool staying set (read comments above CanShoot)
        // are purely visual, instead of gameplay related (although I basically ensured that we won't run into issues on that front.. putting it here guarantees no gameplay issues)
        public override bool CanUseItem(Item item)
        {
            AmmoPool = null; // We reset 
            return true;

        }

        // Called just before ammo is picked / subtracted. We update our pool here. After this, tmodloader consults CanChooseAmmo hooks to decide which ammo to pick / subtract.
        // Our CanChooseAmmo hook will restrict what ammo can be chosen based on Pool[CurrPoolIndex] of the Pool we just updated. After this, tmodloader calls shoot and the pool is set back to null
        // After this, Shoot() is called
        //
        // This hook would never be reached in the first place if they did not have the ammo required to shoot, as it checks in ItemCheck_CheckCanUse before ItemCheck_Shoot happens.
        // "Having the ammo required to shoot" also means that they MUST have at least one stack of ammo that is non consumable or has a stacksize of > 1 (my CanChooseAmmo hook makes *sure* of this if
        // alternating is enabled). This means that the ONLY thing that can possibly "choke" the process is other CanShoot's returning false. That's why we check those hooks to ENSURE that we don't set the
        // pool to something non-null unless KNOW we will get to the end of Shoot() and set the pool back to null. If we fail to set the pool back to null, then 
        public override bool CanShoot(Item weapon)
        {
            if (DontCallMyHooks || AlternatingDisabled)
                return true;

            // We pre-emptively call CombinedHooks.CanShoot to see if other hooks would return false (even if they are naturally called after this one). If any of them return false, we do too
            // This prevents a visual bug that is caused when CanShoot is called here first and the pool is updated, but then another CanShoot returns false. We want to keep the pool null if any CanShoot
            // would return false 
            DontCallMyHooks = true;
            bool result = CombinedHooks.CanShoot(Player, weapon);
            DontCallMyHooks = false;

            // We don't want to update the new pool if a later hook would make it so this item cannot shoot. Or else the pool will
            // be updated
            if (!result)
                return false;

            if (!AlternatingDisabled && !AlternatingAmmoMechanic.Sets.NonAmmoAlternatingItems[weapon.type])
            {
                UpdateAndRotatePool(weapon);
            }

            return true;
        }

        /// <summary>
        /// In CanAddToPool (local function inside UpdateAndRotatePool) we call all ItemLoader.CanChooseAmmo() hooks on all the items in
        /// the player's inventory to dynamically build the ammo pool so we can restrict it (aka alternate current available ammo). The thing is, we don't want this hook to run
        /// when we are calling all hooks manually. If we did this, then our hook would make it so items aren't added to the pool if they aren't equivalent to CurrentAmmoItemType,
        /// so the pool would not work properly at all (it would only have one item type in it).
        /// </summary>
        private static bool DontCallMyHooks { get; set; }

        // Default vanilla/tModloader logic for choosing ammo to be used for a gun upon shooting is to choose the first ammo that ItemLoader.CanChooseAmmo returns true on.
        // Instead of doing this, I want to build a pool of available ammunitions (using the same ItemLoader.CanChooseAmmo hooks), then restrict what ammunition can be used
        // inside my OWN CanChooseAmmo hook inside AlternatingAmmoGun. We restrict what ammunition can be used to one index of the available pool at a time (this index alternates
        // each time a gun is shot)
        public void UpdateAndRotatePool(Item weapon)
        {
            bool ratioBased = Mode == AlternatingAmmoMode.Alternate_PreserveRatio; // Make this a config option. also make this whole mechanic a config option (client side)

            IEnumerable<int> availableItemTypes = ratioBased ? new List<int>() : new HashSet<int>();

            Dictionary<int, bool> anyStackGreaterThan1 = new();

            void RegisterStack(Item item)
            {
                int type = item.type;
                if (!anyStackGreaterThan1.ContainsKey(type)) anyStackGreaterThan1[type] = false;
                if (item.stack > 1)
                    anyStackGreaterThan1[type] = true;
            }

            foreach (Item possibleAmmoItem in Player.inventory)
            {
                int ammoType = possibleAmmoItem.ammo;
                int itemType = possibleAmmoItem.type;

                if (CanAddToPool(weapon, possibleAmmoItem, Player))
                {
                    RegisterStack(possibleAmmoItem);

                    if (availableItemTypes is List<int> list)
                        list.Add(possibleAmmoItem.type);

                    else
                        (availableItemTypes as HashSet<int>).Add(possibleAmmoItem.type);
                }
            }

            // This method determines whether or not the ammo item should be added to the pool of ammunitions that we cycle through
            // We apply some possibly falsy return logic, then consult other hooks, to determine what should be in the ammo pool, the exact same way tmodloader consults hooks to decide what ammos can be used.
            // This is to ensure as much compatibility/flexibility as possible between this mod and other mods / tModloader itself (i.e if weapons use these hooks to allow items to be used as ammo for their modded guns or
            // global guns, my mod will see it and know to add them to the alternating ammo pool)
            static bool CanAddToPool(Item weapon, Item ammoItem, Player player)
            {
                if (ammoItem.type == ItemID.None)
                    return false;

                DontCallMyHooks = true;
                bool result = ItemLoader.CanChooseAmmo(weapon, ammoItem, player);
                DontCallMyHooks = false;
                return result;
            }

            // The where clause removes options from the pool that cannot be used (can't be used if all instance of the ammo in the inventory have a stack count of 1). This prevents the gun
            // from "freezing up" as a result of CanChooseAmmo refusing to choose ammunitions that have 1 left in the stack, but also being required to choose one because it's the CurrentAmmoItemType
            AmmoPool = availableItemTypes.Where(itemType => anyStackGreaterThan1[itemType]).ToArray();

            // If no ammo is found, ammo pool is set to null which means our CanChooseAmmo hook will return null to default to other hook / vanilla logic
            if (AmmoPool.Length == 0)
            {
                AmmoPool = null;
                CurrPoolIndex = 0;
            }
            else
            {
                CurrPoolIndex = (CurrPoolIndex + 1) % AmmoPool.Length;
            }
        }

        internal bool? CanChooseAmmo(Item weapon, Item ammoItem, Player player)
        {
            if (DontCallMyHooks)
                return null;

            // Refuse to use the last item in the stack if alternating is enabled (regardless of if AmmoPool is null, or if this gun is allowed to alternate)
            if (ammoItem.stack == 1 && ammoItem.consumable && !AlternatingDisabled)
                return false;

            // Make it so it only applied to the held item, for efficiency (also because seeing ammunition count changing for other guns besides this one is annoying)
            if (AlternatingDisabled || AmmoPool is null || player.HeldItem.type != weapon.type || ammoItem.type == ItemID.None)
                return null;

            // We are only allowed to choose ammo based on CurrentAmmoItemType, which is the element at AmmoPool[CurrentPoolChoice] 
            return ammoItem.type == AmmoPool[CurrPoolIndex];
        }

        // Shoot is the last thing called
        public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            AmmoPool = null;
            return true;
        }

        public override void Initialize()
        {
            Mode = AlternatingAmmoMode.Disabled;
        }

        public override void SaveData(TagCompound tag)
        {
            // If ammo alternating is disabled we don't save it because default value for int is 0 (Disabled is 0 since it's first in struct)
            if (Mode != AlternatingAmmoMode.Disabled)
                tag["AlternatingAmmoMode"] = (int)Mode;
        }

        // Doesn't get called if no data was saved for this modplayer yet. We set the default in initialize for this situation
        public override void LoadData(TagCompound tag)
        {
            Mode = (AlternatingAmmoMode)tag.Get<int>("AlternatingAmmoMode");
        }
    }

    public class AlternatingAmmoGun : GlobalItem
    {
        public override bool CanConsumeAmmo(Item weapon, Item ammo, Player player)
        {
            return base.CanConsumeAmmo(weapon, ammo, player);
        }
        public override bool AppliesToEntity(Item weapon, bool lateInstantiation) => lateInstantiation && weapon.useAmmo != 0;

        // Redirect to the one we defined in AlternatingAmmoPlayer to keep the logic organized
        public override bool? CanChooseAmmo(Item weapon, Item ammoItem, Player player) => player.GetModPlayer<AlternatingAmmoPlayer>().CanChooseAmmo(weapon, ammoItem, player);
    }

    internal readonly struct AlternatingAmmoMode
    {
        internal static List<AlternatingAmmoMode> ammoModes = new();

        public static int Count => ammoModes.Count;

        public static readonly AlternatingAmmoMode Disabled = new("Disabled", ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/UI/AlternatingAmmoUI/AlternatingMode_Disabled"));
        public static readonly AlternatingAmmoMode Alternate = new("Cycle through ammo, one shot per type", ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/UI/AlternatingAmmoUI/AlternatingMode_ByType"));
        public static readonly AlternatingAmmoMode Alternate_PreserveRatio = new("Cycle through ammo, preserving ratios", ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/UI/AlternatingAmmoUI/AlternatingMode_ByRatio"));

        internal int Value { get; }
        internal string DisplayName { get; }
        internal Asset<Texture2D> TextureAsset { get; }

        private AlternatingAmmoMode(string name, Asset<Texture2D> asset)
        {
            Value = Count;
            TextureAsset = asset;
            DisplayName = name;
            ammoModes.Add(this);
        }

        public static explicit operator int(AlternatingAmmoMode m) => m.Value;

        public static implicit operator AlternatingAmmoMode(int i) => ammoModes[i];

        public static bool operator ==(AlternatingAmmoMode m1, AlternatingAmmoMode m2) => m1.Value == m2.Value;
        public static bool operator !=(AlternatingAmmoMode m1, AlternatingAmmoMode m2) => m1.Value != m2.Value;

        public override int GetHashCode() => Value;
    }
}
