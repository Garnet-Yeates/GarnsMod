using GarnsMod.CodingTools;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace GarnsMod.Content.Items.Weapons.Melee.SlasherSwords
{
    public interface ISlasherSword
    {
        /// <summary>The Terraria.Item this is associated with. No need to implement as ModItem already does and ISlasherSword is implemented by our ModItems</summary>
        public Item Item { get; }

        /// <summary>Getter/Setter used solely by this interface (simply implement it, no logic required)</summary>
        public bool CanResetImmunity { get; set; }

        /// <summary>Getter/Setter used solely by this interface (simply implement it, no logic required)</summary>
        public bool CanHitNPCYet { get; set; }

        /// <summary>The offset for the sine function. This changes the angle that the sword starts at and the initial direction (up/down). It also effects R1 and R2</summary>
        public float Offset { get; }

        /// <summary>Represents the local maximum of the graph between 0 and 1</summary>
        public float R1 => GarnMathHelpers.Modulo(Offset + 0.25f, 1f);

        /// <summary>Represents the localminumum of the graph between 0 and 1</summary>
        public float R2 => GarnMathHelpers.Modulo(Offset - 0.25f, 1f);

        /// <summary> The animation progress (between 0..1) that this sword should be able to start dealing damage.
        /// The lower value of R1,R2 is used for the point where the sword is able to start dealing damage (must reach the first peak/vally before being able to deal damage)</summary>
        private float CanHitNPCAt => R1 < R2 ? R1 : R2;

        /// <summary> The animation progress (between 0..1) that immunity for enemies should get reset. This allows the sword to hit twice<br/>
        /// The higher value of R1,R2 is used for the point where immunity resets and the sword is able to hit a second time (must reach the second peak/vally before being able to hit again)</summary>
        private float ResetImmunityAt => R1 > R2 ? R1 : R2;

        /// <summary>Can be overriden. It is tuned for 'perfect diagonal' swords where the handle is in the bottom left</summary>
        public virtual float MaxAngleDeg => 120f;

        /// <summary>Can be overriden. It is tuned for 'perfect diagonal' swords where the handle is in the bottom left</summary>
        public virtual float MinAngleDeg => -25f;

        /// <summary>Hand rotation is normally based on the sword being a perfect diagonal. If the sword sprite is rotated slightly differently it needs to be compensated here</summary>
        public virtual float HandRotationOffset => 0f;

        public float GetAnimationProgress(Player player)
        {
            return 1 - player.itemAnimation / (float)player.itemAnimationMax;
        }

        public float GetAngleProgress(Player player)
        {
            float animationProgress = GetAnimationProgress(player);
            const float pi = MathHelper.Pi;

            // Per 1 animation cycle (animationProgress = x = 0...1) this item makes a full sine revolution (angleProgress = y = 0...1...0)
            return (float)((Math.Sin(2 * pi * animationProgress - 2 * pi * Offset) / 2) + 0.5); // Look in desmos with progress as x and offset as a slider between 0-1
        }

        /// <summary> The point of rotation is not in the bottom left corner so this method hopes to offset the draw position to compensate for this. It should be smoothed via interpolation 
        public Vector2 GetItemLocationOffset(Player player);

        public void SlasherUseItemFrame(Player player)
        {
            if (player.itemAnimation == player.itemAnimationMax)
            {
                CanResetImmunity = true;
            }

            CheckResetImmunity(player);
        }

        public void CheckResetImmunity(Player player)
        {
            float progress = GetAnimationProgress(player);

            if (progress >= CanHitNPCAt && !CanHitNPCYet) 
            {
                CanHitNPCYet = true;
                SoundEngine.PlaySound(SoundID.Item1);
            }
            else if (CanResetImmunity && progress >= ResetImmunityAt)
            {
                CanResetImmunity = false;
                SoundEngine.PlaySound(SoundID.Item1);

                foreach (NPC npc in Main.npc)
                {
                    if (npc.active)
                    {
                        npc.immune[player.whoAmI] = 0;
                    }
                }
            }
        }

        public float GetItemRotation(Player player)
        {
            float angleProgress = GetAngleProgress(player);
            float startAngle = MathHelper.ToRadians(MaxAngleDeg) * player.direction;
            float endAngle = MathHelper.ToRadians(MinAngleDeg) * player.direction;
            float currAngle = Utils.AngleLerp(startAngle, endAngle, angleProgress);
            return currAngle;
        }

        public void UseItemHitbox(Player player, ref Rectangle hitbox)
        {
            int width = (int) (Item.width * Item.scale);
            int height = (int) (Item.height * Item.scale);
            Vector2 vecToOtherCorner = new(width*1.1f * player.direction, -height*1.1f);

            Vector2 itemLoc = player.Center + new Vector2(player.direction * -2, -6);
            float rot = GetItemRotation(player);

            hitbox = GarnMathHelpers.RectFrom2Points(itemLoc, itemLoc + vecToOtherCorner.RotatedBy(rot));

            // Both hitbox width and height can never be less than half of the item width
            int diff = (int)(0.5f * width - hitbox.Width);

            if (diff > 0)
            {
                if (player.direction == 1)
                {
                    hitbox.Width += diff;
                }
                else
                {
                    hitbox = GarnMathHelpers.RectFrom2Points(new Vector2(hitbox.TopLeft().X - diff, hitbox.TopLeft().Y), hitbox.BottomRight());
                }
            }
            else
            {

            }
            hitbox.Height = Math.Max(height / 2, hitbox.Height);
        }

        public bool CanHitNPC(Player player)
        {
            if (player.itemAnimation == player.itemAnimationMax)
            {
                CanHitNPCYet = false;
            }

            return CanHitNPCYet;
        }

        public void UseStyle(Player player)
        {
            player.itemRotation = GetItemRotation(player);
            player.itemLocation = player.Center + GetItemLocationOffset(player);

            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, 0); // 195 for facing normal
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Quarter, player.itemRotation + MathHelper.ToRadians(player.direction * (225 + HandRotationOffset))); // rotate the arm in a slightly different way because the arm has a different starting angle
        }
    }
}