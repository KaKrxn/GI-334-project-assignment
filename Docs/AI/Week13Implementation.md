# Week 13 implementation

Reference: `(V.2) Colonization-Game Production Manual.docx`, blocks 3126–3407 (final tutorial week). All 90 embedded images were extracted outside Assets and reviewed in eight contact sheets; detailed purchase, prices, scroll, orders, AI and introduction references were also inspected individually. Existing Week 8–12 work is preserved. This milestone uses existing project artwork and adds no packages or external/personal artwork.

## Purchase and train

- Europe PURCHASE opens a 1150×1080 panel with five 1150×90 ship rows. The existing ship art replaces the absent downloaded merchant/wood art. A 1150×600 lower table and transparent upper area retain the current gold header. The question and the five prices match the reference: Caravel 1000, Merchantman 2000, Galleon 3000, Privateer 2000, Frigate 5000. Man O' War is not offered.
- Each successful purchase creates the selected data type, charges its configured price once, registers the original hidden ship with the player and Europe dock lists, selects it and updates its actual two-to-six cargo holds. Failed affordability/invalid-ID requests leave gold and ownership unchanged. Docked purchased ships are off-map and inactive, and use the existing three-turn voyage/return lifecycle rather than being recreated as default Caravels.
- TRAIN opens the 800×950 Royal University panel with its 800×160 header and 800×775 scroll area. A native vertical ScrollRect, masked viewport, fitted content and vertical scrollbar expose ten 120px rows. The initial visible order/prices match the reference: Fishermen 800, Farmers 900, Ore Miners 1000, Lumberjacks 1100, Hardy Pioneers 1400, Master Carpenters 1600 and Veteran Soldiers 1900. The text allows further available specialists; Blacksmiths 2000, Seasoned Scouts 1800 and Free Colonists 600 are configurable additional defaults. Native units are not offered.
- Training creates an owned inactive LandUnit in `Hidden` status, adds it to the waiting queue, charges only successful purchases and creates its waiting icon. Existing Fishermen/Scout art substitutes from earlier weeks remain. Recruit remains disabled: the manual creates its button but implements only Purchase and Train.
- `Hidden` and `ToBoard` append to the existing enum without changing prior serialized numeric values. Hidden units have no CurHex or map occupancy; this avoids the tutorial snippet's visible `(0,0)` placeholder occupying a real playable Hex.

## Orders, passengers and status

- Right-click an eligible idle/hidden/waiting UnitDrag in Europe or an owned town to open the 500×250 OrderPanel. Its 500×60 status header and 500×195 selection area provide None, Board the next ship and Move to the front. The existing terrain-worker right-click profession selector is preserved.
- None cancels the boarding order. Board the next ship marks ToBoard and displays the 64×64 ship status icon at (-28,-48). Move to the front reorders the authoritative Europe/town list and rebuilds matching icons. Europe icons do not start town-worker drags. Closing menus/panels cancels and cleans UI without changing queued orders.
- When a docked ship leaves Europe, ordered passengers board in queue order. Actual nonempty product stacks plus passengers share the ship's hold capacity for this departure. Empty sparse hold placeholders consume no capacity; overflow waits for a subsequent ship. Existing New World boarding retains its earlier passenger berth model.
- Arriving in Europe disembarks original passenger objects into the waiting queue with None status, clears their transport links and reparents them to their faction without destroying/recreating them. Arriving at an owned colony port boards eligible ToBoard land units using existing reservation/capacity rules. Waiting icons refresh after boarding and on reopen.
- The LandUnit prefab now has a status renderer and three existing sprites: Tools for clearing, Carpenter for building, Caravel for boarding. Clearing/building updates show the matching map status; leaving those states hides it. UnitDrag uses the ship entry for its boarding order overlay.

## Destination AI

- Other European starting ships receive the rightmost land destination in the same row within the tutorial's ocean-edge search range. The target remains on land; the ship approaches the shore through ocean Hexes.
- HexCalculator follows the manual's three preferred directions for each relative destination quadrant. It checks nulls/map boundaries, movement cost and land/ocean compatibility. Legal moves costing exactly the remaining points are accepted consistently with existing movement; the tutorial's strict `<` comparison would unnecessarily strand one point on ocean.
- AI uses its destination when present, otherwise the previous single random-neighbor move. Arrival chains destination moves within the current movement budget. AIManager retains the active unit until that chain finishes, then advances; no player handoff occurs during animations. Targets remain available for subsequent turns if blocked or points run out. This is the tutorial's bounded directional movement, not an added pathfinding framework.
- AI landfall/settlement strategy remains outside this milestone, explicitly identified as unfinished in manual block 3364. Fog and authored terrain remain enabled/preserved.

