using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;

// If you are providing a frontend, listen up!
// Your objective is to query for all entities with two components:
// RenderGlyphOld and TextRenderControl.
// Write to them in PresentationSystemGroup before KinemationRenderUpdateSuperSystem
// which is inside UpdatePresentationSystemGroup.
// If you change anything on any of the RenderGlyphs, or resize the buffer,
// you must add the Dirty flag to TextRenderControl. The other flags are optional
// and are simply there so you can defer some calculations to the GPU (they're effectively free).
// For baking, you must bake whatever data you require to populate the RenderGlyphs.
// In the namespace Latios.Kinemation.TextBackend.Authoring, call the IBaker extension
// method BakeTextBackendMeshAndMaterial() to set up the rendering side. This will add
// the required RenderGlyphOld and TextRenderControl components as well as internal rendering
// components.
//
// How it works:
// In the Kinemation Resources directory, there is a special Mesh baked which contains dummy
// vertex attributes, and has multiple submeshes. Each submesh contains the triangle vertex indices
// for various glyph counts. Inside KinemationRenderUpdateSuperSystem, for any TextRenderControl
// with the dirty flag, Kinemation will set the appropriate submesh on the entity's MaterialMeshInfo.
// It will also recalculate the RenderBounds (local-space bounds).
// In the culling loop, Kinemation will update any visible text glyphs to an upload GraphicsBuffer.
// The uploaded glyphs need to be transferred to a persistent GraphicsBuffer which is done via
// a ComputeShader. Because this ComputeShader is primarily a memory-transfer operation, most of the
// time the GPU is doing NOPs waiting on memory. We try to replace those NOPs with useful calculations
// that the CPU would otherwise have to do, such as color space conversion. The culling loop will also
// set the TextShaderIndex material property.
// Lastly, the Latios Text Shader Graph node will parse the glyphs from the persistent buffer based on
// the vertex ID. If the vertex ID does not map to any glyph (because the string is short), the node
// will instead return a vertex that the GPU will discard based on the same mechanism VFX Graph uses.
// Otherwise, the returned vertex will contain all the information the vertex needs to provide to
// TextMeshPro. The glyph stays compressed in its 96 byte form on the GPU and is decoded directly in
// the vertex shader.

namespace TextMeshDOTS.Rendering
{
    /// <summary>
    /// The glyphs to be rendered based on the processed CalliByte buffer.
    /// Copy this buffer to AnimatedRenderGlyph to apply animation to the data.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct RenderGlyph : IBufferElementData
    {
        public float2 blPosition;
        public float2 brPosition;
        public float2 tlPosition;
        public float2 trPosition;

        public float2 blUVB;
        public float2 brUVB;
        public float2 tlUVB;
        public float2 trUVB;

        public half4 blColor;
        public half4 brColor;
        public half4 tlColor;
        public half4 trColor;

        // These should be normalized relative to the padded bounding box extents of [0, 1]
        // The uploader will patch these with the atlas coordinates using math.lerp()
        public float2 blUVA;
        public float2 trUVA;

        public uint arrayIndex;  // Converted to float in upload shader
        public uint glyphEntryId;
        public float scale;
        public uint reserved;
    }

    /// <summary>
    /// When this buffer is present, it overrides the RenderGlyph buffer for rendering purposes.
    /// Copy the RenderGlyph buffer into this buffer and then modify the glyphs for animation purposes
    /// within AnimateGlyphsSuperSystem.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct AnimatedRenderGlyph : IBufferElementData
    {
        public RenderGlyph glyph;
    }

    [MaterialProperty("_TextShaderIndexOld")]
    public struct TextShaderIndexOld : IComponentData
    {
        public uint firstGlyphIndex;
        public uint glyphCount;
    }

    [MaterialProperty("_TextShaderIndex")]
    public struct TextShaderIndex : IComponentData
    {
        public uint firstGlyphIndex;
        public uint glyphCount;
    }

    internal struct GpuState : IComponentData, IEnableableComponent  // Enabled to request dispatch
    {
        internal enum State : byte
        {
            Uncommitted,
            Dynamic,
            DynamicPromoteToResident,
            Resident
        }
        internal State state;
    }

    [InternalBufferCapacity(0)]
    internal struct PreviousRenderGlyph : ICleanupBufferElementData
    {
        public RenderGlyph glyph;
    }

    internal struct ResidentRange : ICleanupComponentData
    {
        public uint start;
        public uint count;
    }

