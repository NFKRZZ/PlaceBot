using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace PlaceBot2._0
{
    public class ColorMapperz
    {
        public static readonly Dictionary<string, int> COLOR_MAP = new Dictionary<string, int>
    {
        { "#6D001A",0 },    // burgundy
        { "#BE0039",1},     // dark red
        { "#FF4500", 2 },   // red
        { "#FFA800", 3 },   // orange T
        { "#FFD635", 4 },   // yellow T
        { "#FFF8B8",5 },    // pale yellow
        { "#00A368", 6 },   // dark green
        { "#00CC78",7 },    // green
        { "#7EED56", 8 },   // light green
        { "#00756F",9 },    // dark teal
        { "#009EAA",10 },   // teal
        { "#00CCC0",11 },   // light teal
        { "#2450A4", 12 },  // dark blue
        { "#3690EA", 13 },  // blue T
        { "#51E9F4", 14 },  // light blue
        { "#493AC1",15 },   // indigo
        { "#6A5CFF",16},    // periwinkle
        { "#94B3FF",17 },   // lavender
        { "#811E9F", 18 },  // dark purple
        { "#B44AC0", 19 },  // purple T
        { "#E4ABFF",20 },   // pale purple
        { "#DE107F",21 },   // magenta
        { "#FF3881",22 },   // pink
        { "#FF99AA", 23 },  // light pink
        { "#6D482F",24 },   // dark brown
        { "#9C6926", 25 },  // brown
        { "#FFB470", 26 },  // beige
        { "#000000", 27 },  // black T
        { "#515252", 28 },  // dark grey
        { "#898D90", 29 },  // grey
        { "#D4D7D9", 30 },  // light grey
        { "#FFFFFF", 31 },  // white, T
    };

        public static readonly Dictionary<int, string> NAME_MAP = new Dictionary<int, string>
    {
        { 0,"Burgundy" },
        { 1,"Dark Red" },
        { 2, "Red" },
        { 3, "Orange" },
        { 4, "Yellow" },
        { 5, "Pale Yellow" },
        { 6, "Green" },
        { 7, "Green" },
        { 8, "Light Green" },
        { 9, "Dark Teal" },
        { 10, "Teal" },
        { 11, "Light Teal" },
        { 12, "Dark Blue" },
        { 13, "Blue" },
        { 14, "Light Blue" },
        { 15, "Indigo" },
        { 16, "Periwinkle" },
        { 17, "Lavender" },
        { 18, "Dark Purple" },
        { 19, "Purple" },
        { 20, "Pale Purple" },
        { 21, "Magenta" },
        { 22, "Pink" },
        { 23, "Light Pink" },
        { 24, "Dark Brown" },
        { 25, "Brown" },
        { 26, "Beige" },
        { 27, "Black" },
        { 28, "Dark Grey" },
        { 29, "Grey" },
        { 30, "Light Grey" },
        { 31, "White" },
    };

        public static string RgbToHex(Color color)
        {
            return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }

        public static string ColorIdToName(int colorId)
        {
            if (NAME_MAP.TryGetValue(colorId, out string name))
                return $"{name} ({colorId})";

            return $"Invalid Color ({colorId})";
        }

        public static Color ClosestColor(Color targetColor, List<Color> rgbColorsArray, bool legacyTransparency)
        {
            if (targetColor.A == 0)
                return Color.FromArgb(69, 42, 0);

            if (targetColor.R == 69 && targetColor.G == 42 && targetColor.B == 0 && legacyTransparency)
                return Color.FromArgb(69, 42, 0);

            int r = targetColor.R;
            int g = targetColor.G;
            int b = targetColor.B;

            double minColorDiff = double.MaxValue;
            Color closestColor = Color.Black; // Fallback color if no match found

            foreach (var color in rgbColorsArray)
            {
                int cr = color.R;
                int cg = color.G;
                int cb = color.B;

                double colorDiff = Math.Sqrt((r - cr) * (r - cr) + (g - cg) * (g - cg) + (b - cb) * (b - cb));
                if (colorDiff < minColorDiff)
                {
                    minColorDiff = colorDiff;
                    closestColor = color;
                }
            }

            return closestColor;
        }

        public static List<Color> GenerateRgbColorsArray()
        {
            List<Color> colorsArray = new List<Color>();

            foreach (string colorHex in COLOR_MAP.Keys)
            {
                Color color = FromHtml(colorHex);
                colorsArray.Add(color);
            }

            return colorsArray;
        }
        public static Color FromHtml(string htmlColor)
        {
            // Remove leading '#' if present
            if (htmlColor.StartsWith("#"))
            {
                htmlColor = htmlColor.Substring(1);
            }

            // Parse the hexadecimal color values
            int red = int.Parse(htmlColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            int green = int.Parse(htmlColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            int blue = int.Parse(htmlColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            // Create the Color object
            Color color = Color.FromArgb(red, green, blue);
            return color;
        }
        static double CalculateColorDistance(Color color1, Color color2)
        {
            int dr = color2.R - color1.R;
            int dg = color2.G - color1.G;
            int db = color2.B - color1.B;

            return Math.Sqrt(dr * dr + dg * dg + db * db);
        }
        public static int getIntFromColor(Color c)
        {
            Color inputRgbColor = c;
            double minDistance = double.MaxValue;
            int closestColorId = -1;

            foreach (var kvp in COLOR_MAP)
            {
                Color mapColor = ColorTranslator.FromHtml(kvp.Key);
                double distance = CalculateColorDistance(inputRgbColor, mapColor);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestColorId = kvp.Value;
                }
            }

            return closestColorId;
        }
    }
}
