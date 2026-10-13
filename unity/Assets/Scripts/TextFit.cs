using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// R9 — the ONE text-fitting helper every popup text goes through.
    /// TextMesh has no auto-layout: no wrapping, no overflow protection — long
    /// strings just spill out of their panel. This module owns all of it:
    ///
    ///   1. LINE BREAKING (WrapToWidth): break only at spaces, never mid-word;
    ///      respect the author's own newlines; a single word wider than the
    ///      whole box is hard-split across lines. The "when does a line end"
    ///      rule is the BOX WIDTH in world units, measured with the font's real
    ///      glyph advances — not a hand-tuned character count.
    ///   2. DYNAMIC SCALE (Apply): after wrapping, the whole block is measured
    ///      and the transform is scaled DOWN (never up) until width and height
    ///      fit the box. Longer content = smaller text, never overflow.
    ///
    /// Scaling happens around the TextMesh pivot, so the anchor decides where
    /// the block hugs: MiddleCenter stays centered, UpperLeft stays top-left,
    /// MiddleLeft keeps its left edge. PanelAnimator only touches the panel
    /// ROOT scale (its fade pop), never these child transforms.
    /// </summary>
    public static class TextFit
    {
        /// <summary>Fraction of the box width a line may actually occupy (breathing room).</summary>
        public const float WidthMargin = 0.96f;

        /// <summary>Vertical line pitch in em (Liberation Sans natural ~1.16, + margin).</summary>
        public const float LinePitch = 1.25f;

        /// <summary>
        /// Wrap + fit: sets tm.text to the word-wrapped text and scales the
        /// transform so the block never leaves the boxW x boxH world-unit
        /// rectangle centered on the mesh's anchor. Call AFTER styling (font,
        /// size, characterSize must be final) and after the final position is
        /// set (Apply touches ONLY localScale, never position/anchor).
        ///
        /// R10b: parent-scale aware. Under a SCALED parent (task tiles: sprite
        /// scaled 0.625x0.2), localScale = s/parentLossy makes the WORLD scale
        /// uniform s — so every size constraint is expressed in measured units
        /// directly (world = measured * s; the parent's scale cancels). The
        /// parent scale appears ONLY in the final localScale division, which
        /// also renders the block UNDISTORTED.
        /// </summary>
        public static void Apply(TextMesh tm, string text, float boxW, float boxH)
        {
            if (tm == null) return;
            var px = 1f; var py = 1f;
            if (tm.transform.parent != null)
            {
                var pl = tm.transform.parent.lossyScale;
                px = Mathf.Max(Mathf.Abs(pl.x), 0.0001f);
                py = Mathf.Max(Mathf.Abs(pl.y), 0.0001f);
            }
            tm.transform.localScale = Vector3.one;          // measure unscaled
            if (string.IsNullOrEmpty(text)) { tm.text = string.Empty; return; }

            // wrap in measured units — at world scale s=1 a line of L units
            // renders exactly L world units wide, so the budget is the box itself
            tm.text = WrapToWidth(tm, text, boxW * WidthMargin);

            // TextMesh world metrics = glyphPixels * characterSize / 10
            // (fontSize-independent). The original adv/fontSize*characterSize
            // under-measured by exactly fontSize/10 (verified: 5.42 predicted
            // vs 17.33 actual renderer bounds) — which is precisely why the
            // char-count era clipped the TIP line.
            float em = tm.fontSize / 10f * tm.characterSize;
            float w = BlockWidth(tm, tm.text);
            float h = LineCount(tm.text) * LinePitch * em;
            float s = Mathf.Min(
                boxW * WidthMargin / Mathf.Max(w, 0.0001f),
                boxH / Mathf.Max(h, 0.0001f));
            s = Mathf.Clamp(s, 0f, 1f);                     // shrink-only: never blow small text up
            // uniform WORLD scale s: divide out the parent's (possibly non-uniform) scale
            tm.transform.localScale = new Vector3(s / px, s / py, 1f);
        }

        /// <summary>
        /// Greedy word-wrap to a world-unit width: emit a line the moment the
        /// next chunk would cross the box edge. Newlines in the source are
        /// honored; words wider than the box are split at glyph level.
        /// </summary>
        public static string WrapToWidth(TextMesh tm, string text, float boxWidth)
        {
            float maxW = boxWidth * WidthMargin;
            var sb = new System.Text.StringBuilder();
            bool firstOut = true;

            void Emit(string s)
            {
                if (!firstOut) sb.Append('\n');
                sb.Append(s);
                firstOut = false;
            }

            foreach (var paragraph in text.Split('\n'))
            {
                if (paragraph.Length == 0) { Emit(""); continue; }   // blank line preserved
                string line = null;
                foreach (var rawWord in paragraph.Split(' '))
                {
                    if (rawWord.Length == 0) continue;               // collapse doubled spaces
                    foreach (var chunk in Chunks(tm, rawWord, maxW))
                    {
                        if (line == null) { line = chunk; continue; }
                        var candidate = line + " " + chunk;
                        if (LineWidth(tm, candidate) <= maxW) line = candidate;
                        else { Emit(line); line = chunk; }           // END LINE, go down
                    }
                }
                if (line != null) Emit(line);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Rendered width of one line in world units. TextMesh world metrics:
        /// glyph advance (pixels at the requested fontSize) * characterSize / 10
        /// — fontSize-independent. Verified against MeshRenderer.bounds.
        /// </summary>
        public static float LineWidth(TextMesh tm, string s)
        {
            var font = tm.font;
            if (font == null || string.IsNullOrEmpty(s)) return 0f;
            font.RequestCharactersInTexture(s, tm.fontSize);         // rasterize even when never rendered
            float adv = 0f;
            CharacterInfo ci;
            foreach (var c in s)
            {
                if (font.GetCharacterInfo(c, out ci, tm.fontSize)) adv += ci.advance;
                else adv += tm.fontSize * 0.55f;                     // atlas miss: average-glyph fallback
            }
            return adv * tm.characterSize / 10f;
        }

        /// <summary>Widest line of a (possibly multi-line) block.</summary>
        public static float BlockWidth(TextMesh tm, string block)
        {
            float w = 0f;
            foreach (var line in block.Split('\n')) w = Mathf.Max(w, LineWidth(tm, line));
            return w;
        }

        public static int LineCount(string block)
        {
            int n = 1;
            foreach (var c in block) if (c == '\n') n++;
            return n;
        }

        /// <summary>A word that alone exceeds maxW is split at glyph level; others pass through.</summary>
        private static System.Collections.Generic.IEnumerable<string> Chunks(TextMesh tm, string word, float maxW)
        {
            if (LineWidth(tm, word) <= maxW) { yield return word; yield break; }
            string cur = "";
            foreach (var ch in word)
            {
                var next = cur + ch;
                if (cur.Length > 0 && LineWidth(tm, next) > maxW) { yield return cur; cur = ch.ToString(); }
                else cur = next;
            }
            if (cur.Length > 0) yield return cur;
        }
    }
}
