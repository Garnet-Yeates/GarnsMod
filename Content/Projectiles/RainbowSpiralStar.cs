using GarnsMod.Content.Shaders;
using GarnsMod.CodingTools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.Graphics.VertexStrip;
using GarnsMod.Content.Items.Weapons.Melee;
using System.IO;

namespace GarnsMod.Content.Projectiles
{
    internal class RainbowSpiralStar : ModProjectile
    {
        public override string Texture => $"{nameof(GarnsMod)}/Content/Images/MultiColorStarCenter";

        private static Asset<Texture2D> StarTexture;
        private static Asset<Texture2D> GrayscaleTexture;

        public override void SetStaticDefaults()
        {
            StarTexture = ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/Content/Images/MultiColorStarCenter");
            GrayscaleTexture = ModContent.Request<Texture2D>($"{nameof(GarnsMod)}/Content/Images/MultiColorStarGrayscale");

            DisplayName.SetDefault("Northern Star");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 45;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.width = 11;
            Projectile.aiStyle = 0;
            Projectile.height = 11;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 200;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 500;
        }

        public static readonly Dictionary<int, ColorGradient> RainbowSpiralStarGradients = ColorGradient.GetRainbowGradientWithOffsetDict(extraStart: 1, extraLoops: 1, reverse: true);

        // Synced property
        private float SineOffset
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        // Synced property
        private int GradientIndex
        {
            get => (int)Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        // Deterministic properties
        private Vector2 wouldBePosition;
        private ColorGradient colorGradient;
        private int colorTicks = 0;
        private int numTicks = 0;
        private Vector2 lowerBound;
        private Vector2 upperBound;

        private const float ColorProgressRate = 4f; // Determines how fast the color of the trail/star shifts

        private float ColorProgress => GarnMathHelpers.Modulo(-colorTicks * ColorProgressRate / 1000f, 1f);

        private Color CurrentColor => colorGradient.GetColor(ColorProgress);

        public override void OnSpawn(IEntitySource source)
        {
            wouldBePosition = Projectile.position;
            colorGradient = RainbowSpiralStarGradients[GradientIndex];
            upperBound = Projectile.velocity.SafeNormalize(default).RotatedBy(-MathHelper.PiOver2);
            lowerBound = Projectile.velocity.SafeNormalize(default).RotatedBy(MathHelper.PiOver2);
        }

        // OnSpawn only called on owner client. GradientIndex and SineOffset are auto synced, but we need to initialize the defaults for other clients too
        // No SendExtraAI as it isnt needed. This is basicallly so other clients have an OnSpawn() call
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            wouldBePosition = Projectile.position;
            colorGradient = RainbowSpiralStarGradients[GradientIndex];
            upperBound = Projectile.velocity.SafeNormalize(default).RotatedBy(-MathHelper.PiOver2);
            lowerBound = Projectile.velocity.SafeNormalize(default).RotatedBy(MathHelper.PiOver2);
        }

        public override void AI()
        {
            numTicks++;

            if (numTicks > 10)
            {
                colorTicks++;
            }

            UpdatePosition();

            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, CurrentColor.ToVector3() * 0.5f);
            }
        }


        // Don't use vanilla position updating
        public override bool ShouldUpdatePosition()
        {
            return false;
        }


        private void UpdatePosition()
        {
            float ampMax = 25f;
            float ampTime = 30; // It takes 30 ticks for amp to grow to the max
            float ampMult = MathHelper.Lerp(0, ampMax, Math.Min(1, numTicks / ampTime));

            float frequencyMult = 0.25f * Projectile.velocity.Length();

            // numTicks divided by 20 is our x on the graph. Every 20 ticks, x goes up by 1
            float x = numTicks / 20f;
            const float pi = MathHelper.Pi;
            float sineResult = (float)Math.Sin(frequencyMult * x - 2 * pi * SineOffset) * ampMult;

            // Projectile position is offset from "wouldBePosition" based on sine function. "wouldBePosition" is the position calculated normally (i.e gets velocity added to it every tick)
            if (sineResult > 0)
            {
                Projectile.position = wouldBePosition + upperBound * sineResult;
            }
            else if (sineResult == 0)
            {
                Projectile.position = wouldBePosition;
            }
            else
            {
                Projectile.position = wouldBePosition + lowerBound * Math.Abs(sineResult);
            }

            // "undo" normal movement calculation beforehand or else our calculated position above will be offset by (velocity.X, velocity.Y) because vanilla adds velocity to pos after AI code.
      //      Projectile.position -= Projectile.velocity;

            // We always draw it at 0 degree rotation. But for the sake of the trail drawing accurately we still rotate the projectile to its direction
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Update our 'wouldBePosition' based on normal rules.
            wouldBePosition += Projectile.velocity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Proajectile.position is top left. hitboxOffset ensures that our drawing is always in the center of the hitbox
            Vector2 hitboxOffset = new(Projectile.width / 2, Projectile.height / 2 + Projectile.gfxOffY);
            Vector2 drawPos = Projectile.position + hitboxOffset - Main.screenPosition;

            default(GradientTrailDrawer).Draw(Projectile, colorGradient, TrailType.Fire, offset: hitboxOffset, progressModifier: (int)(-colorTicks * 1.17f * ColorProgressRate)); ;

            Texture2D starTexture = StarTexture.Value;
            Texture2D grayscaleTexture = GrayscaleTexture.Value;

            float starScale = Projectile.scale;

            Main.EntitySpriteDraw(starTexture, drawPos, null, new Color(255, 255, 255, 255), Projectile.rotation, starTexture.Size() / 2, starScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(grayscaleTexture, drawPos, null, CurrentColor, Projectile.rotation, grayscaleTexture.Size() / 2, starScale, SpriteEffects.None, 0);

            return false;
        }



        // TL;DR we don't want the projectile to die immediately because that would make the trail just instantly disappear
        public override void Kill(int timeLeft)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Item10, Projectile.position);

            for (int i = 0; i < 15; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(1f, 1f) * 3f;
                Dust d = Dust.NewDustDirect(Projectile.position, 10, 10, DustID.RainbowTorch, speed.X, speed.Y, 0, CurrentColor);
                d.noGravity = true;
                d.scale = 1.25f + Main.rand.NextFloat() * 1.5f;
            }
        }
    }
}
