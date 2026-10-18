# Hex terrain generation prompts — Assignments 2 and 3

Prompt-writing only; no images generated, imported or replaced. Based on inspection of the actual project sprites, HexData mapping, prefab and import metadata.

## How to use

Attach the existing PNG listed under each prompt; mentioning a local filename alone does not give an image generator access to it. Generate one tile at a time. First approve a Grassland/Ocean style sample, then attach that approved sample with each biome's original reference to keep the new set consistent. For forest tiles, also attach the approved corresponding cleared base.

“Vector-style” describes the visual appearance. The requested Unity output is a transparent raster PNG; a true editable SVG is a separate drawing/vectorization deliverable.

The actual project sources are 256×384 PNGs, not square 256×256 images. Their ground footprint is square and starts approximately 128 px below the top. The existing custom pivot is (0.5, 0.34), and the inspected source uses 100 pixels per unit. Reducing the generated image to 256×384 allows matching those source settings. If using 1024×1536 directly, preserve the same world footprint by using 400 pixels per unit and the same normalized pivot. These are integration notes, not generation guarantees; check dimensions, alpha and alignment before importing. Do not auto-trim the transparent upper band.

The current data provides four variants per terrain/forest. One approved image per biome gives an initial 18-image set. Four compatible variants of every biome gives 44 base + 28 forest = 72 images; their silhouette, scale, light and ground-edge colors should remain identical.

## Shared prompt


```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.
```

## Full individual prompts

### Assignment 2 — Ocean

Reference: [hexOceanCalm00.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexOceanCalm00.png>)

Suggested output: `Ocean_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: Deep navy and cobalt ocean water, broad curved wave shapes with a few restrained pale-blue highlights, subtle depth variation. Water fills the entire Hex. No land, coastline, islands, ships or oversized foam.
```

### Assignment 2 — Grassland

Reference: [hexPlainsCold00 1.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexPlainsCold00 1.png>)

Suggested output: `Grassland_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: Cool medium-green fertile grassland, short grass clusters, patches of slightly darker soil and a few small rounded gray stones. Low vegetation only; no trees, crops or hills.
```

### Assignment 2 — Prairie

Reference: [hexDirt00 1.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexDirt00 1.png>)

Suggested output: `Prairie_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: Open temperate prairie with warm olive and golden grass tufts over visible muted brown soil. Broad open ground, low varied grasses, sparse small stones. Clearly drier and warmer than Grassland; no trees or cultivated crops.
```

### Assignment 2 — Savanna

Reference: [hexDesertRedGrass00 1.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexDesertRedGrass00 1.png>)

Suggested output: `Savanna_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: Open savanna with muted reddish-orange earth, sun-dried ochre and straw-yellow grass patches, sparse low green tufts and small weathered stones. Warm dry palette. This is the cleared terrain version: no trees.
```

### Assignment 2 — Plain

Reference: [hexTropicalPlains00 1.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexTropicalPlains00 1.png>)

Suggested output: `Plain_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: Broad flat plain with warm fresh-green low grasses, subtle yellow-green patches and small exposed earth shapes. A smooth open lowland distinct from the cooler stony Grassland and drier Prairie. No trees, farms or hills.
```

### Assignment 2 — Tundra

Reference: [hexPlainsColdSnowTransition00 1.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexPlainsColdSnowTransition00 1.png>)

Suggested output: `Tundra_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: Cold open tundra with desaturated gray-green moss, pale brown exposed ground, small scattered stones and a few thin irregular patches of residual snow. Sparse low vegetation, no trees and no glacier.
```

### Assignment 2 — Desert

Reference: [hexDesertYellowDirt00 1.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexDesertYellowDirt00 1.png>)

Suggested output: `Desert_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: Dry yellow-ochre desert soil and sand with shallow wind-shaped patches, small angular sandstone pieces and restrained warm brown shading. Mostly open ground, no trees, forest, water or towering dunes.
```

### Assignment 2 — Swamp

Reference: [hexWetlands00 1.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexWetlands00 1.png>)

Suggested output: `Swamp_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: Wet marshy lowland with dark olive muddy ground, small irregular shallow-water pools, reeds and low wet grasses. Pools remain inside the tile and do not form edge coastlines. No trees in this cleared version.
```

### Assignment 2 — Arctic

Reference: [hexSnowField00 1.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexSnowField00 1.png>)

Suggested output: `Arctic_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: Treeless arctic snowfield, clean ivory snow and pale icy-blue shadow shapes, subtle shallow ice cracks and a few small exposed gray rocks. Bright snow coverage across the Hex, no mountains, trees, ocean or buildings.
```

### Assignment 2 — Hills

Reference: [hexHills00.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexHills00.png>)

Suggested output: `Hills_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: A few rounded low grassy hills, distinct layered green slopes with restrained exposed brown and gray rock patches. Modest height and soft rounded silhouettes, clearly lower than Mountains. No snow-capped peaks or trees.
```

### Assignment 2 — Mountains

Reference: [hexMountain00 1.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexMountain00 1.png>)

