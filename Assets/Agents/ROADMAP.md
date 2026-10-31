# Future work

## First milestone

A colonist works, gets hungry or tired, takes care of themselves, and returns to
work. Build and check this behavior in an open test area using the current
straight-line movement.

Food, inventory, assigned beds, and sleep recovery already exist. The items below
are planned work, not implemented behavior. This is the proposed order; details
can change as the story and gameplay develop.

## Planned order

1. [ ] **Need prioritization.** Choose between hunger and sleep based on urgency.
   Interrupt an activity for a critical need, avoid constantly switching activities,
   and continue another activity when food or an assigned bed is unavailable.
2. [ ] **Visible needs and activity.** Show hunger, energy, and the colonist's
   current activity so their decisions are easy to understand and check.
3. [ ] **Idle behavior.** Give colonists a simple activity when needs are satisfied
   and there is no work to do.
4. [ ] **Basic work.** Add one useful assignable activity, such as gathering
   resources, with clear completion and cancellation.
5. [ ] **Work and rest routines.** Let colonists work autonomously, take breaks
   for food and sleep, and return to work afterward.
6. [ ] **Mood.** Make food happiness, adequate sleep, and unmet needs affect mood.
7. [ ] **Social interaction.** Let colonists seek company and build relationships;
   choose the details to support the story.

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
