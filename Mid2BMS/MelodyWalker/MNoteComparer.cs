using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;


namespace Mid2BMS
{
    class MNoteComparerInTime : IComparer<MNote>
    {
        public int Compare(MNote x, MNote y)
        {
            MNote x1 = (MNote)x;
            MNote y1 = (MNote)y;

            if (x1.t != y1.t) return (x1.t > y1.t) ? 1 : -1;  // t の小さい順
            if (x1.n != y1.n) return (x1.n > y1.n) ? 1 : -1;  // n の小さい順
            if (x1.v != y1.v) return (x1.v < y1.v) ? 1 : -1;  // v の*大きい*順
            if (x1.l != y1.l) return (x1.l > y1.l) ? 1 : -1;  // L の小さい順
            if (x1.prev != null && y1.prev != null && x1.prev.n != y1.prev.n) return (x1.prev.n > y1.prev.n) ? 1 : -1;  // prev.n の小さい順

            return 0;
        }
    }
    class MNoteComparerInGate : IComparer<MNote>
    {
        public int Compare(MNote x, MNote y)
        {
            MNote x1 = (MNote)x;
            MNote y1 = (MNote)y;

            if (x1.n != y1.n) return (x1.n > y1.n) ? 1 : -1;  // n の小さい順
            if (x1.v != y1.v) return (x1.v < y1.v) ? 1 : -1;  // v の*大きい*順
            if (x1.l != y1.l) return (x1.l > y1.l) ? 1 : -1;  // L の小さい順
            if (x1.prev != null && y1.prev != null && x1.prev.n != y1.prev.n) return (x1.prev.n > y1.prev.n) ? 1 : -1;  // prev.n の小さい順

            return 0;
        }
    }
}