Suggested output: `Mountains_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: A compact rugged mountain massif anchored inside the Hex: angular gray and brown rock faces, several pointed peaks, ivory snow on the highest ridges, cool blue-gray shaded slopes and a small green foothill area. Peaks may rise into the reserved transparent upper canvas; no floating island or extruded base.
```

### Assignment 3 — ConiferForest

Reference: [hexForestPine00 1.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexForestPine00 1.png>)

Suggested output: `ConiferForest_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: A full forest Hex on the exact Grassland base: densely grouped evergreen conifers, layered triangular pine silhouettes, dark forest-green foliage, olive highlights, small visible trunks and narrow ground gaps. Ground color must match the approved Grassland tile. Clearing reveals Grassland.

FOREST INTEGRATION: Produce the complete forested Hex tile, including the correct matching ground surface, as a separate transparent PNG for the existing ForestSprite layer. Use the approved cleared base tile as an additional alignment reference. Do not change that ground silhouette, angle, placement or base biome colors. Trees may overlap the top edge of the ground into the reserved upper band, but not the image boundaries. The forest is a separately switchable layer; when it is disabled in Unity, the matching cleared base from Assignment 2 must align underneath without any jump. Do not include unrelated terrain, buildings or extra Hexes.
```

### Assignment 3 — BroadleafForest

Reference: [hexForestBroadleaf00.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexForestBroadleaf00.png>)

Suggested output: `BroadleafForest_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: A full forest Hex on the exact Prairie base: rounded broadleaf tree crowns in varied muted olive and medium green, readable overlapping canopy groups, scattered trunks, occasional small rocks and visible warm prairie ground between trees. Clearing reveals Prairie.

FOREST INTEGRATION: Produce the complete forested Hex tile, including the correct matching ground surface, as a separate transparent PNG for the existing ForestSprite layer. Use the approved cleared base tile as an additional alignment reference. Do not change that ground silhouette, angle, placement or base biome colors. Trees may overlap the top edge of the ground into the reserved upper band, but not the image boundaries. The forest is a separately switchable layer; when it is disabled in Unity, the matching cleared base from Assignment 2 must align underneath without any jump. Do not include unrelated terrain, buildings or extra Hexes.
```

### Assignment 3 — SavannaWoodland

Reference: [hexDesertRedForest00 1.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexDesertRedForest00 1.png>)

Suggested output: `SavannaWoodland_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: A full woodland Hex on the exact Savanna base: widely spaced flat-topped umbrella-shaped trees and small dry bushes, dusty olive crowns, warm reddish soil and dry ochre grasses clearly visible between trees. An open woodland, not a dense jungle. Clearing reveals Savanna.

FOREST INTEGRATION: Produce the complete forested Hex tile, including the correct matching ground surface, as a separate transparent PNG for the existing ForestSprite layer. Use the approved cleared base tile as an additional alignment reference. Do not change that ground silhouette, angle, placement or base biome colors. Trees may overlap the top edge of the ground into the reserved upper band, but not the image boundaries. The forest is a separately switchable layer; when it is disabled in Unity, the matching cleared base from Assignment 2 must align underneath without any jump. Do not include unrelated terrain, buildings or extra Hexes.
```

### Assignment 3 — MixedForest

Reference: [hexWoodlands00.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexWoodlands00.png>)

Suggested output: `MixedForest_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: A full mixed-forest Hex on the exact Plain base: a balanced mixture of rounded broadleaf crowns and a smaller number of pointed conifer silhouettes, muted greens with subtle warm foliage accents, small visible clearings and matching warm-green plain ground. Clearing reveals Plain.

FOREST INTEGRATION: Produce the complete forested Hex tile, including the correct matching ground surface, as a separate transparent PNG for the existing ForestSprite layer. Use the approved cleared base tile as an additional alignment reference. Do not change that ground silhouette, angle, placement or base biome colors. Trees may overlap the top edge of the ground into the reserved upper band, but not the image boundaries. The forest is a separately switchable layer; when it is disabled in Unity, the matching cleared base from Assignment 2 must align underneath without any jump. Do not include unrelated terrain, buildings or extra Hexes.
```

### Assignment 3 — BorealForest

Reference: [hexForestPineSnowTransition00.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexForestPineSnowTransition00.png>)

Suggested output: `BorealForest_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: A full boreal taiga Hex on the exact Tundra base: slender cold-climate conifers, dark desaturated blue-green needles, lighter sparse tree groups, pale mossy ground, small rocks and restrained thin patches of snow. Avoid turning it into a completely snow-covered arctic forest. Clearing reveals Tundra.

FOREST INTEGRATION: Produce the complete forested Hex tile, including the correct matching ground surface, as a separate transparent PNG for the existing ForestSprite layer. Use the approved cleared base tile as an additional alignment reference. Do not change that ground silhouette, angle, placement or base biome colors. Trees may overlap the top edge of the ground into the reserved upper band, but not the image boundaries. The forest is a separately switchable layer; when it is disabled in Unity, the matching cleared base from Assignment 2 must align underneath without any jump. Do not include unrelated terrain, buildings or extra Hexes.
```

### Assignment 3 — ScrubForest

