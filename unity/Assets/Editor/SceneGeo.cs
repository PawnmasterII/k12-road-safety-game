using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SafeStreets.EditorTools
{
    /// <summary>
    /// GEOMETRIC BUILD — systems-first milestone. No textures, no sprite sheets.
    /// Everything is Unity primitives: player = circle, NPCs = squares, buildings = boxes.
    /// Goal: prove the FRAMEWORK (movement, camera, dialog, interaction, NPC, quest state)
    /// with zero art-pipeline noise. Art comes back once this is stable.
    ///
    /// Invoke: unity command eval 'return SafeStreets.EditorTools.SceneGeo.Build();'
    /// </summary>
    public static class SceneGeo
    {
        // ---- Palette (pure color slots, easy to re-skin later) ----
        private static readonly Color GrassBg = new Color(0.30f, 0.45f, 0.28f);
        private static readonly Color RoadCol = new Color(0.25f, 0.26f, 0.28f);
        private static readonly Color PlayerCol = new Color(0.95f, 0.55f, 0.15f);   // orange
        private static readonly Color NpcAda = new Color(0.30f, 0.55f, 0.95f);      // blue
        private static readonly Color NpcRose = new Color(0.85f, 0.40f, 0.65f);     // pink
        private static readonly Color BuildingCol = new Color(0.45f, 0.45f, 0.48f); // gray
        private static readonly Color ZebraCol = new Color(0.92f, 0.92f, 0.88f);

        public static string Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("SafeStreets");

            // ---- Systems ----
            new GameObject("InteractionManager").AddComponent<SafeStreets.InteractionManager>();
            new GameObject("DialogManager").AddComponent<SafeStreets.DialogManager>();
            new GameObject("QuestManager").AddComponent<SafeStreets.QuestManager>();

            // ---- Ground plane (one big quad, not tiles) ----
            var ground = Shape(root, "Ground", GrassBg, new Vector3(0, 0, 50), new Vector2(40, 26));
            ground.GetComponent<SpriteRenderer>().sortingOrder = -10;

            // ---- Road strip ----
            var road = Shape(root, "Road", RoadCol, new Vector3(0, 0, -1), new Vector2(40, 6));
            road.GetComponent<SpriteRenderer>().sortingOrder = -8;

            // ---- Zebra stripes ----
            for (int i = 0; i < 7; i++)
            {
                var z = Shape(root, $"Zebra{i}", ZebraCol, new Vector3(-3.6f + i * 1.2f, 0f, -2), new Vector2(0.7f, 5.2f));
                z.GetComponent<SpriteRenderer>().sortingOrder = -4;
            }

            // ---- Buildings: gray boxes with labels (interactable) ----
            MakeBuilding(root, "SCHOOL", new Vector2(-11, 8), new Vector2(9, 4), "the school");
            MakeBuilding(root, "HOME", new Vector2(11, -8), new Vector2(8, 4), "your home");
            MakeBuilding(root, "PARK", new Vector2(12, 8), new Vector2(9, 4), "the park");
            MakeBuilding(root, "SHOP", new Vector2(-11, -8), new Vector2(8, 4), "the corner shop");

            // ---- Conversations ----
            var ada = Conv("Officer Ada",
                "Hi! I'm Officer Ada. This street needs your help!",
                "Three friends need to cross the road. Talk to them and help them cross safely.",
                "Press SPACE to talk to anyone with a speech marker above their head.");
            var rose = Conv("Granny Rose",
                "Oh dear, I want to visit the shop, but the road scares me.",
                "Will you help me cross, sweetheart?");
            var kid1 = Conv("Little Sam",
                "I wanna get to the PARK but the road is so big!",
                "Will you help me cross?");

            var quest = ScriptableObject.CreateInstance<SafeStreets.QuestData>();
            quest.questName = "Help three friends cross";
            quest.requiredCrossings = 3;
            AssetDatabase.CreateAsset(ada, "Assets/Conv_Ada.asset");
            AssetDatabase.CreateAsset(rose, "Assets/Conv_Rose.asset");
            AssetDatabase.CreateAsset(kid1, "Assets/Conv_Sam.asset");
            AssetDatabase.CreateAsset(quest, "Assets/Quest_Crossings.asset");

            // ---- NPCs: colored squares with name labels ----
            MakeNpc(root, "Officer Ada", NpcAda, new Vector3(2.5f, 4f, 0f), ada);
            MakeNpc(root, "Granny Rose", NpcRose, new Vector3(-8f, -3.6f, 0f), rose, wantsCrossing: true);
            MakeNpc(root, "Little Sam", new Color(0.55f, 0.80f, 0.40f), new Vector3(8f, -3.6f, 0f), kid1, wantsCrossing: true);
            // third friend so the 3-crossing quest is actually winnable
            var mia = Conv("Mia",
                "The LIBRARY is my favorite place. But I can't cross alone...",
                "Will you walk me over the stripes?");
            AssetDatabase.CreateAsset(mia, "Assets/Conv_Mia.asset");
            MakeNpc(root, "Mia", new Color(0.90f, 0.62f, 0.35f), new Vector3(-2.5f, 3.2f, 0f), mia, wantsCrossing: true);

            // ---- Player: orange circle ----
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.SetParent(root.transform);
            player.transform.position = new Vector3(0f, -3.6f, 0f);
            var psr = player.AddComponent<SpriteRenderer>();
            psr.sprite = CircleSprite();
            psr.color = PlayerCol;
            psr.sortingLayerName = "Player";
            psr.sortingOrder = 10;
            player.AddComponent<CircleCollider2D>().radius = 0.4f;
            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            player.AddComponent<SafeStreets.PlayerController2D>();
            SetWorldSize(psr, new Vector2(0.8f, 0.8f));   // player body: 0.8 world units (visual)

            // ---- Camera ----
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = GrassBg;
            camGo.AddComponent<SafeStreets.CameraFollow2D>().target = player.transform;
            camGo.transform.position = new Vector3(0, -3.6f, -10f);

            // ---- HUD: quest counter ----
            var hudGo = new GameObject("QuestHUD");
            var hud = hudGo.AddComponent<TextMesh>();
            SafeStreets.DialogStyle.Style(hud, 0.14f, 32, TextAnchor.UpperLeft, Color.white, 60);
            hud.text = "Helped: 0 / 3";
            hudGo.transform.SetParent(root.transform);
            hudGo.transform.position = new Vector3(0f, 0f, 0f);
            hudGo.AddComponent<SafeStreets.HudAnchor>();
            var qm = SafeStreets.QuestManager.Find();
            qm.RegisterHud(hud, quest);
            SafeStreets.WinBanner.Create(root.transform, hud);   // self-subscribes at play start

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/SafeStreets.unity");
            return $"geo scene built, roots={scene.rootCount}";
        }

        // ---- THE uniform unit system: 10 texture pixels = 1 world unit, everywhere. ----
        // Every generated sprite is authored at this density, so natural size == world size.
        // Hitboxes are specified in world units and therefore always match visuals.
        // To swap in real art later: author it at 10 px per world unit (or scale the
        // GameObject by desiredWorldUnits / spriteNaturalUnits) — colliders don't move.
        public const float PX_PER_UNIT = 10f;

        /// <summary>
        /// THE uniform unit guarantee: whatever sprite is assigned, scale the transform so
        /// the rendered size is exactly `worldSize` world units. Independent of texture
        /// pixel dimensions AND import PPU. Hitboxes are declared in world units and never
        /// touched, so visual size == logical size by construction. Swap any texture in —
        /// the size on screen does not change.
        /// </summary>
        public static void SetWorldSize(SpriteRenderer sr, Vector2 worldSize)
        {
            var b = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(
                b.x > 0.0001f ? worldSize.x / b.x : 1f,
                b.y > 0.0001f ? worldSize.y / b.y : 1f,
                1f);
        }

        private static GameObject Shape(GameObject parent, string name, Color color, Vector3 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            int pxW = Mathf.Max(1, Mathf.RoundToInt(size.x * PX_PER_UNIT));
            int pxH = Mathf.Max(1, Mathf.RoundToInt(size.y * PX_PER_UNIT));
            var tex = new Texture2D(pxW, pxH, TextureFormat.RGBA32, false);
            var fill = new Color[pxW * pxH];
            for (int i = 0; i < fill.Length; i++) fill[i] = Color.white;
            tex.SetPixels(fill);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, pxW, pxH),
                new Vector2(0.5f, 0.5f), PX_PER_UNIT, 0, SpriteMeshType.FullRect);
            sr.color = color;
            return go;
        }

        private static Sprite CircleSprite()
        {
            // persisted so it survives scene reloads
            const string path = "Assets/circle.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var center = new Vector2(S / 2f, S / 2f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    tex.SetPixel(x, y, d <= S / 2f - 1f ? Color.white : Color.clear);
                }
            tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Point;
            imp.spritePixelsPerUnit = PX_PER_UNIT;   // 64px circle => 0.8 world units (uniform system)
            var st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(st);
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void MakeBuilding(GameObject parent, string label, Vector2 pos, Vector2 size, string interactLabel)
        {
            var go = Shape(parent, "Building_" + label, BuildingCol, new Vector3(pos.x, pos.y, 0f), size);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sortingLayerName = "Collision";
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            go.AddComponent<SafeStreets.Interactable>().label = interactLabel;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = Vector3.zero;
            var tm = labelGo.AddComponent<TextMesh>();
            SafeStreets.DialogStyle.Style(tm, 0.16f, 30, TextAnchor.MiddleCenter, Color.white, 0);
            tm.text = label;
        }

        private static void MakeNpc(GameObject parent, string name, Color color, Vector3 pos,
            SafeStreets.Conversation conv, bool wantsCrossing = false)
        {
            var go = new GameObject("NPC_" + name);
            go.transform.SetParent(parent.transform);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SafeStreets.EditorTools.SceneGeo.SquareSprite();
            sr.color = color;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 5;
            SetWorldSize(sr, new Vector2(1.2f, 1.2f));   // NPC body: 1.2 world units (visual)

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3.2f, 2.6f);   // talk zone (world units)

            if (wantsCrossing)
            {
                var c = go.AddComponent<SafeStreets.CrossingNPC>();
                c.npcName = name;
            }
            else
            {
                var npc = go.AddComponent<SafeStreets.DialogNPC>();
                npc.conversation = conv;
                npc.interactLabel = name;
            }

            var labelGo = new GameObject("Name");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            var tm = labelGo.AddComponent<TextMesh>();
            SafeStreets.DialogStyle.Style(tm, 0.11f, 26, TextAnchor.MiddleCenter, Color.white, 5);
            tm.text = name;
        }

        private static SafeStreets.Conversation Conv(string speaker, params string[] lines)
        {
            var c = ScriptableObject.CreateInstance<SafeStreets.Conversation>();
            c.speakerName = speaker;
            c.lines = lines;
            return c;
        }

        /// <summary>Persisted 1x1 white square (shared by NPCs).</summary>
        private static Sprite _square;

        public static Sprite SquareSprite()
        {
            if (_square != null) return _square;
            const string path = "Assets/square.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) { _square = existing; return existing; }

            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Point;
            imp.spritePixelsPerUnit = PX_PER_UNIT;   // 8px square => 0.8 world units (uniform system)
            var st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(st);
            imp.SaveAndReimport();
            _square = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (_square == null)
            {
                UnityEngine.Debug.LogError("SquareSprite: import failed at " + path);
            }
            else
            {
                var natural = _square.bounds.size;   // must be 0.8 x 0.8 world units
                if (Mathf.Abs(natural.x - 0.8f) > 0.01f)
                    UnityEngine.Debug.LogError($"SquareSprite: natural size {natural} != 0.8 — PPU wrong!");
            }
            return _square;
        }
    }
}
