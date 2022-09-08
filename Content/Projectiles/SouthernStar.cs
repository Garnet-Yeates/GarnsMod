using GarnsMod.Content.Items.Weapons;
using GarnsMod.Content.Shaders;
using GarnsMod.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace GarnsMod.Content.Projectiles
{
    internal class SouthernStar : ModProjectile
    {
        public override string Texture => "GarnsMod/Content/Projectiles/StarBullet";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Copy of Northern Star");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 60; 
            ProjectileID.Sets.TrailingMode[Projectile.type] = 3; 
        }

        public override void SetDefaults()
        {
    //        Projectile.usesLocalNPCImmunity = true; // Local immunity means immunity is per projectile inst per player (vs idStatic immunity which is per projectile type per player) (vs normal which is per player)
    //        Projectile.localNPCHitCooldown = 8;
  
            Projectile.width = 22; 
            Projectile.aiStyle = 0;
            Projectile.height = 22;
            Projectile.friendly = true; 
            Projectile.hostile = false;  
            Projectile.DamageType = DamageClass.Ranged; 
            Projectile.penetrate = 6; 
            Projectile.timeLeft = int.MaxValue;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true; 
            Projectile.extraUpdates = 0;
      
        }

        // Non deterministic data (randomly generated or set by the player who spawned it), needs to be synced

        private static readonly int YThreshold = 500;
        private static readonly float YTargetBase = 1600;
        private Color StarColor => NorthernStarSword.StarColors[starColorIndex];

        internal byte starColorIndex;  // Set by the Northern Starsword that shot this proj, then synced with NetMessage

        private float xTarget; // The exact x point it will home in on constantly (besides when frozen in air, and initially for a second or two if low alt). Set in onSpawn and synced automatically
        private float yTarget; // The y point where it stops falling upwards. Will be somewhere between YTargetBase +/- yThreshold/2. Set in onSpawn and synced auto

        // Only called on the instance who spawned it, and then NetUpdate is called
        public override void OnSpawn(IEntitySource source)
        {
            float screenSizeX = Main.ScreenSize.ToVector2().X;
            xTarget = Projectile.position.X - screenSizeX / 2 + Main.rand.NextFloat(screenSizeX);
            yTarget = YTargetBase - (YThreshold / 2) + Main.rand.NextFloat(YThreshold);
            Projectile.rotation = Projectile.velocity.ToRotation();

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

        // Deterministic AI data, doesn't need to be synced

        private int numTicks = 0; // How long has this projectile been alive?
        private bool crashedDown = false; // Did we get to the end of Falling phase and hit the ground yet? crashedDown = true basically means the proj is dead but we keep it alive for the trail
        private int fallingFor = 0; // How many ticks since Falling phase started
        private int peakingFor = 0; // How many ticks since Peak phase started

        private SwordProjectilePhase currPhase;

        private enum SwordProjectilePhase : byte { Upwards, Peaking, Falling };

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            numTicks++;

            float y = Projectile.position.Y;
            float x = Projectile.position.X;

           /* Main.NewText($"DrawOffsetX {DrawOffsetX}");
            Main.NewText($"DrawOriginOffsetX {DrawOriginOffsetX}");
            Main.NewText($"DrawOriginOffsetY {DrawOriginOffsetY}");
            Main.NewText($"Projectile Position: {x} {y}");
            Main.NewText($"Projectile.width (hitbox width) {Projectile.width}");
            Main.NewText($"Projectile.height (hitbox height) {Projectile.height}");*/

            Vector2 targetPosition = new(xTarget, yTarget);
            Vector2 targetsToMe = (Projectile.position - targetPosition);
            Vector2 meToTargets = targetsToMe * -1;

            ref Vector2 velocity = ref Projectile.velocity;

            VectorExtensions.Deconstruct(targetsToMe.Abs(), out float xDiffAbs, out float yDiffAbs);
            VectorExtensions.Deconstruct(meToTargets.Cardinals(), out float dirToXTarget, out float dirToYTarget);
            VectorExtensions.Deconstruct(Projectile.velocity.Cardinals(), out float xDir, out float yDir);
            VectorExtensions.Deconstruct(Projectile.velocity, out float xVel, out float yVel);
            VectorExtensions.Deconstruct(Projectile.velocity.Abs(), out float xVelAbs, out float yVelAbs);

            if (!Main.dedServ && !crashedDown)
            {
                Lighting.AddLight(Projectile.Center, StarColor.ToVector3() * 0.5f);
            }

            switch (currPhase)
            {
                case SwordProjectilePhase.Upwards:

                    // Y logic

                    // Cap it from falling upwards faster than 25 pixels/t
                    if (velocity.Y < -20f)
                    {
                        velocity.Y = -20f;
                    }

                    if (y < 250 && Projectile.velocity.Y < 0) // Slow it down FAST (by 97.5% of its speed per tick) if it is getting very close to the ceiling of the world
                    {
                        Projectile.velocity.Y *= 0.01f;
                    }

                    // Accelerate towards the sky (some random yTarget very high up) if we are below our y target
                    if (y >= yTarget)
                    {
                        if (numTicks > 10)
                        {
                            velocity.Y -= 0.125f;
                        }
                    }
                    else // Otherwise see if the projectile is newly spawned. If it is, force it to go up a bit before phase 2 (so it doesn't instantly go into phase 2)
                    {
                        if (numTicks <= 120) // It MUST wait at least 120 ticks to go into Peaking phase, even if we spawned it above our y target or 'peak'
                        {
                            velocity.Y -= 0.25f; // Make it accelerate up more rapidly than if they spawned it low down 
                        }
                        else
                        {
                            currPhase = SwordProjectilePhase.Peaking;
                        }

                    }

                    // X logic

                    if (numTicks > 55 || y < yTarget)
                    {
                        // Slow down x as it approaches its target (if it is facing it)
                        if (xDiffAbs < 100f && (xDir == dirToXTarget))
                        {
                            Projectile.velocity.X *= 0.9f;
                        }
                        else
                        {
                            velocity.X += 0.5f * dirToXTarget; // Accelerate x towards x target
                        }

                        // Cap x speed at 12 to prevent it from accelerating too fast over time then going back and fourth because it goes past its target
                        if (xVelAbs > 12f)
                        {
                            velocity.X *= 0.98f;
                        }

                    }

                    break;

                case SwordProjectilePhase.Peaking:
                    peakingFor++;

                    // Add a tiny bit of downwards accel...
                    velocity.Y += 0.05f;

                    // ...and also slow down our Y speed by 3% per tick if we are still going up
                    if (velocity.Y < 0)
                    {
                        velocity.Y *= 0.97f;
                    }

                    if (peakingFor < 130) // After 130 ticks of peaking, the next phase will happen. Until then, the stars aren't allowed to fall down regardless of the accel we added above
                    {
                        if (velocity.Y > 0)
                        {
                            velocity.Y = 0;
                        }
                    }
                    else // Move onto falling phase
                    {
                        currPhase = SwordProjectilePhase.Falling;
                        Projectile.penetrate = 6; // Give it its pen back, and then some
                    }

                    // x logic

                    // Repeatedly slow down x velocity to halt our progression towards the X target for now
                    velocity.X *= 0.95f;

                    break;
                case SwordProjectilePhase.Falling:
                    fallingFor++;

                    velocity.Y += 0.25f; // When we start dropping add even more downwards accel

                    if (velocity.Y > 30f)
                    {
                        velocity.Y = 30f;
                    }

                    // x logic

                    if (crashedDown)
                    {
                        velocity.X = 0;
                    }
                    else
                    {
                        if (fallingFor > 60) // Resume our progression towards the x target if we've been falling for at least a second. A bit faster than before but slow at first
                        {
                            // Slow down x as it approaches its target (if it is facing it)
                            if (xDiffAbs < 100f && (xDir == dirToXTarget))
                            {
                                Projectile.velocity.X *= 0.9f;
                            }
                            else
                            {
                                velocity.X += 0.225f * dirToXTarget * Terraria.Utils.GetLerpValue(0, 1, (fallingFor - 60 / 60f), true); // Use lerp to make acceleration slow then fast. Rate of change of the rate of change of the rate of change....
                            }

                            // Cap x speed at 4 here
                            if (xVelAbs > 6f)
                            {
                                velocity.X *= 0.95f;
                            }
                        }
                    }
                    break;
            }
        }

        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            // Penetrate always goes down by 1 after an npc hit. If we aren't in the 'falling' phase, we lose an additional penetrate point so it is -2
            if (fallingFor <= 0)
            {
                Projectile.penetrate -= 1;
                Projectile.penetrate = Math.Max(0, Projectile.penetrate);
            }
            if (fallingFor > 0) // In the falling phase we lose 0 pen (add 1 for net 0), effectively being able to penetrate infinitely
            {
                damage *= 10;
                Projectile.penetrate += 1;
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (crashedDown) // Don't hit NPCs when in the crashedDown (dead) phase
            {
                return false;
            }

            return null;
        }

        public override bool CanHitPvp(Player target)
        {
            return !crashedDown; // ditto
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (fallingFor > 0)
            {
                if (!crashedDown)
                {
                    for (int i = 0; i < 15; i++)
                    {
                        Vector2 speed = Main.rand.NextVector2Circular(1f, 1f) * 3f;
                        List<Color> starColors = NorthernStarSword.StarColors;
                        Dust d = Dust.NewDustDirect(Projectile.position, 10, 10, DustID.RainbowTorch, speed.X, speed.Y, 0, starColors[Main.rand.Next(starColors.Count)]);
                        d.noGravity = true;
                        d.scale = 1.25f + Main.rand.NextFloat() * 1.5f;
                    }

                    // When we crash down we make the proj invisible for a little instead of deleting it immediately because we want the trail to fade out instead of disappearing
                    // crashedDown = true basically means the projectile is dead but not *actually* dead
                    crashedDown = true;
                    Projectile.timeLeft = 100;
                    Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
                    SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
                }
            }
            else
            {
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
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {     
            Texture2D bulletTex = ModContent.Request<Texture2D>("GarnsMod/Content/Projectiles/StarBullet").Value;

            Vector2 origVec1 = new(bulletTex.Width * 0.5f, bulletTex.Height * 0.5f);
            Vector2 drawPos = Projectile.position + origVec1 - Main.screenPosition;
            drawPos += new Vector2(0f, Projectile.gfxOffY + 0f);
          
            ColorGradient grad = NorthernStarSword.NorthStarColorGradients[starColorIndex];
            float? overrideOpacity = 1.25f; // 1.5 fire
            default(GradientTrailDrawer).Draw(Projectile, grad, TrailType.Fire, offset: origVec1, progressModifier: 0, overrideOpacity: overrideOpacity);

            Projectile.scale = 5;

            if (!crashedDown)
            {
                Texture2D starTexture = ModContent.Request<Texture2D>("GarnsMod/Content/Images/MultiColorStarCenter").Value;
                Texture2D grayscaleTexture = ModContent.Request<Texture2D>("GarnsMod/Content/Images/MultiColorStarGrayscale").Value;
                Projectile.scale = Projectile.scale; ;
                float starScale = Projectile.scale;
      //          Main.EntitySpriteDraw(bulletTex, drawPos, null, new Color(0, 0, 0, 255), Projectile.rotation, bulletTex.Size() / 2, starScale, SpriteEffects.None, 0);

                            Main.EntitySpriteDraw(starTexture, drawPos, null, new Color(255, 255, 255, 255), Projectile.rotation, starTexture.Size() / 2, starScale, SpriteEffects.None, 0);
                Color col = StarColor;
                col.A = (byte)(col.A / 1.5f);
                Main.EntitySpriteDraw(grayscaleTexture, drawPos, null, col, Projectile.rotation, grayscaleTexture.Size() / 2, starScale, SpriteEffects.None, 0);
            }
            return false;
        }

        public override void Kill(int timeLeft)
        {
            if (!crashedDown)
            {
                Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
                SoundEngine.PlaySound(SoundID.Item10, Projectile.position);

                for (int i = 0; i < 15; i++)
                {
                    Vector2 speed = Main.rand.NextVector2Circular(1f, 1f) * 3f;
                    List<Color> starColors = NorthernStarSword.StarColors;
                    Dust d = Dust.NewDustDirect(Projectile.position, 10, 10, DustID.RainbowTorch, speed.X, speed.Y, 0, starColors[Main.rand.Next(starColors.Count)]);
                    d.noGravity = true;
                    d.scale = 1.25f + Main.rand.NextFloat() * 1.5f;
                }
            }
        }
    }
}