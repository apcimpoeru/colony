# Project context

## Overview

This is a colony simulator project, similar to The Sims but in a town instead of
one household. It is in the planning stage.

## Current state

Sims support manual GameObject destinations and basic Rigidbody movement.
Hunger increases and energy decreases over scaled game time, clamped to 0–100.
Automatic inventory eating and food seeking from loose items or shared storage are implemented.
Storage containers use slot-based inventories; Sims can travel to deposit or pick up items.
Colonists seek their Inspector-assigned beds at low energy, sleep to recover, and wake.
Without an assigned active bed, they sleep in place on the floor at half recovery
by default; Floor Recovery Multiplier on SimSleeping controls this fraction.
Sims can now carry items in a separate slot-based inventory. Further systems are planned.
SimController now prioritizes hunger versus sleep using normalized urgency, critical
need interruption thresholds, and an activity switch margin. It falls back to the
available need when food cannot currently be used; sleep can use the floor.
Colonists now show a runtime status panel above them with hunger, energy, and
their current activity.

## Project structure

Gameplay code lives under `Assets/Features`, grouped into Items, Inventory,
Storage, Food, Sleep, Needs, Movement, and Colonists. Each area keeps its editor
tools in `Editor` and its tests in `Tests/EditMode` or `Tests/PlayMode` beside the
runtime scripts. Items contains item definitions, InventoryStack, and WorldItem;
Food contains eating and source selection. Colonists contains the controller,
work priorities, and status display.

All gameplay scripts share `Assets/Colony.Runtime.asmdef`. Feature editor and
test folders have separate assembly definitions to keep editor/test dependencies
out of runtime builds. Shared Editor-only helpers live in `Assets/Testing` in
`Colony.Testing`; component/Play Mode helpers live in `Assets/Testing/Runtime`
in the test-only `Colony.Testing.Runtime` assembly. Add tests beside the feature
they cover, with a test assembly definition when introducing a new test folder.

Scenes, materials, prefabs, and item assets keep their existing locations.
Run all Edit Mode and Play Mode tests through Unity's Test Runner or
`Tools/Run-Tests.ps1`; see TEST_ROADMAP.md for the commands.

## Main systems

- Features/Inventory/SimInventory: serialized inventory, eight slots by default with configurable
  capacity. Add fills existing stacks before empty slots and returns quantity
  accepted. TryRemove removes the full requested amount or changes nothing.
  Count and Contains query contents; Changed fires once after a successful mutation.
  Stacks exposes a read-only view. Null items and nonpositive quantities are rejected.
  Lowering capacity below occupied slots preserves items but prevents new slots.
- Features/Items/ItemDefinition: ScriptableObject with display name, icon, and maximum stack
  size (default 20). Item identity is the definition asset, not its display name.
- Features/Items/FoodDefinition: item subtype with level (minimum/default 1), nonnegative nourishment (default 15),
  and nonnegative happiness (default 0). Happiness is stored data only; no Sim mood
  system or happiness effect is implemented.
  Compatible with SimInventory; SimEating uses nourishment to reduce hunger.
- Features/Items/InventoryStack: read-only item and quantity for callers; mutated by inventory.
- Features/Colonists/SimController: forwards the public destinationOverride GameObject position
  to movement. Clearing or destroying the destination stops movement; disabling
  the controller also stops movement. Manual destinations interrupt sleep and food seeking.
  Food and sleep compete using need urgency; sleep uses an assigned bed or the floor.
- Features/Needs/SimNeeds: tracks hunger (0 = full, 100 = starving) and energy (0 = exhausted,
  100 = rested), with configurable rates defaulting to +1 hunger and -0.5 energy
  per second. Energy decay pauses while resting; hunger continues to increase.
  SimSleeping owns the resting flag and clears it on waking or cancellation.
- Features/Food/SimEating: starts eating at configurable hunger threshold (default 60),
  consumes highest-level nourishing food first, choosing lowest positive nourishment within that level,
  then waits one scaled game second between items. Once started, the food need stays active
  until hunger reaches Stop Eating Threshold (default 30) or lower, including when
  carried food runs out and the Sim must seek more. Idle Sims wait until hunger reaches
  Hunger Threshold (default 60) before starting again.
  Removes one inventory item per meal, clamps hunger at zero, and ignores non-food.
  Equal level and nourishment use inventory order. Zero-nourishment food is ignored
  by automatic eating and world-food searches.
- Features/Movement/SimMovement: horizontal Rigidbody movement in FixedUpdate, arrival checks,
  movement speed, and stopping distance. Preserves vertical velocity for gravity
  and freezes rotation. No obstacle navigation or reachability checks yet.
