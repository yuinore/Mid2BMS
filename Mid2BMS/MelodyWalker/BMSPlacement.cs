using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Windows.Forms;

namespace Mid2BMS
{
    class BMSPlacement
    {
        /// <summary>
        /// wavidの割り当て。
        /// wavnms[i] の音は、wavid が wvs[i] であり、その音の実体が nts[i] に対応する。
        /// </summary>
        List<int> wvs;

        /// <summary>
        /// 重複を許す全ノートの一覧に、voiN（音色番号）を加えたもの
        /// 実体は、MelodyWalkerの取次により、mi3 にあるもの(MidiInterpreter3.nta)と等しい。
        /// </summary>
        List<MNote> nta;

        /// <summary>
        /// 重複を許す全ノートの一覧。
        /// 実体は、MelodyWalkerの取次により、mi3 にあるもの(MidiInterpreter3.ntm)と等しい。
        /// BMS化のため、tの分母を揃える操作を行う。
        /// </summary>
        List<MNote> ntm;

        /// <summary>
        /// ntmChordListのインデックスからntaChordのインデックスを求めます。
        /// </summary>
        public Dictionary<int, int> ntm2nta;

        private bool IsRedMode = true;
        private bool IsPurpleMode = true;

        // 別に無限シーケンスに変える必要性無くね・・・？(真顔)
        // iBMSC対応のため、1P皿には配置しない
        static readonly String[] ChannelTemplate = new String[] { 
            "11", "12", "13", "14", "15", "18", "19", "21", "22", "23", "24", "25", "28", "29", "26", "01",  // 16
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01",  // 32
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01",  // 64
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01",  // 128
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01",
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01",  // 256
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01",
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01",  // 384
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01",
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", 
            "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01", "01"};   // 512

        public String Process(ref int bmwavid, ref int chansNumber, int bmes, int emes,
            bool isRedMode, bool isPurpleMode, bool isDrums, bool isChordMode,
            List<MNote> nta_,
            List<MNote> ntm_,
            List<String> wavnms)
        {
            nta = nta_;
            ntm = ntm_;
            IsRedMode = isRedMode;
            IsPurpleMode = isPurpleMode;
            return GetWavDef(ref bmwavid, wavnms) + "\r\n" + haichiAsBMS(ref chansNumber, bmes, emes, isDrums, isChordMode);
        }

        String IntToInt10(int n, int slen)
        {
            int i;
            String s2 = "";
            String num10 = "0123456789";
            for (i = 0; i < slen; i++)
            {
                s2 = num10[n % 10].ToString() + s2;
                n /= 10;
            }

            return s2;
        }

        String IntToHex36Upper(int n, int slen)
        {
            int i;
            String s2 = "";
            String num36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            for (i = 0; i < slen; i++)
            {
                s2 = num36[n % 36] + s2;
                n /= 36;
            }
            return s2;
        }

        int IntFromHex36(String s)
        {
            int i, n = 0;
            String num36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
            for (i = 0; i < s.Length; i++)
            {
                n = num36.IndexOf(s[i]) % 36 + n * 36;
            }
            return n;
        }

        String GetWavDef(ref int wavid, List<String> wavnms)
        {  // ついでにwavidを割り当てる(wvs)
            int i;
            StringSuruyatu s2 = "";
            wvs = new List<int>();

            for (i = 0; i < wavnms.Count; i++)
            {

                s2 += "#WAV" + IntToHex36Upper(wavid, 2) + " " + wavnms[i] + "\r\n";
                wvs.Add(wavid);
                wavid++;
            }
            return s2 + "\r\n";
        }

