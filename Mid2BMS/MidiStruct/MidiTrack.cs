using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics; 

namespace Mid2BMS
{
    // MidiTrack が List<MidiEvent> を継承しているのは間違っているのでは・・・？
    // ということを Effective C++ を読んで思った

    // そもそも MidiTrackというクラスが必要なかったという可能性・・・？？

    // まあ今からこのクラスを消すのは面倒だからしないけれど

    // このクラスはフィールドを持ってはならない。
    // 言い換えると、 new MidiTrack(midiTrack.Select(x => x)) は、元の midiTrack と一致しなければならない。

    class MidiTrack : List<MidiEvent>, IEnumerable<MidiEvent>  // ソート関数OrderByを実装する
    {
        public static float SPLIT_BEATS_INTERVAL = 16;
        public static float SPLIT_BEATS_AUTOMATIONLEFT = 1;
        public static float SPLIT_BEATS_AUTOMATIONRIGHT = 14;
        //const String MIDI_TEXT_ENCODING = "Shift_JIS";

        /// <summary>
        /// 空の MidiTrackを作成する。
        /// </summary>
        public MidiTrack()
        {
        }

        /// <summary>
        /// midiとして有効な(比較的)最小のトラックを作成する。
        /// </summary>
        /// <param name="trackname"></param>
        /// <param name="MIDI_TEXT_ENCODING"></param>
        public MidiTrack(String trackname)//, String MIDI_TEXT_ENCODING)
        {
            int port = 0;

            {  // Track Name
                MidiEventMeta me = new MidiEventMeta();
                me.tick = 0;
                me.ch = 0;
                me.id = 0x03;
                me.text = trackname;
                me.bytes = HatoEnc.Encode(trackname);
                me.val = 0;

                this.Add(me);

                // Instrument Name
                me = (MidiEventMeta)me.Clone();
                me.id = 0x04;
                this.Add(me);
            }
            {  // Port
                MidiEventMeta me = new MidiEventMeta();
                me.tick = 0;
                me.ch = 0;
                me.id = 0x21;
                me.text = "";
                me.bytes = new byte[1];
                me.bytes[0] = (byte)port;
                me.val = port;

                this.Add(me);
            }

        }

        /// <summary>
        /// コピーコンストラクタ
        /// </summary>
        /// <param name="iOrderedEnumerable"></param>
        public MidiTrack(IEnumerable<MidiEvent> iOrderedEnumerable)
            : base(iOrderedEnumerable)
        {
            // TODO: Complete member initialization
        }
        
        /// <summary>
        /// 複製を返す。計算時間 O(n^2) 掛かる気がする。良くないのでは？
        /// 高速化するのは「計測」をしてから、って言うじゃないですか
        /// (もし)EndOfTrackが存在した場合は除去します。
        /// </summary>
        public MidiTrack SplitNotes(MidiStruct midistruct, bool isChordMode)
        {
            return new MidiTrack(
                MidiTrack.SplitNotes((new IEnumerable<MidiEvent>[] { 
                    this.OrderBy(x => x.tick) 
                }).DirectSum(), midistruct, new List<bool> { isChordMode }, new List<bool> { false }, new List<bool> { false }).Select(x => x.Item2)
                );
        }

