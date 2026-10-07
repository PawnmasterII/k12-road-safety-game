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
            EnsureFolders();
            var spriteDir = "Assets/Sprites";
            if (!AssetDatabase.IsValidFolder(spriteDir))
                AssetDatabase.CreateFolder("Assets", "Sprites");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("SafeStreets");

            // ---- Interaction system ----
            var imGo = new GameObject("InteractionManager");
            imGo.AddComponent<SafeStreets.InteractionManager>();

            // ---- Grass base ----
            var grass = NewSprite(root.transform, "Grass", "grass", Palette["Grass"], new Vector3(0, 0, 50), new Vector2(36, 30));
            grass.GetComponent<SpriteRenderer>().sortingOrder = -10;

            // ---- Road ----
            NewSprite(root.transform, "Road", "road", Palette["Road"], new Vector3(0, 0, 0), new Vector2(36, 6));

            // ---- Center dashes (skip the crossing zone) ----
            for (float x = -16f; x <= 16f; x += 2.4f)
            {
                if (x > -5f && x < 5f) continue;
                NewSprite(root.transform, $"Dash_{x:0}", "stripe", Palette["Stripe"], new Vector3(x, 0, -1), new Vector2(1.4f, 0.18f));
            }

            // ---- Sidewalks ----
            NewSprite(root.transform, "Sidewalk_N", "stripe", Palette["Sidewalk"], new Vector3(0, 3.7f, 1), new Vector2(36, 1.4f));
            NewSprite(root.transform, "Sidewalk_S", "stripe", Palette["Sidewalk"], new Vector3(0, -3.7f, 1), new Vector2(36, 1.4f));

            // ---- Zebra crossing ----
            for (int i = 0; i < 7; i++)
            {
                var z = NewSprite(root.transform, $"Zebra_{i}", "zebra", Palette["Zebra"],
                    new Vector3(-3.6f + i * 1.2f, 0f, -2), new Vector2(0.7f, 5.6f));
                z.GetComponent<SpriteRenderer>().sortingOrder = -4;
            }

            // ---- Labeled buildings (all interactable) ----
            MakeBuilding(root.transform, "SCHOOL", new Vector3(-11, 8.5f), new Vector2(9, 5), "school", Palette["School"], "your school");
            MakeBuilding(root.transform, "HOME", new Vector3(11, -8.5f), new Vector2(8, 5), "home", Palette["Home"], "your home");
            MakeBuilding(root.transform, "PARK", new Vector3(12, 8.5f), new Vector2(9, 5), "park", Palette["Park"], "the park");
            MakeBuilding(root.transform, "SHOP", new Vector3(-11, -8.5f), new Vector2(8, 5), "shop", Palette["Shop"], "the corner shop");

            // ---- Kid player at the bottom sidewalk (spawn reset every build) ----
            var player = NewSprite(root.transform, "Player", "kid", Palette["Kid"], new Vector3(0, -3.7f, -10), new Vector2(1.0f, 1.0f));
            player.tag = "Player";
            var rb = player.AddComponent<Rigidbody2D>();
            var col = player.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.8f, 0.9f);
            player.AddComponent<SafeStreets.PlayerController2D>();

            // ---- Camera ----
            var camGo = new GameObject("MainCamera2D");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
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

        /// <summary>Zone-glow sprite uses the same persisted-asset pipeline (white, tinted at runtime).</summary>
        internal static Sprite GetOrCreateSpriteForZone(string key)
        {
            return GetOrCreateSprite(key, Color.white);
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

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }
    }
}
