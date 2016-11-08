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
        /// n個の配列の直和を求めます。
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static IEnumerable<MultiTrackMidiEvent> DirectSum(IEnumerable<IEnumerable<MidiEvent>> x)
        {
            int i = 0;
            foreach (var x1 in x)
            {
                foreach (var element in x1)
                {
                    yield return new MultiTrackMidiEvent(i, element);
                }
                i++;
            }
        }

        /// <summary>
        /// 直和を配列に復元します。
        /// 遅延評価ではありません。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        /// <returns></returns>
        public static List<MidiTrack> DirectDifference(IEnumerable<MultiTrackMidiEvent> x)
        {
            List<List<MidiEvent>> y = new List<List<MidiEvent>>();
            foreach (var tpl in x)
            {
                while (y.Count <= tpl.TrackID)
                {
                    y.Add(new List<MidiEvent>());
                }

                y[tpl.TrackID].Add(tpl.Event);
            }

            return y.Select(z => new MidiTrack(z)).ToList();
        }

        /// <summary>
        /// 複製を返す。計算時間 O(n^2) 掛かる気がする。良くないのでは？
        /// 高速化するのは「計測」をしてから、って言うじゃないですか
        /// (もし)EndOfTrackが存在した場合は除去します。
        /// </summary>
        public MidiTrack SplitNotes(MidiStruct midistruct, bool isChordMode)
        {
            return new MidiTrack(
                MidiTrack.SplitNotes(MidiTrack.DirectSum(new IEnumerable<MidiEvent>[] {
                    this.OrderBy(x => x.tick)
                }), midistruct, new List<bool> { isChordMode }, new List<bool> { false }).Select(x => x.Event)
                );
        }

        /// <summary>
        /// 複製を返す。計算時間 O(n^2) 掛かる気がする。良くないのでは？
        /// 高速化するのは「計測」をしてから、って言うじゃないですか
        /// (もし)EndOfTrackが存在した場合は除去します。
        /// 
        /// ソートされた順に、ノートが配置されます。ですから、 x => x.Event.tick では絶対にソートしないでください。
        /// 
        /// プログラムを「一般化」した結果がこの三角カッコの嵐だよ！！！
        /// </summary>
        public static IEnumerable<MultiTrackMidiEvent> SplitNotes(
            IEnumerable<MultiTrackMidiEvent> tracks, MidiStruct midistruct, List<bool> isChordList, List<bool> isXChainList)
        {
            // List<ArrTuple<int, MidiEvent>> とか書きたくないですね

            //######## TrackIDの順にソートされた入力
            List<MultiTrackMidiEvent> eventsOrderByTrackID = tracks.OrderBy(x => x.TrackID).ToList();  // これはx.Item1, xItem2.tickの順にソートされている

            //######## TrackIDの順にソートされたノーツ入力（サイドチェイントリガノーツを含まない）
            List<MultiTrackMidiEvent> noteEventsOrderByTrackID = 
                eventsOrderByTrackID
                .Where(x => (x.Event is MidiEventNote) && (isXChainList == null || !isXChainList[x.TrackID]))
                .ToList();  // これはx.Item1, xItem2.tickの順にソートされている

            //######## tickの順にソートされた非ノーツ入力（テンポチェンジ以外のメタデータを含まない。また、サイドチェイントリガノーツを含む）
            List<MultiTrackMidiEvent> NonNoteEventsOrderByTick =
                tracks
                .OrderBy(x => x.Event.tick)
                .Where(x => !(x.Event is MidiEventNote) || (isXChainList != null && isXChainList[x.TrackID]))
                .Where(x => !(x.Event is MidiEventMeta) || (((MidiEventMeta)x.Event).id == 0x51))
                .ToList();

            //######## メタイベントのうちトラック名等のオートメーションとして解釈されないもの（テンポチェンジは含まれない）
            List<MultiTrackMidiEvent> metaEventsOrderByTrackID = (eventsOrderByTrackID.Where(mev => (
                (mev.Event is MidiEventMeta) && (
                    ((MidiEventMeta)mev.Event).id == 0x03 ||  // Track Name
                    ((MidiEventMeta)mev.Event).id == 0x04 ||  // Instrument Name
                    ((MidiEventMeta)mev.Event).id == 0x05 ||  // Lyric
                    ((MidiEventMeta)mev.Event).id == 0x21 ||  // Port
                    //((MidiEventMeta)mev.Event).id == 0x51 ||  // Tempo
                    ((MidiEventMeta)mev.Event).id == 0x58 ||  // Signature
                    ((MidiEventMeta)mev.Event).id == 0x59     // Key
                )
                )).ToList());

            //----------------------------------------------------------------------------------------------//

            //######## 最終的な出力結果のうち、ノートイベントであるもの
            List<MultiTrackMidiEvent> outputEventsNote = new List<MultiTrackMidiEvent>();  // OrderByTrackID

            //######## 最終的な出力結果のうち、ノートイベント以外のもの（サイドチェイントリガノーツを含む）
            List<MultiTrackMidiEvent> outputEventsNonNote = new List<MultiTrackMidiEvent>();  // OrderByTrackID
            int deltatick = midistruct.BeatsToTicks(4 * 4);
            
            //----------------------------------------------------------------------------------------------//

            bool messageShown = false;
            int lasttick = -1;
            var st = new Stopwatch();
            st.Start();
            for (int i = 0; i < eventsOrderByTrackID.Count; i++)
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
                int progress = i * 100 / eventsOrderByTrackID.Count;
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
                if ((me = eventsOrderByTrackID[i].Event as MidiEventNote) != null)  // ノーツイベントだった場合
                {
                    //################ outputEventsNoteへの要素追加 ################

                    // サイドチェイントリガノーツだったのでスキップ
                    if (isXChainList != null && isXChainList[eventsOrderByTrackID[i].TrackID]) continue;

                    List<MultiTrackMidiEvent> chord = null;
                    if (isChordList != null && isChordList[eventsOrderByTrackID[i].TrackID])
                    {
                        // chordモード
                        if (lasttick == me.tick) continue;
                        lasttick = me.tick;
                        
                        chord = noteEventsOrderByTrackID.Where(x =>
                            x.TrackID == eventsOrderByTrackID[i].TrackID
                            && x.Event.tick == eventsOrderByTrackID[i].Event.tick)
                            .Select(x =>{
                                MidiEvent m2 = x.Event.Clone();
                                m2.tick = deltatick;
                                return new MultiTrackMidiEvent(x.TrackID, m2);
                            }).ToList();  // クローンしておく
                    }

                    // Note on & Note off pair
                    me = (MidiEventNote)me.Clone();  // dektatickだけ遅延させる
                    var deltatickForEvents = deltatick - eventsOrderByTrackID[i].Event.tick;

                    // 後で必要になる、オートメーションの切り出し範囲
                    int LTime = me.tick - (int)(midistruct.BeatsToTicks(1) * SPLIT_BEATS_AUTOMATIONLEFT);
                    int RTime = me.tick + (int)(midistruct.BeatsToTicks(1) * SPLIT_BEATS_AUTOMATIONRIGHT);
                    me.tick = deltatick;  // updated
                    
                    int MaxLength = -1;
                    if (isChordList != null && isChordList[eventsOrderByTrackID[i].TrackID])
                    {
                        // chordモード
                        outputEventsNote.AddRange(chord);
                        MaxLength = chord.Select(me3 => (me3.Event as MidiEventNote).q).Max();  // 最も長いノーツの長さ
                    }
                    else
                    {
                        // 非chordモード
                        outputEventsNote.Add(new MultiTrackMidiEvent(eventsOrderByTrackID[i].TrackID, me));
                        MaxLength = me.q;
                    }
                    if (MaxLength <= 0) throw new Exception("そもそもゲートが0以下っておかしいよね");  // デバッグ用

                    //################ outputEventsNonNoteへの要素追加 ################

                    // オートメーションの切り出し
                    // この時点において、MidiTrackはソート済みではないがeventsOrderByTickはソート済み
                    var s2 = Clip(NonNoteEventsOrderByTick, LTime, RTime);  // filter ノートの削除
                    if (s2.Any(x => x.Event.tick < LTime)) throw new Exception("みゃ！？・・・///");
                    if (s2.Any(x => x.Event.tick >= RTime)) throw new Exception("みゃ！！・・・///");
                    foreach (MultiTrackMidiEvent mev in s2) mev.Event.tick += deltatickForEvents;  // updated
                    outputEventsNonNote.AddRange(s2);

                    deltatick += MaxLength + (int)(midistruct.BeatsToTicks(1) * SPLIT_BEATS_INTERVAL);
                }
            }
            st.Stop();
            
            // 今回の高速化で学んだこと：ソート(OrderBy関数)は遅い。不必要にソートしてはならない。
            // x.tickの順に安定ソートする
            // outputEventsNote と outputEventsNonNote と metaEventsOrderByTrackID はそれぞれはソートされている
            // 最も要素数が少ないと思われる outputEventsNote と metaEventsOrderByTrackID を先にマージする（重要）
            // metaEventsOrderByTrackID → outputEventsNote → outputEventsNonNote の順にマージする(この順番はあまり重要ではない)

            outputEventsNote = outputEventsNote.OrderBy(x => x.TrackID).OrderBy(x => x.Event.tick).ToList();
            metaEventsOrderByTrackID = metaEventsOrderByTrackID.OrderBy(x => x.TrackID).OrderBy(x => x.Event.tick).ToList();
            // outputEventsNonNote はソートされていることが保証されている・・・？

            //#### マージされた出力配列
            List<MultiTrackMidiEvent> mergedOutputEvents2 = new List<MultiTrackMidiEvent>();

            #region 出力配列のマージ
            {
                MidiEvent dmy = new MidiEventNote();
                dmy.tick = Int32.MaxValue;
                metaEventsOrderByTrackID.Add(new MultiTrackMidiEvent(Int32.MaxValue, dmy));
                outputEventsNote.Add(new MultiTrackMidiEvent(Int32.MaxValue, dmy));

                List<MultiTrackMidiEvent> mergedOutputEvents1 = new List<MultiTrackMidiEvent>();
                {
                    int i1 = 0, i2 = 0;
                    while (i1 < metaEventsOrderByTrackID.Count - 1 || i2 < outputEventsNote.Count - 1)
                    {
                        // ノートオフの方が先なので、同じ場合は ev2 が先にくる。
                        if (metaEventsOrderByTrackID[i1].Event.tick < outputEventsNote[i2].Event.tick || (metaEventsOrderByTrackID[i1].Event.tick < outputEventsNote[i2].Event.tick) && metaEventsOrderByTrackID[i1].TrackID < outputEventsNote[i2].TrackID)
                        {
                            mergedOutputEvents1.Add(metaEventsOrderByTrackID[i1++]);
                        }
                        else
                        {
                            mergedOutputEvents1.Add(outputEventsNote[i2++]);
                        }
                    }
                }

                mergedOutputEvents1.Add(new MultiTrackMidiEvent(Int32.MaxValue, dmy));
                outputEventsNonNote.Add(new MultiTrackMidiEvent(Int32.MaxValue, dmy));
                {
                    int i1 = 0, i2 = 0;
                    while (i1 < mergedOutputEvents1.Count - 1 || i2 < outputEventsNonNote.Count - 1)
                    {
                        // ノートオフの方が先なので、同じ場合は ev2 が先にくる。
                        if (mergedOutputEvents1[i1].Event.tick < outputEventsNonNote[i2].Event.tick || (mergedOutputEvents1[i1].Event.tick == outputEventsNonNote[i2].Event.tick && mergedOutputEvents1[i1].TrackID < outputEventsNonNote[i2].TrackID))
                        {
                            mergedOutputEvents2.Add(mergedOutputEvents1[i1++]);
                        }
                        else
                        {
                            mergedOutputEvents2.Add(outputEventsNonNote[i2++]);
                        }
                    }
                }
            }
            #endregion

            return mergedOutputEvents2;
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
        public static IEnumerable<MultiTrackMidiEvent> Clip(IEnumerable<MultiTrackMidiEvent> tracks, int LeftTick, int RightTick)
        {
            //var events = sorted ? tracks.ToList() : tracks.OrderBy(x => x.Event.tick).ToList();  // 時間順にソート
            var events = tracks.ToList();

            var sMiddle = new List<MultiTrackMidiEvent>();  // tick が LeftTick より大きいもの
            var sLeft = new List<MultiTrackMidiEvent>();  // tick が LeftTick と同じかそれより少ないもの

            //int beginIndex;
            int endIndex;
            {
                //var LeftTickTuple = new MultiTrackMidiEvent(0, new MidiEventNote());
                //LeftTickTuple.Event.tick = LeftTick - 1;
                //beginIndex = events.BinarySearch(LeftTickTuple, x => x.Event.tick);
                //if (beginIndex < 0) beginIndex = ~beginIndex;
                // 見つかった場合は       0 以上 events.Count - 1 以下
                // 見つからなかった場合は 0 以上 events.Count 以下

                var RightTickTuple = new MultiTrackMidiEvent(0, new MidiEventNote());
                RightTickTuple.Event.tick = RightTick + 1;
                endIndex = events.BinarySearch(RightTickTuple, x => x.Event.tick);
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
                MidiEvent me = events[i].Event;
                if (me.tick < RightTick) break;

                // do nothing
            }
            for (; i >= 0; i--)  // while(LeftTick < me.tick && me.tick < RightTick)
            {
                MidiEvent me = events[i].Event;
                if (me.tick <= LeftTick) break;

                MidiEvent me2 = me.Clone();
                if (me2 is MidiEventNote)
                {
                    MidiEventNote me3 = me2 as MidiEventNote;
                    me3.q = Math.Min(me3.q, RightTick - me3.tick);  // me2の値に変更を加える
                }

                sMiddle.Add(new MultiTrackMidiEvent(events[i].TrackID, me2));  // ここが重い (25.8%)
                // Clone()しなくてもいいのか!?・・・ダメだった
                // 範囲より左(me.tick < LeftTick)に時間が掛かっているのかと思ったら、範囲内に時間がかかっているらしい
            }
            for (; i >= 0; i--)  // while(me.tick == LeftTick)
            {
                MidiEvent me = events[i].Event;
                if (me.tick < LeftTick) break;

                MidiEvent me2 = me.Clone();
                if (me2 is MidiEventNote)
                {
                    MidiEventNote me3 = me2 as MidiEventNote;
                    me3.q = Math.Min(me3.q, RightTick - me3.tick);  // me2の値に変更を加える
                }

                sLeft.Add(new MultiTrackMidiEvent(events[i].TrackID, me2));
            }
            for (; i >= 0; i--)  // while(me.tick < LeftTick)
            {
                MidiEvent me = events[i].Event;

                if (!(me is MidiEventNote)) // me.tick < LeftTick && !(me is MidiEventNote)
                {
                    // meがオートメーションデータ
                    // sLeftにmeとレーンが同じデータが含まれているかどうか調べる。
                    // 既に含まれていた場合は追加しない。
                    bool found = false;
                    if (me is MidiEventCC)
                    {
                        foreach (MultiTrackMidiEvent _me3 in sLeft)  // あれ？Listはforeachよりforの方が速いんだっけ
                        {
                            if (!(_me3.Event is MidiEventCC)) continue;
                            if (_me3.TrackID != events[i].TrackID) continue;
                            var me3 = (MidiEventCC)_me3.Event;

                            if (me3.cc == ((MidiEventCC)me).cc)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    else if (me is MidiEventKeyPressure)
                    {
                        foreach (MultiTrackMidiEvent _me3 in sLeft)
                        {
                            if (!(_me3.Event is MidiEventKeyPressure)) continue;
                            if (_me3.TrackID != events[i].TrackID) continue;
                            var me3 = (MidiEventKeyPressure)_me3.Event;

                            if (me3.n == ((MidiEventKeyPressure)me).n)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    else if (me is MidiEventChannelPressure)
                    {
                        foreach (MultiTrackMidiEvent _me3 in sLeft)
                        {
                            if (_me3.TrackID != events[i].TrackID) continue;
                            if (_me3.Event is MidiEventChannelPressure)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    else if (me is MidiEventPB)
                    {
                        foreach (MultiTrackMidiEvent _me3 in sLeft)
                        {
                            if (_me3.TrackID != events[i].TrackID) continue;
                            if (_me3.Event is MidiEventPB)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    else if (me is MidiEventProgram)
                    {
                        foreach (MultiTrackMidiEvent _me3 in sLeft)
                        {
                            if (_me3.TrackID != events[i].TrackID) continue;
                            if (_me3.Event is MidiEventProgram)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    else if (me is MidiEventSysEx)
                    {
                        foreach (MultiTrackMidiEvent _me3 in sLeft)
                        {
                            if (_me3.TrackID != events[i].TrackID) continue;
                            if (_me3.Event is MidiEventSysEx)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    else if (me is MidiEventMeta)  // 主にテンポチェンジ(id==0x51)
                    {
                        /*foreach (MultiTrackMidiEvent _me3 in sLeft)
                        {
                            if (!(_me3.Event is MidiEventMeta)) continue;
                            if (_me3.TrackID != events[i].TrackID) continue;  // Item1は直和した際のトラック番号
                            var me3 = (MidiEventMeta)_me3.Event;

                            if (me3.id == ((MidiEventMeta)me).id)
                            {
                                found = true;
                                break;
                            }
                        }*/

                        found = sLeft
                            .Where(me3 => me3.Event is MidiEventMeta)    // MidiEventMeta型で、
                            .Where(me3 => me3.TrackID != events[i].TrackID)  // Midiトラック番号が同じものの中に、
                            .Select(me3 => (MidiEventMeta)me3.Event)
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
                        sLeft.Add(new MultiTrackMidiEvent(events[i].TrackID, me2));
                    }
                }
            }

            sMiddle.AddRange(sLeft);
            sMiddle.Reverse();
            return sMiddle;
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