Reference: [hexDesertYellowCactiForest00 1.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexDesertYellowCactiForest00 1.png>)

Suggested output: `ScrubForest_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: A full desert scrub Hex on the exact Desert base: low thorny bushes, sparse dry shrubs, gray-olive foliage, warm yellow sandy ground, small stones and a few restrained cactus-like forms matching the supplied reference. Low open vegetation, no tall dense trees. Clearing reveals Desert.

FOREST INTEGRATION: Produce the complete forested Hex tile, including the correct matching ground surface, as a separate transparent PNG for the existing ForestSprite layer. Use the approved cleared base tile as an additional alignment reference. Do not change that ground silhouette, angle, placement or base biome colors. Trees may overlap the top edge of the ground into the reserved upper band, but not the image boundaries. The forest is a separately switchable layer; when it is disabled in Unity, the matching cleared base from Assignment 2 must align underneath without any jump. Do not include unrelated terrain, buildings or extra Hexes.
```

### Assignment 3 — TropicalRainforest

Reference: [hexJungle00.png](<Z:/.Project Unity/GI-334-project-assignment/Assets/Textures/Terrain/Tiles/hexJungle00.png>)

Suggested output: `TropicalRainforest_01.png`

```text
Create ONE production-ready 2D terrain Hex sprite for a Unity turn-based strategy game.

REFERENCE: Use the attached project tile as the visual reference for the silhouette, Hex footprint, viewing angle, terrain scale, muted natural palette and upper-left lighting. Redraw its visual language as clean vector-style artwork: crisp closed shapes, controlled layered color regions, simplified organic forms and restrained two-to-four-tone shading. Preserve the reference's terrain identity and readable depth. Do not imitate its raster noise. This is an original vector-style interpretation, not a screenshot or a 3D render.

CANVAS AND ALIGNMENT: Portrait 2:3 canvas, 1024 x 1536 px, intended to be reduced to the project's existing 256 x 384 sprite canvas. One point-up Hex with top and bottom vertices and vertical left/right sides. Match the supplied project footprint, not a rotated or geometrically regular replacement Hex. The ground occupies the lower 1024 x 1024 region, approximately these vertices: (512,512), (1023,768), (1023,1280), (512,1535), (0,1280), (0,768). Keep the same ground position, width, height and edges in every biome. Do not center the ground in the whole portrait canvas, add padding, trim transparency or crop to content. Trees and mountain peaks may extend upward into the reserved upper band; their bases remain inside the Hex and nothing extends beyond its left, right or bottom boundaries.

ART DIRECTION: A slightly elevated oblique strategy-map view matching the reference, with upright trees and mountains. Clean illustrated vector shapes, natural olive greens, earthy ochres and browns, cool gray stone and deep ocean blues as appropriate. Consistent soft upper-left lighting and restrained lower-right shading inside the tile. Crisp readable forms at 256 px tile width. Flat ground surface with a subtle consistent edge; no thick outline, bevel, pedestal, visible rock slab or floating-island extrusion. Ground must reach all six edges so neighboring tiles can meet without transparent cracks. Do not add a terrain-specific border or shoreline unless requested.

OUTPUT: Exactly one isolated sprite per image, true transparent RGBA PNG. All unused canvas areas must have zero alpha. No white or colored background, fake transparency checkerboard, ground plane, horizon, sky, environmental backdrop, outer drop shadow or glow. No text, numbers, labels, grid, UI, watermark, logo, characters, animals, buildings, roads, resource icons or additional Hexes. No sprite sheet, contact sheet, photorealism, pixel art, blurry edges or excessive tiny detail. Retain the same composition and scale as the approved tile set.

TERRAIN SUBJECT: A full tropical-rainforest Hex on the exact Swamp base: dense layered broadleaf canopy, large rounded tropical crowns, a few palm-like fronds, dark emerald and olive greens with yellow-green highlights, vines and small glimpses of damp ground. Dense and humid, distinct from temperate BroadleafForest. Clearing reveals Swamp.

FOREST INTEGRATION: Produce the complete forested Hex tile, including the correct matching ground surface, as a separate transparent PNG for the existing ForestSprite layer. Use the approved cleared base tile as an additional alignment reference. Do not change that ground silhouette, angle, placement or base biome colors. Trees may overlap the top edge of the ground into the reserved upper band, but not the image boundaries. The forest is a separately switchable layer; when it is disabled in Unity, the matching cleared base from Assignment 2 must align underneath without any jump. Do not include unrelated terrain, buildings or extra Hexes.
```

## Compatible variations

Append this after an individual prompt to request a further version, one image per request:

```text
Create variation 02 of this approved biome tile. Preserve the exact canvas, ground Hex silhouette, placement, edge colors, viewing angle, lighting, palette and visual density. Change only the arrangement of interior grass patches, rocks, waves or tree groups. Keep the biome unmistakably the same. One separate transparent PNG, no sprite sheet.
```

Repeat for 03 and 04. Inspect a tiled map preview before replacing project sprites: prompting cannot guarantee pixel-exact edges or matching alpha between separate generations.
