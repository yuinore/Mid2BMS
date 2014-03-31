using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    enum BMSObjectType
    {
        None = 0,

        Wav,  // reference to #WAVXX
        Bmp,  // reference to  #BMPXX
        ExtendedTempo,  // reference to #BPMXX
        Stop,  // ref to #STOPXX
        Text,  // ref to #TEXTXX ???
        Extank,  // ref to #EXRANKXX ???
        Number  // not reference but number (represents BPM in line of #MMM03:)
    }
}
