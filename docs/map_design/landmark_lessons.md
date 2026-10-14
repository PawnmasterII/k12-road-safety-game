# SafeStreets City Map — Landmark Lessons (Pedagogy Layer)

Source of truth for quiz content: `backend/content.py` (K-12 Traffic Safety Education
Resource Guide citations). Existing banks: `le-crossing` (K-2 crossing/parking),
`sh-school-bike` (3-5 school zone/bike/visibility/bus), `st-riding` (6-8 bike/e-bike/
distraction/blind spots), `rr-florida` (9-12 GDL/night/3-foot/phone-stow). New bank ids
proposed below are prefixed to avoid collision. Grade bands in the table use the
map-facing bands K-1 / 2-3 / 4-5 / 6-8 / 9-12.

Hook key: **quiz zone** = walk-up QuizBank popup · **NPC escort** = GGD-style escort
(CrossingNPC state machine, spec 006) · **task minigame** = DragDropTask panel
(spec 004) · **sign-read stop** = read-the-sign quiz prompt at a placed sign/signal.

## 1. Landmark / element table

| Landmark / Element | Safety lesson (1 line) | Interactive hook | Grade band | Suggested bank |
|---|---|---|---|---|
| SCHOOL (school zone + crossing guard) | Cross only at the marked crosswalk and obey the guard's stop paddle. | NPC escort + quiz zone | 2-3 | sh-school-bike |
| SCHOOL drop-off loop | Drivers and walkers make eye contact before anyone steps into the loop lane. | sign-read stop | 2-3 | sh-school-bike |
| PARK paths (shared-use) | Walkers keep right, bikes pass on the left with a bell/voice warning. | quiz zone | 4-5 | st-riding |
| PARK pond bridge | Stop, look, listen at path-road junctions; never chase a ball into the road. | NPC escort | K-1 | le-crossing |
| LIBRARY front steps | Get off and walk a bike/scooter on crowded sidewalks near entrances. | sign-read stop | 4-5 | st-riding |
| HOSPITAL entrance / ambulance bay | Never block or cross an active emergency driveway; yield to sirens and flashing lights. | sign-read stop | 6-8 | NEW signals-signs |
| FIRE STATION apron | Keep the station apron clear; stop for apparatus pulling out with lights/sirens. | quiz zone | 4-5 | NEW signals-signs |
| POLICE station lobby | A trusted-uniformed officer is a safe person to ask for help crossing or when lost. | NPC escort | K-1 | le-crossing |
| GROCERY storefront sidewalk | Walk bikes/carts on the sidewalk; watch for cars backing out of angled stalls. | task minigame | 2-3 | NEW parkinglot-safety |
| COMMUNITY PLAZA | Eyes up, phone away in crowded shared spaces; set a meeting point with your grown-up. | quiz zone | 2-3 | le-crossing |
| GAS STATION forecourt | No walking through fuel lanes; drivers have blind spots around pumps and pillars. | quiz zone | 4-5 | NEW driveway-blindspot |
| PARKING LOT (marked stalls + aisles) | Walk with a grown-up, use aisles not between cars, never play between parked cars. | task minigame | K-1 | NEW parkinglot-safety |
| HOUSES row driveways | Stop at every driveway edge; look for reversing lights and turning wheels. | sign-read stop | 2-3 | NEW driveway-blindspot |
| BUS STOP (avenue, with shelter) | Wait on the sidewalk, stand back from the curb, and wait for the bus to fully stop. | NPC escort + quiz zone | 2-3 | NEW bus-stop-rules |
| BUS STOP danger zone | If you drop something near the bus, tell the driver — never bend down to grab it. | task minigame | 2-3 | sh-school-bike |
| Avenue BIKE LANE | Ride with traffic in the lane, signal and shoulder-check before any merge or turn. | quiz zone | 6-8 | st-riding |
| Signalized intersection (walk signal) | Cross only on the WALK symbol after checking turning cars still stop. | task minigame | 4-5 | NEW signals-signs |
| 4-way-stop intersection | First to stop goes first; eye-contact and hand waves before stepping out. | sign-read stop | 4-5 | NEW signals-signs |
| 2-way-stop side street | The stopped street yields to all through traffic; pedestrians use the crosswalk leg. | quiz zone | 6-8 | NEW signals-signs |
| Roundabout (single-lane) | Cross at the marked crosswalk one car-length back from the circle; never cross the circle itself. | quiz zone | 6-8 | NEW signals-signs |
| One-way street pair | Look the correct one way (left on a leftward one-way) before stepping off the curb. | sign-read stop | 2-3 | NEW signals-signs |
| RAILROAD CROSSING (gates + flashers) | Never cross when lights flash or gates fall; if stuck on the tracks, get out and call the number on the crossbuck. | quiz zone | 4-5 | NEW railroad-crossing |
| E-bike rental kiosk (plaza/avenue) | Faster + heavier means longer stopping distance; helmet on before unlock. | quiz zone | 6-8 | st-riding |
| Night-lit avenue segment (9-12 overlay) | Bright clothes/reflectors at night; FL teen GDL night limits and passenger risk. | quiz zone | 9-12 | rr-florida |
| Teen driver corridor (arterial edge) | 3-foot passing, phone stowed + nav set before moving, 50 supervised hours incl. 10 at night. | quiz zone | 9-12 | rr-florida |

