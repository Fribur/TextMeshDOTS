# Atlas Arrays Todo List

## Phase 1 - Glyph Generation Decoupled from Atlas and Atlas Entities - Complete

Phase 1 has been completed.

## Phase 2 - Render with Atlas Arrays - Complete

Phase 2 is currently in progress.

### Glyph Generation

- [x] Add new RenderGlyph type to entities alongside RenderGlyphOld
- [x] Populate RenderGlyph inside GlyphGeneration alongside RenderGlyphOld

### Dispatch (DreamingImLatios owns this)

- [x] Identify RenderGlyphs to upload and advance residence state machine and material properties
- [x] Identify glyphs to generate in atlas and create list of atlas array indices to modify
- [x] Upload RenderGlyphs to GPU
- [x] Resize atlas arrays and acquire array indices native buffers
- [x] Generate glyph textures
- [x] Commit textures to GPU

### Shaders

- [x] Create new RenderGlyph reader hlsl file
- [x] Create unified atlas shader (branch on SDF8, SDF16, and Bitmap based on glyph)

### Final Steps

- [x] Move TextShaderIndex to TextShaderIndexOld and replace with TextShaderIndex for new pipeline
- [x] Enable creation of new rendering path systems
- [x] Create temporary workflow so that each entity can pick which rendering path it uses
- [x] Debug, readable text
- [x] Debug, correctly rendered text
- [x] Debug, feature parity

## Phase 3 - Jobify Font Loading and Optimize

Phase 3 is currently in progress.

### Cleanup

- [x] Remove old component types from entities and temporary rendering path switching workflow
- [x] Delete dead code
- [x] Internalize/privatize types
- [x] Clean up namespaces, directory structure, and code organization

### Improve Font Loading

- [ ] Move FontRequest buffer to blob asset with deterministic hash
- [ ] Assign this blob to each text renderer entity instead of having a singleton
- [ ] Jobify font loading
- [ ] Lazy load fonts based on what renderers actually need

### Improve Shaping and Mapping (Optional)

- [ ] Create PreviousCalliByte (not ICleanup) to reduce shaping and glyph generation ops
- [ ] Move glyph horizontal alignment and line wrapping to ShapeJob
- [ ] Create data structure to map CalliByte and RenderGlyph regions (maybe just lines?)
- [ ] Bidirectional algorithm
- [ ] Cursor positioning utilities

### Improve Rendering Capabilities (Optional)

- [ ] Properly support hi-res text and images
- [ ] Support inlined sprites
- [ ] Support highlighting
- [ ] Support underlines and strikethroughs
- [ ] Support user tagged regions
- [ ] Add more shaders and shader graph foundation subgraphs
- [ ] Support raymarched text
- [ ] Support in-panel BVH text

### Optimize (Optional-ish)

- [ ] Figure out Harfbuzz threading hazards (and maybe updatin harfbuzz fixes these?)
- [ ] Optimize texture uploads after first frame (do we need to do sparse uploads with compute?)
- [ ] Optimize job scheduling bubbles
- [ ] Optimize SDF generation
- [ ] Optimize rasterization