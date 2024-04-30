using Unity.Entities;
using UnityEngine;

namespace TextMeshDOTS.Authoring
{
    public class TextStatisticsAuthoring : MonoBehaviour
    {
    }
    class TextStatisticsAuthoringBaker : Baker<TextStatisticsAuthoring>
    {
        public override void Bake(TextStatisticsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);           
            AddComponent(entity, TextMeshDOTSArchetypes.GetTextStatisticsTypeset());
        }
    }
}

