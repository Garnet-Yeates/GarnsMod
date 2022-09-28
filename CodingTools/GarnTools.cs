using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace GarnsMod.CodingTools
{
    internal static class GarnTools
    {
        /// <summary>
        /// Attempts to insert a new TooltipLine directly after the Tooltipline whose text is equal to (<paramref name="query"/>)<br/>
        /// If it cannot find a TooltipLine with this text it will instead insert it at the end of the list
        /// </summary>
        public static void InsertAfter(this List<TooltipLine> tooltiplist, string query, Mod mod, string name, string text)
        {
            TooltipLine lineToInsert = new(mod, name, text);
            if (tooltiplist.IndexOf(query) is not int index || ++index == tooltiplist.Count - 1)
            {
                tooltiplist.Add(lineToInsert);
            }
            else
            {
                tooltiplist.Insert(index, lineToInsert);
            }
        }

        public static int? IndexOf(this List<TooltipLine> tooltipList, string tooltipText)
        {
            for (int i = 0; i < tooltipList.Count; i++)
            {
                if (tooltipList[i].Text == tooltipText)
                {
                    return i;
                }
            }
            return null;
        }

        public static Vector2 MouseScreenForUI()
        {
            return Main.MouseWorld.ToScreenPosition();
        }

        public static Vector2 MouseWorldWithoutZoom()
        {
            return Main.MouseWorld + (Main.MouseScreen - MouseScreenForUI()*Main.UIScale) / Main.GameZoomTarget / Main.UIScale;
            // same as Main.MouseWorld + (Main.MouseScreen - Vector2.Transform(Main.MouseScreen, Main.GameViewMatrix.ZoomMatrix)) / Main.GameZoomTarget / Main.UIScale;
        }
    }
}
