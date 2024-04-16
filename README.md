# TextMeshDOTS

TextMeshDOTS is a fork of [Latios Framework/Calligraphics](https://github.com/Dreaming381/Latios-Framework/tree/master/Calligraphics). It aims to provide DOTS native 
world space text similar to TextMeshPro and TextCore GameObject based text. To keep
cross dependencies to a minimum, we aim to only depend on unmodified [Unity Entities](https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/index.html),
[Unity Entities Graphics](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.2/manual/index.html) and some functionality in the UnityEngine.TextCore namespace to 
be able to utilize TextCore FontAssets.

# How to use
-   Create a Subscene
-   add empty gameobject
-   add TextRenderer component
-   set font asset to TextMeshDOTS/Fonts/LiberationSans SDF


#Know issues
-   Unity 6000.0.b15 apears to have bug extracting GlyphAdjustment table, which contains may non-sensical adjustment pairs between glyphs are not found in the character table 
That leads to a TextRender backing error. So when you re-generate the FontAsset, ensure you do not extract font features (GlyphAdjustment tablke need to have 0 entries).
-   the entire render functionlity (systems that consume DynamicBuffer<RenderGlyph>, and render it) is missing right now


## Special Thanks To the original authors and contributors

-   Dreaming381 -  the author of [Latios Framework](https://github.com/Dreaming381/Latios-Framework)
-   Sovogal – significant contributions to the Calligraphics module of Latios Framework (including the name)
