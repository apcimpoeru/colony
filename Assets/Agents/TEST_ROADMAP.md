# Test roadmap

Coverage plan for all 20 current gameplay/editor C# scripts under Assets. Each
script has its own number. Check an entry when its planned coverage is implemented and passing in
the standard test suite; existing verification scripts do not mark migration complete.

Use Unity Test Framework with Edit Mode tests in Assets/Tests/EditMode and Play
Mode tests in Assets/Tests/PlayMode. Set up runtime and test assembly definitions
before migrating checks. Keep temporary test objects independent of saved scenes,
clean them up after each test, and restore time settings. Use bounded waits for
Play Mode behaviors and parameterized cases for numerical policies.

Start with inventory, storage, and the critical-needs regression already covered
by the verification scripts. Add the remaining coverage incrementally. Simple data
classes can be covered through their consumers; visual Inspector checks can remain
manual where automation would add little value.

## Scripts and planned coverage

1. [x] **ItemDefinition.cs** — [source](../Item/ItemDefinition.cs). Edit Mode.
   Verify display-name fallback for blank names, minimum stack size, and asset
   identity: different definitions with the same name remain different items.
   Implemented in [ItemDefinitionTests.cs](../Tests/EditMode/ItemDefinitionTests.cs):
   17 passing cases in Unity 2022.3.26f1, including unvalidated stack values,
   serialized validation, and same-name definitions kept separate by inventory.

2. [x] **FoodDefinition.cs** — [source](../Item/FoodDefinition.cs). Edit Mode.
   Verify level is at least 1 and nourishment/happiness cannot be negative.
   Cover inherited item settings. Confirm zero-nourishment food is excluded from
   automatic eating through the food-system tests; happiness remains stored data.
   Implemented in [FoodDefinitionTests.cs](../Tests/EditMode/FoodDefinitionTests.cs),
   including the eating preference policy rejecting zero-nourishment food.

3. [x] **InventoryStack.cs** — [source](../Item/InventoryStack.cs). Edit Mode,
   primarily through SimInventory tests. Verify item identity and quantity remain
   correct across stacking, splitting, removal, and transfers. Cover serialization
   of item references and quantities without duplicating trivial accessor tests.
   Covered through [SimInventoryTests.cs](../Tests/EditMode/SimInventoryTests.cs)
   and Play Mode initialization, including a Unity JSON round-trip preserving
   distinct same-name item references and stack quantities.

4. [x] **SimInventory.cs** — [source](../Sim/SimInventory.cs). Edit Mode and
   initialization checks in Play Mode. Cover filling existing stacks before new
   slots, stack/capacity limits, partial additions, atomic failed removals, and
   Count/Contains/HasSpaceFor. Reject invalid items and quantities. Verify transfer
   conservation, partial transfers, self/null/disabled destinations, repeated stacks
   in TransferAllTo, and Changed notifications after both inventories are updated.
   Preserve over-capacity starting contents and items after reducing capacity.
   Migrate existing inventory checks from StorageVerification.
   Implemented in [SimInventoryTests.cs](../Tests/EditMode/SimInventoryTests.cs)
   and [SimInventoryInitializationTests.cs](../Tests/PlayMode/SimInventoryInitializationTests.cs).
   Inventory checks from StorageVerification now have standard-suite coverage;
   retain the legacy runner until its remaining storage/food checks are migrated.

5. [x] **SimWorkPriorities.cs** — [source](../Sim/SimWorkPriorities.cs). Edit Mode.
   Parameterize all 0–4 Mining/Farming priorities and availability combinations.
   Verify defaults, disabled types, lower-number precedence, Mining tie-breaks,
   unavailable-task fallback, and null when no work qualifies. Cover invalid
   serialized priorities, unknown work types, and null availability callbacks.
   Implemented in [SimWorkPrioritiesTests.cs](../Tests/EditMode/SimWorkPrioritiesTests.cs),
   with all 100 valid priority/availability combinations and invalid-input cases.

