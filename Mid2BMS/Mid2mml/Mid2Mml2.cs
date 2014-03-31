using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    class Mid2mml2
    {
        StringSuruyatuSafe s2;
        String br = "\r\n";

        private bool channelEnabled = true;
        private bool portEnabled = false;

        private static String[] noteHeight = new[] { "c", "c+", "d", "d+", "e", "f", "f+", "g", "g+", "a", "a+", "b" };

        public Mid2mml2(MidiStruct ms)
        {
            int resolution = (ms.resolution ?? 480);
            //int maxreso = 48;

            //s2 = (null + "");  // ←！？！？！？！？
            s2 = new StringSuruyatuSafe();

            //s2 = s2 + "TimeBase(" + (ms.resolution ?? 480) + ")" + br;  // ←Error:型intを型StringSuruyatu
            //s2 += "TimeBase(" + (ms.resolution ?? 480) + ")" + br;
            s2 = s2 + "TimeBase(" + resolution + ")" + br;


            for (int i = 0; i < ms.tracks.Count; i++)
            {
                int lastVel = -99;
                int lastOct = -99;
                int lastCh = -99;

                int evcount = 0;
                int evline = 32;


                MidiTrack mtrk = ms.tracks[i];

                s2 = s2 + br + br + "Track(" + i + ")" + br;
                s2 = s2 + " ";

                if (mtrk.Count != 0 && mtrk[0].tick != 0)
                {
                    s2 = s2 + "r%" + mtrk[0].tick;
                    if (++evcount >= evline) { s2 = s2 + br; evcount = 0; }
                }

                for (int j = 0; j < mtrk.Count; j++)
                {
                    MidiEvent me_ = mtrk[j];
                    int deltatime = (j + 1 < mtrk.Count) ? (mtrk[j + 1].tick - me_.tick) : resolution;

                    if (channelEnabled)
                    {
                        if (me_ is MidiEventMeta || me_ is MidiEventSysEx)
                        {
                            // do nothing
                        }
                        else if (me_.ch != lastCh)
                        {
                            s2 = s2 + " CH(" + (me_.ch + 1) + ")";
                            lastCh = me_.ch;
                        }
                    }

                    #region case MidiEventNote:
                    {
                        // ブロックで囲まなきゃいけないのクソすぎるんですが(やっぱり継承は使うべきでなかった？？？
                        MidiEventNote me = me_ as MidiEventNote;
                        if (me != null)
                        {
                            if (me.v != lastVel)
                            {
                                s2 = s2 + " v" + me.v;
                                lastVel = me.v;
                            }

                            int oct = me.n / 12;
                            switch (oct - lastOct)
                            {
                                case 0:
                                    break;
                                case 1:
                                    s2 = s2 + ";>";
                                    break;
                                case -1:
                                    s2 = s2 + ";<";
                                    break;
                                default:
                                    s2 = s2 + " o" + oct;
                                    break;
                            }
                            lastOct = oct;

                            if (deltatime == 0)
                            {
                                // サクラのバグか？？？
                                s2 = s2 + " Sub{" + noteHeight[me.n % 12] + "%1,%" + me.q + "}";
                            }
                            else
                            {
                                /*
                                String gate_postfix = "";

                                // 思ったんだが、忠実にlengthを再現するより、休符と組み合わせたほうが良いのでは

                                #region ゲートの簡易表記
                                if (deltatime != 0 && me.q * 100 % deltatime == 0)
                                {
                                    // ゲートは100分率指定出来る
                                    s2 = s2 + " q" + (me.q * 100 / deltatime);  // 後で修正する
                                }
                                else
                                {
                                    gate_postfix = ",%" + me.q;
                                }
                                #endregion

                                #region Length(deltatime)の簡易表記
                                if (resolution % deltatime == 0)
                                {
                                    //
                                    Frac beattime = (new Frac(deltatime, resolution)).Reduce();  // beat単位

                                    s2 = s2 + "%" + deltatime;
                                }
                                else
                                {
                                    s2 = s2 + "%" + deltatime;
                                }
                                #endregion

                                s2 = s2 + noteHeight[me.n % 12];

                                s2 = s2 + gate_postfix;
                                 */
                                s2 = s2 + noteHeight[me.n % 12] + "%" + deltatime + ",%" + me.q;
                            }

                            if (++evcount >= evline) { s2 = s2 + br; evcount = 0; }
                            continue;
                        }
                    }
                    #endregion

                    #region case MidiEventCC:
                    {
                        MidiEventCC me = me_ as MidiEventCC;
                        if (me != null)
                        {
                            switch (me.cc)
                            {
                                case 1: // Modulation
                                    s2 = s2 + " M(" + me.val + ")";
                                    break;
                                case 7: // Volume
                                    s2 = s2 + " V(" + me.val + ")";
                                    break;
                                case 10: // Panpot
                                    s2 = s2 + " P(" + me.val + ")";
                                    break;
                                case 11: // Expression
                                    s2 = s2 + " EP(" + me.val + ")";
                                    break;
                                //case 64: // Hold Pedal
                                case 91: // Reverb
                                    s2 = s2 + " REV(" + me.val + ")";
                                    break;
                                case 93: // Chorus
                                    s2 = s2 + " CHO(" + me.val + ")";
                                    break;
                                default:
                                    s2 = s2 + " y" + me.cc + "," + me.val + "";
                                    break;
                            }
                            s2 = s2 + "r%" + deltatime;

                            if (++evcount >= evline) { s2 = s2 + br; evcount = 0; }
                            continue;
                        }
                    }
                    #endregion

                    #region case MidiEventMeta:
                    {
                        MidiEventMeta me = me_ as MidiEventMeta;
                        if (me != null)
                        {
                            bool newline = false;
                            switch (me.id)
                            {
                                case 0x01:  // Text
                                    if (evcount != 0) { s2 = s2 + br; }
                                    s2 = s2 + "MetaText={\"" + me.text + "\"}" + br;
                                    newline = true;
                                    break;
                                case 0x02:  // Copyright
                                    if (evcount != 0) { s2 = s2 + br; }
                                    s2 = s2 + "Copyright={\"" + me.text + "\"}" + br;
                                    newline = true;
                                    break;
                                case 0x03: // "Track Name";
                                    if (evcount != 0) { s2 = s2 + br; }
                                    s2 = s2 + "TrackName={\"" + me.text + "\"}" + br;
                                    newline = true;
                                    break;
                                case 0x04: // "Instrument Name";
                                    if (evcount != 0) { s2 = s2 + br; }
                                    s2 = s2 + "InstrumentName={\"" + me.text + "\"}" + br;
                                    newline = true;
                                    break;
                                case 0x05: // "Lyric";
                                    if (evcount != 0) { s2 = s2 + br; }
                                    s2 = s2 + "Lyric={\"" + me.text + "\"}" + br;
                                    newline = true;
                                    break;
                                case 0x06: // "Marker";
                                    if (evcount != 0) { s2 = s2 + br; }
                                    s2 = s2 + "Marker={\"" + me.text + "\"}" + br;
                                    newline = true;
                                    break;
                                case 0x07: // CuePoint
                                    if (evcount != 0) { s2 = s2 + br; }
                                    s2 = s2 + "CuePoint={\"" + me.text + "\"}" + br;
                                    newline = true;
                                    break;

                                case 0x21: // "Port";
                                    if (portEnabled)
                                    {
                                        s2 = s2 + " Port(" + me.val + ")";
                                    }
                                    else
                                    {
                                        s2 = s2 + " /*  Port(" + me.val + ") */ ";
                                    }
                                    break;
                                case 0x2F: // "End of Track";
                                    break;
                                case 0x51: // "Tempo (usec per beat)";
                                    s2 = s2 + " Tempo(" + (Math.Round(1000.0m * (decimal)(60000000.0 / me.val)) * 0.001m).ToString("G6") + ")";
                                    break;
                                //case 0x54: // "SMPTE Offset";
                                case 0x58: // "Signature";
                                    s2 = s2 + " TimeSignature=" + me.bytes[0] + "," + (1 << me.bytes[1]) + ";";
                                    break;
                                //case 0x59: // "Key";

                                default: // "Other (" + this.id + ")";
                                    if (evcount != 0) { s2 = s2 + br; }
                                    s2 = s2 + " /* unknown meta ev " + me.id + " = "
                                        + me.bytes.Select(x => x.ToString("X2")).Join(",") + " */ " + br;
                                    newline = true;
                                    break;
                            }

                            if (newline)
                            {
                                evcount = 0;

                                if (deltatime != 0)
                                {
                                    s2 = s2 + "r%" + deltatime;
                                    if (++evcount >= evline) { s2 = s2 + br; evcount = 0; }
                                }
                            }
                            else
                            {
                                if (++evcount >= evline) { s2 = s2 + br; evcount = 0; }

                                if (deltatime != 0)
                                {
                                    s2 = s2 + "r%" + deltatime;
                                    if (++evcount >= evline) { s2 = s2 + br; evcount = 0; }
                                }
                            }
                            continue;
                        }
                    }
                    #endregion

                    #region MidiEventPB
                    {
                        MidiEventPB me = me_ as MidiEventPB;
                        if (me != null)
                        {
                            s2 = s2 + " p%(" + me.val + ")";

                            if (deltatime != 0)
                            {
                                s2 = s2 + "r%" + deltatime;
                            }
                            if (++evcount >= evline) { s2 = s2 + br; evcount = 0; }
                            continue;
                        }
                    }
                    #endregion

                    #region MidiEventProgram
                    {
                        MidiEventProgram me = me_ as MidiEventProgram;
                        if (me != null)
                        {
                            s2 = s2 + " @" + (me.val + 1) + "";

                            if (deltatime != 0)
                            {
                                s2 = s2 + "r%" + deltatime;
                            }
                            if (++evcount >= evline) { s2 = s2 + br; evcount = 0; }
                            continue;
                        }
                    }
                    #endregion

                    #region MidiEventSysEx
                    {
                        MidiEventSysEx me = me_ as MidiEventSysEx;
                        if (me != null)
                        {
                            if (evcount != 0) { s2 = s2 + br; }
                            evcount = 0;

                            s2 = s2 + " SysEx$=" + me.bytes.Select(x => x.ToString("X2")).Join(",") + ";" + br;

                            if (deltatime != 0)
                            {
                                s2 = s2 + "r%" + deltatime;
                                if (++evcount >= evline) { s2 = s2 + br; evcount = 0; }
                            }
                            continue;
                        }
                    }
                    #endregion

                    #region ↓テンプレ
                    {
                        MidiEventNote me = me_ as MidiEventNote;
                        if (me != null)
                        {
                            // ...

                            if (deltatime != 0)
                            {
                                s2 = s2 + "r%" + deltatime;
                            }
                            if (++evcount >= evline) { s2 = s2 + br; evcount = 0; }
                            continue;
                        }
                    }
                    #endregion

                    // default case
                    if (evcount != 0) { s2 = s2 + br; }
                    evcount = 0;

                    s2 = s2 + " /* " + me_.GetType().ToString() + " */";

                    if (deltatime != 0)
                    {
                        s2 = s2 + "r%" + deltatime;
                        if (++evcount >= evline) { s2 = s2 + br; evcount = 0; }
                    }
                }

                if (evcount != 0) { s2 = s2 + br; }
            }
        }

        public override String ToString()
        {
            return s2.ToString();
        }
    }
}
