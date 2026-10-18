# Generated Hex tiles — 2026-10-17

18 selected images generated with the built-in image_gen tool from the user's existing project references in `Assets Terrain  Referents.zip`.

- Assignment2: 11 cleared base terrains.
- Assignment3: 7 complete forested Hex tiles, each paired with its generated cleared base.
- PNG RGBA, 1024×1536 portrait canvas; one selected image per biome, retaining transparency and the reserved upper area.
- Forest pairs: ConiferForest → Grassland; BroadleafForest → Prairie; SavannaWoodland → Savanna; MixedForest → Plain; BorealForest → Tundra; ScrubForest → Desert; TropicalRainforest → Swamp.
- Vector-style raster illustration; these are PNGs, not editable SVG vectors.
- No Unity sprites, scenes, prefabs or import settings have been replaced. The pack is outside Assets for review. Exact seams/pivots and in-game appearance still require Unity integration validation.
- Original image-generator alpha is preserved without postprocessing. Some image viewers may display hidden RGB colors from transparent pixels; PNG alpha determines visibility.
- Alpha QA: five images have a lower-left corner sample of 1/255 alpha; other sampled corners are zero. Artwork maximum alpha is 254/255 in all images. Exact binary clipping and seam normalization have not been performed; Validation.json records this rounding tolerance explicitly.
- Validation.json records dimensions, alpha checks and SHA-256 hashes. manifest.json maps each output to its reference and original generated source. PromptSpecification.md contains the full design prompt set; manifest includes normalized generation templates.

For matching world scale, use 400 pixels per unit with these full-resolution images, or reduce to 256×384 and use the existing 100 pixels per unit. Match the existing normalized custom pivot (0.5, 0.34). These settings are preparation guidance, not a claim of completed Unity import.
