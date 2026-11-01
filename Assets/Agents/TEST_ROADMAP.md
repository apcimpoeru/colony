# Test roadmap

Coverage and migration plan for the original 20 gameplay/editor C# scripts. Each
script has its own number. Check an entry when its planned coverage is implemented and passing in
the standard test suite; existing verification scripts do not mark migration complete.

Use Unity Test Framework with tests beside each feature under
`Assets/Features/<Area>/Tests/EditMode` and `Tests/PlayMode`. Each test folder has
its own assembly definition; shared test helpers live in `Assets/Testing`.
Keep temporary test objects independent of saved scenes,
clean them up after each test, and restore time settings. Use bounded waits for
Play Mode behaviors and parameterized cases for numerical policies.

Start with inventory, storage, and the critical-needs regression already covered
by the verification scripts. Add the remaining coverage incrementally. Simple data
classes can be covered through their consumers; visual Inspector checks can remain
manual where automation would add little value.

## Scripts and planned coverage

1. [x] **ItemDefinition.cs** — [source](../Features/Items/ItemDefinition.cs). Edit Mode.
   Verify display-name fallback for blank names, minimum stack size, and asset
   identity: different definitions with the same name remain different items.
   Implemented in [ItemDefinitionTests.cs](../Features/Items/Tests/EditMode/ItemDefinitionTests.cs):
   17 passing cases in Unity 2022.3.26f1, including unvalidated stack values,
   serialized validation, and same-name definitions kept separate by inventory.

2. [x] **FoodDefinition.cs** — [source](../Features/Items/FoodDefinition.cs). Edit Mode.
   Verify level is at least 1 and nourishment/happiness cannot be negative.
   Cover inherited item settings. Confirm zero-nourishment food is excluded from
   automatic eating through the food-system tests; happiness remains stored data.
   Implemented in [FoodDefinitionTests.cs](../Features/Items/Tests/EditMode/FoodDefinitionTests.cs),
   including the eating preference policy rejecting zero-nourishment food.

3. [x] **InventoryStack.cs** — [source](../Features/Items/InventoryStack.cs). Edit Mode,
   primarily through SimInventory tests. Verify item identity and quantity remain
   correct across stacking, splitting, removal, and transfers. Cover serialization
   of item references and quantities without duplicating trivial accessor tests.
   Covered through [SimInventoryTests.cs](../Features/Inventory/Tests/EditMode/SimInventoryTests.cs)
   and Play Mode initialization, including a Unity JSON round-trip preserving
   distinct same-name item references and stack quantities.

4. [x] **SimInventory.cs** — [source](../Features/Inventory/SimInventory.cs). Edit Mode and
   initialization checks in Play Mode. Cover filling existing stacks before new
   slots, stack/capacity limits, partial additions, atomic failed removals, and
   Count/Contains/HasSpaceFor. Reject invalid items and quantities. Verify transfer
   conservation, partial transfers, self/null/disabled destinations, repeated stacks
   in TransferAllTo, and Changed notifications after both inventories are updated.
   Preserve over-capacity starting contents and items after reducing capacity.
   Migrate existing inventory checks from StorageVerification.
   Implemented in [SimInventoryTests.cs](../Features/Inventory/Tests/EditMode/SimInventoryTests.cs)
   and [SimInventoryInitializationTests.cs](../Features/Inventory/Tests/PlayMode/SimInventoryInitializationTests.cs).
   Inventory checks from StorageVerification now have standard-suite coverage;
   the legacy runner has been retired along with its remaining migrated checks.

5. [x] **SimWorkPriorities.cs** — [source](../Features/Colonists/SimWorkPriorities.cs). Edit Mode.
   Parameterize all 0–4 Mining/Farming priorities and availability combinations.
   Verify defaults, disabled types, lower-number precedence, Mining tie-breaks,
   unavailable-task fallback, and null when no work qualifies. Cover invalid
   serialized priorities, unknown work types, and null availability callbacks.
   Implemented in [SimWorkPrioritiesTests.cs](../Features/Colonists/Tests/EditMode/SimWorkPrioritiesTests.cs),
   with all 100 valid priority/availability combinations and invalid-input cases.

