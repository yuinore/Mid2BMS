using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Mid2BMS
{
    class MidiTrackWriter
    {
        MidiTrack mt;
        MidiStruct ms;
        Frac t;
        public Frac Tick { get { return new Frac(t); } }
        //int def_v = 100;

        public MidiTrackWriter(MidiTrack mt_, MidiStruct ms_)
        {
            mt = mt_;
            ms = ms_;  // resolutionの参照用
            t = new Frac(0);
        }

        public void AddNote(int n, int v, Frac q, Frac l)
        {
            MidiEventNote me = new MidiEventNote();
            me.ch = 0;
            me.n = n;
            me.v = v;
            me.q = (int)(new Frac(q.n * ms.BeatsToTicks(1) * 4, q.d));
            me.tick = (int)(new Frac((long)t.n * (long)ms.BeatsToTicks(1) * 4L, t.d));  // コードが汚い、やり直し。

            if (me.tick < 0)
            {
                MessageBox.Show("だからなんでtickが負(" + me.tick + ")なのか");
            }

            mt.Add(me);

            t.Add(l);
        }

        public void AddRest(Frac l)
        {
            t.Add(l);
        }

        public MidiTrack Close()
        {
            return mt;
        }
    }
}
