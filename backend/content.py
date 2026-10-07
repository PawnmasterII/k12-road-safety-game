"""Static content: modules from the CUTR proposal table, activities from the
K-12 Traffic Safety Education Resource Guide (every question cites its source)."""

MODULES = [
    {
        "id": "little-explorers",
        "name": "Little Explorers",
        "grade_band": "K-2",
        "grades": ["K", "1", "2"],
        "topics": ["Walking", "Crossing", "School bus", "Parking lot safety"],
        "activity_id": "le-crossing",
        "blurb": "Learn to stop, look, and cross with a grown-up!",
    },
    {
        "id": "safety-heroes",
        "name": "Safety Heroes",
        "grade_band": "3-5",
        "grades": ["3", "4", "5"],
        "topics": ["School zone", "Bike safety", "Visibility", "Safe routes"],
        "activity_id": "sh-school-bike",
        "blurb": "Spot dangers near school and gear up like a hero.",
    },
    {
        "id": "smart-travelers",
        "name": "Smart Travelers",
        "grade_band": "6-8",
        "grades": ["6", "7", "8"],
        "topics": ["Bike", "E-scooter", "E-bike", "Transit", "Distracted walking"],
        "activity_id": "st-riding",
        "blurb": "Ride smarter: bikes, e-bikes, and keeping your eyes up.",
    },
    {
        "id": "road-ready-teens",
        "name": "Road Ready Teens",
        "grade_band": "9-12",
        "grades": ["9", "10", "11", "12"],
        "topics": ["Teen driving", "Passenger safety", "Florida rules",
                   "E-scooter & e-bike interactions", "Crash prevention"],
        "activity_id": "rr-florida",
        "blurb": "Florida road rules, passenger smarts, and risk radar.",
    },
]

