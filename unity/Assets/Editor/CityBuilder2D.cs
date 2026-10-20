using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SafeStreets.EditorTools
{
    /// <summary>
    /// Builds the full city scene (docs/map_design, draft v1) from
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
            // Application.dataPath = <repo>/unity/Assets — immune to the caller's CWD
            // (the old Directory.GetCurrentDirectory()+"/.." only worked when launched
            // from unity/, which an eval or test-runner context can't guarantee)
            string abs = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                Application.dataPath, "..", "..", "docs/map_design/city_layout.json"));
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

            // ---- collision policy (demo polish) ----
            // Shapes used to be plain SpriteRenderers with NO collider, so roads,
            // sidewalks, ponds and every building were pure paint: the kid walked straight
            // through the library "into" it, which reads as broken on a demo screen.
            // MakeSolid() now turns exactly two things solid at the END of the build:
            // BUILDINGS and WATER. Paint (ground/roads/sidewalks/lines) and decor
            // (props/labels) stay non-solid, so they can never fence the kid in.
            var solid = new List<GameObject>();

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

            // centerlines (yellow) on the 2-lane streets — broken at junction boxes
            // (map polish: a solid line painted straight through an intersection reads
            // as one road bleeding into the next)
            foreach (var s in map.streets)
            {
                if (s.lanes < 2) continue;
                bool h = s.orientation == "H";
                float len = StreetLen(s.from, s.to, h ? 80 : 56);
                float mid = h ? s.center.x : s.center.y;
                foreach (var seg in MarkingSegments(mid - len / 2f, mid + len / 2f,
                    CrossingStreets(map, s, h), h))
                {
                    float segLen = seg.y - seg.x, segMid = (seg.x + seg.y) / 2f;
                    var go = Shape(root, "Line_" + s.name, Hex("#E8C55A"),
                        h ? V(segMid, s.center.y, -2) : V(s.center.x, segMid, -2),
                        h ? new Vector2(segLen, 0.16f) : new Vector2(0.16f, segLen));
                    go.GetComponent<SpriteRenderer>().sortingOrder = -6; built++;
                }
            }

            // bike lane along the avenue (south side, per draft) — broken at the
            // junctions it passes through (same rule as the centerlines)
            {
                var bikeCrossers = new List<Street>();
                foreach (var p in map.streets)
                    if (p.orientation == "V" && p.from <= 29.6f && p.to >= 29.6f) bikeCrossers.Add(p);
                foreach (var seg in MarkingSegments(2f, 62f, bikeCrossers, true))
                {
                    var bike = Shape(root, "BikeLane", Hex("#5A9A5A"),
                        V((seg.x + seg.y) / 2f, 29.6f, -3), new Vector2(seg.y - seg.x, 0.8f));
                    bike.GetComponent<SpriteRenderer>().sortingOrder = -5; built++;
                }
            }

            // rail line (west edge)
            var rail = Shape(root, "Rail", Hex("#6B6F75"), V(2, 28, -2), new Vector2(0.9f, 56));
            rail.GetComponent<SpriteRenderer>().sortingOrder = -4; built++;

            // zebra crossings: ladder rungs parallel to traffic across the walking band.
            // Map polish: the JSON centers intersection crosswalks on the junction point,
            // so BOTH orientations painted their ladders straight through the middle of
            // the box — and through each other. Real crossings sit on the intersection
            // LEGS: when a perpendicular street covers the band center, paint one
            // crossing on each leg instead, clear of the junction box. Mid-block
            // crossings (school, bus stop) and the set-back roundabout legs are not
            // inside any junction and keep their JSON positions.
            var crossings = new List<CrossingZone>();
            foreach (var cw in map.crosswalks)
            {
                bool acrossHorizontal = cw.angle == 0;   // crosses a horizontal road
                var centers = new List<float> { acrossHorizontal ? cw.x : cw.y };
                if (acrossHorizontal)
                {
                    foreach (var v in map.streets)
                        if (v.orientation == "V" && cw.y >= v.from && cw.y <= v.to
                            && Mathf.Abs(cw.x - v.center.x) <= v.width / 2f)
                        {
                            float d = v.width / 2f + 1.1f + 0.45f;   // junction edge + half rung + setback
                            centers = new List<float> { v.center.x - d, v.center.x + d };
                            break;
                        }
                }
                else
                {
                    foreach (var h in map.streets)
                        if (h.orientation == "H" && cw.x >= h.from && cw.x <= h.to
                            && Mathf.Abs(cw.y - h.center.y) <= h.width / 2f)
                        {
                            float d = h.width / 2f + 1.1f + 0.45f;
                            centers = new List<float> { h.center.y - d, h.center.y + d };
                            break;
                        }
                }
                foreach (float c in centers)
                {
                    int rungs = Mathf.Max(3, Mathf.RoundToInt(cw.length / 0.75f));
                    var crossingGo = new GameObject("Crossing_" + crossings.Count);
                    crossingGo.transform.SetParent(root.transform);
                    var center = acrossHorizontal ? V(c, cw.y, 0) : V(cw.x, c, 0);
                    crossingGo.transform.position = center;
                    var crossing = crossingGo.AddComponent<CrossingZone>();
                    crossing.roadRunsAlongX = acrossHorizontal;
                    crossing.roadCenter = center;
                    crossing.roadHalfExtent = cw.length * 0.5f;
                    float paintedLength = cw.length * (1f - 1f / rungs) + 0.32f;
                    var size = acrossHorizontal ? new Vector2(2.2f, paintedLength) : new Vector2(paintedLength, 2.2f);
                    crossing.corridor = new Rect((Vector2)center - size * 0.5f, size);
                    crossings.Add(crossing);
                    for (int i = 0; i < rungs; i++)
                    {
                        float t = (i + 0.5f) / rungs - 0.5f;
                        // R4: rungs must be placed at the CROSSING (world pos), not at a
                        // child-local offset — Shape() sets world position, so the old
                        // child approach teleported all rungs to the map origin.
                        var go = Shape(root, "rung", Hex("#EBECE0"),
                            acrossHorizontal ? V(c, cw.y + t * cw.length, -3)
                                             : V(cw.x + t * cw.length, c, -3),
                            acrossHorizontal ? new Vector2(2.2f, 0.32f) : new Vector2(0.32f, 2.2f));
                        go.GetComponent<SpriteRenderer>().sortingOrder = -3; built++;
                    }
                }
            }

            // props from the layout JSON (map polish: these 39 placed markers were
            // parsed but never drawn — traffic lights at the TRUE junction corners,
            // stop signs, trees, lamps, hydrants, benches, bus stops, the rail gate.
            // The old two-square signal stand-ins were wrongly positioned and
            // duplicated this data, so the stand-ins are gone.)
            foreach (var p in map.props)
            {
                Color c; Vector2 sz; int order = 5; bool circle = false;
                switch (p.type)
                {
                    case "trafficlight":  c = Hex("#2C3E50"); sz = new Vector2(0.45f, 0.45f); break;
                    case "stopsign":      c = Hex("#C0392B"); sz = new Vector2(0.40f, 0.40f); break;
                    case "tree":          c = Hex("#2E7D46"); sz = new Vector2(1.5f, 1.5f); order = 2; circle = true; break;
                    case "lamp":          c = Hex("#F4D03F"); sz = new Vector2(0.35f, 0.35f); order = 2; break;
                    case "hydrant":       c = Hex("#E74C3C"); sz = new Vector2(0.35f, 0.35f); order = 2; break;
                    case "bench":         c = Hex("#8D6E63"); sz = new Vector2(0.9f, 0.35f); order = 2; break;
                    case "busstop":       c = Hex("#34495E"); sz = new Vector2(1.3f, 0.45f); break;
                    case "railroad-gate": c = Hex("#C0392B"); sz = new Vector2(2.8f, 0.28f); break;
                    case "crossbuck":     c = Hex("#ECF0F1"); sz = new Vector2(0.8f, 0.8f); break;
                    default:              c = Hex("#7F8C8D"); sz = new Vector2(0.4f, 0.4f); break;
                }
                GameObject pgo;
                if (circle)
                {
                    pgo = new GameObject("Prop_" + p.type);
                    pgo.transform.SetParent(root.transform);
                    pgo.transform.position = V(p.x, p.y, 5);
                    var csr = pgo.AddComponent<SpriteRenderer>();
                    csr.sprite = SceneGeo.CircleSpritePublic();
                    csr.color = c;
                    csr.sortingOrder = order;
                    SceneGeo.SetWorldSize(csr, sz);
                }
                else
                {
                    pgo = Shape(root, "Prop_" + p.type, c, V(p.x, p.y, 5), sz);
                    pgo.GetComponent<SpriteRenderer>().sortingOrder = order;
                }
                built++;
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

            // intersections: lights/signs now come from map.props at the true junction
            // corners (see props loop above). Only the roundabout is geometry, so it
            // stays here.
            foreach (var it in map.intersections)
            {
                if (it.type != "roundabout") continue;
                // circle, not square: the CircleCollider2D is r=2.1, and a gray square
                // over the junction read as a road block, not a roundabout (map polish)
                var ring = new GameObject("Roundabout");
                ring.transform.SetParent(root.transform);
                ring.transform.position = V(it.x, it.y, -3);
                var rsr = ring.AddComponent<SpriteRenderer>();
                rsr.sprite = SceneGeo.CircleSpritePublic();
                rsr.color = Hex("#4A4D52");
                rsr.sortingOrder = -3;
                SceneGeo.SetWorldSize(rsr, new Vector2(4.2f, 4.2f));
                var island = new GameObject("RoundaboutIsland");
                island.transform.SetParent(root.transform);
                island.transform.position = V(it.x, it.y, -3.5f);   // z closer: island always over the ring
                var isr = island.AddComponent<SpriteRenderer>();
                isr.sprite = SceneGeo.CircleSpritePublic();
                isr.color = Hex("#5B8C5A");
                isr.sortingOrder = -3;   // same order: drawn after the ring, on top
                SceneGeo.SetWorldSize(isr, new Vector2(2.3f, 2.3f));
                var rc = ring.AddComponent<CircleCollider2D>(); rc.radius = 2.1f;
                built++;
            }

            // quiz zones (walk-in QuizZone) — each zone now owns its own bank id via
            // ActivityIdForZone, so no zone shares (and none silently loses) a bank.
            int zonesSeen = 0, zonesNullBank = 0; string zoneDebug = "";
            foreach (var z in map.zones)
            {
                zonesSeen++;
                if (z.kind == "park-pond")
                {
                    var pond = Shape(root, "Pond", Hex("#4A7A96"), V(z.x + z.w / 2, z.y + z.h / 2, -2),
                        new Vector2(z.w, z.h));
                    pond.GetComponent<SpriteRenderer>().sortingOrder = -2; built++;
                    solid.Add(pond);       // water: the kid should not swim
                    continue;
                }
                if (z.kind == "school-zone")
                {
                    var szg = Shape(root, "SchoolZone", new Color(1f, 0.95f, 0.3f, 0.30f),
                        V(z.x + z.w / 2, z.y + z.h / 2, -2), new Vector2(z.w, z.h));
                    szg.GetComponent<SpriteRenderer>().sortingOrder = -3; built++;
                    // Demo polish: sat on the y=0 sidewalk line where the crossing guard
                    // walks; lift it to the top edge of the painted zone.
                    Label(root, "SCHOOL ZONE", V(z.x + z.w / 2, z.y + z.h - 0.35f, 0), 0.8f, -1); built++;
                    continue;
                }
                // Demo polish: a MISSING bank used to leave a zone with bank == null, i.e.
                // a gold rectangle that says QUIZ and does nothing when SPACE is pressed.
                // Map the zone to its OWN activity id instead of sharing one bank object
                // across zones, and skip (loudly) any zone whose bank failed to load.
                // zone.name -> "<activityId>|quiz: <Zone Name>" (see ActivityIdForZone).
                string activity = ActivityIdForZone(z.name);
                zoneDebug += z.name + "->" + activity + " ";
                var bankAsset = LoadOrCreateBank(activity);
                if (bankAsset == null)
                {
                    zonesNullBank++;
                    zoneDebug += "(NO BANK) ";
                    continue;
                }
                MakeQuizZone(root, "Quiz_" + activity,
                    V(z.x + z.w / 2, z.y + z.h / 2, -3), new Vector2(z.w, z.h), bankAsset, "QUIZ: " + z.name);
                built++;
            }

            // ---- crossings + NPC cast (demo polish) ----
            var cast = CityContent.All();
            foreach (var npc in map.npcs)
            {
                var role = System.Array.Find(cast, entry => entry.name == npc.name);
                Conversation conversation = role != null ? role.conv : null;
                if (conversation == null)
                {
                    CityContent.EscortScript(npc.name, out var lines, out var tip);
                    conversation = CityContent.Conv(npc.name, lines);
                    conversation.closingTip = tip;
                }
                string path = "Assets/Conversation_City_" + npc.name.Replace(" ", "_") + ".asset";
                var saved = AssetDatabase.LoadAssetAtPath<Conversation>(path);
                if (saved == null) { AssetDatabase.CreateAsset(conversation, path); saved = conversation; }
                else { EditorUtility.CopySerialized(conversation, saved); Object.DestroyImmediate(conversation); EditorUtility.SetDirty(saved); }
                MakeNpc(root, npc.name, Hex("#F28C26"), V(npc.x, npc.y, 0), saved, false);
                Label(root, npc.name, V(npc.x, npc.y - 0.9f, 0), 0.62f, 5);
                built += 2;
            }
            foreach (var obstacle in solid)
            {
                var collider = obstacle.GetComponent<BoxCollider2D>();
                if (collider == null) collider = obstacle.AddComponent<BoxCollider2D>();
                collider.size = obstacle.GetComponent<SpriteRenderer>().sprite.bounds.size;
            }
            AssetDatabase.SaveAssets();

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
        private static string ActivityIdForZone(string name)
        {
            switch (name)
            {
                case "Parking lot": return "parkinglot-safety";
                case "Bus stop": return "bus-stop-rules";
                case "Bike lane": return "sh-school-bike";
                case "Railroad crossing": return "railroad-crossing";
                case "Roundabout approach": return "signals-signs";
                case "Gas forecourt": return "driveway-blindspot";
                case "Teen driver corridor": return "rr-florida";
                default: return "le-crossing";
            }
        }

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

        /// <summary>Streets perpendicular to s that actually cross it (marking-break candidates).</summary>
        private static List<Street> CrossingStreets(CityMap map, Street s, bool sIsHorizontal)
        {
            var list = new List<Street>();
            foreach (var p in map.streets)
            {
                if ((p.orientation == "H") == sIsHorizontal) continue;   // same orientation never crosses
                bool coversS = sIsHorizontal ? (p.from <= s.center.y && p.to >= s.center.y)
                                             : (p.from <= s.center.x && p.to >= s.center.x);
                bool coveredByS = sIsHorizontal ? (s.from <= p.center.x && s.to >= p.center.x)
                                                : (s.from <= p.center.y && s.to >= p.center.y);
                if (coversS && coveredByS) list.Add(p);
            }
            return list;
        }

        /// <summary>
        /// Map polish: splits [from,to] at each crossing street's band, so centerlines
        /// and the bike lane break at junction boxes instead of painting through them.
        /// </summary>
        private static List<Vector2> MarkingSegments(float from, float to, List<Street> crossers,
            bool brokenStreetIsHorizontal)
        {
            var segs = new List<Vector2>();
            var bands = new List<Vector2>();
            foreach (var c in crossers)
            {
                float m = brokenStreetIsHorizontal ? c.center.x : c.center.y;   // band along the broken axis
                bands.Add(new Vector2(m - c.width / 2f, m + c.width / 2f));
            }
            bands.Sort((a, b) => a.x.CompareTo(b.x));
            float cursor = from;
            foreach (var b in bands)
            {
                if (b.x >= to || b.y <= cursor) continue;
                if (b.x > cursor + 0.05f) segs.Add(new Vector2(cursor, b.x));
                cursor = Mathf.Max(cursor, b.y);
            }
            if (to > cursor + 0.05f) segs.Add(new Vector2(cursor, to));
            return segs;
        }

        private static int Sidewalk(GameObject root, Vector3 pos, float w, float h)
        {
            var go = Shape(root, "Sidewalk", Hex("#B9BDC4"), pos, new Vector2(w, h));
            go.GetComponent<SpriteRenderer>().sortingOrder = -7;
            return 1;
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
                npc.conversation = conv;
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

    }
}