6. [x] **SimNeeds.cs** — [source](../Features/Needs/SimNeeds.cs). Edit Mode for bounds;
   Play Mode for time. Verify hunger/energy clamp to 0–100, configured decay rates,
   hunger continuing during rest, energy decay pausing during rest, and no changes
   while game time is paused or the component is disabled.
   Implemented in [SimNeedsTests.cs](../Features/Needs/Tests/EditMode/SimNeedsTests.cs)
   and [SimNeedsTimeTests.cs](../Features/Needs/Tests/PlayMode/SimNeedsTimeTests.cs).

7. [x] **WorldItem.cs** — [source](../Features/Items/WorldItem.cs). Edit Mode for selection
   and reservations; Play Mode for pickup/destruction. Cover active registration,
   pickup-point fallback, exclusive reservation and release, carrier identity,
   horizontal pickup range, partial pickup, and immediate unavailability when empty.
   Verify nearest-food/level preference, filters, capacity checks, and exclusion of
   non-food or zero-nourishment items. Disabling/destroying a target must not leave
   another Sim locked out after cancellation.
   Implemented in [WorldItemTests.cs](../Features/Items/Tests/EditMode/WorldItemTests.cs)
   and [WorldItemPickupTests.cs](../Features/Items/Tests/PlayMode/WorldItemPickupTests.cs).

8. [x] **ItemStorage.cs** — [source](../Features/Storage/ItemStorage.cs). Edit Mode and
   Play Mode interaction checks. Cover interaction-point fallback, horizontal range,
   stopping-distance allowance, disabled containers/inventories, and self-transfer
   rejection. Test all deposit/withdraw directions, empty/full containers, partial
   transfers, and two carriers requesting the final item without duplication.
   Migrate existing storage-access and transfer checks.
   Implemented in [ItemStorageTests.cs](../Features/Storage/Tests/EditMode/ItemStorageTests.cs),
   with real trips in [StorageTripTests.cs](../Features/Storage/Tests/PlayMode/StorageTripTests.cs).

9. [x] **SimFoodSource.cs** — [source](../Features/Food/SimFoodSource.cs). Edit Mode,
   with pickup behavior exercised in Play Mode. Verify one shared nearest-source
   neighborhood across storage and loose food, higher-level preference, distance
   and nourishment tie-breaks, and a fixed neighborhood that does not expand through
   candidates. Cover ignored sources, full carriers, empty/disabled/destroyed sources,
   loose-food reservations, and one-item storage withdrawals. Migrate existing
   storage-versus-loose-food selection and fallback checks.
   Implemented through public eating/search APIs in
   [SimFoodSourceTests.cs](../Features/Food/Tests/EditMode/SimFoodSourceTests.cs)
   and [SimEatingCycleTests.cs](../Features/Food/Tests/PlayMode/SimEatingCycleTests.cs).

10. [x] **SimEating.cs** — [source](../Features/Food/SimEating.cs). Edit Mode for food
    preference; Play Mode for the eating/seeking cycle. Verify start/stop threshold
    hysteresis, highest level then lowest positive nourishment, inventory-order ties,
    one-item meals, hunger clamping, and the scaled one-second bite cooldown.
    Cover carried-food preference, storage/loose pickup, no eating while resting,
    search timing, blocked-target timeout/retry, and reservation cleanup on manual
    movement, disable, or cancellation. Test pause and disappearing targets.
    Implemented in [SimEatingTests.cs](../Features/Food/Tests/EditMode/SimEatingTests.cs)
    and [SimEatingCycleTests.cs](../Features/Food/Tests/PlayMode/SimEatingCycleTests.cs).

11. [x] **SimBed.cs** — [source](../Features/Sleep/SimBed.cs). Edit Mode with lifecycle
    checks in Play Mode. Verify private assignment, rejection of unassigned or
    other-colonist beds, nearest assigned active bed selection, sleep-point fallback,
    reassignment, and removal from searches when disabled or destroyed.
    Implemented in [SimBedTests.cs](../Features/Sleep/Tests/EditMode/SimBedTests.cs)
    and the Play Mode sleeping tests.

12. [x] **SimSleeping.cs** — [source](../Features/Sleep/SimSleeping.cs). Play Mode.
    Verify sleep/wake thresholds, assigned-bed travel and arrival, full-rate bed
    recovery, configurable floor recovery, and clearing the resting flag on wake
    or cancellation. Cover bed removal/reassignment, assigning a bed during floor
    sleep, hunger continuing during sleep, pause, manual interruption, and disable.
    Implemented in [SimSleepingTests.cs](../Features/Sleep/Tests/PlayMode/SimSleepingTests.cs).

