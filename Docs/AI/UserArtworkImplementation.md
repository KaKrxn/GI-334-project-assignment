# User artwork integration — 2026-10-18

Source ZIPs supplied by the user: Colonial_Units_20_PNG.zip, Colonial_Naval_Units_6_PNG.zip, Week6_Native_American_8_Nations.zip. Source contents are retained unmodified under Art/UserAssets/2026-10-18. Included notes are reference material, not executable instructions.

## Suitability and scope

All fifty distinct images decode as RGBA PNG with transparent pixels and opaque or nearly opaque subject pixels. The archives include high-resolution originals and duplicate 256-pixel unit/ship exports. The visual contact sheet was inspected on gray using actual alpha. Subjects are readable and suitable for the existing static SpriteRenderer/uGUI presentation. Some alpha bounding boxes reach source edges; this check does not guarantee recoverable off-canvas detail or animation readiness. No image content was regenerated or manually retouched.

Imported original-resolution unit and ship PNGs, plus native PNGs, into Assets/Textures/UserArtwork. Unity generated metadata; IDs and original asset files were preserved. Sprite importer uses full rectangles, centered pivots, bilinear filtering, clamped edges, no mipmaps, NPOT dimensions preserved, 2048 max size and high-quality compression. PPU derives from native PNG dimensions rather than first-pass import dimensions.

Land sprites retain a 10.24-world-unit canvas before the existing prefab body scale 0.15625; naval sprites retain a 1.28-unit canvas before scale 1.25. Both render at the previous 1.6-unit canvas height. Village canvases are 2.56 units, matching the existing town scale; flag maximum extent is 5.12 units, matching authored flag scaling. No prefab transforms, collider sizes, stats, prices, IDs or runtime gameplay logic were changed. LandUnit and NavalUnit prefab preview body sprites were updated using PrefabUtility.

## Active mapping

The first twelve numbered land images map in order onto the twelve existing LandUnitData entries. Soldier maps to VeteranSoldiers; Scouts maps to SeasonedScouts. Fishermen now have their supplied Fisherman image instead of the previous colonist substitute. Six numbered ship images map in order onto Caravel, Merchantman, Galleon, Privateer, Frigate and ManOWar.

Eight additional land images (Fur Trapper, Fur Trader, Cotton Planter, Cotton Weaver, Missionary, Statesman, Gunsmith, Criminal) are imported reserves. No unsupported unit classes or profession mechanics were added.

Four matching Native sets are active: Wendat → Huron, Mexica → Aztec, Onondaga → Iroquois, Inca → Inca. Village, leader and flag references are wired; the flag also supplies the faction marker/shield field. Existing display names and enum values remain. Taino, Powhatan, Mapuche and Wampanoag are imported reserves because they do not match the remaining Sioux/Tupi/Apache/Maya entries. An optional user question about changing the nation roster was unanswered during integration; the conservative default keeps the manual's roster.

## Validation

After roster migration, native import passed at 02:28:25 on 2026-10-18. Focused Play Mode validation passed at 02:28:39: all twelve land and six naval prefabs initialize with supplied sprites and 1.6-unit renderer canvas heights; all eight Native faction names/leaders and fifty-eight generated villages match their new data. The rendered unit gallery was visually inspected. Unity compiled the editor tooling and returned to Edit Mode; runtime test objects were discarded. No standalone player build or full gameplay regression chain was run for this art replacement.

Evidence: Validation/UserArtwork/Import.result.txt, PlayMode.result.txt and UnitGallery.png. Reapply with Tools/Colonization/Apply User Unit and Native Artwork. Older milestone setup menus intentionally restore their original art choices, so rerun the user-art setup if using those menus later.
