using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace GarnsMod.Content.Items.Weapons.SwingySwords
{
    internal class AncientSword : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ancient Stone Sword");

            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }
        public override string Texture => "GarnsMod/Content/Items/Weapons/NorthernStarSword";
        public override void SetDefaults()
        {
            Item.damage = 150;
            Item.useTime = 100;
            Item.useAnimation = 55;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.useTurn = true;

            Item.width = 26;
            Item.height = 42;

            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 12;
            Item.crit = 12;

            Item.value = Item.buyPrice(gold: 10);
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Pink;
        }

        private bool canResetImmunity = true;

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            SwingySwordHelpers.UseStyle(player, ref canResetImmunity);
        }

        // Called every frame the item is swung
        public override void UseItemHitbox(Player player, ref Rectangle hitbox, ref bool noHitbox)
        {
            SwingySwordHelpers.UseItemHitbox(player, Item, ref hitbox);
        }
    }
}
