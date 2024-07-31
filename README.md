# TextMeshDOTS

TextMeshDOTS is a standalone text package for DOTS, forked from [Latios Framework/Calligraphics](https://github.com/Dreaming381/Latios-Framework/tree/master/Calligraphics). 
Utilizing TextCore font assets, TextMeshDOTS renders world space text similar to TextMeshPro. It leverages the [Unity Entities](https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/index.html) 
package to generate the vertex data required for rendering, and uses native [Unity Entities Graphics](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.2/manual/index.html) for rendering. The HDRP and URP shader are 
wrapper around the TextMeshPro 4.0 SRP shader. TextMeshDOTS supports almost all rich text tags of [TextMeshPro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/RichText.html) and TextCore:
\<allcaps\>, \<alpha\>, \<b\>, \<color\>, \<cspace\>, \<font\>, \<i>, \<lowercase\>, \<sub\>, 
\<sup\>, \<pos\>, \<voffset\>, \<size\>, \<space=000.00\>, \<mspace=xx.x\>, \<smallcaps\>, 
<scale=xx.x>, \<rotate\>. Other tags are recognized but not yet rendered. 

# How to use

(1) Autoring workflow
  -	Generate backend mesh: `Menue-->TextMeshDOTS-->Text BackendMesh`
    - this only needs to be done once in a given project
  -	Create a `SubScene`
  -	Add empty `GameObject`, and `TextRenderer` component on it
  - Create a Font Asset. You can use `TextMeshDOTS/Fonts/LiberationSans SDF`. If you create one yourself, 
use the normal TextCore workflow to create a font asset, 
populate the atlas in `static` mode, and ensure the material shader is set to 
`TextMeshDOTS/Shader/TextMeshDOTS-URP` (or `-HDRP`)
 -	Add font asset `TextMeshDOTS/Fonts/LiberationSans SDF` to the list of fonts of the `TextRenderer`, or add the font assets you created
 -  When attaching multiple fonts, the rich text tag <font=name_of_font> can be used for selecting them
 -  Type in some text or rich text
  -	Close the `SubScene`
  -	You should now see the text    

(2) Runtime instantiation workflow
  -	Generate backend mesh: `Menue-->TextMeshDOTS-->Text BackendMesh`
    - this only needs to be done once in a given project
  -	Create a `SubScene`
  -	Add empty `GameObject`, add `FontBlobAuthoring` component on it
  -	Add font asset `TextMeshDOTS/Fonts/LiberationSans SDF` to the list of fonts (and any additional fonts you like to access during runtim)
  -	Close the `SubScene`
  -	Enable auto creation and modify `TextMeshDOTS/RuntimeSpawner/RuntimeSingleFontTextRendererSpawner.cs` or `RuntimeMultiFontTextRendererSpawner.cs` to spawn any number of `TextRenderer` entities. Per default, auto creation of both systems is disabled. When auto creation is enabled, which spawner system runs depends on how many fonts are added to `FontBlobAuthoring`
  -	Hit play


# Known issues
-   None at this time


## Special Thanks To the original authors and contributors

-   Dreaming381 - not only has he created the amazing [Latios Framework](https://github.com/Dreaming381/Latios-Framework), including the Calligraphics text module, but has also been of tremendous support in figuring out how to create a standalone version of Calligraphics that uses Entity Graphics instead of the Kinemation rendering engine. 
-   Sovogal – significant contributions to the Calligraphics module of Latios Framework (including the name)
