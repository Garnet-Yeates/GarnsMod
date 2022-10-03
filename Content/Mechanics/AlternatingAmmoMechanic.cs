using GarnsMod.UI.AlternatingAmmoUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
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

        // When we change mode we Halt
        public AlternatingAmmoMode Mode { get => _mode; set { _mode = value; Halt(); } }

        public bool AlternatingDisabled => Mode == AlternatingAmmoMode.Disabled;

        public int AmmoAlternatingTimer { get; private set; }

        private int[] AvailableAmmoItemTypes;

        // Halted basically means don't use ammo alternating logic in AlternatingAmmoGun.CanChooseAmmo until the next time a bullet is shot.
        // When a bullet is shot, AvailableAmmoItemTypes will be recalculated and it will un-halt as long as Mode != Disabled
        public bool Halted => AvailableAmmoItemTypes is null;

        public void Halt()
        {
            AvailableAmmoItemTypes = null;
            AmmoAlternatingTimer = 0;
        }

        public int CurrentAmmoType => Halted ? 0 : AvailableAmmoItemTypes[AmmoAlternatingTimer % AvailableAmmoItemTypes.Length]; 

        private int alternatorTimeout = 0;

        public override void PostUpdate()
        {
            if (alternatorTimeout > 0)
            {
                alternatorTimeout--;
                if (alternatorTimeout == 0)
                {
                    // Set to null to do default vanilla logic until they shoot again
                    Halt();
                }
            }

        }

        // Called after Shoot() on global item
        public void RotateCurrentAmmoType(Item weapon)
        {
            int[] alsoAcceptAmmoTypes = AlternatingAmmoGun.Sets.AlsoAcceptAmmunitionTypes[weapon.type] ?? Array.Empty<int>();

            AmmoAlternatingTimer++; // Timer goes up each time a bullet is shot
            alternatorTimeout = 60;

            bool ratioBased = Mode == AlternatingAmmoMode.Alternate_PreserveRatio; // Make this a config option. also make this whole mechanic a config option (client side)

            IEnumerable<int> availableItemTypes = ratioBased ? new List<int>() : new HashSet<int>();

            // <ItemID, Total number of unique stacks of this ItemID in the inventory>
            Dictionary<int, int> numUniqueStacks = new();

            // ItemID, Total number of this item in the inventory 
            Dictionary<int, int> counts = new();

            void AddStackWithCount(int itemType, int count)
            {
                if (!numUniqueStacks.ContainsKey(itemType))
                    numUniqueStacks[itemType] = 0;
                numUniqueStacks[itemType]++;

                if (!counts.ContainsKey(itemType))
                    counts[itemType] = 0;
                counts[itemType] += count;
            }

            foreach (Item possibleAmmoItem in Player.inventory)
            {
                int ammoType = possibleAmmoItem.ammo;
                int itemType = possibleAmmoItem.type;
                if (itemType != ItemID.None && (ammoType == weapon.useAmmo || alsoAcceptAmmoTypes.Contains(ammoType)))
                {
                    AddStackWithCount(itemType, possibleAmmoItem.stack);
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
            // TL;DR CurrentAmmoType can not be an ammo type that we only have stacks of 1, of or else the weapon won't shoot and will have to wait for the automatic Halt() timeout to shoot again
            AvailableAmmoItemTypes = availableItemTypes.Where(itemType => counts[itemType] / numUniqueStacks[itemType] != 1).ToArray();
        }

        public override void Initialize()
        {
            Mode = AlternatingAmmoMode.Disabled;
        }

        public override void OnEnterWorld(Player player)
        {
            ModContent.GetInstance<AlternatingAmmoUISystem>().UIState.CalculateOrigin();
        }

        public override void SaveData(TagCompound tag)
        {
            // If ammo alternating is disabled we don't save it because default value for int is 0 (Disabled is 0 since it's first in struct)
            if (Mode != AlternatingAmmoMode.Disabled)
                tag["AlternatingAmmoMode"] = (int) Mode;
        }

        // Doesn't get called if no data was saved for this modplayer yet. We set the default in initialize for this situation
        public override void LoadData(TagCompound tag)
        {
            Mode = (AlternatingAmmoMode) tag.Get<int>("AlternatingAmmoMode");
        }

    }

    public class AlternatingAmmoGun : GlobalItem
    {
        public static class Sets
        {
            public static bool[] NonAmmoAlternatingItems { get; private set; }
            public static int[][] AlsoAcceptAmmunitionTypes { get; private set; }

            public static void SetStaticDefaults()
            {
                // Base array capacity off some other random ItemID set array
                int setLength = ItemID.Sets.IsAMaterial.Length;

                NonAmmoAlternatingItems = new bool[setLength];
                AlsoAcceptAmmunitionTypes = new int[setLength][];
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

        public override bool? CanChooseAmmo(Item weapon, Item ammoItem, Player player)
        {
            AlternatingAmmoPlayer altPlayer = player.GetModPlayer<AlternatingAmmoPlayer>();

            // Refuse to use the last item in the stack if alternating is enabled (regardless of if it's halted, or if this gun is allowed to alternate)
            if (ammoItem.stack == 1 && !altPlayer.AlternatingDisabled)
            {
                return false;
            }

            // Make it so it only applied to the held item, for efficiency (also because seeing ammunition count changing constantly is annoying)
            if (altPlayer.AlternatingDisabled || altPlayer.Halted || Sets.NonAmmoAlternatingItems[weapon.type] || player.HeldItem.type != weapon.type || ammoItem.type == ItemID.None) 
            {
                return null; // Return null for vanilla logic 
            }

            int[] alsoAcceptAmmoTypes = Sets.AlsoAcceptAmmunitionTypes[weapon.type] ?? Array.Empty<int>();

            // If this ammo type isn't allowed by the weapon and we don't make an exception with Sets.AlsoAcceptAmmunitionTypes for this weapon
            // then we return null to allow other mod's hooks / vanilla logic to take effect
            if (ammoItem.ammo != weapon.useAmmo && !alsoAcceptAmmoTypes.Contains(ammoItem.ammo))
            {
                return null; 
            }

            return ammoItem.type == altPlayer.CurrentAmmoType;
        }   
    }

    internal readonly struct AlternatingAmmoMode
    {
        internal static List<AlternatingAmmoMode> ammoModes = new();

        public static int Count => ammoModes.Count;

        public static readonly AlternatingAmmoMode Disabled = new("Disabled", ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/UI/AlternatingAmmoUI/AlternatingMode_Disabled"));
        public static readonly AlternatingAmmoMode Alternate = new("Cycle through ammo", ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/UI/AlternatingAmmoUI/AlternatingMode_ByType"));
        public static readonly AlternatingAmmoMode Alternate_PreserveRatio = new("Cycle through ammo, keeping ratios", ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/UI/AlternatingAmmoUI/AlternatingMode_ByRatio"));

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
