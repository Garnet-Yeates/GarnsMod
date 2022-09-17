using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace GarnsMod.Content.Items.Weapons.SlasherSwords
{
    internal class GarnBlade : ModItem, ISlasherSword
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Garn's Blade");

            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }
        public override string Texture => "GarnsMod/Content/Items/Weapons/NorthernStarSword";

        public override void SetDefaults()
        {
            Item.damage = 40;
            Item.useTime = 100;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.useTurn = true;

            Item.UseSound = null;

            Item.width = 26;
            Item.height = 42;

            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 6;
            Item.crit = 12;

            Item.value = Item.buyPrice(gold: 10);
            Item.rare = ItemRarityID.Pink;
        }

        #region SlasherOverrides

        public float Offset => 0.25f;

        public bool CanResetImmunity { get; set; }

        public bool CanHitNPCYet { get; set; }

        public ISlasherSword SlasherSword => this;

        public override bool? CanHitNPC(Player player, NPC target)
        {
            if (!SlasherSword.CanHitNPC(player))
            {
                return false;
            }

            return null;
        }

        public override void UseItemFrame(Player player)
        {
            SlasherSword.SlasherUseItemFrame(player);
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            SlasherSword.UseStyle(player);
        }

        public override void UseItemHitbox(Player player, ref Rectangle hitbox, ref bool noHitbox)
        {
            SlasherSword.UseItemHitbox(player, ref hitbox);
        }

        #endregion
    }
}
        

