﻿using GarnsMod.Content.Projectiles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static GarnsMod.ColorHelper;


namespace GarnsMod.Content.Items.Tools
{
    public class GarnsFishingRod : ModItem
    {
        // Fields
        internal int level = 1;
        internal int fishTillNextLevel = GetFishNeededAtLevel(1);
        internal int totalFishCaught = 0;
        internal ShootMode shootMode = ShootMode.Cone;

        // Constants
        public static readonly int MaxLevel = 30;
        public static readonly int BaseFishingPower = 20;
        public static readonly int ValuePerFish = 100; // Each fish caught increases rod value by 1 silver

        public static readonly int LineDoesntBreakLevel = 3;
        public static readonly int CrateChanceLevel = 6;
        public static readonly int LavaFishingLevel = 9;

        // Properties (all based on fields)
        public float ShootSpeedMultiplier => 1 + 1.5f * ((level - 1.0f) / (MaxLevel - 1.0f)); // ShootSpeed => 1x to 2x

        public float BaitConsumptionReductionPercent => level * 2.5f; // Multiplicative, better than additive for high bait power

        public float FishingPowerMultIncrease => level * 1.0f;

        public int FishingPowerAdditiveIncrease => (int)(level * 2.0f);

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Garn's Rod");
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.WoodFishingPole);
            Item.value = 500;
            Item.height = 60;
            Item.fishingPole = BaseFishingPower; // Base fishing power is 30, but will go up with level (see HoldItem() hook)
            Item.shootSpeed = 10f; // Sets the speed in which the bobbers are launched. Wooden Fishing Pole is 9f and Golden Fishing Rod is 17f.
            Item.shoot = ModContent.ProjectileType<GarnsFishingRodBobber>(); // The Bobber projectile.
        }

        // Called when:
        // Player joins, Item is dropped, or is spawned on client-side and MessageID.SyncItem is called (latter not used in my code)
        // Item is in inventory, but MessageID.SyncEquipment was called
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(level);
            writer.Write(fishTillNextLevel);
            writer.Write(totalFishCaught);
            writer.Write((int)shootMode);
        }

        public override void NetReceive(BinaryReader reader)
        {
            int level = reader.ReadInt32();
            int fishTillNextLevel = reader.ReadInt32();
            int totalFishCaught = reader.ReadInt32();
            shootMode = reader.ReadInt32();
            SetStats(level, fishTillNextLevel, totalFishCaught);
        }

        // Called by LoadData() and NetReceive() to set the item's values to the loaded/syned values
        public void SetStats(int level, int fishTillNextLevel, int totalFishCaught)
        {
            this.level = level;
            this.fishTillNextLevel = fishTillNextLevel;
            this.totalFishCaught = totalFishCaught;
            Item.value += totalFishCaught * ValuePerFish;
        }


        // Only called on the client leveling up, which is why we use Main.myPlayer here (don't need a player instance to be passed)
        // Upon leveling up, we send a SyncEquipment so that the other clients know what level the fishing rod is
        // the only reason other clients need to know what level the fishing rod is while it is in someone else's inventory
        // is so the HoldItem() lighting effects are synced properly without the item having to be dropped / the player relogging
        public void OnLevelUp()
        {
            level++;
            fishTillNextLevel = GetFishNeededAtLevel(++level);

            Item[] inventory = Main.player[Main.myPlayer].inventory;
            int inventoryIndex = -1;
            for (int i = 0; i < inventory.Length; i++)
            {
                if (inventory[i] == Item)
                {
                    inventoryIndex = i;
                }
            }
            // SyncEquipment is similar to SyncItem but it is for items that are inside a player's inventory as opposed to out in the world
            NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, Main.myPlayer, inventoryIndex, Item.prefix);
        }

        // Only called on the client that caught the fish (via FishingRodPlayer.ModifyCaughtFish())
        // This means that fishTillNextLevel/totalFishCaught is not in sync with the other clients until a level up happens, the player relogs, or the item
        // is dropped. However the level must be synced even before the item is dropped. See OnLevelUp() for why and how.
        public void OnCatchFish()
        {
            totalFishCaught++;
            Item.value += ValuePerFish;
            if (level < MaxLevel && --fishTillNextLevel < 1)
            {
                OnLevelUp();
            }
        }

        public static int GetFishNeededAtLevel(int level)
        {
            const float b = 450; // b is base amount of fish needed to be caught at level 1
            const float p = 0.8f; // p being below 1 means it gets easier and easier to level up as you level up
            return (int)(b * Math.Pow(level, p));

            // https://www.desmos.com/calculator/xnhxbfvdcn
            // l = current level, m = mins it takes to go from this level to the next, M = mins it takes to get to this level from lvl 1, 
            // t is fish needed to get from this level to the next, T is total fish needed to get to this level from lvl 1
            // s is an approximation of how long it takes to catch a fish, which obviously has a lot of variance due to fishing power
            // o is the ratio of how much "work" you have to do compared to at level 1 to ascend to the next level. goes down as l goes up if p is < 1. otherwise goes up
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string currGlowColor = GetCurrentGlowColor().Hex3();

            foreach (TooltipLine tl in tooltips)
            {
                tl.Text = tl.Text.Replace($"{BaseFishingPower}% fishing power", $"[c/{currGlowColor}:{BaseFishingPower + FishingPowerAdditiveIncrease}%] fishing power");
            }

            tooltips.Add(new TooltipLine(Mod, "level", $"Current level: [c/{currGlowColor}:{level}]"));
            tooltips.Add(new TooltipLine(Mod, "numBobbers", $"Fires [c/{currGlowColor}:{level}] fishing line{(level == 1 ? "" : "s")}"));
            tooltips.Add(new TooltipLine(Mod, "fishingSpeedInfo", $"Bobber projectile speed multiplier: [c/{currGlowColor}:{Math.Round(ShootSpeedMultiplier, 2)}]"));
            tooltips.Add(new TooltipLine(Mod, "fishingMultIncrease", $"[c/{currGlowColor}:{FishingPowerMultIncrease}%] multiplicative increase to fishing power"));
            tooltips.Add(new TooltipLine(Mod, "baitConsumptionInfo", $"[c/{currGlowColor}:{BaitConsumptionReductionPercent}%] multiplicative decrease to bait consumption"));
            tooltips.Add(new TooltipLine(Mod, "baitConsumptionInfo", $"The line never breaks at level [c/{currGlowColor}:{LineDoesntBreakLevel}]"));
            tooltips.Add(new TooltipLine(Mod, "crateChanceInfo", $"10% increased crate chance at level [c/{currGlowColor}:{CrateChanceLevel}]"));
            tooltips.Add(new TooltipLine(Mod, "canFishInLava", $"Can fish in lava at level [c/{currGlowColor}:{LavaFishingLevel}]"));
            tooltips.Add(new TooltipLine(Mod, "totalCaught", $"Total fish caught: [c/{currGlowColor}:{totalFishCaught}]"));
            tooltips.Add(new TooltipLine(Mod, "fishNeeded", $"Catch [c/{currGlowColor}:{fishTillNextLevel}] more fish to reach the next level"));
            tooltips.Add(new TooltipLine(Mod, "fishNeeded", $"[c/{currGlowColor}:]"));
            tooltips.Add(new TooltipLine(Mod, "val", $"Value: {Item.value / 100f} silver"));
        }


        private int glowColorIndex = 0;
        private int colorProgress = 0;
        private readonly int ticksPerColor = 45;

        // Called on all clients/server
        // For some reason setting accFishingLine to true in HoldItem doesn't make the line invincible. This is a workaround though. However
        // you must actually be holding it in your hand, not on your cursor
        public override void UpdateInventory(Player player)
        {
            if (player.HeldItem.ModItem is GarnsFishingRod rod && rod.level >= LineDoesntBreakLevel)
            {
                player.accFishingLine = true;
            }

            // This is for updating the tooltip color and the glow color when the item is held out
            int maxIndex = Math.Min(level, RainbowColors.Count);
            int nextIndex = (glowColorIndex + 1) % maxIndex;
            if (++colorProgress >= ticksPerColor)
            {
                colorProgress = 0;
                glowColorIndex = nextIndex;
            }
        }

        private Color GetCurrentGlowColor()
        {
            int maxIndex = Math.Min(level, RainbowColors.Count);
            int nextIndex = (glowColorIndex + 1) % maxIndex;

            Color nextColor = RainbowColors[nextIndex];
            Color currColor = RainbowColors[glowColorIndex];

            float progressPercent = colorProgress / (float)ticksPerColor;

            // It is weighted towards the next color, with the weight of nextColor being equal to our progress percent towards that color
            return nextColor.Average(currColor, progressPercent);
        }

        // Called on all clients/server every tick that the item is in their hand
        public override void HoldItem(Player player)
        {
            player.fishingSkill += FishingPowerAdditiveIncrease;
            if (!Main.dedServ)
            {
                Vector2 loc = player.position + new Vector2(player.direction * 5f, 5f);
                Lighting.AddLight(loc, GetCurrentGlowColor().ToVector3());
            }
        }


        // Called when the player right clicks. Normally used to dynamically decide if the item's alt function can be used, but I use this hook to change the shootmode
        public override bool AltFunctionUse(Player player)
        {
            shootMode = ((int)shootMode + 1) % ShootMode.Count;
            Main.NewText($"Changed item shoot mode to [c/{GetCurrentGlowColor().Hex3()}:{shootMode.Name}]");
            return false;
        }

        // Called on just the client that is shooting
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            velocity *= ShootSpeedMultiplier;
        }

        // Called on just the client that shot it
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Could be eventually set to a server-sided config called 'Broken'/'Unbalanced'. I made this just for fun
            int bonus = 0;

            if (level == 1)
            {
                for (int i = 0; i < 1 + bonus; i++)
                {
                    int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
                    GarnsFishingRodBobber spawnedBobber = (GarnsFishingRodBobber)Main.projectile[p].ModProjectile;
                    spawnedBobber.fishingLineColorIndex = 0;
                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p);
                }

                return false;
            }

            if (shootMode == ShootMode.Line)
            {
                ShootLine(position, velocity, source, player, type, bonus);
            }

            if (shootMode == ShootMode.Cone)
            {
                ShootCone(position, velocity, source, player, type, bonus);
            }

            if (shootMode == ShootMode.Compact)
            {
                ShootRandom(position, velocity, source, player, type, bonus);
            }

            if (shootMode == ShootMode.Auto)
            {
                ShootAuto(position, velocity, source, player, type, bonus);
            }

            return false;
        }

        private void ShootLine(Vector2 position, Vector2 velocity, EntitySource_ItemUse_WithAmmo source, Player player, int type, int bonus)
        {
            int bobberAmount = level;

            Vector2 current = velocity;

            for (int colorIndex = 0; colorIndex < bobberAmount; ++colorIndex)
            {
                for (int j = 0; j < 1 + bonus; j++)
                {
                    Vector2 bobberVector = current;
                    Vector2 bonusSpread = j == 0 ? new Vector2(0, 0) : GetBonusSpreadVector(bobberVector, 0.1f);

                    // Generate new bobbers
                    int p = Projectile.NewProjectile(source, position, bobberVector + bonusSpread, type, 0, 0f, player.whoAmI);

                    GarnsFishingRodBobber spawnedBobber = (GarnsFishingRodBobber)Main.projectile[p].ModProjectile;

                    // This client that spawns the projectile determines what the fishingLineColor is, but this occurs after OnSpawn() is called inside the created projectile...
                    spawnedBobber.fishingLineColorIndex = (byte)(colorIndex % RainbowColors.Count);

                    // ...so Send/ReceiveExtraAI already got called. Right here we are triggering a projectile Sync which will call Send/ReceiveExtraAI for us so the other clients/server know what the color is.
                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p);
                }

                current *= 0.9f;
            }
        }

        private void ShootCone(Vector2 position, Vector2 velocity, EntitySource_ItemUse_WithAmmo source, Player player, int type, int bonus)
        {
            int bobberAmount = level;

            float MinSpread = 10f;
            float MaxSpread = 120f;

            // Spread starts at MinSpread and scales up to MaxSpread depending on fishing rod level
            float spread = MinSpread + (float)level / MaxLevel * (MaxSpread - MinSpread);

            Vector2 current = velocity.RotatedBy(MathHelper.ToRadians(spread / 2));

            float increment = MathHelper.ToRadians(-spread / (bobberAmount - 1));

            for (int index = 0; index < bobberAmount; ++index)
            {
                for (int j = 0; j < 1 + bonus; j++)
                {
                    // Generate new bobbers
                    Vector2 bobberVector = current;
                    Vector2 bonusSpread = j == 0 ? new Vector2(0, 0) : GetBonusSpreadVector(bobberVector, 0.1f);

                    int p = Projectile.NewProjectile(source, position, bobberVector + bonusSpread, type, 0, 0f, player.whoAmI);

                    GarnsFishingRodBobber spawnedBobber = (GarnsFishingRodBobber)Main.projectile[p].ModProjectile;
                    spawnedBobber.fishingLineColorIndex = (byte)(index % RainbowColors.Count);
                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p);
                }

                current = current.RotatedBy(increment);
            }
        }

        private void ShootRandom(Vector2 position, Vector2 velocity, EntitySource_ItemUse_WithAmmo source, Player player, int type, int bonus)
        {
            int bobberAmount = level;

            for (int index = 0; index < bobberAmount; ++index)
            {
                for (int j = 0; j < 1 + bonus; j++)
                {
                    float spreadAmount = 20f; // how much the different bobbers are randomly spread out.

                    Vector2 bobberVector = velocity + new Vector2(Main.rand.NextFloat(-spreadAmount, spreadAmount) * 0.05f, Main.rand.NextFloat(-spreadAmount, spreadAmount) * 0.04f);
                    Vector2 bonusSpread = j == 0 ? new Vector2(0, 0) : GetBonusSpreadVector(bobberVector, 0.1f);

                    int p = Projectile.NewProjectile(source, position, bobberVector + bonusSpread, type, 0, 0f, player.whoAmI);
                    GarnsFishingRodBobber spawnedBobber = (GarnsFishingRodBobber)Main.projectile[p].ModProjectile;
                    spawnedBobber.fishingLineColorIndex = (byte)(index % RainbowColors.Count);

                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, p);
                }
            }
        }

        private void ShootAuto(Vector2 position, Vector2 velocity, EntitySource_ItemUse_WithAmmo source, Player player, int type, int bonus)
        {
            float rotation = MathHelper.ToDegrees(velocity.ToRotation());

            Main.NewText(rotation);

            // Threshhold of degrees (offset from <0, 0>, aka facing right) the velocity must be within to trigger 'ShootLine'
            float lineThreshold = 30; // 30 means that if we are looking within 30 degrees up, or within 30 degrees down (but not more), we will shoot a Line 

            if ((rotation > 0 - lineThreshold && rotation < lineThreshold) || (rotation < -180 + lineThreshold || rotation > 180 - lineThreshold))
            {
                // Should always be less than lineThreshold, since we are 'within' lineThreshold here
                float speedBoostThreshold = 18f;

                // Don't need to do the 0 || -180 checks here because Y > 0 means we know we are facing down
                if (velocity.Y > 0 && rotation > speedBoostThreshold && rotation < 180 - speedBoostThreshold)
                {
                    Main.NewText("x boost");
                    velocity = new Vector2(1.5f, 1) * velocity;
                }

                ShootLine(position, velocity * (new Vector2(1, 1)), source, player, type, bonus);
            }
            else
            {
                if (velocity.Y < 0)
                {
                    ShootRandom(position, velocity, source, player, type, bonus);
                    return;
                }

                velocity *= new Vector2(1f, 1f);
                ShootCone(position, velocity, source, player, type, bonus);
            }
        }

        private static Vector2 GetBonusSpreadVector(Vector2 bobberVector, float offsetSpeedPercent)
        {
            float speed = bobberVector.Length();
            return new Vector2(speed * offsetSpeedPercent) * Main.rand.NextVector2Unit();
        }

        public override void SaveData(TagCompound tag)
        {
            tag["level"] = level;
            tag["fishTillNextLevel"] = fishTillNextLevel;
            tag["totalFishCaught"] = totalFishCaught;
            tag["shootMode"] = (int)shootMode;
        }

        public override void LoadData(TagCompound tag)
        {
            int level = tag.Get<int>("level");
            int fishTillNextLevel = tag.Get<int>("fishTillNextLevel");
            int totalFishCaught = tag.Get<int>("totalFishCaught");
            shootMode = tag.Get<int>("shootMode");
            SetStats(level, fishTillNextLevel, totalFishCaught);
        }

        internal readonly struct ShootMode
        {
            internal static List<ShootMode> shootModes = new();

            public static int Count => shootModes.Count;

            public static readonly ShootMode Line = new("Line");
            public static readonly ShootMode Cone = new("Cone");
            public static readonly ShootMode Compact = new("Compact");
            public static readonly ShootMode Auto = new("Auto");

            internal int Value { get; }
            internal string Name { get; }

            private ShootMode(string name)
            {
                Value = shootModes.Count;
                Name = name;
                shootModes.Add(this);
            }

            public static explicit operator int(ShootMode m) => m.Value;

            public static implicit operator ShootMode(int i) => shootModes[i];

            public static bool operator ==(ShootMode m1, ShootMode m2) => m1.Value == m2.Value;
            public static bool operator !=(ShootMode m1, ShootMode m2) => m1.Value != m2.Value;

            public override bool Equals(object obj) => obj is ShootMode m && m.Value == Value;

            public override int GetHashCode() => Value.GetHashCode();
        }
    }
}