#define DEBUGNOW

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace Mid2BMS
{
    class MelodyWalker
    {

        //public int VacantWavid = 1;  // 36進数
        public int VacantBMSChannelIdx = 0;  // 0から始まる、 channelTemplate の添え字
        public int WavidSpacing = 4;

        List<String> MMLs = new List<String>();
        List<String> MidiTrackNames = new List<String>();

        String WavFileName_Prefix_Blue = "b_";
        String WavFileName_Prefix_Red = "r_";
        String WavFileName_Prefix_Purple = "p_";
        String WavFileName_In = "_3.";
        String WavFileName_Out = "_";
        String WavFileName_Bms = "_";

        /// <summary>
        /// MMLデータ(text0_stdout_part1.mml)を受け取り、text1～text8 他を生成します。
        /// </summary>
        public int MultiProcess(List<String> MMLs_, List<String> MidiTrackNames_,
            List<bool> isDrumsList, List<bool> ignoreList, List<bool> isChordList, List<bool> isXChainList, List<bool> isOneShotList, bool sequenceLayer, String pathBase,
            bool isRedMode, bool isPurpleMode, bool createExFiles, ref int VacantWavid, int timebase,
            String margintime_beats, out String trackCsv, out List<bool> isEmptyList, decimal bpm,
            ref double progressValue, double progressMin, double progressMax)
        {
            MMLs = MMLs_;
            MidiTrackNames = MidiTrackNames_;
            //VacantWavid = VacantWavid_;
            MMLs_ = MidiTrackNames_ = null;  // 初期化
            //VacantWavid_ = -9999;  // 初期化

            isEmptyList = new List<bool>();

            //String[] text = new String[9];  // String Builderじゃない！良くない！
            //text[0] = text[1] = text[2] = text[3] = text[4] = text[5] = text[6] = text[7] = text[8] = "";
            StringSuruyatu[] text = new StringSuruyatu[9];
            //for (int j = 0; j < 9; j++) text[j] = new StringSuruyatu();

            //text[2] = "TimeBase(384*16)\r\n\r\n";
            text[2] = "TimeBase(3840)\r\n\r\n";

            text[8] += "Tr\t" + "nta\t" + "ntm\t" + "TrackName\r\n";
            text[8] += "\t" + "(waves)\t" + "(notes)\t" + "\r\n";

            text[5] += "#TITLE " + MidiTrackNames[0] + "\r\n";  // BMS header
            text[5] += "#BPM " + ((bpm == (Int64)bpm) ? ((Int64)bpm).ToString() : bpm.ToString()) + "\r\n";  // そこまでして小数bpmに対応したかったのか？
            text[5] += "#PLAYER 3\r\n\r\n";

            if (bpm >= 300 || bpm < 50)
            {
                MessageBox.Show(
                    "BPMが" + (bpm >= 300 ? "高い" : "低い") + "midiファイルを読み込んだようです。(BPM = " + bpm + ")\n" +
                    "もし、次の段階で正常に音切りが出来なかった場合は、\n" +
                    "[1]Mid2MMLタブにある、MarginTimeの項を、「 BPM / 10 」程度の値にしてみてください。",
                    "Notice : BPM is very high");
            }
            
            MidiStruct tanon_ms = new MidiStruct(3840);  // 15360は大きすぎるかな、と思いこの値に
            Frac midiTime = new Frac(4);

            //bool messageShown = false;

            for (int i = 0; i < MMLs.Count; i++)
            {
                /*if (!messageShown && VacantWavid >= 36 * 36 + 4)
                {
                    if (DialogResult.Yes == MessageBox.Show(
                        "BMSの定義番号がを1295を超過しました。(Yes to abort)",
                         "Confirm to continue", MessageBoxButtons.YesNo))
                    {
                        throw new Exception("ユーザーの指示により処理を中断しました。");
                    }
                    messageShown = true;
                }*/

                bool isEmpty = true;
                try
                {

                    // ArrayかList<>かIEnumerableかで迷ったら、とりあえずList使っとけばいいみたいなのはある
                    bool isDrums = (isDrumsList == null) ? false : isDrumsList[i];
                    //progressValue = progressMin + (progressMax - progressMin) * i / mml_multi.Length;
                    bool xchainChecked = (isXChainList == null) ? false : isXChainList[i];
                    bool ignoreChecked = (ignoreList == null) ? false : ignoreList[i];
                    bool ignore = ignoreChecked || (isRedMode && sequenceLayer && xchainChecked);  // BMSおよびrenamer_array（およびpurpleまたはblueの場合はmidi）を出力するかどうか
                    bool isChordMode = (isChordList == null) ? false : isChordList[i];
                    bool isOneShot = (isOneShotList == null) ? false : isOneShotList[i];
                    
                    if (ignore)
                    {
                        //isEmptyList.Add(true);  // バグ・・・フローの改善を求めます・・・
                        continue;
                    }

                    if (!sequenceLayer) midiTime = new Frac(4);

                    Process(i, (i < MidiTrackNames.Count) ? MidiTrackNames[i] : "MidiTrack " + (i + 1), MMLs[i], pathBase, text, tanon_ms,
                        0, 0, isRedMode, isPurpleMode, isDrums, isChordMode, isOneShot,
                        out isEmpty, midiTime, ref VacantWavid, timebase, margintime_beats,
                        ref progressValue,
                        progressMin + (progressMax - progressMin) * i / MMLs.Count,
                        progressMin + (progressMax - progressMin) * (i + 1) / MMLs.Count);

                    if (!isEmpty)
                    {
                        midiTime.Add(4);  // トラック同士の間に、追加の４ターンを得ることが出来る！！
                    }

                    text[0] += "\r\n\r\n\r\n";  // これを if(!isEmpty)の中に入れてはならない
                    text[1] += "\r\n\r\n\r\n";
                    text[2] += "\r\n\r\n\r\n";
                    text[3] += "\r\n\r\n\r\n";
                    text[4] += "";
                    text[5] += "\r\n\r\n\r\n";
                    text[6] += "\r\n\r\n\r\n";
                    text[7] += "\r\n";
                    text[8] += "\r\n";


                    //isEmptyList.Add(isEmpty);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());

                    text[7] += "----( TrackNo = " + i + " (0 origin) )-----------------------------------\r\n";
                    text[7] += e.ToString() + "\r\n";
                    text[7] += "\r\n---------------------------------------\r\n";
                    text[7] += /*mml_single + */ "\r\n";
                    text[7] += "---------------------------------------\r\n";
                }
                finally
                {
                    isEmptyList.Add(isEmpty);
                }
            }

            String BMSFileName = isRedMode ? @"text6_bms_red.txt" : (isPurpleMode ? @"text6_bms_purple.txt" : @"text6_bms_blue.txt");
            FileIO.WriteAllText(pathBase + @"text5_renamer_array.txt", text[4]);
            FileIO.WriteAllText(pathBase + BMSFileName, text[5]);
            FileIO.WriteAllText(pathBase + @"text9_trackname_csv.txt", text[8]);

            if (createExFiles)
            {
                FileIO.WriteAllText(pathBase + @"text1_midtable_debug.txt", text[0]);
                FileIO.WriteAllText(pathBase + @"text2_mmlrenew_debug.txt", text[1]);
                FileIO.WriteAllText(pathBase + @"text3_tanon.mml", text[2]);
                FileIO.WriteAllText(pathBase + @"text4_renamer.txt", text[3]);
                //FileStreamFactory.WriteAllText(pathBase + @"text7_bms_for_debug.txt", text[6]);
                //FileStreamFactory.WriteAllText(pathBase + @"text8_errorlog_debug.txt", text[7]);
            }

            if (!isRedMode)
            {
                String tanon_smf_filename = @"text3_tanon_smf" + (isPurpleMode ? "_purple" : "_blue") + @".mid";
                tanon_ms.Export(neu.IFileStream(pathBase + tanon_smf_filename, FileMode.Create, FileAccess.Write), true);
            }

            trackCsv = text[8];
            return 0;
        }
        public int Process(int TrackIndex, String MidiTrackName, String mml, String pathBase, StringSuruyatu[] text, MidiStruct tanon_ms,
            int channel, int wavid, bool isRedMode, bool isPurpleMode,
            bool isDrums, bool isChordMode, bool isOneShot, out bool isEmpty, Frac midiTime, ref int VacantWavid, int timebase, String margintime_beats,
            ref double progressValue, double progressMin, double progressMax)
        {
            MidInterpreter mi;
            MidInterpreter2 mw;
            MidInterpreter3 mi3;
            NameWaves nw;
            BMSPlacement bp;
            //NameWaves nw2;
            //BMSPlacement bp2;

            // MidInterpreter.Process()
            // MidInterpreter2.walkOnAMelody_Godo()
            // MidInterpreter3.walkOnAMelodyV2()
            // NameWaves.AllNoteToName()
            // BMSPlacement.Process()

            mi = new MidInterpreter();
            mi.SetTimeBase(timebase);
            String midtable = mi.Process(mml);
            text[0] += midtable;

            String BMSTrackName = MidiTrackName; //"[$$$TRACKNAME(" + TrackIndex + ")$$$]";


            mw = new MidInterpreter2();
            mw.SetTimeBase(timebase);
            StringSuruyatu errmsg;
            String midtable2 = mw.walkOnAMelody_Godo(midtable, out errmsg);  // ここが一番遅い処理（だった？）らしいよ
            text[1] += midtable2;
            text[7] += errmsg;
            progressValue = progressMin + (progressMax - progressMin) * 0.20;

            


            mi3 = new MidInterpreter3();  // ここがほとんどメインの処理です
            mi3.ChordModeEnabled = isChordMode;
            mi3.margintime_beats = margintime_beats;
            text[2] += mi3.walkOnAMelodyV2(tanon_ms, TrackIndex, MidiTrackName, midtable2, midiTime, isPurpleMode, isOneShot);
            progressValue = progressMin + (progressMax - progressMin) * 0.40;
            isEmpty = isChordMode ? (mi3.ntaChord.Count == 0) : (mi3.nta.Count == 0);



            nw = new NameWaves();
            String WavFileName_Prefix = isRedMode ? WavFileName_Prefix_Red : isPurpleMode ? WavFileName_Prefix_Purple : WavFileName_Prefix_Blue;
            String outInArray;
            if (isChordMode)
            {
                text[3] += nw.AllNoteToName(0,
                    //WavFileName_Prefix + BMSTrackName + WavFileName_In, ".wav",
                    BMSTrackName, ".wav",
                    WavFileName_Prefix + BMSTrackName + WavFileName_Out, ".wav",
                    WavFileName_Prefix + BMSTrackName + WavFileName_Bms, ".wav",
                    out outInArray, isRedMode, isPurpleMode, isRedMode ? mi3.ntmChordList : mi3.ntaChord);
            }
            else
            {
                text[3] += nw.AllNoteToName(0,
                    //WavFileName_Prefix + BMSTrackName + WavFileName_In, ".wav",
                    BMSTrackName, ".wav",
                    WavFileName_Prefix + BMSTrackName + WavFileName_Out, ".wav",
                    WavFileName_Prefix + BMSTrackName + WavFileName_Bms, ".wav",
                    out outInArray, isRedMode, isPurpleMode, isRedMode ? mi3.ntm : mi3.nta, isOneShot);
            }
            progressValue = progressMin + (progressMax - progressMin) * 0.60;
            text[4] += outInArray;


            int lastMeasure = isChordMode
                ? ((mi3.ntmChordList.Count == 0) ? 2 : ((int)Math.Floor(mi3.ntmChordList.Select(x => (double)(x[0].t)).Max()) + 2))
                : ((mi3.ntm.Count == 0) ? 2 : ((int)Math.Floor(mi3.ntm.Select(x => (double)(x.t)).Max()) + 2));
            int placementEndMeasure = Math.Min(999, lastMeasure + 100);

            bp = new BMSPlacement();
            int vacant_wi = VacantWavid;
            int vacant_ch = VacantBMSChannelIdx;
            if (isChordMode)
            {
                if (isDrums) throw new Exception("Drums? と Chord? を同時に有効にすることは出来ません");
                bp.ntm2nta = mi3.ntm2nta;
                text[5] += bp.Process(ref VacantWavid, ref VacantBMSChannelIdx, 1, placementEndMeasure,
                    isRedMode, isPurpleMode, isDrums, isChordMode,
                    mi3.ntaChord.Select(x => x[0]).ToList(),
                    mi3.ntmChordList.Select(x => x[0]).ToList(),
                    nw.wavnms);
            }
            else
            {
                text[5] += bp.Process(ref VacantWavid, ref VacantBMSChannelIdx, 1, placementEndMeasure,
                    isRedMode, isPurpleMode, isDrums, isChordMode,
                    mi3.nta, mi3.ntm, nw.wavnms);
            }
            if (!isEmpty)
            {
                VacantWavid += WavidSpacing;
            }
            progressValue = progressMin + (progressMax - progressMin) * 0.80;



            // デバッグ用BMS書き出し（実際に使われた試しはない）
            /*
            nw2 = new NameWaves();
            String text_renamer2 = nw2.AllNoteToName(1,
                WavFileName_Prefix + BMSTrackName + WavFileName_In, ".wav",
                WavFileName_Prefix + BMSTrackName + WavFileName_Out, ".wav",
                WavFileName_Prefix + BMSTrackName + WavFileName_Bms, ".wav", out outInArray,
                isRedMode, isPurpleMode, isRedMode ? mi3.ntm : mi3.nta);
                //false, mi3.nta);
            
            bp2 = new BMSPlacement();
            text[6] += bp2.Process(ref vacant_wi, ref vacant_ch, 1, 200,
                isRedMode, isPurpleMode,
                mi3.nta, mi3.ntm, nw2.wavnms);
            progressValue = progressMin + (progressMax - progressMin) * 1.00;
            */

            if (isChordMode)
            {
                text[8] += (TrackIndex) + "\t" + (isRedMode ? mi3.ntmChordList.Count : mi3.ntaChord.Count) + "\t" + mi3.ntmChordList.Count + "\t" + MidiTrackName;
            }
            else
            {
                text[8] += (TrackIndex) + "\t" + (isRedMode ? mi3.ntm.Count : mi3.nta.Count) + "\t" + mi3.ntm.Count + "\t" + MidiTrackName;
            }

            return 0;
        }

    }
}