- Features/Colonists/SimController need prioritization: compares hunger and sleep urgency when both
  solutions are available, keeps the current activity within a configurable switch
  margin, interrupts for critical hunger or energy, and selects the available
  alternative when only food or sleep can be used. Sleeping does not require a bed.
  The defaults are a 0.15 urgency
  switch margin, critical hunger 85, and critical energy 10.
  When both needs are critical and usable food exists, food takes precedence until
  hunger falls below Critical Hunger, including between bites. Critical exhaustion
  then takes over. Without usable food, the Sim can still sleep. This prevents
  food/bed oscillation at very high hunger and very low energy.

## Running and checking

Attach SimController to a Sim GameObject; it requires SimMovement, which requires
Rigidbody. Add SimMovement manually to existing controller instances if missing.
SimController also requires SimNeeds, SimEating, and SimSleeping; SimEating requires SimInventory.
Add these manually to existing Sims if missing. Need values/rates moved from
SimController to SimNeeds; reapply any previous custom settings there.
Use a non-kinematic Rigidbody, a collider on the Sim, and a floor collider.
Drag a scene GameObject into Destination Override to move toward it; clear it to
stop. Moving that object updates the destination. Arrival checks ignore height.
Configure speed and stopping distance on SimMovement (defaults 2 and 0.2).

Configure Need Prioritization on SimController with Activity Switch Margin,
Critical Hunger, and Critical Energy. A larger switch margin makes the current
activity more persistent; critical values allow the corresponding need to interrupt
the other activity.

Add SimInventory to a Sim to give it storage. Create item assets using
Assets > Create > Item > Item Definition, or Item > Food Definition for food with
nourishment. Existing generic item assets remain generic; create a Food Definition
asset to define food. Other systems can call Add, TryRemove,
Count, and Contains with those assets. SimEating consumes food to restore hunger;
energy is restored by sleeping in an assigned bed or on the floor. Set SimNeeds hunger to 80 and supply inventory
food to try eating. Disable SimEating to keep only manual movement and need decay.
Configure Items on SimInventory in the Inspector by assigning item assets and
quantities. This serialized list IS the live inventory: eating decreases quantities
and removes empty stacks directly. Previous Starting Items entries migrate to Items.
Awake repacks contents to respect stack limits. Over-capacity starting contents are
preserved with a warning; no new slots can be added until space is freed.
Play Mode edits affect live contents; Unity resets Play Mode changes when play stops.
There is no runtime inventory UI, equipment, or weight system yet.
The obsolete editor verification for automatic need decisions was removed with
that system.

## Next steps

Continue with basic mining and farming work as separate steps in an open test area
using the implemented storage, item transfers, and numerical work priorities. Need
prioritization and visible needs are implemented. Idle behavior is last in the roadmap.
Navigation is deferred for now.

See [ROADMAP.md](ROADMAP.md) for the planned order and deferred features.

## Work priorities

- Select a colonist and expand Work Priorities on SimController in the Inspector.
  Mining and Farming each have a dropdown: 0 disables the type, 1 is highest,
  and 4 is lowest. Both default to 3 (Normal), including on existing Sims.
  Edit outside Play Mode to save priorities; Play Mode edits last only for that session.
- SimWorkPriorities is serialized per colonist inside SimController, so no extra
  component or scene setup is needed. It ranks work only; food, sleep, and manual
  destinations retain their current behavior.
- SimController.WorkPriorities.ChooseAvailableWorkType accepts a read-only task
  availability callback and returns the highest-priority enabled available work
  type, or null. Equal priorities select Mining before Farming. Disabled and
  unavailable types are skipped. GetPriority returns a type's numerical priority.
- Mining/farming targets, task reservations, and work execution are not implemented
  yet. The next mining and farming steps will connect available
  tasks to this selection policy.

## Storage and item transfers

- Create a storage GameObject and add ItemStorage. It requires SimInventory;
  configure Capacity and Items there, just as for a colonist. Add a visible mesh
  or collider as desired. Place it on accessible ground. An optional Interaction
  Point beside the container lets Sims stand outside its collider; Interaction
  Distance defaults to 0.75, raised to the Sim's stopping distance when needed.
- Containers are shared. Multiple Sims can approach the same container; quantities
  are rechecked at arrival. Storage does not reserve an entire container or promise
  items before arrival. Ownership and access permissions remain deferred.
- On SimController, expand Storage Transfer and assign Storage. Select Deposit All,
  Deposit Item, or Pick Up Item. The latter two use Item and Quantity. In Play Mode,
  click Start storage transfer. The Sim travels there and transfers what fits once;
  it does not repeat automatically. This works on existing Sims without extra components.
  Start another request to replace the current one, or click Cancel storage transfer.
  Configuration edits during a trip apply to the next request.
- Food and sleep pause the trip, which resumes when those activities finish.
  Manual destinations and disabling the controller cancel the request. Destroyed
  or disabled storage, empty sources, full destinations, and five scaled seconds
  without progress end the request with a status message. Pausing game time also
  pauses travel and transfers. The status panel and Inspector report transfer state.
