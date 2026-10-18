# Week 8: Main menu and European nation selection

Reference: the user's `(V.2) Colonization-Game Production Manual.docx`, blocks 1902–2268, including the Inspector screenshots. Week 9 starts at block 2269 and is outside this change.

## Implemented scope

- `MainMenu` scene with the existing Art Deco background, cropped 710x900 menu frame, title, New Game/Load Game/Option labels, separators and Exit button. Its 400x200 menu layout uses the specified 300x30 buttons, font size 44 and color C8AD6D. New Game opens SelectNation and resets the selection to England. Exit quits a player or stops Editor Play Mode.
- `SelectNation` scene with the specified 1416x1080 frame, title, 600x820 nation panels, 550x715 vertical ScrollRect, reusable 550x100 NationCard toggle prefab, leader panel, statues, bottom bar and Back/Start Game buttons.
- Five exclusive choices in the manual's order: England, France, Spain, Netherland and Portugal. Cards show the existing flag, a temporary leader proxy and nation name. Choosing a card updates the large image and `Settings.playerNationId`; deselection callbacks cannot overwrite the new selection.
- Start Game loads Map01. GameManager selects the chosen European faction, whose original ship, two passengers, shield and camera initialization are reused. Invalid selection IDs fall back to England; direct Map01 play defaults to England after a fresh domain load.
- Build scene order is MainMenu, SelectNation, Map01. Existing additional build entries, if present, are retained after these three scenes.

## Asset policy and boundaries

All artwork already existed in `Assets/Resources/Art Deco GUI` and the faction assets. Native derived Sprite assets under `Assets/Resources/Week8UI` retain the manual's crop rectangles and nine-slice borders without changing source texture importers or existing GUIDs. No artwork, packages or external dependencies were imported.

The project has no leader portraits. Each nation's existing shield temporarily fills the small and large leader image. The large proxy fits inside the original LeaderPanel rather than overlapping the nation list; its image is 500x650 because shields lack the transparent margins of the manual's 768x1024 character art. The viewport uses a real mask, VerticalLayoutGroup and preferred-height ContentSizeFitter; five cards fit without a redundant scrollbar. These are deliberate asset/layout adaptations.

Load Game and Option remain disabled placeholders because Week 8 specifies their appearance but provides no save/load or options behavior. Nation bonuses, warehouse harvesting, conversion recipes and expert multipliers are not added. Assignment 9's personal production-chain artwork is not generated or imported.

## Native setup and validation

`Tools > Colonization > Apply Week 8 From Existing Assets` saves the new scenes, prefab and derived sprites through Unity APIs and configures the build scene order. It preserves existing scene objects and refuses unsaved open scenes or Play Mode. Rerunning setup reuses scene objects, prefab references and asset GUIDs.

`Run Week 8 Play Mode Smoke Test` first runs the Week 5/6/7 behavioral suites, then exercises New Game, Back, all five cards, all five complete menu-to-Map01 paths, selection guards and Exit. The flow test uses GraphicRaycaster and EventSystem dispatch in the player loop and captures the main menu, Portugal selection and Portugal's starting ship. Runtime state is not saved and the original Editor scene is restored.

Native setup passed on 2026-10-14 at 14:51:02 (Asia/Bangkok), and the final setup rerun passed at 15:04:07 with identical GUID metadata for all eight derived sprites. Fresh Week 5/6/7 behavioral suites passed at 14:51:33, 14:51:52 and 14:52:09; the Week 5 map generated 50 Native towns. The final Week 8 flow passed at 15:08:16, including all five selected factions, fresh maps, player ships/passengers/shields/camera, New Game reset, Back, invalid IDs, false-toggle guards and Exit. The runner waits for the 0.1-second selection fade to finish and verifies the visible selected/unselected graphic alpha before taking its screenshot. Reloading the final saved scenes also verifies exactly five correctly bound nation cards after setup was repeated.

All three final 1920x1080 screenshots were inspected: main-menu typography/frame/separators, the Portugal selection highlight and matching large shield, and Portugal's selected ship render correctly without clipped controls or pink materials. Runtime and Editor source compilation against actual Unity assembly references passed with exit code 0. Evidence is retained in `Validation/Week8`.

Intermediate image-check runs exposed script-reimport interruption and the project's disabled background frame loop after earlier full flows had passed. The v3 runner uses a versioned request, waits for stable imports, detects lost coroutines after reload, and has an Editor watchdog/stop request. It temporarily enables Application.runInBackground and queues player-loop updates, then restores the previous background setting. The watchdog was observed returning a stalled test to Edit Mode. Runtime state is not saved and the original Map01 Editor scene is restored. Pointer checks dispatch through GraphicRaycaster/EventSystem; no physical mouse-device, standalone player build or platform quit test was performed.