    internal partial struct NewEntitiesArrays : ICollectionComponent
    {
        public NativeArray<Entity> newGlyphEntities;
        public uint lastTouchedGlobalSystemVersion;

        public JobHandle TryDispose(JobHandle inputDeps) => inputDeps;
    }

    // Only present if there are child fonts
    [MaterialProperty("_TextMaterialMaskShaderIndex")]
    public struct TextMaterialMaskShaderIndex : IComponentData
    {
        public uint firstMaskIndex;
    }

    /// <summary> 96 byte glyph data </summary>
    [InternalBufferCapacity(0)]
    public struct RenderGlyphOld : IBufferElementData
    {
        public float2 blPosition;   //0
        public float2 trPosition;   //8   
        public float2 blUVA;        //16
        public float2 trUVA;        //24

        public float2 blUVB;        //32
        public float2 tlUVB;        //40
        public float2 trUVB;        //48      
        public float2 brUVB;        //56

        // Assign a UnityEngine.Color32 to these.
        public PackedColor blColor; //64
        public PackedColor tlColor; //68
        public PackedColor trColor; //72
        public PackedColor brColor; //76

        public uint  glyphID;        //80 not needed anywhere-->remove from struct?
        public float shear;         //84 Should be equal to topLeft.x - bottomLeft.x
        public float scale;         //88
        public float rotationCCW;   //92
    }

    public struct PackedColor
    {
        public uint packedColor;

        public uint a
        {
            get => packedColor >> 24;
            set => packedColor = (packedColor & 0x00ffffff) | (value << 24);
        }

        public uint b
        {
            get => (packedColor >> 16) & 0xff;
            set => packedColor = (packedColor & 0xff00ffff) | (value << 16);
        }

        public uint g
        {
            get => (packedColor >> 8) & 0xff;
            set => packedColor = (packedColor & 0xffff00ff) | (value << 8);
        }

        public uint r
        {
            get => packedColor & 0xff;
            set => packedColor = (packedColor & 0xffffff00) | value;
        }

        public static implicit operator PackedColor(UnityEngine.Color32 unityColor)
        {
            uint result                           = (uint)unityColor.a << 24;
            result                               |= (uint)unityColor.b << 16;
            result                               |= (uint)unityColor.g << 8;
            result                               |= (uint)unityColor.r;
            return new PackedColor { packedColor  = result };
        }

        public static implicit operator UnityEngine.Color32(PackedColor packedColor)
        {
            return new UnityEngine.Color32
            {
                r = (byte)(packedColor.packedColor & 0xff),
                g = (byte)((packedColor.packedColor >> 8) & 0xff),
                b = (byte)((packedColor.packedColor >> 16) & 0xff),
                a = (byte)((packedColor.packedColor >> 24) & 0xff)
            };
        }
    }

    public struct TextRenderControl : IComponentData
    {
        public enum Flags : byte
        {
            None = 0,
            Dirty = 1 << 0,
        }

        public Flags flags;
    }

    /// <summary>
    /// An additional rendered text entity containing a different font and material.
    /// The additional entity shares the RenderGlyphOld buffer, and uses a mask to identify
    /// the glyphs to render.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct AdditionalFontMaterialEntity : IBufferElementData
    {
        public Entity entity;
    }

    /// <summary>
    /// A per-glyph index into the font and material that should be used to render it.
    /// Index 0 is this entity. Index 1 is the first entity in AdditionalFontMaterialEntity buffer.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct FontMaterialSelectorForGlyph : IBufferElementData
    {
        public byte fontMaterialIndex;
    }

    /// <summary>
    /// A buffer that should be present on every entity posessing or referenced by the
    /// AdditionalFontMaterialEntity buffer. This buffer contains the GPU mask representation,
    /// and its contents will automatically be maintained by the Calligraphics rendering backend.
    /// Public so that you can add/remove it or maybe even read it (if you are brave).
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct RenderGlyphMask : IBufferElementData
    {
        public uint lowerOffsetUpperMask16;
    }
    #region components required by TextRenderingUpdateSystem and TextRenderingDispatchSystem
    //These singleton components will be added to TextRenderingUpdateSystem in OnCreate()
    internal struct TextStatisticsTag : IComponentData { }
    internal struct GlyphCountThisFrame : IComponentData
    {
        public uint glyphCount;
    }
    internal struct MaskCountThisFrame : IComponentData
    {
        public uint maskCount;
    }
    #endregion
}

