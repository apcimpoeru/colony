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
5. [ ] **Notifications panel.** Show notifications from every Sim in one shared
   panel, grouped by Sim. Use three categories: Important, Warning, and Regular.
   Order Sim groups by their highest notification category and show the current
   notification count for each category. Let the player remove notifications one
   by one. Keep the first version simple while supporting hundreds of colonists;
   see the plan below.
6. [ ] **Basic work: mining.** Use the numerical Mining priority to select work
   at a designated mining spot. Travel there and work over game time, adding mined
   items directly to the Sim's inventory. When no more mined items fit, travel to
   storage, deposit the output, and return to mining. Handle unavailable or full
   storage without losing items. Define task completion and cancellation, and
   reserve active mining spots so multiple colonists do not work the same target.
7. [ ] **Basic work: farming.** Use the numerical Farming priority to select work
   at a designated plot. Retrieve needed supplies (such as seeds) from storage,
   plant a crop, let it grow over game time, then return to harvest it. Growth does
   not occupy the colonist; they can do other available work while waiting.
   Planting and harvesting are tasks within the farming work type. Deposit harvested
   items in storage. Define task completion and cancellation, and reserve active
   tasks so multiple colonists do not work the same target.
8. [ ] **Work and rest routines.** Let colonists work autonomously, take breaks
   for food and sleep, and return to work afterward.
9. [ ] **Mood.** Make food happiness, adequate sleep, and unmet needs affect mood.
10. [ ] **Social interaction.** Let colonists seek company and build relationships;
   choose the details to support the story.
11. [ ] **Idle behavior.** Give colonists a simple activity when needs are satisfied
   and there is no work to do.

## Notification plan

- [ ] **Shared panel.** Display notifications from all Sims in one scrollable
  list, grouped by Sim. Each group identifies the Sim and contains their
  notification entries, each with a message and category: Important, Warning,
  or Regular.
- [ ] **Priority and counts.** Order Sim groups by their highest remaining
  notification category: Important first, then Warning, then Regular. Within each
  group, sort entries in the same category order. Keep equal-priority ordering
  stable. Show separate counts of notifications currently in the panel for all
  three categories; these count entries, not Sims.
- [ ] **Individual removal.** Give each entry a remove button. Removing it takes
  it out of the list and immediately updates its category count and the group's
  priority. Remove the Sim's group when its final notification is removed.
- [ ] **Simple communication.** Sims send notifications to one shared collection;
  the panel updates when entries are added or removed. Avoid scanning every Sim
  each frame or broadcasting between Sims. Support hundreds of colonists by
  reusing list rows and drawing only visible entries.
- [ ] **Checks.** Verify notifications from multiple Sims, all three category
  counts, grouping by Sim, group/entry priority ordering, and individual removal,
  including group reordering and removal of empty groups. Check responsiveness with
  hundreds of colonists and a long notification list. Add concrete coverage to
  [TEST_ROADMAP.md](TEST_ROADMAP.md) when the scripts are introduced.

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
