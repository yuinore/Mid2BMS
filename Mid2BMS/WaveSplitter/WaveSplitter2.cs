using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace Mid2BMS
{
    /// <summary>
    /// 改良
    /// </summary>
    class WaveSplitter2 // : IWaveSplitter
    {
        bool ApplyFadeIn = true;
        bool ApplyTailCut = true;

        public double ThresholdInDB { get; set; }  // tailcut時のthreshold

        //readonly float[] SilenceLevelsSquare = new float[] { 0.0001f, 0.00001f, 0.000001f, 0.0000001f };  // -40, -50, -60, -70dB
        //float[] SilenceLevelsSquare = new float[] { 0.01f, 0.001f, 0.0001f, 0.00001f, 0.000001f, 0.0000001f };  // -20, -30, -40, -50, -60, -70dB
        public double SilenceTime { get; set; }  // seconds

        public int FadeInSamples { get; set; }
        public int FadeOutSamples { get; set; }
        
        public WaveSplitter2()
        {
            ThresholdInDB = -60;  // tailcut時のthreshold
            FadeInSamples = 10;
            FadeOutSamples = 10;
            SilenceTime = 0.75;
        }

        public int Process(
            String[][] WaveSplitter_Text, IEnumerable<String[]> WaveRenamer_Text,
            String InputWaveFilePath, String RenamedWaveFilePath, float[] SilenceLevelsSquare,
            ref double progressValue, double progressMin, double progressMax)
        {
            double progressMin2;
            double progressMax2;

            //int renameNames_index1 = 0;
            int renameNames_index2 = 3;  // 3から始まる
            IEnumerator<String[]> WaveRenamer_Enumerator = WaveRenamer_Text.GetEnumerator();
            WaveRenamer_Enumerator.MoveNext();

            int createdWavCount = 0;

            List<float>[] buffer = new[] { new List<float>(), new List<float>() };  // 出力バッファ

            for (int trackId = 0; trackId < WaveSplitter_Text.Length; trackId++)
            {
                progressMin2 = progressMin + (progressMax - progressMin) * trackId / WaveSplitter_Text.Length;
                progressMax2 = progressMin + (progressMax - progressMin) * (trackId + 0.999) / WaveSplitter_Text.Length;

                int wavindex = 0;  // wavindex==0のときはファイルを書き出さない
                int readbytes = 0;
                buffer[0].Clear();  // バッファもクリア
                buffer[1].Clear();
                int[] assert = new int[SilenceLevelsSquare.Length];  // アサートもリセット

                String inputFilename = InputWaveFilePath + WaveSplitter_Text[trackId][0];

                using (var reader = new WaveFileReaderWithSilence(neu.IFileStream(inputFilename, FileMode.Open, FileAccess.Read)))
                {
                    float indt1, indt2;

                    //if (reader.ChannelsCount != 2)
                    //{
                    //    MessageBox.Show("ステレオのwavファイル以外には対応していません", "Sorry, Stereo wav only");
                    //    throw new Exception("ステレオのwavファイル以外には対応していません");
                    //}

                    while (true)
                    {
                        progressValue = progressMin2 + (progressMax2 - progressMin2) * readbytes++ / (double)reader.SamplesCount;

                        bool isEnd = false;
                        //indt1 = indt2 = 0.0f;
                        if (reader.ChannelsCount >= 2)
                        {
                            if (!reader.ReadSample(out indt1)) isEnd = true;
                            if (!reader.ReadSample(out indt2)) isEnd = true;
                        }
                        else
                        {
                            if (!reader.ReadSample(out indt1)) isEnd = true;
                            indt2 = indt1;
                        }
                        float ave_square = Math.Max(indt1 * indt1, indt2 * indt2);
                        bool isSplitPoint = false;

                        for (int i = 0; i < SilenceLevelsSquare.Length; i++)
                        {
                            if (ave_square < SilenceLevelsSquare[i])  // 指定の音量を超えない
                            {
                                if (assert[i] >= 0) assert[i]++;
                            }
                            else  // 指定の音量を超える
                            {
                                if (assert[i] != -1 && assert[i] / (double)reader.SamplingRate >= SilenceTime)
                                {
                                    isSplitPoint = true;
                                    for (int j = 0; j < i; j++)
                                    {
                                        assert[j] = -1;  // 上位のアサートを無効にする。
                                    }
                                    for (int j = i; j < SilenceLevelsSquare.Length; j++)
                                    {
                                        assert[j] = -1;  // 下位のアサートを初期化する。
                                    }
                                    assert[i] = 0;
                                    break;
                                }
                                assert[i] = 0;
                            }
                        }

                        if (isSplitPoint || isEnd)
                        {
                            // ここでbufferを指定のファイルに書き出し、wavindexをインクリメントし、
                            // bufferをクリアする。

                            if (ApplyTailCut)
                            {
                                int j;
                                float threshold = (float)Math.Pow(10.0, ThresholdInDB / 20.0);  // * 32768.0
                                for (j = buffer[0].Count - 1; j >= 0; j--)
                                {
                                    if (Math.Max(buffer[0][j] * buffer[0][j], buffer[1][j] * buffer[1][j]) >= threshold * threshold) break;  // 2で掛けるのを忘れてた -> maxに変えました
                                }
                                // now I need j-th sample, so now new_filesize is ((j+1)*4+44)
                                buffer[0].RemoveRange(j + 1, buffer[0].Count - j - 1);
                                buffer[1].RemoveRange(j + 1, buffer[1].Count - j - 1);
                            }
                            if (ApplyFadeIn && buffer[0].Count >= 10)
                            {
                                int scnt = buffer[0].Count;
                                for (int j = 0; j < FadeInSamples; j++)
                                {
                                    buffer[0][j] *= (j + 1) / (float)FadeInSamples;
                                    buffer[1][j] *= (j + 1) / (float)FadeInSamples;
                                }
                                for (int j = 0; j < FadeOutSamples; j++)
                                {
                                    buffer[0][scnt - j - 1] *= (j + 1) / (float)FadeOutSamples;
                                    buffer[1][scnt - j - 1] *= (j + 1) / (float)FadeOutSamples;
                                }
                            }

                            try
                            {
                                if (wavindex >= Convert.ToInt32(WaveRenamer_Enumerator.Current[2]))
                                {
                                    // 書き出しを行う
                                    String filename = WaveRenamer_Enumerator.Current[renameNames_index2];
                                    if (!(filename.Length >= 10 && filename.Substring(0, 10) == "____dummy_"))
                                    {
                                        WaveFileWriter.WriteAllSamples(
                                            RenamedWaveFilePath + filename,
                                            new float[][] { buffer[0].ToArray(), buffer[1].ToArray() },
                                            reader.ChannelsCount, reader.SamplingRate, reader.BitDepth);
                                        // モノラルの書き出しにも対応しているというクソ仕様
                                        createdWavCount++;
                                    }
                                    renameNames_index2++;
                                }
                            }
                            finally
                            {
                                wavindex++;
                            }

                            while (renameNames_index2 >= WaveRenamer_Enumerator.Current.Length)
                            {
                                renameNames_index2 = 3;
                                if (WaveRenamer_Enumerator.MoveNext() == false)
                                {
                                    return createdWavCount;
                                }
                            }
                            buffer[0].Clear();
                            buffer[1].Clear();
                        }

                        if (isEnd) break;  // 次の入力wavファイルへ

                        buffer[0].Add(indt1);
                        buffer[1].Add(indt2);
                    }
                }
            }

            return createdWavCount;
        }

    }
}
