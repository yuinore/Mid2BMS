using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    class MultiTrackMidiEvent
    {
        public int TrackID;
        public MidiEvent Event;

        public MultiTrackMidiEvent() { }

        public MultiTrackMidiEvent(int trackID, MidiEvent Event)
        {
            this.TrackID = trackID;
            this.Event = Event;
        }
    }
}
