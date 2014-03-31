using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    interface IWaveSplitter
    {
        double ThresholdInDB { get; set; } // tailcut時のthreshold

        int FadeInSamples { get; set; }
        int FadeOutSamples { get; set; }

        double SilenceTime { get; set; }

        int Process(
            String[][] WaveSplitter_Text, String[][] WaveRenamer_Text, out String[][] RenameResults,
            String InputWaveFilePath, String IndexedWaveFilePath, String RenamedWaveFilePath,
            ref double progressValue, double progressMin, double progressMax);
    }
}
