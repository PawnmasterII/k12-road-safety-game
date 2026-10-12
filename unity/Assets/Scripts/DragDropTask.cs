using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Spec 004 — GGD DraggableTaskPanel port: drag item tiles into ordered
    /// slots. All tiles placed in the right slots -> CompleteAndClose.
    /// Wrong slot -> tile bounces back home (kid-friendly: no fail state; the
    /// base-class FailAndClose stays available / used by the X button).
    /// Built from primitives; content for the SCHOOL "crossing steps" task.
    /// </summary>
    public class DragDropTask : TaskPanel
    {
        public static DragDropTask ActiveInstance { get; private set; }

        private string _title;
        private string[] _steps;          // correct order
        private GameObject _backdrop;
        private readonly System.Collections.Generic.List<Tile> _tiles = new();
        private readonly System.Collections.Generic.List<Slot> _slots = new();

        private struct Slot { public GameObject go; public string expected; public bool filled; public Vector2 pos; }
        private class Tile
        {
            public GameObject go; public string step; public Vector2 home;
            public bool placed;
        }

        private static readonly Color PanelCol = new(0.07f, 0.13f, 0.20f, 0.97f);
        private static readonly Color SlotCol = new(0.16f, 0.26f, 0.38f, 1f);
        private static readonly Color SlotOkCol = new(0.30f, 0.65f, 0.35f, 1f);
        private static readonly Color TileCol = new(0.95f, 0.72f, 0.25f, 1f);

        /// <summary>Build the minigame panel as its OWN child GameObject under host.
        /// (Never AddComponent onto host itself — SetActive(false) on the panel would
        /// deactivate the host, and with host=scene root that kills the whole world.)</summary>
        public static DragDropTask Create(GameObject host, string title, string[] stepsInCorrectOrder)
        {
            var old = host.GetComponentInChildren<DragDropTask>(true);
            if (old != null) DestroyImmediate(old.gameObject);
            var panelGo = new GameObject("TaskGame_" + title);
            panelGo.transform.SetParent(host.transform, false);
            var t = panelGo.AddComponent<DragDropTask>();
            t._title = title;
            t._steps = stepsInCorrectOrder;
            t.BuildUi();
            t.gameObject.SetActive(false);   // hidden until SPACE at SCHOOL opens it
            return t;
        }

        private void BuildUi()
        {
            var host = gameObject;
            host.name = "TaskGame_" + _title;

            // full panel backdrop
            _backdrop = new GameObject("Backdrop");
            _backdrop.transform.SetParent(host.transform, false);
            var bsr = _backdrop.AddComponent<SpriteRenderer>();
            bsr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4),
                new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
            bsr.color = PanelCol;
            bsr.sortingOrder = 50;
            _backdrop.transform.localScale = new Vector3(9f, 5.2f, 1f);

            // title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(host.transform, false);
            var ttm = titleGo.AddComponent<TextMesh>();
            InteractionManager.StyleText(ttm, _title, 0.30f, 40, TextAnchor.MiddleCenter,
                new Color(1f, 0.8f, 0.3f), 52);
            titleGo.transform.localPosition = new Vector3(0f, 2.1f, 0f);

            // instruction
            var insGo = new GameObject("Instruction");
            insGo.transform.SetParent(host.transform, false);
            var itm = insGo.AddComponent<TextMesh>();
            InteractionManager.StyleText(itm, "Drag the steps into the right order.", 0.16f, 30,
                TextAnchor.MiddleCenter, new Color(0.75f, 0.85f, 0.95f), 52);
            insGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            // slots (correct order), top area
            float slotY = 0.7f;
            for (int i = 0; i < _steps.Length; i++)
            {
                float x = (i - (_steps.Length - 1) * 0.5f) * 2.8f;
                var slotGo = new GameObject("Slot" + i);
                slotGo.transform.SetParent(host.transform, false);
                slotGo.transform.localPosition = new Vector3(x, slotY, 0f);
                var ssr = slotGo.AddComponent<SpriteRenderer>();
                ssr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4),
                    new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
                ssr.color = SlotCol;
                ssr.sortingOrder = 51;
                slotGo.transform.localScale = new Vector3(2.5f, 0.9f, 1f);

                var numGo = new GameObject("Num");
                numGo.transform.SetParent(slotGo.transform, false);
                numGo.transform.localPosition = new Vector3(0f, 0.62f, 0f);
                var ntm = numGo.AddComponent<TextMesh>();
                InteractionManager.StyleText(ntm, (i + 1) + ".", 0.13f, 26, TextAnchor.LowerCenter,
                    Color.white, 52);

                _slots.Add(new Slot { go = slotGo, expected = _steps[i], filled = false, pos = slotGo.transform.position });
            }

            // tiles (shuffled), bottom area
            var order = (string[])_steps.Clone();
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            for (int i = 0; i < order.Length; i++)
            {
                float x = (i - (order.Length - 1) * 0.5f) * 2.8f;
                var tileGo = new GameObject("Tile_" + order[i]);
                tileGo.transform.SetParent(host.transform, false);
                tileGo.transform.localPosition = new Vector3(x, -1.3f, 0f);
                var tsr = tileGo.AddComponent<SpriteRenderer>();
                tsr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4),
                    new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
                tsr.color = TileCol;
                tsr.sortingOrder = 53;
                tileGo.transform.localScale = new Vector3(2.5f, 0.8f, 1f);
                var col = tileGo.AddComponent<BoxCollider2D>();
                col.size = new Vector2(2.5f, 0.8f);

                var txtGo = new GameObject("Txt");
                txtGo.transform.SetParent(tileGo.transform, false);
                txtGo.transform.localPosition = new Vector3(0f, 0f, -0.1f);
                var ttm2 = txtGo.AddComponent<TextMesh>();
                InteractionManager.StyleText(ttm2, order[i], 0.135f, 26, TextAnchor.MiddleCenter,
                    new Color(0.15f, 0.12f, 0.05f), 54);

                _tiles.Add(new Tile { go = tileGo, step = order[i], home = tileGo.transform.position, placed = false });
            }

            // X button (cancel -> FailAndClose, GGD style)
            var xGo = new GameObject("X");
            xGo.transform.SetParent(host.transform, false);
            xGo.transform.localPosition = new Vector3(4.1f, 2.2f, 0f);
            var xtm = xGo.AddComponent<TextMesh>();
            InteractionManager.StyleText(xtm, "X", 0.3f, 40, TextAnchor.MiddleCenter,
                new Color(1f, 0.4f, 0.35f), 53);
            var xcol = xGo.AddComponent<BoxCollider2D>();
            xcol.size = new Vector2(0.9f, 0.9f);
            xGo.AddComponent<TaskCloseButton>().Owner = this;

            host.AddComponent<TaskPanelFollowCamera>();
        }

        public override void Open()
        {
            base.Open();
            ActiveInstance = this;
            // reset: tiles home, slots empty
            foreach (var t in _tiles)
            {
                t.placed = false;
                t.go.transform.position = t.home;
                var sr = t.go.GetComponent<SpriteRenderer>();
                sr.color = TileCol;
                t.go.SetActive(true);
            }
            for (int i = 0; i < _slots.Count; i++)
            {
                var s = _slots[i];
                s.filled = false;
                s.go.GetComponent<SpriteRenderer>().color = SlotCol;
                _slots[i] = s;
            }
        }

        private void Update()
        {
            // drag handling (mouse); depth = camera plane offset so world z stays 0.
            // R6 (GGD GamepadCursor port): the TILE follows the pointer with a lerp,
            // not a hard snap — removes the jittery feel on drag.
            // R5: gated — stray clicks / stale swallows must never grab or drop tiles.
            if (!ModalInputGate.InputAllowed)
            {
                if (_dragging && Input.GetMouseButtonUp(0)) DropTile(_dragTile);   // never stick mid-swing
                return;
            }
            var cam = Camera.main;
            if (cam == null) return;
            var mouseWorld = cam.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, -cam.transform.position.z));

            if (Input.GetMouseButtonDown(0) && !_dragging)
            {
                var hit = Physics2D.OverlapPoint(mouseWorld);
                if (hit != null)
                {
                    foreach (var t in _tiles)
                        if (t.go == hit.gameObject && !t.placed) { _dragTile = t; _dragging = true; }
                }
            }
            if (_dragging && _dragTile != null && Input.GetMouseButton(0))
            {
                var cur = _dragTile.go.transform.position;
                var target = new Vector3(mouseWorld.x, mouseWorld.y, 0f);
                _dragTile.go.transform.position = Vector3.Lerp(cur, target, Mathf.Min(1f, DragSmoothing * Time.deltaTime * 60f));
            }
            if (_dragging && Input.GetMouseButtonUp(0)) DropTile(_dragTile);
        }

        private Tile _dragTile;
        private bool _dragging;

        [Header("GGD GamepadCursor port")]
        /// <summary>Tile chase speed: 1 = instant snap, lower = smoother follow.</summary>
        public float DragSmoothing = 14f;

        private void DropTile(Tile t)
        {
            _dragging = false;
            if (t == null) return;
            var pos = (Vector2)t.go.transform.position;

            int bestIdx = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].filled) continue;
                float d = Vector2.Distance(_slots[i].pos, pos);
                if (d < 1.2f && d < bestD) { bestD = d; bestIdx = i; }
            }

            if (bestIdx >= 0 && _slots[bestIdx].expected == t.step)
            {
                t.placed = true;
                t.go.transform.position = _slots[bestIdx].pos;
                _slots[bestIdx].go.GetComponent<SpriteRenderer>().color = SlotOkCol;
                var s = _slots[bestIdx];
                s.filled = true;
                _slots[bestIdx] = s;

                if (AllPlaced()) CompleteAndClose();   // GGD: CompleteAndClose -> resumes movement
            }
            else
            {
                // bounce back home
                t.go.transform.position = t.home;
            }
        }

        private bool AllPlaced()
        {
            foreach (var t in _tiles) if (!t.placed) return false;
            return true;
        }

        /// <summary>Test hook: programmatically place every tile correctly (same code path as a real drop).</summary>
        public void DebugPlaceAll()
        {
            foreach (var t in _tiles)
            {
                if (t.placed) continue;
                for (int i = 0; i < _slots.Count; i++)
                {
                    if (_slots[i].filled || _slots[i].expected != t.step) continue;
                    t.placed = true;
                    t.go.transform.position = _slots[i].pos;
                    var sr = _slots[i].go.GetComponent<SpriteRenderer>();
                    sr.color = SlotOkCol;
                    var tmp = _slots[i]; tmp.filled = true; _slots[i] = tmp;
                    break;
                }
            }
            if (AllPlaced()) CompleteAndClose();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (ActiveInstance == this) ActiveInstance = null;
        }
    }

    /// <summary>The X on the minigame panel (GGD FailAndClose path).</summary>
    public class TaskCloseButton : MonoBehaviour
    {
        public TaskPanel Owner;
        private void OnMouseDown() { if (Owner != null) Owner.FailAndClose(); }
    }

    /// <summary>Keeps the panel in front of the camera (same trick as InteractionManager.PositionPanel).</summary>
    public class TaskPanelFollowCamera : MonoBehaviour
    {
        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y,
                transform.position.z);
        }
    }
}
