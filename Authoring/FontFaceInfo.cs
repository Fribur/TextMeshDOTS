#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace TextMeshDOTS.Authoring
{
    /// <summary>
    /// Serializable key-value pair mapping a variation axis tag to its design coordinate value.
    /// </summary>
    [System.Serializable]
    public struct AxisCoordPair
    {
        public string axisTag;
        public float value;
    }

    /// <summary>
    /// Serializable data for a single face or named instance extracted from a font file.
    /// </summary>
    [System.Serializable]
    public class FontFaceInfo
    {
        public int faceIndex;
        public string fontFamily;
        public string fontSubFamily;
        public string typographicFamily;
        public string typographicSubfamily;
        public float weight;
        public float width;
        public bool isItalic;
        public float slant;

        /// <summary>
        /// If this entry represents a named instance of a variable font, the instance index.
        /// -1 for the base face.
        /// </summary>
        public int namedInstanceIndex;

        /// <summary>
        /// Named instance name (from the subfamily name ID of the instance).
        /// Empty for the base face.
        /// </summary>
        public string instanceName;

        /// <summary>
        /// Design coordinates for each variation axis (only populated for named instances).
        /// </summary>
        public List<AxisCoordPair> designCoords;
    }
}
#endif
