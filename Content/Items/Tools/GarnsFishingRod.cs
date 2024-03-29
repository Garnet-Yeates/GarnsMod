﻿using GarnsMod.Content.Projectiles;
using GarnsMod.Content.Shaders;
using GarnsMod.UI.FishingRodUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static GarnsMod.CodingTools.ColorGradient;

namespace GarnsMod.Content.Items.Tools
{
    public class GarnsFishingRod : ModItem
    {
        // Constants
        public const int MaxLevel = 30;
        public const int BaseFishingPower = 20;
        public const int ValuePerFish = 100; // Each fish caught increases rod value by 1 silver

        public const int LineDoesntBreakLevel = 3;
        public const int CrateChanceLevel = 6;
        public const int LavaFishingLevel = 9;

        // Fields
        internal byte level = 1;
        internal int fishTillNextLevel = GetFishNeededAtLevel(1);
        internal int totalFishCaught = 0;

        internal ShootMode shootMode = ShootMode.Cone;
        internal TrailColorMode trailColorMode = TrailColorMode.SingleColor;
        internal TrailTypeMode trailTypeMode = TrailTypeMode.Plain;

        // Properties (all based on fields)
        public float ShootSpeedMultiplier => 1 + 1f * ((level - 1.0f) / (MaxLevel - 1.0f)); // ShootSpeed => 1x to 2x

        public float BaitConsumptionReductionPercent => level * 2.5f; // Multiplicative, better than additive for high bait power

        public float FishingPowerMultIncrease => level * 1.0f;

        public int FishingPowerAdditiveIncrease => (int)(level * 2.0f);

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Garn's Rainbow Fishing Rod");
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
            writer.Write((byte)shootMode);
            writer.Write((byte)trailColorMode);
            writer.Write((byte)trailTypeMode);
        }

        public override void NetReceive(BinaryReader reader)
        {
            byte level = reader.ReadByte();
            int fishTillNextLevel = reader.ReadInt32();
            int totalFishCaught = reader.ReadInt32();
            shootMode = reader.ReadByte();
            trailColorMode = reader.ReadByte();
            trailTypeMode = reader.ReadByte();
            SetStats(level, fishTillNextLevel, totalFishCaught);
        }

