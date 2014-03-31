using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    /// <summary>
    /// Midi Note を説明する型
    /// [ノート番号, 音符長さ.nume, 音符長さ.deno, voiN, t1, t2] を要素に持つ。
    /// </summary>
    class MNote
    {
        public Frac t;  // null許容
        public Frac l;  // null非許容
        public int n;
        //public int VoiceN;
        public int v;
        public MNote prev;  // prev.nのみを参照します(Equals()の実装を確認してください)

        /// <summary>
        /// 0 ... no time nor voiceN;
        /// 1 ... no time (obsolete);
        /// 2 ... full set;
        /// 3 ... (no time or full set) with previous note;
        /// 
        /// voiceNがobsoleteになったため、availability 1 は削除されました。
        /// 
        /// 継承を使うべきだった気もしますがこれを作った時は継承とか知らなかったので許せ
        /// とは言っても冗長感がやばい
        /// </summary>
        private int availability;

        public MNote(MNote mn)
        {
            t = mn.t;
            l = mn.l;
            n = mn.n;
            v = mn.v;
            //VoiceN = mn.VoiceN;
            prev = mn.prev;

            availability = mn.availability;
        }

        /// <summary>
        /// availability が 3 の MNote を作成します。
        /// </summary>
        public MNote(MNote mn, MNote prevnote)
        //public MNote(int noteNumber, long length0, long length1, int velocity, int voiceNumber, long time0, long time1)
        {
            if (mn.availability >= 3) throw new Exception("availability が 3 以上の MNote インスタンスを引数として MNote(MNote mn, MNote prevnote) を呼ぶことは出来ません。");

            t = ((mn.t == null) ? null : new Frac(mn.t));  // maybe null
            l = new Frac(mn.l);
            n = mn.n;
            v = mn.v;
            //VoiceN = voiceNumber;
            prev = prevnote;  // reference

            availability = 3;
        }

        /// <summary>
        /// availability が 2 の MNote を作成します。
        /// </summary>
        public MNote(int noteNumber, long length0, long length1, int velocity, long time0, long time1)
        //public MNote(int noteNumber, long length0, long length1, int velocity, int voiceNumber, long time0, long time1)
        {
            t = new Frac(time0, time1);
            l = new Frac(length0, length1);
            n = noteNumber;
            v = velocity;
            //VoiceN = voiceNumber;
            prev = null;

            availability = 2;
        }

        /*
        /// <summary>
        /// availability が 1 の MNote を作成します。
        /// </summary>
        public MNote(int noteNumber, int length0, int length1, int velocity)
        //public MNote(int noteNumber, int length0, int length1, int velocity, int voiceNumber)
        {
            t = null;
            l = new Frac(length0, length1);
            n = noteNumber;
            v = velocity;
            //VoiceN = voiceNumber;

            availability = 1;
        }
         */

        /*
        /// <summary>
        /// availability が 1 の MNote を作成します。
        /// </summary>
        public MNote(MNote mn)
        //public MNote(MNote mn, int voiceNumber)
        {
            if (mn.availability >= 1) throw new Exception("availability が 1 以上の MNote インスタンスを引数として MNote(MNote mn, int voiceNumber) を呼ぶことは出来ません。");

            t = null;
            l = new Frac(mn.l);
            n = mn.n;
            v = mn.v;
            //VoiceN = voiceNumber;

            availability = 1;
        }*/

        /// <summary>
        /// availability が 0 の MNote を作成します。
        /// </summary>
        public MNote(int noteNumber, long length0, long length1, int velocity)
        {
            t = null;
            l = new Frac(length0, length1);
            n = noteNumber;
            v = velocity;
            //VoiceN = 0;
            prev = null;

            availability = 0;
        }


        public static int Swap(MNote a, MNote b)
        {
            MNote w = new MNote(a);

            a.n = b.n;
            a.l = b.l;
            //a.VoiceN = b.VoiceN;
            a.t = b.t;
            a.v = b.v;
            a.prev = b.prev;
            a.availability = b.availability;

            b.n = w.n;
            b.l = w.l;
            //b.VoiceN = w.VoiceN;
            b.t = w.t;
            b.v = w.v;
            b.prev = w.prev;
            b.availability = w.availability;

            return 0;
        }

        public override int GetHashCode()
        {
            return (this.t == null ? 0 : this.t.GetHashCode()) + this.n.GetHashCode() * 3 + this.n + this.v;  // 適当
        }

        public override bool Equals(object obj)
        {
            if (this.GetType() != obj.GetType())
                return false;
            MNote b = (MNote)obj;
            if ((object)b == null) return false; // これが無いのは重大なバグだった可能性？いや、そうでもないか？

            // thisとbは共にFrac型

            if (this.n != b.n) return false;
            if (this.v != b.v) return false;
            if (this.t != b.t) return false;  // 確か共にnullならtrueだったはず・・・
            if (this.l != b.l) return false;

            if (this.prev == null && b.prev == null) return true;
            if (this.prev == null || b.prev == null) return false;
            if (this.prev.n != b.prev.n) return false;
            return true;
        }
    }
}