6. [ ] **SimNeeds.cs** — [source](../Sim/SimNeeds.cs). Edit Mode for bounds;
   Play Mode for time. Verify hunger/energy clamp to 0–100, configured decay rates,
   hunger continuing during rest, energy decay pausing during rest, and no changes
   while game time is paused or the component is disabled.

7. [ ] **WorldItem.cs** — [source](../Item/WorldItem.cs). Edit Mode for selection
   and reservations; Play Mode for pickup/destruction. Cover active registration,
   pickup-point fallback, exclusive reservation and release, carrier identity,
   horizontal pickup range, partial pickup, and immediate unavailability when empty.
   Verify nearest-food/level preference, filters, capacity checks, and exclusion of
   non-food or zero-nourishment items. Disabling/destroying a target must not leave
   another Sim locked out after cancellation.

8. [ ] **ItemStorage.cs** — [source](../Item/ItemStorage.cs). Edit Mode and
   Play Mode interaction checks. Cover interaction-point fallback, horizontal range,
   stopping-distance allowance, disabled containers/inventories, and self-transfer
   rejection. Test all deposit/withdraw directions, empty/full containers, partial
   transfers, and two carriers requesting the final item without duplication.
   Migrate existing storage-access and transfer checks.

9. [ ] **SimFoodSource.cs** — [source](../Sim/SimFoodSource.cs). Edit Mode,
   with pickup behavior exercised in Play Mode. Verify one shared nearest-source
   neighborhood across storage and loose food, higher-level preference, distance
   and nourishment tie-breaks, and a fixed neighborhood that does not expand through
   candidates. Cover ignored sources, full carriers, empty/disabled/destroyed sources,
   loose-food reservations, and one-item storage withdrawals. Migrate existing
   storage-versus-loose-food selection and fallback checks.

10. [ ] **SimEating.cs** — [source](../Sim/SimEating.cs). Edit Mode for food
    preference; Play Mode for the eating/seeking cycle. Verify start/stop threshold
    hysteresis, highest level then lowest positive nourishment, inventory-order ties,
    one-item meals, hunger clamping, and the scaled one-second bite cooldown.
    Cover carried-food preference, storage/loose pickup, no eating while resting,
    search timing, blocked-target timeout/retry, and reservation cleanup on manual
    movement, disable, or cancellation. Test pause and disappearing targets.

11. [ ] **SimBed.cs** — [source](../Sim/SimBed.cs). Edit Mode with lifecycle
    checks in Play Mode. Verify private assignment, rejection of unassigned or
    other-colonist beds, nearest assigned active bed selection, sleep-point fallback,
    reassignment, and removal from searches when disabled or destroyed.

12. [ ] **SimSleeping.cs** — [source](../Sim/SimSleeping.cs). Play Mode.
    Verify sleep/wake thresholds, assigned-bed travel and arrival, full-rate bed
    recovery, configurable floor recovery, and clearing the resting flag on wake
    or cancellation. Cover bed removal/reassignment, assigning a bed during floor
    sleep, hunger continuing during sleep, pause, manual interruption, and disable.

13. [ ] **SimMovement.cs** — [source](../Sim/SimMovement.cs). Edit Mode for
    arrival boundaries; Play Mode for Rigidbody movement. Verify horizontal movement,
    configured speed, stopping distance, moving destinations, and no overshoot at
    arrival. Preserve vertical velocity and freeze rotation. Cover Stop, disable,
    kinematic bodies, and pausing physics through game time.

14. [ ] **SimStorageTransfer.cs** — [source](../Sim/SimStorageTransfer.cs).
    Edit Mode for request validation; Play Mode for trips. Cover Deposit All,
    Deposit Item, Pick Up Item, configuration snapshots, replacing/cancelling
    requests, and a single transfer on arrival. Verify partial results, invalid
    requests, unavailable/full/empty containers, progress timeout, pause/resume
    resetting the timeout, and accurate destination/status cleanup. Migrate existing
    trip checks and add real movement coverage.

15. [ ] **SimController.cs** — [source](../Sim/SimController.cs). Edit Mode
    for decision cases; Play Mode for behavior integration. Cover urgency comparisons,
    switch margin, critical thresholds, and unavailable-need fallback. Permanently
    reproduce 100 hunger / 0 energy with carried food and storage food: eat below
    critical hunger, then recover energy within a time limit without alternating
    food/bed trips. Verify manual-destination precedence, target clearing/destruction,
    controller disable, storage trips pausing for needs and resuming afterward,
    and activity labels/details that do not reserve or search for targets.

