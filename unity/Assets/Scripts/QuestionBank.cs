using System.Collections.Generic;
using UnityEngine;

namespace SafeStreets
{
    /// <summary>A question shown inside a quiz zone. Sourced from the CUTR Resource Guide.</summary>
    [System.Serializable]
    public class QuizQuestion
    {
        public string text;
        public string[] choices;
        public int correct;
        public string source;
    }

    /// <summary>A physical place in the world that teaches one safety lesson.</summary>
    [System.Serializable]
    public class QuizZoneDef
    {
        public string zoneId;      // e.g. "zebra"
        public string title;       // shown on the card
        public string activityId;  // backend activity to POST ("" = local only)
        public QuizQuestion[] questions;
    }

    public static class QuestionBank
    {
        // Zebra zone mirrors the web app's le-crossing activity (same 5 questions,
        // same order) so a submitted run grades correctly against the backend.
        public static readonly QuizZoneDef Zebra = new QuizZoneDef
        {
            zoneId = "zebra",
            title = "CROSSING THE STREET",
            activityId = "le-crossing",
            questions = new[]
            {
                new QuizQuestion { text = "Where do you stop before you cross the street?",
                    choices = new[] { "At the curb", "In the middle of the road", "On the grass far away", "Anywhere I like" },
                    correct = 0, source = "Guide, PreK-Gr1: stop at the curb; hold an adult's hand" },
                new QuizQuestion { text = "Who helps you cross the road?",
                    choices = new[] { "A grown-up", "Nobody", "My pet", "A stranger" },
                    correct = 0, source = "Guide, PreK-Gr1: adult-led crossing" },
                new QuizQuestion { text = "What do you do before crossing?",
                    choices = new[] { "Stop, look, and listen", "Run really fast", "Close my eyes", "Look at my shoes" },
                    correct = 0, source = "Guide, PreK-Gr1: stop, look, listen, wait for an adult" },
                new QuizQuestion { text = "Your ball rolls into the street. What do you do?",
                    choices = new[] { "Tell a grown-up", "Chase it quickly", "Wait for cars and grab it", "Throw my hat at it" },
                    correct = 0, source = "Guide, PreK-Gr1: never chase a ball into the street" },
                new QuizQuestion { text = "When is it okay to walk in a parking lot?",
                    choices = new[] { "When a grown-up stays close", "Always, cars watch for kids", "When it is dark", "When I run fast" },
                    correct = 0, source = "Guide, PreK-Gr1: keep an adult close in parking lots" },
            },
        };

        public static readonly QuizZoneDef School = new QuizZoneDef
        {
            zoneId = "school",
            title = "SCHOOL ZONE SMARTS",
            activityId = "",
            questions = new[]
            {
                new QuizQuestion { text = "Where should you cross the street near school?",
                    choices = new[] { "At the crosswalk or crossing guard", "Anywhere it is shorter", "Between parked cars", "Around the corner" },
                    correct = 0, source = "Guide, Gr2-3: use a designated crossing; follow guard instructions" },
                new QuizQuestion { text = "Before crossing, what should you watch for?",
                    choices = new[] { "Cars that are turning or backing up", "Only fast cars", "Only red cars", "Nothing special" },
                    correct = 0, source = "Guide, Gr2-3: turning vehicles, driveway traffic, backing vehicles" },
            },
        };

        public static readonly QuizZoneDef Park = new QuizZoneDef
        {
            zoneId = "park",
            title = "BIKE & HELMET CHECK",
            activityId = "",
            questions = new[]
            {
                new QuizQuestion { text = "How should your bike helmet fit?",
                    choices = new[] { "Level on your head with snug straps", "Tilted back to look cool", "Loose so it is comfy", "Backwards" },
                    correct = 0, source = "Guide, Gr4-5: helmet level, straps form a V, chin strap snug" },
                new QuizQuestion { text = "Why wear bright colors when walking or biking?",
                    choices = new[] { "So drivers can see you", "To look nice", "For good luck", "To match your backpack" },
                    correct = 0, source = "Guide, Gr4-5: increase visibility in low light" },
            },
        };

        public static readonly QuizZoneDef Home = new QuizZoneDef
        {
            zoneId = "home",
            title = "SAFE ROUTES HOME",
            activityId = "",
            questions = new[]
            {
                new QuizQuestion { text = "You dropped something near the school bus. What do you do?",
                    choices = new[] { "Tell the bus driver", "Quickly grab it", "Ask a friend to get it", "Come back for it later" },
                    correct = 0, source = "Guide, Gr2-3: tell the driver instead of retrieving it" },
                new QuizQuestion { text = "Walking home, the light turns red mid-crossing. You...",
                    choices = new[] { "Keep going to finish crossing, then wait", "Run back the way I came", "Stop in the middle and play", "Cross diagonally" },
                    correct = 0, source = "Guide, Gr4-5: continue scanning; finish crossing safely, then wait" },
            },
        };

        public static readonly QuizZoneDef Shop = new QuizZoneDef
        {
            zoneId = "shop",
            title = "PARKING LOT WATCH",
            activityId = "",
            questions = new[]
            {
                new QuizQuestion { text = "A car is backing out of a parking space. You...",
                    choices = new[] { "Stop and wait where the driver can see me", "Sprint behind it", "Bang on the trunk", "Duck under the car" },
                    correct = 0, source = "Guide, Gr4-5: recognize backing vehicles; choose spots where drivers can see you" },
                new QuizQuestion { text = "The safest place to walk in a parking lot is...",
                    choices = new[] { "The sidewalk or marked walkway", "Between parked cars", "The driving lane", "Behind big trucks" },
                    correct = 0, source = "Guide, Gr4-5: do not dart between parked cars" },
            },
        };

        public static readonly QuizZoneDef[] All = { Zebra, School, Park, Home, Shop };

        public static QuizZoneDef ByZoneId(string id)
        {
            foreach (var z in All) if (z.zoneId == id) return z;
            return null;
        }
    }
}