        /// <summary>
        /// BMSに配置します。
        /// wavnms, wvs, ntm, nta などを参照します
        /// </summary>
        /// <param name="chansNumber">BMSチャンネルが定義されている配列 BMSPlacement.ChannelTemplate のインデックス</param>
        /// <param name="bmes">開始小節番号(値は1でなければならない)</param>
        /// <param name="emes">終了小節番号(その値を含む)</param>
        /// <returns>生成されたbms</returns>
        String haichiAsBMS(ref int chansNumber, int bmes, int emes, bool isDrums, bool isChordMode)
        { // ソートして１つ１つ
            int i, j, w;
            List<MNote> a2;//, w2;
            StringBuilder s2 = new StringBuilder("");  // これは全然意味が無いよ
            String[] chans2;
            // あ！wavnmsはソートしないからね！！

            IComparer<MNote> iComp = new MNoteComparerInTime();
            ntm.Sort(iComp);  // 変更を加えている可能性が微レ存・・・？？というかもともとソートされている気がしますが。

            w = 1;
            for (i = 0; i < ntm.Count; i++)
            {
                w = (int)getMCBaisu(ntm[i].t.d, w);  // minimum cou baisu
            }
            for (i = 0; i < ntm.Count; i++)
            {
                // 分母を w にする
                ntm[i].t.n = ntm[i].t.n * w / ntm[i].t.d;
                ntm[i].t.d = w;
            }

            //alert(ntm)

            // ここでチャンネルの最大値を求めることにしましょう
            // 同時に鳴っている音の最大数を求めます。最大４和音であれば、channelMaxは4です。
            int channelMax = 0;
            List<MNote> uniq = null;  // ついIEnumerableではなくListを使ってしまう

            if (!isDrums)
            {
                int chCount = 0;
                Frac tim = new Frac(-1);
                for (j = 0; j < ntm.Count; j++)
                {
                    if (ntm[j].t == tim)
                    {
                        chCount++;
                        if (chCount > channelMax) channelMax = chCount;
                    }
                    else
                    {
                        chCount = 1;
                        if (chCount > channelMax) channelMax = chCount;
                        tim.SetValue(ntm[j].t);
                    }
                }
            }
            else
            {
                // HashSet<MNote> hset = new HashSet<MNote>();
                // MNote x,yに対して同値関係 x～y ⇔ x.n == y.n を使いたいのですが・・・？？
                // あ、これ分かった、アレや、HashSetじゃなくてLinQ使うやつや！！！
                uniq = ntm.Distinct(x => x.n).OrderBy(x => x.n).ToList();
                // ↑同じノート番号の音は同時に２つ鳴らない、と思ったらReason(←DTM用ソフト)はそういうmidiを書き出すことがあるようで・・・
                // 配置順はノート番号順にする。出現順じゃない。
                channelMax = uniq.Count;
            }

            chans2 = new String[channelMax];  // 配置に用いるBMSチャンネルのリスト
            for (i = 0; i < channelMax; i++)
            {
                if (chansNumber + 1 >= ChannelTemplate.Length) throw new Exception("配置が必要なBMSチャンネル数が多すぎます。トラック数を減らすか、midiファイルを幾つかに分けてください。");
                chans2[i] = ChannelTemplate[chansNumber++];
            }

            if (!isDrums)
            {
                for (i = 0; i < chans2.Length; i++)  // 非常に汚いコードだ（処理時間が数倍かかっている）
                {
                    a2 = new List<MNote>();
                    var a2toNtm = new Dictionary<int, int>();
                    int th = 0;  // 現在の和音番号のインデックス ( 0 <= th < chans2.Length (== channelMax) )
                    Frac ti = -1;  // １つ前の音符の時間
                    // ところでFracってstaticであるべきだったのでは・・・？

                    for (j = 0; j < ntm.Count; j++)
                    {
                        if (ntm[j].t == ti)
                        {
                            th++;
                        }
                        else
                        {
                            th = 0;
                            ti.n = ntm[j].t.n;
                            ti.d = ntm[j].t.d;
                        }
                        if (th == i)
                        {
                            a2.Add(ntm[j]);
                            a2toNtm.Add(a2.Count - 1, j);
                        }
                    }
                    // ひどいコードを書いたからバグが起きてしまったじゃないか！訴訟！
                    //w2 = ntm;  // えええ・・・その書き方酷くないですかね・・・？？
                    //ntm = a2;

                    // a2は現在のチャンネル番号に配置したいノートのコレクション
                    s2.Append(haichiAsBMS__(chans2[i], bmes, emes, (int)a2[0].t.d, a2, a2toNtm, isChordMode));
                    s2.Append("\r\n");
                    //ntm = w2;
                }
            }
            else
            {
                for (i = 0; i < chans2.Length; i++)  // 非常に汚いコードだ（処理時間が数倍かかっている）
                {
                    a2 = ntm.Where(x => x.n == uniq[i].n).ToList();
                    // a2は現在のチャンネル番号に配置したいノートのコレクション
                    s2.Append(haichiAsBMS__(chans2[i], bmes, emes, (int)a2[0].t.d, a2, null, isChordMode));
                    s2.Append("\r\n");
                }
            }
            return s2.ToString();
        }

        /// <summary>
        /// 関数名にアンダースコア付けただけなのやめろ。
        /// すべてのiについて ntmP[i].t.d == n を満たす必要があります
        /// </summary>
        /// <param name="chan">BMSチャンネル</param>
        /// <param name="bmes">開始小節番号(値は1でなければならない)</param>
        /// <param name="emes">終了小節番号(その値を含む)</param>
        /// <param name="n">小節あたりの分解能</param>
        /// <param name="ntmP">MNoteのリスト</param>
        /// <returns></returns>
        String haichiAsBMS__(String chan, int bmes, int emes, int n, List<MNote> ntmP,
             Dictionary<int, int> a2toNtm, bool isChordMode)
        {
            int i, j, x = 0;
            StringBuilder s2 = new StringBuilder("");  // なんでfor文の中で宣言しない
            //alert(ntm)
            for (i = bmes, j = 0; i <= emes; i++)
            {
                x = 0;
                s2.Append("#" + IntToInt10(i, 3) + chan + ":");
                for (; j < ntmP.Count; j++)
                {
                    if (ntmP[j].t < i + 1)
                    // ↑暗黙のキャスト的なことがやってみたかった
                    //    if (ntmP[j].t < new Frac(i + 1))
                    {
                        for (; x < (ntmP[j].t.n % ntmP[j].t.d); x++)
                        {
                            s2.Append("00");
                        }
                        // ↓そうか、ntaへの参照を保持してないのか。処理が重そう（小並感）
                        if (isChordMode)
                        {
                            if (!IsRedMode)
                            {
                                // blue mode
                                s2.Append(IntToHex36Upper(wvs[ntm2nta[a2toNtm[j]]], 2));
                            }
                            else
                            {
                                // red mode
                                s2.Append(IntToHex36Upper(wvs[a2toNtm[j]], 2));
                            }
                        }
                        else
                        {
                            s2.Append(IntToHex36Upper(getTaiousuruWavid(ntmP[j]), 2));
                        }
                        x++;
                    }
                    else
                    {
                        break;
                    }
                }
                for (; x < n; x++) s2.Append("00");
                s2.Append("\r\n");
            }
            return s2.ToString();
        }

