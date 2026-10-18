# Week 9: Warehouse harvesting and loading ships in port

Reference: the user's `(V.2) Colonization-Game Production Manual.docx`, blocks 2269–2539 and their code/Inspector screenshots. Week 10 starts at block 2540.

## Implemented scope

- Sixteen 120x120 StockSlots in the full 1920px product row, with 64px icons, 42pt quantities and one reusable 64px StockDrag per slot. Existing product objects and warehouse text references are upgraded in place. Four reusable prefabs are created through Unity APIs: StockSlot, StockDrag, ShipInPortIcon and CargoSlot.
- Town production is recalculated from town/worker state before harvest, independent of whether its UI is open. The end of the AI turn invokes GameManager.StartNewTurn, which harvests every town of every faction once for the new GameTurn. Repeated turn-start/harvest calls cannot double the warehouse. Selecting professions or reopening a town changes no stored quantities.
- Backspace can end a turn from TownPanel. Closing the town cancels a pending profession and returns any stock still held by a drag before the AI starts. Existing movement, player-turn and AI completion guards remain in force.
- Left-clicking an owned town opens its UI. Selecting/arriving land units hides stationary friendly land peers on the same Hex, while in-flight units remain active so their movement can finish. The camera follows the selected unit.
- Ships can animate from adjacent water into their own town and back out, preserving normal adjacency, movement-point, boarding-reservation, turn and passenger-membership rules. Other land and enemy towns reject entry.
- The port uses the manual's 420x160 ship grid (128px cells), 450x80 cargo panel and six 64px CargoSlots. Only the selected ship's hold count is enabled; choosing another docked ship updates that binding. Opening/closing or switching towns removes old ship icons and disables stale holds.
- Stock drags reserve at most 100 units. An empty numbered hold accepts that product, or an existing matching stack fills to 100. Sparse hold indices are preserved. Mixed products/full holds reject the remainder, which returns to its original town on release. Cancelled, invalid, closed-UI and ship/town-switch drags conserve stock.

## Asset policy and milestone boundary

Only existing product/unit sprites and native UI components are used. No personal art, external images or packages are imported. The old footer Exit object is retained inactive because sixteen 120px slots fill the entire row; the existing header Exit remains available.

Week 9 explicitly ends after loading data into CargoList, before cargo-content icons exist. Those icons, dragging cargo back into stock, Europe travel and trading begin in later weeks. Consumption, growth, expert multipliers and manufactured-goods conversion are also outside this change. Passenger berths and product holds retain the tutorial's separate capacity models.

## Validation

Native Week 9 setup passed at 15:57:32 and the complete Week 9 Play Mode suite passed at 16:17:25 on 2026-10-14 (Asia/Bangkok). Fresh Week 5–8 behavioral regressions also passed; the Week 5 fixture generated 62 Native towns. Both runtime/Editor source assemblies compile against actual Unity references with zero errors. Native Editor compilation and the final Play Mode run confirmed the imported sources.

The Week 9 suite checked two actual Backspace/AI harvest boundaries for all factions, warehouse/preview separation, sparse/partial/full/mixed cargo loads and refunds, port entry/departure with passenger/cargo persistence, two/six-hold selection, input guards, repeated opening and cleanup. Pointer actions use GraphicRaycaster/Physics2DRaycaster plus EventSystem dispatch; keyboard actions use the Input System. No runtime scene state was saved.

The earlier Week 6 fixture retains movement/work/camera blocking checks; Backspace now belongs to the Week 9 turn test because the manual deliberately makes it close TownPanel and end the turn. The fixture uses the header exit when the full-width Week 9 stock row is configured. Its v9 runner starts from stable Editor updates instead of background Inspector delay callbacks, and queues player-loop updates while testing. This fixes a harness wait after Week 5, without changing gameplay behavior.

Both retained 1920x1080 screenshots were inspected. The port view shows two ship icons, the selected Caravel's two empty-looking holds (cargo-content images belong to Week 10), and all sixteen readable warehouse amounts. The next-turn view shows Food increasing from 10 to 13 and Lumber from 0 to 5. The six-hold Galleon and loading into its sixth hold were verified behaviorally. No pink materials or overlapping stock controls were observed.

Evidence is retained under `Docs/AI/Validation/Week9`: native setup, Week 5–9 smoke results, compilation result, the two screenshots and final Editor status. Unity finished in Edit Mode with only saved Map01 open and no dirty scene. No standalone player build or physical mouse-device test is claimed. Week 8 and Week 9 changes remain uncommitted.
