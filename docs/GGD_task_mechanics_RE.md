# GGD Task Mechanics — Reverse-Engineered Reference

Source: Goose Goose Duck IL2CPP dump (user's own copy, dumped 2026-10-08).
Artifacts: `Il2CppDump/dump.cs` (1,352,925 lines — every class/field/method signature
with native RVA), `script.json` (Ghidra function naming), `il2cpp.h`.
Method: static signature reconstruction from dump.cs. Method BODIES are native code in
GameAssembly.dll — behavior below is inferred from signatures, field layouts, coroutine
names, and SafeStreets' own verified TaskPanel port. Deeper "how" questions = load
script.json into Ghidra and read the pseudocode at the RVAs cited.
Scope/legality: offline analysis of files on disk from a personally-owned copy; nothing
here is for use against GGD's online services; do not redistribute the dump.

Line anchors (dump.cs): TaskPanel 139190 · DraggableTaskPanel 139041 · GamepadCursor 3252 ·
GameTask 212574 · TaskObjectHandler 242814 · TaskPanelHandler 242894 · TasksHandler 243295 ·
TaskPrefabHandler (after 242894 block) · SabotageButtonHandler 241185 · SabotagePanelHandler 241356 ·
SabotageReplicationData 141982 · DuckPond example 242557 · namespaces Prefabs.UI.Task_Panels.*
128470–137071.

---

## 1. Architecture: four layers

```
[DATA]     GameTask (one per station in the scene; the "what/where/rules")
    ↓ assignment
[ORCH]     TasksHandler (per-player manager: pool, assignment, completion, replenish)
    ↓ station wrapper
[WORLD]    TaskObjectHandler (physical object + Interactable) + TaskPanelHandler
           (world-side panel owner: ID, cooldown, connect/disconnect, close-delay)
    ↓ opens child
[UI]       TaskPanel (abstract) → DraggableTaskPanel → 60+ concrete minigames
```

Parallel layer: **Sabotage** (SabotageButtonHandler → SabotagePanelHandler singleton →
networked SabotageReplicationData) and the **task-list HUD** (TaskPrefabHandler rows).

---

## 2. TaskPanel — the UI base class (all minigames inherit this)

```
abstract TaskPanel : MonoBehaviour
  const float DEFAULT_CLOSE_DELAY = 0.25
  bool  HasInitialized            // init latch
  TaskPanelHandler parentTaskPanelHandler   // back-ref to the world-side owner
  AudioSource audioSource                   // per-panel SFX
  TaskPanel taskMaster                      // multi-step chains: panel this one hands off to
  bool  isClosing                           // re-entry guard (double-complete safe)
  Action<bool> onComplete                   // success/failure callback event
```

Mechanics:
- **Completion is a delayed coroutine, not instant.** `CompleteAndClose(delay = 0.25)` →
  `CoCompleteAndClose`: sets result, holds ~0.25s (player sees the success state), then
  closes → panel reports `onComplete(true)` up to the handler. `FailAndClose` mirrors it
  with `false` (GGD has fail-able tasks — timers, the falcon-hunt countdown).
- `isClosing` guards double-fire: second click during the 0.25s window is ignored.
- `Close(delay = 0)` → `CoClose` — abort path, no success report.
- `taskMaster` + `HasInitialized` support chained panels (finish step 1's panel, open
  step 2's; SafeStreets' "continuation" equivalent lives on GameTask, see §5).
- SafeStreets port status: **fully mirrored** (Open/CompleteAndClose/FailAndClose/CoClose/
  OnComplete) — that port was verified correct against this dump.

## 3. DraggableTaskPanel — the point-and-click subclass (~40 of 60+ panels)

```
abstract DraggableTaskPanel : TaskPanel
  Canvas canvas; float padding; RectTransform mainFrame
  GamepadCursor gamepadCursor
  Dictionary<Rigidbody2D, float> rigidbodies   // bodies frozen while panel open!
  bool skipFirstUpdate
```

Mechanics:
- **World pauses under the panel:** any Rigidbody2D the task touches is tracked in
  `rigidbodies` and held (gravity/velocity frozen) for the duration;
  `UpdateGravityOfAttachedBodies()` restores on close. This is how GGD stops the room's
  physics while you're in a minigame.
- Implements a family of cursor-capability interfaces — the panel tells the system what
  input it supports: `IGamepadCursor` (needs the virtual cursor), `IGamepadCursorUpdate`
  (per-frame cursor moves), `IGamepadPickup` (grab/release semantics).
- Typical flow of a concrete panel: OnEnable wires cursor + freezes bodies → pointer
  drag/lerp handling (see DuckPond example: coroutines per moving duck, RandomizeDucks,
  GenerateAllCombinations, SelectDuck, FinishTask, FailTask, ResetTask) →
  CompleteAndClose/FailAndClose.
- SafeStreets port status: DragDropTask mirrors the freeze+lerp concept (spec 004 R6);
  GamepadCursor interfaces NOT ported (no gamepad support in SafeStreets).

## 4. TaskPanelHandler — per-station world-side owner (MonoBehaviour on the station prefab)

```
string taskID                      // the join key to GameTask / TasksHandler dictionaries
bool   canClosePanel               // gate: some panels can't be dismissed mid-flow
Canvas taskCanvas; GraphicRaycaster taskRaycaster
bool   inCooldown; float currentCooldown      // anti-spam: task can't re-open instantly
TaskPanel childPanel               // the UI it hosts
Action onClose, onEndSFXPlay
```

API surface → mechanics:
- `OpenPanel() / ClosePanel() / ClosePanelWithDelay(float) / ClosePanelInstantly(forcefully)`
  — delayed close is a coroutine (`ClosePanelDelay`), instant-close used on sabotage
  resolution / death / round end.
- `ConnectToTask() / DisconnectFromTask() / GetConnectedPlayers()` — **GGD tasks are
  co-op**: multiple players can be "connected" to one station simultaneously
  (e.g. two geese fixing one reactor). Completion rules per task flag (`isTaskShared`).
- `CompleteTask() / CompleteTaskIgnoreCD()` — completion normally respects the cooldown;
  IgnoreCD variant is for forced completion (sabotage fixed server-side).
- `Update()` runs the cooldown timer; `ResetCooldown()` on assignment.
- `SendAssignLocalTaskTimer` + `SendAssignLocalTaskTimer` naming suggests network sync of
  assignment timing.
- SafeStreets port status: **NOT ported** — SafeStreets' zones open panels directly with
  no cooldown/connect/delay layer. This is the biggest missing piece if multi-player
  tasks are ever wanted.

## 5. GameTask — the data/rules definition (one per station)

The heart of the system. Field dump (offsets trimmed) with interpretation:

| Field | Mechanic |
|---|---|
| `taskId, taskDisplayName` | identity + list HUD name |
| `taskObject` / `secondaryTaskObject` / `hasSecondaryTaskObject` | primary world object + optional second station (two-stage tasks) |
| `taskPanel` | the TaskPanelHandler prefab at this station |
| `currentContinuationIndex, continuationIndex, taskContinuation` | **task chains**: "Fix Reset Shuttle" step N of M; when `currentContinuationIndex == continuationIndex` the chain completes |
| `isRandomlyAssignable, hasToBeAssigned` | pool membership: random pool vs. forced-assignment (role-critical tasks) |
| `notEligibleForFakeTask` | excluded when the game needs **fake tasks** (duck role gets real-looking list entries that do nothing) |
| `isFakeTask` + `TurnTaskFake() / TurnTaskReal()` | runtime fake-flag flip — fake tasks open the real panel but never count |
| `isSabotage, sabotageTime, excludeFromSabotageConfigurations, sabotageButton` | sabotage coupling: this station can be broken, for how long, per-config exclusions, its map button |
| `dontCountAsCompletedTask, dontReplenishWhenCompleted` | progress accounting: one-off/story tasks vs. the replenishing pool |
| `taskDuplicateType[]` | how duplicates behave when N copies exist on the map |
| `taskCompletionCallback (UnityEvent), isCallbackVisual, isTaskCompletionCallbackAlsoRemote` | designer-wired side effects on completion (door opens, cutscene); remote replication flag |
| `isTaskShared, globalTask` | shared = any player's progress counts for all connected; global = counts for the whole team instantly |
| `isImpostorTask` | duck-only interactive object (portal/wing feats) |
| `huntType (EHuntType)` | hunt-event linkage: FALCON/PELICAN/CROW/SURVIVALIST/RAVEN/HAWK/CUCKOO countdown constants (HuntEventInfo) — these tasks are objectives during role hunts |
| `destroyAfterCompletion` | one-shot stations vanish |
| `validAchievements[], awardAchievementsOnCompletion` | achievement hooks |
| `isEventTask, eventTaskStartDate/EndDate (long)` | time-limited event tasks |
| `taskDimension (EMapDimension)` | multi-dimension maps (normal vs. nether side) |
| `alternateObjects (List<TaskPair>)` + Assign/UnAssign/SetupAlternateTasks | variant stations per map layout |
| `useCircleAsTaskPointOnMap` | map icon style |
| `taskUIInList (TaskPrefabHandler)`, `connectedPlayers`, `hasLocalTimer` | HUD row back-ref, live connect count, per-task local countdown |

## 6. TaskObjectHandler — the physical station

```
bool useProximityPosition          // task point = near-position, not the object center
Interactable interactable          // GGD's interact system (SafeStreets' Interactable mirrors this)
List<string> ForbiddenTasksForGhosts   // ghost restrictions per task
```
`AssignTask(panel)/UnAssignTask()` link the world object to its active panel;
`GetProximityPosition()` gives the "stand here" point (used for the map arrow and
interaction range). Ghosts (dead players) can do some tasks — per-task forbidden list.

## 7. TasksHandler — per-player manager (the conductor)

```
static bool CanTargetTasks
GameTask[] tasks                              // ALL stations on this map
Dictionary<string,GameTask> tasksDictionary   // id → task
Dictionary<string,GameTask> assignedTasks     // MY current list
List<GameTask> sortedAssignedTasks            // HUD order (GetTaskPriority sorts)
GameObject taskUIPrefab; Transform taskList   // list UI container
int timesReplenished                          // pool refill count (task-based win progress)
```

Assignment flows:
- `AssignPlayerTasksMaster()` — host/first assignment pass.
- `AssignPlayerTasks(int number, int sabotageConfiguration)` — deal N tasks from the
  random pool under a sabotage config filter.
- `AssignTasksFowlPlay(int number)` — the Fowl Play special-mode variant.
- `AssignTask(GameTask, bool isFakeTask = false)` — the fake-flag entry point: ducks are
  handed `isFakeTask = true` instances from the same pool (respecting
  `notEligibleForFakeTask`).

Lifecycle: `ConnectToTask(id)` (walk up + interact) → panel opens → panel completes →
`CompleteTask(id, remote, silent, isUnassign, disableAnim)` (flags: remote = networked
completion from another client, silent = no SFX/anim, isUnassign = remove from my list,
disableAnim = skip the checkmark animation) → `UnAssignTask(id)` → `AddAssignedTaskToLists
/ RemoveAssignedTaskFromLists` → if `!dontReplenishWhenCompleted`, a new task is dealt and
`timesReplenished++` → `UpdateTaskVisuals()` refreshes the HUD → `PlaySFXAfterDelay`
coroutine for the completion sting.

## 8. Task-list HUD — TaskPrefabHandler (the row prefab)

```
taskNameText, taskTarget (map icon), taskTargetArrow (off-screen arrow)
checkmarkSprite, isCompleted, baseText
isThisTaskTimedSabotage, isWaitingTimer, waitingTimer, startWaitingTime
statics: HasHuntCountdown, IsTimedSabotage, CurrentSabotagesCount, TimedSabotageTasks
```
Each row is a live tracker: name, in-world/map arrow, completion checkmark, and — for
sabotage stations — a countdown. The statics are the global "a timed sabotage is running"
broadcast. `ControllerUITaskManager/ControllerTaskUI/ControllerUITaskData` are the
gamepad/controller UI variants.

## 9. Sabotage layer (parallel system, same vocabulary)

- `SabotageButtonHandler` (241185): the map-screen button per sabotagable task; wired from
  `GameTask.sabotageButton`.
- `SabotagePanelHandler : Singleton<>` (241356): the saboteur's UI (choose which system to
  break); gamepad-navigation coroutine in SetupGamepadNavigation.
