using Unity.Collections;
using UnityEngine;

namespace TextMeshDOTS.RichText
{
    internal static class ValidateRichTextTags
    {
        /// <summary>
        /// Call this function when enumerator.Current is rich text opening tag '<'.
        /// </summary>
        /// <param name="enumerator"></param>
        /// <returns>character right after valid Rich text</returns>
        internal static Unicode.Rune GetCharacterAfterValidHtmlTag(ref CalliString.Enumerator enumerator, ref FixedList512Bytes<RichTextTagIdentifier> richTextTagIndentifiers)
        {
            Unicode.Rune unicode = enumerator.Current;
            var startEnumerator = enumerator;
            if (IsValidTag(ref enumerator, ref richTextTagIndentifiers))
            {
                if (enumerator.MoveNext())
                    unicode = enumerator.Current; // To-Do: consider recursive call to this very function in case the next char is again rich text opening tag '<'
            }

            enumerator = startEnumerator; //restore Enumerator to point to '<'
            return unicode;
        }        
        private enum ParserState : byte
        {
            Zero,
            One,
            Two,
        }
        /// <summary>
        /// Function to identify and validate the rich tag. Returns the byte position of the > if the tag was valid.
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <returns></returns>
        internal static bool IsValidTag(
            ref CalliString.Enumerator enumerator,
            ref FixedList512Bytes<RichTextTagIdentifier> richTextTagIndentifiers)  //this is just a cache to avoid allocation
        {
            richTextTagIndentifiers.Clear();
            int tagCharCount = 0;
            int tagByteCount = 0;
            int startByteIndex = enumerator.CurrentByteIndex;
            ParserState tagIndentifierFlag = ParserState.Zero;

            int tagIndentifierIndex = richTextTagIndentifiers.Length;
            richTextTagIndentifiers.Add(RichTextTagIdentifier.Empty);
            ref var currentTagIndentifier = ref richTextTagIndentifiers.ElementAt(tagIndentifierIndex);
            var tagValueType = currentTagIndentifier.valueType = TagValueType.None;
            var tagUnitType = currentTagIndentifier.unitType = TagUnitType.Pixels;

            bool isTagSet = false;
            bool isValidHtmlTag = false;

            Unicode.Rune unicode = Unicode.BadRune;
            while (enumerator.MoveNext() && (unicode = enumerator.Current) != Unicode.BadRune && unicode != '<')
            {
                if (unicode == '>')  // ASCII Code of End HTML tag '>'
                {
                    isValidHtmlTag = true;
                    break;
                }

                int byteCount = unicode.LengthInUtf8Bytes();
                tagCharCount += 1;
                tagByteCount += byteCount;

                if (tagIndentifierFlag == ParserState.One)
                {
                    if (tagValueType == TagValueType.None)
                    {
                        // Check for tagIndentifier type
                        if (unicode == '+' || unicode == '-' || unicode == '.' || Unicode.Rune.IsDigit(unicode))
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = currentTagIndentifier.valueType = TagValueType.NumericalValue;
                            currentTagIndentifier.valueStartIndex = enumerator.CurrentByteIndex - unicode.LengthInUtf8Bytes();
                            currentTagIndentifier.valueLength += byteCount;
                        }
                        else if (unicode == '#')
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = currentTagIndentifier.valueType = TagValueType.ColorValue;
                            currentTagIndentifier.valueStartIndex = enumerator.CurrentByteIndex - unicode.LengthInUtf8Bytes();
                            currentTagIndentifier.valueLength += byteCount;
                        }
                        else if (unicode == '"')
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = currentTagIndentifier.valueType = TagValueType.StringValue;
                            currentTagIndentifier.valueStartIndex = enumerator.CurrentByteIndex;
                        }
                        else
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = currentTagIndentifier.valueType = TagValueType.StringValue;
                            currentTagIndentifier.valueStartIndex = enumerator.CurrentByteIndex - unicode.LengthInUtf8Bytes();
                            currentTagIndentifier.valueHashCode = (currentTagIndentifier.valueHashCode << 5) + currentTagIndentifier.valueHashCode ^ unicode.value;
                            currentTagIndentifier.valueLength += byteCount;
                        }
                    }
                    else
                    {
                        if (tagValueType == TagValueType.NumericalValue)
                        {
                            // Check for termination of numerical value.
                            if (unicode == 'p' || unicode == 'e' || unicode == '%' || unicode == ' ')
                            {
                                tagIndentifierFlag = ParserState.Two;
                                tagValueType = TagValueType.None;

                                switch (unicode.value)
                                {
                                    case 'e':
                                        currentTagIndentifier.unitType = tagUnitType = TagUnitType.FontUnits;
                                        break;
                                    case '%':
                                        currentTagIndentifier.unitType = tagUnitType = TagUnitType.Percentage;
                                        break;
                                    default:
                                        currentTagIndentifier.unitType = tagUnitType = TagUnitType.Pixels;
                                        break;
                                }

                                tagIndentifierIndex += 1;
                                richTextTagIndentifiers.Add(RichTextTagIdentifier.Empty);
                                currentTagIndentifier = ref richTextTagIndentifiers.ElementAt(tagIndentifierIndex);
                            }
                            else if (tagIndentifierFlag != ParserState.Two)
                            {
                                currentTagIndentifier.valueLength += byteCount;
                            }
                        }
                        else if (tagValueType == TagValueType.ColorValue)
                        {
                            if (unicode != ' ')
                            {
                                currentTagIndentifier.valueLength += byteCount;
                            }
                            else
                            {
                                tagIndentifierFlag = ParserState.Two;
                                tagValueType = TagValueType.None;
                                tagUnitType = TagUnitType.Pixels;
                                tagIndentifierIndex += 1;
                                richTextTagIndentifiers.Add(RichTextTagIdentifier.Empty);
                                currentTagIndentifier = ref richTextTagIndentifiers.ElementAt(tagIndentifierIndex);
                            }
                        }
                        else if (tagValueType == TagValueType.StringValue)
                        {
                            // Compute HashCode value for the named tag.
                            if (unicode != '"')
                            {
                                currentTagIndentifier.valueHashCode = (currentTagIndentifier.valueHashCode << 5) + currentTagIndentifier.valueHashCode ^ unicode.value;
                                currentTagIndentifier.valueLength += byteCount;
                            }
                            else
                            {
                                tagIndentifierFlag = ParserState.Two;
                                tagValueType = TagValueType.None;
                                tagUnitType = TagUnitType.Pixels;
                                tagIndentifierIndex += 1;
                                richTextTagIndentifiers.Add(RichTextTagIdentifier.Empty);
                                currentTagIndentifier = ref richTextTagIndentifiers.ElementAt(tagIndentifierIndex);
                            }
                        }
                    }
                }

                if (unicode == '=') // '='
                    tagIndentifierFlag = ParserState.One;

                // Compute HashCode for the name of the tagIndentifier
                if (tagIndentifierFlag == ParserState.Zero && unicode == ' ')
                {
                    if (isTagSet)
                        return false;

                    isTagSet = true;
                    tagIndentifierFlag = ParserState.Two;

                    tagValueType = TagValueType.None;
                    tagUnitType = TagUnitType.Pixels;
                    tagIndentifierIndex += 1;
                    richTextTagIndentifiers.Add(RichTextTagIdentifier.Empty);
                    currentTagIndentifier = ref richTextTagIndentifiers.ElementAt(tagIndentifierIndex);
                }

                if (tagIndentifierFlag == ParserState.Zero)
                    currentTagIndentifier.nameHashCode = (currentTagIndentifier.nameHashCode << 3) - currentTagIndentifier.nameHashCode + unicode.value;

                if (tagIndentifierFlag == ParserState.Two && unicode == ' ')
                    tagIndentifierFlag = ParserState.Zero;
            }
            if (!isValidHtmlTag)
                return false;

            var firstTagIndentifier = richTextTagIndentifiers[0];
            switch (firstTagIndentifier.nameHashCode)
            {
                case 98:  // <b>
                case 66:  // <B>
                case 427:  // </b>
                case 395:  // </B>
                case 105:  // <i>
                case 73:  // <I>
                case 434:  // </i>
                case 402:  // </I>
                case 115:  // <s>
                case 83:  // <S>
                case 444:  // </s>
                case 412:  // </S>
                case 117:  // <u>
                case 85:  // <U>
                case 446:  // </u>
                case 414:  // </U>
                case 43045:  // <mark=#FF00FF80>
                case 30245:  // <MARK>
                case 155892:  // </mark>
                case 143092:  // </MARK>
                case 6552:  // <sub>
                case 4728:  // <SUB>
                case 22673:  // </sub>
                case 20849:  // </SUB>
                case 6566:  // <sup>
                case 4742:  // <SUP>
                case 22687:  // </sup>
                case 20863:  // </SUP>
                case -330774850:  // <font-weight>
                case 2012149182:  // <FONT-WEIGHT>
                case -1885698441:  // </font-weight>
                case 457225591:  // </FONT-WEIGHT>
                case 6380:  // <pos=000.00px> <pos=0em> <pos=50%>
                case 4556:  // <POS>
                case 22501:  // </pos>
                case 20677:  // </POS>
                case 16034505:  // <voffset>
                case 11642281:  // <VOFFSET>
                case 54741026:  // </voffset>
                case 50348802:  // </VOFFSET>
                case 43969:  // <nobr>
                case 31169:  // <NOBR>
                case 156816:  // </nobr>
                case 144016:  // </NOBR>
                case 45545:  // <size=>
                case 32745:  // <SIZE>
                case 158392:  // </size>
                case 145592:  // </SIZE>
                case 41311:  // <font=xx>
                case 28511:  // <FONT>
                case 154158:  // </font>
                case 141358:  // </FONT>
                case 320078:  // <space=000.00>
                case 230446:  // <SPACE>
                case 276254:  // <alpha=#FF>
                case 186622:  // <ALPHA>
                case 327550:  // <width=xx>
                case 237918:  // <WIDTH>
                case 1117479:  // </width>
                case 1027847:  // </WIDTH>
                case 281955:  // <color> <color=#FF00FF> or <color=#FF00FF00>
                case 192323:  // <COLOR=#FF00FF>
                case 125395:  // <color=red>
                case -992792864:  // <color=lightblue>
                case 3573310:  // <color=blue>
                case 3680713:  // <color=grey>
                case 117905991:  // <color=black>
                case 121463835:  // <color=green>
                case 140357351:  // <color=white>
                case 26556144:  // <color=orange>
                case -36881330:  // <color=purple>
                case 554054276:  // <color=yellow>
                case 1983971:  // <cspace=xx.x>
                case 1356515:  // <CSPACE>                
                case 7513474:  // </cspace>
                case 6886018:  // </CSPACE>
                case 2152041:  // <mspace=xx.x>
                case 1524585:  // <MSPACE>
                case 7681544:  // </mspace>
                case 7054088:  // </MSPACE>
                case 280416:  // <class="name">
                case 1071884:  // </color>
                case 982252:  // </COLOR>
                case 2068980:  // <indent=10px> <indent=10em> <indent=50%>
                case 1441524:  // <INDENT>
                case 7598483:  // </indent>
                case 6971027:  // </INDENT>
                case 1109386397:  // <line-indent>
                case -842656867:  // <LINE-INDENT>
                case -445537194:  // </line-indent>
                case 1897386838:  // </LINE-INDENT>
                case 730022849:  // <lowercase>
                case 514803617:  // <LOWERCASE>
                case -1668324918:  // </lowercase>
                case -1883544150:  // </LOWERCASE>
                case 13526026:  // <allcaps>
                case 9133802:  // <ALLCAPS>
                case 781906058:  // <uppercase>
                case 566686826:  // <UPPERCASE>
                case 52232547:  // </allcaps>
                case 47840323:  // </ALLCAPS>
                case -1616441709:  // </uppercase>
                case -1831660941:  // </UPPERCASE>
                case 766244328:  // <smallcaps>
                case 551025096:  // <SMALLCAPS>
                case -1632103439:  // </smallcaps>
                case -1847322671:  // </SMALLCAPS>
                case 2109854:  // <margin=00.0> <margin=00em> <margin=50%>
                case 1482398:  // <MARGIN>
                case 7639357:  // </margin>
                case 7011901:  // </MARGIN>
                case 1100728678:  // <margin-left=xx.x>
                case -855002522:  // <MARGIN-LEFT>
                case -884817987:  // <margin-right=xx.x>
                case -1690034531:  // <MARGIN-RIGHT>
                case 1109349752:  // <line-height=xx.x>
                case -842693512:  // <LINE-HEIGHT>
                case -445573839:  // </line-height>
                case 1897350193:  // </LINE-HEIGHT>
                case 15115642:  // <noparse>
                case 10723418:  // <NOPARSE>
                case 315682:  // <scale=xx.x>
                case 226050:  // <SCALE=xx.x>
                case 1105611:  // </scale>
                case 1015979:  // </SCALE>
                case 2227963:  // <rotate=xx.x>
                case 1600507:  // <ROTATE=xx.x>
                case 7757466:  // </rotate>
                case 7130010:  // </ROTATE>
                case 317446:  // <table>
                case 227814:  // <TABLE>
                case 1107375:  // </table>
                case 1017743:  // </TABLE>
                case 926:  // <tr>
                case 670:  // <TR>
                case 3229:  // </tr>
                case 2973:  // </TR>
                case 916:  // <th>
                case 660:  // <TH>
                case 3219:  // </th>
                case 2963:  // </TH>
                case 912:  // <td>
                case 656:  // <TD>                
                case 3215:  // </td>
                case 2959:  // </TD>
                    return true;
                    default:
                    return false;
            }
        }
    }
}

