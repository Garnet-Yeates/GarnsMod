using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static GarnsMod.MyShaderDrawer;
using static GarnsMod.ColorHelper;

namespace GarnsMod.Content.Projectiles
{
    public class GarnsFishingRodBobber : ModProjectile
    {
        // This holds the index of the fishing line color in the ColorHelper.RainbowColors array.
        public byte fishingLineColorIndex;

        private Color FishingLineColor => RainbowColors[fishingLineColorIndex];

        private bool Chilling => Projectile.ai[1] == 0;
        private bool Jigglin => Projectile.ai[1] < 0;
        private bool CapturedItem => Projectile.ai[1] > 0;
        private bool ReelingIn => Projectile.ai[0] != 0;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 55;
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
            fishingLineColorIndex = (byte)Main.rand.Next(RainbowColors.Count);
        }

        // Called on clients and servers
        public override void AI()
        {
            if (ReelingIn)
            {
                Projectile.extraUpdates = 2;
            }

            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, FishingLineColor.ToVector3() * 0.75f);
            }
        }

        public override void ModifyFishingLine(ref Vector2 lineOriginOffset, ref Color lineColor)
        {
            lineOriginOffset = new Vector2(47, -30);
            lineColor = FishingLineColor;
        }


        public override bool PreDraw(ref Color lightColor)
        {
            if (!Jigglin)
            {
                var bbbb = ColorGradient.RainbowGradients[fishingLineColorIndex];
         //       default(MyShaderDrawer).Draw(Projectile, FishingLineColor, Color.White, new Vector2(8, 0));
                default(MyShaderDrawer).DrawGradient(Projectile, ColorGradient.RainbowGradients[fishingLineColorIndex], new Vector2(8, 0));
                
            }
            //   return false;
            return true;
        }


        public override Color? GetAlpha(Color lightColor)
        {
            // lightlevel represents the magnitude of light, where 0 is pitch black and 1 is full bright white light. We use this 'lightLevel' to limit our color expression
            float lightLevel = lightColor.ToVector3().Length() / 1.732f; // Highest length (full white, (1, 1, 1)) is 1.732 which is why we divide by that

            // The color of each bobber is 30% fishing line color, 70% white
            Vector3 white = new(1f, 1f, 1f);
            Vector3 color = FishingLineColor.ToVector3().Average(white, 0.3f);

            return new Color(color * lightLevel); // Here we use 'lightLevel' to limit our color expression so if it is dark you cannot see it
        }

        // Used for syncing. Called immediately after OnSpawn() as well as whenever MessageID.SyncProjectile is called (such as when .netupdate is called inside of AI())
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(fishingLineColorIndex);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            fishingLineColorIndex = reader.ReadByte();
        }
    }
}