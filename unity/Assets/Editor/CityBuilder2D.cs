using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SafeStreets.EditorTools
{
    /// <summary>
    /// Builds the full city scene (specs/map_design, draft v1) from
    /// docs/map_design/city_layout.json. JSON uses a top-left origin; Unity is
    /// center-origin, so every y flips: uy = 28 - jy. Street widths, zebra ladder
    /// orientation (rungs parallel to traffic), intersection control props, the
    /// school zone, quiz zones and the rail line all mirror the approved HTML
    /// draft. Run: Tools > City Builder or
    /// unity command eval 'return SafeStreets.EditorTools.CityBuilder2D.Build();'
    /// </summary>
    public static class CityBuilder2D
    {
        private const float PX_PER_UNIT = 10f;

        [MenuItem("Tools/Build City Scene")]
        static void MenuBuild()
        {
            var result = Build();
            EditorUtility.DisplayDialog("City Builder",
                result.StartsWith("city built") ? result : "FAILED - see Console", "OK");
        }

        public static string Build()
        {
            string jsonPath = "Packages/map_design/city_layout.json";
            if (!System.IO.File.Exists(jsonPath))
                jsonPath = "docs/map_design/city_layout.json"; // project root relative (cwd = unity/)
            string abs = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "..", "docs/map_design/city_layout.json"));
            if (!System.IO.File.Exists(abs))
                return "FAIL: city_layout.json not found at " + abs;
            var map = JsonUtility.FromJson<CityMap>(System.IO.File.ReadAllText(abs));
            if (map == null || map.streets == null || map.streets.Length == 0)
                return "FAIL: city_layout.json parsed empty (JsonUtility needs the [Serializable] shape below)";

            // R3: quiz-bank assets may have been created moments ago via eval — force an
            // import pass or LoadAssetAtPath below silently returns null (0 zones built).
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("SafeStreetsCity");
            int built = 0;

            // ground
            var ground = Shape(root, "Ground", Hex("#5B8C5A"), V(40, 28, 50), new Vector2(80, 56));
            ground.GetComponent<SpriteRenderer>().sortingOrder = -10; built++;

            // streets
            foreach (var s in map.streets)
            {
                bool h = s.orientation == "H";
                var go = Shape(root, "Street_" + s.name, Hex("#3F4247"),
                    h ? V(s.center.x, s.center.y, -1) : V(s.center.x, s.center.y, -1),
                    h ? new Vector2(StreetLen(s.from, s.to, 80), s.width)
                      : new Vector2(s.width, StreetLen(s.from, s.to, 56)));
                go.GetComponent<SpriteRenderer>().sortingOrder = -8; built++;

                if (s.sidewalks)
                {
                    float off = s.width / 2f + 0.3f;
                    if (h)
                    {
                        built += Sidewalk(root, V(s.center.x, s.center.y - off, -1), StreetLen(s.from, s.to, 80), 0.6f);
                        built += Sidewalk(root, V(s.center.x, s.center.y + off, -1), StreetLen(s.from, s.to, 80), 0.6f);
                    }
                    else
                    {
                        built += Sidewalk(root, V(s.center.x - off, s.center.y, -1), 0.6f, StreetLen(s.from, s.to, 56));
                        built += Sidewalk(root, V(s.center.x + off, s.center.y, -1), 0.6f, StreetLen(s.from, s.to, 56));
                    }
                }
            }

            // centerlines (yellow) on the 2-lane streets
            foreach (var s in map.streets)
            {
                if (s.lanes < 2) continue;
                bool h = s.orientation == "H";
                var go = Shape(root, "Line_" + s.name, Hex("#E8C55A"),
                    h ? V(s.center.x, s.center.y, -2) : V(s.center.x, s.center.y, -2),
                    h ? new Vector2(StreetLen(s.from, s.to, 80), 0.16f) : new Vector2(0.16f, StreetLen(s.from, s.to, 56)));
                go.GetComponent<SpriteRenderer>().sortingOrder = -6; built++;
            }

            // bike lane along the avenue (south side, per draft)
            var bike = Shape(root, "BikeLane", Hex("#5A9A5A"), V(32, 29.6f, -3), new Vector2(60, 0.8f));
            bike.GetComponent<SpriteRenderer>().sortingOrder = -5; built++;

            // rail line (west edge)
            var rail = Shape(root, "Rail", Hex("#6B6F75"), V(2, 28, -2), new Vector2(0.9f, 56));
            rail.GetComponent<SpriteRenderer>().sortingOrder = -4; built++;

            // zebra crossings: ladder rungs parallel to traffic across the walking band
            foreach (var cw in map.crosswalks)
            {
                bool acrossHorizontal = cw.angle == 0;   // crosses a horizontal road
                int rungs = Mathf.Max(3, Mathf.RoundToInt(cw.length / 0.75f));
                for (int i = 0; i < rungs; i++)
                {
                    float t = (i + 0.5f) / rungs - 0.5f;
                    // R4: rungs must be placed at the CROSSING (world pos), not at a
                    // child-local offset — Shape() sets world position, so the old
                    // child approach teleported all rungs to the map origin.
                    var go = Shape(root, "rung", Hex("#EBECE0"),
                        acrossHorizontal ? V(cw.x, cw.y + t * cw.length, -3)
                                         : V(cw.x + t * cw.length, cw.y, -3),
                        acrossHorizontal ? new Vector2(2.2f, 0.32f) : new Vector2(0.32f, 2.2f));
                    go.GetComponent<SpriteRenderer>().sortingOrder = -3; built++;
                }
            }

            // landmarks
            foreach (var lm in map.landmarks)
            {
                var go = Shape(root, "LM_" + lm.name, Hex(lm.color), V(lm.x + lm.w / 2, lm.y + lm.h / 2, -1),
                    new Vector2(lm.w, lm.h));
                go.GetComponent<SpriteRenderer>().sortingOrder = -2;
                Label(root, lm.name, V(lm.x + lm.w / 2, lm.y + lm.h / 2 - (lm.h > 4 ? 1.2f : 0.6f), 0),
                    lm.w > 10 ? 1.1f : 0.7f, -1);
                built++;
                // solid colliders so buildings block movement (and fog rays)
                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(lm.w, lm.h);
                col.offset = Vector2.zero;
                if (go.layer == 0) go.layer = LayerMask.NameToLayer("Collision") >= 0 ? LayerMask.NameToLayer("Collision") : 0;
            }

            // intersections: control props
            foreach (var it in map.intersections)
            {
                switch (it.type)
                {
                    case "signal":
                        PropMarker(root, "Signal", V(it.x - 1.6f, it.y - 1.6f, 5), Hex("#2C3E50"), 0.5f); built++;
                        PropMarker(root, "Signal", V(it.x + 1.6f, it.y + 1.6f, 5), Hex("#2C3E50"), 0.5f); built++;
                        break;
                    case "stop4":
                    case "stop2":
                        PropMarker(root, "Stop", V(it.x - 1.4f, it.y + 1.4f, 5), Hex("#C0392B"), 0.45f); built++;
                        break;
                    case "roundabout":
                        var ra = Shape(root, "Roundabout", Hex("#4A4D52"), V(it.x, it.y, -3), new Vector2(4.2f, 4.2f));
                        ra.GetComponent<SpriteRenderer>().sortingOrder = -3;
                        var rc = ra.AddComponent<CircleCollider2D>(); rc.radius = 2.1f;
                        built++;
                        break;
                }
            }

            // quiz zones (walk-in QuizZone)
            string[] zoneBanks =
            {
                "rr-florida", "le-crossing", "sh-school-bike", "st-riding", "rr-florida",
                "parkinglot-safety", "bus-stop-rules", "signals-signs", "railroad-crossing", "driveway-blindspot"
            };
            int zi = 0, zonesSeen = 0, zonesNullBank = 0; string zoneDebug = "";
            foreach (var z in map.zones)
            {
                zonesSeen++;
                if (z.kind == "park-pond")
                {
                    var pond = Shape(root, "Pond", Hex("#4A7A96"), V(z.x + z.w / 2, z.y + z.h / 2, -2),
                        new Vector2(z.w, z.h));
                    pond.GetComponent<SpriteRenderer>().sortingOrder = -2; built++;
                    continue;
                }
                if (z.kind == "school-zone")
                {
                    var szg = Shape(root, "SchoolZone", new Color(1f, 0.95f, 0.3f, 0.30f),
                        V(z.x + z.w / 2, z.y + z.h / 2, -2), new Vector2(z.w, z.h));
                    szg.GetComponent<SpriteRenderer>().sortingOrder = -3; built++;
                    Label(root, "SCHOOL ZONE", V(z.x + z.w - 3, z.y + 0.4f, 0), 0.8f, -1); built++;
                    continue;
                }
                string bankId = zoneBanks[zi % zoneBanks.Length]; zi++;
                zoneDebug += z.name + "->" + bankId + " ";
                var bankAsset = LoadOrCreateBank(bankId);
                MakeQuizZone(root, "Quiz_" + bankId,
                    V(z.x + z.w / 2, z.y + z.h / 2, -3), new Vector2(z.w, z.h), bankAsset, "QUIZ: " + z.name);
                built++;
            }

            // NPCs (talk/escort cast at their posts)
            foreach (var npc in map.npcs)
            {
                MakeNpc(root, npc.name, Hex("#F28C26"), V(npc.x, npc.y, 0), null, false);
                Label(root, npc.name, V(npc.x, npc.y + 0.9f, 0), 0.62f, 5);
                built += 2;
            }

            // player spawn (house corner, per draft)
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.SetParent(root.transform);
            player.transform.position = V(19.7f, 37.5f, 0);
            var psr = player.AddComponent<SpriteRenderer>();
            psr.sprite = SceneGeo.CircleSpritePublic();
            psr.color = Hex("#F28C26");
            psr.sortingLayerName = "Player";
            psr.sortingOrder = 10;
            player.AddComponent<CircleCollider2D>().radius = 0.4f;
            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            player.AddComponent<SafeStreets.PlayerController2D>();
            SceneGeo.SetWorldSize(psr, new Vector2(0.8f, 0.8f));
            var sense = new GameObject("SenseTrigger");
            sense.transform.SetParent(player.transform, false);
            var senseCol = sense.AddComponent<CircleCollider2D>();
            senseCol.isTrigger = true;
            senseCol.radius = 0.75f / Mathf.Max(0.0001f, player.transform.localScale.x);
            built++;

            // systems + camera + fog
            new GameObject("InteractionManager").AddComponent<SafeStreets.InteractionManager>();
            new GameObject("DialogManager").AddComponent<SafeStreets.DialogManager>();
            new GameObject("QuizManager").AddComponent<SafeStreets.QuizManager>();
            new GameObject("QuestManager").AddComponent<SafeStreets.QuestManager>();
            var fogGo = new GameObject("FogOfWar2D");
            fogGo.AddComponent<SafeStreets.FogOfWar2D>();
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("#5B8C5A");
            camGo.AddComponent<SafeStreets.CameraFollow2D>().target = player.transform;
            camGo.transform.position = V(19.7f, 37.5f, -10);
            built += 6;

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/City.unity");
            return $"city built, roots={scene.rootCount}, items={built} | zonesSeen={zonesSeen} nullBank={zonesNullBank} | {zoneDebug}";
        }

        // ---- local shape helpers (mirror SceneGeo's private builders) ----

        /// <summary>
        /// R3: eval-created .asset files sometimes fail LoadAssetAtPath in the same
        /// session; this loads OR re-creates the five new banks so quiz zones can
        /// never silently skip. Content = Agent B's pedagogy file, verbatim.
        /// </summary>
        private static SafeStreets.QuizBank LoadOrCreateBank(string id)
        {
            var existing = AssetDatabase.LoadAssetAtPath<SafeStreets.QuizBank>("Assets/Quiz_" + id + ".asset");
            if (existing != null) return existing;
            SafeStreets.QuizBank b = null;
            switch (id)
            {
                case "parkinglot-safety":
                    b = SafeStreets.EditorTools.SceneGeo.MakeBank(id, "Parking Lot Safety",
                        SafeStreets.EditorTools.SceneGeo.MC("Where do you walk in a parking lot?", "In the aisle where drivers can see you, with a grown-up", "Between parked cars to be faster", "Behind cars that are backing up", "Anywhere - cars always stop", "Guide, Preschool-Grade 1: keep an adult close in parking lots; parking areas are not play spaces"),
                        SafeStreets.EditorTools.SceneGeo.MC("A car has white backup lights on. What do you do?", "Stop and wait until it leaves or the driver waves you past", "Run behind it quickly", "Tap the car so it stops", "Keep walking - it sees you", "Guide, Grades 2-3: look for turning vehicles, driveway traffic, backing vehicles"),
                        SafeStreets.EditorTools.SceneGeo.MC("Your toy rolls under a parked car. What do you do?", "Tell a grown-up instead of crawling under", "Crawl under and grab it fast", "Ask a stranger to get it", "Kick the car until it rolls out", "Guide, Preschool-Grade 1: children do not chase a ball or retrieve an item"));
                    break;
                case "bus-stop-rules":
                    b = SafeStreets.EditorTools.SceneGeo.MakeBank(id, "Bus Stop Rules",
                        SafeStreets.EditorTools.SceneGeo.MC("Where do you wait for the bus?", "On the sidewalk, two big steps back from the curb", "Right at the curb edge", "In the street so the driver sees you", "Behind the shelter where nobody sees you", "Guide, Grades 2-3: wait on the sidewalk away from the curb until the bus fully stops"),
                        SafeStreets.EditorTools.SceneGeo.MC("When is it safe to walk to the bus door?", "After the bus fully stops and the driver signals you", "While it is still rolling up", "As soon as you see it coming", "When your friend runs first", "Guide, Grades 2-3: board only after the bus stops and the driver signals"),
                        SafeStreets.EditorTools.SceneGeo.MC("You drop your backpack in the bus danger zone. What do you do?", "Tell the bus driver instead of picking it up", "Dive under the bus to grab it", "Have a friend crawl in for it", "Wait under the bus until it leaves", "Guide, Grades 2-3: if an item falls near the bus, tell the driver instead of retrieving it"));
                    break;
                case "signals-signs":
                    b = SafeStreets.EditorTools.SceneGeo.MakeBank(id, "Signals and Signs",
                        SafeStreets.EditorTools.SceneGeo.MC("The WALK sign just turned on. What do you do first?", "Still look both ways for turning cars before stepping out", "Run without looking - the sign protects you", "Look at your phone while crossing", "Cross diagonally to save time", "Guide, Grades 4-5: obey pedestrian signals and still check for turning vehicles"),
                        SafeStreets.EditorTools.SceneGeo.MC("At a 4-way stop, when may you cross in front of a stopped car?", "After eye contact or a wave showing the driver sees you", "The instant the car stops", "While the car is still rolling", "Only if no driver is inside", "Guide, Grades 4-5: make eye contact with drivers before crossing"),
                        SafeStreets.EditorTools.SceneGeo.MC("Where do you cross near a roundabout?", "At the marked crosswalk set back from the circle", "Straight across the middle of the circle", "Between cars waiting to enter", "Anywhere - cars go slow there", "Guide, Grades 6-8: use designated crossings at roundabouts; never cross the circulatory roadway"));
                    break;
                case "railroad-crossing":
                    b = SafeStreets.EditorTools.SceneGeo.MakeBank(id, "Railroad Crossings",
                        SafeStreets.EditorTools.SceneGeo.MC("The crossing lights are flashing but the gate is still going down. What do you do?", "Stop and wait - never cross when lights flash", "Hurry across before the gate falls", "Duck under the gate quickly", "Cross if no train is visible yet", "Guide, Grades 4-5: never cross railroad tracks when lights flash or gates are moving"),
                        SafeStreets.EditorTools.SceneGeo.MC("Your bike gets stuck on the tracks and a train is coming. What first?", "Leave the bike, get off the tracks, then call for help", "Stay and keep pulling the bike", "Lie down between the rails", "Run toward the train waving", "Guide, Grades 4-5: if stuck on tracks, leave belongings and get clear immediately"),
                        SafeStreets.EditorTools.SceneGeo.MC("Where do you stand while waiting for a train to pass?", "Well behind the gate and the stop line", "Right next to the rails to see better", "Between the gate and the tracks", "On the tracks if the gate is up", "Guide, Grades 4-5: wait behind gates and stop lines until lights stop and gates fully rise"));
                    break;
                case "driveway-blindspot":
                    b = SafeStreets.EditorTools.SceneGeo.MakeBank(id, "Driveways and Blind Spots",
                        SafeStreets.EditorTools.SceneGeo.MC("What tells you a parked car might back up over the sidewalk?", "White reverse lights and turning back wheels", "The cars color", "Loud music inside", "A clean windshield", "Guide, Grades 2-3: look for turning vehicles, driveway traffic, backing vehicles"),
                        SafeStreets.EditorTools.SceneGeo.MC("A driver is sitting in a parked car. What should you assume?", "They might not see you - walk where they can see you or wait", "They always see you", "It is safe to walk right behind the car", "Hood up means go", "Guide, Grades 6-8: door zone, large-vehicle blind spots, I can see, I can be seen"),
                        SafeStreets.EditorTools.SceneGeo.MC("Riding past parked cars, where is the danger zone?", "Right beside car doors that can swing open", "In the middle of the road", "On the far sidewalk", "There is no danger in daylight", "Guide, Grades 6-8: door zone, large-vehicle blind spots, I can see, I can be seen"));
                    break;
            }
            if (b != null)
            {
                AssetDatabase.CreateAsset(b, "Assets/Quiz_" + id + ".asset");
                AssetDatabase.SaveAssets();
            }
            return b;
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

        // ---- JSON shape (JsonUtility needs concrete arrays) ----
        [System.Serializable] private class CityMap
        {
            public World world; public Street[] streets; public Intersection[] intersections;
            public Crosswalk[] crosswalks; public Landmark[] landmarks; public Prop[] props;
            public Zone[] zones; public Npc[] npcs;
        }
        [System.Serializable] private class World { public float width; public float height; }
        [System.Serializable] private class Center { public float x; public float y; }
        [System.Serializable] private class Street
        {
            public string name; public string orientation; public Center center; public float from; public float to;
            public float width; public int lanes;
            public bool oneway; public bool bikeLane; public bool sidewalks;
        }
        [System.Serializable] private class Intersection { public float x; public float y; public string type; public string[] streets; }
        [System.Serializable] private class Crosswalk { public float x; public float y; public int angle; public float length; public string note; }
        [System.Serializable] private class Landmark
        {
            public string name; public string kind; public float x; public float y; public float w; public float h;
            public string color; public string[] props;
        }
        [System.Serializable] private class Prop { public string type; public float x; public float y; public float angle; }
        [System.Serializable] private class Zone { public string name; public string kind; public float x; public float y; public float w; public float h; }
        [System.Serializable] private class Npc { public string name; public string role; public float x; public float y; }

        // ---- helpers ----
        // JSON top-left origin -> Unity center origin: uy = 28 - jy
        private static float Y(float jsonY) => 28f - jsonY;
        private static Vector3 V(float jx, float jy, float z) => new Vector3(jx, Y(jy), z);
        private static float StreetLen(float from, float to, float full)
            => (to - from) >= full - 0.01f ? full : (to - from);

        private static int Sidewalk(GameObject root, Vector3 pos, float w, float h)
        {
            var go = Shape(root, "Sidewalk", Hex("#B9BDC4"), pos, new Vector2(w, h));
            go.GetComponent<SpriteRenderer>().sortingOrder = -7;
            return 1;
        }

        private static void PropMarker(GameObject root, string name, Vector3 pos, Color color, float size)
        {
            var go = Shape(root, name, color, pos, new Vector2(size, size));
            go.GetComponent<SpriteRenderer>().sortingOrder = 5;
        }

        private static void Label(GameObject root, string text, Vector3 pos, float size, int order)
        {
            var go = new GameObject("Lbl_" + text);
            go.transform.SetParent(root.transform);
            go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            // R2: size*0.04 rendered ~2.5x too small (vision check); 0.10-0.15 char size
            // is the legible band used elsewhere in this project.
            SafeStreets.DialogStyle.Style(tm, Mathf.Clamp(size * 0.11f, 0.09f, 0.16f), 26,
                TextAnchor.MiddleCenter, Color.white, order >= 0 ? order : 5);
            tm.text = text;
        }

        private static void MakeQuizZone(GameObject parent, string name, Vector3 pos, Vector2 size,
            SafeStreets.QuizBank bank, string label)
        {
            var go = Shape(parent, name, new Color(1f, 0.95f, 0.4f, 0.32f), pos, size);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sortingOrder = -3;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
            var zone = go.AddComponent<SafeStreets.QuizZone>();
            zone.bank = bank;
            var lgo = new GameObject("ZoneLabel");
            lgo.transform.SetParent(go.transform);
            lgo.transform.localPosition = new Vector3(0f, size.y * 0.5f + 0.45f, 0f);
            var tm = lgo.AddComponent<TextMesh>();
            SafeStreets.DialogStyle.Style(tm, 0.12f, 28, TextAnchor.MiddleCenter,
                new Color(1f, 0.95f, 0.6f), 5);
            tm.text = label;
        }

        private static void MakeNpc(GameObject parent, string name, Color color, Vector3 pos,
            SafeStreets.Conversation conv, bool wantsCrossing)
        {
            var go = new GameObject("NPC_" + name);
            go.transform.SetParent(parent.transform);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SceneGeo.SquareSpritePublic();
            sr.color = color;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 5;
            SceneGeo.SetWorldSize(sr, new Vector2(1.2f, 1.2f));
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3.2f, 2.6f);
            if (wantsCrossing)
            {
                var c = go.AddComponent<SafeStreets.CrossingNPC>();
                c.npcName = name;
                go.AddComponent<SafeStreets.FogEntity>();
            }
            else
            {
                var npc = go.AddComponent<SafeStreets.DialogNPC>();
                npc.interactLabel = name;
                go.AddComponent<SafeStreets.FogEntity>();
            }
        }

        public static Color Hex(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                float r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
                float g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
                float b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
                return new Color(r, g, b);
            }
            return Color.magenta;
        }

        private static Vector3 V2(float jx, float jy, float z) => V(jx, jy, z);
    }
}
