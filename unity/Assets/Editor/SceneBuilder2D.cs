using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SafeStreets.EditorTools
{
    /// <summary>
    /// Editor-driven construction of the 2D SafeStreets scene (top-down).
    /// v3: persisted sprite assets (fixes invisible character), quiz zones, QuizUI.
    /// Invoke via: unity command eval 'return SafeStreets.EditorTools.SceneBuilder2D.Build();'
    /// </summary>
    public static class SceneBuilder2D
    {
        private static readonly Dictionary<string, Color> Palette = new Dictionary<string, Color>
        {
            ["Grass"] = new Color(0.42f, 0.68f, 0.38f),
            ["Road"] = new Color(0.30f, 0.31f, 0.33f),
            ["Sidewalk"] = new Color(0.78f, 0.78f, 0.74f),
            ["Stripe"] = new Color(0.95f, 0.95f, 0.90f),
            ["Zebra"] = Color.white,
            ["Kid"] = new Color(0.95f, 0.55f, 0.20f),
            ["School"] = new Color(0.75f, 0.35f, 0.30f),
            ["Home"] = new Color(0.90f, 0.75f, 0.45f),
            ["Park"] = new Color(0.36f, 0.60f, 0.44f),
            ["Shop"] = new Color(0.55f, 0.60f, 0.72f),
        };

        public static string Build()
        {
            EnsureFolder("Assets/Scenes");
            var spriteDir = "Assets/Sprites";
            if (!AssetDatabase.IsValidFolder(spriteDir))
                AssetDatabase.CreateFolder("Assets", "Sprites");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("SafeStreets");

            // ---- Interaction system ----
            var imGo = new GameObject("InteractionManager");
            imGo.AddComponent<SafeStreets.InteractionManager>();

            // ---- World tiles from the pack's Field tileset (16x16, world tile = 1 unit) ----
            // TilesetField.png 80x240 = 5 cols x 15 rows. Known-good cells (col,row) —
            // grass patches, dirt path, flowers; verified by cropping before wiring.
            var grass = GetOrCreateTile("tileset_field.png", "t_grass", 2, 10);
            var grass2 = GetOrCreateTile("tileset_field.png", "t_grass2", 3, 10);
            var flower = GetOrCreateTile("tileset_field.png", "t_flower", 1, 13);
            var dirt = GetOrCreateTile("tileset_field.png", "t_dirt", 0, 10);

            // Ground: 40 x 28 tiles of grass (1 unit per tile), deterministic variation
            int gw = 40, gh = 28;
            var groundRoot = new GameObject("GroundTiles");
            groundRoot.transform.SetParent(root.transform);
            for (int ty = -gh / 2; ty < gh / 2; ty++)
            {
                for (int tx = -gw / 2; tx < gw / 2; tx++)
                {
                    // pseudo-random but deterministic variation
                    int h = (tx * 73856093) ^ (ty * 19349663);
                    Sprite s = ((h & 15) == 0) ? flower : (((h >> 4) & 7) == 0 ? grass2 : grass);
                    var t = new GameObject($"g{tx}_{ty}");
                    t.transform.SetParent(groundRoot.transform);
                    t.transform.position = new Vector3(tx + 0.5f, ty + 0.5f, 50f);
                    var sr = t.AddComponent<SpriteRenderer>();
                    sr.sprite = s;
                    sr.sortingOrder = -10;
                }
            }

            // ---- Road: dirt path strip, 6 tiles tall, through the middle ----
            var roadRoot = new GameObject("RoadTiles");
            roadRoot.transform.SetParent(root.transform);
            for (int ty = -3; ty < 3; ty++)
            {
                for (int tx = -gw / 2; tx < gw / 2; tx++)
                {
                    var t = new GameObject($"r{tx}_{ty}");
                    t.transform.SetParent(roadRoot.transform);
                    t.transform.position = new Vector3(tx + 0.5f, ty + 0.5f, -5f);
                    var sr = t.AddComponent<SpriteRenderer>();
                    sr.sprite = dirt;
                    sr.sortingOrder = -8;
                }
            }

            // ---- Zebra crossing: white paint stripes ON the road (thin boxes over dirt) ----
            var stripe = GetOrCreateTile("tileset_field.png", "t_stripe", 2, 10); // reuse grass white? no — use plain white
            for (int i = 0; i < 7; i++)
            {
                var z = new GameObject($"Zebra_{i}");
                z.transform.SetParent(root.transform);
                z.transform.position = new Vector3(-3.6f + i * 1.2f, 0f, -2f);
                var sr = z.AddComponent<SpriteRenderer>();
                sr.sprite = UnityEngine.Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 1f);
                sr.color = new Color(0.95f, 0.95f, 0.92f);
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = new Vector2(0.7f, 5.6f);
                sr.sortingOrder = -4;
            }

            // ---- Sidewalks: lighter dirt border rows ----
            for (int tx = -gw / 2; tx < gw / 2; tx++)
            {
                foreach (var yy in new[] { 3.7f, -3.7f })
                {
                    var t = new GameObject($"sw{tx}_{yy:0.0}");
                    t.transform.SetParent(root.transform);
                    t.transform.position = new Vector3(tx + 0.5f, yy, -3f);
                    var sr = t.AddComponent<SpriteRenderer>();
                    sr.sprite = grass2;
                    sr.sortingOrder = -6;
                }
            }

            // ---- Labeled buildings (all interactable) ----
            MakeBuilding(root.transform, "SCHOOL", new Vector3(-11, 8.5f), new Vector2(9, 5), "school", Palette["School"], "your school");
            MakeBuilding(root.transform, "HOME", new Vector3(11, -8.5f), new Vector2(8, 5), "home", Palette["Home"], "your home");
            MakeBuilding(root.transform, "PARK", new Vector3(12, 8.5f), new Vector2(9, 5), "park", Palette["Park"], "the park");
            MakeBuilding(root.transform, "SHOP", new Vector3(-11, -8.5f), new Vector2(8, 5), "shop", Palette["Shop"], "the corner shop");

            // ---- Kid player: Ninja Adventure "Boy" walk sheet (4x4: down/left/right/up) ----
            const string SheetPath = "Assets/Sprites/Ninja/boy_walk.png";
            const string ShadowPath = "Assets/Sprites/Ninja/boy_shadow.png";

            var player = new GameObject("Player");
            player.transform.SetParent(root.transform);
            player.transform.position = new Vector3(0, -3.7f, -10f);
            player.tag = "Player";
            var psr = player.AddComponent<SpriteRenderer>();
            psr.sprite = GetOrCreateSprite("kid", Palette["Kid"]);   // fallback until sheet sliced
            psr.sortingOrder = 10;                                    // in front of world
            var psc = psr; // alias for readability below

            var sheetImporter = (TextureImporter)AssetImporter.GetAtPath(SheetPath);
            if (sheetImporter != null && sheetImporter.spriteImportMode != SpriteImportMode.Multiple)
            {
                SetupWalkSheet(sheetImporter);
            }

            var rb = player.AddComponent<Rigidbody2D>();
            var col = player.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.55f, 0.45f);   // 16px sprite: smaller feet-area collider
            col.offset = new Vector2(0f, -0.15f);
            var controller = player.AddComponent<SafeStreets.PlayerController2D>();

            var animator = player.AddComponent<SafeStreets.PlayerAnimator>();
            // SeparateAnim/Walk layout: row0=down, row1=left, row2=right, row3=up
            animator.walkDown = LoadRow(SheetPath, "down");
            animator.walkUp = LoadRow(SheetPath, "up");
            animator.walkLeft = LoadRow(SheetPath, "left");
            animator.walkRight = LoadRow(SheetPath, "right");

            // drop shadow (the pack's own shadow sprite) — sells the 2.5D look
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(player.transform);
            shadow.transform.localPosition = new Vector3(0f, -0.28f, 1f);
            var ssr = shadow.AddComponent<SpriteRenderer>();
            var shadowTex = AssetDatabase.LoadAssetAtPath<Sprite>(ShadowPath);
            if (shadowTex != null) ssr.sprite = shadowTex;
            ssr.sortingOrder = 9;
            ssr.color = new Color(0f, 0f, 0f, 0.4f);

            // ---- Camera ----
            var camGo = new GameObject("MainCamera2D");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 4.5f;   // tighter view: 16px sprites, was 8 for big tiles
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.30f, 0.42f, 0.30f);
            camGo.AddComponent<SafeStreets.CameraFollow2D>().target = player.transform;
            camGo.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);

            // ---- Quiz zones REMOVED (replaced by Interactable + InteractionManager) ----

            // ---- Title ----
            var titleGo = new GameObject("ZoneTitle");
            var tmp = titleGo.AddComponent<TextMesh>();
            tmp.text = "SAFE STREET - walk up to a building and press SPACE!";
            tmp.characterSize = 0.28f;
            tmp.fontSize = 40;
            tmp.anchor = TextAnchor.MiddleCenter;
            tmp.color = Color.white;
            titleGo.transform.SetParent(root.transform);
            titleGo.transform.position = new Vector3(0, 2.2f, -6);

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/SafeStreets.unity");

            var report = new StringBuilder();
            report.Append("scene=SafeStreets v3 roots=").Append(scene.rootCount);
            return report.ToString();
        }

        /// <summary>Persisted sprite asset per key — survives save/reload (fixes invisibility).</summary>
        private static GameObject NewSprite(Transform parent, string name, string spriteKey, Color color,
            Vector3 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateSprite(spriteKey, color);
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            return go;
        }

        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        private static Sprite GetOrCreateSprite(string key, Color color)
        {
            if (_spriteCache.TryGetValue(key, out var cached) && cached != null) return cached;

            string path = $"Assets/Sprites/{key}.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                var px = new Color[8 * 8];
                for (int i = 0; i < px.Length; i++) px[i] = color;
                tex.SetPixels(px);
                tex.Apply();
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteMeshType != SpriteMeshType.FullRect)
            {
                // Sliced draw mode requires FullRect; enforce on every build (fixes Tight default).
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            _spriteCache[key] = sprite;
            return sprite;
        }

        /// <summary>Slice a 16x16 walk sheet (7 rows x 4 cols) into per-row sprite arrays.</summary>
        private static void SetupWalkSheet(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var meta = new System.Collections.Generic.List<SpriteMetaData>();
            string[] names = { "down", "up", "left", "right", "rundown", "runup", "runleft" };
            for (int row = 0; row < 7; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    meta.Add(new SpriteMetaData
                    {
                        name = $"{names[row]}{col}",
                        rect = new Rect(col * 16, row * 16, 16, 16),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    });
                }
            }
            importer.spritesheet = meta.ToArray();
            importer.SaveAndReimport();
        }

        /// <summary>Load the 4 frames of one walk row from the sliced sheet.</summary>
        private static Sprite[] LoadRow(string sheetPath, string rowName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
            var row = new Sprite[4];
            foreach (var a in assets)
            {
                if (a is Sprite s && s.name.StartsWith(rowName))
                {
                    if (int.TryParse(s.name.Substring(rowName.Length), out int idx) && idx >= 0 && idx < 4)
                        row[idx] = s;
                }
            }
            return row;
        }

        private static GameObject MakeBuilding(Transform parent, string label, Vector2 pos, Vector2 size, string spriteKey, Color color, string interactLabel)
        {
            var go = NewSprite(parent, "Building_" + label, spriteKey, color, new Vector3(pos.x, pos.y, 1), size);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            go.AddComponent<SafeStreets.Interactable>().label = interactLabel;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = Vector3.zero;
            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = label;
            tm.characterSize = 0.22f;
            tm.fontSize = 36;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 1f, 1f, 0.92f);
            return go;
        }

            // ---- (quiz zone builder removed — interaction system replaced it) ----

        /// <summary>
        /// Persisted world tile: crop a 16x16 region from a tileset sheet into its own
        /// sprite asset (Assets/Sprites/Tiles/&lt;name&gt;.png). Cached per name.
        /// </summary>
        private static Sprite GetOrCreateTile(string tilesetFile, string name, int col, int row)
        {
            string path = $"Assets/Sprites/Tiles/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            var sheetPath = $"Assets/Sprites/Ninja/{tilesetFile}";
            var sheet = AssetDatabase.LoadAssetAtPath<Sprite>(sheetPath);
            var sheetImp = (TextureImporter)AssetImporter.GetAtPath(sheetPath);
            if (sheet == null || sheetImp == null || !sheetImp.isReadable)
            {
                // import (or re-import) tileset as readable single sprite
                if (sheetImp != null)
                {
                    sheetImp.textureType = TextureImporterType.Sprite;
                    sheetImp.spriteImportMode = SpriteImportMode.Single;
                    sheetImp.filterMode = FilterMode.Point;
                    sheetImp.mipmapEnabled = false;
                    sheetImp.textureCompression = TextureImporterCompression.Uncompressed;
                    sheetImp.isReadable = true;   // needed for GetPixels when cropping tiles
                    sheetImp.SaveAndReimport();
                    sheet = AssetDatabase.LoadAssetAtPath<Sprite>(sheetPath);
                }
            }
            if (sheet == null) return null;

            var tex = sheet.texture;
            var tile = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tile.filterMode = FilterMode.Point;
            var srcY = tex.height - (row + 1) * 16;   // texture coords are bottom-up
            tile.SetPixels(tex.GetPixels(col * 16, srcY, 16, 16));
            tile.Apply();

            EnsureFolder("Assets/Sprites/Tiles");
            System.IO.File.WriteAllBytes(path, tile.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var imp2 = (TextureImporter)AssetImporter.GetAtPath(path);
            imp2.textureType = TextureImporterType.Sprite;
            imp2.spriteImportMode = SpriteImportMode.Single;
            imp2.filterMode = FilterMode.Point;
            imp2.mipmapEnabled = false;
            imp2.textureCompression = TextureImporterCompression.Uncompressed;
            var st = new TextureImporterSettings();
            imp2.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            imp2.SetTextureSettings(st);
            imp2.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = path.Substring(0, path.LastIndexOf('/'));
                var leaf = path.Substring(path.LastIndexOf('/') + 1);
                AssetDatabase.CreateFolder(parent, leaf);
            }
        }
    }
}