ACTIVITIES = {
    "le-crossing": {
        "id": "le-crossing",
        "module_id": "little-explorers",
        "title": "Crossing with a Grown-Up",
        "questions": [
            {
                "id": "le-q1",
                "text": "Where do you stop before you cross the street?",
                "choices": ["At the curb", "In the middle of the road",
                            "On the grass far away", "Anywhere I like"],
                "correct_index": 0,
                "source": "Guide, Preschool-Grade 1: stop at the curb; hold an adult's hand",
            },
            {
                "id": "le-q2",
                "text": "Who helps you cross the road?",
                "choices": ["A grown-up", "Nobody", "My pet", "A stranger"],
                "correct_index": 0,
                "source": "Guide, Preschool-Grade 1: adult-led crossing",
            },
            {
                "id": "le-q3",
                "text": "What do you do before crossing?",
                "choices": ["Stop, look, and listen", "Run really fast",
                            "Close my eyes", "Look at my shoes"],
                "correct_index": 0,
                "source": "Guide, Preschool-Grade 1: stop, look, listen, wait for an adult, cross together",
            },
            {
                "id": "le-q4",
                "text": "Your ball rolls into the street. What do you do?",
                "choices": ["Tell a grown-up", "Chase it quickly",
                            "Wait for cars and grab it", "Throw my hat at it"],
                "correct_index": 0,
                "source": "Guide, Preschool-Grade 1: children do not chase a ball or retrieve an item",
            },
            {
                "id": "le-q5",
                "text": "When is it okay to walk in a parking lot?",
                "choices": ["When a grown-up stays close", "Always, cars watch for kids",
                            "When it is dark", "When I run fast"],
                "correct_index": 0,
                "source": "Guide, Preschool-Grade 1: keep an adult close in parking lots; parking areas are not play spaces",
            },
        ],
    },
    "sh-school-bike": {
        "id": "sh-school-bike",
        "module_id": "safety-heroes",
        "title": "School Zone + Bike Gear Check",
        "questions": [
            {
                "id": "sh-q1",
                "text": "Where should you cross the street near school?",
                "choices": ["At the crosswalk or crossing guard", "Anywhere it is shorter",
                            "Between parked cars", "Around the corner"],
                "correct_index": 0,
                "source": "Guide, Grades 2-3: use a designated crossing; follow guard instructions",
            },
            {
                "id": "sh-q2",
                "text": "Before crossing, what should you watch for?",
                "choices": ["Cars that are turning or backing up", "Only fast cars",
                            "Only red cars", "Nothing special"],
                "correct_index": 0,
                "source": "Guide, Grades 2-3: look for turning vehicles, driveway traffic, backing vehicles",
            },
            {
                "id": "sh-q3",
                "text": "How should your bike helmet fit?",
                "choices": ["Level on your head with snug straps", "Tilted back to look cool",
                            "Loose so it is comfy", "Backwards"],
                "correct_index": 0,
                "source": "Guide, Grades 4-5: helmet level, two finger-widths above eyebrows, straps form a V, chin strap snug",
            },
            {
                "id": "sh-q4",
                "text": "Why wear bright colors or reflectors when walking or biking?",
                "choices": ["So drivers can see you", "To look nice",
                            "For good luck", "To match your backpack"],
                "correct_index": 0,
                "source": "Guide, Grades 4-5: increase visibility in low light; visibility topic",
            },
            {
                "id": "sh-q5",
                "text": "You drop something near the school bus. What do you do?",
                "choices": ["Tell the bus driver", "Quickly grab it",
                            "Ask a friend to get it", "Come back for it later"],
                "correct_index": 0,
                "source": "Guide, Grades 2-3: if an item falls near the bus, tell the driver instead of retrieving it",
            },
        ],
    },
    "st-riding": {
        "id": "st-riding",
        "module_id": "smart-travelers",
        "title": "Riding Ready: Bikes, E-bikes, Phones",
        "questions": [
            {
                "id": "st-q1",
                "text": "Florida law: who must wear a helmet when riding a bicycle?",
                "choices": ["Riders under 16", "Only racers", "Nobody", "Only adults"],
                "correct_index": 0,
                "source": "Guide, Florida legal checks: F.S. 316.2065 requires riders and passengers under 16 to wear a helmet",
            },
            {
                "id": "st-q2",
                "text": "You are about to turn on your bike. What comes first?",
                "choices": ["Look over your shoulder and signal", "Turn quickly",
                            "Speed up", "Yell 'turning!' and turn"],
                "correct_index": 0,
                "source": "Guide, Grades 6-8: scanning before turns, hand signals, predictable riding",
            },
            {
                "id": "st-q3",
                "text": "An e-bike is faster and heavier than a regular bike. What does that mean?",
                "choices": ["You need more distance to stop", "Nothing changes",
                            "You can skip looking behind", "Helmets are not needed"],
                "correct_index": 0,
                "source": "Guide, Grades 6-8: e-bike speed and handling - allow more stopping distance",
            },
            {
                "id": "st-q4",
                "text": "Your phone buzzes while you are riding. What do you do?",
                "choices": ["Stop in a safe place before checking", "Check it while riding",
                            "Answer with one hand", "Have a friend read it while riding"],
                "correct_index": 0,
                "source": "Guide, Grades 6-8: stop in a safe place before checking navigation or messages",
            },
            {
                "id": "st-q5",
                "text": "Where might a driver NOT see you, even in daylight?",
                "choices": ["In the driver's blind spot or right beside parked cars (door zone)",
                            "Only at night", "Only on highways", "Nowhere - drivers always see bikes"],
                "correct_index": 0,
                "source": "Guide, Grades 6-8: door zone, large-vehicle blind spots, 'I can see, I can be seen'",
            },
        ],
    },
    "rr-florida": {
        "id": "rr-florida",
        "module_id": "road-ready-teens",
        "title": "Florida Rules & Risk Radar",
        "questions": [
            {
                "id": "rr-q1",
                "text": "Florida requires how many supervised driving hours (including 10 at night) before a teen license?",
                "choices": ["50 hours", "10 hours", "100 hours", "5 hours"],
                "correct_index": 0,
                "source": "Guide, Grades 9-12: FLHSMV lists 50 hours of supervised driving, including 10 at night",
            },
            {
                "id": "rr-q2",
                "text": "A 16-year-old licensed driver in Florida generally may drive between...",
                "choices": ["6 a.m. and 11 p.m.", "Any time", "9 a.m. and 5 p.m.", "Midnight and 6 a.m."],
                "correct_index": 0,
                "source": "Guide, Grades 9-12: 16-year-old license generally carries a 6 a.m.-11 p.m. limit",
            },
            {
                "id": "rr-q3",
                "text": "Florida's 3-foot rule applies when a driver is...",
                "choices": ["Passing a bicyclist", "Passing a school", "Changing lanes", "Parking"],
                "correct_index": 0,
                "source": "Guide, Task 3 of proposal: selected Florida rules such as the 3-foot passing rule",
            },
            {
                "id": "rr-q4",
                "text": "Best habit before you start the car?",
                "choices": ["Set navigation and stow the phone", "Check texts quickly",
                            "Grab a snack", "Turn the music up first"],
                "correct_index": 0,
                "source": "Guide, Grades 9-12: set navigation before moving; silence and stow the phone",
            },
            {
                "id": "rr-q5",
                "text": "Why is night driving with a car full of friends risky for a new driver?",
                "choices": ["Inexperience, passengers, and night driving all raise risk",
                            "Friends help the driver focus", "New drivers drive best at night",
                            "Passengers can hold the wheel"],
                "correct_index": 0,
                "source": "Guide, Grades 9-12: limited experience combined with peer passengers and night driving can increase risk",
            },
        ],
    },
}


def module_by_id(module_id: str):
    return next((m for m in MODULES if m["id"] == module_id), None)


def activity_by_id(activity_id: str):
    return ACTIVITIES.get(activity_id)


def module_for_grade(grade: str):
    return next((m for m in MODULES if grade in m["grades"]), None)
