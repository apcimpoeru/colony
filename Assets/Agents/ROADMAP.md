# Future work

For the numbered script coverage and test migration plan, see [TEST_ROADMAP.md](TEST_ROADMAP.md).

## First milestone

A colonist works, gets hungry or tired, takes care of themselves, and returns to
work. Build and check this behavior in an open test area using the current
straight-line movement.

Food, inventory, assigned beds, floor sleeping, and sleep recovery already exist. The items below
track completed and planned work. This is the proposed order; details
can change as the story and gameplay develop.

## Planned order

1. [x] **Need prioritization.** `SimController` compares normalized hunger and sleep
   urgency, uses a switch margin to avoid thrashing, interrupts for critical hunger
   or energy, and falls back to sleep when food is unavailable. Without an assigned
   active bed, Sims sleep on the floor at a configurable fraction of normal recovery
   (default half). Configure the policy under SimController's Need Prioritization
   section. When hunger and energy are both critical and usable food exists,
   resolve critical hunger first, then let critical exhaustion take over.
2. [x] **Visible needs and activity.** Each colonist has a runtime status panel
   showing hunger, energy, and current activity. Values and colored bars make
   need changes visible while moving, eating, seeking food, and sleeping.
3. [x] **Numerical work priorities.** Configure Mining and Farming dropdowns under
   SimController > Work Priorities in the Inspector: 1 is highest, 4 is lowest,
   and 0 disables that type. Both default to 3. SimWorkPriorities provides selection
   of the highest-priority enabled type with available tasks; Mining wins ties,
   and unavailable types are skipped. These priorities rank work only; food,
   sleep, and manual destinations continue to use the controller's existing rules.
   Mining and farming will connect task execution to this policy.
4. [x] **Storage and item transfers.** ItemStorage containers use SimInventory.
   Configure a SimController storage trip in the Inspector to deposit all carried
   items, deposit a specific item, or pick up supplies such as seeds. Transfers
   respect stack limits and capacity; untransferred items stay at the source.
   Food/sleep pause trips; manual destinations cancel them. Empty/full/disabled
   containers and blocked trips end safely with status feedback. Hungry Sims seek
   stored food alongside loose food, using one shared nearest-source neighborhood
   and the existing food-level preference, hunger thresholds, and eating cooldown.
   Mining and farming will issue transfer requests automatically in their steps.
5. [ ] **Basic work: mining.** Use the numerical Mining priority to select work
   at a designated mining spot. Travel there and work over game time, adding mined
   items directly to the Sim's inventory. When no more mined items fit, travel to
   storage, deposit the output, and return to mining. Handle unavailable or full
   storage without losing items. Define task completion and cancellation, and
   reserve active mining spots so multiple colonists do not work the same target.
6. [ ] **Basic work: farming.** Use the numerical Farming priority to select work
   at a designated plot. Retrieve needed supplies (such as seeds) from storage,
   plant a crop, let it grow over game time, then return to harvest it. Growth does
   not occupy the colonist; they can do other available work while waiting.
   Planting and harvesting are tasks within the farming work type. Deposit harvested
   items in storage. Define task completion and cancellation, and reserve active
   tasks so multiple colonists do not work the same target.
7. [ ] **Work and rest routines.** Let colonists work autonomously, take breaks
   for food and sleep, and return to work afterward.
8. [ ] **Mood.** Make food happiness, adequate sleep, and unmet needs affect mood.
9. [ ] **Social interaction.** Let colonists seek company and build relationships;
   choose the details to support the story.
10. [ ] **Idle behavior.** Give colonists a simple activity when needs are satisfied
   and there is no work to do.

## Deferred features

- [ ] **Obstacle navigation and reachability.** Navigate around walls and furniture,
  check access to food and beds, and recover from blocked paths.
- [ ] **Property ownership and access.** Prevent colonists from taking or eating
  food that belongs to someone else. Check permission when selecting, collecting,
  and consuming food. Keep ownership separate from temporary reservations. Extend
  this to shared storage, trading, and stealing when those behaviors are needed.
  Assigned beds already restrict use to their assigned colonist.
- [ ] **Cooking.** Turn ingredients into meals when the core daily routine works.
- [ ] **Food spoilage.** Add food storage and freshness rules later.
- [ ] **Bladder and toilets.** Add later for fun; currently low priority for the story.

Keep current implementation and setup notes in [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md).
Update this roadmap as priorities change and features are completed.
