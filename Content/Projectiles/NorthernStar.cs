using GarnsMod.Content.Items.Weapons;
using GarnsMod.Content.Shaders;
using GarnsMod.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.Graphics.VertexStrip;
using static tModPorter.ProgressUpdate;

namespace GarnsMod.Content.Projectiles
{
    internal class NorthernStar : ModProjectile
    {
        public override string Texture => "GarnsMod/Content/Images/MultiColorStarCenter";

        public override void SetStaticDefaults()
        {
            StarTexture = ModContent.Request<Texture2D>("GarnsMod/Content/Images/MultiColorStarCenter");
            GrayscaleTexture = ModContent.Request<Texture2D>("GarnsMod/Content/Images/MultiColorStarGrayscale");

            DisplayName.SetDefault("Northern Star");   
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 35; 
            ProjectileID.Sets.TrailingMode[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.usesLocalNPCImmunity = true; // Local immunity means immunity is per projectile inst per player (vs idStatic immunity which is per projectile type per player) (vs normal which is per player)
            Projectile.localNPCHitCooldown = 8;
            Projectile.width = 11;
            Projectile.aiStyle = 0;
            Projectile.height = 11; 
            Projectile.friendly = true; 
            Projectile.hostile = false; 
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 5; 
            Projectile.timeLeft = 1800; 
            Projectile.ignoreWater = true; 
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1;
        }

        private static readonly int YThreshold = 500;
        private static readonly float YTargetBase = 1500;

        private Color StarColor => NorthernStarSword.StarColors[starColorIndex];

        // Non deterministic data (randomly generated / set by the player who spawned it), needs to be synced 

        internal byte starColorIndex;  // Set by the Northern Starsword that shot this proj, then synced with NetMessage
        private float xTarget; // The exact x point it will home in on constantly (besides when frozen in air, and initially for a second or two if low alt). Set in onSpawn and synced automatically
        private float yTarget; // The y point where it stops falling upwards. Will be somewhere between YTargetBase +/- yThreshold/2. Set in onSpawn and synced auto

        // Only called on the instance who spawned it, and then NetUpdate is called
        public override void OnSpawn(IEntitySource source)
        {
            float screenSizeX = Main.ScreenSize.ToVector2().X;
            xTarget = Projectile.position.X - screenSizeX / 2 + Main.rand.NextFloat(screenSizeX);
            yTarget = YTargetBase - (YThreshold / 2) + Main.rand.NextFloat(YThreshold);
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
        private bool technicallyDead = false; // Did we get to the end of Falling phase and hit the ground yet? crashedDown = true basically means the proj is dead but we keep it alive for the trail
        private int fallingFor = 0; // How many ticks since Falling phase started
        private int peakingFor = 0; // How many ticks since Peak phase started

        private SwordProjectilePhase currPhase;

        private enum SwordProjectilePhase : byte { Upwards, Peaking, Falling };

   
        public override void AI()
        {
            
            numTicks++;

            if (technicallyDead)
            {
                Projectile.velocity = new Vector2(0, 0);
                return;
            }

            float y = Projectile.position.Y;
            float x = Projectile.position.X;

            Vector2 pos = Projectile.Center;
            Vector2 targetPosition = new(xTarget, yTarget);
            Vector2 targetsToMe = (Projectile.Center - targetPosition);
            Vector2 meToTargets = -targetsToMe;

            ref Vector2 velocity = ref Projectile.velocity;

            var (xDiffAbs, yDiffAbs) = targetsToMe.Abs();
            var (dirToXTarget, dirToYTarget) = meToTargets.Cardinals();
            var (xDir, yDir) = velocity.Cardinals();
            var (xVelAbs, yVelAbs) = velocity.Abs();

            if (!Main.dedServ && !technicallyDead) 
            {
                Lighting.AddLight(Projectile.Center, StarColor.ToVector3() * 0.5f);
            }

            switch (currPhase)
            {
                case SwordProjectilePhase.Upwards:

                    // Y logic

                    velocity.SlowYIfFasterThan(25f, 0.1f);

                    if (pos.IsHigherUpThan(400) && velocity.IsGoingTowardsSky()) 
                    {
                        velocity.SlowY(0.95f);
                    }

                    // If we are below or y target....
                    if (pos.IsLowerDownThan(yTarget))
                    {
                        if (numTicks > 20) // Accelerate upwards. Don't do this unless it's been at least 20 ticks
                        {
                            velocity.Y -= 0.15f;
                        }
                    }
                    else // Otherwise...
                    {
                        if (numTicks <= 150) // Go into peaking phase if it's been at least 150 ticks
                        {
                            velocity.Y -= 0.25f; // Make it accelerate up more rapidly than if they spawned it below the y target
                        }
                        else
                        {
                            currPhase = SwordProjectilePhase.Peaking;
                            Main.NewText("eek");
                        }

                    }

                    // X logic

                    if (numTicks > 40 || y < yTarget)
                    {
                        // Slow down x as it approaches its target (if it is facing it)
                        if (pos.IsXCloserThan(100f, xTarget) && velocity.IsGoingTowardsX(xTarget))
                        {
                            velocity.SlowX(0.05f);
                        }
                        else
                        {
                            velocity.X += 0.5f * dirToXTarget; // Accelerate x towards x target
                        }

                        // Cap x speed at 12 to prevent it from accelerating too fast over time then going back and fourth because it goes past its target
                        velocity.SlowXIfFasterThan(12f, 0.02f);
                    }

                    break;

                case SwordProjectilePhase.Peaking:
                    peakingFor++;

                    // Add a bit of downwards accel...
                    velocity.Y += 0.075f;

                    // ...and also slow down our Y speed by 5% per tick if we are still going up
                    if (velocity.IsGoingTowardsSky())
                    {
                        velocity.SlowY(0.05f);
                    }

                    if (peakingFor < 130) // After 130 ticks of peaking, the next phase will happen. Until then, the stars aren't allowed to fall down regardless of the accel we added above
                    {
                        if (velocity.IsGoingTowardsHell())
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
                    velocity.SlowX(0.05f);

                    break;
                case SwordProjectilePhase.Falling:
                    fallingFor++;

                    velocity.Y += 0.35f; // When we start dropping add even more downwards accel

                    velocity.SlowYIfFasterThan(30f, 0.1f);

                    // x logic

                    if (technicallyDead)
                    {
                        velocity.X = 0;
                    }
                    else
                    {
                        if (fallingFor > 60) // Resume our progression towards the x target if we've been falling for at least a second. A bit faster than before but slow at first
                        {
                            // Slow down x as it approaches its target (if it is facing it)
                            if (pos.IsXCloserThan(100f, xTarget) && velocity.IsGoingTowardsX(xTarget))
                            {
                                velocity.SlowX(0.05f);
                            }
                            else
                            {
                                velocity.X += 0.225f * dirToXTarget * Utils.GetLerpValue(0, 1, fallingFor / 60f, true); // Use lerp to make acceleration slow then fast. Rate of change of the rate of change of the rate of change....
                            }

                            // Cap x speed at 4 here
                            velocity.SlowXIfFasterThan(6f, 0.05f);
                        }
                    }
                    break;
            }
        }

        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (fallingFor > 0) // In the falling phase we lose 0 pen (add 1 for net 0), effectively being able to penetrate infinitely
            {
                damage *= 10;
                Projectile.penetrate += 1;
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (technicallyDead) // Don't hit NPCs when in the crashedDown (dead) phase
            {
                return false;
            }

            return null;
        }

        public override bool CanHitPvp(Player target)
        {
            return !technicallyDead; // ditto
        }

        private void KillIt()
        {
            // When we crash down we make the proj invisible for a little instead of deleting it immediately because we want the trail to fade out instead of disappearing
            // crashedDown = true basically means the projectile is dead but not *actually* dead *yet*
            technicallyDead = true;
            Projectile.timeLeft = 100;
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

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (technicallyDead)
            {
                return false;
            }

            if (fallingFor > 0)
            {
                    KillIt();
            }
            else
            {
                Projectile.penetrate--;
                if (Projectile.penetrate <= 0)
                {
                    Projectile.penetrate = 1; // During AI if the penetrate is ever 0 the projectile will insta die. This prevents that because we use our KillIt() method so our trail stays
                    KillIt();
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

        private static Asset<Texture2D> StarTexture;
        private static Asset<Texture2D> GrayscaleTexture;

        public override bool PreDraw(ref Color lightColor)
        {
            // Projectile.position is top left. hitboxOffset ensures that our drawing is always in the center of the hitbox
            Vector2 hitboxOffset = new(Projectile.width / 2, Projectile.height / 2 + Projectile.gfxOffY);
            Vector2 drawPos = Projectile.position + hitboxOffset - Main.screenPosition;

            ColorGradient grad = NorthernStarSword.NorthStarColorGradients[starColorIndex];
            float? overrideOpacity = 1.5f; // 1.5 fire  
            default(GradientTrailDrawer).Draw(Projectile, grad, TrailType.Fire, offset: hitboxOffset, progressModifier: 0, overrideOpacity: overrideOpacity);
            
            if (!technicallyDead)
            {
                Texture2D starTexture = StarTexture.Value;
                Texture2D grayscaleTexture = GrayscaleTexture.Value;

                float starScale = Projectile.scale;

                Main.EntitySpriteDraw(starTexture, drawPos, null, new Color(255, 255, 255, 255), Projectile.rotation, starTexture.Size() / 2, starScale, SpriteEffects.None, 0);
                Color col = StarColor;
             //   col.A = (byte)(col.A / 1.5f);
                Main.EntitySpriteDraw(grayscaleTexture, drawPos, null, col, Projectile.rotation, grayscaleTexture.Size() / 2, starScale, SpriteEffects.None, 0);
            }

            return false;
        }

        public override void Kill(int timeLeft)
        {
            if (!technicallyDead)
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