        /// <summary>
        /// 関数名にアンダースコア付けただけなのやめろ。
        /// すべてのiについて ntmP[i].t.d == n を満たす必要があります
        /// </summary>
        /// <param name="chan">BMSチャンネル</param>
        /// <param name="bmes">開始小節番号(値は1でなければならない)</param>
        /// <param name="emes">終了小節番号(その値を含む)</param>
        /// <param name="n">小節あたりの分解能</param>
        /// <param name="ntmP">MNoteのリスト</param>
        /// <returns></returns>
        public String haichiTuplesAsBMS(String chan, int bmes, int emes, int n, List<ArrTuple<Frac, int>> ntmP)
        {
            int i, j, x = 0;
            StringBuilder s2 = new StringBuilder("");  // なんでfor文の中で宣言しない
            //alert(ntm)
            for (i = bmes, j = 0; i <= emes; i++)
            {
                x = 0;
                s2.Append("#" + IntToInt10(i, 3) + chan + ":");
                for (; j < ntmP.Count; j++)
                {
                    if (ntmP[j].Item1 < i + 1)
                    // ↑暗黙のキャスト的なことがやってみたかった
                    //    if (ntmP[j].t < new Frac(i + 1))
                    {
                        for (; x < (ntmP[j].Item1.n % ntmP[j].Item1.d); x++)
                        {
                            s2.Append("00");
                        }
                        // ↓そうか、ntaへの参照を保持してないのか。処理が重そう（小並感）
                        s2.Append(IntToHex36Upper(ntmP[j].Item2, 2));
                        x++;
                    }
                    else
                    {
                        break;
                    }
                }
                for (; x < n; x++) s2.Append("00");
                s2.Append("\r\n");
            }
            return s2.ToString();
        }


        /// <summary>
        /// availability==1 の深さで、対応するノートを探す。
        /// みつかったら、その音の wavid を返す。
        /// ntm から nta を探している。
        /// ・・・O(n)も掛かってる気がするのですがそれは
        /// Dictionary使えーーー
        /// </summary>
        int getTaiousuruWavid(MNote mn)
        {
            if (!IsRedMode && !IsPurpleMode)  // blue
            {
                int i;
                for (i = 0; i < nta.Count; i++)
                {
                    //if (nta[i].n == mn.n && nta[i].l == mn.l && nta[i].v == mn.v && nta[i].VoiceN == mn.VoiceN) return wvs[i];
                    if (nta[i].n == mn.n && nta[i].l == mn.l && nta[i].v == mn.v) return wvs[i];
                }
                //alert("wave NAIYO!");
                return -1;
            }
            else if (IsPurpleMode)  // purple
            {
                int i;
                for (i = 0; i < nta.Count; i++)
                {
                    if (nta[i].n == mn.n && nta[i].l == mn.l && nta[i].v == mn.v && nta[i].prev.n == mn.prev.n) return wvs[i];
                }
                //alert("wave NAIYO!");
                return -1;
            }
            else  // red
            {
                int i;
                for (i = 0; i < ntm.Count; i++)
                {
                    //if (ntm[i].n == mn.n && ntm[i].l == mn.l && ntm[i].v == mn.v && ntm[i].VoiceN == mn.VoiceN && ntm[i].t == mn.t) return wvs[i];
                    if (ntm[i].n == mn.n && ntm[i].l == mn.l && ntm[i].v == mn.v && ntm[i].t == mn.t) return wvs[i];
                }
                //alert("wave NAIYO!");
                return -1;
            }
        }

        /// <summary>
        /// 最小公倍数を求めます。
        /// </summary>
        int getMCBaisu(int a, int b)
        {
            int i, n = a;
            for (i = a; i >= 2; i--)
            {
                if (a % i == 0 && b % i == 0)
                {
                    a /= i;
                    b /= i;
                    break;
                }
            }
            return n * b;
        }
        /// <summary>
        /// 最小公倍数を求めます。
        /// </summary>
        long getMCBaisu(long a, long b)
        {
            long i, n = a;
            for (i = a; i >= 2; i--)
            {
                if (a % i == 0 && b % i == 0)
                {
                    a /= i;
                    b /= i;
                    break;
                }
            }
            return n * b;
        }

    }
}