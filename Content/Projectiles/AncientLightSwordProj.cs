using GarnsMod.Content.Items.Weapons;
using GarnsMod.Content.Shaders;
using GarnsMod.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static GarnsMod.Content.Items.Tools.GarnsFishingRod;
using static GarnsMod.Tools.ColorGradient;
using Color = Microsoft.Xna.Framework.Color;

namespace GarnsMod.Content.Projectiles
{
    internal class AncientLightSwordProj : ModProjectile
    {
        private Color StarColor => AncientLightSword.StarColors[starColorIndex];


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Example Bullet"); // The English name of the projectile
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 60; // The length of old position to be recorded
            ProjectileID.Sets.TrailingMode[Projectile.type] = 3; // The recording mode
        }

        public override void SetDefaults()
        {
            Projectile.usesLocalNPCImmunity = true; // Local immunity means immunity is per projectile inst per player (vs idStatic immunity which is per projectile type per player) (vs normal which is per player)
            Projectile.localNPCHitCooldown = 5;

            Projectile.width = 16; // The width of projectile hitbox
            Projectile.height = 16; // The height of projectile hitbox
            Projectile.friendly = true; // Can the projectile deal damage to enemies?
            Projectile.hostile = false; // Can the projectile deal damage to the player? 
            Projectile.DamageType = DamageClass.Ranged; // Is the projectile shoot by a ranged weapon?
            Projectile.penetrate = 8; // How many monsters the projectile can penetrate. (OnTileCollide below also decrements penetrate for bounces as well)
            Projectile.timeLeft = 1800; // The live time for the projectile (60 = 1 second, so 600 is 10 seconds)
            Projectile.ignoreWater = true; // Does the projectile's speed be influenced by water?
            Projectile.tileCollide = true; // Can the projectile collide with tiles?
            Projectile.extraUpdates = 0;

    //        AIType = ProjectileID.Bullet; // Act exactly like default Bullet
        }

        private static readonly int YThreshold = 1000;
        private static readonly float YTargetBase = 1500;


        // Set by the AncientLightSword that shot this proj, then synced with NetMessage
        internal byte starColorIndex;


        // Set in onSpawn() and synced automatically
        private float xTarget; // The exact x point it will home in on constantly (besides when frozen in air, and initially for a second or two if low alt). Set in onSpawn and synced automatically
        private float yTarget; // The y point where it stops falling upwards. Will be somewhere between YTargetBase +/- yThreshold/2. Set in onSpawn and synced auto
        
        // Deterministic, doesn't need to be synced
        private int numTicks = 0;
        private bool tileCollided = false;