## 2. New quiz banks needed (max 5)

Bank ids double as Unity `QuizBank.activityId` values and `ACTIVITIES` keys in `content.py`.
Question style mirrors existing banks: short prompt, 3–4 choices, correct_index 0,
`source` = "Guide, <band>: <rule>".

### NEW `parkinglot-safety` (grades K-5; primary hook: PARKING LOT + GROCERY)

- Q1. "Where do you walk in a parking lot?" — choices: ["In the aisle where drivers can see you, with a grown-up", "Between parked cars to be faster", "Behind cars that are backing up", "Anywhere — cars always stop"] — correct 0 — citation: `Guide, Preschool-Grade 1: keep an adult close in parking lots; parking areas are not play spaces`.
- Q2. "A car has white backup lights on. What do you do?" — choices: ["Stop and wait until it leaves or the driver waves you past", "Run behind it quickly", "Tap the car so it stops", "Keep walking — it sees you"] — correct 0 — citation: `Guide, Grades 2-3: look for turning vehicles, driveway traffic, backing vehicles`.
- Q3. "Your toy rolls under a parked car. What do you do?" — choices: ["Tell a grown-up instead of crawling under", "Crawl under and grab it fast", "Ask a stranger to get it", "Kick the car until it rolls out"] — correct 0 — citation: `Guide, Preschool-Grade 1: children do not chase a ball or retrieve an item`.

### NEW `bus-stop-rules` (grades 2-5; primary hook: BUS STOPS)

- Q1. "Where do you wait for the bus?" — choices: ["On the sidewalk, two big steps back from the curb", "Right at the curb edge", "In the street so the driver sees you", "Behind the shelter where nobody sees you"] — correct 0 — citation: `Guide, Grades 2-3: wait on the sidewalk away from the curb until the bus fully stops`.
- Q2. "When is it safe to walk to the bus door?" — choices: ["After the bus fully stops and the driver signals you", "While it is still rolling up", "As soon as you see it coming", "When your friend runs first"] — correct 0 — citation: `Guide, Grades 2-3: board only after the bus stops and the driver signals`.
- Q3. "You drop your backpack in the bus danger zone. What do you do?" — choices: ["Tell the bus driver instead of picking it up", "Dive under the bus to grab it", "Have a friend crawl in for it", "Wait under the bus until it leaves"] — correct 0 — citation: `Guide, Grades 2-3: if an item falls near the bus, tell the driver instead of retrieving it`.

### NEW `signals-signs` (grades 4-8; primary hooks: signalized / 4-way / 2-way / roundabout / hospital / fire station / one-way)

- Q1. "The WALK sign just turned on. What do you do first?" — choices: ["Still look both ways for turning cars before stepping out", "Run without looking — the sign protects you", "Look at your phone while crossing", "Cross diagonally to save time"] — correct 0 — citation: `Guide, Grades 4-5: obey pedestrian signals and still check for turning vehicles`.
- Q2. "At a 4-way stop, when may you cross in front of a stopped car?" — choices: ["After eye contact or a wave showing the driver sees you", "The instant the car stops", "While the car is still rolling", "Only if no driver is inside"] — correct 0 — citation: `Guide, Grades 4-5: make eye contact with drivers before crossing`.
- Q3. "Where do you cross near a roundabout?" — choices: ["At the marked crosswalk set back from the circle", "Straight across the middle of the circle", "Between cars waiting to enter", "Anywhere — cars go slow there"] — correct 0 — citation: `Guide, Grades 6-8: use designated crossings at roundabouts; never cross the circulatory roadway`.

### NEW `railroad-crossing` (grades 4-8; primary hook: RAILROAD CROSSING)

