﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Mid2BMS
{
    class MyForm
    {
        public String PathBase = ":";  // 円記号で終わらなければならない
        public String WavePathBase = ":";
        public String RenamedPathBase = ":";
        public String FileName_MidiFile = "";  //@"stdread.mid";
        public String FileName_WaveFile = "";  //@"";
        public String FileName_BMSFile = "";  //@"";

        //***********************************************************************************
        //*** ハッシュとそのチェックに関する記述
        //***********************************************************************************

        private String GetHash(String fileName)
        {
            System.IO.FileStream fs = new System.IO.FileStream(
                fileName,
                System.IO.FileMode.Open,
                System.IO.FileAccess.Read,
                System.IO.FileShare.Read);
            var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            //ハッシュ値を計算する
            byte[] bs = md5.ComputeHash(fs);
            fs.Close();

            //byte型配列を16進数の文字列に変換
            StringBuilder result = new StringBuilder();
            foreach (byte b in bs)
            {
                result.Append(b.ToString("x2"));
            }
            return result.ToString();
        }

        String[] Mid2BMS_HashTesteeFileNames = new String[] {
                @"text0_stdout_part1.mml",
                @"text1_midtable_debug.txt",
                @"text2_mmlrenew_debug.txt",
                @"text3_tanon.mml",
                @"text4_renamer.txt",
                @"text5_renamer_array.txt",
                @"text6_bms.txt",
                @"text7_bms_for_debug.txt",
                @"text8_errorlog_debug.txt",
                @"text9_trackname_csv.txt",
                @"wavesplitter_input.txt",
        };
        String[] WaveSplit_HashTesteeFileNames = new String[] {
                @"wavesplitter_renameresult.txt",
        };

        private bool Mid2BMS_CheckHash()
        {
            return CheckHash(Mid2BMS_HashTesteeFileNames, @"hashvalues1.txt");
        }
        private int Mid2BMS_WriteHash()
        {
            if (false)
            {
                return WriteHash(Mid2BMS_HashTesteeFileNames, @"hashvalues1.txt");
            }
            return 0;
        }
        private bool WaveSplit_CheckHash()
        {
            return CheckHash(WaveSplit_HashTesteeFileNames, @"hashvalues2.txt");
        }
        private int WaveSplit_WriteHash()
        {
            if (false)
            {
                return WriteHash(WaveSplit_HashTesteeFileNames, @"hashvalues2.txt");
            }
            return 0;
        }

        /// <summary>
        /// ファイルの書き込み処理を中断する場合、falseが返ります。
        /// 処理を続行して良い場合、trueが返ります。
        /// </summary>
        private bool CheckHash(String[] s1, String s2)
        {
            if (!System.IO.File.Exists(PathBase + s2)) return true;
            String[] HashValues = System.IO.File.ReadAllLines(PathBase + s2);
            for (int i = 0; i < s1.Length && i < HashValues.Length; i++)
            {
                if (!File.Exists(PathBase + s1[i])) continue;
                if (GetHash(PathBase + s1[i]) != HashValues[i])
                {
                    DialogResult dr = MessageBox.Show(
                        "ファイル " + s1[i] + "が変更されています。\r\n処理によっては上書きされることがあります。続行しますか？",
                        "確認", MessageBoxButtons.YesNoCancel);
                    if (dr == DialogResult.Yes) continue;
                    return false;
                }
            }
            return true;
        }
        private int WriteHash(String[] s1, String s2)
        {
            using (StreamWriter w = new StreamWriter(neu.IFileStream(PathBase + s2, FileMode.Create, FileAccess.Write), HatoEnc.Encoding))
            {
                for (int i = 0; i < s1.Length; i++)
                {
                    if (!File.Exists(PathBase + s1[i]))
                    {
                        w.WriteLine();
                        continue;
                    }
                    w.WriteLine(GetHash(PathBase + s1[i]));
                }
            }

            return 0;
        }

        //***********************************************************************************
        //*** Mid2BMS に関する記述
        //***********************************************************************************
        /// <summary>
        /// midiファイルからbmsファイル等の音切りに必要なファイルを生成します。
        /// [1] Mid2BMS タブの Process ボタンに相当します。
        /// </summary>
        /// <param name="isRedMode">red modeであるかどうかを表すbool値です。</param>
        /// <param name="isPurpleMode">purple modeであるかどうかを表すbool値です。</param>
        /// <param name="createExFiles">テンポチェンジ定義BMS等の追加のファイルを生成するかどうかを示すbool値です。</param>
        /// <param name="VacantWavid">定義を開始するWAV定義番号です。</param>
        /// <param name="DefaultVacantBMSChannelIdx">配置を開始するレーンを表す、static string[] Mid2BMS.BMSPlacement.ChannelTemplate の添字です。</param>
        /// <param name="LookAtInstrumentName">キー音ファイル名に Instrument Name を用いるか、Track Name を用いるかを表すフラグの、各トラックに対する値の配列です。</param>
        /// <param name="margintime_beats">キー音とキー音の間に設けられる無音時間を、拍で表した長さです。</param>
        /// <param name="WavidSpacing">トラックとトラックの間に設けられる、キー音が定義されないWAV定義の数です。通常は0です。</param>
        /// <param name="trackCsv">midiファイルを解析した結果を格納するcsvを表す文字列です。</param>
        /// <param name="MidiTrackNames">[nullable] midiファイルを解析した結果得られたTrack Nameを格納する配列です。ただしnull以外が与えられた場合は、それに従ってキー音にファイル名を与えます。</param>
        /// <param name="MidiInstrumentNames">[nullable] midiファイルを解析した結果得られたInstrument Nameを格納する配列です。</param>
        /// <param name="isDrumsList">[nullable] 各音程に対し1つのBMSレーンを割り当てるかどうかを示すフラグの配列です。フラグのデフォルト値はfalseです。</param>
        /// <param name="ignoreList">[nullable] トラックを無視するかどうかを示すフラグの配列です。フラグのデフォルト値はfalseです。</param>
        /// <param name="isChordList">[nullable] 同時に発音された音を1つのキー音にまとめるかどうかを示すフラグの配列です。フラグのデフォルト値はfalseです。</param>
        /// <param name="isXChainList">[nullable] RedModeとシーケンスレイヤーの両方が選択されている場合に、サイドチェイントリガノーツとして扱うかどうかを示すフラグの配列です。フラグのデフォルト値はfalseです。</param>
        /// <param name="isOneShotList">[nullable] RedModeとシーケンスレイヤーの両方が選択されている場合に、オートメーションをグローバルとして扱うかどうかを示すフラグの配列です。フラグのデフォルト値はtrueです。</param>
        /// <param name="sequenceLayer">それぞれのトラックが重ならないように単音midiを時間的にずらすかどうかを示すフラグです。デフォルト値はfalseです。</param>
        /// <param name="ProgressBarValue">プログレスバーに対して値を反映させるための変数です。</param>
        /// <param name="ProgressBarFinished">プログレスバーの増加が終了したかどうかを示すフラグです。</param>
        public void Mid2BMS_Process(
            bool isRedMode, bool isPurpleMode, bool createExFiles, ref int VacantWavid, ref int DefaultVacantBMSChannelIdx,
            bool LookAtInstrumentName, String margintime_beats, int WavidSpacing,
            out String trackCsv, ref List<String> MidiTrackNames, out List<String> MidiInstrumentNames,
            List<bool> isDrumsList, List<bool> ignoreList, List<bool> isChordList, List<bool> isXChainList, List<bool> isOneShotList, bool sequenceLayer,
            int newTimebase, int velocityStep,
            ref double ProgressBarValue, ref bool ProgressBarFinished)
        {
            #region ファイルの更新チェック
            if (!Mid2BMS_CheckHash())  // TODO: 不要なコードの削除or修正
            {
                // 操作をキャンセルする
                ProgressBarValue = 1.00;
                ProgressBarFinished = true;
                trackCsv = null;
                MidiTrackNames = null;
                MidiInstrumentNames = null;
                return;
            }
            #endregion

            Func<Stream> quantizedMidiStreamGenerator;

            #region Midiのクオンタイズ
            if (newTimebase > 0)
            {
                var quantizedMidiWriteStream = new MemoryStream();

                MyForm.ChangeMidiTimebase(
                    neu.IFileStream(this.PathBase + this.FileName_MidiFile, FileMode.Open, FileAccess.Read),
                    quantizedMidiWriteStream,
                    newTimebase);

                quantizedMidiWriteStream.Close();

                var mbuf = quantizedMidiWriteStream.GetBuffer();

                quantizedMidiStreamGenerator = () => new MemoryStream(mbuf);

                if (createExFiles)
                {
                    File.WriteAllBytes(this.PathBase + "midiinput_timequantizedstream.mid", quantizedMidiWriteStream.GetBuffer());
                }
            }
            else
            {
                quantizedMidiStreamGenerator = () => neu.IFileStream(this.PathBase + this.FileName_MidiFile, FileMode.Open, FileAccess.Read);
            }
            #endregion

            #region ベロシティの量子化
            if (velocityStep >= 2)
            {
                var quantizedMidiWriteStream2 = new MemoryStream();

                MyForm.QuantizeVelocity(
                    quantizedMidiStreamGenerator(),
                    quantizedMidiWriteStream2,
                    velocityStep);

                quantizedMidiWriteStream2.Close();

                var mbuf = quantizedMidiWriteStream2.GetBuffer();

                quantizedMidiStreamGenerator = () => new MemoryStream(mbuf);  // quantizedMidiStreamGenerator に上書き

                if (createExFiles)
                {
                    File.WriteAllBytes(this.PathBase + "midiinput_velocityquantizedstream.mid", quantizedMidiWriteStream2.GetBuffer());
                }
            }
            else
            {
                // 何もしない
            }
            #endregion

            #region timebase及びmidi_bpmの取得、及び重複ノーツのチェック、テンポチェンジBMSの作成
            int timebase;
            decimal midi_bpm;
            {
                Stream rf = quantizedMidiStreamGenerator();
                MidiStruct ms = new MidiStruct(rf);
                if (ms.resolution == null) throw new Exception("resolutionがnull #とは");
                timebase = ms.resolution ?? 480;
                double uspb = ms.InitalUSecondPerBeat ?? (60.0 * 1.0e6 / 120.0);
                midi_bpm = 0.001m * (decimal)Math.Round(1000.0 * 60.0 * 1.0e6 / uspb);  // そこまで0.001単位にこだわる必要があったのかどうか

                bool messageShown = false;

                //double newResolution = 4 * 24;  // BMS分解能、マジックナンバー感ある。16分音符を24個に分解出来る。
                // ↑これは恐らく、テンポチェンジがMIDIファイルに多すぎた場合の対処だと思います。
                // めんどくさいのでとりあえずこのままで（要修正）

                double newResolution = timebase;

                for (int trackindex = 0; trackindex < ms.tracks.Count; trackindex++ )
                {
                    var dict = new HashSet<Tuple<int, long>>();  // Tuple<ノート番号, 発音時間>

                    foreach (var _me in ms.tracks[trackindex])
                    {
                        if (_me is MidiEventNote)
                        {
                            MidiEventNote me = _me as MidiEventNote;
                            if (me.n > 127)
                            {
                                throw new Exception("いや、逆にそれはおかしい");
                            }
                            Tuple<int, long> serialized = Tuple.Create(
                                me.n,
                                (long)Math.Round((double)me.tick * newResolution / (double)ms.resolution));  // このnewResolutionって何ですか・・・

                            if (!messageShown && dict.Contains(serialized))
                            {
                                // todo: 表記が分かりづらいので修正

                                if (DialogResult.Yes == MessageBox.Show(
                                    "トラック番号" + trackindex + ", " 
                                    + (1 + me.tick / (4 * (int)ms.resolution))  + "小節, "
                                    + "ノート番号" + me.n + "に重複した音符が存在します。\n"
                                    + "このまま続行すると、変換結果が正常とならない可能性があります。(特にisDrumsを選択した場合)\n"
                                    + "処理を中断しますか。(Click \"Yes\" to Abort)\n"
                                    + "(注：Mid2BMSは、Midiチャンネルには対応していません)",
                                     "Confirm to continue", MessageBoxButtons.YesNo))
                                {
                                    throw new Exception("ユーザーの指示により処理を中断しました。");
                                }
                                messageShown = true;
                            }
                            dict.Add(serialized);
                        }
                    }

                    // トラック00(AAAAA), 00000小節, 位置0/0, ノート番号00 に重複したノートが存在します。
                    // 処理を中断しますか。(Click \"Yes\" to Abort)
                    // (注：Mid2BMSは、Midiチャンネルには対応していません)
                }
                
                if (createExFiles) // テンポチェンジBMSの作成
                {
                    var tempochanges = ms.tracks[0].Where(x => x is MidiEventMeta && ((MidiEventMeta)x).id == 0x51);
                    StringSuruyatu tempobms = "";
                    int bpmid = 1;  // 0じゃダメだよ
                    List<ArrTuple<Frac, int>> tempos = new List<ArrTuple<Frac, int>>();
                    int lasttick = -99999999;
                    int TEMPOCHANGE_RESOLUTION = 32;  // 拍
                    int PRECISION = 1000;  // BPMの精度
                    foreach (var tempoev in tempochanges)
                    {
                        if (bpmid >= 36 * 36) break;
                        if (tempoev.tick - lasttick < (ms.resolution ?? 480) * 4 / TEMPOCHANGE_RESOLUTION) continue;
                        lasttick = tempoev.tick;
                        // += としてはダメ！
                        tempobms = tempobms +
                            "#BPM" + BMSParser.IntToHex36Upper(bpmid) + " "
                            + Math.Round((60000000.0 / ((MidiEventMeta)tempoev).val) * PRECISION) / (double)PRECISION + "\r\n";
                        tempos.Add(Arr.ay(new Frac((long)(tempoev.tick * 192.0 * 0.25 / (ms.resolution ?? 480) + 192.0), 192), bpmid)); // 面倒なので分解能は192

                        bpmid++;
                    }
                    tempobms += "\r\n";
                    tempobms += (new BMSPlacement()).haichiTuplesAsBMS("08", 0, 200, 192, tempos);
                    FileIO.WriteAllText(this.PathBase + "text6_tempochangebms.txt", tempobms);
                }
            }
            #endregion

            #region midiファイルからmmlへの変換、Track Name、Instrument Nameの取得
            Mid2mml m2m = new Mid2mml();

            List<String> MMLs = new List<String>();
            List<String> MidiTrackIdentifier = MidiTrackNames;  // null以外が与えられた場合はそれに従う

            MidiInstrumentNames = new List<String>();   // 無駄な初期化感
            MidiTrackNames = new List<String>();

            List<bool> isEmptyList;

            m2m.Process(quantizedMidiStreamGenerator(), PathBase + @"text0_stdout_part1.txt", out MMLs,
                out MidiTrackNames, out MidiInstrumentNames, createExFiles, ref ProgressBarValue, 0.00, 0.10);

            if (MidiTrackIdentifier == null)
            {
                // nullが与えられた場合はmidiファイルから読み込んだデータを用いる
                MidiTrackIdentifier = new List<String>(LookAtInstrumentName ? MidiInstrumentNames : MidiTrackNames);  // ちゃんとCloneする
                for (int i = 0; i < MidiTrackIdentifier.Count; i++)
                {
                    if (MidiTrackIdentifier[i] == "") MidiTrackIdentifier[i] = "untitled " + i;  // コンダクタートラックに関する処理をどうするか
                }
            }
            else
            {
                // null以外が与えられた場合はそれに従う
            }
            #endregion

            #region 何か書き出しっぽいの
            String[][] TextFormatOut = new String[MMLs.Count][];
            for (int i = 0; i < MMLs.Count; i++)
            {
                TextFormatOut[i] = new String[2];
                TextFormatOut[i][0] = MidiTrackIdentifier[i];
                TextFormatOut[i][1] = MMLs[i];
            }
            String TextFormatOutStr = TextTransaction.JoinString(TextFormatOut,
                "\r\n========================================\r\n",
                "\r\n########################################\r\n");

            if (createExFiles)
            {
                FileIO.WriteAllText(PathBase + @"text0_stdout_part1_array.txt", TextFormatOutStr);
            }
            #endregion

            #region MMLから各出力ファイルへの変換（多分）
            MelodyWalker mw = new MelodyWalker();
            mw.VacantBMSChannelIdx = DefaultVacantBMSChannelIdx;
            mw.WavidSpacing = WavidSpacing;
            mw.MultiProcess(MMLs, MidiTrackIdentifier, isDrumsList, ignoreList, isChordList, isXChainList, isOneShotList, sequenceLayer, PathBase,
                isRedMode, isPurpleMode, createExFiles, ref VacantWavid, timebase, margintime_beats, out trackCsv, out isEmptyList, midi_bpm,
                ref ProgressBarValue, 0.10, 1.00);
            #endregion

            #region トラックリストファイルの作成。WaveSplitterで使用する（？？？？？？？？？？？？）
            {
                String Text1;
                SoundRunner sr = new SoundRunner();
                List<String> validTrackIds = new List<String>();
                for (int ii = 0; ii < MidiTrackIdentifier.Count; ii++)
                {
                    if (!isEmptyList[ii])
                    {
                        validTrackIds.Add(MidiTrackIdentifier[ii]);
                    }
                }

                if (createExFiles)
                {
                    sr.CreateText(validTrackIds.ToArray(), out Text1);
                    FileIO.WriteAllText(PathBase + @"wavesplitter_input.txt", Text1);
                }

                //FileStreamFactory.WriteAllText(
                //    PathBase + @"wavesplitter_tracklist.txt",
                //    validTrackIds.Join("\r\n"));
            }
            #endregion

            #region RedModeの場合のMidi書き出し処理

            // RedModeの場合はmidiをSplitしたものを提出する
            // ignoreListをちゃんと見て！

            if (isRedMode)
            {
                MidiStruct ms2 = new MidiStruct(quantizedMidiStreamGenerator(), true);

                int margintime_beats_int = (int)Math.Ceiling(Convert.ToDouble(margintime_beats));
                MidiTrack.SPLIT_BEATS_INTERVAL = margintime_beats_int + 4;
                MidiTrack.SPLIT_BEATS_AUTOMATIONLEFT = 2;
                MidiTrack.SPLIT_BEATS_AUTOMATIONRIGHT = margintime_beats_int + 0;

                if (ignoreList != null)
                {
                    for (int trid = 0; trid < ignoreList.Count; trid++)
                    {
                        if (ignoreList[trid])
                        {
                            ms2.tracks[trid] = new MidiTrack(ms2.tracks[trid].Where(x => !(x is MidiEventNote)));
                        }
                    }
                }

                if (!sequenceLayer)  // シーケンスレイヤーとして書き出す（テンポチェンジを含む場合はチェックしてください）
                {
                    for (int i = 1; i < ms2.tracks.Count; i++)  // 1から処理
                    {
                        bool isChordMode = (isChordList == null ? false : isChordList[i]);
                        ms2.tracks[i] = ms2.tracks[i].SplitNotes(ms2, isChordMode);  // コンダクタートラックはそのままにする(主にテンポ保持のため)
                    }
                }
                else
                {
                    // ノート数が5000を超える場合は中断しても良いと思う(でもノート数よりオートメーションが極端に多いと問題の解決にならない)
                    // 小節数が9999を超える場合はさすがに中断しよう
                    try
                    {
                        var directsum = MidiTrack.DirectSum(ms2.tracks);
                        var splitted = MidiTrack.SplitNotes(directsum, ms2, isChordList, isXChainList);
                        ms2.tracks = MidiTrack.DirectDifference(splitted);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString());
                    }
                }

                ms2.Export(neu.IFileStream(PathBase + @"text3_tanon_smf_red.mid", FileMode.Create, FileAccess.Write), true);
            }
            #endregion

            Mid2BMS_WriteHash();

            ProgressBarValue = 1.00;
            ProgressBarFinished = true;

        }

        //***********************************************************************************
        //*** WaveSplit に関する記述
        //***********************************************************************************

        public void WaveSplit_Process(
            double tailcut_threshold, int fadeintime, int fadeouttime, double silence_time, bool inputFileIndicated,
            bool renamingEnabled, String renamingFilename, float[] SilenceLevelsSquare,
            ref double ProgressBarValue, ref bool ProgressBarFinished)
        {
            if (!WaveSplit_CheckHash())
            {
                ProgressBarValue = 1.00;
                ProgressBarFinished = true;
                return;
            }


            //SoundRunner sr = new SoundRunner();

            String[][] WaveSplitter_Text;
            IEnumerable<String[]> WaveRenamer_Text;
            //sr.CreateText(MidiTrackNames.ToArray(), out Text1, out Text2, out Text3);
            int RenameRequiredFilesCount;

            //sr.CreateText(new String[] { "omni" }, out Text2);

            if (renamingEnabled)
            {
                WaveRenamer_Text = TextTransaction.SplitString(
                    FileIO.ReadAllText(PathBase + @"text5_renamer_array.txt"),
                    "\r\n", "//", StringSplitOptions.RemoveEmptyEntries);

                if (inputFileIndicated)
                {
                    WaveSplitter_Text = new[] { new[] { FileName_WaveFile } };
                }
                else
                {
                    WaveSplitter_Text = WaveRenamer_Text.Select(x => new[] { x[0] + x[1] }).ToArray();
                }

                RenameRequiredFilesCount = WaveRenamer_Text.Select(
                    x => x.Skip(3).Count(
                        y => !(y.Length >= 10 && y.Substring(0, 10) == "____dummy_")  // ダミーファイルはカウントに含めない
                    )
                ).Sum();
            }
            else
            {
                if (inputFileIndicated == false) throw new Exception("enableRenaming == false && inputFileIndicated == false の組み合わせは使用できません");
                
                WaveSplitter_Text = new[] { new[] { FileName_WaveFile } };
                WaveRenamer_Text = new LambdaEnumerable<String[]>(i => new[] { "ハッピー", "ハードコア", "1", String.Format(renamingFilename, (object)(i + 1)) });  // ボックス化を避けたい・・・？

                RenameRequiredFilesCount = -1;
            }

            //PerformanceTest pt = new PerformanceTest();
            //pt.PerformanceTest3();

            WaveSplitter2 ws = null;
            Directory.CreateDirectory(PathBase + @"renamed\");
            ws = new WaveSplitter2();
            ws.ThresholdInDB = tailcut_threshold;
            ws.FadeInSamples = fadeintime;
            ws.FadeOutSamples = fadeouttime;
            ws.SilenceTime = silence_time;
            int createdWavCount = ws.Process(
                WaveSplitter_Text, WaveRenamer_Text, 
                WavePathBase, PathBase + @"renamed\",
                SilenceLevelsSquare,
                ref ProgressBarValue, 0.00, 1.00);

            WaveSplit_WriteHash();

            ProgressBarValue = 1.00;
            ProgressBarFinished = true;

            if (RenameRequiredFilesCount == -1)
            {
                MessageBox.Show(
                    createdWavCount + " 個のwavを書き出しました。" + 
                    "もし、音切りに失敗していた場合は、「無音検出時間 (Silence Time)」の値を大きくしてみて下さい。",
                    "Split Result");
            }
            else
            {
                if (RenameRequiredFilesCount > createdWavCount)
                {
                    MessageBox.Show(
                        RenameRequiredFilesCount + " 個のwavを書き出さなければならないのに対し、" + createdWavCount + " 個のwavを書き出しました。" + 
                        "つまり、正しく音切り出来ていない可能性があります。" +
                        "その場合は、「無音検出時間 (Silence Time)」の値を大きくしてみて下さい。",
                        "Split Failed?");
                }
                else
                {
                    MessageBox.Show(
                        RenameRequiredFilesCount + " 個のwavを書き出さなければならないのに対し、" + createdWavCount + " 個のwavを書き出しました。" +
                        "もし、音切りに失敗していた場合は、「無音検出時間 (Silence Time)」の値を大きくしてみて下さい。",
                        "Renaming Result");
                }
            }
        }

        //***********************************************************************************
        //*** DupeDef に関する記述
        //***********************************************************************************

        // 誰かBMS界隈に詳しい英語の読める人、重複定義の英訳教えて
        public void DupeDef_Process(double intervaltime, int maxLayerCount, ref double ProgressBarValue, ref bool ProgressBarFinished)
        {
            /*if (!DupeDef_CheckHash())
            {
                ProgressBarValue = 1.00;
                ProgressBarFinished = true;
                return;
            }*/
            String[] text;
            String[] textNames;
            bool outputExtraFiles = false;

            DupeDefinition dd = new DupeDefinition();
            String bms = FileIO.ReadAllText(RenamedPathBase + FileName_BMSFile);  // @"renamed\text6_bms.bms"

            try
            {
                dd.Process(bms, intervaltime, RenamedPathBase, maxLayerCount, out textNames, out text, ref ProgressBarValue, 0.0, 1.0);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return;
            }

            if (outputExtraFiles)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    FileIO.WriteAllText(RenamedPathBase + textNames[i], text[i]);
                }
            }
            
            String newBmsPath = RenamedPathBase + Path.GetFileNameWithoutExtension(FileName_BMSFile) + "_multidefined.bms";
            FileIO.WriteAllText(newBmsPath, text[4]);


            //DupeDef_WriteHash();

            ProgressBarValue = 1.00;
            ProgressBarFinished = true;

            MessageBox.Show("\"" + newBmsPath + "\" にBMSを書き込みました。");
        }

        //***********************************************************************************
        //*** その他
        //***********************************************************************************

        public static void ChangeMidiTimebase(Stream inStream, Stream outStream, long newTimeBase)
        {
            MidiStruct ms = new MidiStruct(inStream, true);

            Debug.Assert(ms.resolution != null);
            long oldTimeBase = ms.resolution ?? 480;  // この480って必要？

            for (int i = 0; i < ms.tracks.Count; i++)
            {
                MidiTrack mt = ms.tracks[i];

                for (int j = 0; j < mt.Count; j++)
                {
                    // 切り捨てではなく四捨五入に修正
                    int tick_old = mt[j].tick;

                    mt[j].tick = (int)Math.Round((tick_old * newTimeBase) / (double)oldTimeBase);  // 切り捨て
                    if (mt[j] is MidiEventNote)
                    {
                        MidiEventNote me = (MidiEventNote)mt[j];
                        me.q = (int)Math.Round(((tick_old + me.q) * newTimeBase) / (double)oldTimeBase) - mt[j].tick;  // 切り捨て
                        me.q = Math.Max(1, me.q);  // ただし1以上
                    }
                }
            }

            ms.resolution = (int)newTimeBase;

            ms.Export(outStream, true);

            inStream.Close();
        }

        public static void QuantizeVelocity(Stream inStream, Stream outStream, int velocityStep)
        {
            if (velocityStep < 1) throw new Exception("Velocity Quantization Interval は 1以上である必要があります");

            Stream rf = inStream;

            MidiStruct ms = new MidiStruct(rf, true);

            foreach (MidiTrack mt in ms.tracks)
            {
                foreach (MidiEvent me_ in mt)
                {
                    MidiEventNote me = me_ as MidiEventNote;
                    if (me != null)
                    {
                        if (me.v >= 1)
                        {
                            me.v = ((int)Math.Round((double)me.v / (double)velocityStep)) * velocityStep;
                            if (me.v < 1) me.v = 1;
                            else if (me.v > 127) me.v = 127;
                        }
                    }
                }
            }

            ms.Export(outStream, true);

            rf.Close();
        }
    }
}
