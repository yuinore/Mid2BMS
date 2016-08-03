using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using System.Xml.Serialization;

namespace Mid2BMS
{
    /// <summary>
    /// 2007年の6月～7月にかけて作成されました。
    /// その頃にBMS配置も可能になっていました。
    /// </summary>
    class MidInterpreter3
    {
        /// <summary>
        /// 重複のないノートの一覧。あるいはキー音の一覧。
        /// [ノート番号, 音符長さ.nume, 音符長さ.deno] を要素に持つ配列。
        /// NotesSingle
        /// 一時的に使用する目的。
        /// </summary>
        private List<MNote> nts;

        /// <summary>
        /// nts の配列の全要素に voiN を加えたもの。
        /// voiNがobsoleteになったことによって、ntsとntaは一致するようになった。
        /// 重複のないノートの一覧。あるいはキー音の一覧。
        /// publicだけど変更は加えないで欲しいって、おかしいですか？
        /// </summary>
        public List<MNote> nta;  // notes - all
        
        /// <summary>
        /// 重複を許す全ノートの一覧。あるいはBMS配置。
        /// [ノート番号, 音符長さ.nume, 音符長さ.deno, voiN, t1, t2] を要素に持つ配列。
        /// ちなみに t は 1オリジンです多分。1小節目の頭の音符の時間が 1 です。
        /// NotesMidi
        /// </summary>
        public List<MNote> ntm;    // notes - midi-like

        public String margintime_beats = "12.0";

        /*
        /// <summary>
        /// 重複のない和音(ノート集合)の一覧。あるいはキー音の一覧。
        /// </summary>
        public Dictionary<Frac, HashSet<MNote>> ntaChord = new Dictionary<Frac, HashSet<MNote>>();
        */
        /// <summary>
        /// 重複のない和音(ノート集合)の一覧。あるいはblue/purpleモード時のキー音の一覧。
        /// Distinct使うからHashSet使う意味ないもんねー
        /// </summary>
        public List<List<MNote>> ntaChord = new List<List<MNote>>();


        /// <summary>
        /// 重複を許す全和音(ノート集合)の一覧。あるいはBMS配置。ただしtは0で固定される。
        /// </summary>
        private Dictionary<Frac, HashSet<MNote>> ntmChordSet = new Dictionary<Frac, HashSet<MNote>>();
        
        /// <summary>
        /// 重複を許す全和音(ノート集合)の一覧。あるいはBMS配置及びredモード時のキー音(命名に用いる)。
        /// </summary>
        public List<List<MNote>> ntmChordList = new List<List<MNote>>();

        /// <summary>
        /// ntmChordListのインデックスからntaChordのインデックスを求めます。
        /// </summary>
        public Dictionary<int, int> ntm2nta = new Dictionary<int, int>();


        public bool ChordModeEnabled = true;