16. [ ] **SimStatusDisplay.cs** — [source](../Sim/SimStatusDisplay.cs).
    Play Mode smoke checks plus manual visual checks. Verify missing camera/controller
    handling and no errors for off-screen/behind-camera Sims. Visually check numeric
    values, hunger/energy bar direction, activity/details, wrapping, screen-edge
    placement, pause visibility, and hiding when disabled. Keep pixel-perfect UI
    assertions out of the initial suite.

17. [ ] **SimInventoryEditor.cs** — [source](../Editor/SimInventoryEditor.cs).
    Editor smoke/manual checks. Confirm serialized inventory fields remain editable,
    Play Mode quantity changes repaint, and the help text explains live contents
    and Play Mode reset behavior. Reuse inventory tests for quantity correctness.

18. [ ] **SimControllerEditor.cs** — [source](../Editor/SimControllerEditor.cs).
    Editor smoke/manual checks. Confirm priority/transfer settings are editable and
    start/cancel buttons enable only in the supported states. Verify starting and
    cancelling a configured trip and showing its status. Keep transfer logic covered
    by controller/transfer tests rather than repeating it as UI tests.

19. [ ] **StorageVerification.cs** — [source](../Editor/StorageVerification.cs).
    Migration task. Move its 49 current assertions into independently named standard
    tests for the relevant scripts above. Preserve conservation, notifications,
    repeated-stack deposits, initial overflow, range, competing withdrawals, trips,
    food selection, appetite, and conflicting-critical-need cases. Confirm cleanup
    and isolation, then retire the custom runner once replacement coverage passes.

20. [ ] **CriticalNeedsVerification.cs** — [source](../Editor/CriticalNeedsVerification.cs).
    Migration task. Convert both existing Play Mode scenarios into standard
    parameterized/integration tests: carried food and pantry food, each starting at
    100 hunger / 0 energy. Preserve bounded completion, eating before sleeping,
    actual energy recovery, fixture cleanup, and time-scale restoration. Retire the
    SessionState/editor-polling runner after the replacements pass.

## Completion and maintenance

The runtime scripts compile in `Colony.Runtime`; editor tools compile in the
Editor-only `Colony.Editor` assembly. Edit Mode tests use `Colony.EditMode.Tests`.
Play Mode tests use `Colony.PlayMode.Tests` under `Assets/Tests/PlayMode`.
Items 1–5 pass in Unity 2022.3.26f1: 175 Edit Mode cases and 3 Play Mode cases.

In Unity, open **Window > General > Test Runner**, select **EditMode**, and choose
**Run All**; repeat on the **PlayMode** tab. For repeatable command-line runs that
also work while this project is open in Unity, run from the project root in PowerShell:

```powershell
./Tools/Run-Tests.ps1 -UnityEditor 'F:/unity_install/2022.3.26f1/Editor/Unity.exe'
./Tools/Run-Tests.ps1 -UnityEditor 'F:/unity_install/2022.3.26f1/Editor/Unity.exe' -Platform PlayMode
```

Use the Unity 2022.3.26f1 editor path installed on your machine. The script copies
Assets, Packages, ProjectSettings, and the existing package cache into a fresh
ignored `Temp/StandardTests-*` project. It saves NUnit XML results and the Unity
log under `Logs/TestResults/<run>-<platform>/`, and fails on failing or missing tests.
Temporary projects remain under Temp for inspection and can be removed after Unity
exits.

Run the standard Edit Mode and Play Mode suites through Test Runner and provide
a repeatable command-line run with saved results. A passing suite must not change
saved scenes, leave temporary objects/reservations behind, or depend on test order.
Add every new script to this list and add a regression case for each behavior bug.
Mining and farming coverage will be added when those scripts exist.

See [ROADMAP.md](ROADMAP.md) for gameplay work and [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md)
for current behavior and setup.
