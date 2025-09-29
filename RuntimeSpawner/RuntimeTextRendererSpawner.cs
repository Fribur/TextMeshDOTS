using TextMeshDOTS.HarfBuzz;
using TextMeshDOTS.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

// Use this system for spawning new TextRender at runtime

namespace TextMeshDOTS
{
    [BurstCompile]
    //[DisableAutoCreation]
    partial struct RuntimeTextRendererSpawner : ISystem
    {
        int frameCount;
        EntityArchetype textRenderArchetype;
        TextBaseConfiguration textBaseConfiguration;
        RenderFilterSettings renderFilterSettings;
        MaterialMeshInfo materialMeshInfo;
        FixedString512Bytes text1, text2, text3, text4, kerningTest;

        public void OnCreate(ref SystemState state)
        {
            textRenderArchetype = TextMeshDOTSArchetypes.GetSingleFontTextArchetype(ref state);
            textBaseConfiguration = new TextBaseConfiguration
            {
                defaultFontFamilyHash = TextHelper.GetHashCodeCaseInSensitive("Noto Sans Display"),
                fontSize = (half)12,
                color = Color.white,                
                maxLineWidth = 30,
                lineJustification = HorizontalAlignmentOptions.Left,
                verticalAlignment = VerticalAlignmentOptions.TopBase,
                isOrthographic = false,
                fontStyles = FontStyles.Normal,
                fontWeight = FontWeight.Normal,
                fontWidth = FontWidth.Normal, 
                wordSpacing = (half)0,
                lineSpacing = (half)0,
                paragraphSpacing = (half)0,
            };
            var layer = 1;
            renderFilterSettings = new RenderFilterSettings
            {
                Layer = layer,
                RenderingLayerMask = (uint)(1 << layer),
                ShadowCastingMode = ShadowCastingMode.Off,
                ReceiveShadows = false,
                MotionMode = MotionVectorGenerationMode.ForceNoMotion,
                StaticShadowCaster = false,
            };
            materialMeshInfo = new MaterialMeshInfo { MaterialID = BatchMaterialID.Null, MeshID = BatchMeshID.Null};
            text1 = "äáà aâa aâ̈a bb̂b bb̂̈b bb̧b bb͜b bb︠︡b Tota persona té dret a l'educació. L'educació serà gratuïta, si més no, en la instrucció elemental i fonamental. La instrucció elemental serà obligatòria.";
            text2 = "The quick brown fox jumps over the lazy dog\n ¶";
            text3 = "Test 123";
            text4 = "ZYX";
            kerningTest = "WAVES in my Yard YAWN AT MY LAWN Toyota AWAY PALM";


            state.RequireForUpdate<RuntimeFontMaterial>();
            state.RequireForUpdate<FontTable>();
        }
        public void OnDestroy(ref SystemState state)
        {           
        }

        public void OnUpdate(ref SystemState state)
        {
            var runtimeFontMaterial = SystemAPI.GetSingleton<RuntimeFontMaterial>();
            if (runtimeFontMaterial.batchMaterialID == BatchMaterialID.Null)
                return;
            else if(materialMeshInfo.MaterialID ==  BatchMaterialID.Null)
                materialMeshInfo = new MaterialMeshInfo { MaterialID = runtimeFontMaterial.batchMaterialID, MeshID = runtimeFontMaterial.batchMeshID };

            if (frameCount == 10)
                SpawnText(text2, "Noto Sans Display", 30,  new float3(-10, 7, 0), Color.goldenRod, ref state);

            if (frameCount == 50)
                SpawnTextArray(text3, "Arial", 50, 3, Color.blue, 3.0f, ref state);            

            if (frameCount == 100)
                SpawnTextArray(text4, "Arial", 15, 10, Color.red, 2.0f, ref state);

            frameCount++;
        }        
        void SpawnTextArray(FixedString512Bytes text, FixedString128Bytes fontFamily, int count, float maxLineWidth, Color textcolor, float spreadFactor, ref SystemState state)
        {
            int half = count / 2;
            textBaseConfiguration.defaultFontFamilyHash = TextHelper.GetHashCodeCaseInSensitive(fontFamily);
            textBaseConfiguration.color = textcolor;
            textBaseConfiguration.maxLineWidth = maxLineWidth;
            var entities = state.EntityManager.CreateEntity(textRenderArchetype, count * count, state.WorldUpdateAllocator);
            for (int x = 0; x < count; x++)
            {
                for (int y = 0; y < count; y++)
                {
                    var entity = entities[x * count + y];
                    state.EntityManager.SetSharedComponent(entity, renderFilterSettings);
                    var calliByteBuffer = state.EntityManager.GetBuffer<CalliByte>(entity);
                    var calliString = new CalliString(calliByteBuffer);
                    //string text = i.ToString() + j.ToString();
                    calliString.Append(text);

                    state.EntityManager.SetComponentData(entity, textBaseConfiguration);
                    state.EntityManager.SetComponentData(entity, LocalTransform.FromPosition(new float3((x - half) * spreadFactor - 1, (y - half) * spreadFactor - 1, 0)));
                    state.EntityManager.SetComponentData(entity, materialMeshInfo);
                }
            }
            Debug.Log($"spawned {count} instances of {text}");
        }
        void SpawnText(FixedString512Bytes text, FixedString128Bytes fontFamily, float maxLineWidth, float3 position, Color textcolor,  ref SystemState state)
        {
            textBaseConfiguration.defaultFontFamilyHash = TextHelper.GetHashCodeCaseInSensitive(fontFamily);
            textBaseConfiguration.color = textcolor;
            textBaseConfiguration.maxLineWidth = maxLineWidth;
            var entity = state.EntityManager.CreateEntity(textRenderArchetype);
           
                    state.EntityManager.SetSharedComponent(entity, renderFilterSettings);
                    var calliByteBuffer = state.EntityManager.GetBuffer<CalliByte>(entity);
                    var calliString = new CalliString(calliByteBuffer);
                    //string text = i.ToString() + j.ToString();
                    calliString.Append(text);

                    state.EntityManager.SetComponentData(entity, textBaseConfiguration);
                    state.EntityManager.SetComponentData(entity, LocalTransform.FromPosition(position));
                    state.EntityManager.SetComponentData(entity, materialMeshInfo);

            Debug.Log($"spawned {text}");
        }
    }
}
