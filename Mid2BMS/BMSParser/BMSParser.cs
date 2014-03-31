using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    /// <summary>
    /// 多分 RANDOM 構文には対応しないと思います。当分は。
    /// とりあえず、オブジェクト全てを読み込んで配列にして、
    /// それを変更したものを書き込めればそれでいいかな、と思う。
    /// </summary>
    class BMSParser
    {
        const String NewLine = "\r\n";

        List<BMSObject> bmo = new List<BMSObject>();  // notes
        List<String> defines = new List<String>();
        List<String> others = new List<String>();
        Dictionary<int, int> isWavUsedInBMS = new Dictionary<int, int>();  // int wavid, bool isWavUsedInBMS
        Dictionary<int, string> WaveDef = new Dictionary<int, string>();  // int wavid, String wavfilename

        public double BPM { get; set; }
        public int LNOBJ { get; set; }

        public BMSParser(String bms)
        {
            BPM = 120.0;
            LNOBJ = 0;

            List<String> lines = new List<string>(bms.Split(new String[] { NewLine }, StringSplitOptions.None));

            for (int i = 0; i < lines.Count; i++)
            {
                try
                {
                    if (IsLineOfObjs(lines[i]))
                    {
                        AddObjectsFromLine(lines[i]);
                    }
                    else if (IsLineOfWAVXX(lines[i]))
                    {
                        AddWaveDefinitionFromLine(lines[i]);
                    }
                    else if (IsLineOfDefine(lines[i]))
                    {
                        String defname = lines[i].Substring(1).Split(' ')[0];
                        String defvalue = lines[i].Substring(defname.Length + 2);

                        switch (defname.ToUpper())
                        {
                            case "BPM":
                                BPM = Convert.ToDouble(defvalue);
                                break;
                            case "LNOBJ":
                                LNOBJ = IntFromHex36(defvalue);
                                break;
                            default:
                                defines.Add(lines[i]);
                                break;
                        }
                    }
                    else
                    {
                        others.Add(lines[i]);
                    }
                }
                catch  // 行が読み込めない場合は無視する
                {
                }
            }
            bmo.Sort();
        }

        /// <summary>
        /// Type が BMSObjectType.Wav のオブジェから wavid が同じものをすべて見つけて配列で返します。
        /// なければnullだ！
        /// </summary>
        public List<BMSObject> FindAllWavId(int wavid)
        {
            int existFlag;
            if (isWavUsedInBMS.TryGetValue(wavid, out existFlag) == false || existFlag == 0)
            {
                return null;
            }

            List<BMSObject> ret = new List<BMSObject>();

            for (int i = 0; i < bmo.Count; i++)
            {
                if (bmo[i].Type == BMSObjectType.Wav && bmo[i].wav == wavid)
                {
                    ret.Add(bmo[i]);
                }
            }
            return ret.Count == 0 ? null : ret;
        }

        /*public int FindWavid(int wavid, int StartIndex)
        {
            for (int i = StartIndex; i < bmo.Count; i++)
            {
                if (bmo[i].wav == wavid)
                {
                    return i;
                }
            }
            return 0;
        }*/
        
        public String GetWaveFileName(int wavid) {
            String ret;
            return WaveDef.TryGetValue(wavid, out ret) ? ret : "";
        }

        //******************************************************************
        //*** 行解釈（bmoフィールドを使用するもの）
        //******************************************************************

        public int AddObjectsFromLine(String s)
        {
            if (!IsLineOfObjs(s)) return 1;

            int measure = (s[1] - '0') * 100 + (s[2] - '0') * 10 + (s[3] - '0');
            int channel = IntFromHex16(s[4], s[5]);

            String s1 = s.Substring(7);

            int wavid;
            Frac time;
            Frac j = new Frac(0, s1.Length / 2); // 自動で約分されないという点が重要です。
            for (int i = 0; i < s1.Length; i += 2, j.n++)
            {
                wavid = IntFromHex36(s1[i], s1[i + 1]);
                if (wavid != 0)
                {
                    if (wavid < 0) throw new Exception("BMSが正しくありません：" + s);
                    time = new Frac(measure, 1);
                    time.Add(j);
                    time.Reduce();
                    bmo.Add(new BMSObject(channel, wavid, time));
                    isWavUsedInBMS[wavid] = 1;
                }
            }

            return 0;
        }
        public int AddWaveDefinitionFromLine(String s)
        {
            if (!IsLineOfWAVXX(s)) return 1;
            int wavid = IntFromHex36(s[4], s[5]);
            s = s.Substring(7);
            WaveDef[wavid] = s;
            return 0;
        }


        //******************************************************************
        //*** 補助処理(static)
        //******************************************************************

        /// <summary>
        /// s が #[0-9A-Za-z][0-9A-Za-z][0-9][0-9][0-9]: で始まるなら true を返します。
        /// ただし#???02に当てはまるときは false を返します
        /// というかここ間違ってないか？？？？？？？？？？？？？？？？？？？？？？？？
        /// というか正規表現を使えよーーーー
        /// </summary>
        public static bool IsLineOfObjs(String s)
        {
            if (s.Length < 7) return false;
            if (s[0] == '#' && s[6] == ':')
            {
                for (int i = 1; i < 3; i++)
                {
                    if (('0' <= s[i] && s[i] <= '9') || ('A' <= s[i] && s[i] <= 'Z') || ('a' <= s[i] && s[i] <= 'z')) continue;
                    return false;
                }
                for (int i = 3; i < 6; i++)
                {
                    if ('0' <= s[i] && s[i] <= '9') continue;
                    return false;
                }

                if (s[4] == '0' && s[5] == '2') return false;  // あっ既に例外処理してるじゃん

                return true;
            }
            return false;
        }

        /// <summary>
        /// s が #WAVXX で始まるなら true を返します。
        /// </summary>
        public static bool IsLineOfWAVXX(String s)
        {
            if (s.Length < 7) return false;
            if (s[0] == '#' && s[1] == 'W' && s[2] == 'A' && s[3] == 'V')
            {
                for (int i = 4; i < 6; i++)
                {
                    if (('0' <= s[i] && s[i] <= '9') || ('A' <= s[i] && s[i] <= 'Z') || ('a' <= s[i] && s[i] <= 'z')) continue;
                    return false;
                }
                return true;
            }
            return false;
        }

        /*
        public int TerminalOf(int ObjectIndex)
        {
            bool condition1, condition2;
            for (int i = ObjectIndex; i < bmo.Count; i++)
            {
                // &16 というのは &15 の間違いでは無いでしょうか・・・？
                // さらに言うと &31 の間違いでは？？良く分かっていない
                // まあこのメソッド、使われてないんですけど
                condition1 = ((bmo[i].wav == LNOBJ) && ((bmo[i].ch & 16) == (bmo[ObjectIndex].ch & 16))); // LNOBJ
                condition2 = false; // 0x5?, 0x6?
                if (condition1 || condition2) return i;
            }
            return -1;
        }*/

        /// <summary>
        /// s が # で始まるなら true を返します。
        /// </summary>
        public static bool IsLineOfDefine(String s)
        {
            if (s.Length < 1) return false;
            if (s[0] == '#') return true;
            return false;
        }

        /// <summary>
        /// BMSチャンネルからObjectTypeを取得します。
        /// BMSequenceTypeがObjsでない場合、およびBMSチャンネルが未識別の場合はBMSObjectType.Noneを返します。
        /// LNObjの場合はfalseを返すように修正する予定です
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static BMSObjectType BMSChannelToObjectType(int ch)
        {
            if (ch == 01) return BMSObjectType.Wav;
            if (0x10 <= ch && ch <= 0x2F) return BMSObjectType.Wav;

            switch (ch)
            {
                // あっ、C#には8進数って無いじゃん

                case 0x01: return BMSObjectType.Wav;
                //case 02 //measure length
                case 0x03: return BMSObjectType.Number;  // BPM
                case 0x04: return BMSObjectType.Bmp;
                //case 04: // undefined
                case 0x06: return BMSObjectType.Bmp;
                case 0x07: return BMSObjectType.Bmp;
                case 0x08: return BMSObjectType.ExtendedTempo;
                case 0x09: return BMSObjectType.Stop;

                case 0x99: return BMSObjectType.Text;
                case 0xA0: return BMSObjectType.Extank;
            }

            // otherwise
            switch (ch / 16)
            {
                case 0x1: return BMSObjectType.Wav;
                case 0x2: return BMSObjectType.Wav;
                case 0x3: return BMSObjectType.Wav;
                case 0x4: return BMSObjectType.Wav;
                case 0x5: return BMSObjectType.Wav;
                case 0x6: return BMSObjectType.Wav;

                case 0xD: return BMSObjectType.Wav;
                case 0xE: return BMSObjectType.Wav;
            }

            return BMSObjectType.None;
        }

        /// <summary>
        /// BMSチャンネルからSequenceTypeを取得します。
        /// 未識別のチャンネルが与えられた場合は、BMSequenceType.Objsを返します。
        /// </summary>
        /// <param name="ch">BMSチャンネル</param>
        /// <returns>SequenceType</returns>
        public static BMSequenceType BMSChannelToSequenceType(int ch)
        {
            if (ch == 0x02) return BMSequenceType.Number;

            return BMSequenceType.Objs;
        }

        /*public static Tuple<BMSequenceType, BMSObjectType> BMSChannelToSequenceType(int ch)
        {
            var t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.None, BMSObjectType.None);

            if (ch == 01)
            {
                t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Wav);
                return t;
            }
            if (0x10 <= ch && ch <= 0x2F)
            {
                t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Wav);
                return t;
            }

            switch (ch)
            {
                // あっ、C#には8進数って無いじゃん

                // #MMM02の行だけ例外処理した方が楽だったのでは・・・？？？？？？？？？？？？？？？？

                case 0x01: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Wav); break;
                case 0x02: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Number, BMSObjectType.None); break;
                case 0x03: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Number); break;  // BPM
                case 0x04: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Bmp); break;
                //case 04: // undefined
                case 0x06: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Bmp); break;
                case 0x07: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Bmp); break;
                case 0x08: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.ExtendedTempo); break;
                case 0x09: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Stop); break;

                case 0x99: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Text); break;
                case 0xA0: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Extank); break;
            }

            // otherwise
            switch (ch / 16)
            {
                case 0x1: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Wav); break;
                case 0x2: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Wav); break;
                case 0x3: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Wav); break;
                case 0x4: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Wav); break;
                case 0x5: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Wav); break;
                case 0x6: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Wav); break;

                case 0xD: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Wav); break;
                case 0xE: t = new Tuple<BMSequenceType, BMSObjectType>(BMSequenceType.Objs, BMSObjectType.Wav); break;
            }

            return t;
        }*/


        /// <summary>
        /// x > y のとき正の数を返します
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int MyCompare(BMSParser X, BMSParser Y, BMSObject x, BMSObject y)
        {
            if (x.t != y.t)
            {
                return x.t > y.t ? 1 : -1;
            }
            if (X.GetWaveFileName(x.wav) != Y.GetWaveFileName(y.wav))
            {
                return X.GetWaveFileName(x.wav).CompareTo(Y.GetWaveFileName(y.wav));
            }
            return 0;
        }
        public static String Differentiate(BMSParser X, BMSParser Y)
        {
            var dmyobj = new BMSObject(01, 01, int.MaxValue);
            List<BMSObject> x2 = X.bmo.OrderBy(x => X.GetWaveFileName(x.wav)).OrderBy(x => x.t).Concat(new BMSObject[] { dmyobj }).ToList();
            List<BMSObject> y2 = Y.bmo.OrderBy(x => Y.GetWaveFileName(x.wav)).OrderBy(x => x.t).Concat(new BMSObject[] { dmyobj }).ToList();

            int i, j;
            int errcnt = 0;
            StringSuruyatu s = new StringSuruyatu();

            s += "X : " + (x2.Count - 1) + " objs\r\n";
            s += "Y : " + (y2.Count - 1) + " objs\r\n\r\n";

            s += "******** duplicate check ********\r\nbar\tposition\twavid\r\n";

            for (i = 0; i < x2.Count - 1; i++)
            {
                //if (MyCompare(X, Y, x2[i], x2[i + 1]) == 0)
                if (MyCompare(X, X, x2[i], x2[i + 1]) == 0)
                {
                    s += x2[i] + " is duplicated in X. (" + X.GetWaveFileName(x2[i].wav) + ")\r\n";
                    errcnt++;
                    x2.RemoveAt(i + 1);
                    i--;
                }
            }
            for (i = 0; i < y2.Count - 1; i++)
            {
                if (MyCompare(Y, Y, y2[i], y2[i + 1]) == 0)
                {
                    s += y2[i] + " is duplicated in Y. (" + Y.GetWaveFileName(y2[i].wav) + ")\r\n";
                    errcnt++;
                    y2.RemoveAt(i + 1);
                    i--;
                }
            }

            s += "\r\n******** difference check ********\r\nbar\tposition\twavid\r\n";

            for (i = j = 0; i < x2.Count - 1 || j < y2.Count - 1; )
            {
                if (MyCompare(X, Y, x2[i], y2[j]) == 0)
                {
                    i++; j++;
                }
                else if (MyCompare(X, Y, x2[i], y2[j]) < 0)  // x2[i] < y2[j]
                {
                    s += x2[i] + " is missing in Y. (" + X.GetWaveFileName(x2[i].wav) + ")\r\n";
                    errcnt++;
                    i++;
                }
                else  // x2[i] > y2[j]
                {
                    s += y2[j] + " is missing in X. (" + Y.GetWaveFileName(y2[j].wav) + ")\r\n";
                    errcnt++;
                    j++;
                }
            }

            s += "\r\n******** " + errcnt + " errors found. ********\r\n\r\n";
            s += "******** end of report ********\r\n";

            return s;
        }


        //******************************************************************
        //*** 変換系 (static)
        //******************************************************************
        /// <summary>
        /// 長さが２の36進数に変換します
        /// </summary>
        public static String IntToHex36Upper(int n)
        {
            int i;
            int slen = 2;
            String s2 = "";
            String num36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            for (i = 0; i < slen; i++)
            {
                s2 = num36[n % 36] + s2;
                n /= 36;
            }
            // この関数って、BMSPlacementクラスにも同じものがあって、そこだと例外は出さないのね・・・。
            if (n > 0) throw new Exception("数値(#WAV番号)が変換できる範囲(01～ZZ)を超えています。midiのノート数が多すぎるか、red modeです。＠BMSParser.IntToHex36Upper(int n)");
            return s2;
        }

        public static int IntFromHex36(String s)
        {
            // 昔の私の興味はコードを短くすること、今の私の興味は処理を速くすること、もしかしたらそうなのかもしれない
            int i, n = 0, x;
            String num36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
            for (i = 0; i < s.Length; i++)
            {
                x = num36.IndexOf(s[i]);
                if (x < 0) throw new Exception("36進数に用いられない文字が含まれています＠BMSParser.IntFromHex36(String s)");
                n = x % 36 + n * 36;
            }
            return n;
        }

        /// <summary>
        /// 変換できなかった場合、-1を返します。
        /// </summary>
        public static int IntFromHex36(char cHigh, char cLow)
        {
            String num36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
            int idxH = num36.IndexOf(cHigh);
            int idxL = num36.IndexOf(cLow);
            if (idxH < 0 || idxL < 0) return -1;
            return (idxH % 36) * 36 + idxL % 36;
        }

        /// <summary>
        /// 変換できなかった場合、-1を返します。
        /// </summary>
        public static int IntFromHex16(char cHigh, char cLow)
        {
            String num36 = "0123456789ABCDEF0123456789abcdef";
            int idxH = num36.IndexOf(cHigh);
            int idxL = num36.IndexOf(cLow);
            if (idxH < 0 || idxL < 0) return -1;
            return (idxH % 36) * 36 + idxL % 36;
        }
    }
}