        /// <summary>
        /// 複製を返す。計算時間 O(n^2) 掛かる気がする。良くないのでは？
        /// 高速化するのは「計測」をしてから、って言うじゃないですか
        /// (もし)EndOfTrackが存在した場合は除去します。
        /// 
        /// ソートされた順に、ノートが配置されます。ですから、 x => x.Item2.tick では絶対にソートしないでください。
        /// 
        /// プログラムを「一般化」した結果がこの三角カッコの嵐だよ！！！
        /// </summary>
        public static IEnumerable<ArrTuple<int, MidiEvent>> SplitNotes(
            IEnumerable<ArrTuple<int, MidiEvent>> tracks, MidiStruct midistruct, List<bool> isChordList, List<bool> isXChainList, List<bool> isGlobalList)
        {
            // List<ArrTuple<int, MidiEvent>> とか書きたくないですね
            
            List<ArrTuple<int, MidiEvent>> events = tracks.OrderBy(x => x.Item1).ToList();  // これはx.Item1, xItem2.tickの順にソートされている
            List<ArrTuple<int, MidiEvent>> eventsOrderByTick = tracks.OrderBy(x => x.Item2.tick).ToList();
            List<ArrTuple<int, MidiEvent>> NotNoteEventsOrderByTick =
                eventsOrderByTick
                .Where(mev => !(mev.Item2 is MidiEventNote))
                .Where(mev => (!(mev.Item2 is MidiEventMeta) || (((MidiEventMeta)mev.Item2).id == 0x51)))
                .ToList();

            // なんか間違っている気がする・・・MidiTrackの代わりにList<MidiEvent>使うべきな気がする・・・
            // というわけで使いました。ついでに IEnumerable の割合もかなり増えました。
            List<ArrTuple<int, MidiEvent>> s1meta;  // これはx.Item1, xItem2.tick の順でソートされている
            List<ArrTuple<int, MidiEvent>> s1a = new List<ArrTuple<int, MidiEvent>>();  // これはx.Item1, xItem2.tick の順の優先度でソートされている。Noteのみを含む
            List<ArrTuple<int, MidiEvent>> s1b = new List<ArrTuple<int, MidiEvent>>();  // これはx.Item1, xItem2.tick の順の優先度でソートされている。Noteを含まない。
            int deltatick = midistruct.BeatsToTicks(4 * 4);

            s1meta = (events.Where(mev => (
                (mev.Item2 is MidiEventMeta) && (
                    ((MidiEventMeta)mev.Item2).id == 0x03 ||  // Track Name
                    ((MidiEventMeta)mev.Item2).id == 0x04 ||  // Instrument Name
                    ((MidiEventMeta)mev.Item2).id == 0x05 ||  // Lyric
                    ((MidiEventMeta)mev.Item2).id == 0x21 ||  // Port
                //((MidiEventMeta)mev.Item2).id == 0x51 ||  // Tempo
                    ((MidiEventMeta)mev.Item2).id == 0x58 ||  // Signature
                    ((MidiEventMeta)mev.Item2).id == 0x59     // Key
                )
                )).ToList());  // これはx.Item1の順にソートされている

            bool messageShown = false;
            int lasttick = -1;
            var st = new Stopwatch();
            st.Start();
            for (int i = 0; i < events.Count; i++)
            {
                if (!messageShown && deltatick > 9999 * 4 * midistruct.BeatsToTicks(1))
                {
                    if (DialogResult.Yes == MessageBox.Show(
                        "text3_tanon_red.mml の小節数が9999を超過しました。処理を中断しますか？(Yes to abort)",
                         "Confirm to continue", MessageBoxButtons.YesNo))
                    {
                        throw new Exception("ユーザーの指示により処理を中断しました。");
                    }
                    messageShown = true;
                }
                int progress = i * 100 / events.Count;
                if (!messageShown && st.ElapsedMilliseconds > 5000 && progress < 50)
                {
                    if (DialogResult.Yes == MessageBox.Show(
                        "5秒経過しましたが" + progress + "% しか処理が完了していません。処理を中断しますか？(Yes to abort)",
                         "Confirm to continue", MessageBoxButtons.YesNo))
                    {
                        throw new Exception("ユーザーの指示により処理を中断しました。");
                    }
                    messageShown = true;
                }

                MidiEventNote me;
                if ((me = events[i].Item2 as MidiEventNote) != null)
                {
                    List<ArrTuple<int, MidiEvent>> chord = null;
                    if (isChordList != null && isChordList[events[i].Item1])
                    {
                        if (lasttick == me.tick) continue;
                        lasttick = me.tick;

                        // Arr.ayはクソ、はっきりわかんだね
                        // (Item1とItem2が何を指しているのかわかりづらいため)
                        chord = events.Where(x =>
                            x.Item1 == events[i].Item1
                            && x.Item2.tick == events[i].Item2.tick
                            && x.Item2 is MidiEventNote)
                            .Select(x =>{
                                MidiEvent m2 = x.Item2.Clone();
                                m2.tick = deltatick;
                                return Arr.ay(x.Item1, m2);
                            }).ToList();  // クローンしておく

                        // FindAll と Where のうまい使い分け方とかあるんだろうか
                    }

                    // Note on & Note off pair
                    me = (MidiEventNote)me.Clone();  // dektatickだけ遅延させる
                    var deltatickForEvents = deltatick - events[i].Item2.tick;

                    int LTime = me.tick - (int)(midistruct.BeatsToTicks(1) * SPLIT_BEATS_AUTOMATIONLEFT);  // 負の数になることもある
                    int RTime = me.tick + (int)(midistruct.BeatsToTicks(1) * SPLIT_BEATS_AUTOMATIONRIGHT);
                    //me.tick += deltatick;
                    me.tick = deltatick;  // updated
                    //me.tick += (deltatick - events[i].Item2.tick);  // updated 
                    // これはme.tick = deltatick; でも同じ意味になる


                    //s1.Add(Arr.ay(events[i].Item1, me));  // 型推論に失敗！！！
                    int MaxLength = me.q;
                    if (isChordList != null && isChordList[events[i].Item1])
                    {
                        foreach (var me3 in chord)
                        {
                            s1a.Add(me3);
                            MaxLength = Math.Max(MaxLength, (me3.Item2 as MidiEventNote).q);
                        }
                    }
                    else
                    {
                        s1a.Add(Arr.ay(events[i].Item1, (MidiEvent)me));  // 型推論に失敗！！！
                    }
                    if (MaxLength == 0) throw new Exception("そもそもゲートが0っておかしいよね");  // デバッグ用

                    // オートメーションの切り出し
                    // この時点において、MidiTrackはソート済みではないがeventsOrderByTickはソート済み
                    var s2 = Clip(NotNoteEventsOrderByTick, LTime, RTime);  // filter ノートの削除
                    if (s2.Any(x => x.Item2.tick < LTime)) throw new Exception("みゃ！？・・・///");
                    if (s2.Any(x => x.Item2.tick >= RTime)) throw new Exception("みゃ！！・・・///");
                    //foreach (MidiEvent mev in s2) mev.tick += deltatick;
                    foreach (ArrTuple<int, MidiEvent> mev in s2) mev.Item2.tick += deltatickForEvents;  // updated
                    //if (s2.Any(x => x.Item2.tick < 0)) throw new Exception("みゃ！？・・・///");
                    s1b.AddRange(s2);

                    deltatick += MaxLength + (int)(midistruct.BeatsToTicks(1) * SPLIT_BEATS_INTERVAL);
                }
            }
            st.Stop();

            //s1b = s1b;  // テンポチェンジを除くメタイベントを除去

            //s1meta.AddRange(s1);
            //s1meta = s1meta.OrderBy(x => x.Item2).ToList();  // ここで非常に時間が掛かっている(32.9%)
            // 今回の高速化で学んだこと：ソート(OrderBy関数)は遅い。不必要にソートしてはならない。
            // x.tickの順に安定ソートする
            //s1aとs1bとs1metaはそれぞれはソートされている
            // 最も要素数が少ないと思われる s1a と s1meta を先にマージする（重要）
            // s1meta → s1a → s1b の順にマージする(この順番はあまり重要ではない)

            s1a = s1a.OrderBy(x => x.Item1).OrderBy(x => x.Item2.tick).ToList();
            s1meta = s1meta.OrderBy(x => x.Item1).OrderBy(x => x.Item2.tick).ToList();

            //if (!s1a.SequenceEqual(s1a.OrderBy(x => x.Item1).OrderBy(x => x.Item2.tick))) throw new Exception("でやー");
            //if (!s1b.SequenceEqual(s1b.OrderBy(x => x.Item1).OrderBy(x => x.Item2.tick))) throw new Exception("うらー");  // 例外「うらー」が発生。理由は「tickよってソートされていたから」
            //if (!s1meta.SequenceEqual(s1meta.OrderBy(x => x.Item1).OrderBy(x => x.Item2.tick))) throw new Exception("とあー");

            List<ArrTuple<int, MidiEvent>> merged2 = new List<ArrTuple<int, MidiEvent>>();
            {
                MidiEvent dmy = new MidiEventNote();
                dmy.tick = Int32.MaxValue;
                s1meta.Add(Arr.ay(Int32.MaxValue, dmy));
                s1a.Add(Arr.ay(Int32.MaxValue, dmy));

                List<ArrTuple<int, MidiEvent>> merged1 = new List<ArrTuple<int, MidiEvent>>();
                {
                    int i1 = 0, i2 = 0;
                    while (i1 < s1meta.Count - 1 || i2 < s1a.Count - 1)
                    {
                        // ノートオフの方が先なので、同じ場合は ev2 が先にくる。
                        if (s1meta[i1].Item2.tick < s1a[i2].Item2.tick || (s1meta[i1].Item2.tick < s1a[i2].Item2.tick) && s1meta[i1].Item1 < s1a[i2].Item1)
                        {
                            merged1.Add(s1meta[i1++]);
                        }
                        else
                        {
                            merged1.Add(s1a[i2++]);
                        }
                    }
                }
                //if (!merged1.SequenceEqual(merged1.OrderBy(x => x.Item1).OrderBy(x => x.Item2.tick))) throw new Exception("にゃー");

                merged1.Add(Arr.ay(Int32.MaxValue, dmy));
                s1b.Add(Arr.ay(Int32.MaxValue, dmy));
                {
                    int i1 = 0, i2 = 0;
                    while (i1 < merged1.Count - 1 || i2 < s1b.Count - 1)
                    {
                        // ノートオフの方が先なので、同じ場合は ev2 が先にくる。
                        if (merged1[i1].Item2.tick < s1b[i2].Item2.tick || (merged1[i1].Item2.tick == s1b[i2].Item2.tick && merged1[i1].Item1 < s1b[i2].Item1))
                        {
                            merged2.Add(merged1[i1++]);
                        }
                        else
                        {
                            merged2.Add(s1b[i2++]);
                        }
                    }
                }
            }

            //if (!merged2.SequenceEqual(merged2.OrderBy(x => x.Item1).OrderBy(x => x.Item2.tick))) throw new Exception("みゃー");


            /*
            for (int i = 0; i < Math.Min(1, s1meta.Count); i++)  // ソートしてからチェックする
            {
                if (s1meta[i].Item2.tick < 0) throw new Exception("タイムシフトしたらマイナスになった（オーバーフローした）");
            }
            */

            return merged2;
        }

