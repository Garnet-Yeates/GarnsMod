using GarnsMod.Tools;
using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace GarnsMod.Content.Items.Weapons.SwingySwords
{
    public class SwingySwordHelpers
    {
        public static float GetAnimationProgress(Player player)
        {
            return 1 - player.itemAnimation / (float)player.itemAnimationMax;
        }

        public static float GetAngleProgress(Player player)
        {
            float animationProgress = GetAnimationProgress(player);
            float offset = 0.1f;
            const float pi = MathHelper.Pi;

            // Per 1 animation cycle (animationProgress = x = 0...1) this item makes a full sine revolution (angleProgress = y = 0...1...0)
            return (float)((Math.Sin(2 * pi * animationProgress - 2 * pi * offset) / 2) + 0.5); // Look in desmos with progress as x and offset as a slider between 0-1
        }

        // The point of rotation is not in the bottom left corner so this method hopes to offset the draw position to compensate for this. It is smoothed using interpolation. Makes a big difference
        public static Vector2 GetItemLocationOffset(Player player)
        {
            float angleProgress = GetAngleProgress(player);
            Vector2 itemLocationOffset = new(0, MathHelper.Lerp(0, -4f, angleProgress * (1f / 0.25f)));
            if (angleProgress > 0.25) itemLocationOffset = new(MathHelper.Lerp(0, -6 * player.direction, (angleProgress - 0.25f) * (1f / 0.75f)), -4f);

            return itemLocationOffset;
        }

        public static float GetItemRotation(Player player)
        {
            float angleProgress = GetAngleProgress(player);
            float startAngle = MathHelper.ToRadians(145f) * player.direction;
            float endAngle = MathHelper.ToRadians(-10f) * player.direction;
            float currAngle = Utils.AngleLerp(startAngle, endAngle, angleProgress);
            return currAngle;
        }

        public static void UseItemHitbox(Player player, Item item, ref Rectangle hitbox)
        {
            Vector2 vecToOtherCorner = new Vector2(item.width * player.direction, -item.height);

            Vector2 itemLoc = player.Center + new Vector2(player.direction * -2, -6);
            float rot = GetItemRotation(player);

            hitbox = GarnMathHelpers.RectFrom2Points(itemLoc, itemLoc + vecToOtherCorner.RotatedBy(rot));

            // Both hitbox width and height can never be less than half of the item width
            hitbox.Width = Math.Max(item.width / 2, hitbox.Width);
            hitbox.Height = Math.Max(item.width / 2, hitbox.Height);
        }

        public static void CheckResetImmunity(ref bool canResetImmunity, Player player)
        {
            float progress = GetAnimationProgress(player);

            if (canResetImmunity && progress >= 0.4f)
            {
                canResetImmunity = false;

                foreach (NPC npc in Main.npc)
                {
                    if (npc.active)
                    {
                        npc.immune[player.whoAmI] = 0;
                    }
                }
            }

            if (player.itemAnimation == 1)
            {
                canResetImmunity = true;
            }
        }

        public static void UseStyle(Player player, ref bool canResetImmunity)
        {
            // Progress is a number between 0-1 representing how far along the animation we are
            CheckResetImmunity(ref canResetImmunity, player);

            player.itemRotation = GetItemRotation(player);
            player.itemLocation = player.Center + GetItemLocationOffset(player);

            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.None, 0); // 195 for facing normal
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Quarter, player.itemRotation + MathHelper.ToRadians(player.direction * 200)); // rotate the arm in a slightly different way because the arm has a different starting angle
        }


    }
}
