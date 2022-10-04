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

    // Creates a client-sided timer (never synced) as a means of rotating through ammo types
    internal class AlternatingAmmoPlayer : ModPlayer
    {
        private AlternatingAmmoMode _mode;

        // When we change mode we Reset
        public AlternatingAmmoMode Mode { get => _mode; set { _mode = value; Reset(); } }

        public bool AlternatingDisabled => Mode == AlternatingAmmoMode.Disabled;

        public int AmmoAlternatingCounter { get; private set; }

        public int[] AmmoPool { get; private set; }

        // IsReset basically means don't use ammo alternating logic in AlternatingAmmoGun.CanChooseAmmo until the next time a bullet is shot.
        // When a bullet is shot, AmmoPool will be recalculated and it will set to a non-null again as long as Mode != Disabled
        // && The weapon is allowed to alternate && the weapon has ammo that can be used (based on consulting other hooks to find ammo)

        // The main reason we use IsReset is to stop the gun from choking up from trying to use expired ammo alternating logic
        // (i.e logic that was determined last shoot isn't guaranteed to stay valid. So we reset after a short timeout, as well as in other situations)
        public bool IsReset => AmmoPool is null;

        public void Reset()
        {
            AmmoPool = null;
            AmmoAlternatingCounter = 0;
        }

        public int CurrentAmmoItemType => IsReset ? 0 : AmmoPool[AmmoAlternatingCounter % AmmoPool.Length];

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


        // This hook is the first one called in ItemLoader, it will always be called. Globals are always called too but the ModItem one won't be called if any return false
        public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (AlternatingDisabled && !AlternatingAmmoMechanic.Sets.NonAmmoAlternatingItems[source.Item.type])
            {
                UpdateAndRotatePool(source.Item);
            }

            return true;
        }

        // Called after Shoot(). We calculate the available ammo pool after we shoot (as opposed to constantly during CanChooseAmmo
        // hook). The reason why is because 1. I like the idea of it cycling on a per-shot counter instead of on a time-based timer, it just makes more
        // sense that way. Also, CanChooseAmmo gets called [numGunsInInventory] times every tick, as opposed to once every shot which is virtually guaranteed to occur less,
        // so putting recalculation logic there just makes more sense from an efficiency perspective. Coding it like this way definitely requires more checks to prevent issues
        // (see IsReset) but overall will feel way better in game
        public void UpdateAndRotatePool(Item weapon)
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
                if (!counts.ContainsKey(type)) counts[type] = new ItemWithCountAndStackCount { Consumable = item.consumable, Count = 0, Stacks = 0 };
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
                        list.Add(possibleAmmoItem.type);

                    else
                        (availableItemTypes as HashSet<int>).Add(possibleAmmoItem.type);
                }
            }

            static bool CanAddToPool(Item weapon, Item ammoItem, Player player)
            {
                if (ammoItem.type == ItemID.None)
                    return false;

                AlternatingAmmoGun.DontRunHook = true;
                bool result = ItemLoader.CanChooseAmmo(weapon, ammoItem, player);
                AlternatingAmmoGun.DontRunHook = false;
                return result;
            }

            // The where clause removes options from the pool that cannot be used (can't be used if all instance of the ammo in the inventory have a stack count of 1). This prevents the gun
            // from "freezing up" as a result of CanChooseAmmo refusing to choose ammunitions that have 1 left in the stack, but also being required to choose one because it's the CurrentAmmoItemType
            // TL;DR CurrentAmmoItemType can not be an ammo type that we only have stacks of 1, of or else the weapon won't shoot and will have to wait for the automatic Reset() timeout to shoot again
            AmmoPool = availableItemTypes.Where(itemType => !(counts[itemType].Consumable && counts[itemType].Count / counts[itemType].Stacks == 1)).ToArray();

            // If no ammo is found, we reset to tell the next call to our CanChooseAmmo hook to use default logic.
            // The pool won't be recalculated again until after the next call to Shoot() happens and UpdateAndRotatePool() is called again
            if (AmmoPool.Length == 0)
                Reset();
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
        public override bool AppliesToEntity(Item weapon, bool lateInstantiation) => lateInstantiation && weapon.useAmmo != 0;


        /// <summary>
        /// In CanAddToPool (local function inside UpdateAndRotatePool) we call all ItemLoader.CanChooseAmmo() hooks on all the items in
        /// the player's inventory to dynamically build the ammo pool so we can restrict it (aka alternate current available ammo). The thing is, we don't want this hook to run
        /// when we are calling all hooks manually. If we did this, then our hook would make it so items aren't added to the pool if they aren't equivalent to CurrentAmmoItemType,
        /// so the pool would not work properly at all (it would only have one item type in it).
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
                return false;

            // Make it so it only applied to the held item, for efficiency (also because seeing ammunition count changing for other guns besides this one is annoying)
            if (altPlayer.AlternatingDisabled || altPlayer.IsReset || player.HeldItem.type != weapon.type || ammoItem.type == ItemID.None)
                return null;

            // We are only allowed to choose ammo based on CurrentAmmoItemType, which is the element at AmmoPool[CurrentPoolChoice] 
            return ammoItem.type == altPlayer.CurrentAmmoItemType;
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
