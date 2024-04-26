# TextMeshDOTS

TextMeshDOTS is a fork of [Latios Framework/Calligraphics](https://github.com/Dreaming381/Latios-Framework/tree/master/Calligraphics). 
It aims to provide DOTS native world space text similar to TextMeshPro and TextCore GameObject based text. To keep
cross dependencies to a minimum, we aim to only depend on unmodified [Unity Entities](https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/index.html),
[Unity Entities Graphics](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.2/manual/index.html) and some functionality in the `UnityEngine.TextCore` namespace to 
be able to utilize TextCore FontAssets. Possibly we use a custom BatchRenderGroup instead of Entity Graphics. 

# How to use

You can choose to render text either via a custom BatchRenderGroup (BatchDrawCommandProcedural), or via native Entities Graphics.
To do so, add [DisableAutoCreation] either to the system found in `Systems\ProceduralBRG`, or to systems in `Systems\Rendering`.
BatchDrawCommandProcedural does not appear to have any advantage compared to the Entities Graphics, so it is currently disabled.

(1) Autoring workflow
-	Generate backend Mesh: `Menue-->TextMeshDOTS-->Text Backend mesh`
-   Create a SubScene
-   When using Entities Graphics, add a dummy cube mesh (otherwise text will not be rendered...reason unknown at this time)
-   Add empty GameObject
-   Add TextRenderer component
-   Set font asset to `TextMeshDOTS/Fonts/LiberationSans SDF`, type in some text
-   Close subscene
-	Font entity will be backed to an entity containing a number of components, amongst them  `DynamicBuffer<RenderGlyph>`. This buffer contains all data required for rendering the text

(2) Runtime instantiation workflow
-   Create a SubScene
-   Add empty GameObject
-   Add "FontBlobAuthoring" component
-   Set font asset to `TextMeshDOTS/Fonts/LiberationSans SDF`
-   Close subscene
-   Modify `RuntimeTextRendererSpawner.cs` to spawn any number of TextRenderer
-   Hit play
- 

# Known issues
-   When using Entities Graphics and autoring workflow, text will not be rendered...reason unknown at this time. Can be fixed by adding a cube GameObject into the subscene.
-   Spawned text is not updated right now 
-   FontAsset switching functionality of Calligraphics has been temporarily removed


## Special Thanks To the original authors and contributors

-   Dreaming381 -  the author of [Latios Framework](https://github.com/Dreaming381/Latios-Framework)
-   Sovogal – significant contributions to the Calligraphics module of Latios Framework (including the name)