- Q1. "The crossing lights are flashing but the gate is still going down. What do you do?" — choices: ["Stop and wait — never cross when lights flash", "Hurry across before the gate falls", "Duck under the gate quickly", "Cross if no train is visible yet"] — correct 0 — citation: `Guide, Grades 4-5: never cross railroad tracks when lights flash or gates are moving`.
- Q2. "Your bike gets stuck on the tracks and a train is coming. What first?" — choices: ["Leave the bike, get off the tracks, then call for help", "Stay and keep pulling the bike", "Lie down between the rails", "Run toward the train waving"] — correct 0 — citation: `Guide, Grades 4-5: if stuck on tracks, leave belongings and get clear immediately`.
- Q3. "Where do you stand while waiting for a train to pass?" — choices: ["Well behind the gate and the stop line", "Right next to the rails to see better", "Between the gate and the tracks", "On the tracks if the gate is up"] — correct 0 — citation: `Guide, Grades 4-5: wait behind gates and stop lines until lights stop and gates fully rise`.

### NEW `driveway-blindspot` (grades 2-6; primary hooks: HOUSES driveways + GAS STATION + door-zone kiosk)

- Q1. "What tells you a parked car might back up over the sidewalk?" — choices: ["White reverse lights and turning back wheels", "The car's color", "Loud music inside", "A clean windshield"] — correct 0 — citation: `Guide, Grades 2-3: look for turning vehicles, driveway traffic, backing vehicles`.
- Q2. "A driver is sitting in a parked car. What should you assume?" — choices: ["They might not see you — walk where they can see you or wait", "They always see you", "It is safe to walk right behind the car", "Hood up means go"] — correct 0 — citation: `Guide, Grades 6-8: door zone, large-vehicle blind spots, 'I can see, I can be seen'`.
- Q3. "Riding past parked cars, where is the danger zone?" — choices: ["Right beside car doors that can swing open", "In the middle of the road", "On the far sidewalk", "There is no danger in daylight"] — correct 0 — citation: `Guide, Grades 6-8: door zone, large-vehicle blind spots, 'I can see, I can be seen'`.

## 3. NPC cast (keep existing four; add two)

Existing (keep as-is): **Officer Ada** — quest giver / safe-crossing coach (POLICE + school crossing);
**Granny Rose** — slow-walker escort client (GROCERY ↔ HOUSES); **Little Sam** — run-risk
escort client who must be stopped from chasing balls (PARK pond); **Mia** — library-goer
escort client (LIBRARY crosswalk).

New:

- **Guard Gus (school crossing guard)** — stands at the SCHOOL crosswalk with a stop paddle; gives the "wait for my signal, then cross on the stripes" briefing and starts the school escort chain.
- **Driver Dee (city bus driver)** — waits at the avenue BUS STOP; teaches curb distance, boarding signal, and the danger-zone rule ("tell me, don't grab it") before her escort/quiz.

Optional 6th (only if scene budget allows): **Chief Cleo (fire station)** — one-line siren/apron safety tip at the FIRE STATION ("when you hear the siren, freeze at the curb").

## 4. Escort / minigame opportunities

GGD-style escort = CrossingNPC state machine (spec 006: leash 6, escort radius 2.5,
opposite-shoulder completion, corridor containment). Drag-drop = DragDropTask panel
(spec 004: ordered tiles, wrong slot bounces back, 0.25 s CompleteAndClose).

| Spot | Type | Safety behavior rehearsed |
|---|---|---|
| SCHOOL crosswalk (Guard Gus → child NPC) | Escort | Wait for guard signal, stay on stripes, arrive together on far shoulder |
| PARK pond ball-chase (Little Sam) | Escort | Do NOT chase a ball into the road; tell a grown-up (le-q4 behavior) |
| POLICE → LIBRARY walk (Mia) | Escort | Curb stop → look left-right-left → cross on designated crossing (sh-q1 chain) |
| BUS STOP boarding (Driver Dee) | Escort | Stand back from curb, board only on driver signal, single file |
| PARKING LOT path (Granny Rose + cart) | Drag-drop minigame | Order the safe path: grown-up close → aisle walking → check backup lights → no play between cars |
| BUS danger zone (dropped item) | Drag-drop minigame | Order the response: freeze → tell driver → wait for signal → driver retrieves |
| Signalized intersection | Drag-drop minigame | Order the crossing: press button → wait for WALK → check turning cars → cross, eyes up |
| SCHOOL building (existing) | Drag-drop minigame (keep) | Crossing steps: Look Left → Look Right → Hold a Grown-Up's Hand (spec 004 §3) |
| RAILROAD CROSSING gates | Sign-read stop (NOT escort/minigame) | Deliberately no escort here: the rehearsed behavior is stop-and-wait, never traversal under pressure |
