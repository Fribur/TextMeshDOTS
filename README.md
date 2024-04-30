# TextMeshDOTS

TextMeshDOTS is a fork of [Latios Framework/Calligraphics](https://github.com/Dreaming381/Latios-Framework/tree/master/Calligraphics). 
Utilizing TextCore font assets, TextMeshDOTS renders world space text similar to TextMeshPro. It leverages DOTS provided by the [Unity Entities](https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/index.html) 
package to layout the text, and uses native [Unity Entities](https://docs.unity3d.com/Packages/com.unity.entities@1.2/manual/index.html) for rendering it. The HDRP and URP shader are 
wrapper/ extension of the TextMeshPro SRP shader. TextMeshDots supports almost all richtext tags of [TextMeshPro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/RichText.html) and TextCore:
\<allcaps\>, \<alpha\>, \<b\>, \<color\>, \<cspace\>, \<font\>, <\i>, \<lowercase\>, \<sub\>, 
\<sup\>, \<pos\>, \<voffset\>, \<size\>, \<space=000.00\>, \<mspace=xx.x\>, \<smallcaps\>, 
<scale=xx.x>, \<rotate\>. Other tags are recognized but not yet rendered. 
# How to use

(1) Autoring workflow
-	Generate backend mesh: `Menue-->TextMeshDOTS-->Text Backend mesh`
-   Create a SubScene
-   Add a dummy cube mesh (otherwise text will not be rendered...reason unknown at this time.)
-   Add empty GameObject, and TextRenderer component on it
-   Set font asset to `TextMeshDOTS/Fonts/LiberationSans SDF`, type in some text
-   when attaching Multiple fonts, the richtexttag <font=name_of_font> and </font> can be used for selecting them
-   Close subscene
-	You should now see the text    

(2) Runtime instantiation workflow
-   Create a SubScene
-   Add empty GameObject
-   Add "FontBlobAuthoring" component
-   Set font asset to `TextMeshDOTS/Fonts/LiberationSans SDF`
-   Close subscene
-   Modify `RuntimeTextRendererSpawner.cs` to spawn any number of TextRenderer
-   Hit play


# Known issues
-   When using Entities Graphics and authoring workflow, text will not be rendered unless subscene also contains another GameObject that will be rendered by Entity Graphics once backed. Reason unknown at this time. 


## Special Thanks To the original authors and contributors

-   Dreaming381 -  the author of [Latios Framework](https://github.com/Dreaming381/Latios-Framework)
-   Sovogal – significant contributions to the Calligraphics module of Latios Framework (including the name)
