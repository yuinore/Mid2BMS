using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    class Monoauralizer
    {
        public static bool Monoauralize(String inFilename, String outFilename, double threshold)
        {
            float[][] data = WaveFileReader.ReadAllSamples(inFilename);
            float[][] newdata = new float[][] { new float[data[0].Length] };
            int srate = 44100;
            int bdepth = 16;
            using (var r = new WaveFileReader(inFilename))
            {
                srate = r.SamplingRate;
                bdepth = r.BitDepth;
            }

            if (data.Length != 2) return false;

            double thresholdvalsquare = Math.Pow(10, threshold * 2.0 / 20.0);

            for (int i = 0; i < data[0].Length; i++)
            {
                var dif = data[1][i] - data[0][i];
                if (dif * dif > thresholdvalsquare)
                {
                    return false;
                }
                newdata[0][i] = (data[1][i] + data[0][i]) * 0.5f;
            }

            WaveFileWriter.WriteAllSamples(outFilename, newdata, 1, srate, bdepth);

            return true;
        }
    }
}