        // Called by LoadData() and NetReceive() to set the item's values to the loaded/syned values
        public void SetStats(byte level, int fishTillNextLevel, int totalFishCaught)
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

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // SyncEquipment is similar to SyncItem but it is for items that are inside a player's inventory as opposed to out in the world
                NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, Main.myPlayer, inventoryIndex, Item.prefix);
            }
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
            const float p = 1.1f; // p being below 1 means it gets easier and easier to level up as you level up
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
        private const int ticksPerColor = 45;

        // Called on all clients/server 
        public override void UpdateInventory(Player player)
        {
            // Setting accFishingLine to true in HoldItem doesn't make the line invincible.  This is a workaround. Note that this won't work if it's held on cursor
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
            return Color.Lerp(currColor, nextColor, progressPercent);
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

        // Called when the player right clicks. Normally used to dynamically decide if the item's alt function can be used, but I use this hook to open UI
        public override bool AltFunctionUse(Player player)
        {
            FishingRodUISystem system = ModContent.GetInstance<FishingRodUISystem>();
            if (system.IsFishingRodUIOpen)
            {
                system.CloseFishingRodUI();
            }
            else
            {
                system.OpenFishingRodUI(shootMode, trailColorMode, trailTypeMode, player.selectedItem);
            }
            return false;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            velocity *= ShootSpeedMultiplier;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Could be eventually set to a server-sided config called 'Broken'/'Unbalanced'. I made this just for fun
            int bonus = 0;

            if (level == 1)
            {
                for (int i = 0; i < 1 + bonus; i++)
                {
                    Vector2 bonusSpread = i == 0 ? default : GetBonusSpreadVector(velocity, 0.15f);
                    ShootBobber(0, source, position, velocity + bonusSpread);
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

            if (shootMode == ShootMode.Auto)
            {
                ShootAuto(position, velocity, source, player, type, bonus);
            }

            return false;
        }

        // Helper method for Shoot method. Should only be called from Shoot method in GarnsFishingRod (or similar) because it doesn't do any net check before Projectile.NewProjectile and assumes the owner of the proj is Main.myPlayer
        private void ShootBobber(int fishingLineColorIndex, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity)
        {
            GarnsFishingRodBobber newBobber = (GarnsFishingRodBobber)Projectile.NewProjectileDirect(source, position, velocity, ModContent.ProjectileType<GarnsFishingRodBobber>(), 0, 0f, Main.myPlayer).ModProjectile;
            newBobber.fishingLineColorIndex = (byte)fishingLineColorIndex;
            newBobber.fishingRodLevel = level;
            newBobber.trailColorMode = trailColorMode;
            newBobber.trailTypeMode = trailTypeMode;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, newBobber.Projectile.whoAmI);
            }
        }

        private void ShootLine(Vector2 position, Vector2 velocity, EntitySource_ItemUse_WithAmmo source, Player player, int type, int bonus)
        {
            int bobberAmount = level;

            Vector2 current = velocity;
            float xDec = velocity.X * 0.66f / bobberAmount;
            float yDec = velocity.Y * 0.66f / bobberAmount;

            for (int colorIndex = 0; colorIndex < bobberAmount; ++colorIndex)
            {
                for (int j = 0; j < 1 + bonus; j++)
                {
                    Vector2 bobberVector = current;
                    Vector2 bonusSpread = j == 0 ? default : GetBonusSpreadVector(bobberVector, 0.15f);
                    ShootBobber(colorIndex % RainbowColors.Count, source, position, bobberVector + bonusSpread);
                }

                current -= new Vector2(xDec, yDec); ;
            }
        }

        private void ShootCone(Vector2 position, Vector2 velocity, EntitySource_ItemUse_WithAmmo source, Player player, int type, int bonus)
        {
            int bobberAmount = level;

            float MinSpread = MathHelper.ToRadians(10f) / 2;
            float MaxSpread = MathHelper.ToRadians(120f) / 2;

            // Spread starts at MinSpread and scales up to MaxSpread depending on fishing rod level
            float spread = MinSpread + (float)level / MaxLevel * (MaxSpread - MinSpread);

            for (int colorIndex = 0; colorIndex < bobberAmount; ++colorIndex)
            {
                for (int j = 0; j < 1 + bonus; j++)
                {
                    // Generate new bobbers
                    float rot = Utils.AngleLerp(-spread, spread, (float)colorIndex / bobberAmount);
                    Vector2 bobberVector = velocity.RotatedBy(rot);
                    Vector2 bonusSpread = j == 0 ? default : GetBonusSpreadVector(bobberVector, 0.15f);
                    ShootBobber(colorIndex % RainbowColors.Count, source, position, bobberVector + bonusSpread);
                }
            }
        }

        private void ShootAuto(Vector2 position, Vector2 velocity, EntitySource_ItemUse_WithAmmo source, Player player, int type, int bonus)
        {
            float rotation = MathHelper.ToDegrees(velocity.ToRotation());

            // Threshhold of degrees (offset from <0, 0>, aka facing right) the velocity must be within to trigger 'ShootLine'
            float lineThreshold = 30; // 30 means that if we are looking within 30 degrees up, or within 30 degrees down (but not more), we will shoot a Line 

            if ((rotation > 0 - lineThreshold && rotation < lineThreshold) || (rotation < -180 + lineThreshold || rotation > 180 - lineThreshold))
            {
                // Should always be less than lineThreshold, since we are 'within' lineThreshold here
                float speedBoostThreshold = 18f;

                // Don't need to do the 0 || -180 checks here because Y > 0 means we know we are facing down
                if (velocity.Y > 0 && rotation > speedBoostThreshold && rotation < 180 - speedBoostThreshold)
                {
                    velocity = new Vector2(2f, 1) * velocity;
                }

                ShootLine(position, velocity * (new Vector2(1, 1)), source, player, type, bonus);
            }
            else
            {
                velocity *= new Vector2(1f, 1f);
                ShootCone(position, velocity, source, player, type, bonus);
            }
        }

        private Vector2 GetBonusSpreadVector(Vector2 bobberVector, float offsetSpeedPercent)
        {
            float speed = bobberVector.Length();
            offsetSpeedPercent *= 1 + 0.5f * (level / (float)MaxLevel);
            return new Vector2(speed * offsetSpeedPercent) * Main.rand.NextVector2Unit();
        }

        public override void SaveData(TagCompound tag)
        {
            tag["level"] = level;
            tag["fishTillNextLevel"] = fishTillNextLevel;
            tag["totalFishCaught"] = totalFishCaught;
            tag["shootMode"] = (byte)shootMode;
            tag["trailColorMode"] = (byte)trailColorMode;
            tag["trailTypeMode"] = (byte)trailTypeMode;
        }

        public override void LoadData(TagCompound tag)
        {
            byte level = tag.Get<byte>("level");
            int fishTillNextLevel = tag.Get<int>("fishTillNextLevel");
            int totalFishCaught = tag.Get<int>("totalFishCaught");
            shootMode = tag.Get<byte>("shootMode");
            trailColorMode = tag.Get<byte>("trailColorMode");
            trailTypeMode = tag.Get<byte>("trailTypeMode");
            SetStats(level, fishTillNextLevel, totalFishCaught);
        }

        internal readonly struct ShootMode
        {
            internal static List<ShootMode> shootModes = new();

            public static int Count => shootModes.Count;

            public static readonly ShootMode Line = new("Line", ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/UI/FishingRodUI/ShootMode_Line"));
            public static readonly ShootMode Cone = new("Cone", ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/UI/FishingRodUI/ShootMode_Cone"));
            public static readonly ShootMode Auto = new("Auto", ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/UI/FishingRodUI/ShootMode_Auto"));

            internal int Value { get; }
            internal string Name { get; }
            internal Asset<Texture2D> TextureAsset { get; }

            private ShootMode(string name, Asset<Texture2D> asset)
            {
                Value = shootModes.Count;
                TextureAsset = asset;
                Name = name;
                shootModes.Add(this);
            }

            public static explicit operator int(ShootMode m) => m.Value;

            public static implicit operator ShootMode(int i) => shootModes[i];

            public static bool operator ==(ShootMode m1, ShootMode m2) => m1.Value == m2.Value;
            public static bool operator !=(ShootMode m1, ShootMode m2) => m1.Value != m2.Value;
            public override int GetHashCode() => Value;
        }

        internal readonly struct TrailColorMode
        {
            internal static List<TrailColorMode> colorModes = new();

            public static int Count => colorModes.Count;

            public static readonly TrailColorMode SingleColor = new("Single Color", ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/UI/FishingRodUI/TrailColor_Single"));
            public static readonly TrailColorMode AvailableColors = new("Available Colors", ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/UI/FishingRodUI/TrailColor_Available"));

            internal int Value { get; }
            internal string Name { get; }
            internal Asset<Texture2D> TextureAsset { get; }

            private TrailColorMode(string name, Asset<Texture2D> asset)
            {
                Value = colorModes.Count;
                TextureAsset = asset;
                Name = name;
                colorModes.Add(this);
            }

            public static explicit operator int(TrailColorMode m) => m.Value;

            public static implicit operator TrailColorMode(int i) => colorModes[i];

            public static bool operator ==(TrailColorMode m1, TrailColorMode m2) => m1.Value == m2.Value;
            public static bool operator !=(TrailColorMode m1, TrailColorMode m2) => m1.Value != m2.Value;

            public override int GetHashCode() => Value;
        }

        internal readonly struct TrailTypeMode
        {
            internal static List<TrailTypeMode> typeModes = new();

            public static int Count => typeModes.Count;

            public static readonly TrailTypeMode Plain = new("Plain", TrailType.Plain, ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/UI/FishingRodUI/TrailType_Plain"));
            public static readonly TrailTypeMode Fire = new("Fire", TrailType.Fire, ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/UI/FishingRodUI/TrailType_Fire"));
            public static readonly TrailTypeMode Stream = new("Stream", TrailType.Stream, ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/UI/FishingRodUI/TrailType_Stream"));

            internal Asset<Texture2D> TextureAsset { get; }
            internal string Name { get; }
            internal TrailType CorrespondingTrailType { get; }
            private int Value { get; }

            // trailType parameter in constructor must be declared as an int (not TrailType.whatever) because it is possible that this gets initialized before TrailType enum
            private TrailTypeMode(string name, TrailType trailType, Asset<Texture2D> asset)
            {
                Value = typeModes.Count;
                TextureAsset = asset;
                Name = name;
                CorrespondingTrailType = trailType;
                typeModes.Add(this);
            }

            public static explicit operator int(TrailTypeMode m) => m.Value;

            public static implicit operator TrailTypeMode(int i) => typeModes[i];

            public static bool operator ==(TrailTypeMode m1, TrailTypeMode m2) => m1.Value == m2.Value;
            public static bool operator !=(TrailTypeMode m1, TrailTypeMode m2) => m1.Value != m2.Value;
            public override int GetHashCode() => Value;
        }
    }

    // This ModPlayer is used to make things happen when the player fishes specifically with GarnsFishingRod
    public class GarnsFishingRodPlayer : ModPlayer
    {
        // This is used for multiplicatively increasing fishing skill. Works similar to how time of day/moonphase/weather affects it
        // Pretty sure it is only called on the client who is fishing
        // For our additive increase we do it in GarnFishingRod.HoldItem
        public override void GetFishingLevel(Item fishingRod, Item bait, ref float fishingLevelMultiplier)
        {
            if (fishingRod.ModItem is GarnsFishingRod rod)
            {
                fishingLevelMultiplier += rod.FishingPowerMultIncrease * 0.01f;
            }
        }

        // Only called on the client fishing
        public override void ModifyFishingAttempt(ref FishingAttempt attempt)
        {
            // This if block runs if the player is fishing with Garn's fishing rod
            if (attempt.playerFishingConditions.Pole.ModItem is GarnsFishingRod rod)
            {
                // The pole can fish in lava at [LavaFishingLevel]
                if (rod.level >= GarnsFishingRod.LavaFishingLevel)
                {
                    attempt.CanFishInLava = true;
                }

                // At level [CrateChanceLevel], there is 10% additional chance that the catch will be a crate
                // The "tier" of the crate depends on the rarity, which we don't modify here (see ExampleMod ExampleFishingPlayer.CatchFish comments)
                if (!attempt.crate && rod.level >= GarnsFishingRod.CrateChanceLevel)
                {
                    if (Main.rand.Next(100) < 10)
                    {
                        attempt.crate = true;
                    }
                }
            }
        }

        // Only called on the client who is fishing
        public override bool? CanConsumeBait(Item bait)
        {
            PlayerFishingConditions conditions = Player.GetFishingConditions(); ;

            // This makes it so there is a multiplicative % decrease in bait consumption. We return false or null which means "dont consume" or "let vanilla decide"
            if (conditions.Pole.ModItem is GarnsFishingRod rod)
            {
                return Main.rand.Next(100) < rod.BaitConsumptionReductionPercent ? false : null;
            }

            return null; // Let the default vanilla logic run
        }

        // Only called on the client that is fishing
        public override void ModifyCaughtFish(Item fish)
        {
            if (Player.GetFishingConditions().Pole.ModItem is GarnsFishingRod rod)
            {
                rod.OnCatchFish();
            }
        }
    }
}