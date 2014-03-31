using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    /// <summary>
    /// １日頑張ってもButterworthFilterの実装がうまくいかなかったので、
    /// 諦めてFIRフィルタやります。
    /// FFT使うかどうかは後で考えます。
    /// 
    /// あとdoubleとfloatの計算時間が今のプロセッサではほとんど変わらないみたいな話
    /// </summary>
    class FIRFilter : DigitalFilter
    {
        double[] impulseResponse;
        FixedCircularQueue<double> buf;

        private FIRFilter()
        {
        }

        public FIRFilter(double[] impulseResponse)
        {
            this.impulseResponse = (double[])impulseResponse.Clone();

            Initialize();
        }

        public FIRFilter(float[] impulseResponse)
        {
            this.impulseResponse = new double[impulseResponse.Length];

            for (int i = 0; i < impulseResponse.Length; i++)
            {
                this.impulseResponse[i] = impulseResponse[i];
            }

            Initialize();
        }

        public FIRFilter(Stream monauralWavefileStream)
        {
            // ぼく「自首しようとすると捕まるのはヤラセ感しか無い」
            // 隣に居た人「自首するって（カメラに向かって）言わないで自首しに行ったら良いんじゃないの？」
            // ぼく「それだ！」
            List<double> ir = new List<double>();
            WaveFileReader wr = new WaveFileReader(monauralWavefileStream);
            float indt;

            while (wr.ReadSample(out indt))
            {
                ir.Add(indt);
            }

            this.impulseResponse = ir.ToArray();

            Initialize();

            wr.Close();

            // どうやら、彼にはaとrという2つの文字ではなく、
            // aとarとar^2という3つの項が3つの異なる文字に見えていたらしい。
            // なんでや・・・
        }

        private void Initialize()
        {
            buf = new FixedCircularQueue<double>(impulseResponse.Length);
        }

        public double Process(double val)
        {
            double ret = 0;

            buf.PushOnTop(val);

            // あんまりC#で高速化のこととか考えたくないな・・・
            // (FFTは別として、ポインタと配列の違いとか、メソッド呼び出しのオーバーヘッドとか)
            // でもCircularBufの%をmaskに変えるのはやってみてもいいかもしれない
            // (計測したらget_Item()が33%だった)
            // 誤差が小さくなるように大きな方から総和を取る
            for (int i = impulseResponse.Length - 1; i >= 0; i--)
            {
                ret += buf[i] * impulseResponse[i];   // bufは反転されている
            }

            return ret;
        }

        public double CharacteristicCurve(double _2pi_normalized_frequency)
        {
            throw new InvalidOperationException("FIR Characteristic Curve undefined");
        }

        public void Reset()
        {
            for (int i = 0; i < buf.Count; i++)
            {
                buf[i] = 0;
            }
        }

        /// <summary>
        /// bufはコピーされません
        /// </summary>
        /// <returns></returns>
        public DigitalFilter Clone()
        {
            var f = (FIRFilter)this.MemberwiseClone();
            f.Initialize();
            return f;
        }
    }
}
