# Week 7: Town yield and profession selection

Reference: the user's `(V.2) Colonization-Game Production Manual.docx`, blocks 1582–1901. Week 8 starts at block 1902 and is outside this change.

## Implemented scope

- Sixteen `ProductData` assets in the manual's ID order: Food, Sugar, Tobacco, Cotton, Furs, Lumber, IronOres, Silver, Horses, Rum, Cigars, Clothes, Coats, TradeGoods, Tools, Musket. Food has separate corn and fish sprites.
- Eight base resource values per terrain. Ocean `[4,0,0,0,0,0,0,0]`, Plain `[5,4,3,3,3,3,0,0]` and Grassland `[3,0,5,0,5,5,0,0]` retain the manual's examples. Other terrain rows are configurable initial choices for the design exercise; rerunning setup preserves existing eight-value arrays.
- Forest reduces every resource except Furs/Lumber by two. Furs/Lumber require forest. Town terrain yields half, rounded up. Results clamp to zero.
- The town center automatically produces Food. A surrounding Hex produces its chosen resource when assigned a worker. Dropping a worker opens the eight-profession modal; right-clicking an assigned worker reopens it to change the resource.
- 64x64 yield icons, numeric per-Hex yield, corn/fish selection, an aggregate Food row, overlap spacing that resets when counts shrink, and sixteen warehouse icon/amount displays.
- Modal input blocking, invalid-ID guards, cleanup on closing, and preserved assignments and forecasts across repeated opening and switching towns. Escape cancels a new unselected assignment; cancelling an existing profession change retains its previous resource. Escape outside the modal closes the town.
- `Town` owns production totals and recomputes them from current assignments, rather than accumulating UI opening events. This prevents double counts, negative totals, and lost production when closing the UI.

## Asset policy and milestone boundary

All sprites already existed in the project. No personal assets, artwork downloads, packages or external dependencies were imported. Coats temporarily uses the existing fur icon; Musket uses the existing repeating-rifle icon. Tools uses the existing pickaxe and TradeGoods the existing supply-goods icon. These can be replaced later without changing product IDs.

The manual's Week 7 adds production previews and warehouse data fields. It does not yet implement turn-based collection into the warehouse, food consumption, population growth, expert multipliers or conversion of raw resources into manufactured goods. Warehouse counts therefore remain unchanged by selecting or previewing professions.

## Native setup and validation

`Tools > Colonization > Apply Week 7 From Existing Assets` adds and wires the new assets through Unity APIs, preserving existing GUIDs and extending saved Map01. A saved Map01 in Edit Mode is required. `Run Week 7 Play Mode Smoke Test` first reruns Week 5/6 behavior, then tests the Week 7 interaction and arithmetic in a fresh play session. Week 4/5/6 test menus forward to the latest configured suite.

Native setup passed at 22:48:22 on 2026-10-13 (Asia/Bangkok). Week 5 and Week 6 behavioral regressions passed at 22:52:15 and 22:52:34 respectively; the map generated 59 Native towns. The final Week 7 native Play Mode suite passed at 23:14:14 after moving UI raycast sampling into the game's player loop. It exercises all eight persistent profession buttons, terrain rules, modal input blocking, assignment cancellation, two-worker totals, corn/fish variants, repeated town opening, switching towns, edge cells and both visual cleanup and warehouse-preview separation.

Both final 1920x1080 screenshots were inspected: profession labels, amounts, resource sprites, town-center Food, field Lumber and the aggregate Food row render correctly without clipped controls or pink materials. Runtime and Editor source compilation against actual Unity assemblies passed with exit code 0. The harness restores temporary input settings and returns Unity to Edit Mode. Evidence is retained in `Validation/Week7`; earlier harness failures are retained separately for diagnosis. Pointer checks use GraphicRaycaster and EventSystem dispatch, with real Input System Enter/Escape events. No standalone player build or physical mouse-device test was performed.