- `SabotageInfo` (60156) / `SabotageReplicationData : BaseReplicationData` (141982) with
  nested `SabotageData / GodSabotageData / WarlockData / ArtifactData`: the **networked**
  sabotage state (which task, until when, which special role variant). BaseReplicationData
  is GGD's network state base — sabotage state lives on the net, not on the scene objects.
- `SabotageIO : TaskPanel` (131533): the panel shown to the FIXING player.
- `SabotageControl_IO` (28259), `SabotageButtonData/SabotagePanelData` (SO configs).

## 10. Panel taxonomy (concrete minigames, 60+)

- **IO point-and-click (DraggableTaskPanel, largest family)**: claw machines, Align
  Planets/Satellites, Clean Crying Statue, Feed Yee Ucky, Sort Books, Stamp Document, Wipe
  Chalkboard, Burn Body, Fix Fence, Loot Truck, Measure Casket, Paint Mask, Mix Smoothie,
  Feed Tiger, Rose Drop, Cuckoo Smash Egg…
- **State-sequence panels (plain TaskPanel)**: FixResetShuttle, ActivateShuttle,
  ActivateTeleporter, ActivateWarp, ConnectTheDotsChapel, OrganizePotions, ReplaceWheel,
  SecretKnock, SelectHerbs, ThreeCardMonte, SpotTheDifferences (IDestructableTask —
  destructible/attackable UI!), ThrowTomatoes, PumpBalloon, Skeeball, TestStrength…
