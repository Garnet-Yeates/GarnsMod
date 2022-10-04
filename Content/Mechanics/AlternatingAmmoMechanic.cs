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
        // When a bullet is shot, AmmoPool may be recalculated based on conditions (see Shoot and UpdateAndRotatePool)

        // The main reason we use IsReset is to stop the gun from choking up from trying to use expired ammo alternating logic
        // (i.e logic that was determined last shoot isn't guaranteed to stay valid. So we reset after a short timeout, as well as in other situations)
        public bool IsReset => AmmoPool is null;

        public void Reset()
        {
            AmmoPool = null;
            AmmoAlternatingCounter = 0;
        }

        public int CurrentAmmoItemType => IsReset ? 0 : AmmoPool[AmmoAlternatingCounter % AmmoPool.Length];

        // When this hits 0 we Reset
        private int alternatorTimeout = 0;

        // In PostUpdate we use this to determine if they switched their current hotbar index. Useless info: this is 58 when they have an item on their cursor. 
        private int lastHotbarIndex = 0;

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

        /// <summary>
        /// In CanAddToPool (local function inside UpdateAndRotatePool) we call all ItemLoader.CanChooseAmmo() hooks on all the items in
        /// the player's inventory to dynamically build the ammo pool so we can restrict it (aka alternate current available ammo). The thing is, we don't want this hook to run
        /// when we are calling all hooks manually. If we did this, then our hook would make it so items aren't added to the pool if they aren't equivalent to CurrentAmmoItemType,
        /// so the pool would not work properly at all (it would only have one item type in it).
        /// </summary>
        private static bool DontRunHook { get; set; }

        internal bool? CanChooseAmmo(Item weapon, Item ammoItem, Player player)
        {
            if (DontRunHook)
                return null;

            // Refuse to use the last item in the stack if alternating is enabled (regardless of if it's reset, or if this gun is allowed to alternate)
            if (ammoItem.stack == 1 && ammoItem.consumable && !AlternatingDisabled)
                return false;

            // Make it so it only applied to the held item, for efficiency (also because seeing ammunition count changing for other guns besides this one is annoying)
            if (AlternatingDisabled || IsReset || player.HeldItem.type != weapon.type || ammoItem.type == ItemID.None)
                return null;

            // We are only allowed to choose ammo based on CurrentAmmoItemType, which is the element at AmmoPool[CurrentPoolChoice] 
            return ammoItem.type == CurrentAmmoItemType;
        }

        // This hook is the first one called in ItemLoader, it will always be called. Globals are always called too but the ModItem one won't be called if any return false

        // We calculate the available ammo pool after we shoot (as opposed to constantly during CanChooseAmmo
        // hook). The reason why is because 1. I like the idea of it cycling on a per-shot counter instead of on a time-based timer, it just makes more
        // sense that way. Also, CanChooseAmmo gets called [numGunsInInventory] times every tick, as opposed to once every shot which is virtually guaranteed to occur less,
        // so putting recalculation logic there just makes more sense from an efficiency perspective. Coding it like this way definitely requires more checks to prevent issues
        // (see IsReset) but overall will feel way better in game
        public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!AlternatingDisabled && !AlternatingAmmoMechanic.Sets.NonAmmoAlternatingItems[source.Item.type])
            {
                UpdateAndRotatePool(source.Item);
            }

            return true;
        }

        // Default vanilla/tModloader logic for choosing ammo to be used for a gun upon shooting is to choose the first ammo that ItemLoader.CanChooseAmmo returns true on.
        // Instead of doing this, I want to build a pool of available ammunitions (using the same ItemLoader.CanChooseAmmo hooks), then restrict what ammunition can be used
        // inside my OWN CanChooseAmmo hook inside AlternatingAmmoGun. We restrict what ammunition can be used to one index of the available pool at a time (this index alternates
        // each time a gun is shot)
        public void UpdateAndRotatePool(Item weapon)
        {
            AmmoAlternatingCounter++; // Timer goes up each time a bullet is shot
            alternatorTimeout = 450;

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

                DontRunHook = true;
                bool result = ItemLoader.CanChooseAmmo(weapon, ammoItem, player);
                DontRunHook = false;
                return result;
            }

            // The where clause removes options from the pool that cannot be used (can't be used if all instance of the ammo in the inventory have a stack count of 1). This prevents the gun
            // from "freezing up" as a result of CanChooseAmmo refusing to choose ammunitions that have 1 left in the stack, but also being required to choose one because it's the CurrentAmmoItemType
            // TL;DR CurrentAmmoItemType can not be an ammo type that we only have stacks of 1, of or else the weapon won't shoot and will have to wait for the automatic Reset() timeout to shoot again
            AmmoPool = availableItemTypes.Where(itemType => anyStackGreaterThan1[itemType]).ToArray();

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
