using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace Mid2BMS
{
    class MidiStruct
    {
        // ---------------constants---------------
        //const int MAX_TRACKS = 128;
        //const String MIDI_TEXT_ENCODING = "Shift_JIS";
        const int MAX_MIDIEVENTS = 16777216;  // 65536(16), 1048576(20), 16777216(24)
        
        const uint MThd = 0x4D546864u;
        const uint MTrk = 0x4D54726Bu;
        const uint MThd_L = 0x6468544Du;
        const uint MTrk_L = 0x6B72544Du;

        const bool GROUP_NOTE_ON_OFF = true;  // デバッグ用だった（過去形）
        // falseはほとんどのメソッドでサポートされていませんが、
        // MidiStructコンストラクタ及びToStringメソッドのみ対応しています（多分）

        // ---------------fields---------------
        // Midi Informations
        int? formatnumber = null;  // 1以外は未対応
        //public int? trackcount = null; // tracks.Length
        public int? resolution = null;  // unittime

        public int? InitalUSecondPerBeat { get; private set; }

        public List<MidiTrack> tracks;

        public MidiStruct(int resolution_)
        {
            resolution = resolution_;
            formatnumber = 1;
            tracks = new List<MidiTrack>();
        }

        /// <summary>
        /// ストリームからmidiファイルを読み込み、MidiStructを生成します。
        /// </summary>
        /// <param name="str_"></param>
        public MidiStruct(Stream str_) : this(str_, false)
        {
            //InitalUSecondPerBeat = null;  // MidiStruct(Stream str_, bool removeEndOfTrack)を実行した後にこれが実行されてしまう
        }

        /// <summary>
        /// ストリームからmidiファイルを読み込み、MidiStructを生成します。
        /// removeEndOfTrackがtrueの場合、endOfTrackを除去します。これはEndOfTrackがMTrkの最後に来ないバグを回避するために役立ちます。
        /// </summary>
        /// <param name="str_"></param>
        public MidiStruct(Stream str_, bool removeEndOfTrack)
        {
            InitalUSecondPerBeat = null;

            //try
            using (var fp = new ImprovedBinaryReader(str_))
            {
                tracks = new List<MidiTrack>();

                // -- header --
                if (fp.ReadBigInt32() != MThd) throw new Exception("midi file header is not 'MThd'.");
                if (fp.ReadBigInt32() != 6) throw new Exception("midi file read error.");
                formatnumber = fp.ReadInt16();  // midi format
                formatnumber = ((formatnumber & 0x00FF) << 8) | ((formatnumber & 0xFF00) >> 8);
                if (formatnumber != 1)
                {
                    MessageBox.Show("midi formatが1じゃないとか正直どうなの、まあ変換するけど\nExSMF 2.00(MISO氏作)とか使うと良いと思うよ（適当");
                }
                int trackcount_temp = fp.ReadByte();  // the number of tracks
                trackcount_temp = (trackcount_temp << 8) + fp.ReadByte();
                resolution = fp.ReadInt16();  // unit time
                resolution = ((resolution & 0x00FF) << 8) | ((resolution & 0xFF00) >> 8);

                // -- tracks --
                for (int j = 0; j < trackcount_temp; j++)
                {
                    try
                    {
                        if (fp.ReadBigInt32() != MTrk) throw new Exception("midiファイルが不正すぎてきれそう");
                        // 0x4D54726B == "MTrk"
                    }
                    catch (Exception)
                    {
                        break;  // あとで修正したい
                    }

                    //List<MidiEvent> ev = new List<MidiEvent>();
                    var ev = new MidiTrack();

                    int trBytes = fp.ReadBigInt32();  // 捨てる
                    int trNumber = j;

                    int t = 0; // floatだとちょっと精度が足りないかも。
                    bool isEnd = false;

                    int lastEv = 0;  // ランニングステータス用の、直前のステータスバイト
                    bool omniModeOn = false;

                    int k = 0;
                    for (k = 0; k < MAX_MIDIEVENTS; k++)
                    {

                        t += fp.ReadDeltaTime();

                        int statusbyte = fp.ReadByte();
                        int noteChannel = (statusbyte & 0x0F);

                        #region Midiイベント
                        if (statusbyte < 0xF0)
                        {
                            int noteVal2;

                            // ●▼●Midi イベント
                            if (statusbyte < 0x80)
                            {
                                noteVal2 = statusbyte;
                                statusbyte = lastEv;
                                noteChannel = (statusbyte & 0x0F);
                                if (lastEv < 0x80) throw new Exception("まだイベントが来てないのにランニングステータスが来た");
                                //SeekBinBackward(ref fpR, 1);
                                // ランニングステータスは、 MIDIイベントのみに有効
                            }
                            else
                            {
                                noteVal2 = fp.ReadByte();
                            }


                            switch (statusbyte / 16)
                            {
                                case 0x8:  // ●▼●チャンネル０ノートoff
                                    {
                                        var me = new MidiEventNote();
                                        me.ch = noteChannel;
                                        me.tick = t;
                                        me.n = noteVal2;
                                        //me.v = fp.ReadByte();
                                        fp.ReadByte();  // 読み捨て
                                        me.v = 0;
                                        me.q = 0;
                                        ev.Add(me);
                                    }
                                    lastEv = statusbyte;
                                    break;

                                case 0x9:  // ●▼●チャンネル０ノートon
                                    {
                                        var me = new MidiEventNote();
                                        me.ch = noteChannel;
                                        me.tick = t;
                                        me.n = noteVal2;
                                        //me.v = fp.ReadByte();
                                        int vv = fp.ReadByte();
                                        me.v = (vv == 0) ? 0 : vv;
                                        ev.Add(me);
                                    }
                                    lastEv = statusbyte;
                                    break;

                                case 0xA:  // ●▼●ポリフォニックキープレッシャー
                                    {
                                        var me = new MidiEventKeyPressure();
                                        me.ch = noteChannel;
                                        me.tick = t;
                                        me.n = noteVal2;
                                        me.val = fp.ReadByte();
                                        ev.Add(me);
                                    }
                                    lastEv = statusbyte;
                                    break;
                                case 0xB:  // ●▼●コントロールチェンジ
                                    {
                                        // この部分があってるかどうか自信ないですね
                                        var me = new MidiEventCC();
                                        me.ch = noteChannel;
                                        me.tick = t;
                                        me.cc = noteVal2;
                                        if (me.cc == 0x7C)
                                        {
                                            me.val = fp.ReadByte();  // OMNI OFF
                                            omniModeOn = false;
                                        }
                                        else if (me.cc == 0x7D)
                                        {
                                            me.val = fp.ReadByte();  // OMNI ON
                                            omniModeOn = true;
                                        }
                                        else if (me.cc == 0x7E)  // MONO ON
                                        {
                                            if (omniModeOn == false)
                                            {
                                                fp.ReadByte();
                                                me.val = fp.ReadByte();
                                            }
                                            else
                                            {
                                                me.val = fp.ReadByte();
                                            }
                                        }
                                        else
                                        {
                                            me.val = fp.ReadByte();
                                        }
                                        ev.Add(me);
                                    }
                                    lastEv = statusbyte;
                                    break;

                                case 0xE:  // ●▼●ピッチベンド
                                    {
                                        var me = new MidiEventPB();
                                        me.ch = noteChannel;
                                        me.tick = t;
                                        me.val = noteVal2;
                                        me.val = me.val + (fp.ReadByte() << 7) - 8192;
                                        ev.Add(me);
                                    }
                                    lastEv = statusbyte;
                                    break;

                                case 0xC:  // ●▼●プログラムチェンジ
                                    {
                                        var me = new MidiEventProgram();
                                        me.ch = noteChannel;
                                        me.tick = t;
                                        me.val = noteVal2;
                                        ev.Add(me);
                                    }
                                    lastEv = statusbyte;
                                    break;

                                case 0xD:  // ●▼●チャンネルプレッシャー
                                    {
                                        var me = new MidiEventChannelPressure();
                                        me.ch = noteChannel;
                                        me.tick = t;
                                        me.val = noteVal2;
                                        ev.Add(me);
                                    }
                                    lastEv = statusbyte;
                                    break;

                                default:
                                    throw new Exception("謎のバグが起きた");
                            }
                        }
                        #endregion

                        #region SysExイベント
                        else if (statusbyte == 0xF0 || statusbyte == 0xF7)
                        {  // ●▼●SysEx

                            var me = new MidiEventSysEx();
                            me.ch = noteChannel;
                            me.tick = t;
                            me.stbyte = statusbyte;

                            int siz = fp.ReadDeltaTime();
                            me.bytes = fp.ReadBytes(siz);  // 注意：頭のF0を含まない

                            ev.Add(me);
                        }
                        #endregion

                        #region メタイベント
                        else if (statusbyte == 0xFF)
                        {
                            var me = new MidiEventMeta();
                            me.ch = noteChannel;
                            me.tick = t;
                            me.id = fp.ReadByte();

                            //me.name = "";
                            int siz = fp.ReadByte();
                            me.bytes = fp.ReadBytes(siz);

                            me.text = null;
                            me.val = -1;

                            switch (me.id)
                            {
                                case 0x01:
                                case 0x02:
                                case 0x03:  // Track Name
                                case 0x04:  // Instrument Name
                                case 0x05:
                                case 0x06:
                                case 0x07:
                                case 0x08:
                                case 0x09:
                                    //me.name = "Track Name";
                                    me.text = HatoEnc.Encode(me.bytes);
                                    //while (MidiTrackNames.Count <= j) { MidiTrackNames.Add(""); }
                                    //MidiTrackNames[j] = HatoEnc.Encode(buffe);
                                    break;

                                case 0x2F:  // End of Track
                                    //me.name = "End of Track";
                                    isEnd = true;
                                    break;

                                case 0x20:
                                case 0x21:
                                    me.val = me.bytes[0];
                                    break;

                                case 0x51:  // Tempo (USecondPerBeat)
                                    me.val = ((int)me.bytes[0] << 16) + ((int)me.bytes[1] << 8) + (int)me.bytes[2];
                                    InitalUSecondPerBeat = InitalUSecondPerBeat ?? me.val;
                                    break;

                                default:
                                    //me.name = "Other (" + me.id + ")";
                                    break;
                            }
                            if (!(isEnd && removeEndOfTrack)) ev.Add(me);
                        }
                        #endregion

                        else throw new Exception("if文に、よくわからないバグがありそうですね。");

                        if (isEnd) break;
                    }

                    if (k == MAX_MIDIEVENTS) throw new Exception("Midiイベント数が多すぎて解析することができませんでした。全てはList<>を使わなかった私のせいです。お手数をお掛けして申し訳ありません。");

                    if (GROUP_NOTE_ON_OFF)
                    {
                        // ここでev[].qを設定した後、evからノートオフを削除する
                        for (k = 0; k < ev.Count; k++)
                        {
                            if (ev[k] is MidiEventNote)
                            {
                                MidiEventNote me = (MidiEventNote)ev[k];  // ノートオンイベント
                                if (me.v == 0) continue;

                                int iidx = ev.FindIndex(k + 1, _me2 =>
                                {
                                    if (!(_me2 is MidiEventNote)) return false;
                                    MidiEventNote me2 = (MidiEventNote)_me2;

                                    return me2.n == me.n && me2.v == 0;  // 対応するノートオフイベントを見つける
                                });


                                if (iidx < 0) me.q = 1; //throw new Exception("ノートオフが見つからないよ");
                                else me.q = ((MidiEventNote)ev[iidx]).tick - me.tick;
                            }
                        }

                        ev.RemoveAll(me => ((me is MidiEventNote) && ((MidiEventNote)me).v == 0));
                    }
                    //ev;

                    tracks.Add(ev);
                }
            }
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.ToString());
            //}
        }

        /// <summary>
        /// この関数を使用することは推奨されません。代わりにMidiStruct.Export(str_, true)を使用してください。
        /// </summary>
        /// <param name="str_"></param>
        public void Export(Stream str_)
        {
            Export(str_, false);
        }

        /// <summary>
        /// midiバイナリファイルをストリームに書き出します。
        /// </summary>
        /// <param name="str_"></param>
        /// <param name="AddEndOfTrack"></param>
        public void Export(Stream str_, bool AddEndOfTrack)
        {
            using (BinaryWriter fp = new BinaryWriter(str_, HatoEnc.Encoding))
            {
                //MThd == 0x4D546864u;
                //MTrk == 0x4D54726Bu;

                // header
                fp.Write((uint)MThd_L);

                fp.Write((byte)0);  // header size
                fp.Write((byte)0);
                fp.Write((byte)0);
                fp.Write((byte)6);
                fp.Write((byte)0);  // midi format
                fp.Write((byte)1);
                fp.Write((byte)(tracks.Count / 256));  // the number of tracks
                fp.Write((byte)(tracks.Count % 256));
                fp.Write((byte)(resolution / 256));  // unit time
                fp.Write((byte)(resolution % 256));

                // -- tracks --
                for (int j = 0; j < tracks.Count; j++)
                {
                    fp.Write((uint)MTrk_L);
                    long ptrTrackDataLength = fp.Seek(0, SeekOrigin.Current);
                    fp.Write((uint)0);  // dummy
                    
                    List<MidiEvent> ev = new List<MidiEvent>();  // これはソート済み
                    List<MidiEvent> ev2 = new List<MidiEvent>();  // これはソートされていない
                    // LinkedListを使う必要性に迫られたことって今までに無いなあ・・・

                    foreach (MidiEvent me in tracks[j])
                    {
                        //MidiEvent me = tracks[j][k];
                        ev.Add(me);
                        MidiEventNote me2;
                        if ((me2 = me as MidiEventNote) != null)
                        {
                            //MidiEventNote me2 = (MidiEventNote)me.Clone();
                            me2 = (MidiEventNote)me2.Clone();

                            me2.tick += me2.q;
                            me2.v = 0;  // Midiノートオフ
                            me2.q = 0;  // will not used
                            ev2.Add(me2);

                            if (me2.tick < 0)
                            {
                                throw new Exception("なんでTrack" + (j + 1) + "(1 origin)内のノートオフのtickがマイナスなの");
                                //だからusing使えって言ってるだろ！！
                            }
                        }
                    }

                    //ev = new MidiTrack(ev.OrderBy(x => x).ToList());  // ？？？？？？？？？？？？？？？？？？？？？？？？？？？
                    ev2 = new List<MidiEvent>(ev2.OrderBy(x => x));
                    var dmy = new MidiEventNote();
                    dmy.tick = Int32.MaxValue;
                    ev.Add(dmy);
                    ev2.Add(dmy);
                    List<MidiEvent> merged = new List<MidiEvent>();
                    {
                        int i1 = 0, i2 = 0;
                        while (i1 < ev.Count - 1 || i2 < ev2.Count - 1)
                        {
                            // ノートオフの方が先なので、同じ場合は ev2 が先にくる。
                            if (ev[i1].tick < ev2[i2].tick)
                            {
                                merged.Add(ev[i1++]);
                            }
                            else
                            {
                                merged.Add(ev2[i2++]);
                            }
                        }
                    }

                    MidiTrack product = new MidiTrack(merged);
                    ev = ev2 = null;
                    merged = null;

                    if (AddEndOfTrack)
                    {
                        product.AddEndOfTrack(this);
                    }

                    int t = 0;

                    for (int k = 0; k < product.Count; k++)
                    {
                        MidiEvent me = product[k];

                        #region デルタタイムの書き込み
                        {
                            int deltatime = me.tick - t;/////////////////////////////////
                            t = me.tick;
                            if (deltatime < 0) throw new Exception("ソートされてない");

                            // デルタタイムの書き込み
                            /*
                            int nextMSB = 0;
                            List<byte> split7bit = new List<byte>();  // 結構処理時間がかかっている(4.7%)
                            while (deltatime != 0 || nextMSB == 0)
                            {
                                int nextbyte = deltatime & 0x7F;
                                deltatime >>= 7;

                                split7bit.Add((byte)(nextbyte | nextMSB));

                                nextMSB = 0x80;
                            }
                            split7bit.Reverse();  // 結構処理時間がかかっている(8.1%)
                            fp.Write(split7bit.ToArray());
                             * */
                            // 4バイトまでしかダメなんだっけ？
                            if (deltatime < 0x80)
                            {
                                fp.Write((byte)deltatime);
                            }
                            else if (deltatime < 0x4000)
                            {
                                fp.Write((byte)((deltatime >> 7) | 0x80));
                                fp.Write((byte)(deltatime & 0x7F));
                            }
                            else if (deltatime < 0x200000)
                            {
                                fp.Write((byte)((deltatime >> 14) | 0x80));
                                fp.Write((byte)((deltatime >> 7) | 0x80));
                                fp.Write((byte)(deltatime & 0x7F));
                            }
                            else if (deltatime < 0x10000000)
                            {
                                fp.Write((byte)((deltatime >> 21) | 0x80));
                                fp.Write((byte)((deltatime >> 14) | 0x80));
                                fp.Write((byte)((deltatime >> 7) | 0x80));
                                fp.Write((byte)(deltatime & 0x7F));
                            }
                            else
                            {
                                fp.Write((byte)((deltatime >> 28) | 0x80));
                                fp.Write((byte)((deltatime >> 21) | 0x80));
                                fp.Write((byte)((deltatime >> 14) | 0x80));
                                fp.Write((byte)((deltatime >> 7) | 0x80));
                                fp.Write((byte)(deltatime & 0x7F));
                            }
                            // ↑処理時間を減らせた！！
                        }
                        #endregion

                        #region イベントの内容の書き込み
                        if (me is MidiEventNote)
                        {
                            int statusbyte = 0x90 + (me.ch & 15);
                            fp.Write((byte)statusbyte);
                            fp.Write((byte)((MidiEventNote)me).n);
                            fp.Write((byte)((MidiEventNote)me).v);
                        }
                        else if (me is MidiEventCC)
                        {
                            int statusbyte = 0xB0 + (me.ch & 15);
                            fp.Write((byte)statusbyte);
                            fp.Write((byte)((MidiEventCC)me).cc);
                            fp.Write((byte)((MidiEventCC)me).val);
                            if (((MidiEventCC)me).cc == 0x7E)
                            {
                                throw new Exception("コントロールチェンジ 0x7E はやめてーーー");
                            }
                        }
                        else if (me is MidiEventKeyPressure)
                        {
                            int statusbyte = 0xA0 + (me.ch & 15);
                            fp.Write((byte)statusbyte);
                            fp.Write((byte)((MidiEventKeyPressure)me).n);
                            fp.Write((byte)((MidiEventKeyPressure)me).val);
                        }
                        else if (me is MidiEventChannelPressure)
                        {
                            int statusbyte = 0xD0 + (me.ch & 15);
                            fp.Write((byte)statusbyte);
                            fp.Write((byte)((MidiEventChannelPressure)me).val);
                        }
                        else if (me is MidiEventProgram)
                        {
                            int statusbyte = 0xC0 + (me.ch & 15);
                            fp.Write((byte)statusbyte);
                            fp.Write((byte)((MidiEventProgram)me).val);
                        }
                        else if (me is MidiEventPB)
                        {
                            int statusbyte = 0xE0 + (me.ch & 15);
                            fp.Write((byte)statusbyte);
                            fp.Write((byte)(((((MidiEventPB)me).val + 8192) >> 0) & 0x7F));  // 括弧多すぎて間違える
                            fp.Write((byte)(((((MidiEventPB)me).val + 8192) >> 7) & 0x7F));
                        }
                        else if (me is MidiEventSysEx)
                        {
                            // 実装間違ってるよ！！！！！！！！！！！！！！！！？？？？？？？？？？めんどい！！！！！！！！！！！
                            int statusbyte = ((MidiEventSysEx)me).stbyte;
                            if (statusbyte != 0xF0 && statusbyte != 0xF7) throw new Exception("SysExなのにstatusbyteがおかしい");
                            fp.Write((byte)statusbyte);
                            fp.Write((byte)((MidiEventSysEx)me).bytes.Length);
                            fp.Write(((MidiEventSysEx)me).bytes);
                        }
                        else if (me is MidiEventMeta)
                        {
                            int statusbyte = 0xFF;
                            fp.Write((byte)statusbyte);
                            fp.Write((byte)((MidiEventMeta)me).id);
                            fp.Write((byte)((MidiEventMeta)me).bytes.Length);
                            fp.Write(((MidiEventMeta)me).bytes);
                        }
                        else
                        {
                            throw new Exception("謎");
                        }
                        #endregion
                    }

                    long ptrTrackEnd = fp.Seek(0, SeekOrigin.Current);
                    fp.Seek((int)ptrTrackDataLength, SeekOrigin.Begin);
                    int trackLength = (int)(ptrTrackEnd - ptrTrackDataLength - 4L);
                    fp.Write((byte)((trackLength >> 24) & 0xFF));
                    fp.Write((byte)((trackLength >> 16) & 0xFF));
                    fp.Write((byte)((trackLength >> 8) & 0xFF));
                    fp.Write((byte)((trackLength >> 0) & 0xFF));
                    fp.Seek((int)ptrTrackEnd, SeekOrigin.Begin);
                }

                //fp.Close();
            }
        }

        /// <summary>
        /// Midiファイル構造を表す文字列に変換します。
        /// 改行文字は\nです
        /// </summary>
        /// <returns></returns>
        public override String ToString()
        {
            StringBuilder s0 = new StringBuilder();
            s0.Append("Midi Struct\n");
            s0.Append("  midi format = " + formatnumber + "\n");
            s0.Append("  the number of track = " + tracks.Count + "\n");
            s0.Append("  unit time = " + resolution + "\n");
            s0.Append("\n");

            for (int i = 0; i < tracks.Count; i++)
            {
                s0.Append("---------------------------------------------\n");
                if (i == 0)
                {
                    s0.Append("Track " + i + " (conductor track)\n");
                }
                else
                {
                    s0.Append("Track " + i + "\n");
                }
                s0.Append(tracks[i].ToString());
                s0.Append("\n");
            }
            return s0.ToString();
        }

        public int BeatsToTicks(int beats)
        {
            if (resolution == null) throw new Exception("resolution指定されてない");
            return beats * (resolution ?? 480);
        }
    }

}
