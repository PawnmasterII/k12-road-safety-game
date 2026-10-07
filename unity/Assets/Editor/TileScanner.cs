using UnityEngine;

namespace SafeStreets.EditorTools
{
    /// <summary>Finds green (grass) tiles in TilesetFloor by scanning pixels. One-shot diagnostic.</summary>
    public static class TileScanner
    {
        public static string Scan()
        {
            var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath("Assets/Tilesets/TilesetFloor.png");
            imp.isReadable = true;
            imp.SaveAndReimport();

            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/Tilesets/TilesetFloor.png");
            int cols = tex.width / 16, rows = tex.height / 16;
            var green = new System.Collections.Generic.List<string>();
            for (int r0 = 0; r0 < rows; r0++)
                for (int c0 = 0; c0 < cols; c0++)
                {
                    var px = tex.GetPixels(c0 * 16, r0 * 16, 16, 16);
                    int g = 0, op = 0;
                    foreach (var p in px)
                    {
                        if (p.a > 0.5f)
                        {
                            op++;
                            if (p.g > p.r + 0.05f && p.g > p.b + 0.05f && p.g > 0.3f) g++;
                        }
                    }
                    if (op > 200 && g > op * 0.55f)
                        green.Add($"c{c0}_r{r0}_i{(rows - 1 - r0) * cols + c0} g={g}/{op}");
                }
            imp.isReadable = false;
            imp.SaveAndReimport();
            return green.Count + " green tiles: " + string.Join(" ", green.ToArray());
        }
    }
}