        public String walkOnAMelodyV2(MidiStruct tanon_ms, int TrackIndex, String TrackName, String s, Frac midiTime, bool isPurpleMode)
        {
            // Mid2BMS、WaveSplitter、そしてその他のすべての、
            // すべてのはじまり
            int j, j2;
            long l = 8, l2 = -1;
            int o = 5, ddo = 0, k = 0, v = 60;
            StringSuruyatuSafe s2 = new StringSuruyatuSafe();
            Frac t = new Frac(0);
            //int voiN = 0;  // voice number 音色番号 (outdated)
            //int lastVoiN = 0;
            //bool ihave = true;  // obsoleteです。voiNを使用していた頃の名残。1つのMMLで複数のトラックを扱うことが出来る
            //int lastJ = 0;  // obsoleteです。voiNを使用していた頃の名残。
            //long[] lastDt = new long[2] { 0, 0 };  // obsoleteです。voiNを使用していた頃の名残。
            Frac subTiming = new Frac(0, 1);  // Sub{～}は入れ子には出来ません
            int quartDash = 1;  // 0 でオン。''を使用した和音
            MNote prevnote = null; // for purple mode

            //t.Add(midiTime);// バグだね
            t.Add(1);  // 重要ぽい

            nta = new List<MNote>();
            ntm = new List<MNote>();

            MidiTrack mt = new MidiTrack(TrackName);
            MidiTrackWriter mtw = new MidiTrackWriter(mt, tanon_ms);  // 単音midiを書き出すためのデータ
            tanon_ms.tracks.Add(mt);

            //mtw.AddRest(4);
            //midiTime.Add(4);
            mtw.AddRest(midiTime);

            s += "     ";

            Frac margintime_frac = new Frac();

            //while (ihave)
            {
                //ihave = false;
                nts = new List<MNote>();
                //lastVoiN = voiN;

                // 一文字ずつ構文解析
                for (j = 0; j < s.Length - 4; j++)
                {

                    if (s[j] == '^' || s[j] == 'r')
                    {
                        t.Add(new Frac(quartDash, l));
                        continue;
                    }
                    if ("cdefgab".IndexOf(s[j]) >= 0)
                    {
                        //ntm.Add(getNextNote2EX(s, j, o + ddo, l, v,  k, t, voiN, quartDash));
                        if (!isPurpleMode)
                        {
                            // なぜお前は同じような関数を3回も呼んでいるんだ？
                            ntm.Add(getNextNote2EX(s, j, o + ddo, l, v, k, t, quartDash));
                            if (INeed(getNextNote(s, j, o + ddo, l, v, k), isPurpleMode))
                            {
                                nts.Add(getNextNote(s, j, o + ddo, l, v, k));
                            }
                            // JavaScriptにもセットってあったんだろうか（過去形
                            // まあどうせ多くても数千ノーツだから速度的にはこれ↑で問題ないんだろうけど
                            // purple modeのchord modeは未対応(常識的に考えて)

                            if (ChordModeEnabled)
                            {
                                MNote mn = ntm[ntm.Count - 1];  // 時間情報(t)付き
                                HashSet<MNote> set1;
                                if (ntmChordSet.TryGetValue(mn.t, out set1) == false)
                                {
                                    ntmChordSet.Add(new Frac(mn.t), set1 = (new HashSet<MNote>()));  // MNoteのEqualsって・・・？　←今実装しました
                                }
                                // 不変型でないオブジェクトをdictionaryのキーにして良いのか？？？？？

                                //MNote note = ntm.Last();  // ←遅そう
                                //MNote note = new MNote(mn);

                                //note.availability = 0;  // クソ
                                //note.t = new Frac(0);  // まだだ、まだ情報を消す時間じゃない

                                set1.Add(new MNote(mn));
                                // ↑重複する場合は追加されない（まあ重複は絶対にしないわけだが
                                // setを使う理由はSetComparerが使いたいからですね
                            }
                        }
                        else
                        {
                            // prevnote2 は null であってはならない
                            MNote prevnote2 = prevnote;  // previous note (for portament)

                            MNote nextnote_time = new MNote(getNextNote2EX(s, j, o + ddo, l, v, k, t, quartDash), prevnote2);  // an element of "ntm"
                            MNote nextnote_prev = new MNote(getNextNote(s, j, o + ddo, l, v, k), prevnote2);  // an element of "nts"

                            if (prevnote2 == null) prevnote2 = nextnote_prev;
                            nextnote_time.prev = prevnote2;
                            nextnote_prev.prev = prevnote2;

                            ntm.Add(nextnote_time);
                            if (INeed(nextnote_prev, isPurpleMode))
                            {
                                nts.Add(nextnote_prev);
                            }

                            prevnote = nextnote_prev;  // update nextnote
                        }
                        ddo = 0;
                        continue;
                    }
                    if (s[j] == 'l')
                    {
                        l = 0;
                        while (s.Length >= j + 2 && '0' <= s[j + 1] && s[j + 1] <= '9')
                        {
                            l = l * 10 + (s[j + 1] - '0');
                            j++;
                        }
                        continue;
                    }
                    if (s[j] == 'v')
                    {  // v* または v** または v*** のみ有効
                        v = s[j + 1] - '0';
                        if ("0123456789".IndexOf(s[j + 2]) >= 0)
                        {
                            v = (s[j + 1] - '0') * 10 + (s[j + 2] - '0');
                            if ("0123456789".IndexOf(s[j + 3]) >= 0)
                            {
                                v = (s[j + 1] - '0') * 100 + (s[j + 2] - '0') * 10 + (s[j + 3] - '0');
                                if ("0123456789".IndexOf(s[j + 4]) >= 0)
                                {
                                    throw new Exception("vの数値指定は3文字まで！ around:" + s.Substring(Math.Max(0, j - 20), Math.Min(s.Length, j + 20)));
                                }
                                j++;
                            }
                            j++;
                        }
                        j++;
                        continue;
                    }
                    if (s[j] == 'k')
                    {  // k* または k** のみ有効
                        k = (s[j + 1] - '0');
                        if ("0123456789".IndexOf(s[j + 2]) >= 0)
                        {
                            k = (s[j + 1] - '0') * 10 + (s[j + 2] - '0');
                            if ("0123456789".IndexOf(s[j + 3]) >= 0)
                            {
                                throw new Exception("kの数値指定は2文字まで！ around:" + s.Substring(Math.Max(0, j - 20), Math.Min(s.Length, j + 20)));
                            }
                            j++;
                        }
                        k -= 64;
                        j++;
                        continue;
                    }
                    if (s[j] == 'o')
                    {  // o* のみ有効
                        o = s[j + 1] - '0';
                        j++;
                        continue;
                    }
                    if (s[j] == '>')
                    {
                        o++;
                        continue;
                    }
                    if (s[j] == '<')
                    {
                        o--;
                        continue;
                    }
                    if (s[j] == '`')
                    {
                        ddo++;
                        continue;
                    }
                    if (s[j] == '"')
                    {
                        ddo--;
                        continue;
                    }
                    if (s[j] == '.')
                    {
                        t.Add(new Frac(quartDash, l * 2));
                        continue;
                    }
                    /*if (s[j] == '@')
                    {
                        if (nts.Count > 0)
                        {
                            ihave = true;
                            voiN = (s[j + 1] - '0');
                            j += 2;
                            break;
                        }
                        else
                        {
                            voiN = (s[j + 1] - '0');
                            lastVoiN = voiN; // ←
                            j++;
                        }
                        continue;
                    }*/
                    if (s[j] == '{')
                    {
                        subTiming = new Frac(t);
                        continue;
                    }
                    if (s[j] == '}')
                    {
                        t = new Frac(subTiming);
                        continue;
                    }
                    if (s[j] == '\'')
                    {
                        if ((quartDash = 1 - quartDash) != 0)
                        {  // オフになったら どうするの
                            t.Add(new Frac(quartDash, l));
                        }
                        continue;
                    }

                }
                // ******** 構文解析ここまで ********

                // ただし、s中に"@"が来て、解析が中断された可能性がある。

                //lastJ = j;



                /*for (j = 1; j < nts.Count; j++)
                {
                    if (nts[j - 1].n > nts[j].n)
                    {
                        MNote.Swap(nts[j - 1],nts[j]);
                        j -= 2; if (j < 0) j = 0;
                    }
                }

                for (j = 1; j < nts.Count; j++)
                {
                    if (nts[j - 1].l > nts[j].l)
                    {
                        MNote.Swap(nts[j - 1], nts[j]);
                        j -= 2; if (j < 0) j = 0;
                    }
                }*/
                //MNote[] nts_array = nts.GetRawArray();
                //IComparer iComp = new MNoteComparerInGate();
                //Array.Sort(nts.GetRawArray(), 0, nts.Count, iComp);
                //nts.SetRawArray(nts_array);
                //List<MNote>.Sort((System.Comparison<MNote>)(new List<MNote>()));

                IComparer<MNote> iComp = new MNoteComparerInGate();
                nts.Sort(iComp);

                /*if (lastVoiN != 0 || s2 != "")
                {
                    s2 += "\r\n@" + lastVoiN;
                }*/

                //lastDt = new long[2] { l, o };
                l = -1; l2 = -1;
                o = -1;
                v = -1;  // 再初期化
                margintime_frac = new Frac((int)Math.Ceiling(Convert.ToDouble(margintime_beats)), 4);
                int margintime_mmllength = (int)Math.Ceiling(Convert.ToDouble(margintime_beats) / 4);
                for (j = 0; j < nts.Count; j++)
                {
                    // purplemode は mml 非対応

                    #region nts[j] を mml で表現するためにつらつらと書く
                    if (nts[j].v != v)
                    {
                        v = nts[j].v;
                        s2 += "\r\n" + "v" + v;

                        l = nts[j].l.d;
                        l2 = nts[j].l.n;
                        s2 += "\r\n" + " l" + l;

                        o = (nts[j].n / 12);
                        s2 += "\r\n" + "  o" + (nts[j].n / 12) + " ";
                    }
                    if (nts[j].l.d != l || nts[j].l.n != l2)
                    {
                        l = nts[j].l.d;
                        l2 = nts[j].l.n;
                        s2 += "\r\n" + " l" + l;

                        o = (nts[j].n / 12);
                        s2 += "\r\n" + "  o" + (nts[j].n / 12) + " ";
                    }
                    if ((nts[j].n / 12) != o)
                    {
                        o = (nts[j].n / 12);
                        s2 += "\r\n" + "  o" + (nts[j].n / 12) + " ";
                    }
                    s2 += "c c+d d+e f f+g g+a a+b ".Substring((nts[j].n % 12) << 1, 2);
                    for (j2 = 1; j2 < nts[j].l.n; j2++)
                    {
                        s2 += "^";
                    }
                    for (int r1n = 0; r1n < margintime_mmllength; r1n++)
                    {
                        s2 += "r1" + " ";// "r1r1r1"
                    }
                    #endregion

                    #region Midiへ直接書き出し
                    {
                        Frac lpp;

                        if (ChordModeEnabled == false)
                        {
                            if (isPurpleMode)
                            {
                                // previous note (dummy)
                                Frac dummynote_length = new Frac(1, 4);  // 1拍 (0.25小節)
                                lpp = new Frac(dummynote_length);
                                lpp.Add(margintime_frac);  // 3小節のマージン
                                // todo:margintime_mmlを参照する
                                mtw.AddNote(nts[j].prev.n, nts[j].v, dummynote_length, lpp);  // 長さ = 1拍; velocity = current noteと同じ
                            }

                            lpp = new Frac(nts[j].l);
                            lpp.Add(margintime_frac);  // 3小節のマージン
                            // todo:margintime_mmlを参照する
                            mtw.AddNote(nts[j].n, nts[j].v, nts[j].l, lpp);
                        }
                    }
                    #endregion

                    //s2+="\r\n";
                }
                //alert(nts);

                for (j = 0; j < nts.Count; j++)
                {
                    //nta.Add(new MNote(nts[j], lastVoiN));
                    nta.Add(new MNote(nts[j]));  // もはや意味ない

                }
                //l = lastDt[0];
                //o = (int)lastDt[1];
            }  // while(ihave)

            //alert(ntm)

            s2 = new StringSuruyatuSafe() + "// Track " + (TrackIndex) + (TrackIndex == 0 ? " (conductor track)" : "") +"\r\n"
                + "Track(" + (TrackIndex + 1) + ") q100\r\n"
                + "TrackName = {\"" + TrackName + "\"} r1r1r1r1\r\n" + s2.ToString();

            if (ChordModeEnabled == false)
            {
                midiTime.n = mtw.Tick.n;  // 値を呼び出し元に返す
                midiTime.d = mtw.Tick.d;
                mtw.Close();
                //mt.AddEndOfTrack(tanon_ms);// ここでAddEndOfTrackしたらダメでしょ
            }

            //ntaChord = new HashSet<HashSet<MNote>>(ntmChord.Select(x => x.Value));
            // たった１行で重複を削除出来る！すごい！
            // まあ順序は保持されないけど別に良いよね、setだし
            // と思ったらDistinctとかいう便利なメソッドがあったの忘れてたわ
            // ソートせずにunique出来るのって神では～？

            if (ChordModeEnabled)
            {
                nta = ntm = nts = null;

                Func<HashSet<MNote>, HashSet<MNote>> RemoveTimeInformation = x =>
                {
                    HashSet<MNote> z = new HashSet<MNote>();
                    foreach (var y in x)
                    {
                        var y2 = new MNote(y);
                        y2.t = new Frac(0);
                        z.Add(y2);
                    }
                    return z;
                };

                var sorted = ntmChordSet.OrderBy(x => x.Key).Select(x => x.Value);

                var ntaChord_hset = new List<HashSet<MNote>>(
                    sorted
                    .Select(RemoveTimeInformation)
                    .Distinct(HashSet<MNote>.CreateSetComparer()));  // 集合の等価性を使用してDistinctする
                ntaChord = new List<List<MNote>>(
                    ntaChord_hset.Select(x => x.OrderBy(y => y.n).ToList()));

                var ntmChord_hset = new List<HashSet<MNote>>(
                    sorted);
                ntmChordList = new List<List<MNote>>(
                    ntmChord_hset.Select(x => x.OrderBy(y => y.n).ToList())
                    );

                var ntmChord_hset_RemoveTimeInfo = ntmChord_hset.Select(RemoveTimeInformation).ToList();
                for (int srcIdx = 0; srcIdx < ntmChord_hset.Count; srcIdx++)
                {
                    int dstIdx = ntaChord_hset.FindIndex(x => x.SetEquals(ntmChord_hset_RemoveTimeInfo[srcIdx]));
                    ntm2nta.Add(srcIdx, dstIdx);
                }

                // 単音midi書き出し
                {
                    Frac fraczero = new Frac(0);
                    double MaxLength = 0;

                    foreach (var chord in ntaChord)
                    {
                        MaxLength = 0;
                        foreach (var mnote in chord)
                        {
                            MaxLength = Math.Max(MaxLength, (double)mnote.l);
                            mtw.AddNote(mnote.n, mnote.v, mnote.l, fraczero);
                        }
                        mtw.AddRest(margintime_frac);
                        mtw.AddRest(new Frac(MaxLength));
                    }

                    midiTime.n = mtw.Tick.n;  // 値を呼び出し元に返す
                    midiTime.d = mtw.Tick.d;
                    mtw.Close();
                    //mt.AddEndOfTrack(tanon_ms);// ここでAddEndOfTrackしたらダメでしょ
                }
            }

            return s2.ToString();
        }

