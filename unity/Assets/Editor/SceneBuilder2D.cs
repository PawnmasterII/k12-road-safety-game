using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SafeStreets.EditorTools
{
    /// <summary>
    /// Scene construction following Game Code Library's Top Down Unity 2D #3-#4:
    /// real Tilemaps (Ground + Collision layers), sorting layers, sprite player.
    /// Invoke via: unity command eval 'return SafeStreets.EditorTools.SceneBuilder2D.Build();'
    /// </summary>
    public static class SceneBuilder2D
    {
        // ---- Sorting layers (tutorial #3) ----
        private static readonly string[] Layers = { "Ground", "WalkInFront", "Collision", "Player", "WalkBehind", "Decor" };

        private static TileBase Tile(string assetName, int index)
        {
            var path = $"Assets/Tilesets/{assetName}";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{path}#{index}");
            if (sprite == null) return null;
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            return tile;
        }

        private static TileBase FindTile(string assetName, string spriteName)
        {
            var path = $"Assets/Tilesets/{assetName}";
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
                if (a is Sprite s && s.name == spriteName)
                {
                    var tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = s;
                    return tile;
                }
            return null;
        }

        public static string Build()
        {
            // ---- Sorting layers (idempotent) ----
            var tagManager = new UnityEditor.SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layersProp = tagManager.FindProperty("layers");
            var existing = new HashSet<string>();
            for (int i = 0; i < 32; i++)
                if (layersProp.GetArrayElementAtIndex(i).stringValue is string s && !string.IsNullOrEmpty(s))
                    existing.Add(s);
            foreach (var layer in Layers)
            {
                if (existing.Contains(layer)) continue;
                for (int i = 8; i < 32; i++)
                {
                    var el = layersProp.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(el.stringValue))
                    {
                        el.stringValue = layer;
                        break;
                    }
                }
            }
            tagManager.ApplyModifiedProperties();

            // ---- "Player" tag exists by default; ensure it does ----
            var tagManager2 = new UnityEditor.SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager2.FindProperty("tags");
            bool hasPlayer = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == "Player") hasPlayer = true;
            if (!hasPlayer)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "Player";
            }
            tagManager2.ApplyModifiedProperties();

            // ---- Scene ----
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("SafeStreets");

            // ---- Grid + Tilemaps (tutorial #3) ----
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(root.transform);
            var grid = gridGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1, 1, 0);

            var groundGo = new GameObject("Ground");
            groundGo.transform.SetParent(gridGo.transform);
            var ground = groundGo.AddComponent<Tilemap>();
            var groundR = groundGo.AddComponent<TilemapRenderer>();
            groundR.sortingLayerName = "Ground";

            var decorGo = new GameObject("Decor");
            decorGo.transform.SetParent(gridGo.transform);
            var decor = decorGo.AddComponent<Tilemap>();
            var decorR = decorGo.AddComponent<TilemapRenderer>();
            decorR.sortingLayerName = "WalkInFront";

            var collisionGo = new GameObject("Collision");
            collisionGo.transform.SetParent(gridGo.transform);
            var collision = collisionGo.AddComponent<Tilemap>();
            var collisionR = collisionGo.AddComponent<TilemapRenderer>();
            collisionR.sortingLayerName = "Collision";
            var collisionTc = collisionGo.AddComponent<TilemapCollider2D>();

            // ---- Fill Ground with Floor tiles — VERIFIED cell indices (pixel-scanned) ----
            // Grass: i277 (c13_r13, 256/256 green), i268, i265 · Dirt path band: i319-i328 (row 11)
            // White floor: i396-cells (row 7) used for zebra stripes
            TileBase grass = FindTile("TilesetFloor.png", "TilesetFloor_277") ?? Tile("TilesetFloor.png", 277);
            TileBase grass2 = FindTile("TilesetFloor.png", "TilesetFloor_268") ?? Tile("TilesetFloor.png", 268);
            TileBase flower = FindTile("TilesetFloor.png", "TilesetFloor_265") ?? Tile("TilesetFloor.png", 265);
            TileBase path = FindTile("TilesetFloor.png", "TilesetFloor_323") ?? Tile("TilesetFloor.png", 323);
            TileBase white = FindTile("TilesetFloor.png", "TilesetFloor_399") ?? Tile("TilesetFloor.png", 399);

            int gw = 40, gh = 26;
            for (int ty = -gh / 2; ty < gh / 2; ty++)
            {
                for (int tx = -gw / 2; tx < gw / 2; tx++)
                {
                    int h = (tx * 73856093) ^ (ty * 19349663);
                    TileBase t = ((h & 15) == 0) ? flower : (((h >> 4) & 7) == 0 ? grass2 : grass);
                    if (t != null) ground.SetTile(new Vector3Int(tx, ty, 0), t);
                }
            }

            // ---- Road: path tiles across the middle, 6 tall ----
            for (int ty = -3; ty < 3; ty++)
                for (int tx = -gw / 2; tx < gw / 2; tx++)
                    if (path != null) ground.SetTile(new Vector3Int(tx, ty, 0), path);

            // ---- Zebra stripes: verified white floor tiles across the path ----
            if (white != null)
                for (int i = 0; i < 7; i++)
                    for (int ty = -2; ty <= 2; ty++)
                        decor.SetTile(new Vector3Int(-4 + i, ty, 0), white);

            // ---- Player (tutorial #1/#2): sprite from sliced sheet, collider, animator ----
            var player = new GameObject("Player");
            player.transform.SetParent(root.transform);
            player.transform.position = new Vector3(0f, -3.7f, 0f);
            player.tag = "Player";
            var sr = player.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Player";
            var boyIdle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tilesets/boy_idle.png#0");
            sr.sprite = boyIdle != null ? boyIdle : AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tilesets/boy_walk.png#0");

            var rb = player.AddComponent<Rigidbody2D>();
            var col = player.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.5f, 0.4f);
            col.offset = new Vector2(0f, -0.2f);
            player.AddComponent<SafeStreets.PlayerController2D>();

            var animator = player.AddComponent<SafeStreets.PlayerAnimator>();
            var walkFrames = new Sprite[16];
            var walkAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Tilesets/boy_walk.png");
            foreach (var a in walkAssets)
                if (a is Sprite s && s.name.StartsWith("boy_walk_"))
                    if (int.TryParse(s.name.Substring("boy_walk_".Length), out int idx) && idx < 16)
                        walkFrames[idx] = s;
            animator.frames = walkFrames;

            // ---- Shadow ----
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(player.transform);
            shadow.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            var ssr = shadow.AddComponent<SpriteRenderer>();
            ssr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tilesets/shadow.png");
            ssr.sortingLayerName = "WalkInFront";
            ssr.color = new Color(0f, 0f, 0f, 0.45f);

            // ---- Buildings (Collision layer boxes, interactable) ----
            MakeBuilding(root.transform, "SCHOOL", new Vector2(-11, 8.5f), new Vector2(9, 5), "your school");
            MakeBuilding(root.transform, "HOME", new Vector2(11, -8.5f), new Vector2(8, 5), "your home");
            MakeBuilding(root.transform, "PARK", new Vector2(12, 8.5f), new Vector2(9, 5), "the park");
            MakeBuilding(root.transform, "SHOP", new Vector2(-11, -8.5f), new Vector2(8, 5), "the corner shop");

            // ---- InteractionManager ----
            var imGo = new GameObject("InteractionManager");
            imGo.AddComponent<SafeStreets.InteractionManager>();

            // ---- Camera (tutorial #4 style follow) ----
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;    // ~11 tiles tall at 1080p: nice close pixel view
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.20f, 0.28f, 0.20f);
            camGo.AddComponent<SafeStreets.CameraFollow2D>().target = player.transform;
            camGo.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);

            // ---- Title ----
            var titleGo = new GameObject("ZoneTitle");
            var tmp = titleGo.AddComponent<TextMesh>();
            tmp.text = "SAFE STREET - walk up to a building and press SPACE!";
            tmp.characterSize = 0.22f;
            tmp.fontSize = 36;
            tmp.anchor = TextAnchor.MiddleCenter;
            tmp.color = Color.white;
            titleGo.transform.SetParent(root.transform);
            titleGo.transform.position = new Vector3(0, 2.2f, 0f);
            titleGo.GetComponent<MeshRenderer>().sortingLayerName = "Decor";

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/SafeStreets.unity");
            return $"scene=SafeStreets roots={scene.rootCount} tilesGround={(ground.size.x * ground.size.y)}";
        }

        private static void MakeBuilding(Transform parent, string label, Vector2 pos, Vector2 size, string interactLabel)
        {
            var go = new GameObject("Building_" + label);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UnityEngine.Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 16f);
            sr.color = new Color(0.45f, 0.36f, 0.30f);   // muted wood-brown placeholder
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.sortingLayerName = "Collision";

            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            go.AddComponent<SafeStreets.Interactable>().label = interactLabel;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = Vector3.zero;
            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = label;
            tm.characterSize = 0.18f;
            tm.fontSize = 32;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 1f, 1f, 0.95f);
            labelGo.GetComponent<MeshRenderer>().sortingLayerName = "Decor";
        }
    }
}
