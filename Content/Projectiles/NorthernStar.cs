using GarnsMod.Content.Items.Weapons;
using GarnsMod.Content.Shaders;
using GarnsMod.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
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
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 900 * (Projectile.extraUpdates + 1);

        }

        private const int YThreshold = 250;

        private Color StarColor => NorthernStarSword.StarColors[starColorIndex];

        // Non deterministic data (randomly generated / set by the player who spawned it), needs to be synced 

        internal byte starColorIndex;  // Set by the Northern Starsword that shot this proj, then synced with NetMessage
        private float xTarget; // The exact x point it will home in on constantly (besides when frozen in air, and initially for a second or two if low alt). Set in onSpawn and synced automatically
        private float yTarget; // The y point where it stops falling upwards. Will be somewhere between YTargetBase +/- yThreshold/2. Set in onSpawn and synced auto

        // Only called on the instance who spawned it, and then NetUpdate is called
        public override void OnSpawn(IEntitySource source)
        {
            var (screenX, screenY) = Main.ScreenSize.ToVector2();
            xTarget = Projectile.position.X - screenX / 2 + Main.rand.NextFloat(screenX);
            yTarget = Projectile.position.Y - screenY / 2 - 600f - Main.rand.NextFloat(YThreshold);
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

        private NorthernStarPhase currPhase;

        private enum NorthernStarPhase : byte { Rising, Peaking, Falling };

        private bool Falling => currPhase == NorthernStarPhase.Falling;
        private bool Peaking => currPhase == NorthernStarPhase.Peaking;
        private bool Rising => currPhase == NorthernStarPhase.Rising;

        public override void AI()
        {
            numTicks++;

            if (technicallyDead)
            {
                Projectile.velocity = new Vector2(0, 0);
                return;
            }

            if (!Main.dedServ && !technicallyDead)
            {
                Lighting.AddLight(Projectile.Center, StarColor.ToVector3() * 0.5f);
            }

            switch (currPhase)
            {
                case NorthernStarPhase.Rising:
                    AI_Rising();
                    break;
                case NorthernStarPhase.Peaking:
                    AI_Peaking();
                    break;
                case NorthernStarPhase.Falling:
                    AI_Falling();
                    break;
            }
        }

        public void AI_Rising()
        {
            Vector2 pos = Projectile.Center;
            ref Vector2 velocity = ref Projectile.velocity;
            float dirToXTarget = (xTarget - pos.X).Cardinal();

            // Y logic
            if (Projectile.IsHigherUpThan(600) && Projectile.IsGoingTowardsSky())
            {
                Projectile.SlowY(0.99f);
                currPhase = NorthernStarPhase.Peaking;
            }

            // Slowly accelerate towards the sky if we are below our yTarget
            if (Projectile.IsLowerDownThan(yTarget))
            {
                velocity.Y -= 0.15f;
            }
            // Otherwise go into phase two
            else
            {
                currPhase = NorthernStarPhase.Peaking;
            }

            Projectile.CapYSpeed(25f);

            // X logic
            if (numTicks > 50 || Projectile.IsHigherUpThan(yTarget))
            {

                // Accelerate x towards target if we aren't going towards it, or we are going towards it but we are further than 150px away
                if (!Projectile.IsGoingTowardsX(xTarget) || Projectile.IsXFurtherThan(150f, xTarget))
                {
                    velocity.X += 1.15f * dirToXTarget;
                }
                else
                {
                    Projectile.SlowXIfCloserThan(50f, xTarget, 0.025f, 0.25f); // Slow between 2.5% and 25% based on how close we are within 50 units
                }
            }

            Projectile.CapXSpeed(30f);
        }

        public void AI_Peaking()
        {
            ref Vector2 velocity = ref Projectile.velocity;

            peakingFor++;

            // Add a bit of downwards accel...
            velocity.Y += 0.075f;

            // ...and also slow down our Y speed by 5% per tick if we are still going up
            if (Projectile.IsGoingTowardsSky())
            {
                Projectile.SlowY(0.045f);
            }

            if (peakingFor < 120) // After 130 ticks of peaking, the next phase will happen. Until then, the stars aren't allowed to fall down regardless of the accel we added above
            {
                if (Projectile.IsGoingTowardsHell())
                {
                    velocity.Y = 0;
                }
            }
            else // Move onto falling phase
            {
                currPhase = NorthernStarPhase.Falling;
                Projectile.penetrate = 6; // Give it its pen back, and then some
            }

            // Repeatedly slow down x velocity to halt our progression towards the X target for now
            Projectile.SlowX(0.1f);
        }

        public void AI_Falling()
        {
            Vector2 pos = Projectile.Center;
            ref Vector2 velocity = ref Projectile.velocity;
            float dirToXTarget = (xTarget - pos.X).Cardinal();

            fallingFor++;
            velocity.Y += 0.35f; // When we start dropping add even more downwards accel
            Projectile.CapYSpeed(30f);

            // x logic
            if (fallingFor > 30) // Resume our progression towards the x target if we've been falling for at least a second. A bit faster than before but slow at first
            {
                // Slow down x as it approaches its target (if it is facing it)
                if (pos.IsXCloserThan(100f, xTarget) && Projectile.IsGoingTowardsX(xTarget))
                {
                    Projectile.SlowX(0.05f);
                }
                else
                {
                    velocity.X += 1f * dirToXTarget * Utils.GetLerpValue(0, 1, fallingFor / 60f, true); // Use lerp to cap ratio value at 1
                }
            }

            Projectile.CapXSpeed(8f);
        }

        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (Falling) // In the falling phase we lose 0 pen (add 1 for net 0), effectively being able to penetrate infinitely
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

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            return base.TileCollideStyle(ref width, ref height, ref fallThrough, ref hitboxCenterFrac);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (technicallyDead)
            {
                return false;
            }

            if (Falling)
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

                    if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
                    {
                        Projectile.velocity.X = -oldVelocity.X;
                    }

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
                Main.EntitySpriteDraw(grayscaleTexture, drawPos, null, col, Projectile.rotation, grayscaleTexture.Size() / 2, starScale, SpriteEffects.None, 0);
            }
            return false;
        }

        // TL;DR we don't want the projectile to die immediately because that would make the trail just instantly disappear
        private void KillIt()
        {
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