using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    class NameWaves
    {
        public List<String> wavnms;
        public bool IsRedMode = true;

        String getNoteHeight(int nt)
        {
            switch (nt % 12)
            {
                case 0: return "c";
                case 1: return "cp";
                case 2: return "d";
                case 3: return "dp";
                case 4: return "e";
                case 5: return "f";
                case 6: return "fp";
                case 7: return "g";
                case 8: return "gp";
                case 9: return "a";
                case 10: return "ap";
                case 11: return "b";
            }
            return "";
        }

        String getNoteString(int namingway, int nt, long Ln1, long Ln2, int velo)
        {
            if (namingway == 0)
            {
                //if (at > 0)
                //{
                //    return "at" + at + "v" + velo + "l" + Ln2 + (Ln1 == 1 ? "" : ("_" + Ln1)) + "" + "o" + (nt / 12) + getNoteHeight(nt);
                //}
                //else
                //{
                return "v" + velo + "l" + Ln2 + (Ln1 == 1 ? "" : ("_" + Ln1)) + "" + "o" + (nt / 12) + getNoteHeight(nt);
                //}
            }
            if (namingway == 1)
            {
                return "o" + (nt / 12) + getNoteHeight(nt);
            }
            return "NULL";
        }

        String getNoteStringPurpleMode(int namingway, int nt, long Ln1, long Ln2, int velo, int prevNoteN)
        {
            if (namingway == 0)
            {
                //if (at > 0)
                //{
                //    return "at" + at + "v" + velo + "l" + Ln2 + (Ln1 == 1 ? "" : ("_" + Ln1)) + "" + "o" + (nt / 12) + getNoteHeight(nt);
                //}
                //else
                //{
                return "v" + velo + "l" + Ln2 + (Ln1 == 1 ? "" : ("_" + Ln1)) + "" + "o" + (nt / 12) + getNoteHeight(nt) + "_" + "o" + (prevNoteN / 12) + getNoteHeight(prevNoteN);
                //}
            }
            if (namingway == 1)
            {
                return "o" + (nt / 12) + getNoteHeight(nt);
            }
            return "NULL";
        }

        String getNoteStringRedMode(int namingway, int number, int nt, long Ln1, long Ln2, int velo)
        {
            if (namingway == 0)
            {
                //if (at > 0)
                //{
                //    return String.Format("{0:D5}_",number + 1) + "at" + at + "v" + velo + "l" + Ln2 + (Ln1 == 1 ? "" : ("_" + Ln1)) + "" + "o" + (nt / 12) + getNoteHeight(nt);
                //}
                //else
                //{
                    return String.Format("{0:D5}_", number + 1) + "v" + velo + "l" + Ln2 + (Ln1 == 1 ? "" : ("_" + Ln1)) + "" + "o" + (nt / 12) + getNoteHeight(nt);
                //}
            }
            if (namingway == 1)
            {
                return "o" + (nt / 12) + getNoteHeight(nt);
            }
            return "NULL";
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
        
        /// <summary>
        /// ChordModeがtrueの場合に使用します。
        /// wavファイルに名前を付けます
        /// [ InputFileNamePrefix, InputFileNameSuffix, OriginalIndex, Filename1, Filename2, ... ]
        /// </summary>
        /// <param name="namingway">予約領域</param>
        /// <param name="ib">入力</param>
        /// <param name="ia">入力</param>
        /// <param name="ob">出力</param>
        /// <param name="oa">出力</param>
        /// <param name="bb">BMS</param>
        /// <param name="ba">BMS</param>
        /// <param name="ntantmC">MidiInterpreter3.GetNta()</param>
        /// <returns></returns>
        public String AllNoteToName(
            int namingway, String ib, String ia, String ob, String oa, String bb, String ba,
            out String OutputInArrayFormat, bool isRedMode, bool isPurpleMode, List<List<MNote>> ntantmC)
        {
            if (isPurpleMode) throw new Exception("purplemodeの場合にchord modeを選択することは出来ないよ");

            IsRedMode = isRedMode;

            int i;
            //String s2 = "";
            String w;
            StringBuilder s2 = new StringBuilder();
            String s0;

            wavnms = new List<string>();

            // input file name prefix
            s2.Append("" + ib + "\r\n");

            // input file name suffix
            s2.Append("" + ia + "\r\n");

            s2.Append("1" + "\r\n");  // original index

            for (i = 0; i < ntantmC.Count; i++)
            {
                //s0 = getNoteString(namingway, ntantm[i].VoiceN, ntantm[i].n, ntantm[i].l.n, ntantm[i].l.d, ntantm[i].v);
                s0 = String.Format("{0:D5}_", i + 1);
                foreach (MNote mnote in ntantmC[i])
                {
                    s0 += getNoteHeight(mnote.n);
                }
                wavnms.Add(bb + s0 + ba);
                w = ob + s0 + oa;
                s2.Append(w + "\r\n");
            }

            s2.Append("//\r\n");

            OutputInArrayFormat = s2.ToString();
            return "";
        }


        /// <summary>
        /// wavファイルに名前を付けます
        /// [ InputFileNamePrefix, InputFileNameSuffix, OriginalIndex, Filename1, Filename2, ... ]
        /// </summary>
        /// <param name="namingway">予約領域</param>
        /// <param name="ib">入力</param>
        /// <param name="ia">入力</param>
        /// <param name="ob">出力</param>
        /// <param name="oa">出力</param>
        /// <param name="bb">BMS</param>
        /// <param name="ba">BMS</param>
        /// <param name="ntantm">MidiInterpreter3.GetNta()</param>
        /// <returns></returns>
        public String AllNoteToName(
            int namingway , String ib, String ia, String ob, String oa, String bb, String ba,
            out String OutputInArrayFormat, bool isRedMode, bool isPurpleMode, List<MNote> ntantm)
        {
            IsRedMode = isRedMode;

            int i;
            //String s2 = "";
            String w;
            StringBuilder s2 = new StringBuilder();
            String s0;

            wavnms = new List<string>();

            // input file name prefix
            s2.Append("" + ib + "\r\n");

            // input file name suffix
            s2.Append("" + ia + "\r\n");

            s2.Append("1" + "\r\n");  // original index

            if (!IsRedMode && !isPurpleMode)  // blue mode
            {
                for (i = 0; i < ntantm.Count; i++)
                {
                    //s0 = getNoteString(namingway, ntantm[i].VoiceN, ntantm[i].n, ntantm[i].l.n, ntantm[i].l.d, ntantm[i].v);
                    s0 = getNoteString(namingway, ntantm[i].n, ntantm[i].l.n, ntantm[i].l.d, ntantm[i].v);
                    wavnms.Add(bb + s0 + ba);
                    w = ob + s0 + oa;
                    s2.Append(w + "\r\n");
                }
            }
            else if (isPurpleMode)  // purple mode
            {
                for (i = 0; i < ntantm.Count; i++)
                {
                    //s0 = getNoteStringRedMode(namingway, i, ntantm[i].VoiceN, ntantm[i].n, ntantm[i].l.n, ntantm[i].l.d, ntantm[i].v);
                    s0 = getNoteStringPurpleMode(namingway, ntantm[i].n, ntantm[i].l.n, ntantm[i].l.d, ntantm[i].v, ntantm[i].prev.n);
                    wavnms.Add(bb + s0 + ba);
                    w = ob + s0 + oa;

                    // previous note
                    s2.Append("____dummy_" + w + "\r\n");

                    // current note
                    s2.Append(w + "\r\n");
                }
            }
            else  // red mode
            {
                for (i = 0; i < ntantm.Count; i++)
                {
                    //s0 = getNoteStringRedMode(namingway, i, ntantm[i].VoiceN, ntantm[i].n, ntantm[i].l.n, ntantm[i].l.d, ntantm[i].v);
                    s0 = getNoteStringRedMode(namingway, i, ntantm[i].n, ntantm[i].l.n, ntantm[i].l.d, ntantm[i].v);
                    wavnms.Add(bb + s0 + ba);
                    w = ob + s0 + oa;
                    s2.Append(w + "\r\n");
                }
            }

            s2.Append("//\r\n");

            OutputInArrayFormat = s2.ToString();
            return "";
        }
    }
}