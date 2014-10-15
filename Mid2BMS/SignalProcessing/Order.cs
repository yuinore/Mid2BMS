using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    class Order
    {
        public double Rate = 1.0;//0.999;

        public double Evaluate(float[] src)
        {
            double sum = 0;
            double exp = 1.0;

            for (int i = 0; i < src.Length; i++)
            {
                sum += src[i] * src[i] * exp;
                exp *= Rate;
            }

            return sum;
        }

        /// <summary>
        /// inFilename == outFilename のとき、上書き保存します。
        /// </summary>
        public double Evaluate(String inFilename)
        {
            var wr = new WaveFileReader(inFilename);
            int chN = wr.ChannelsCount;
            int sr = wr.SamplingRate;
            int bd = wr.BitDepth;
            wr.Close();

            float[][] buf = WaveFileReader.ReadAllSamples(inFilename);
            bool isStereo = chN == 2;

            if (isStereo)
            {
                return 0.5 * (Evaluate(buf[0]) + Evaluate(buf[1]));
            }
            else
            {
                return Evaluate(buf[0]);
            }
        }
    }
}
