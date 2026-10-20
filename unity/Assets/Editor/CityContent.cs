using UnityEngine;

namespace SafeStreets.EditorTools
{
    /// <summary>
    /// Post-game content + the city cast's conversation scripts (demo polish).
    ///
    /// WHY THIS EXISTS: after the map moved to docs/map_design/city_layout.json the six
    /// city NPCs were built with NO conversation and no CrossingNPC, so every SPACE near
    /// them fell through the router to whatever building was underfoot — the "NPCs are
    /// mute" gap on the demo path. The lines live here (not in the JSON) so the map stays
    /// pure geometry and the dialogue is one reviewable, editable table.
    ///
    /// Three of the six are ESCORT partners (CrossingNPC, cross "to the zebra, beyond
    /// ~28" so they are outside the painted stripes and can be walked across) and three
    /// are TALK NPCs (DialogNPC, static, give the rules for their spot).
    /// </summary>
    public static class CityContent
    {
        /// <summary>Conversation + role for one NPC name from city_layout.json.</summary>
        public sealed class Cast
        {
            public string name;
            public bool escort;          // true => CrossingNPC, false => DialogNPC
            public Conversation conv;    // DialogNPC script (null for escorts: CrossingNPC owns its lines)
            public int escortOrder;      // escorts only: 0..QuestManager.required-1, or -1 = bonus scenery
        }

        private const string GradeTip = "Rules from the K-12 Traffic Safety Education Resource Guide";

        /// <summary>Every city_layout.json NPC name, in JSON order, with its script.</summary>
        public static Cast[] All()
        {
            return new[]
            {
                // Crossing guard at the school crossing: TELLS the crossing rules.
                new Cast
                {
                    name = "Guard Gus", escort = false,
                    conv = Conv("Guard Gus",
                        "Morning! I'm Gus, the crossing guard for this school crossing.",
                        "My job is easy to copy: STOP at the curb, LOOK left-right-left, LISTEN, then cross when it is clear.",
                        "If I hold my paddle up, that means STOP - even if you do not see a car.",
                        "Want to practise? Little Sam is waiting by the park path. Walk him across the stripes for me."),
                },

                // Bus driver: TELLS the bus-stop rules.
                new Cast
                {
                    name = "Driver Dee", escort = false,
                    conv = Conv("Driver Dee",
                        "Hi there! I drive the school bus. Name's Dee.",
                        "Stand two big steps back from the curb while you wait - I need room to pull in and see you.",
                        "Wait until I stop AND wave you forward before you walk to the door.",
                        "And if you drop something near my bus, tell me. Never crawl underneath for it. That is the one rule I never bend."),
                },

                // Town guide / quest giver: the briefing + replayable quiz tip.
                new Cast
                {
                    name = "Officer Ada", escort = false,
                    conv = Conv("Officer Ada",
                        "Welcome to Safe Travelers, recruit! I'm Officer Ada, your town guide.",
                        "Three neighbours need a walking buddy to get across these streets: Granny Rose by the grocery, Little Sam by the park path, and Mia down by the library.",
                        "Walk up to them, press SPACE to talk, then stay close while you both cross on the white stripes.",
                        "School at the top left, library at the top right, rail crossing on the far west edge - and a quiz zone at each spot. Good luck!"),
                },

                // 1) Grocery run: parking-lot rules.
                new Cast
                {
                    name = "Granny Rose", escort = true, escortOrder = 0,
                },

                // 2) Park path: shared-use path + road crossing.
                new Cast
                {
                    name = "Little Sam", escort = true, escortOrder = 1,
                },

                // 3) Library: sidewalk + one-way street look.
                new Cast
                {
                    name = "Mia", escort = true, escortOrder = 2,
                },
            };
        }

        // ---- CrossingNPC companion lines ----
        // Each escort states WHY they are out and WHAT the lesson is; the closing tip is
        // the "do it now" instruction (DialogManager shows it as its own screen).

        /// Lines for an escort, in the shape CrossingNPC.BuildLines expects: one array of
        /// spoken lines + the "do it now" closing tip (DialogManager shows it as its own
        /// screen). Kept here so all six scripts are reviewable in one place.
        public static void EscortScript(string npcName, out string[] lines, out string closingTip)
        {
            switch (npcName)
            {
                case "Granny Rose":
                    lines = new[]
                    {
                        "Oh, hello dear. I'm Rose - I need to get across to the grocery store.",
                        "My eyes aren't what they were. Hold on, will you walk over with me?",
                        "Parking lots are the worst part: cars back out and nobody looks for a little old lady.",
                    };
                    closingTip = "Stay on the stripes. And if a car has white backup lights on, we stop and wait.";
                    return;
                case "Little Sam":
                    lines = new[]
                    {
                        "Hi! I'm Sam. I'm trying to get to the park for the shared-use path.",
                        "The park path is for bikes too, so I always walk on my own side.",
                        "But I still have to cross this street to get there. Can you come with me?",
                    };
                    closingTip = "Stop at the curb, look left-right-left, listen - then we cross together.";
                    return;
                case "Mia":
                    lines = new[]
                    {
                        "Hey - I'm Mia. Library books are due today and I can't reach the crossing on my own.",
                        "The library is up this one-way street, so cars can come from a side you never expect.",
                        "Walk with me? I'll stay beside you the whole way.",
                    };
                    closingTip = "Look both ways anyway - on a one-way street, cars can still come from either side.";
                    return;
                default:
                    lines = new[] { $"Hi! I'm {npcName} and I need to cross this road." };
                    closingTip = "Walk onto the white stripes and cross to the other side together!";
                    return;
            }
        }

        /// <summary>ScriptableObject from lines (same shape SceneGeo.Conv builds).</summary>
        public static Conversation Conv(string speaker, params string[] lines)
        {
            var c = ScriptableObject.CreateInstance<Conversation>();
            c.speakerName = speaker;
            c.lines = lines;
            c.closingTip = GradeTip;
            return c;
        }
    }
}
