using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace GarnsMod.Tools
{
    internal static class MainHelpers
    {
        public static Vector2 MouseScreenWithoutZoom()
        {
            Vector2 screenSize = Main.ScreenSize.ToVector2();
            Vector2 smallScreenSize = screenSize / Main.GameZoomTarget;
            Vector2 zoomScreenOffset = (screenSize - smallScreenSize) / 2f;
            Vector2 smallScreenPos = Main.MouseScreen - zoomScreenOffset;
            Vector2 smallScreenPercThru = smallScreenPos / smallScreenSize;
            return screenSize * smallScreenPercThru;
        }

        public static Vector2 MouseWorldWithoutZoom()
        {
            return Main.MouseWorld + (Main.MouseScreen - MouseScreenWithoutZoom()) / Main.GameZoomTarget;
        }
    }
}
