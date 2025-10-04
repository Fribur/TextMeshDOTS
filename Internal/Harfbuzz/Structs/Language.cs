using System;
using System.Runtime.InteropServices;

namespace TextMeshDOTS.HarfBuzz
{
    internal struct Language
    {
        public IntPtr ptr;

        public Language(string language, int len)
        {
            ptr = Harfbuzz.hb_language_from_string(language, len);
        }
        public Language(string language)
        {
            ptr = Harfbuzz.hb_language_from_string(language, -1);
        }
        /// <summary>
        /// Converts captial letter <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/languagetags">Opentype language tags</see> into BCP 47 language subtags
        /// </summary>
        public Language(uint tag)
        {
            ptr = Harfbuzz.hb_ot_tag_to_language(tag);
        }
        public override string ToString()
        {
            string result;
            unsafe
            {
                result = Marshal.PtrToStringUTF8(Harfbuzz.hb_language_to_string(ptr));
            }
            return result;
        }
    }
}
