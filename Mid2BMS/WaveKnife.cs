using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    class WaveKnife
    {
        public double crossfadebeats = 4.0 / 16.0; // 16th note

        // WaveReaderとWaveWriterを早く作れよ
        public void Knife(
            String BasePath, String srcFilename, String filenamebase,
            double bpm, double prebeats, double intervalbeats
            )
        {
            WaveFileReader r = null;
            WaveFileWriter w1 = null;
            WaveFileWriter w2 = null;

            try
            {
                Directory.CreateDirectory(BasePath + "waveknife\\");

                Func<int, String> outFilename = (int i) => String.Format(filenamebase, i);

                r = new WaveFileReader(BasePath + srcFilename);

                w1 = null;
                w2 = new WaveFileWriter(
                    BasePath + @"waveknife\" + outFilename(1),
                    r.ChannelsCount, r.SamplingRate, r.BitDepth);

                float smp;

                double sample_per_beat = 60.0 * r.SamplingRate / bpm;

                int pre_samples = (int)((prebeats - crossfadebeats) * sample_per_beat + 0.5);
                int crossfade_samples = (int)(crossfadebeats * sample_per_beat + 0.5);
                int interval_samples = (int)((intervalbeats - crossfadebeats) * sample_per_beat + 0.5);

                for (int i = 0; i < pre_samples; i++)
                {
                    r.ReadSample(out smp);
                    w2.WriteSample(smp);
                    if (r.ChannelsCount >= 2)
                    {
                        r.ReadSample(out smp);
                        w2.WriteSample(smp);
                    }
                }

                double gainDelta = 1.0 / crossfade_samples;

                int idx = 2;
                int idx2 = 1;
                while (true)
                {
                    int i;

                    if (w1 != null) w1.Close();
                    w1 = w2;
                    w2 = null;
                    Action makefile = () =>
                    {
                        //int idx2 = idx;  // ←意味ない
                        w2 = new WaveFileWriter(
                            //BasePath + @"waveknife\" + filenamebase + (idx / 10) + (idx % 10) + ".wav",
                            //BasePath + @"waveknife\" + String.Format(filenamebase, idx),
                             BasePath + @"waveknife\" + outFilename(idx2),
                            r.ChannelsCount, r.SamplingRate, r.BitDepth);
                    };
                    idx2 = idx;
                    idx++;

                    double gain = 0.0;
                    for (i = 0; i < crossfade_samples; i++)
                    {
                        // w1はフェードアウトする
                        // w2はフェードインする

                        if (!r.ReadSample(out smp)) break;
                        w1.WriteSample((float)(smp * (1.0 - gain)));
                        if(w2 == null) makefile();
                        w2.WriteSample((float)(smp * gain));

                        if (r.ChannelsCount >= 2)
                        {
                            if (!r.ReadSample(out smp)) break;
                            w1.WriteSample((float)(smp * (1.0 - gain)));
                            w2.WriteSample((float)(smp * gain));
                        }

                        gain += gainDelta;
                    }
                    if (i != crossfade_samples) break;

                    for (i = 0; i < interval_samples; i++)
                    {
                        if (!r.ReadSample(out smp)) break;
                        if (w2 == null) makefile();
                        w2.WriteSample(smp);

                        if (r.ChannelsCount >= 2)
                        {
                            if (!r.ReadSample(out smp)) break;
                            w2.WriteSample(smp);
                        }
                    }
                    if (i != interval_samples) break;
                }
            }
            finally
            {
                if (w1 != null) w1.Close();
                if (w2 != null) w2.Close();
                if (r != null) r.Close();
            }
        }
    }
}
