using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    /// <summary>
    /// チャンネルと、変更前後のwavid、ファイル位置を指定して、データの変更を試みます。
    /// RANDOM などの出現に対しても正常に処理をするという特徴があります。（ほんとかな？）
    /// もし indexInLine が行の範囲を超えていたら、次の行から検索を始めます。
    /// 変更順序はファイルにおける出現順であるため、
    /// 複数のチャンネルに渡って同じwavidのノートが書かれている場合は、
    /// 正常に変更できない可能性があります。
    /// </summary>
    static class BMSRawModifier
    {
        /// <summary>
        /// #MMMCC:内のwavid を１個だけ置き換えます。
        /// 変更できたら 0 を返します。
        /// 変更できなかったら -1 を返します。
        /// 変更できなかった場合、ref渡しの引数に変化はありません。
        /// </summary>
        static public int WavidReplace(String[] lines, int oldWavid, int newWavid, ref int indexOfLine, ref int indexInLine)
        {
            int i = indexOfLine;
            int j = indexInLine;
            int wavid;

            if (j < 7)
            {
                j = 7;
            }
            if (j >= lines[i].Length)
            {
                i++;
                j = 7;
            }

            for (; i < lines.Length; i++)
            {
                if (!BMSParser.IsLineOfObjs(lines[i])) continue;

                for (; j < lines[i].Length; j+=2)
                {
                    wavid = BMSParser.IntFromHex36(lines[i][j], lines[i][j + 1]);
                    if (wavid < 0) throw new Exception("BMSが不正です");
                    if (wavid == oldWavid)
                    {
                        lines[i] = lines[i].Substring(0, j) + BMSParser.IntToHex36Upper(newWavid) + lines[i].Substring(j + 2);
                        indexOfLine = i;
                        indexInLine = j;
                        return 0;
                    }
                }
                j = 7;
            }

            return -1;
        }

        /// <summary>
        /// #WAVXXのwavid(XX)と、#MMMCC:内のwavid をすべて置き換えます。
        /// 変更できたら変換した数を返します。
        /// 変更できなかったら 0 を返します。
        /// 変更できなかった場合、ref渡しの引数に変化はありません。
        /// </summary>
        static public int WavidReplaceWhole(String[] lines, int oldWavid, int newWavid)
        {
            int i = 0;
            int j = 7;
            int wavid;
            int convertedN = 0;

            if (j >= lines[i].Length)
            {
                i++;
                j = 7;
            }

            for (; i < lines.Length; i++)
            {
                if (!BMSParser.IsLineOfObjs(lines[i]))
                {
                    if (BMSParser.IsLineOfWAVXX(lines[i]))
                    {
                        wavid = BMSParser.IntFromHex36(lines[i][4], lines[i][5]);
                        if (wavid == oldWavid)
                        {
                            lines[i] = lines[i].Substring(0, 4) + BMSParser.IntToHex36Upper(newWavid) + lines[i].Substring(6);
                        }
                    }
                    continue;
                }

                for (; j < lines[i].Length; j += 2)
                {
                    wavid = BMSParser.IntFromHex36(lines[i][j], lines[i][j + 1]);
                    if (wavid < 0) throw new Exception("BMSが不正です");
                    if (wavid == oldWavid)
                    {
                        lines[i] = lines[i].Substring(0, j) + BMSParser.IntToHex36Upper(newWavid) + lines[i].Substring(j + 2);
                        convertedN++;
                    }
                }
                j = 7;
            }

            return convertedN;
        }

    }
}