## Introduction and integration

- SelectNation Start Game now opens BlackPanel and KingsCommandPanel, then PrepareToLeavePanel, then LeftThePortPanel. The final `>>` loads Map01. Native persistent button references wire all transitions. Selection/Back and direct premature StartGame calls cannot change the selected nation or skip the active introduction.
- Fullscreen panels use the existing Art Deco Europe background, selected nation shield and Caravel artwork, plus readable mission text consistent with Week 12. Downloaded narrative illustrations are absent and are not imported. The selected nation persists through all three screens and normal map initialization.
- Existing Scene/Prefab/SO GUIDs and authored objects are preserved. `Tools/Colonization/Apply Week 13 From Existing Assets` performs native serialized changes in saved Map01, the UnitDrag/LandUnit prefabs, prices and SelectNation, then returns to Map01. It refuses unsaved/Play Mode setup.
- New runtime files: `GameManager.EuropeUnits.cs`, `UIManager.EuropeUnits.cs`. Existing EuropeManager, Unit/LandUnit/NavalUnit, UnitDrag, UIManager, GameManager, HexCalculator, AIManager and SelectNation are extended. The earlier public behavior and serialized field names remain available.

## Validation

Status: **Ready with limitations** — requested Week 13 behavior and native setup passed. Final results and nine inspected 1920×1080 screenshots are retained in `Docs/AI/Validation/Week13`. No standalone player build or physical mouse-device test is requested or claimed. Keyboard scenarios use Input System events; pointer/drag scenarios use GraphicRaycaster/Physics2DRaycaster and EventSystem handlers against the actual configured controls.

The complete Week 5–13 chain passed at **20:03:24 on 2026-10-14 (Asia/Bangkok)**. After a small presentation correction (keep gold visible under Purchase and label idle units as “No orders”), native setup and compilation passed again; the entire focused Week 13 scenario passed at **20:06:38**. Runtime and Editor source compilation against actual Unity assembly references both returned exit 0; native Editor imports and Play Mode also succeeded. Expected serialized-field CS0649 warnings remain, with no new unresolved runtime/compilation errors in the final runs.

| Criterion | Evidence | Result |
|---|---|---|
| Native saved references, prefabs and prices | NativeSetup.result.txt, source compilation | Passed |
| Five ship buttons, selected types/holds and exact charges | Week13SmokeTest.result.txt; Purchase.png | Passed |
| Ten scrollable specialist buttons and exact charges | Week13SmokeTest.result.txt; Train.png, Waiting.png | Passed |
| Insufficient funds, invalid IDs and no off-map occupancy | Week13SmokeTest.result.txt | Passed |
| All three orders, queue reordering, icons and full/sparse hold capacity | Week13SmokeTest.result.txt; Orders.png, Boarding.png | Passed |
| Three-turn outbound voyage and trained passenger arrival | Week13SmokeTest.result.txt | Passed |
| Town orders and animated arrival boarding | Week13SmokeTest.result.txt | Passed |
| Six-turn round trip, original identity, disembarkation and cleanup | Week13SmokeTest.result.txt; Returned.png | Passed |
| Destination AI, bounded points, edges/null/reached targets, passenger sync and handoff | Week13SmokeTest.result.txt and Week5 regression | Passed |
| Three sequential introductions for all five nations | Week8SmokeTest.result.txt; KingsCommand.png, PrepareToLeave.png, LeftThePort.png | Passed |

Fresh regressions passed before the dedicated scenario: Week 5 at 19:55:48 (53 Native towns), Week 6 at 19:56:06, Week 7 at 19:56:24, Week 8 at 19:57:03, Week 9 at 19:58:03, Week 10–11 at 20:00:15 and Week 12 at 20:01:08. The Week 5 fixture now checks destination-unit movement against its full budget while retaining the one-neighbor assertions for random Native movement. The Week 8 fixture now clicks and checks each required introduction instead of expecting an immediate map transition. These changes validate the new milestone behavior; no runtime feature is disabled.

Both source assemblies compile, temporary keyboard/background settings are restored and the final Editor has only saved Map01 open in Edit Mode. No Week 13 ProjectSettings changes were added. All Week 8–13 changes remain uncommitted; no commit or push was requested in this turn.

Initial diagnostics are retained separately: the first native setup encountered Unity's editor fake-null SpriteRenderer wrapper; using Unity's object comparison before AddComponent corrected it. The first focused scenario passed purchasing, all ten training buttons, all orders, boarding capacity, outbound travel, AI and town boarding, then attempted a naval Yes click before the newly enabled UI had drawn. A separate drawn-frame phase corrects the harness timing without changing naval behavior. Screenshot phases also wait before subsequent UI mutations.
