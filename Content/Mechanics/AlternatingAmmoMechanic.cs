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
    // Creates a client-sided timer (never synced) as a means of rotating through ammo types
    internal class AlternatingAmmoPlayer : ModPlayer
    {
        private AlternatingAmmoMode _mode;

        // When we change mode we Reset
        public AlternatingAmmoMode Mode { get => _mode; set { _mode = value; Reset(); } }

        public bool AlternatingDisabled => Mode == AlternatingAmmoMode.Disabled;

        public int AmmoAlternatingCounter { get; private set; }

        public int[] AvailableAmmoItemTypes { get; private set; }

        // IsReset basically means don't use ammo alternating logic in AlternatingAmmoGun.CanChooseAmmo until the next time a bullet is shot.
        // When a bullet is shot, AvailableAmmoItemTypes will be recalculated and it will set to a non-null again as long as Mode != Disabled
        // && The weapon is allowed to alternate && the weapon has ammo that can be used (based on consulting other hooks to find ammo)

        // The main reason we use IsReset is to stop the gun from choking up from trying to use expired ammo alternating logic. When I say expired,
        // what I mean is this:
        // Since AvailableAmmoTypes/CurrentAmmoType are determined AFTER a gun is successfully shot and ammo is subtracted,
        // it calculates what the available ammo is for the NEXT shot after shooting (not before the gun is shot by using CanChooseAmmo hook, because
        // that is called every game tick and would be much less code-efficient). But what if we modified the some of the ammo in our inventory, or switched to a gun
        // that can't use the available ammo that was determined after the last gun was shot? It will refuse to shoot aka choke up, because with alternating logic
        // the gun is ONLY allowed to use CurrentAmmoType. This is why we have the timeout to make the logic "expire" aka set AvailableAmmoTypes to null
        public bool IsReset => AvailableAmmoItemTypes is null;

        public void Reset()
        {
            AvailableAmmoItemTypes = null;
            AmmoAlternatingCounter = 0;
        }

        public int CurrentAmmoType => IsReset ? 0 : AvailableAmmoItemTypes[AmmoAlternatingCounter % AvailableAmmoItemTypes.Length];

        private int alternatorTimeout = 0;

        private int lastHotbarIndex = 0; // Useless info: this is 58 when they have an item on their cursor. In PostUpdate we use this to determine if they switched their current hotbar index

        public override void PostUpdate()
        {
            if (lastHotbarIndex != Player.selectedItem)
            {
                lastHotbarIndex = Player.selectedItem;
                Reset(); // Reset when we switch hotbar index
            }

            if (alternatorTimeout > 0)
            {
                alternatorTimeout--;
                if (alternatorTimeout == 0)
                {
                    // Set to null to do default vanilla logic until they shoot again
                    Reset();
                }
            }

        }

        private class ItemWithCountAndStackCount
        {
            public bool Consumable { get; init; }
            public int Stacks { get; set; }
            public int Count { get; set; }
        }

        /// <summary>
        /// This method determines whether or not the ammo item should be added to the pool of ammunitions to cycle through<br/>
        /// We consult other hooks to determine what should be in the ammo pool, the exact same way tmodloader consults these hooks. This is to ensure as much compatibility/flexibility
        /// as possible between this mod and other mods (i.e if other mods use these hooks to add things to the ammo pool, my mod will see it and know to add them to the alternating pool)
        /// </summary>
        public static bool CanAddToPool(Item weapon, Item ammoItem, Player player)
        {
            if (AlternatingAmmoGun.Sets.NonAmmoAlternatingItems[weapon.type] || ammoItem.type == ItemID.None)
            {
                return false;
            }

            AlternatingAmmoGun.DontRunHook = true;
            bool result = ItemLoader.CanChooseAmmo(weapon, ammoItem, player);
            AlternatingAmmoGun.DontRunHook = false;
            return result;
        }

        // Called after Shoot() on global item. We calculate the available ammo pool after we shoot instead of before we shoot as opposed to constantly during CanChooseAmmo
        // hook. The reason why is because 1. I like the idea of it cycling on a per-shot counter instead of on a time-based timer, it just makes more
        // sense that way. Also, CanChooseAmmo gets called [numGunsInInventory] times every tick, as opposed to once every shot which is virtually guaranteed to occur less,
        // so putting recalculation logic there just makes more sense from an efficiency perspective. Coding it like this way definitely requires more checks to prevent issues
        // (see IsReset) but overall will feel way better in game
        public void RotateCurrentAmmoType(Item weapon)
        {
            AmmoAlternatingCounter++; // Timer goes up each time a bullet is shot
            alternatorTimeout = 45;

            bool ratioBased = Mode == AlternatingAmmoMode.Alternate_PreserveRatio; // Make this a config option. also make this whole mechanic a config option (client side)

            IEnumerable<int> availableItemTypes = ratioBased ? new List<int>() : new HashSet<int>();

            // Maps ItemID => Total number of this item in the inventory, total number of stacks of this item in inventory, and whether or not it is consumable
            Dictionary<int, ItemWithCountAndStackCount> counts = new();

            void AddStackWithCount(Item item)
            {
                int type = item.type;
                if (!counts.ContainsKey(type))
                    counts[type] = new ItemWithCountAndStackCount { Consumable = item.consumable, Count = 0, Stacks = 0 };
                counts[type].Count += item.stack;
                counts[type].Stacks++;
            }

            foreach (Item possibleAmmoItem in Player.inventory)
            {
                int ammoType = possibleAmmoItem.ammo;
                int itemType = possibleAmmoItem.type;

                if (CanAddToPool(weapon, possibleAmmoItem, Player))
                {
                    AddStackWithCount(possibleAmmoItem);
                    if (availableItemTypes is List<int> list)
                    {
                        list.Add(possibleAmmoItem.type);
                    }
                    else
                    {
                        (availableItemTypes as HashSet<int>).Add(possibleAmmoItem.type);
                    }
                }
            }

            // The where clause removes options from the pool that cannot be used (can't be used if all instance of the ammo in the inventory have a stack count of 1). This prevents the gun
            // from "freezing up" as a result of CanChooseAmmo refusing to choose ammunitions that have 1 left in the stack, but also being required to choose one because it's the CurrentAmmoType
            // TL;DR CurrentAmmoType can not be an ammo type that we only have stacks of 1, of or else the weapon won't shoot and will have to wait for the automatic Reset() timeout to shoot again
            AvailableAmmoItemTypes = availableItemTypes.Where(itemType => !(counts[itemType].Consumable && counts[itemType].Count / counts[itemType].Stacks == 1)).ToArray();

            if (AvailableAmmoItemTypes.Length == 0)
            {
                // If no ammo is found, we reset to tell the next call to our CanChooseAmmo hook to use default logic.
                // The pool won't be recalculated again until after the next call to Shoot() happens and RotateCurrentAmmoType() is called again
                Reset();
            }
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
        public static class Sets
        {
            public static bool[] NonAmmoAlternatingItems { get; private set; }

            public static void SetStaticDefaults()
            {
                // Base array capacity off some other random ItemID set array
                NonAmmoAlternatingItems = new bool[ItemID.Sets.IsAMaterial.Length];
            }
        }

        public override void SetStaticDefaults()
        {
            Sets.SetStaticDefaults();
        }

        public override bool AppliesToEntity(Item weapon, bool lateInstantiation)
        {
            return lateInstantiation && weapon.useAmmo != 0;
        }

        // If any global shoot hook returns false, then the ModItem shoot won't get called. This made me worried that if a global shoot returns false, then it would
        // stop other global hook shoots from running aka possibly stop this hook from running. I dug through TML and found that this is not the case though :)
        // How it works is it calls PlayerLoader.Shoot to get default value and pass it into ItemLoader. Then ItemLoader calls all GlobalItem shoot hooks to possibly
        // change that default value to false (it can't get tru-er, only falser). Then if default value is not false, it calls ModItem.Shoot
        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            AlternatingAmmoPlayer altPlayer = player.GetModPlayer<AlternatingAmmoPlayer>();

            if (!altPlayer.AlternatingDisabled && !Sets.NonAmmoAlternatingItems[source.Item.type])
            {
                altPlayer.RotateCurrentAmmoType(source.Item);
            }

            return true;
        }

        /// <summary>
        /// In <see cref="AlternatingAmmoPlayer.CanAddToPool(Item, Item, Player)"/> we call all ItemLoader.CanChooseAmmo() hooks on all the items in
        /// the player's inventory (similar to how tModloader does it, except we dont stop at the first one that returns true in the inventory) 
        /// to get the pool of available ammo dynamically (for compatibility / flexibility). We use this pool, along with a 'clock' that goes up every time a gun is shot
        /// to restrict what type of ammo can be chosen (in the CanChooseAmmo hook below). By restricting the type of ammo that can be chosen to one index of
        /// the available ammo pool at a time, we effectively get an "ammo alternating / distribution" effect. The thing is, we don't want this hook to run
        /// when we are calling all hooks to dynamically decide what to add to the pool. If we did this, then it won't add items to the pool that have an item
        /// stack of 1, which will cause our ratio-based mode to not work properly if they have any stacks of 1. So we use DontRunHook to return null to pass the logic onto
        /// the next hook/vanilla
        /// </summary>
        internal static bool DontRunHook { get; set; }

        // CanChooseAmmo (Weapon) hooks work like this: First it tries all global hooks, then tries all ModItem hooks. If any return false it stops (meaning returning false on global
        // stops other globals from running and also stops the hook from being called on the Moditem). If they return null or true it continues. If not true by the end it uses item.ammo == weapon.useAmmo
        public override bool? CanChooseAmmo(Item weapon, Item ammoItem, Player player)
        {
            if (DontRunHook)
                return null;

            AlternatingAmmoPlayer altPlayer = player.GetModPlayer<AlternatingAmmoPlayer>();

            // Refuse to use the last item in the stack if alternating is enabled (regardless of if it's reset, or if this gun is allowed to alternate)
            if (ammoItem.stack == 1 && ammoItem.consumable && !altPlayer.AlternatingDisabled)
            {
                return false;
            }

            // Make it so it only applied to the held item, for efficiency (also because seeing ammunition count changing for other guns besides this one is annoying)
            if (altPlayer.AlternatingDisabled || altPlayer.IsReset || player.HeldItem.type != weapon.type || ammoItem.type == ItemID.None)
            {
                return null; // Return null for vanilla logic 
            }

            return ammoItem.type == altPlayer.CurrentAmmoType;
        }
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
