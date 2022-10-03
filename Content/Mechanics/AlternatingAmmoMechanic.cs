using GarnsMod.Content.Items.Weapons.Ranged;
using GarnsMod.UI.AlternatingAmmoUI;
using GarnsMod.UI.FishingRodUI;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace GarnsMod.Content.Mechanics
{
    // Creates a client-sided timer (never synced) as a means of rotating through ammo types
    internal class AlternatingAmmoPlayer : ModPlayer
    {
        public int AmmoAlternatingTimer { get; private set; }

        private AlternatingAmmoMode _selectedAlternatingAmmoMode;

        public AlternatingAmmoMode SelectedAlternatingAmmoMode 
        { 
            get => _selectedAlternatingAmmoMode;
            set
            {
                _selectedAlternatingAmmoMode = value;

                // Set to null to do default vanilla logic until they shoot again
                AvailableAmmoItemTypes = null;
            }
        }

        private int alternatorTimeout = 0;

        public int[] AvailableAmmoItemTypes { get; private set; }

        public override void PostUpdate()
        {
            AmmoAlternatingTimer++;

            if (alternatorTimeout > 0)
            {
                alternatorTimeout--;
                if (alternatorTimeout == 0)
                {
                    // Set to null to do default vanilla logic until they shoot again
                    AvailableAmmoItemTypes = null;
                }
            }

        }

        public override void Initialize()
        {
            SelectedAlternatingAmmoMode = AlternatingAmmoMode.Disabled;
        }

        public override void OnEnterWorld(Player player)
        {
            ModContent.GetInstance<AlternatingAmmoUISystem>().UIState.CalculateOrigin();
        }

        public override void SaveData(TagCompound tag)
        {
            // If ammo alternating is disabled we don't save it because default value for int is 0 (Disabled is 0 since it's first in struct)
            if (SelectedAlternatingAmmoMode != AlternatingAmmoMode.Disabled)
                tag["AlternatingAmmoMode"] = (int) SelectedAlternatingAmmoMode;
        }

        // Doesn't get called if no data was saved for this modplayer yet. We set the default in initialize for this situation
        public override void LoadData(TagCompound tag)
        {
            SelectedAlternatingAmmoMode = (AlternatingAmmoMode) tag.Get<int>("AlternatingAmmoMode");
        }

    }

    public class AlternatingAmmoGun : GlobalItem
    {
        public static class Sets
        {
            public static bool[] NonAmmoAlternatingItems { get; private set; }
            public static int[][] AlsoAcceptAmmunitionType { get; private set; }

            public static void SetStaticDefaults()
            {
                // Base array capacity off some other random ItemID set array
                int setLength = ItemID.Sets.IsAMaterial.Length;

                NonAmmoAlternatingItems = new bool[setLength];
                AlsoAcceptAmmunitionType = new int[setLength][];
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

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var dd = player.inventory;

            return base.Shoot(item, player, source, position, velocity, type, damage, knockback);
        }

        public override bool? CanChooseAmmo(Item weapon, Item ammoItem, Player player)
        {
            // Make it so it only applied to the held item, for efficiency (also because seeing ammunition count changing constantly is annoying)
            if (Sets.NonAmmoAlternatingItems[weapon.type] || player.HeldItem.type != weapon.type) 
            {
                return null;
            }

            int[] alsoAcceptAmmoTypes = Sets.AlsoAcceptAmmunitionType[weapon.type] ?? Array.Empty<int>();

            // If this ammo type isn't allowed by the weapon and we don't make an exception with Sets.AlsoAcceptAmmunitionTypes for this weapon
            // then we return null to allow other mod's hooks / vanilla logic to take effect
            if (ammoItem.ammo != weapon.useAmmo && !alsoAcceptAmmoTypes.Contains(ammoItem.ammo))
            {

                return null; 
            }

            bool ratioBased = true; // Make this a config option. also make this whole mechanic a config option (client side)

            IEnumerable<int> availableItemTypes = ratioBased ? new List<int>() : new HashSet<int>(); 

            foreach (Item possibleAmmoItem in player.inventory)
            {
                if (possibleAmmoItem.type != ItemID.None && (possibleAmmoItem.ammo == weapon.useAmmo || alsoAcceptAmmoTypes.Contains(ammoItem.ammo)))
                {
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
          
            int timer = player.GetModPlayer<AlternatingAmmoPlayer>().AmmoAlternatingTimer; // This will be set to smthing from player
            int choice = timer / 30 % availableItemTypes.Count();
            int mustBeItemType = availableItemTypes.ToArray()[choice];

            return ammoItem.type == mustBeItemType;

            // In the future it would be cool if they could supply "frequency" list like [5, 5, 10] editable in config.
            // Index 0 would be the first ammo found in their inv, index 1 would be second ammo found, etc so they position the ammo in their inventory to control what index it's at in the freq chart
            // then ctr % accumulatedFrequencies (i.e 5+5+10.. 20) would be used to get 'frequency position' then we can figure out which one to choose based on frequency position
            // if frequency position was < 5, it would choose the first one. If it is >= 5 < 10, it would choose second, if it was >= 10 < 20 it would choose the third. This wld def use an accumulator function
        }   
    }
}
