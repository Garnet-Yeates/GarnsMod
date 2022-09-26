using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static GarnsMod.CodingTools.ColorGradient;
using Microsoft.Xna.Framework.Graphics;
using System;
using static GarnsMod.Content.Items.Tools.GarnsFishingRod;
using GarnsMod.Content.Shaders;
using GarnsMod.CodingTools;

namespace GarnsMod.Content.Projectiles
{
    public class GarnsFishingRodBobber : ModProjectile
    {
        // These fields are all specific to each projectile and are synced with ExtraAI
        internal byte fishingLineColorIndex;
        internal byte fishingRodLevel;
        internal TrailTypeMode trailTypeMode;
        internal TrailColorMode trailColorMode;

        // Useful information that we can deduce from AI/colorIndex. Not saved to the projectile as they are all getters
        private Color FishingLineColor => RainbowColors[fishingLineColorIndex];

        internal bool Chilling => Projectile.ai[1] == 0;
        internal bool Wigglin => Projectile.ai[1] < 0;
        internal bool CapturedItem => Projectile.ai[1] > 0;
        internal bool ReelingIn => Projectile.ai[0] != 0;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 75;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 3;
            DisplayName.SetDefault("Garn's Bobber");
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.BobberWooden);
            DrawOriginOffsetY = -8; // Adjusts the draw position
        }

        // When our bobber spawns, we initially set it to a random color. This color is normally overriden by our code in
        // GarnFishingRod.Shoot and then the overriden value is synced with SyncProjectile, but we set it to a random one in case they are
        // also using the MultipleLures mod, because the MultipleLures mod deletes the projectile when it spawns then spawns
        // a new one, so we would lose our color and they would all be red. This code basically makes it so that if MultipleLures
        // mod is on, they will be random colors instead of all red.
        public override void OnSpawn(IEntitySource source)
        {
            fishingRodLevel = 1;
            fishingLineColorIndex = (byte)Main.rand.Next(RainbowColors.Count);
            trailColorMode = 0;
            trailTypeMode = 0;
        }

        private int notWigglingFor = int.MaxValue - 1; // The bobber has to not be wiggling for at least 60 ticks in order for the trail to be drawn (see predraw)

        // Called on clients and servers
        public override void AI()
        {
            if (Wigglin)
            {
                notWigglingFor = 0;
                if (Main.rand.NextBool(3))
                {
                    Vector2 speed = Main.rand.NextVector2Circular(3f, 3f);
                    Dust d = Dust.NewDustPerfect(Projectile.position + VanillaDrawOffset, DustID.RainbowTorch, speed, 0, FishingLineColor);
                    d.noGravity = true;
                    d.scale = 1.25f + Main.rand.NextFloat() * 1.5f;
                }
            }
            else
            {
                notWigglingFor = Math.Min(notWigglingFor + 1, int.MaxValue - 1);
            }

            if (ReelingIn)
            {
                Projectile.extraUpdates = 2;

                if (CapturedItem)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.position + VanillaDrawOffset, DustID.RainbowTorch, new(0, 0), 0, FishingLineColor, 1f);
                    d.noGravity = false;
                }
            }

            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, FishingLineColor.ToVector3() * 0.75f);
            }
        }

        public override void PostAI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
        }


        public override void ModifyFishingLine(ref Vector2 lineOriginOffset, ref Color lineColor)
        {
            lineOriginOffset = new Vector2(47, -30);
            lineColor = FishingLineColor;
        }

        public int progressModifier = 0;

        public static readonly Vector2 VanillaDrawOffset = new(15f, 7f);

        public override bool PreDraw(ref Color lightColor)
        {
            // Cant make a trail in water if it has recently wiggled. Can always make a trail when dry
            if (!Wigglin && (notWigglingFor > 60 || !Projectile.wet))
            {
                ColorGradient trailGradient = null;
                TrailColorMode trailColorMode = this.trailColorMode;
                if (trailColorMode == TrailColorMode.AvailableColors && fishingRodLevel == 1)
                {
                    trailColorMode = TrailColorMode.SingleColor;
                }
                if (trailColorMode == TrailColorMode.SingleColor)
                {
                    trailGradient = new ColorGradient(new() { Color.Lerp(FishingLineColor, Color.White, 0.15f), FishingLineColor });
                }
                else if (trailColorMode == TrailColorMode.AvailableColors)
                {
                    if (fishingRodLevel >= RainbowColors.Count)
                    {
                        trailGradient = FullRainbowGradientsWithExtraStart[fishingLineColorIndex];
                        progressModifier += trailTypeMode == TrailTypeMode.Stream ? 15 : -2;
                    }
                    else
                    {
                        trailGradient = PartialRainbowGradients[fishingRodLevel - 1][fishingLineColorIndex];
                    }
                }
                TrailType type = trailTypeMode.CorrespondingTrailType;
                default(GradientTrailDrawer).Draw(Projectile, trailGradient, type, VanillaDrawOffset, progressModifier: progressModifier);
            }
            return false;
        }

        public override void PostDraw(Color lightColor)
        {
            // Vector from the top left of your screen (world x,y) to the center of the projectile. Think of a line drawn from the TL of your screen to the proj center
            // For EntitySpriteDraw (0,0) is the top left of the screen so we need this offset vector to find out where it i s in relation to the screen
            Vector2 bobberPos = Projectile.position - Main.screenPosition;

            // Offsetting because bobber is weird
            bobberPos += VanillaDrawOffset;

            SpriteEffects spriteEffects = Projectile.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.FlipHorizontally;
            Texture2D starTexture = ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/Content/Images/MultiColorStarCenter").Value;
            Texture2D grayscaleTexture = ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/Content/Images/MultiColorStarGrayscale").Value;

            float starScale = Projectile.scale * 1.0f;
            Main.EntitySpriteDraw(starTexture, bobberPos, null, new Color(180, 180, 180, 0), 0f, starTexture.Size() / 2, starScale, spriteEffects, 0);
            Color col = FishingLineColor;
            col.A = (byte)(col.A / 1.5f);
            Main.EntitySpriteDraw(grayscaleTexture, bobberPos, null, col, 0f, grayscaleTexture.Size() / 2, starScale, spriteEffects, 0);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(fishingLineColorIndex);
            writer.Write(fishingRodLevel);
            writer.Write((byte)trailColorMode);
            writer.Write((byte)trailTypeMode);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            fishingLineColorIndex = reader.ReadByte();
            fishingRodLevel = reader.ReadByte();
            trailColorMode = reader.ReadByte();
            trailTypeMode = reader.ReadByte();
        }
    }
}