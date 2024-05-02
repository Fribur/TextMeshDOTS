using Unity.Collections;
using UnityEngine;

namespace TextMeshDOTS
{
    internal static class CalligraphicsInternalExtensions
    {
        public static void GetSubString(this in CalliString calliString, ref FixedString128Bytes htmlTag, int startIndex, int length)
        {
            htmlTag.Clear();
            for (int i = startIndex, end = startIndex + length; i < end; i++)
                htmlTag.Append((char)calliString[i]);
        }
        public static bool Compare(this Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        }

        public static bool IsAscii(this Unicode.Rune rune) => rune.value < 0x80;

        // Todo: Add support for other languages in a Burst-compatible way.
        public static Unicode.Rune ToLower(this Unicode.Rune rune)
        {
            if (rune.IsAscii())
                return new Unicode.Rune(rune.value + (((uint)(rune.value - 'A') <= ('Z' - 'A')) ? 0x20 : 0));
            return rune;
        }

        public static Unicode.Rune ToUpper(this Unicode.Rune rune)
        {
            if (rune.IsAscii())
                return new Unicode.Rune(rune.value - (((uint)(rune.value - 'a') <= ('z' - 'a')) ? 0x20 : 0));
            return rune;
        }
        public static bool IsWhiteSpace(this Unicode.Rune rune)
        {
            switch (rune.value)
            {
                case 0x20: //space
                case 0xA0: //no breaking space
                case 0x1680: //OGHAM SPACE MARK
                case 0x2000: //EN QUAD
                case 0x2001: //EM QUAD
                case 0x2002: //EN SPACE
                case 0x2003: //EM SPACE
                case 0x2004: //THREE-PER-EM SPACE
                case 0x2005: //FOUR-PER-EM SPACE
                case 0x2006: //SIX-PER-EM SPACE
                case 0x2007: //FIGURE SPACE
                case 0x2008: //PUNCTUATION SPACE
                case 0x2009: //THIN SPACE
                case 0x200A: //HAIR SPACE
                case 0x202F: //NARROW NO-BREAK SPACE
                case 0x205F: //MEDIUM MATHEMATICAL SPACE
                case 0x3000: //IDEOGRAPHIC SPACE
                case 0x2028: //LINE SEPARATOR
                case 0x2029: //PARAGRAPH SEPARATOR
                case 0x0009: //CHARACTER TABULATION
                case 0x000A: //LINE FEED
                case 0x000B: //LINE TABULATION 
                case 0x000C: //FORM FEED
                case 0x000D: //CARRIAGE RETURN
                case 0x0085: //NEXT LINE
                    return (true);
                default:
                    return false;
            }
        }
    }
}