- ItemStorage.Deposit, DepositAll, and Withdraw require the carrier to be in range.
  SimInventory.TransferTo and TransferAllTo supply the underlying capacity-aware
  transfers. Partial transfers leave the remainder at the source. Both inventories
  are updated before Changed notifications, so observers never see duplicated items.
  Future mining/farming code can issue trips via SimController.StorageTransfer.Request.
- Hungry Sims automatically consider storage and loose food together after carried
  food. Find the nearest usable source, then prefer the highest food level within
  nearest distance + SimEating's Food Preference Distance. Equal levels choose the
  nearest source; equal-distance options prefer lower positive nourishment. Searches
  ignore disabled/empty containers and food that cannot fit in the carrier's inventory.
  Storage food is withdrawn one item at a time, then eaten with the existing thresholds
  and one-second cooldown. Loose-food pickup still collects as much as fits.
- Food trips use the same no-progress timeout and retry delay for containers as for
  loose food. Manual movement, sleeping, or disabling eating cancels food seeking.
  A full carrier with no food cannot retrieve a meal until inventory space is freed.
- Quick check: put food and generic items in a reachable container, clear the Sim's
  manual destination, and try each transfer direction in Play Mode. Test partial
  capacity, empty/full/disabled containers, need interruptions, and two Sims withdrawing
  the final item. Set hunger to 80 with no carried food to check automatic pantry use.
  Put nearby loose food of different levels beside it to check the shared preference.
- Standard Unity Edit Mode and Play Mode tests now cover storage access, transfers,
  conservation, notifications, repeated stacks, overflow, competing withdrawals,
  trips, food selection, and critical-needs decisions. The two former custom
  verification runners have been retired. `SimControllerIntegrationTests` runs
  both critical-needs regressions with real Update/LateUpdate/physics: carried
  food and pantry food, each starting at 100 hunger and 0 energy. Both must eat
  below critical hunger before sleeping, then recover energy without repeated
  food/bed trips. See TEST_ROADMAP.md for suite commands and remaining visual checks.

## Visible needs and activity

- Features/Colonists/SimStatusDisplay draws a compact Game view panel above each colonist using
  runtime IMGUI. It shows the GameObject name, activity, numeric hunger and energy
  out of 100, and colored bars. High hunger is bad; low energy is bad.
- SimController.ActivityLabel reads current behavior: Idle, Moving to destination,
  At destination, Seeking food, Eating, Going to bed, Sleeping, Sleeping on floor,
  or Controller disabled.
  It does not run need selection or reserve food/beds. Eating stays visible for
  the one-second interval after a bite, including the final bite.
- Below the activity, ActivityDetails explains food and rest separately, including
  unmet goals and blockers. A hungry colonist with no usable food shows Idle and
  "Looking for food: none available." A tired colonist without a usable assigned
  bed sleeps on the floor; the details show the actual energy recovery per second
  and whether they are in bed or on the floor. Available needs deferred
  by the other activity show that they are waiting. Explanations use the last
  decision's availability results; displaying them does not search or reserve targets.
  Detail text wraps and the panel grows to fit it.
- SimStatusDisplay is included on the Sim prefab and both existing scenes.
  New SimController components require it automatically. For other existing Sims,
  add SimStatusDisplay manually. No Canvas, fonts, or EventSystem setup is needed.
- A camera tagged MainCamera is required. Panels follow the configured World Offset
  (default 1.5 units above the Sim), remain within the screen edges, and hide when
  their anchor is behind or outside the camera. Panel Width defaults to 220 pixels.
  Disable SimStatusDisplay to hide a panel. Nearby colonists' panels may overlap;
  this is a simple readout for the open test area.
- Enter Play Mode in either scene and watch hunger rise and energy fall. Give a
  hungry colonist carried food or reachable world food to check Eating and Seeking
  food. Assign a reachable SimBed and lower energy to check Going to bed, Sleeping,
  and recovery. Set/clear Destination Override to check manual movement and arrival.
  Pause with Time.timeScale = 0: the panels stay visible and needs stop changing.



## World food pickup

- Features/Items/WorldItem links a scene object to an ItemDefinition, quantity, and optional
  accessible Pickup Point. Active objects register for nearest-food searches and
  support one-Sim reservations. Pickup transfers only accepted quantities and
  destroys the GameObject when empty; partial stacks stay in the world.
- Features/Food/SimEating also supplies destinations to SimController when hunger reaches
  SimEating.HungerThreshold and inventory has no nourishing FoodDefinition items.
  Seeking continues below that starting threshold until the stop threshold is reached. Searches
  at most once per scaled second and skips food that cannot fit. Uses straight-line
  distance with nearby level preference, horizontal pickup range (default 0.75, at least movement stopping
  distance), and cancels if nourishing food is acquired, the stop threshold is reached,
  or the target becomes invalid.