        public void AddTempo(double BPM, MidiStruct midistruct)
        {
            MidiEventMeta tempometa = new MidiEventMeta();
            tempometa.ch = 0;
            tempometa.tick = 0;
            tempometa.id = 0x51;  // tempo
            tempometa.val = (int)(60.0 * 1000000 / BPM);
            if (tempometa.val > 0x1000000) tempometa.val = 0xFFFFFF;
            tempometa.bytes = new byte[3];
            tempometa.bytes[0] = (byte)((tempometa.val >> 16) & 0xFF);
            tempometa.bytes[1] = (byte)((tempometa.val >> 8) & 0xFF);
            tempometa.bytes[2] = (byte)((tempometa.val >> 0) & 0xFF);
            this.Add(tempometa);
        }
        public void AddEndOfTrack(MidiStruct midistruct)
        {
            MidiEventMeta endOfTrack = new MidiEventMeta();
            endOfTrack.ch = 0;
            endOfTrack.tick = ((this.Count >= 1) ? this[this.Count - 1].tick : 0) + midistruct.BeatsToTicks(4);
            endOfTrack.id = 0x2F;  // end of track
            endOfTrack.bytes = new byte[0];
            endOfTrack.val = 0;
            this.Add(endOfTrack);
        }

        /// <summary>
        /// データを切り出し、MidiTrackの複製を返します。
        /// ただし、オートメーションは連続値とみなして、左端の設定値を補完します。
        /// 
        /// MidiTrackはtickによってソート済でなければなりません
        /// </summary>
        /// <param name="LeftTick"></param>
        /// <param name="RightTick"></param>
        /// <returns></returns>
        public static IEnumerable<ArrTuple<int, MidiEvent>> Clip(IEnumerable<ArrTuple<int, MidiEvent>> tracks, int LeftTick, int RightTick)
        {
            //var events = sorted ? tracks.ToList() : tracks.OrderBy(x => x.Item2.tick).ToList();  // 時間順にソート
            var events = tracks.ToList();

            var s1 = new List<ArrTuple<int, MidiEvent>>();
            var sLeft = new List<ArrTuple<int, MidiEvent>>();  // tick が LeftTick と同じかそれより少ないもの

            //int beginIndex;
            int endIndex;
            {
                //var LeftTickTuple = new ArrTuple<int, MidiEvent>(0, new MidiEventNote());
                //LeftTickTuple.Item2.tick = LeftTick - 1;
                //beginIndex = events.BinarySearch(LeftTickTuple, x => x.Item2.tick);
                //if (beginIndex < 0) beginIndex = ~beginIndex;
                // 見つかった場合は       0 以上 events.Count - 1 以下
                // 見つからなかった場合は 0 以上 events.Count 以下

                var RightTickTuple = new ArrTuple<int, MidiEvent>(0, new MidiEventNote());
                RightTickTuple.Item2.tick = RightTick + 1;
                endIndex = events.BinarySearch(RightTickTuple, x => x.Item2.tick);
                if (endIndex < 0) endIndex = ~endIndex;
                // 見つかった場合は       0 以上 events.Count - 1 以下
                // 見つからなかった場合は 0 以上 events.Count 以下
            }

            // アルゴリズム上の都合により、me を逆順に(インデックスの大きい方から)探索します。

            //for (int i = events.Count - 1; i >= 0; i--)
            //for (int i = endIndex - 1; i >= beginIndex; i--)
            int i = endIndex - 1;
            for (; i >= 0; i--)  // while(me.tick >= RightTick)
            {
                MidiEvent me = events[i].Item2;
                if (me.tick < RightTick) break;

                // do nothing
            }
            for (; i >= 0; i--)  // while(LeftTick < me.tick && me.tick < RightTick)
            {
                MidiEvent me = events[i].Item2;
                if (me.tick <= LeftTick) break;

                s1.Add(Arr.ay(events[i].Item1, me.Clone()));  // ここが重い (25.8%)
                //s1.Add(events[i]);  // これでいいのか!?・・・ダメだった
                // 範囲より左(me.tick < LeftTick)に時間が掛かっているのかと思ったら、範囲内に時間がかかっているらしい
            }
            for (; i >= 0; i--)  // while(me.tick == LeftTick)
            {
                MidiEvent me = events[i].Item2;
                if (me.tick < LeftTick) break;

                sLeft.Add(Arr.ay(events[i].Item1, me.Clone()));
            }
            for (; i >= 0; i--)  // while(me.tick < LeftTick)
            {
                MidiEvent me = events[i].Item2;

                if (!(me is MidiEventNote)) // me.tick < LeftTick && !(me is MidiEventNote)
                {
                    // meがオートメーションデータ
                    // sLeftにmeとレーンが同じデータが含まれているかどうか調べる。
                    // 既に含まれていた場合は追加しない。
                    bool found = false;
                    if (me is MidiEventCC)
                    {
                        foreach (ArrTuple<int, MidiEvent> _me3 in sLeft)  // あれ？Listはforeachよりforの方が速いんだっけ
                        {
                            if (!(_me3.Item2 is MidiEventCC)) continue;
                            if (_me3.Item1 != events[i].Item1) continue;
                            var me3 = (MidiEventCC)_me3.Item2;

                            if (me3.cc == ((MidiEventCC)me).cc)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    else if (me is MidiEventKeyPressure)
                    {
                        foreach (ArrTuple<int, MidiEvent> _me3 in sLeft)
                        {
                            if (!(_me3.Item2 is MidiEventKeyPressure)) continue;
                            if (_me3.Item1 != events[i].Item1) continue;
                            var me3 = (MidiEventKeyPressure)_me3.Item2;

                            if (me3.n == ((MidiEventKeyPressure)me).n)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    else if (me is MidiEventChannelPressure)
                    {
                        foreach (ArrTuple<int, MidiEvent> _me3 in sLeft)
                        {
                            if (_me3.Item1 != events[i].Item1) continue;
                            if (_me3.Item2 is MidiEventChannelPressure)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    else if (me is MidiEventPB)
                    {
                        foreach (ArrTuple<int, MidiEvent> _me3 in sLeft)
                        {
                            if (_me3.Item1 != events[i].Item1) continue;
                            if (_me3.Item2 is MidiEventPB)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    else if (me is MidiEventProgram)
                    {
                        foreach (ArrTuple<int, MidiEvent> _me3 in sLeft)
                        {
                            if (_me3.Item1 != events[i].Item1) continue;
                            if (_me3.Item2 is MidiEventProgram)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    else if (me is MidiEventSysEx)
                    {
                        foreach (ArrTuple<int, MidiEvent> _me3 in sLeft)
                        {
                            if (_me3.Item1 != events[i].Item1) continue;
                            if (_me3.Item2 is MidiEventSysEx)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    else if (me is MidiEventMeta)  // 主にテンポチェンジ(id==0x51)
                    {
                        /*foreach (ArrTuple<int, MidiEvent> _me3 in sLeft)
                        {
                            if (!(_me3.Item2 is MidiEventMeta)) continue;
                            if (_me3.Item1 != events[i].Item1) continue;  // Item1は直和した際のトラック番号
                            var me3 = (MidiEventMeta)_me3.Item2;

                            if (me3.id == ((MidiEventMeta)me).id)
                            {
                                found = true;
                                break;
                            }
                        }*/

                        found = sLeft
                            .Where(me3 => me3.Item2 is MidiEventMeta)    // MidiEventMeta型で、
                            .Where(me3 => me3.Item1 != events[i].Item1)  // Midiトラック番号が同じものの中に、
                            .Select(me3 => (MidiEventMeta)me3.Item2)
                            .Any(me3 => me3.id == ((MidiEventMeta)me).id); // idが同じものがあるかどうか
                    }
                    else
                    {
                        found = true;  // メタイベントなど？？それともバグ？
                    }

                    if (!found)  // もし見つからなかった場合は
                    {
                        MidiEvent me2 = me.Clone();
                        me2.tick = LeftTick;
                        sLeft.Add(Arr.ay(events[i].Item1, me2));
                    }
                }
            }

            s1.AddRange(sLeft);
            s1.Reverse();
            return s1;
        }

        /// <summary>
        /// ペダル(Midi CC64)を適用します。このMidiTrackインスタンス自身が変更されます。
        /// </summary>
        public void ApplyHoldPedal()
        {
            MidiEventNote[] lastNote = new MidiEventNote[128];  // デフォルト値はnullです
            bool isHoldPedalOn = false;  // hold pedal offが来たらfalse
            int pedalOnTime = 0;
            // 15360 (/1拍) って、180BPMで約776小節か。999小節行かないという・・・

            // ノートオンとノートオフを別個に処理したほうが簡単だったと思うんですが・・・

            for (int i = 0; i < this.Count; i++)
            {
                if (this[i] is MidiEventNote)
                {
                    // note event
                    MidiEventNote me = (MidiEventNote)this[i];

                    // (1) 以前のノートの終端を設定
                    if (isHoldPedalOn)
                    {
                        MidiEventNote me2 = lastNote[me.n & 127];
                        if (me2 != null)
                        {
                            if (pedalOnTime <= me2.tick + me2.q)
                            {
                                //me2.q = me.tick - me2.tick;  // + offset
                                me2.q = me.tick - me2.tick - 1;  // 1 is offset
                            }
                        }
                    }

                    // (2) lastNoteを更新
                    lastNote[me.n & 127] = me;  // C++じゃないから、もう&127なんて書かなくていいんだよ・・・？
                }
                else if (this[i] is MidiEventCC)
                {
                    MidiEventCC me = (MidiEventCC)this[i];

                    if (me.cc == 64)  // Hold Pedal
                    {
                        if (me.val >= 32)  // 適当なしきい値
                        {
                            // Hold Pedal ON
                            pedalOnTime = me.tick;
                            isHoldPedalOn = true;
                        }
                        else
                        {
                            // Hold Pedal OFF
                            isHoldPedalOn = false;

                            for (int j = 0; j < 128; j++)
                            {
                                if (lastNote[j] != null
                                    && pedalOnTime < lastNote[j].tick + lastNote[j].q
                                    && lastNote[j].tick + lastNote[j].q < me.tick)
                                {
                                    // ペダルが離される時間までノートの<s>デュレーション</s>ゲートを伸ばす
                                    lastNote[j].q = me.tick - lastNote[j].tick;
                                    //lastNote[j] = null; // あっても無くてもどちらでもOK
                                }
                            }
                        }

                        // ペダルを無効にする
                        me.val = 0;  // Cloneしない
                    }

                }
            }

            // ↓アルゴリズムの根幹に重大なバグがあった
            /*
            Nullable<int>[] noteOffTime = new int?[128];  // デフォルト値はnullです
            int? pedalOffTime = null;
            bool isHoldPedalOn = false;  // hold pedal offが来たらtrue (後ろから読んでいるため)
            // 15360 (/1拍) って、180BPMで約776小節か。999小節行かないという・・・
            
            for (int i = this.Count - 1; i >= 0; i--)
            {
                if (this[i] is MidiEventNote)
                {
                    // note event
                    MidiEventNote me = (MidiEventNote)this[i];

                    // (1) このノートの終端を設定
                    if (isHoldPedalOn)
                    {
                        me = (MidiEventNote)me.Clone();

                        // http://manbow.nothing.sh/event/event.cgi?action=More_def&num=81&event=36
                        int endtime = Math.Min(
                            pedalOffTime ?? int.MaxValue,
                            noteOffTime[me.n & 127] ?? int.MaxValue);  // C++じゃないから、もう&127なんて書かなくていいんだよ・・・？
                        // ↑nullableにした意味全くねぇ！！

                        // me.qを設定
                        me.q = endtime - me.tick;   // + offset

                        this[i] = me;  // Cloneする必要はあったのかどうか
                    }

                    // (2) noteOffTimeを更新
                    noteOffTime[me.n & 127] = me.tick;  // C++じゃないから、もう&127なんて書かなくていいんだよ・・・？
                }
                else if (this[i] is MidiEventCC)
                {
                    MidiEventCC me = (MidiEventCC)this[i];

                    if (me.cc == 64)  // Hold Pedal
                    {
                        if (me.val >= 32)  // 適当なしきい値
                        {
                            // Hold Pedal ON
                            isHoldPedalOn = false;
                        }
                        else
                        {
                            // Hold Pedal OFF
                            pedalOffTime = me.tick;
                            isHoldPedalOn = true;
                        }

                        me = (MidiEventCC)me.Clone();
                        me.val = 0;
                        this[i] = me;

                    }
                    
                }
            }
            */
        }

        public override String ToString()
        {
            StringBuilder s0 = new StringBuilder();
            for (int i = 0; i < this.Count; i++)
            {
                s0.Append(this[i].ToString());
            }
            return s0.ToString();
        }

    }

}
