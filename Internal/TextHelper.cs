using Unity.Collections;

namespace TextMeshDOTS
{
    internal static class TextHelper
    {       
        public static int GetHashCodeCaseInsensitive(FixedString128Bytes text)
        {
            var s = text.GetEnumerator();
            int num = 0;
            while (s.MoveNext())
            {
                num = ((num << 5) + num) ^ s.Current.ToUpper().value;
            }
            return num;
        }
        public static int GetValueHash(FixedString128Bytes text)
        {
            var s = text.GetEnumerator();
            int num = 0;
            while (s.MoveNext())
            {
                num = (num << 5) + num ^ s.Current.value;
                //num = ((num << 5) + num) ^ s.Current.ToUpper().value;
            }
            return num;
        }
    }
}