using System.Text;

namespace SimpleBot
{
    static class TextUtils
    {
        public static string FoldToASCII(ReadOnlySpan<char> input, Predicate<char> filter = null)
            => FoldToASCII(input, new StringBuilder(), filter).ToString();

        // altered https://github.com/apache/lucenenet/blob/master/src/Lucene.Net.Analysis.Common/Analysis/Miscellaneous/ASCIIFoldingFilter.cs
        public static StringBuilder FoldToASCII(ReadOnlySpan<char> input, StringBuilder output, Predicate<char> filter = null)
        {
            filter ??= c => true;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (!filter(c))
                    continue;

                if (c < '\u0080')
                {
                    output.Append(c);
                    continue;
                }

                switch (c)
                {
                    case 'À':
                    case 'Á':
                    case 'Â':
                    case 'Ã':
                    case 'Ä':
                    case 'Å':
                    case 'Ā':
                    case 'Ă':
                    case 'Ą':
                    case 'Ə':
                    case 'Ǎ':
                    case 'Ǟ':
                    case 'Ǡ':
                    case 'Ǻ':
                    case 'Ȁ':
                    case 'Ȃ':
                    case 'Ȧ':
                    case 'Ⱥ':
                    case 'ᴀ':
                    case 'Ḁ':
                    case 'Ạ':
                    case 'Ả':
                    case 'Ấ':
                    case 'Ầ':
                    case 'Ẩ':
                    case 'Ẫ':
                    case 'Ậ':
                    case 'Ắ':
                    case 'Ằ':
                    case 'Ẳ':
                    case 'Ẵ':
                    case 'Ặ':
                    case 'Ⓐ':
                    case 'Ａ':
                        output.Append('A');
                        break;
                    case 'à':
                    case 'á':
                    case 'â':
                    case 'ã':
                    case 'ä':
                    case 'å':
                    case 'ā':
                    case 'ă':
                    case 'ą':
                    case 'ǎ':
                    case 'ǟ':
                    case 'ǡ':
                    case 'ǻ':
                    case 'ȁ':
                    case 'ȃ':
                    case 'ȧ':
                    case 'ɐ':
                    case 'ə':
                    case 'ɚ':
                    case 'ᶏ':
                    case 'ᶕ':
                    case 'ḁ':
                    case 'ẚ':
                    case 'ạ':
                    case 'ả':
                    case 'ấ':
                    case 'ầ':
                    case 'ẩ':
                    case 'ẫ':
                    case 'ậ':
                    case 'ắ':
                    case 'ằ':
                    case 'ẳ':
                    case 'ẵ':
                    case 'ặ':
                    case 'ₐ':
                    case 'ₔ':
                    case 'ⓐ':
                    case 'ⱥ':
                    case 'Ɐ':
                    case 'ａ':
                        output.Append('a');
                        break;
                    case 'Ꜳ':
                        output.Append('A');
                        output.Append('A');
                        break;
                    case 'Æ':
                    case 'Ǣ':
                    case 'Ǽ':
                    case 'ᴁ':
                        output.Append('A');
                        output.Append('E');
                        break;
                    case 'Ꜵ':
                        output.Append('A');
                        output.Append('O');
                        break;
                    case 'Ꜷ':
                        output.Append('A');
                        output.Append('U');
                        break;
                    case 'Ꜹ':
                    case 'Ꜻ':
                        output.Append('A');
                        output.Append('V');
                        break;
                    case 'Ꜽ':
                        output.Append('A');
                        output.Append('Y');
                        break;
                    case '⒜':
                        output.Append('(');
                        output.Append('a');
                        output.Append(')');
                        break;
                    case 'ꜳ':
                        output.Append('a');
                        output.Append('a');
                        break;
                    case 'æ':
                    case 'ǣ':
                    case 'ǽ':
                    case 'ᴂ':
                        output.Append('a');
                        output.Append('e');
                        break;
                    case 'ꜵ':
                        output.Append('a');
                        output.Append('o');
                        break;
                    case 'ꜷ':
                        output.Append('a');
                        output.Append('u');
                        break;
                    case 'ꜹ':
                    case 'ꜻ':
                        output.Append('a');
                        output.Append('v');
                        break;
                    case 'ꜽ':
                        output.Append('a');
                        output.Append('y');
                        break;
                    case 'Ɓ':
                    case 'Ƃ':
                    case 'Ƀ':
                    case 'ʙ':
                    case 'ᴃ':
                    case 'Ḃ':
                    case 'Ḅ':
                    case 'Ḇ':
                    case 'Ⓑ':
                    case 'Ｂ':
                        output.Append('B');
                        break;
                    case 'ƀ':
                    case 'ƃ':
                    case 'ɓ':
                    case 'ᵬ':
                    case 'ᶀ':
                    case 'ḃ':
                    case 'ḅ':
                    case 'ḇ':
                    case 'ⓑ':
                    case 'ｂ':
                        output.Append('b');
                        break;
                    case '⒝':
                        output.Append('(');
                        output.Append('b');
                        output.Append(')');
                        break;
                    case 'Ç':
                    case 'Ć':
                    case 'Ĉ':
                    case 'Ċ':
                    case 'Č':
                    case 'Ƈ':
                    case 'Ȼ':
                    case 'ʗ':
                    case 'ᴄ':
                    case 'Ḉ':
                    case 'Ⓒ':
                    case 'Ｃ':
                        output.Append('C');
                        break;
                    case 'ç':
                    case 'ć':
                    case 'ĉ':
                    case 'ċ':
                    case 'č':
                    case 'ƈ':
                    case 'ȼ':
                    case 'ɕ':
                    case 'ḉ':
                    case 'ↄ':
                    case 'ⓒ':
                    case 'Ꜿ':
                    case 'ꜿ':
                    case 'ｃ':
                        output.Append('c');
                        break;
                    case '⒞':
                        output.Append('(');
                        output.Append('c');
                        output.Append(')');
                        break;
                    case 'Ð':
                    case 'Ď':
                    case 'Đ':
                    case 'Ɖ':
                    case 'Ɗ':
                    case 'Ƌ':
                    case 'ᴅ':
                    case 'ᴆ':
                    case 'Ḋ':
                    case 'Ḍ':
                    case 'Ḏ':
                    case 'Ḑ':
                    case 'Ḓ':
                    case 'Ⓓ':
                    case 'Ꝺ':
                    case 'Ｄ':
                        output.Append('D');
                        break;
                    case 'ð':
                    case 'ď':
                    case 'đ':
                    case 'ƌ':
                    case 'ȡ':
                    case 'ɖ':
                    case 'ɗ':
                    case 'ᵭ':
                    case 'ᶁ':
                    case 'ᶑ':
                    case 'ḋ':
                    case 'ḍ':
                    case 'ḏ':
                    case 'ḑ':
                    case 'ḓ':
                    case 'ⓓ':
                    case 'ꝺ':
                    case 'ｄ':
                        output.Append('d');
                        break;
                    case 'Ǆ':
                    case 'Ǳ':
                        output.Append('D');
                        output.Append('Z');
                        break;
                    case 'ǅ':
                    case 'ǲ':
                        output.Append('D');
                        output.Append('z');
                        break;
                    case '⒟':
                        output.Append('(');
                        output.Append('d');
                        output.Append(')');
                        break;
                    case 'ȸ':
                        output.Append('d');
                        output.Append('b');
                        break;
                    case 'ǆ':
                    case 'ǳ':
                    case 'ʣ':
                    case 'ʥ':
                        output.Append('d');
                        output.Append('z');
                        break;
                    case 'È':
                    case 'É':
                    case 'Ê':
                    case 'Ë':
                    case 'Ē':
                    case 'Ĕ':
                    case 'Ė':
                    case 'Ę':
                    case 'Ě':
                    case 'Ǝ':
                    case 'Ɛ':
                    case 'Ȅ':
                    case 'Ȇ':
                    case 'Ȩ':
                    case 'Ɇ':
                    case 'ᴇ':
                    case 'Ḕ':
                    case 'Ḗ':
                    case 'Ḙ':
                    case 'Ḛ':
                    case 'Ḝ':
                    case 'Ẹ':
                    case 'Ẻ':
                    case 'Ẽ':
                    case 'Ế':
                    case 'Ề':
                    case 'Ể':
                    case 'Ễ':
                    case 'Ệ':
                    case 'Ⓔ':
                    case 'ⱻ':
                    case 'Ｅ':
                        output.Append('E');
                        break;
                    case 'è':
                    case 'é':
                    case 'ê':
                    case 'ë':
                    case 'ē':
                    case 'ĕ':
                    case 'ė':
                    case 'ę':
                    case 'ě':
                    case 'ǝ':
                    case 'ȅ':
                    case 'ȇ':
                    case 'ȩ':
                    case 'ɇ':
                    case 'ɘ':
                    case 'ɛ':
                    case 'ɜ':
                    case 'ɝ':
                    case 'ɞ':
                    case 'ʚ':
                    case 'ᴈ':
                    case 'ᶒ':
                    case 'ᶓ':
                    case 'ᶔ':
                    case 'ḕ':
                    case 'ḗ':
                    case 'ḙ':
                    case 'ḛ':
                    case 'ḝ':
                    case 'ẹ':
                    case 'ẻ':
                    case 'ẽ':
                    case 'ế':
                    case 'ề':
                    case 'ể':
                    case 'ễ':
                    case 'ệ':
                    case 'ₑ':
                    case 'ⓔ':
                    case 'ⱸ':
                    case 'ｅ':
                        output.Append('e');
                        break;
                    case '⒠':
                        output.Append('(');
                        output.Append('e');
                        output.Append(')');
                        break;
                    case 'Ƒ':
                    case 'Ḟ':
                    case 'Ⓕ':
                    case 'ꜰ':
                    case 'Ꝼ':
                    case 'ꟻ':
                    case 'Ｆ':
                        output.Append('F');
                        break;
                    case 'ƒ':
                    case 'ᵮ':
                    case 'ᶂ':
                    case 'ḟ':
                    case 'ẛ':
                    case 'ⓕ':
                    case 'ꝼ':
                    case 'ｆ':
                        output.Append('f');
                        break;
                    case '⒡':
                        output.Append('(');
                        output.Append('f');
                        output.Append(')');
                        break;
                    case 'ﬀ':
                        output.Append('f');
                        output.Append('f');
                        break;
                    case 'ﬃ':
                        output.Append('f');
                        output.Append('f');
                        output.Append('i');
                        break;
                    case 'ﬄ':
                        output.Append('f');
                        output.Append('f');
                        output.Append('l');
                        break;
                    case 'ﬁ':
                        output.Append('f');
                        output.Append('i');
                        break;
                    case 'ﬂ':
                        output.Append('f');
                        output.Append('l');
                        break;
                    case 'Ĝ':
                    case 'Ğ':
                    case 'Ġ':
                    case 'Ģ':
                    case 'Ɠ':
                    case 'Ǥ':
                    case 'ǥ':
                    case 'Ǧ':
                    case 'ǧ':
                    case 'Ǵ':
                    case 'ɢ':
                    case 'ʛ':
                    case 'Ḡ':
                    case 'Ⓖ':
                    case 'Ᵹ':
                    case 'Ꝿ':
                    case 'Ｇ':
                        output.Append('G');
                        break;
                    case 'ĝ':
                    case 'ğ':
                    case 'ġ':
                    case 'ģ':
                    case 'ǵ':
                    case 'ɠ':
                    case 'ɡ':
                    case 'ᵷ':
                    case 'ᵹ':
                    case 'ᶃ':
                    case 'ḡ':
                    case 'ⓖ':
                    case 'ꝿ':
                    case 'ｇ':
                        output.Append('g');
                        break;
                    case '⒢':
                        output.Append('(');
                        output.Append('g');
                        output.Append(')');
                        break;
                    case 'Ĥ':
                    case 'Ħ':
                    case 'Ȟ':
                    case 'ʜ':
                    case 'Ḣ':
                    case 'Ḥ':
                    case 'Ḧ':
                    case 'Ḩ':
                    case 'Ḫ':
                    case 'Ⓗ':
                    case 'Ⱨ':
                    case 'Ⱶ':
                    case 'Ｈ':
                        output.Append('H');
                        break;
                    case 'ĥ':
                    case 'ħ':
                    case 'ȟ':
                    case 'ɥ':
                    case 'ɦ':
                    case 'ʮ':
                    case 'ʯ':
                    case 'ḣ':
                    case 'ḥ':
                    case 'ḧ':
                    case 'ḩ':
                    case 'ḫ':
                    case 'ẖ':
                    case 'ⓗ':
                    case 'ⱨ':
                    case 'ⱶ':
                    case 'ｈ':
                        output.Append('h');
                        break;
                    case 'Ƕ':
                        output.Append('H');
                        output.Append('V');
                        break;
                    case '⒣':
                        output.Append('(');
                        output.Append('h');
                        output.Append(')');
                        break;
                    case 'ƕ':
                        output.Append('h');
                        output.Append('v');
                        break;
                    case 'Ì':
                    case 'Í':
                    case 'Î':
                    case 'Ï':
                    case 'Ĩ':
                    case 'Ī':
                    case 'Ĭ':
                    case 'Į':
                    case 'İ':
                    case 'Ɩ':
                    case 'Ɨ':
                    case 'Ǐ':
                    case 'Ȉ':
                    case 'Ȋ':
                    case 'ɪ':
                    case 'ᵻ':
                    case 'Ḭ':
                    case 'Ḯ':
                    case 'Ỉ':
                    case 'Ị':
                    case 'Ⓘ':
                    case 'ꟾ':
                    case 'Ｉ':
                        output.Append('I');
                        break;
                    case 'ì':
                    case 'í':
                    case 'î':
                    case 'ï':
                    case 'ĩ':
                    case 'ī':
                    case 'ĭ':
                    case 'į':
                    case 'ı':
                    case 'ǐ':
                    case 'ȉ':
                    case 'ȋ':
                    case 'ɨ':
                    case 'ᴉ':
                    case 'ᵢ':
                    case 'ᵼ':
                    case 'ᶖ':
                    case 'ḭ':
                    case 'ḯ':
                    case 'ỉ':
                    case 'ị':
                    case 'ⁱ':
                    case 'ⓘ':
                    case 'ｉ':
                        output.Append('i');
                        break;
                    case 'Ĳ':
                        output.Append('I');
                        output.Append('J');
                        break;
                    case '⒤':
                        output.Append('(');
                        output.Append('i');
                        output.Append(')');
                        break;
                    case 'ĳ':
                        output.Append('i');
                        output.Append('j');
                        break;
                    case 'Ĵ':
                    case 'Ɉ':
                    case 'ᴊ':
                    case 'Ⓙ':
                    case 'Ｊ':
                        output.Append('J');
                        break;
                    case 'ĵ':
                    case 'ǰ':
                    case 'ȷ':
                    case 'ɉ':
                    case 'ɟ':
                    case 'ʄ':
                    case 'ʝ':
                    case 'ⓙ':
                    case 'ⱼ':
                    case 'ｊ':
                        output.Append('j');
                        break;
                    case '⒥':
                        output.Append('(');
                        output.Append('j');
                        output.Append(')');
                        break;
                    case 'Ķ':
                    case 'Ƙ':
                    case 'Ǩ':
                    case 'ᴋ':
                    case 'Ḱ':
                    case 'Ḳ':
                    case 'Ḵ':
                    case 'Ⓚ':
                    case 'Ⱪ':
                    case 'Ꝁ':
                    case 'Ꝃ':
                    case 'Ꝅ':
                    case 'Ｋ':
                        output.Append('K');
                        break;
                    case 'ķ':
                    case 'ƙ':
                    case 'ǩ':
                    case 'ʞ':
                    case 'ᶄ':
                    case 'ḱ':
                    case 'ḳ':
                    case 'ḵ':
                    case 'ⓚ':
                    case 'ⱪ':
                    case 'ꝁ':
                    case 'ꝃ':
                    case 'ꝅ':
                    case 'ｋ':
                        output.Append('k');
                        break;
                    case '⒦':
                        output.Append('(');
                        output.Append('k');
                        output.Append(')');
                        break;
                    case 'Ĺ':
                    case 'Ļ':
                    case 'Ľ':
                    case 'Ŀ':
                    case 'Ł':
                    case 'Ƚ':
                    case 'ʟ':
                    case 'ᴌ':
                    case 'Ḷ':
                    case 'Ḹ':
                    case 'Ḻ':
                    case 'Ḽ':
                    case 'Ⓛ':
                    case 'Ⱡ':
                    case 'Ɫ':
                    case 'Ꝇ':
                    case 'Ꝉ':
                    case 'Ꞁ':
                    case 'Ｌ':
                        output.Append('L');
                        break;
                    case 'ĺ':
                    case 'ļ':
                    case 'ľ':
                    case 'ŀ':
                    case 'ł':
                    case 'ƚ':
                    case 'ȴ':
                    case 'ɫ':
                    case 'ɬ':
                    case 'ɭ':
                    case 'ᶅ':
                    case 'ḷ':
                    case 'ḹ':
                    case 'ḻ':
                    case 'ḽ':
                    case 'ⓛ':
                    case 'ⱡ':
                    case 'ꝇ':
                    case 'ꝉ':
                    case 'ꞁ':
                    case 'ｌ':
                        output.Append('l');
                        break;
                    case 'Ǉ':
                        output.Append('L');
                        output.Append('J');
                        break;
                    case 'Ỻ':
                        output.Append('L');
                        output.Append('L');
                        break;
                    case 'ǈ':
                        output.Append('L');
                        output.Append('j');
                        break;
                    case '⒧':
                        output.Append('(');
                        output.Append('l');
                        output.Append(')');
                        break;
                    case 'ǉ':
                        output.Append('l');
                        output.Append('j');
                        break;
                    case 'ỻ':
                        output.Append('l');
                        output.Append('l');
                        break;
                    case 'ʪ':
                        output.Append('l');
                        output.Append('s');
                        break;
                    case 'ʫ':
                        output.Append('l');
                        output.Append('z');
                        break;
                    case 'Ɯ':
                    case 'ᴍ':
                    case 'Ḿ':
                    case 'Ṁ':
                    case 'Ṃ':
                    case 'Ⓜ':
                    case 'Ɱ':
                    case 'ꟽ':
                    case 'ꟿ':
                    case 'Ｍ':
                        output.Append('M');
                        break;
                    case 'ɯ':
                    case 'ɰ':
                    case 'ɱ':
                    case 'ᵯ':
                    case 'ᶆ':
                    case 'ḿ':
                    case 'ṁ':
                    case 'ṃ':
                    case 'ⓜ':
                    case 'ｍ':
                        output.Append('m');
                        break;
                    case '⒨':
                        output.Append('(');
                        output.Append('m');
                        output.Append(')');
                        break;
                    case 'Ñ':
                    case 'Ń':
                    case 'Ņ':
                    case 'Ň':
                    case 'Ŋ':
                    case 'Ɲ':
                    case 'Ǹ':
                    case 'Ƞ':
                    case 'ɴ':
                    case 'ᴎ':
                    case 'Ṅ':
                    case 'Ṇ':
                    case 'Ṉ':
                    case 'Ṋ':
                    case 'Ⓝ':
                    case 'Ｎ':
                        output.Append('N');
                        break;
                    case 'ñ':
                    case 'ń':
                    case 'ņ':
                    case 'ň':
                    case 'ŉ':
                    case 'ŋ':
                    case 'ƞ':
                    case 'ǹ':
                    case 'ȵ':
                    case 'ɲ':
                    case 'ɳ':
                    case 'ᵰ':
                    case 'ᶇ':
                    case 'ṅ':
                    case 'ṇ':
                    case 'ṉ':
                    case 'ṋ':
                    case 'ⁿ':
                    case 'ⓝ':
                    case 'ｎ':
                        output.Append('n');
                        break;
                    case 'Ǌ':
                        output.Append('N');
                        output.Append('J');
                        break;
                    case 'ǋ':
                        output.Append('N');
                        output.Append('j');
                        break;
                    case '⒩':
                        output.Append('(');
                        output.Append('n');
                        output.Append(')');
                        break;
                    case 'ǌ':
                        output.Append('n');
                        output.Append('j');
                        break;
                    case 'Ò':
                    case 'Ó':
                    case 'Ô':
                    case 'Õ':
                    case 'Ö':
                    case 'Ø':
                    case 'Ō':
                    case 'Ŏ':
                    case 'Ő':
                    case 'Ɔ':
                    case 'Ɵ':
                    case 'Ơ':
                    case 'Ǒ':
                    case 'Ǫ':
                    case 'Ǭ':
                    case 'Ǿ':
                    case 'Ȍ':
                    case 'Ȏ':
                    case 'Ȫ':
                    case 'Ȭ':
                    case 'Ȯ':
                    case 'Ȱ':
                    case 'ᴏ':
                    case 'ᴐ':
                    case 'Ṍ':
                    case 'Ṏ':
                    case 'Ṑ':
                    case 'Ṓ':
                    case 'Ọ':
                    case 'Ỏ':
                    case 'Ố':
                    case 'Ồ':
                    case 'Ổ':
                    case 'Ỗ':
                    case 'Ộ':
                    case 'Ớ':
                    case 'Ờ':
                    case 'Ở':
                    case 'Ỡ':
                    case 'Ợ':
                    case 'Ⓞ':
                    case 'Ꝋ':
                    case 'Ꝍ':
                    case 'Ｏ':
                        output.Append('O');
                        break;
                    case 'ò':
                    case 'ó':
                    case 'ô':
                    case 'õ':
                    case 'ö':
                    case 'ø':
                    case 'ō':
                    case 'ŏ':
                    case 'ő':
                    case 'ơ':
                    case 'ǒ':
                    case 'ǫ':
                    case 'ǭ':
                    case 'ǿ':
                    case 'ȍ':
                    case 'ȏ':
                    case 'ȫ':
                    case 'ȭ':
                    case 'ȯ':
                    case 'ȱ':
                    case 'ɔ':
                    case 'ɵ':
                    case 'ᴖ':
                    case 'ᴗ':
                    case 'ᶗ':
                    case 'ṍ':
                    case 'ṏ':
                    case 'ṑ':
                    case 'ṓ':
                    case 'ọ':
                    case 'ỏ':
                    case 'ố':
                    case 'ồ':
                    case 'ổ':
                    case 'ỗ':
                    case 'ộ':
                    case 'ớ':
                    case 'ờ':
                    case 'ở':
                    case 'ỡ':
                    case 'ợ':
                    case 'ₒ':
                    case 'ⓞ':
                    case 'ⱺ':
                    case 'ꝋ':
                    case 'ꝍ':
                    case 'ｏ':
                        output.Append('o');
                        break;
                    case 'Œ':
                    case 'ɶ':
                        output.Append('O');
                        output.Append('E');
                        break;
                    case 'Ꝏ':
                        output.Append('O');
                        output.Append('O');
                        break;
                    case 'Ȣ':
                    case 'ᴕ':
                        output.Append('O');
                        output.Append('U');
                        break;
                    case '⒪':
                        output.Append('(');
                        output.Append('o');
                        output.Append(')');
                        break;
                    case 'œ':
                    case 'ᴔ':
                        output.Append('o');
                        output.Append('e');
                        break;
                    case 'ꝏ':
                        output.Append('o');
                        output.Append('o');
                        break;
                    case 'ȣ':
                        output.Append('o');
                        output.Append('u');
                        break;
                    case 'Ƥ':
                    case 'ᴘ':
                    case 'Ṕ':
                    case 'Ṗ':
                    case 'Ⓟ':
                    case 'Ᵽ':
                    case 'Ꝑ':
                    case 'Ꝓ':
                    case 'Ꝕ':
                    case 'Ｐ':
                        output.Append('P');
                        break;
                    case 'ƥ':
                    case 'ᵱ':
                    case 'ᵽ':
                    case 'ᶈ':
                    case 'ṕ':
                    case 'ṗ':
                    case 'ⓟ':
                    case 'ꝑ':
                    case 'ꝓ':
                    case 'ꝕ':
                    case 'ꟼ':
                    case 'ｐ':
                        output.Append('p');
                        break;
                    case '⒫':
                        output.Append('(');
                        output.Append('p');
                        output.Append(')');
                        break;
                    case 'Ɋ':
                    case 'Ⓠ':
                    case 'Ꝗ':
                    case 'Ꝙ':
                    case 'Ｑ':
                        output.Append('Q');
                        break;
                    case 'ĸ':
                    case 'ɋ':
                    case 'ʠ':
                    case 'ⓠ':
                    case 'ꝗ':
                    case 'ꝙ':
                    case 'ｑ':
                        output.Append('q');
                        break;
                    case '⒬':
                        output.Append('(');
                        output.Append('q');
                        output.Append(')');
                        break;
                    case 'ȹ':
                        output.Append('q');
                        output.Append('p');
                        break;
                    case 'Ŕ':
                    case 'Ŗ':
                    case 'Ř':
                    case 'Ȑ':
                    case 'Ȓ':
                    case 'Ɍ':
                    case 'ʀ':
                    case 'ʁ':
                    case 'ᴙ':
                    case 'ᴚ':
                    case 'Ṙ':
                    case 'Ṛ':
                    case 'Ṝ':
                    case 'Ṟ':
                    case 'Ⓡ':
                    case 'Ɽ':
                    case 'Ꝛ':
                    case 'Ꞃ':
                    case 'Ｒ':
                        output.Append('R');
                        break;
                    case 'ŕ':
                    case 'ŗ':
                    case 'ř':
                    case 'ȑ':
                    case 'ȓ':
                    case 'ɍ':
                    case 'ɼ':
                    case 'ɽ':
                    case 'ɾ':
                    case 'ɿ':
                    case 'ᵣ':
                    case 'ᵲ':
                    case 'ᵳ':
                    case 'ᶉ':
                    case 'ṙ':
                    case 'ṛ':
                    case 'ṝ':
                    case 'ṟ':
                    case 'ⓡ':
                    case 'ꝛ':
                    case 'ꞃ':
                    case 'ｒ':
                        output.Append('r');
                        break;
                    case '⒭':
                        output.Append('(');
                        output.Append('r');
                        output.Append(')');
                        break;
                    case 'Ś':
                    case 'Ŝ':
                    case 'Ş':
                    case 'Š':
                    case 'Ș':
                    case 'Ṡ':
                    case 'Ṣ':
                    case 'Ṥ':
                    case 'Ṧ':
                    case 'Ṩ':
                    case 'Ⓢ':
                    case 'ꜱ':
                    case 'ꞅ':
                    case 'Ｓ':
                        output.Append('S');
                        break;
                    case 'ś':
                    case 'ŝ':
                    case 'ş':
                    case 'š':
                    case 'ſ':
                    case 'ș':
                    case 'ȿ':
                    case 'ʂ':
                    case 'ᵴ':
                    case 'ᶊ':
                    case 'ṡ':
                    case 'ṣ':
                    case 'ṥ':
                    case 'ṧ':
                    case 'ṩ':
                    case 'ẜ':
                    case 'ẝ':
                    case 'ⓢ':
                    case 'Ꞅ':
                    case 'ｓ':
                        output.Append('s');
                        break;
                    case 'ẞ':
                        output.Append('S');
                        output.Append('S');
                        break;
                    case '⒮':
                        output.Append('(');
                        output.Append('s');
                        output.Append(')');
                        break;
                    case 'ß':
                        output.Append('s');
                        output.Append('s');
                        break;
                    case 'ﬆ':
                        output.Append('s');
                        output.Append('t');
                        break;
                    case 'Ţ':
                    case 'Ť':
                    case 'Ŧ':
                    case 'Ƭ':
                    case 'Ʈ':
                    case 'Ț':
                    case 'Ⱦ':
                    case 'ᴛ':
                    case 'Ṫ':
                    case 'Ṭ':
                    case 'Ṯ':
                    case 'Ṱ':
                    case 'Ⓣ':
                    case 'Ꞇ':
                    case 'Ｔ':
                        output.Append('T');
                        break;
                    case 'ţ':
                    case 'ť':
                    case 'ŧ':
                    case 'ƫ':
                    case 'ƭ':
                    case 'ț':
                    case 'ȶ':
                    case 'ʇ':
                    case 'ʈ':
                    case 'ᵵ':
                    case 'ṫ':
                    case 'ṭ':
                    case 'ṯ':
                    case 'ṱ':
                    case 'ẗ':
                    case 'ⓣ':
                    case 'ⱦ':
                    case 'ｔ':
                        output.Append('t');
                        break;
                    case 'Þ':
                    case 'Ꝧ':
                        output.Append('T');
                        output.Append('H');
                        break;
                    case 'Ꜩ':
                        output.Append('T');
                        output.Append('Z');
                        break;
                    case '⒯':
                        output.Append('(');
                        output.Append('t');
                        output.Append(')');
                        break;
                    case 'ʨ':
                        output.Append('t');
                        output.Append('c');
                        break;
                    case 'þ':
                    case 'ᵺ':
                    case 'ꝧ':
                        output.Append('t');
                        output.Append('h');
                        break;
                    case 'ʦ':
                        output.Append('t');
                        output.Append('s');
                        break;
                    case 'ꜩ':
                        output.Append('t');
                        output.Append('z');
                        break;
                    case 'Ù':
                    case 'Ú':
                    case 'Û':
                    case 'Ü':
                    case 'Ũ':
                    case 'Ū':
                    case 'Ŭ':
                    case 'Ů':
                    case 'Ű':
                    case 'Ų':
                    case 'Ư':
                    case 'Ǔ':
                    case 'Ǖ':
                    case 'Ǘ':
                    case 'Ǚ':
                    case 'Ǜ':
                    case 'Ȕ':
                    case 'Ȗ':
                    case 'Ʉ':
                    case 'ᴜ':
                    case 'ᵾ':
                    case 'Ṳ':
                    case 'Ṵ':
                    case 'Ṷ':
                    case 'Ṹ':
                    case 'Ṻ':
                    case 'Ụ':
                    case 'Ủ':
                    case 'Ứ':
                    case 'Ừ':
                    case 'Ử':
                    case 'Ữ':
                    case 'Ự':
                    case 'Ⓤ':
                    case 'Ｕ':
                        output.Append('U');
                        break;
                    case 'ù':
                    case 'ú':
                    case 'û':
                    case 'ü':
                    case 'ũ':
                    case 'ū':
                    case 'ŭ':
                    case 'ů':
                    case 'ű':
                    case 'ų':
                    case 'ư':
                    case 'ǔ':
                    case 'ǖ':
                    case 'ǘ':
                    case 'ǚ':
                    case 'ǜ':
                    case 'ȕ':
                    case 'ȗ':
                    case 'ʉ':
                    case 'ᵤ':
                    case 'ᶙ':
                    case 'ṳ':
                    case 'ṵ':
                    case 'ṷ':
                    case 'ṹ':
                    case 'ṻ':
                    case 'ụ':
                    case 'ủ':
                    case 'ứ':
                    case 'ừ':
                    case 'ử':
                    case 'ữ':
                    case 'ự':
                    case 'ⓤ':
                    case 'ｕ':
                        output.Append('u');
                        break;
                    case '⒰':
                        output.Append('(');
                        output.Append('u');
                        output.Append(')');
                        break;
                    case 'ᵫ':
                        output.Append('u');
                        output.Append('e');
                        break;
                    case 'Ʋ':
                    case 'Ʌ':
                    case 'ᴠ':
                    case 'Ṽ':
                    case 'Ṿ':
                    case 'Ỽ':
                    case 'Ⓥ':
                    case 'Ꝟ':
                    case 'Ꝩ':
                    case 'Ｖ':
                        output.Append('V');
                        break;
                    case 'ʋ':
                    case 'ʌ':
                    case 'ᵥ':
                    case 'ᶌ':
                    case 'ṽ':
                    case 'ṿ':
                    case 'ⓥ':
                    case 'ⱱ':
                    case 'ⱴ':
                    case 'ꝟ':
                    case 'ｖ':
                        output.Append('v');
                        break;
                    case 'Ꝡ':
                        output.Append('V');
                        output.Append('Y');
                        break;
                    case '⒱':
                        output.Append('(');
                        output.Append('v');
                        output.Append(')');
                        break;
                    case 'ꝡ':
                        output.Append('v');
                        output.Append('y');
                        break;
                    case 'Ŵ':
                    case 'Ƿ':
                    case 'ᴡ':
                    case 'Ẁ':
                    case 'Ẃ':
                    case 'Ẅ':
                    case 'Ẇ':
                    case 'Ẉ':
                    case 'Ⓦ':
                    case 'Ⱳ':
                    case 'Ｗ':
                        output.Append('W');
                        break;
                    case 'ŵ':
                    case 'ƿ':
                    case 'ʍ':
                    case 'ẁ':
                    case 'ẃ':
                    case 'ẅ':
                    case 'ẇ':
                    case 'ẉ':
                    case 'ẘ':
                    case 'ⓦ':
                    case 'ⱳ':
                    case 'ｗ':
                        output.Append('w');
                        break;
                    case '⒲':
                        output.Append('(');
                        output.Append('w');
                        output.Append(')');
                        break;
                    case 'Ẋ':
                    case 'Ẍ':
                    case 'Ⓧ':
                    case 'Ｘ':
                        output.Append('X');
                        break;
                    case 'ᶍ':
                    case 'ẋ':
                    case 'ẍ':
                    case 'ₓ':
                    case 'ⓧ':
                    case 'ｘ':
                        output.Append('x');
                        break;
                    case '⒳':
                        output.Append('(');
                        output.Append('x');
                        output.Append(')');
                        break;
                    case 'Ý':
                    case 'Ŷ':
                    case 'Ÿ':
                    case 'Ƴ':
                    case 'Ȳ':
                    case 'Ɏ':
                    case 'ʏ':
                    case 'Ẏ':
                    case 'Ỳ':
                    case 'Ỵ':
                    case 'Ỷ':
                    case 'Ỹ':
                    case 'Ỿ':
                    case 'Ⓨ':
                    case 'Ｙ':
                        output.Append('Y');
                        break;
                    case 'ý':
                    case 'ÿ':
                    case 'ŷ':
                    case 'ƴ':
                    case 'ȳ':
                    case 'ɏ':
                    case 'ʎ':
                    case 'ẏ':
                    case 'ẙ':
                    case 'ỳ':
                    case 'ỵ':
                    case 'ỷ':
                    case 'ỹ':
                    case 'ỿ':
                    case 'ⓨ':
                    case 'ｙ':
                        output.Append('y');
                        break;
                    case '⒴':
                        output.Append('(');
                        output.Append('y');
                        output.Append(')');
                        break;
                    case 'Ź':
                    case 'Ż':
                    case 'Ž':
                    case 'Ƶ':
                    case 'Ȝ':
                    case 'Ȥ':
                    case 'ᴢ':
                    case 'Ẑ':
                    case 'Ẓ':
                    case 'Ẕ':
                    case 'Ⓩ':
                    case 'Ⱬ':
                    case 'Ꝣ':
                    case 'Ｚ':
                        output.Append('Z');
                        break;
                    case 'ź':
                    case 'ż':
                    case 'ž':
                    case 'ƶ':
                    case 'ȝ':
                    case 'ȥ':
                    case 'ɀ':
                    case 'ʐ':
                    case 'ʑ':
                    case 'ᵶ':
                    case 'ᶎ':
                    case 'ẑ':
                    case 'ẓ':
                    case 'ẕ':
                    case 'ⓩ':
                    case 'ⱬ':
                    case 'ꝣ':
                    case 'ｚ':
                        output.Append('z');
                        break;
                    case '⒵':
                        output.Append('(');
                        output.Append('z');
                        output.Append(')');
                        break;
                    case '⁰':
                    case '₀':
                    case '⓪':
                    case '⓿':
                    case '０':
                        output.Append('0');
                        break;
                    case '¹':
                    case '₁':
                    case '①':
                    case '⓵':
                    case '❶':
                    case '➀':
                    case '➊':
                    case '１':
                        output.Append('1');
                        break;
                    case '⒈':
                        output.Append('1');
                        output.Append('.');
                        break;
                    case '⑴':
                        output.Append('(');
                        output.Append('1');
                        output.Append(')');
                        break;
                    case '²':
                    case '₂':
                    case '②':
                    case '⓶':
                    case '❷':
                    case '➁':
                    case '➋':
                    case '２':
                        output.Append('2');
                        break;
                    case '⒉':
                        output.Append('2');
                        output.Append('.');
                        break;
                    case '⑵':
                        output.Append('(');
                        output.Append('2');
                        output.Append(')');
                        break;
                    case '³':
                    case '₃':
                    case '③':
                    case '⓷':
                    case '❸':
                    case '➂':
                    case '➌':
                    case '３':
                        output.Append('3');
                        break;
                    case '⒊':
                        output.Append('3');
                        output.Append('.');
                        break;
                    case '⑶':
                        output.Append('(');
                        output.Append('3');
                        output.Append(')');
                        break;
                    case '⁴':
                    case '₄':
                    case '④':
                    case '⓸':
                    case '❹':
                    case '➃':
                    case '➍':
                    case '４':
                        output.Append('4');
                        break;
                    case '⒋':
                        output.Append('4');
                        output.Append('.');
                        break;
                    case '⑷':
                        output.Append('(');
                        output.Append('4');
                        output.Append(')');
                        break;
                    case '⁵':
                    case '₅':
                    case '⑤':
                    case '⓹':
                    case '❺':
                    case '➄':
                    case '➎':
                    case '５':
                        output.Append('5');
                        break;
                    case '⒌':
                        output.Append('5');
                        output.Append('.');
                        break;
                    case '⑸':
                        output.Append('(');
                        output.Append('5');
                        output.Append(')');
                        break;
                    case '⁶':
                    case '₆':
                    case '⑥':
                    case '⓺':
                    case '❻':
                    case '➅':
                    case '➏':
                    case '６':
                        output.Append('6');
                        break;
                    case '⒍':
                        output.Append('6');
                        output.Append('.');
                        break;
                    case '⑹':
                        output.Append('(');
                        output.Append('6');
                        output.Append(')');
                        break;
                    case '⁷':
                    case '₇':
                    case '⑦':
                    case '⓻':
                    case '❼':
                    case '➆':
                    case '➐':
                    case '７':
                        output.Append('7');
                        break;
                    case '⒎':
                        output.Append('7');
                        output.Append('.');
                        break;
                    case '⑺':
                        output.Append('(');
                        output.Append('7');
                        output.Append(')');
                        break;
                    case '⁸':
                    case '₈':
                    case '⑧':
                    case '⓼':
                    case '❽':
                    case '➇':
                    case '➑':
                    case '８':
                        output.Append('8');
                        break;
                    case '⒏':
                        output.Append('8');
                        output.Append('.');
                        break;
                    case '⑻':
                        output.Append('(');
                        output.Append('8');
                        output.Append(')');
                        break;
                    case '⁹':
                    case '₉':
                    case '⑨':
                    case '⓽':
                    case '❾':
                    case '➈':
                    case '➒':
                    case '９':
                        output.Append('9');
                        break;
                    case '⒐':
                        output.Append('9');
                        output.Append('.');
                        break;
                    case '⑼':
                        output.Append('(');
                        output.Append('9');
                        output.Append(')');
                        break;
                    case '⑩':
                    case '⓾':
                    case '❿':
                    case '➉':
                    case '➓':
                        output.Append('1');
                        output.Append('0');
                        break;
                    case '⒑':
                        output.Append('1');
                        output.Append('0');
                        output.Append('.');
                        break;
                    case '⑽':
                        output.Append('(');
                        output.Append('1');
                        output.Append('0');
                        output.Append(')');
                        break;
                    case '⑪':
                    case '⓫':
                        output.Append('1');
                        output.Append('1');
                        break;
                    case '⒒':
                        output.Append('1');
                        output.Append('1');
                        output.Append('.');
                        break;
                    case '⑾':
                        output.Append('(');
                        output.Append('1');
                        output.Append('1');
                        output.Append(')');
                        break;
                    case '⑫':
                    case '⓬':
                        output.Append('1');
                        output.Append('2');
                        break;
                    case '⒓':
                        output.Append('1');
                        output.Append('2');
                        output.Append('.');
                        break;
                    case '⑿':
                        output.Append('(');
                        output.Append('1');
                        output.Append('2');
                        output.Append(')');
                        break;
                    case '⑬':
                    case '⓭':
                        output.Append('1');
                        output.Append('3');
                        break;
                    case '⒔':
                        output.Append('1');
                        output.Append('3');
                        output.Append('.');
                        break;
                    case '⒀':
                        output.Append('(');
                        output.Append('1');
                        output.Append('3');
                        output.Append(')');
                        break;
                    case '⑭':
                    case '⓮':
                        output.Append('1');
                        output.Append('4');
                        break;
                    case '⒕':
                        output.Append('1');
                        output.Append('4');
                        output.Append('.');
                        break;
                    case '⒁':
                        output.Append('(');
                        output.Append('1');
                        output.Append('4');
                        output.Append(')');
                        break;
                    case '⑮':
                    case '⓯':
                        output.Append('1');
                        output.Append('5');
                        break;
                    case '⒖':
                        output.Append('1');
                        output.Append('5');
                        output.Append('.');
                        break;
                    case '⒂':
                        output.Append('(');
                        output.Append('1');
                        output.Append('5');
                        output.Append(')');
                        break;
                    case '⑯':
                    case '⓰':
                        output.Append('1');
                        output.Append('6');
                        break;
                    case '⒗':
                        output.Append('1');
                        output.Append('6');
                        output.Append('.');
                        break;
                    case '⒃':
                        output.Append('(');
                        output.Append('1');
                        output.Append('6');
                        output.Append(')');
                        break;
                    case '⑰':
                    case '⓱':
                        output.Append('1');
                        output.Append('7');
                        break;
                    case '⒘':
                        output.Append('1');
                        output.Append('7');
                        output.Append('.');
                        break;
                    case '⒄':
                        output.Append('(');
                        output.Append('1');
                        output.Append('7');
                        output.Append(')');
                        break;
                    case '⑱':
                    case '⓲':
                        output.Append('1');
                        output.Append('8');
                        break;
                    case '⒙':
                        output.Append('1');
                        output.Append('8');
                        output.Append('.');
                        break;
                    case '⒅':
                        output.Append('(');
                        output.Append('1');
                        output.Append('8');
                        output.Append(')');
                        break;
                    case '⑲':
                    case '⓳':
                        output.Append('1');
                        output.Append('9');
                        break;
                    case '⒚':
                        output.Append('1');
                        output.Append('9');
                        output.Append('.');
                        break;
                    case '⒆':
                        output.Append('(');
                        output.Append('1');
                        output.Append('9');
                        output.Append(')');
                        break;
                    case '⑳':
                    case '⓴':
                        output.Append('2');
                        output.Append('0');
                        break;
                    case '⒛':
                        output.Append('2');
                        output.Append('0');
                        output.Append('.');
                        break;
                    case '⒇':
                        output.Append('(');
                        output.Append('2');
                        output.Append('0');
                        output.Append(')');
                        break;
                    case '«':
                    case '»':
                    case '“':
                    case '”':
                    case '„':
                    case '″':
                    case '‶':
                    case '❝':
                    case '❞':
                    case '❮':
                    case '❯':
                    case '＂':
                        output.Append('"');
                        break;
                    case '‘':
                    case '’':
                    case '‚':
                    case '‛':
                    case '′':
                    case '‵':
                    case '‹':
                    case '›':
                    case '❛':
                    case '❜':
                    case '＇':
                        output.Append('\'');
                        break;
                    case '‐':
                    case '‑':
                    case '‒':
                    case '–':
                    case '—':
                    case '⁻':
                    case '₋':
                    case '－':
                        output.Append('-');
                        break;
                    case '⁅':
                    case '❲':
                    case '［':
                        output.Append('[');
                        break;
                    case '⁆':
                    case '❳':
                    case '］':
                        output.Append(']');
                        break;
                    case '⁽':
                    case '₍':
                    case '❨':
                    case '❪':
                    case '（':
                        output.Append('(');
                        break;
                    case '⸨':
                        output.Append('(');
                        output.Append('(');
                        break;
                    case '⁾':
                    case '₎':
                    case '❩':
                    case '❫':
                    case '）':
                        output.Append(')');
                        break;
                    case '⸩':
                        output.Append(')');
                        output.Append(')');
                        break;
                    case '❬':
                    case '❰':
                    case '＜':
                        output.Append('<');
                        break;
                    case '❭':
                    case '❱':
                    case '＞':
                        output.Append('>');
                        break;
                    case '❴':
                    case '｛':
                        output.Append('{');
                        break;
                    case '❵':
                    case '｝':
                        output.Append('}');
                        break;
                    case '⁺':
                    case '₊':
                    case '＋':
                        output.Append('+');
                        break;
                    case '⁼':
                    case '₌':
                    case '＝':
                        output.Append('=');
                        break;
                    case '！':
                        output.Append('!');
                        break;
                    case '‼':
                        output.Append('!');
                        output.Append('!');
                        break;
                    case '⁉':
                        output.Append('!');
                        output.Append('?');
                        break;
                    case '＃':
                        output.Append('#');
                        break;
                    case '＄':
                        output.Append('$');
                        break;
                    case '⁒':
                    case '％':
                        output.Append('%');
                        break;
                    case '＆':
                        output.Append('&');
                        break;
                    case '⁎':
                    case '＊':
                        output.Append('*');
                        break;
                    case '，':
                        output.Append(',');
                        break;
                    case '．':
                        output.Append('.');
                        break;
                    case '⁄':
                    case '／':
                        output.Append('/');
                        break;
                    case '：':
                        output.Append(':');
                        break;
                    case '⁏':
                    case '；':
                        output.Append(';');
                        break;
                    case '？':
                        output.Append('?');
                        break;
                    case '⁇':
                        output.Append('?');
                        output.Append('?');
                        break;
                    case '⁈':
                        output.Append('?');
                        output.Append('!');
                        break;
                    case '＠':
                        output.Append('@');
                        break;
                    case '＼':
                        output.Append('\\');
                        break;
                    case '‸':
                    case '\uff3e':
                        output.Append('^');
                        break;
                    case '\uff3f':
                        output.Append('_');
                        break;
                    case '⁓':
                    case '～':
                        output.Append('~');
                        break;
                    default:
                        output.Append(c);
                        break;
                }
            }
            return output;
        }
    }
}
