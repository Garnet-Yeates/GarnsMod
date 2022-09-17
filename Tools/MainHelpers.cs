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
        // Main.MouseScreen if the pixel offset from the top right of your screen. No matter what your zoom is this will be the same if your cursor stays in place
        // For UI drawing, it wants the MouseScreen position AS IF zoom is at 100% (default) so this method finds where MouseScreen would be if we were zoomed out
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