- SimController requires SimEating; add it manually to existing Sims if
  missing. Disabling the controller/eating component or setting a manual destination releases
  reservations. Only the controller issues movement commands.
- Setup: create an apple GameObject, add WorldItem, assign the Apple Food Definition,
  set Quantity, and place on accessible ground. Optional Pickup Point lets the Sim
  stand beside a solid object. Clear Destination Override, empty carried food, and
  set hunger to 80 to test. No trigger collider or pickup input is required.
- Pickup is followed by existing inventory eating, preserving highest level then lowest nourishment
  selection, start/stop threshold checks, and the one-second eating cooldown. No obstacle
  avoidance exists yet. After No Progress Timeout (default 5 scaled seconds) without
  getting at least 0.1 horizontal units closer, the Sim releases its reservation and
  skips that item for Unreachable Retry Delay (default 15 scaled seconds). Other Sims
  can still reserve it. Expired or destroyed skipped targets are cleared on searches.

Food seeking and eating share SimEating; start/stop hunger thresholds, pickup distance, and unreachable-food recovery
are configured there. Stop Eating Threshold is clamped below Hunger Threshold. WorldItem represents collectible objects.

Unused SimNeedTarget (including its SimAction enum) and the empty Test component
were removed. No saved scene/prefab references existed for either component.


Food pickup preference: Food Preference Distance on SimEating defaults to 2 world
units. First find the nearest available food that fits inventory, then choose the
highest-level food within nearest distance + this allowance. Equal levels choose
the nearest. Example: level 1 at distance 5 and level 3 at 6 selects level 3, but a
higher-level food at distance 20 is ignored. Zero allowance chooses nearest food.
The neighborhood is anchored to the original nearest distance, not expanded per
candidate. Existing reservations stay until pickup/cancellation; targets are not
continuously replaced as the Sim travels. Eating cooldown and hunger threshold
checks remain unchanged.

## Assigned beds and sleeping

- Add SimBed to a bed GameObject and drag the colonist's SimController into
  Assigned Colonist on the bed Inspector. Beds are private to their assigned colonist;
  unassigned beds are not used. No general property system is implemented.
- Set an optional Sleep Point to an accessible child Transform beside a solid bed.
  Without a Sleep Point, the bed Transform is the destination. The Sim stays at that
  point; no teleporting, lying-down animation, or collider changes are performed.
- SimSleeping is included on the Sim prefab and colonists in both existing scenes.
  Add it manually to other existing colonists if missing. New SimController
  components require it automatically. Configure Sleep Threshold (default 20),
  Wake Threshold (90), Energy Recovery Per Second (5), Floor Recovery Multiplier (0.5),
  and Sleep Distance (0.75)
  on SimSleeping. Wake Threshold is clamped above Sleep Threshold.
- When energy reaches the sleep threshold or lower, SimController considers sleep
  alongside food using need prioritization. Choosing sleep releases any food reservation.
  If an assigned active bed exists, it stops movement
  within the larger of Sleep Distance and movement Stopping Distance, restores energy
  using scaled game time, and wakes at the wake threshold or higher. Energy does not
  decay while resting. Sims cannot eat while asleep; hunger still increases. Food in
  inventory may still be eaten while travelling to bed.
- A started sleep trip or floor sleep continues until waking or interruption.
  Manual destinations, critical hunger with available food, or disabling the
  controller/sleep component interrupt sleeping.
  Reassigning, disabling, or destroying a bed is checked on the next unpaused tick;
  the Sim uses another assigned bed if one exists, otherwise sleeps on the floor.
  A bed assigned during floor sleep is used on the next unpaused tick.
  If multiple beds are assigned to
  one colonist, the nearest is selected when starting a trip; a valid current bed is kept.
- Without an assigned active bed, the colonist stops and sleeps at their current
  position. Recovery is Energy Recovery Per Second times Floor Recovery Multiplier
  (default 5 * 0.5 = 2.5 energy per scaled second). The multiplier is configurable
  from 0.01 to 1. Energy decay pauses, hunger continues, and eating is blocked, just
  as in bed. Floor sleep uses the same sleep/wake thresholds and interruption rules.
  There is no lying-down animation or search for a floor position.
  Bed seeking uses the existing
  straight-line movement; obstacle navigation and unreachable-bed recovery remain future work.
- To try it, assign a reachable bed, clear Destination Override, set energy to 10,
  and enter Play Mode. Check travel, recovery, waking, manual interruption, and bed
  reassignment. Remove/disable the assigned bed to check floor sleep and half-rate
  recovery; change Floor Recovery Multiplier to verify its effect. Assign a bed
  while sleeping on the floor to check the transition back to bed.
  Pausing Time.timeScale also pauses travel, recovery, and need decay.
