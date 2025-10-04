using System;
using Unity.Collections;
using UnityEngine;

namespace TextMeshDOTS.HarfBuzz
{
    internal class FontDebug
    {
        public static void GetNameTags(Face face)
        {
            var language = new Language(Harfbuzz.HB_TAG('E', 'N', 'G', ' '));
            var result = new FixedString128Bytes();
            var values = Enum.GetValues(typeof(NameID));
            foreach (NameID value in values)
            {
                result = face.GetFaceInfo(value, language);
                Debug.Log($"{value}: {result}");
                result.Clear();
            }
        }
    }
}