- **Minigame venues**: Arcade (MadQuacks, CarnyCommand), Carnival (CottonCandy, DrawCanvas,
  FortuneTeller, MakePopcorn, MarkMap, RiggedBallIntoBasket), Godzilla, MallardMansion
  (Gramophone_IO), Mall.
- **Conversation : TaskPanel** — the role/subterfuge call UI: question selection
  (ConversationIOQuestion), previous-question memory, static/video noise fades, chatter
  SFX. SafeStreets' DialogManager is a sibling concept but simpler (no question trees).
- **Sabotage repairs**: FixStabilizeReactor, ChargeBattery, CleanShuttlePlatform,
  HoseDown, BirdVaccine…

## 11. SafeStreets gap analysis (what to port, ranked)

| GGD mechanic | SafeStreets today | Port value |
|---|---|---|
| Completion delay + isClosing guard (0.25s, CoCompleteAndClose) | ported (verified identical) | — |
| Per-station cooldown (inCooldown/Update/ResetCooldown) | none — zones re-arm instantly | HIGH: stops quiz spam-retrigger |
| Replenishing pool + timesReplenished (task-based win) | fixed 3-crossing quest | MEDIUM: endless "help 3 more friends" loop for replayability |
| Fake tasks (TurnTaskFake, notEligibleForFakeTask) | none | MEDIUM: distractor quizzes for older grade bands ("which are safe?") |
| Task chains (continuationIndex) | none | MEDIUM: multi-step lessons (stop→look→listen→cross) |
| Shared/connect multi-player tasks | single-player only | LOW (no netcode) |
| Sabotage layer + replication | none | LOW (needs net; concept reuse: timed hazard events) |
| GamepadCursor family | keyboard only | LOW unless controller support wanted |
| Task list HUD w/ arrows (TaskPrefabHandler) | quest counter text only | HIGH for kids: off-screen arrow to next safe-crossing site |
| Ghost/forbidden restrictions | n/a | — |

## 12. Next step for any "how does X actually behave" question

Signatures say WHAT exists; bodies live in native code. Load `script.json` into Ghidra
(Free), search the function by name (e.g. `TasksHandler$$CompleteTask`), read pseudocode at
RVA. Every method above carries its RVA in dump.cs (`// RVA: 0x...`).
