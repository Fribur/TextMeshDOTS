# Atlas Arrays Todo List

## Phase 1 - Glyph Generation Decoupled from Atlas and Atlas Entities - Complete

Phase 1 has been completed.

## Phase 2 - Render with Atlas Arrays

Phase 2 is currently in progress.

### Glyph Generation

- [x] Add new RenderGlyph type to entities alongside RenderGlyphOld
- [x] Populate RenderGlyph inside GlyphGeneration alongside RenderGlyphOld

### Glyph Generation Optional

- [ ] Move glyph line wrapping logic to ShapeJob


### Dispatch (DreamingImLatios owns this)

- [x] Identify RenderGlyphs to upload and advance residence state machine and material properties
- [x] Identify glyphs to generate in atlas and create list of atlas array indices to modify
- [x] Upload RenderGlyphs to GPU
- [x] Resize atlas arrays and acquire array indices native buffers
- [x] Generate glyph textures
- [x] Commit textures to GPU

### Shaders

- [x] Create new RenderGlyph reader hlsl file
- [ ] Create unified atlas shader (branch on SDF8, SDF16, and Bitmap based on glyph)

### Final Steps

- [x] Move TextShaderIndex to TextShaderIndexOld and replace with TextShaderIndex for new pipeline
- [x] Enable creation of new rendering path systems
- [x] Create temporary workflow so that each entity can pick which rendering path it uses
- [ ] Debug, readable text
- [ ] Debug, correctly rendered text
- [ ] Debug, feature parity

## Phase 3 - Jobify Font Loading and Optimize

Phase 3 has not started yet

### Cleanup

- [ ] Remove old component types from entities and temporary rendering path switching workflow
- [ ] Delete dead code
- [ ] Internalize/privatize types
- [ ] Clean up namespaces, directory structure, and code organization

### Improve

- [ ] Move glyph line wrapping logic to ShapeJob
- [ ] Double-buffer CalliByte with PreviousCalliByte for aggressive change filtering
- [ ] Jobify font loading reactively (probably needs more breakdown)
- [ ] Support sprites

### Optimize

- [ ] Optimize job scheduling bubbles
- [ ] Optimize SDF generation
- [ ] Optimize rasterization