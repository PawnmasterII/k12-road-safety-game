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
            new GameObject("QuizManager").AddComponent<SafeStreets.QuizManager>();
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

            // ---- Spec 006: crossing corridor authority (one logic object over the stripes;
            //      CrossingZone recomputes its corridor from the Zebra* sprites itself) ----
            var czGo = new GameObject("CrossingZone");
            czGo.transform.SetParent(root.transform);
            czGo.transform.position = new Vector3(0f, 0f, -3f);
            var czCol = czGo.AddComponent<BoxCollider2D>();
            czCol.isTrigger = true;
            czCol.size = new Vector2(6 * 1.2f + 0.7f, 5.2f);   // 7 stripes at 1.2 pitch, 0.7 wide → x span 7.9
            czGo.AddComponent<SafeStreets.CrossingZone>();

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

            // ---- Feature 002: quiz banks (prompts/citations from backend/content.py,
            //      K-12 Traffic Safety Education Resource Guide) — 3 MC + 2 TF each ----
            var bankLe = MakeBank("le-crossing", "Crossing with a Grown-Up",
                MC("Where do you stop before you cross the street?",
                    "At the curb", "In the middle of the road", "On the grass far away", "Anywhere I like",
                    "Guide, Preschool-Grade 1: stop at the curb; hold an adult's hand"),
                MC("Who helps you cross the road?",
                    "A grown-up", "Nobody", "My pet", "A stranger",
                    "Guide, Preschool-Grade 1: adult-led crossing"),
                MC("Your ball rolls into the street. What do you do?",
                    "Tell a grown-up", "Chase it quickly", "Wait for cars and grab it", "Throw my hat at it",
                    "Guide, Preschool-Grade 1: children do not chase a ball or retrieve an item"),
                TF("Before crossing, I stop, look, and listen.", true,
                    "Guide, Preschool-Grade 1: stop, look, listen, wait for an adult, cross together"),
                TF("A parking lot is safe to walk in only when a grown-up stays close.", true,
                    "Guide, Preschool-Grade 1: keep an adult close in parking lots; they are not play spaces"));
            var bankSh = MakeBank("sh-school-bike", "School Zone + Bike Gear",
                MC("Where should you cross the street near school?",
                    "At the crosswalk or crossing guard", "Anywhere it is shorter", "Between parked cars", "Around the corner",
                    "Guide, Grades 2-3: use a designated crossing; follow guard instructions"),
                MC("How should your bike helmet fit?",
                    "Level on your head with snug straps", "Tilted back to look cool", "Loose so it is comfy", "Backwards",
                    "Guide, Grades 4-5: helmet level, two finger-widths above eyebrows, straps form a V"),
                MC("You drop something near the school bus. What do you do?",
                    "Tell the bus driver", "Quickly grab it", "Ask a friend to get it", "Come back for it later",
                    "Guide, Grades 2-3: if an item falls near the bus, tell the driver instead of retrieving it"),
                TF("Wearing bright colors or reflectors helps drivers see me.", true,
                    "Guide, Grades 4-5: increase visibility in low light; visibility topic"),
                TF("When crossing, I only need to watch for fast cars.", false,
                    "Guide, Grades 2-3: look for turning vehicles, driveway traffic, backing vehicles"));
            var bankSt = MakeBank("st-riding", "Riding Ready: Bikes + Phones",
                MC("Florida law: who must wear a helmet when riding a bicycle?",
                    "Riders under 16", "Only racers", "Nobody", "Only adults",
                    "Guide, Florida legal checks: F.S. 316.2065 requires riders and passengers under 16 to wear a helmet"),
                MC("Your phone buzzes while you are riding. What do you do?",
                    "Stop in a safe place before checking", "Check it while riding", "Answer with one hand", "Have a friend read it while riding",
                    "Guide, Grades 6-8: stop in a safe place before checking navigation or messages"),
                MC("Where might a driver NOT see you, even in daylight?",
                    "In the driver's blind spot or beside parked cars (door zone)", "Only at night", "Only on highways", "Nowhere - drivers always see bikes",
                    "Guide, Grades 6-8: door zone, large-vehicle blind spots, 'I can see, I can be seen'"),
                TF("An e-bike needs more distance to stop because it is faster and heavier.", true,
                    "Guide, Grades 6-8: e-bike speed and handling - allow more stopping distance"),
                TF("It is okay to speed up when turning my bike, to get it over with.", false,
                    "Guide, Grades 6-8: scanning before turns, hand signals, predictable riding"));
            var bankRr = MakeBank("rr-florida", "Florida Rules & Risk Radar",
                MC("Florida requires how many supervised driving hours (including 10 at night) before a teen license?",
                    "50 hours", "10 hours", "100 hours", "5 hours",
                    "Guide, Grades 9-12: FLHSMV lists 50 hours of supervised driving, including 10 at night"),
                MC("A 16-year-old licensed driver in Florida generally may drive between...",
                    "6 a.m. and 11 p.m.", "Any time", "9 a.m. and 5 p.m.", "Midnight and 6 a.m.",
                    "Guide, Grades 9-12: 16-year-old license generally carries a 6 a.m.-11 p.m. limit"),
                MC("Florida's 3-foot rule applies when a driver is...",
                    "Passing a bicyclist", "Passing a school", "Changing lanes", "Parking",
                    "Guide, Task 3 of proposal: selected Florida rules such as the 3-foot passing rule"),
                TF("Best habit before starting the car: set navigation and stow the phone.", true,
                    "Guide, Grades 9-12: set navigation before moving; silence and stow the phone"),
                TF("A car full of friends helps a new driver focus at night.", false,
                    "Guide, Grades 9-12: limited experience combined with peer passengers and night driving can increase risk"));
            AssetDatabase.CreateAsset(bankLe, "Assets/Quiz_le-crossing.asset");
            AssetDatabase.CreateAsset(bankSh, "Assets/Quiz_sh-school-bike.asset");
            AssetDatabase.CreateAsset(bankSt, "Assets/Quiz_st-riding.asset");
            AssetDatabase.CreateAsset(bankRr, "Assets/Quiz_rr-florida.asset");

            // ---- Feature 002: walk-in quiz zones (zone-to-bank mapping is a design choice:
            //      crossing zone teaches crossing, school zone school-zone rules, etc.) ----
            MakeQuizZone(root, "Quiz_Zebra", new Vector3(0f, 0f, -3f), new Vector2(3.6f, 5.2f), bankLe, "QUIZ: Crossing");
            MakeQuizZone(root, "Quiz_School", new Vector3(-11f, 4.9f, -3f), new Vector2(3.4f, 2.4f), bankSh, "QUIZ: School");
            MakeQuizZone(root, "Quiz_Park", new Vector3(12f, 4.9f, -3f), new Vector2(3.4f, 2.4f), bankSt, "QUIZ: Park");
            MakeQuizZone(root, "Quiz_Shop", new Vector3(-11f, -4.9f, -3f), new Vector2(3.4f, 2.4f), bankRr, "QUIZ: Shop");

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

            // ---- Spec 003 (GGD): second collider — the "sense bubble". A child
            //      TRIGGER that walks into Proximity2D zones; the solid body above
            //      still blocks walls. GGD: roomCollider + PlayerTriggerCollider.
            //      NOTE: SetWorldSize scaled the player ROOT (~0.125), so express
            //      the radius in WORLD units by dividing by the inherited scale.
            var sense = new GameObject("SenseTrigger");
            sense.transform.SetParent(player.transform, false);
            var senseCol = sense.AddComponent<CircleCollider2D>();
            senseCol.isTrigger = true;
            float worldRadius = 0.75f;
            senseCol.radius = worldRadius / Mathf.Max(0.0001f, player.transform.localScale.x);

            // ---- Spec 004 (GGD FogOfWar): dark overlay + vision mesh + NPC LOS hiding.
            //      The component self-bootstraps at PLAY start (scene reload wipes
            //      runtime refs); here we only place the component in the scene.
            var fogGo = new GameObject("FogOfWar2D");
            fogGo.transform.SetParent(root.transform);
            fogGo.AddComponent<SafeStreets.FogOfWar2D>();

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
            // ---- Physics layer MUST match the fog's raycast mask ("Collision", slot 10).
            //      "Collision" the SORTING layer (set below) is unrelated — without this,
            //      buildings sit on Default(0) and wall rays pass straight through them.
            int lid = LayerMask.NameToLayer("Collision");
            if (lid >= 0) go.layer = lid;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sortingLayerName = "Collision";
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            var inter = go.AddComponent<SafeStreets.Interactable>();
            inter.label = interactLabel;

            // ---- Spec 003 (GGD): a Proximity2D walk-in zone extending past the
            //      walls — standing in it makes this building the candidate.
            var zoneGo = new GameObject("Proximity2D");
            zoneGo.transform.SetParent(go.transform, false);
            var zoneCol = zoneGo.AddComponent<BoxCollider2D>();
            zoneCol.isTrigger = true;
            zoneCol.offset = new Vector2(0f, -0.55f);   // biased toward the road (approach side)
            zoneCol.size = size + new Vector2(2.4f, 1.6f);
            zoneGo.AddComponent<SafeStreets.Proximity2D>();
            inter.CollectProximities();

            // ---- Spec 004 (GGD SeeThroughMesh): fade the building while the player
            //      stands in its zone so the character is never hidden behind it.
            go.AddComponent<SafeStreets.SeeThroughWall2D>();

            // ---- Spec 004 (GGD TaskPanel): SCHOOL hosts the crossing-steps minigame.
            if (label == "SCHOOL")
            {
                var task = SafeStreets.DragDropTask.Create(parent,
                    "Get ready to cross",
                    new[] { "Stop at the curb", "Look left, look right", "Hold a grown-up's hand" });
                inter.TaskGame = task;
            }

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
                go.AddComponent<SafeStreets.FogEntity>();   // R4: fades in/out with vision (GGD)
            }
            else
            {
                var npc = go.AddComponent<SafeStreets.DialogNPC>();
                npc.conversation = conv;
                npc.interactLabel = name;
                go.AddComponent<SafeStreets.FogEntity>();   // R4: fades in/out with vision (GGD)
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

        // ---- Feature 002 helpers ----

        /// <summary>Multiple-choice question (correct = the FIRST choice; MC shuffles nothing).</summary>
        private static SafeStreets.QuizQuestion MC(string prompt, string correct,
            string wrong1, string wrong2, string wrong3, string source)
            => new SafeStreets.QuizQuestion
            {
                kind = QuestionKind.MultipleChoice,
                prompt = prompt,
                choices = new[] { correct, wrong1, wrong2, wrong3 },
                correctIndex = 0,
                source = source,
            };

        /// <summary>True/false question (right/wrong).</summary>
        private static SafeStreets.QuizQuestion TF(string statement, bool trueIsCorrect, string source)
            => new SafeStreets.QuizQuestion
            {
                kind = QuestionKind.TrueFalse,
                prompt = statement,
                trueIsCorrect = trueIsCorrect,
                source = source,
            };

        private static SafeStreets.QuizBank MakeBank(string activityId, string title,
            params SafeStreets.QuizQuestion[] questions)
        {
            var b = ScriptableObject.CreateInstance<SafeStreets.QuizBank>();
            b.activityId = activityId;
            b.title = title;
            b.questions = questions;
            return b;
        }

        /// <summary>Translucent walk-in zone + trigger + QuizZone + floating label.</summary>
        private static void MakeQuizZone(GameObject parent, string name, Vector3 pos,
            Vector2 size, SafeStreets.QuizBank bank, string label)
        {
            var go = Shape(parent, name, new Color(1f, 0.95f, 0.4f, 0.18f), pos, size);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sortingOrder = -3;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
            var zone = go.AddComponent<SafeStreets.QuizZone>();
            zone.bank = bank;

            var labelGo = new GameObject("ZoneLabel");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = new Vector3(0f, size.y * 0.5f + 0.45f, 0f);
            var tm = labelGo.AddComponent<TextMesh>();
            SafeStreets.DialogStyle.Style(tm, 0.12f, 28, TextAnchor.MiddleCenter,
                new Color(1f, 0.95f, 0.6f), 5);
            tm.text = label;
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
