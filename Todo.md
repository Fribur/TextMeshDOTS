# Atlas Arrays Todo List

## Phase 1 - Glyph Generation Decoupled from Atlas and Atlas Entities - Complete

Phase 1 has been completed.

## Phase 2 - Render with Atlas Arrays

Phase 2 is currently in progress.

### Glyph Generation

[ ] Add new RenderGlyph type to entities alongside RenderGlyphOld
[ ] Populate RenderGlyph inside GlyphGeneration alongside RenderGlyphOld

### Glyph Generation Optional

[ ] Move glyph line wrapping logic to ShapeJob
[ ] Internalize/privatize types
[ ] Clean up namespaces and directory structure

### Dispatch (DreamingImLatios owns this)

[ ] Identify RenderGlyphs to upload and advance residence state machine and material properties
[ ] Identify glyphs to generate in atlas and create list of atlas array indices to modify
[ ] Resize atlas arrays and acquire array indices native buffers
[ ] Generate glyph textures and populate RenderGlyph upload buffers
[ ] Commit textures and buffers to GPU

### Shaders

[ ] Create new RenderGlyph reader hlsl file
[ ] Create unified atlas shader (branch on SDF8, SDF16, and Bitmap based on glyph)

## Phase 3 - Jobify Font Loading and Optimize

Phase 3 has not started yet