        // Only called on the instance who spawned it and then NetUpdate is called
        public override void OnSpawn(IEntitySource source)
        {
            float screenSizeX = Main.ScreenSize.ToVector2().X;
            xTarget = Projectile.position.X - screenSizeX / 2 + Main.rand.NextFloat(screenSizeX);
            yTarget = YTargetBase - (YThreshold / 2) + Main.rand.NextFloat(YThreshold);
            // TODO sync tomorrow ALSO make sure u sync the color choice from Sword.cs
        }


        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            Projectile.penetrate-= 3;
            Projectile.penetrate = Math.Max(0, Projectile.penetrate);
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
//            base.OnHitNPC(target, damage, knockback, crit);
        }


        public override bool? CanHitNPC(NPC target)
        {
            if (tileCollided)
            {
                return false;
            }

            return null;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // If collide with tile, reduce the penetrate.
            if (hitPeak)
            {
                if (!tileCollided)
                {
                    for (int i = 0; i < 15; i++)
                    {
                        Vector2 speed = Main.rand.NextVector2Circular(1f, 1f) * 3f ;
                        List<Color> starColors = AncientLightSword.StarColors;
                        Dust d = Dust.NewDustDirect(Projectile.position, 10, 10, DustID.RainbowTorch, speed.X, speed.Y, 0, starColors[Main.rand.Next(starColors.Count)]);
                        d.noGravity = true;
                        d.scale = 1.25f + Main.rand.NextFloat() * 1.5f;
                    }


                    tileCollided = true;
                    Projectile.timeLeft = 100;
                    Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
                    SoundEngine.PlaySound(SoundID.Item10, Projectile.position);

                }
                return false;
            }

            Projectile.penetrate--;
            if (Projectile.penetrate <= 0)
            {
                Projectile.Kill();
            }
            else
            {
                Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
                SoundEngine.PlaySound(SoundID.Item10, Projectile.position);

                // If the projectile hits the left or right side of the tile, reverse the X velocity
                if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
                {
                    Projectile.velocity.X = -oldVelocity.X;
                }

                // If the projectile hits the top or bottom side of the tile, reverse the Y velocity
                if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
                {
                    Projectile.velocity.Y = -oldVelocity.Y;
                }
            }

            return false;
        }

        public override bool PreAI()
        {
            Projectile.extraUpdates = 1;
            return false ;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!tileCollided)
            {
                Texture2D starTexture = ModContent.Request<Texture2D>("GarnsMod/Content/Images/MultiColorStarCenter").Value;
                Texture2D grayscaleTexture = ModContent.Request<Texture2D>("GarnsMod/Content/Images/MultiColorStarGrayscale").Value;
                Texture2D bulletTex = ModContent.Request<Texture2D>("GarnsMod/Content/Projectiles/AncientLightSwordProj").Value;

                Vector2 origVec1 = new(bulletTex.Width * 0.5f, bulletTex.Height * 0.5f);
                Vector2 drawPos = Projectile.position + origVec1 - Main.screenPosition;
                drawPos += new Vector2(0f, Projectile.gfxOffY + 0f);

                ColorGradient grad = AncientLightSword.ColorGradients[starColorIndex];
                float? overrideOpacity = 1.5f; // 1.5 fire
                default(GradientTrailDrawer).Draw(Projectile, grad, TrailType.Fire, offset: origVec1, progressModifier: 0, overrideOpacity: overrideOpacity);

                Projectile.scale = 1.25f;
                float starScale = Projectile.scale;
                Main.EntitySpriteDraw(starTexture, drawPos, null, new Color(255, 255, 255, 255), Projectile.rotation, starTexture.Size() / 2, starScale, SpriteEffects.None, 0);
                Color col = StarColor;
                col.A = (byte)(col.A / 1.5f);
                Main.EntitySpriteDraw(grayscaleTexture, drawPos, null, col, Projectile.rotation, grayscaleTexture.Size() / 2, starScale, SpriteEffects.None, 0);
            }

            return false;

        }

        //      private readonly int decelMax = 0f;

        private bool hitPeak = false;
        private bool falling;
        private int fallingFor = 0;
        int hitPeakFor = 0;
        public override void PostAI()
        {
            numTicks++;

            if (!Main.dedServ && !tileCollided)
            {
                Lighting.AddLight(Projectile.Center, StarColor.ToVector3()*0.5f);
            }

            float y = Projectile.position.Y;
            float x = Projectile.position.X;
            if (falling)
            {
                fallingFor++;
            }

            // Y logic

            if (y < 300 && Projectile.velocity.Y < 0)
            {
                Projectile.velocity.Y *= 0.1f; // SKRRRT slow down if are are about to hit ceiling
            }

            if (hitPeak)
            {
                hitPeakFor++;
   
                // Add a tiny bit of downwards accel
                Projectile.velocity.Y += 0.05f;

                // Slow down if we are still going up
                if (Projectile.velocity.Y < 0)
                {
                    Projectile.velocity.Y *= 0.98f;
                }

                if (hitPeakFor < 250) // Don't drop down just yet
                {
                    if (Projectile.velocity.Y > 0)
                    {
                        Projectile.velocity.Y = 0;
                    }
                }
                else
                {
                    falling = true;
                    Projectile.velocity.Y += 0.2f; // When we start dropping add even more downwards accel

                    if (Projectile.velocity.Y > 30f)
                    {
                        Projectile.velocity.Y = 30f;
                    }
                }
                 
            }
            else // If we havent hit peak yet... 'fall upwards' until we do
            {
                if (Projectile.velocity.Y < -25f)
                {
                    Projectile.velocity.Y = -25f;
                }

                // Fall upwards and also pre
                if (y >= yTarget || numTicks <= 150)
                {
                    Projectile.velocity.Y -= 0.125f; // Fall upwards if we are below Y target OR if it hasn't been active for less than xxx ticks
                }
                else 
                {
                    hitPeak = true;
                }
            }

            // x logic
            if (numTicks > 25 || y < yTarget)
            {
                float xdiff = x - xTarget;
                int dirToTarget = xdiff > 0 ? -1 : 1;
                int myDir = Projectile.velocity.X > 0 ? 1 : 0;
                float distFromXTarget = Math.Abs(xdiff);
                int xSpeed = (int)Math.Abs(Projectile.velocity.X);

                // Accelerate x velocity towards xTarget

                float xAccel = 0.35f * dirToTarget;

                if (hitPeak)
                {
                    Projectile.velocity.X *= 0.95f;

                    if (!falling || fallingFor < 60)
                    {
                        xAccel = 0;
                    }
                    else
                    {
                        xAccel *= Terraria.Utils.GetLerpValue(0, 2, (fallingFor - 60 / 60f), true);
                    }
                }

                if (distFromXTarget < 20f && (myDir == dirToTarget))
                {
                    Projectile.velocity.X *= 0.875f;
   
                }

                if (xSpeed > 15f)
                {
                    Projectile.velocity.X *= 0.95f;
                }

                Projectile.velocity.X += xAccel;
   
            }


        }

        public override void Kill(int timeLeft)
        {
            // This code and the similar code above in OnTileCollide spawn dust from the tiles collided with. SoundID.Item10 is the bounce sound you hear.
            if (!tileCollided)
            {
                Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
                SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(xTarget);
            writer.Write(yTarget);
            writer.Write(starColorIndex);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            xTarget = reader.ReadSingle();
            yTarget = reader.ReadSingle();
            starColorIndex = reader.ReadByte();
        }
    }
}