        /// <summary>
        /// nts から適当な同値関係を用いて a と同じノートを探索します。
        /// 次のノートが新たに必要かどうかを判定します。
        /// ２分探索すらしてないとか正直どうなの（笑）
        /// </summary>
        bool INeed(MNote a, bool isPurpleMode)
        {
            int i;
            for (i = 0; i < nts.Count; i++)
            {
                // prev は null ではない
                if ((nts[i].n == a.n) && (nts[i].l == a.l) && (nts[i].v == a.v) && (!isPurpleMode || (nts[i].prev.n == a.prev.n)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// s[i]はaからg
        /// [note, length.n, length.d]
        /// availabilityが0の（tを含まない）MNote（tを含む）を返します。
        /// </summary>
        /// <param name="s"></param>
        /// <param name="i"></param>
        /// <param name="o"></param>
        /// <param name="l"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        MNote getNextNote(String s, int i, int o, long l, int v, int k)
        {
            int j;
            long[] a = new long[] { 0, 1, 0 };
            a[2] = l;
            a[0] = "ccddeffggaab".IndexOf(s[i]) + o * 12 + k;
            for (j = i + 1; j < s.Length; j++)
            {
                if ("cdefgabr".IndexOf(s[j]) >= 0)
                    break;
                //if("0123456789".indexOf(s.charAt(j))>=0) {
                //  if()
                //}
                if (s[j] == '.')
                {
                    a[1] *= 3; a[2] *= 2;
                }
                if (s[j] == '-')
                    a[0]--;
                if (s[j] == '+')
                    a[0]++;
                if (s[j] == '#')
                    a[0]++;
                if (s[j] == '^')
                    a[1]++;
            }
            for (j = (int)a[1]; j >= 2; j--)
            {
                if (a[2] % j == 0 && a[1] % j == 0)
                {
                    a[1] /= j; a[2] /= j;
                    break;
                }
            }
            return new MNote((int)a[0], a[1], a[2], v);
        }

        /// <summary>
        /// 【tは変更される。】ただし、「^」の分はふやしません。
        /// availabilityが2の（tを含む）MNote（tを含む）を返します。
        /// </summary>
        MNote getNextNote2EX(String s, int i, int o, long l, int v, int k, Frac t, 
            //int vn,
            int QD)
        {
            int j;
            int[] a = new int[] { 0, 1 };
            long a_2 = 0;
            a_2 = l;
            a[0] = "ccddeffggaab".IndexOf(s[i]) + o * 12 + k;
            for (j = i + 1; j < s.Length; j++)
            {
                if ("cdefgabr".IndexOf(s[j]) >= 0)
                    break;
                //if("0123456789".indexOf(s.charAt(j))>=0) {
                //  if()
                //}
                if (s[j] == '.')
                {
                    a[1] *= 3; a_2 *= 2;
                }
                if (s[j] == '-')
                    a[0]--;
                if (s[j] == '+')
                    a[0]++;
                if (s[j] == '#')
                    a[0]++;
                if (s[j] == '^')
                    a[1]++;
            }
            for (j = a[1]; j >= 2; j--)
            {
                if (a_2 % j == 0 && a[1] % j == 0)
                {
                    a[1] /= j; a_2 /= j;
                    break;
                }
            }
            //MNote aaa = new MNote(a[0], a[1], a_2, v, vn, t.n, t.d);  // n l1 l2 vN t1 t2
            MNote aaa = new MNote(a[0], a[1], a_2, v, t.n, t.d);  // n l1 l2 vN t1 t2
            t.Add(new Frac(QD, l));
            return aaa;
        }
    }
}