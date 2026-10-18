# Hex tiles v2 — repaired framing

18 RGBA PNG files, 1024x1536. Edited with built-in image_gen to reconstruct complete lower vertices and leave clear canvas margins. Original v1 files preserved.

All 18 passed alpha >8 bounding-box clearance of at least 24 pixels on every canvas edge. Near-invisible alpha noise <=8 may remain outside the silhouette. Preview.jpg composites actual alpha onto gray.

These are raster PNGs with vector-style artwork, not SVG. Ground size and position vary between generated images; Unity pivots, footprint normalization, seamless tiling and forest/base registration still require integration work. Not imported into Unity Assets.

The edit prompt is recorded in manifest.json; Arctic was additionally reframed smaller to provide a larger bottom safety band.
