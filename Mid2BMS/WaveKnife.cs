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
            double bpm, IEnumerable<double> cuttingPointBeats
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

                var cuttingPointBeatsEnumerator = cuttingPointBeats.GetEnumerator();

                int pre_samples = Int32.MaxValue;
                double lastCuttingPointBeats = 0;
                if (cuttingPointBeatsEnumerator.MoveNext())
                {
                    pre_samples = (int)(cuttingPointBeatsEnumerator.Current * sample_per_beat + 0.5);
                    // cutting point の後ろにxfadeが作成されるため注意

                    lastCuttingPointBeats = cuttingPointBeatsEnumerator.Current;
                }

                int crossfade_samples = (int)(crossfadebeats * sample_per_beat + 0.5);
                

                for (int i = 0; i < pre_samples; i++)
                {

                    if (!r.ReadSample(out smp)) break;
                    w2.WriteSample(smp);
                    if (r.ChannelsCount >= 2)
                    {
                        if (!r.ReadSample(out smp)) break;
                        w2.WriteSample(smp);
                    }
                }

                double gainDelta = 1.0 / crossfade_samples;

                int idx = 2;
                int idx2 = 1;
                while (true)
                {
                    int interval_samples = -1;
                    // クロスフェードを含まない、現在の区間のwavのサンプル数

                    if (cuttingPointBeatsEnumerator.MoveNext())
                    {
                        interval_samples = (int)((cuttingPointBeatsEnumerator.Current - lastCuttingPointBeats - crossfadebeats) * sample_per_beat + 0.5);
                        // 四捨五入するせいで誤差が蓄積しそう

                        if (interval_samples < 0)
                        {
                            throw new Exception("クロスフェードが長すぎるッピ！");
                        }

                        lastCuttingPointBeats = cuttingPointBeatsEnumerator.Current;
                    }
                    else
                    {
                        interval_samples = Int32.MaxValue;
                        lastCuttingPointBeats = Int32.MaxValue;
                    }
                    
                    int i;

                    if (w1 != null) w1.Close();
                    w1 = w2;
                    w2 = null;
                    Action makefile = () =>
                    {
                        w2 = new WaveFileWriter(
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