13. [x] **SimMovement.cs** — [source](../Features/Movement/SimMovement.cs). Edit Mode for
    arrival boundaries; Play Mode for Rigidbody movement. Verify horizontal movement,
    configured speed, stopping distance, moving destinations, and no overshoot at
    arrival. Preserve vertical velocity and freeze rotation. Cover Stop, disable,
    kinematic bodies, and pausing physics through game time.
    Implemented in [SimMovementTests.cs](../Features/Movement/Tests/EditMode/SimMovementTests.cs)
    and [SimMovementPhysicsTests.cs](../Features/Movement/Tests/PlayMode/SimMovementPhysicsTests.cs).
    Includes the regression for arrival exactly at the stopping distance.

14. [x] **SimStorageTransfer.cs** — [source](../Features/Storage/SimStorageTransfer.cs).
    Edit Mode for request validation; Play Mode for trips. Cover Deposit All,
    Deposit Item, Pick Up Item, configuration snapshots, replacing/cancelling
    requests, and a single transfer on arrival. Verify partial results, invalid
    requests, unavailable/full/empty containers, progress timeout, pause/resume
    resetting the timeout, and accurate destination/status cleanup. Migrate existing
    trip checks and add real movement coverage.
    Implemented in [SimStorageTransferTests.cs](../Features/Storage/Tests/EditMode/SimStorageTransferTests.cs)
    and [StorageTripTests.cs](../Features/Storage/Tests/PlayMode/StorageTripTests.cs).

15. [x] **SimController.cs** — [source](../Features/Colonists/SimController.cs). Edit Mode
    for decision cases; Play Mode for behavior integration. Cover urgency comparisons,
    switch margin, critical thresholds, and unavailable-need fallback. Permanently
    reproduce 100 hunger / 0 energy with carried food and storage food: eat below
    critical hunger, then recover energy within a time limit without alternating
    food/bed trips. Verify manual-destination precedence, target clearing/destruction,
    controller disable, storage trips pausing for needs and resuming afterward,
    and activity labels/details that do not reserve or search for targets.
    Implemented in [SimControllerTests.cs](../Features/Colonists/Tests/EditMode/SimControllerTests.cs),
    [SimControllerIntegrationTests.cs](../Features/Colonists/Tests/PlayMode/SimControllerIntegrationTests.cs),
    and storage trip tests. Includes the regression for urgency exactly at the
    activity switch margin.

16. [ ] **SimStatusDisplay.cs** — [source](../Features/Colonists/SimStatusDisplay.cs).
    Play Mode smoke checks plus manual visual checks. Verify missing camera/controller
    handling and no errors for off-screen/behind-camera Sims. Visually check numeric
    values, hunger/energy bar direction, activity/details, wrapping, screen-edge
    placement, pause visibility, and hiding when disabled. Keep pixel-perfect UI
    assertions out of the initial suite.
    Automated smoke coverage: [SimStatusDisplayTests.cs](../Features/Colonists/Tests/PlayMode/SimStatusDisplayTests.cs).
    Still open for the visual sign-off below; headless runs do not verify rendering.

17. [ ] **SimInventoryEditor.cs** — [source](../Features/Inventory/Editor/SimInventoryEditor.cs).
    Editor smoke/manual checks. Confirm serialized inventory fields remain editable,
    Play Mode quantity changes repaint, and the help text explains live contents
    and Play Mode reset behavior. Reuse inventory tests for quantity correctness.
    Automated smoke coverage: [SimInventoryEditorTests.cs](../Features/Inventory/Tests/EditMode/SimInventoryEditorTests.cs).
    Still open for the live Inspector/repaint and help-text sign-off below.

18. [ ] **SimControllerEditor.cs** — [source](../Features/Colonists/Editor/SimControllerEditor.cs).
    Editor smoke/manual checks. Confirm priority/transfer settings are editable and
    start/cancel buttons enable only in the supported states. Verify starting and
    cancelling a configured trip and showing its status. Keep transfer logic covered
    by controller/transfer tests rather than repeating it as UI tests.
    Automated smoke coverage: [SimControllerEditorTests.cs](../Features/Colonists/Tests/EditMode/SimControllerEditorTests.cs)
    and [StorageTripTests.cs](../Features/Storage/Tests/PlayMode/StorageTripTests.cs).
    Still open for the visible button-state and status sign-off below.

