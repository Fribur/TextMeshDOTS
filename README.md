# TextMeshDOTS

TextMeshDOTS is a fork of [Latios Framework/Calligraphics](https://github.com/Dreaming381/Latios-Framework/tree/master/Calligraphics). 
Utilizing TextCore font assets, TextMeshDOTS renders world space text similar to TextMeshPro. It leverages DOTS provided by the [Unity Entities](https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/index.html) 
package to generate the vertex data required for rendering, and uses native [Unity Entities Graphics](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.2/manual/index.html) for rendering. The HDRP and URP shader are 
wrapper/ extensions of the TextMeshPro SRP shader. TextMeshDOTS supports almost all richtext tags of [TextMeshPro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/RichText.html) and TextCore:
\<allcaps\>, \<alpha\>, \<b\>, \<color\>, \<cspace\>, \<font\>, <\i>, \<lowercase\>, \<sub\>, 
\<sup\>, \<pos\>, \<voffset\>, \<size\>, \<space=000.00\>, \<mspace=xx.x\>, \<smallcaps\>, 
<scale=xx.x>, \<rotate\>. Other tags are recognized but not yet rendered. 

# How to use

(1) Autoring workflow
  -	Generate backend mesh: `Menue-->TextMeshDOTS-->Text Backend mesh`
  -	Create a SubScene
  -	Add empty GameObject, and `TextRenderer` component on it
    - Add a dummy cube mesh. Can be inactive, however the MeshRenderer component on it needs to be active, otherwise text will not be rendered in editor mode...reason unknown at this time.
  -	Set font asset to `TextMeshDOTS/Fonts/LiberationSans SDF`, type in some text
    - when attaching multiple fonts, the rich text tag <font=name_of_font> can be used for selecting them
  -	Close subscene
  -	You should now see the text    

(2) Runtime instantiation workflow
  -	Generate backend mesh: `Menue-->TextMeshDOTS-->Text Backend mesh`
  -	Create a SubScene
  -	Add empty GameObject, add `FontBlobAuthoring` component
  -	Set font asset to `TextMeshDOTS/Fonts/LiberationSans SDF`
  -	Close subscene
  -	Modify `RuntimeTextRendererSpawner.cs` to spawn any number of TextRenderer
    - the runtime archtetype is right now setup to use single fonts. Using the authoring workflow for multiple fonts, inspects how to modify the archetype for multiple fonts 
  -	Hit play


# Known issues
-   When using Entities Graphics and authoring workflow, text will not be rendered in editor mode unless subscene also contains another GameObject having a MeshRenderer component. Reason unknown at this time. 


## Special Thanks To the original authors and contributors

-   Dreaming381 -  the author of [Latios Framework](https://github.com/Dreaming381/Latios-Framework)
-   Sovogal – significant contributions to the Calligraphics module of Latios Framework (including the name)
