using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

// Use this system for adding at runtime new fonts to the fonttable

namespace TextMeshDOTS
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [DisableAutoCreation]
    partial struct RuntimeFontSpawner : ISystem
    {
        EntityQuery fontRequestQ;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            fontRequestQ = SystemAPI.QueryBuilder()
                .WithAll<FontLoadDescription>()
                .Build();
            state.RequireForUpdate(fontRequestQ);        
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var fontRequestBuffer = fontRequestQ.GetSingletonBuffer<FontLoadDescription>();            
            var newFontRequest = GetFontReference();
            if (!fontRequestBuffer.AsNativeArray().Contains(newFontRequest))
                fontRequestBuffer.Add(newFontRequest);
            state.Enabled = false;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        
        }
        public FontLoadDescription GetFontReference()
        {
            //use FontUtility Scriptable Object to extract the following needed information
            //see ReadMe for more details how
            return new FontLoadDescription
            {
                filePath = "Notosans/NotoSansDisplay-Regular.ttf",
                streamingAssetLocationValidated = true,
                isSystemFont = false,
                //faceIndex = default,

                //face Information
                fontFamily = "Noto Sans Display",
                fontSubFamily = "Regular",
                typographicFamily = "",
                typographicSubfamily = "",
                defaultWeight = 400,
                defaultWidth = 100,
                isItalic = false,
                slant = 0,                
            };
        }
    }
}
