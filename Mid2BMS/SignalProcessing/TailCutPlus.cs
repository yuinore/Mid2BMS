using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    static class TailCutPlus
    {
        public static void Process(string inFilename, string outFilename, double Threshold, double FadeOutTime, bool WriteOriginalIfNoSound)
        {
            float[][] buffer = WaveFileReader.ReadAllSamples(inFilename);
            int sr;
            int bd;
            int cc;

            using(var wfr = new WaveFileReader(inFilename)) {
                sr = wfr.SamplingRate;
                bd = wfr.BitDepth;
                cc= wfr.ChannelsCount;
            }

            float[][] buf2 = Process2(buffer, Threshold, FadeOutTime, WriteOriginalIfNoSound);

            WaveFileWriter.WriteAllSamples(outFilename, buf2, cc, sr, bd);
        }

        private static float[][] Process2(float[][] buffer, double Threshold, double FadeOutTime, bool WriteOriginalIfNoSound)
        {
            double threshold2 = Math.Pow(10.0, Threshold / 20.0);
            int fadeoutsamples = (int)Math.Round(FadeOutTime * 44100);  // FIXME: サンプリングレート
            int i;

            for (i = buffer[0].Length - 1; i >= 0; i--)
            {
                for (int ch = 0; ch < buffer.Length; ch++)
                {
                    if (Math.Abs(buffer[ch][i]) >= threshold2)
                    {
                        goto loopout;
                    }
                }
            }

        loopout:
            if (i < 0)
            {
                // 音が存在しなかった
                if (WriteOriginalIfNoSound)
                {
                    return buffer;  // As it is
                }
                else
                {
                    float[][] buf2 = new float[buffer.Length][];

                    for (int ch = 0; ch < buffer.Length; ch++)
                    {
                        buf2[ch] = new float[1] { 0 };  // なんとなく出力が0サンプルというのはバグと誤解されそうだなと思いました。
                    }

                    return buf2;
                }
            }
            else
            {
                // i 番目のサンプルは大きな音なので、その次のサンプルからフェードアウトを開始する。
                //
                // |￣|＿|￣|＿|￣|＿|￣|＿～～～～～～～～～----------------------　↑振幅
                // 　　　　　　　　　　　　　　　　　↑　　　　　↑
                // 　　　　　　　　　　　　　　fadeoutFrom　　fadeoutTo　　　　　　　→時間（単位：サンプル）
                //
                // 注1) 最後のサンプルの時点でthreshold以上だった場合は、
                //      i + 1 == fadeoutFrom == fadeoutTo == buffer[0].Length となり、何も行いません。
                // 注2) 最後のサンプルからFadeOutTime以下の位置でthreshold以上だった場合でも、
                //      フェードアウトのスロープ（傾き）は急激には**なりません**。
                // 注3) すべてのサンプルがthresholdを下回っていた場合は、
                //      i == -1 となり、上のコードで例外処理が行われます。

                // フェードアウトの開始位置
                int fadeoutFrom = i + 1;  // 単位：サンプル

                // フェードアウトの仮想的な終了位置
                int fadeoutTo = fadeoutFrom + (int)Math.Round(FadeOutTime * 44100);
                if (fadeoutTo < fadeoutFrom) fadeoutTo = fadeoutFrom;

                // フェードアウトが打ち切られて音源が終了する位置。出力される.wavの長さ。
                int fadeoutEnd = Math.Min(fadeoutTo, buffer[0].Length);

                float[][] buf2 = new float[buffer.Length][];

                for (int ch = 0; ch < buffer.Length; ch++)
                {
                    buf2[ch] = new float[fadeoutEnd];

                    for (int i2 = 0; i2 < fadeoutEnd; i2++)
                    {
                        if (i2 < fadeoutFrom)
                        {
                            buf2[ch][i2] = buffer[ch][i2];
                        }
                        else
                        {
                            buf2[ch][i2] = buffer[ch][i2] * (fadeoutTo - i2) / (float)(fadeoutTo - fadeoutFrom);  // linear
                        }
                    }
                }

                return buf2;
            }
        }
    }
}
