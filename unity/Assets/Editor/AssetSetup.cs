using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SafeStreets.EditorTools
{
    /// <summary>
    /// One-time asset setup following Game Code Library's Top Down Unity 2D series:
    /// PPU=16, Point filter, no compression, grid slicing for all sheets.
    /// Run via: unity command eval 'return SafeStreets.EditorTools.AssetSetup.Run();'
    /// </summary>
    public static class AssetSetup
    {
        private static readonly List<string> Report = new List<string>();

        public static string Run()
        {
            Report.Clear();

            // Walk sheets sliced as 4x4 grids (16 frames total). The tutorial slices
            // Walk.png grid 16x16 and uses frames COLUMN-major:
            //   down = 0,4,8,12 · left = 2,6,10,14 · right = 3,7,11,15 · up = 1,5,9,13
            SetupSheet("Assets/Tilesets/boy_walk.png", 4, 4);
            SetupSheet("Assets/Tilesets/boy_idle.png", 4, 1);
            SetupSheet("Assets/Tilesets/frog_walk.png", 4, 4);
            SetupSheet("Assets/Tilesets/frog_idle.png", 4, 1);

            // Tilesets sliced into 16x16 tiles for the Tilemap palette
            SetupTileset("Assets/Tilesets/TilesetFloor.png");
            SetupTileset("Assets/Tilesets/TilesetHouse.png");

            // Shadows + misc: single sprites
            SetupSingle("Assets/Tilesets/shadow.png");

            Report.Add("done");
            return string.Join("|", Report);
        }

        private static void Base(TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Multiple;
            imp.filterMode = FilterMode.Point;              // crisp pixels (tutorial)
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.spritePixelsPerUnit = 16;                   // 16px sprites => PPU 16 (tutorial)
            imp.isReadable = false;
        }

        private static void SetupSheet(string path, int cols, int rows)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return;
            Base(imp);
            var meta = new List<SpriteMetaData>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    meta.Add(new SpriteMetaData
                    {
                        name = $"{System.IO.Path.GetFileNameWithoutExtension(path)}_{r * cols + c}",
                        rect = new Rect(c * 16, r * 16, 16, 16),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    });
            imp.spritesheet = meta.ToArray();
            imp.SaveAndReimport();
        }

        private static void SetupTileset(string path)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return;
            Base(imp);
            imp.GetSourceTextureWidthAndHeight(out int srcW, out int srcH);
            int cols = srcW / 16, rows = srcH / 16;            var meta = new List<SpriteMetaData>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    meta.Add(new SpriteMetaData
                    {
                        name = $"{System.IO.Path.GetFileNameWithoutExtension(path)}_{r * cols + c}",
                        rect = new Rect(c * 16, r * 16, 16, 16),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    });
            imp.spritesheet = meta.ToArray();
            imp.SaveAndReimport();
            Report.Add($"{System.IO.Path.GetFileName(path)}: {cols}x{rows} tiles");
        }

        private static void SetupSingle(string path)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return;
            Base(imp);
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.SaveAndReimport();
        }
    }
}
