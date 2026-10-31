# Project context

## Overview

This is a colony simulator project, similar to The Sims but in a town instead of
one household. It is in the planning stage.

## Current state

Sims support manual GameObject destinations and basic Rigidbody movement.
Hunger increases and energy decreases over scaled game time, clamped to 0–100.
Automatic inventory eating and nearest-world-food seeking are implemented.
Colonists seek their Inspector-assigned beds at low energy, sleep to recover, and wake.
Sims can now carry items in a separate slot-based inventory. Further systems are planned.

## Main systems

- Sim/SimInventory: serialized inventory, eight slots by default with configurable
  capacity. Add fills existing stacks before empty slots and returns quantity
  accepted. TryRemove removes the full requested amount or changes nothing.
  Count and Contains query contents; Changed fires once after a successful mutation.
  Stacks exposes a read-only view. Null items and nonpositive quantities are rejected.
  Lowering capacity below occupied slots preserves items but prevents new slots.
- Item/ItemDefinition: ScriptableObject with display name, icon, and maximum stack
  size (default 20). Item identity is the definition asset, not its display name.
- Item/FoodDefinition: item subtype with level (minimum/default 1), nonnegative nourishment (default 15),
  and nonnegative happiness (default 0). Happiness is stored data only; no Sim mood
  system or happiness effect is implemented.
  Compatible with SimInventory; SimEating uses nourishment to reduce hunger.
- Item/InventoryStack: read-only item and quantity for callers; mutated by inventory.
- Sim/SimController: forwards the public destinationOverride GameObject position
  to movement. Clearing or destroying the destination stops movement; disabling
  the controller also stops movement. Manual destinations interrupt sleep and food seeking.
  Assigned-bed sleep takes priority over food seeking while energy is low.
- Sim/SimNeeds: tracks hunger (0 = full, 100 = starving) and energy (0 = exhausted,
  100 = rested), with configurable rates defaulting to +1 hunger and -0.5 energy
  per second. Energy decay pauses while resting; hunger continues to increase.
  SimSleeping owns the resting flag and clears it on waking or cancellation.
- Sim/SimEating: starts eating at configurable hunger threshold (default 60),
  consumes highest-level nourishing food first, choosing lowest positive nourishment within that level,
  then waits one scaled game second between items. Once started, the food need stays active
  until hunger reaches Stop Eating Threshold (default 30) or lower, including when
  carried food runs out and the Sim must seek more. Idle Sims wait until hunger reaches
  Hunger Threshold (default 60) before starting again.
  Removes one inventory item per meal, clamps hunger at zero, and ignores non-food.
  Equal level and nourishment use inventory order. Zero-nourishment food is ignored
  by automatic eating and world-food searches.
- Sim/SimMovement: horizontal Rigidbody movement in FixedUpdate, arrival checks,
  movement speed, and stopping distance. Preserves vertical velocity for gravity
  and freezes rotation. No obstacle navigation or reachability checks yet.

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

Add SimInventory to a Sim to give it storage. Create item assets using
Assets > Create > Item > Item Definition, or Item > Food Definition for food with
nourishment. Existing generic item assets remain generic; create a Food Definition
asset to define food. Other systems can call Add, TryRemove,
Count, and Contains with those assets. SimEating consumes food to restore hunger;
energy is restored by sleeping in an assigned bed. Set SimNeeds hunger to 80 and supply inventory
food to try eating. Disable SimEating to keep only manual movement and need decay.
Configure Items on SimInventory in the Inspector by assigning item assets and
quantities. This serialized list IS the live inventory: eating decreases quantities
and removes empty stacks directly. Previous Starting Items entries migrate to Items.
Awake repacks contents to respect stack limits and capacity, warning about excess.
Play Mode edits affect live contents; Unity resets Play Mode changes when play stops.
There is no runtime inventory UI, equipment, or weight system yet.
The obsolete editor verification for automatic need decisions was removed with
that system.

## Next steps

Build the colonist's daily behavior in an open test area, starting with need
prioritization. Navigation is deferred for now.

See [ROADMAP.md](ROADMAP.md) for the planned order and deferred features.



## World food pickup

- Item/WorldItem links a scene object to an ItemDefinition, quantity, and optional
  accessible Pickup Point. Active objects register for nearest-food searches and
  support one-Sim reservations. Pickup transfers only accepted quantities and
  destroys the GameObject when empty; partial stacks stay in the world.
- Sim/SimEating also supplies destinations to SimController when hunger reaches
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
- Add SimSleeping manually to existing colonists if missing. New SimController
  components require it automatically. Configure Sleep Threshold (default 20),
  Wake Threshold (90), Energy Recovery Per Second (5), and Sleep Distance (0.75)
  on SimSleeping. Wake Threshold is clamped above Sleep Threshold.
- When energy reaches the sleep threshold or lower, SimController prioritizes the
  assigned bed over food seeking and releases any food reservation. It stops movement
  within the larger of Sleep Distance and movement Stopping Distance, restores energy
  using scaled game time, and wakes at the wake threshold or higher. Energy does not
  decay while resting. Sims cannot eat while asleep; hunger still increases. Food in
  inventory may still be eaten while travelling to bed.
- A started sleep trip continues until waking or interruption. Manual destinations,
  disabling the controller/sleep component, or losing bed access interrupt sleeping.
  Reassigning, disabling, or destroying a bed is checked on the next unpaused tick;
  the Sim uses another assigned bed if one exists. If multiple beds are assigned to
  one colonist, the nearest is selected when starting a trip; a valid current bed is kept.
- Without an assigned active bed, the colonist continues other activities and energy
  keeps decreasing. There is no ground-sleep fallback. Bed seeking uses the existing
  straight-line movement; obstacle navigation and unreachable-bed recovery remain future work.
- To try it, assign a reachable bed, clear Destination Override, set energy to 10,
  and enter Play Mode. Check travel, recovery, waking, manual interruption, and bed
  reassignment. Pausing Time.timeScale also pauses travel, recovery, and need decay.
