using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace Mid2BMS
{
    /// <summary>
    /// 2007年6月から8月にかけて作成されました
    /// 2GBを超えるWavファイルには対応していません。ファイルを分割してください。
    /// このプログラムフローが複雑すぎて読みたくない
    /// </summary>
    class WaveSplitter : IWaveSplitter
    {
        /// <summary>
        /// 10サンプルのフェードインをします。
        /// </summary>
        bool ApplyFadeIn = true;
        bool ApplyTailCut = true;

        public double ThresholdInDB { get; set; }   // tailcut時のthreshold
        public int FadeInSamples { get; set; }
        public int FadeOutSamples { get; set; }

        public double SilenceThreshold { get; set; }
        public double SilenceTime { get; set; } // seconds
        bool ReadOptionsFromFile = false;

        byte[] HeaderData;
        int wavindex = 0, nsLen = 0, siLen = 0;
        bool LoopEsc = false, LoopChk = false, Tokken = false;

        int LastSampleReadCount = 0;
        int? ThisSampleReadCount = 0;

        public WaveSplitter()
        {
            ThresholdInDB = -60;  // tailcut時のthreshold
            FadeInSamples = 10;
            FadeOutSamples = 10;
            SilenceThreshold = -40;
            SilenceTime = 0.75;
        }

        public int Process(
            String[][] WaveSplitter_Text, String[][] WaveRenamer_Text, out String[][] RenameResults,
            String InputWaveFilePath, String IndexedWaveFilePath, String RenamedWaveFilePath,
            ref double progressValue, double progressMin, double progressMax)
        {
            List<String[]> renameResults = new List<String[]>();

            BinaryWriter w = null;  // C#に移植するときにフィールドに置いたら怒られた
            BinaryReader r2 = null;

            double progressMin2;
            double progressMax2;
            int inputfilesize;
            int readbytes;

            int renameNames_index1 = 0;
            int renameNames_index2 = 3;
            
            // 入力ファイルごとにループ
            for (int trackId = 0; trackId < WaveSplitter_Text.Length; trackId++)
            {  // ここではomniを使う場合 StdIn.Length == 1;

                progressMin2 = progressMin + (progressMax - progressMin) * trackId / WaveSplitter_Text.Length;
                progressMax2 = progressMin + (progressMax - progressMin) * (trackId + 0.99) / WaveSplitter_Text.Length;

                String INPUT_FILE_NAME1 = InputWaveFilePath + WaveSplitter_Text[trackId][0];

                if (!File.Exists(INPUT_FILE_NAME1))
                {
                    MessageBox.Show("ファイル\"" + INPUT_FILE_NAME1 + "\"が見つかりません。wavesplitter_input.txtに誤りがありませんか？" );
                    continue;
                }
                System.IO.FileInfo fii = new System.IO.FileInfo(INPUT_FILE_NAME1);
                inputfilesize = (int)fii.Length;  //ファイルのサイズを取得
                readbytes = 0;

                using (var r = new WaveFileReaderWithSilence(File.Open(INPUT_FILE_NAME1, FileMode.Open, FileAccess.Read)))
                {
                    wavindex = 0; nsLen = 0; siLen = 0;
                    LoopEsc = false; LoopChk = false; Tokken = false;
                    LastSampleReadCount = 0;
                    ThisSampleReadCount = 0;

                    String OUTPUT_FILE_NAME1 = WaveSplitter_Text[trackId][1];
                    String OUTPUT_FILE_NAME2 = WaveSplitter_Text[trackId][2];
                    int NOSOUND_LEVEL = (int)(32768 * Math.Pow(10, SilenceThreshold / 20));
                    int SILENCE_LEVEL = (int)(32768 * Math.Pow(10, SilenceThreshold / 20));
                    int NOSOUND_SAMPLES = (int)(SilenceTime * 44100);
                    int SILENCE_SAMPLES = (int)(SilenceTime * 44100);
                    if (ReadOptionsFromFile)
                    {
                        NOSOUND_LEVEL = Convert.ToInt32(WaveSplitter_Text[trackId][3]);//1025;
                        SILENCE_LEVEL = Convert.ToInt32(WaveSplitter_Text[trackId][4]);//1024;
                        NOSOUND_SAMPLES = Convert.ToInt32(WaveSplitter_Text[trackId][5]);//33075;
                        SILENCE_SAMPLES = Convert.ToInt32(WaveSplitter_Text[trackId][6]);//33075;
                    }


                    HeaderData = new byte[44] {
                        (byte)'R',(byte)'I',(byte)'F',(byte)'F',
                        0, 0, 0, 0,
                        (byte)'W',(byte)'A',(byte)'V',(byte)'E',
                        (byte)'f',(byte)'m',(byte)'t',(byte)' ',
                        16, 0, 0, 0,
                        1, 0, 2, 0,
                        0x44, 0xAC, 0x00, 0x00,
                        0x10, 0xB1, 0x02, 0x00,
                        0x04, 0x00, 0x10, 0x00,
                        (byte)'d',(byte)'a',(byte)'t',(byte)'a',
                        0, 0, 0, 0,
                    };
                    /*{
                        // read header

                        if(BitConverter.ToUInt32(r.ReadBytes(4), 0) != 0x46464952u)  // RIFF
                            throw new Exception("waveファイルのヘッダが誤っている可能性があります");
                        r.ReadBytes(4);  // filesize - 8

                        if (BitConverter.ToUInt32(r.ReadBytes(4), 0) != 0x45564157u)  // WAVE
                            throw new Exception("waveファイルのヘッダが誤っている可能性があります");

                        while (true)
                        {
                            if (BitConverter.ToUInt32(r.ReadBytes(4), 0) != 0x61746164u) break; // data

                            // data以外
                            // というか、リトルエンディアンじゃないアーキテクチャだと死ぬのでは？
                            int chunksize = (int)BitConverter.ToUInt32(r.ReadBytes(4), 0);
                            if (chunksize > 0xFFFFFF) throw new Exception("・・・ん？");
                            r.ReadBytes(chunksize);
                        }
                        // data
                        r.ReadBytes(4);  // dataチャンクのバイト数
                    }*/
                    readbytes += 44;  // バグっぽいですが
                    progressValue = progressMin2 + (progressMax2 - progressMin2) * (double)readbytes / (double)inputfilesize;

                    String nm0 = OUTPUT_FILE_NAME1 + String.Format("{0:D5}", wavindex++) + OUTPUT_FILE_NAME2;
                    String nm = IndexedWaveFilePath + nm0;
                    w = new BinaryWriter(File.Open(nm, FileMode.Create, FileAccess.Write));

                    w.Write(HeaderData);

                    int indt1, indt2, inLevel;
                    while (true)
                    {
                        //try
                        //{
                            float indt1f, indt2f;
                            if(!r.ReadSample(out indt1f)) break;  // ステレオだよ！
                            if(!r.ReadSample(out indt2f)) break;  // ステレオだよ！
                            indt1 = (int)(indt1f * 32768);
                            indt2 = (int)(indt2f * 32768);  // コードが汚い。やり直し。
                            readbytes += 4;
                            progressValue = progressMin2 + (progressMax2 - progressMin2) * (double)readbytes / (double)inputfilesize;
                        //}
                        //catch (EndOfStreamException)
                        //{
                            // このやりかたは良くない
                        //    break;
                        //}


                        inLevel = (((indt1 < 0) ? -indt1 : indt1) + ((indt2 < 0) ? -indt2 : indt2)) / 2;
                        //if (inLevel < 0) inLevel = -inLevel;

                        //for(i=4;i;i-=2) {// 符号付16ビット＊２
                        // // やや条件に不平等があるがキニシナイ
                        //inval= ((int)indt[4-i])+(((int)(indt[5-i]&127))<<8)-(((int)(indt[5-i]&128))<<8);

                        if (inLevel <= SILENCE_LEVEL)
                        {  // 無音判定
                            if (siLen >= SILENCE_SAMPLES) { continue; }
                            else if (siLen + 1 >= SILENCE_SAMPLES) { LoopEsc = true; siLen++; }  // wav書き出しストップフラグ
                            else { siLen++; }
                        }
                        else if (inLevel <= NOSOUND_LEVEL)
                        {  // 区切り可能
                            Tokken = false;
                            siLen = 0;
                            if (nsLen >= NOSOUND_SAMPLES) { }
                            else if (nsLen + 1 >= NOSOUND_SAMPLES) { nsLen++; }
                            else { nsLen++; }

                        }
                        else
                        {  // 音あり
                            if ((!Tokken) && (siLen < SILENCE_SAMPLES) && (nsLen >= NOSOUND_SAMPLES)) { LoopChk = true; }  // ここで区切るフラグ
                            Tokken = false;
                            nsLen = 0;
                            siLen = 0;
                        }
                        //}


                        if (LoopEsc || LoopChk)
                        {  // 同時には立たないはず
                            LoopChk = false;

                            // ★★★ファイルの終了処理
                            w.Close();
                            System.IO.FileInfo fi = new System.IO.FileInfo(nm);
                            int filesize = (int)fi.Length;  //ファイルのサイズを取得
                            r2 = new BinaryReader(File.Open(nm, FileMode.Open, FileAccess.Read));
                            byte[] wholedata = r2.ReadBytes(filesize);
                            byte[] trimmeddata = null;
                            r2.Close();
                            byte[] intdata1 = new byte[2];
                            byte[] intdata2 = new byte[2];
                            int invalue1;
                            int invalue2;
                            if (ApplyTailCut)
                            {
                                int j;
                                short threshold = (short)(Math.Pow(10.0, ThresholdInDB / 20.0) * 32768.0);
                                for (j = ((filesize - 44) / 4) - 1; j >= 0; j--)
                                {
                                    Array.Copy(wholedata, 44 + 4 * j, intdata1, 0, 2);
                                    Array.Copy(wholedata, 46 + 4 * j, intdata2, 0, 2);
                                    invalue1 = (int)BitConverter.ToInt16(intdata1, 0);
                                    invalue2 = (int)BitConverter.ToInt16(intdata2, 0);
                                    if ((invalue1 + invalue2) / 2 >= threshold)
                                        break;
                                }
                                // now I need j-th sample, so now new_filesize is ((j+1)*4+44)
                                filesize = Math.Max((j + 1), 10) * 4 + 44;
                                trimmeddata = new byte[filesize];
                                Array.Copy(wholedata, trimmeddata, filesize);
                                wholedata = trimmeddata;
                                System.IO.File.Delete(nm);  // deleteは使わないって約束したじゃない！
                            }
                            if (ApplyFadeIn)
                            {
                                for (int j = 0; j < FadeInSamples; j++)
                                {
                                    Array.Copy(wholedata, 44 + 4 * j, intdata1, 0, 2);
                                    Array.Copy(wholedata, 46 + 4 * j, intdata2, 0, 2);
                                    intdata1 = BitConverter.GetBytes((short)(((int)BitConverter.ToInt16(intdata1, 0)) * (j + 1) / FadeInSamples));
                                    intdata2 = BitConverter.GetBytes((short)(((int)BitConverter.ToInt16(intdata2, 0)) * (j + 1) / FadeInSamples));
                                    Array.Copy(intdata1, 0, wholedata, 44 + 4 * j, 2);
                                    Array.Copy(intdata2, 0, wholedata, 46 + 4 * j, 2);
                                }
                                for (int j = 0; j < FadeOutSamples; j++)
                                {
                                    Array.Copy(wholedata, filesize - 4 * j - 4, intdata1, 0, 2);
                                    Array.Copy(wholedata, filesize - 4 * j - 2, intdata2, 0, 2);
                                    intdata1 = BitConverter.GetBytes((short)(((int)BitConverter.ToInt16(intdata1, 0)) * (j + 1) / FadeOutSamples));
                                    intdata2 = BitConverter.GetBytes((short)(((int)BitConverter.ToInt16(intdata2, 0)) * (j + 1) / FadeOutSamples));
                                    Array.Copy(intdata1, 0, wholedata, filesize - 4 * j - 4, 2);
                                    Array.Copy(intdata2, 0, wholedata, filesize - 4 * j - 2, 2);
                                }
                            }
                            byte[] sizedata1 = BitConverter.GetBytes(filesize - 8);
                            byte[] sizedata2 = BitConverter.GetBytes(filesize - 44);
                            Array.Copy(sizedata1, 0, wholedata, 4, 4);
                            Array.Copy(sizedata2, 0, wholedata, 40, 4);

                            w = new BinaryWriter(File.Open(nm, FileMode.Create, FileAccess.Write));
                            w.Write(wholedata);
                            w.Close();

                            // てかここらへんのコードの可読性の低さがやばい
                            while (renameNames_index2 >= WaveRenamer_Text[renameNames_index1].Length)
                            {
                                renameNames_index2 = 3;
                                renameNames_index1++;
                                if (renameNames_index1 >= WaveRenamer_Text.Length)
                                {
                                    RenameResults = renameResults.ToArray();
                                    return 0;
                                }
                            }

                            // 存在しなくても例外はスローされない
                            System.IO.File.Delete(RenamedWaveFilePath + WaveRenamer_Text[renameNames_index1][renameNames_index2]);
                            if (wavindex - 1 >= Convert.ToInt32(WaveRenamer_Text[renameNames_index1][2]))
                            {
                                File.Copy(nm, RenamedWaveFilePath + WaveRenamer_Text[renameNames_index1][renameNames_index2]);
                                renameResults.Add(new String[] {
                                    String.Format("{0, 12}", ThisSampleReadCount),
                                    String.Format("{0, 12}", ThisSampleReadCount - LastSampleReadCount),
                                    nm0,
                                    WaveRenamer_Text[renameNames_index1][renameNames_index2] });
                                renameNames_index2++;
                            }
                            LastSampleReadCount = ThisSampleReadCount ?? 0;
                            ThisSampleReadCount = null;
                            while (renameNames_index2 >= WaveRenamer_Text[renameNames_index1].Length)
                            {
                                renameNames_index2 = 3;
                                renameNames_index1++;
                                if (renameNames_index1 >= WaveRenamer_Text.Length)
                                {
                                    RenameResults = renameResults.ToArray();
                                    return 0;
                                }
                            }
                            // ★★★ファイルの終了処理ここまで

                            nm0 = OUTPUT_FILE_NAME1 + String.Format("{0:D5}", wavindex++) + OUTPUT_FILE_NAME2;
                            nm = IndexedWaveFilePath + nm0;
                            w = new BinaryWriter(File.Open(nm, FileMode.Create, FileAccess.Write));
                            w.Write(HeaderData);


                            if (LoopEsc)
                            {
                                LoopEsc = false;
                                Tokken = true;
                            }
                        }
                        if (Tokken) continue;

                        ThisSampleReadCount = ThisSampleReadCount ?? ((readbytes - 44) / 4);  // 更新は、そのサンプルが採用されなくても行わなくちゃ。
                        w.Write((short)indt1);
                        w.Write((short)indt2); // 書き出しは4byte// 8bitですが何か？

                    }
                    w.Close();

                }


            }

            RenameResults = renameResults.ToArray();
            return 0;
        }

    }
}
