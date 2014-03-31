using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    // 音質を下げてまでメモリ展開時のファイルサイズを減らしたいかと言われるとそんなことは無いので、
    // 心理的なアレを考慮したアレまでは実装しなくても良いかと思います
    // 
    // 最初の1024サンプルを窓付きでFFT→ノイズ性が見られる→ゲインに下駄
    // ノイズ性→スペクトル値の中央値と最大値の比
    // 周波数が背景と一致していない音は判別しやすい？(ノイズ、声)
    //
    // どうやらABXテストをする必要性があるようだな
    class AdaptiveDownsampler
    {
        DigitalFilter lowpass;
        DigitalFilter hipass;

        double Threshold = -42.0;  // 単位dB, -90dB ～ 0dB の範囲
        // 30 ... ファイルサイズ超節約
        // 36 ... ファイルサイズ節約(うるさい曲ならこれでもok)
        // 42 ... ふつう
        // 48 ... ちょっと余裕を持って
        // 54 ... ぜいたく

        double threshold_exact = 0;
        double threshold_square = 0;

        public AdaptiveDownsampler(double threshold)
            : this (
                // ストリームって、読み込みカーソルの情報は持ってないのか？
                new FIRFilter(Properties.Resources.impulse_response_lowpass_x2),
                new FIRFilter(Properties.Resources.impulse_response_hipass_x2),
                threshold)
        {
        }

        // Biquadではそれっぽい結果になるので、任意のフィルタでも
        // hipass + lowpass = 1 になるものだと思っていたのだがそうでもないらしい
        public AdaptiveDownsampler(DigitalFilter lowpass, DigitalFilter hipass, double threshold)
        {
            this.lowpass = lowpass;
            this.hipass = hipass;
            this.Threshold = threshold;

            threshold_exact = Math.Pow(10, Threshold / 20);
            threshold_square = threshold_exact * threshold_exact;  // square
        }

        /// <summary>
        /// 2倍ダウンサンプリングに成功したらtrue
        /// </summary>
        /// <returns></returns>
        public bool DownSample(float[] src, float[] dst)
        {
            double val;
            int dsti = 0;
            int ph = 0;

            lowpass.Reset();
            hipass.Reset();

            for (int i = 0; i < src.Length; i++)
            {
                val = hipass.Process(src[i]);
                if (val * val > threshold_square)
                {
                    return false;
                }

                val = lowpass.Process(src[i]);
                if(ph == 0) dst[dsti++] = (float)val;
                ph = 1 - ph;
            }

            return true;
        }
        
        /// <summary>
        /// inFilename == outFilename のとき、上書き保存します。
        /// </summary>
        public static bool DownSample(String inFilename, String outFilename, double threshold)
        {
            double dmy = 0;
            return DownSample(inFilename, outFilename, threshold, ref dmy, 0, 1);
        }

        /// <summary>
        /// inFilename == outFilename のとき、上書き保存します。
        /// </summary>
        public static bool DownSample(
            String inFilename, String outFilename, double threshold,
            ref double progressValue, double progressMin, double progressMax)
        {
            AdaptiveDownsampler addsL = new AdaptiveDownsampler(threshold);  // DigitalFilterクラスの使い回ししないの？？？
            AdaptiveDownsampler addsR = new AdaptiveDownsampler(threshold);

            var wr = new WaveFileReader(inFilename);
            int chN = wr.ChannelsCount;
            int sr = wr.SamplingRate;
            int bd = wr.BitDepth;
            wr.Close();

            if ((sr & 1) != 0) return false;  // 2で割り切れないsampling rateだったら諦める ex)11025Hz
            // 気分的には22050Hzと44100Hz以外をはじきたい

            float[][] buf = WaveFileReader.ReadAllSamples(inFilename);
            bool isStereo = chN == 2;
            float[] dstL = new float[(buf[0].Length + 1) >> 1];  // サンプル数が奇数のときは・・・？？
            float[] dstR = isStereo ? new float[(buf[1].Length + 1) >> 1] : null;

            progressValue = (progressMax - progressMin) * 0.1 + progressMin;  // 10 %

            bool result1 = addsL.DownSample(buf[0], dstL);

            progressValue = (progressMax - progressMin) * 0.3 + progressMin;  // 30 %

            bool result2 = isStereo ? addsR.DownSample(buf[1], dstR) : true;

            progressValue = (progressMax - progressMin) * 0.5 + progressMin;  // 50 %

            if (result1 && result2)
            {
                WaveFileWriter.WriteAllSamples(
                    outFilename, isStereo ? new float[][] { dstL, dstR } : new float[][] { dstL },
                    chN, sr / 2, bd);

                if (sr >= 44100)
                {
                    // もし44100のファイルを処理したのであれば、さらにもう一度挑戦する
                    DownSample(inFilename, outFilename, threshold, 
                        ref progressValue, 0.5 * (progressMin + progressMax), progressMax);
                }
            }

            progressValue = progressMax;  // 100%

            return result1 && result2;
        }
    }
}