19. [x] **StorageVerification.cs** — runner retired; standard tests replace it.
    Its 49 assertions have standard-suite replacements for conservation,
    notifications, repeated-stack deposits, initial overflow, range, competing
    withdrawals, trips, food selection, appetite, and conflicting critical needs.
    Fixtures clean up temporary objects and isolate searches from open scenes.
    Replacements: inventory tests (item 4), [ItemStorageTests.cs](../Features/Storage/Tests/EditMode/ItemStorageTests.cs),
    [SimStorageTransferTests.cs](../Features/Storage/Tests/EditMode/SimStorageTransferTests.cs),
    [SimFoodSourceTests.cs](../Features/Food/Tests/EditMode/SimFoodSourceTests.cs),
    controller decision tests (item 15), and the Play Mode trip tests.

20. [x] **CriticalNeedsVerification.cs** — runner retired; standard tests replace it.
    Both former scenarios are now standard integration tests: carried food and
    pantry food, each starting at 100 hunger / 0 energy. They preserve bounded
    completion, eating before sleeping, actual energy recovery, fixture cleanup,
    and time-scale restoration. The SessionState/editor-polling runner is removed.
    Both real-frame scenarios are in [SimControllerIntegrationTests.cs](../Features/Colonists/Tests/PlayMode/SimControllerIntegrationTests.cs).

## Manual visual sign-off

The automated smoke tests for items 16–18 are implemented. These entries remain
unchecked until the following checks are performed in Unity; batch runs do not
establish visual correctness.

- [ ] **Status panel (16):** check values against SimNeeds; hunger and energy bar
  direction/color; activity and details; wrapped text; screen-edge placement;
  visibility while paused; and hiding when SimStatusDisplay is disabled.
- [ ] **Inventory Inspector (17):** watch eating/transfers update quantities during
  Play Mode; confirm repainting and the help text explaining live contents and
  Play Mode reset behavior.
- [ ] **Controller Inspector (18):** check editable priority/transfer settings,
  start/cancel button states outside Play Mode, with disabled controller/manual
  destination, and with/without a queued trip; start/cancel a trip and check status.

## Completion and maintenance

Verified in Unity 2022.3.26f1: **256 Edit Mode cases and 48 Play Mode cases passed**.
Items 1–15 and both migrations (19–20) are complete. Items 16–18 have passing
automated smoke tests and remain open only for the manual visual sign-off above.

The runtime scripts share `Colony.Runtime`. Each feature's `Editor` folder uses
an Editor-only `Colony.<Area>.Editor` assembly. Tests use
`Colony.<Area>.EditMode.Tests` or `Colony.<Area>.PlayMode.Tests`; Edit Mode test
assemblies that need Editor helpers reference `Colony.Testing`. Component and
Play Mode helpers use the test-only `Colony.Testing.Runtime` assembly; it isolates
search registries from open scenes and restores them and time settings on teardown.

In Unity, open **Window > General > Test Runner**, select **EditMode**, and choose
**Run All**; repeat on the **PlayMode** tab. For repeatable command-line runs that
also work while this project is open in Unity, run from the project root in PowerShell:

```powershell
$env:DOTNET_PROCESSOR_COUNT = '2'
./Tools/Run-Tests.ps1 -UnityEditor 'F:/unity_install/2022.3.26f1/Editor/Unity.exe'
./Tools/Run-Tests.ps1 -UnityEditor 'F:/unity_install/2022.3.26f1/Editor/Unity.exe' -Platform PlayMode
```

Use the Unity 2022.3.26f1 editor path installed on your machine. The script copies
Assets, Packages, ProjectSettings, and the existing package cache into a fresh
ignored `Temp/StandardTests-*` project. It saves NUnit XML results and the Unity
log under `Logs/TestResults/<run>-<platform>/`, and fails on failing or missing tests.
Temporary projects remain under Temp for inspection and can be removed after Unity
exits.
Run the commands sequentially. The .NET processor setting used during verification
reduces compiler memory usage; it does not change the game's simulation settings.

Run the standard Edit Mode and Play Mode suites through Test Runner and provide
a repeatable command-line run with saved results. A passing suite must not change
saved scenes, leave temporary objects/reservations behind, or depend on test order.
Add every new script to this list and add a regression case for each behavior bug.
Mining and farming coverage will be added when those scripts exist.

See [ROADMAP.md](ROADMAP.md) for gameplay work and [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md)
for current behavior and setup.
