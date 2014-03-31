using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    /// <summary>
    /// 要素数固定の巡回キュー
    /// maskとか使わずに素朴な実装でも別に良い気がしてきた
    /// </summary>
    class FixedCircularQueue<T>
    {
        double[] buf;
        public int Count { get; private set; }
        int index;
        //int mask;
        //int capacity;
       
        public FixedCircularQueue(int count)
        {
            // (-3) & 0xF
            //         => 13
            // (-3) % 16
            //         => -3

            if (count <= 0) throw new Exception("invalid count @ FixedCircularQueue");

            this.Count = count;
            index = 0;

            //capacity = (count & (count - 1));
            //if (capacity == 0) capacity = count;

            //int mask = capacity - 1;

            //buf = new double[capacity];
            buf = new double[count];
        }

        public double this[int i]
        {
            get
            {
                //return buf[(i + index) & mask]; 
                return buf[(i + index) % Count];
            }
            set
            {
                //buf[(i + index) & mask] = value;
                buf[(i + index) % Count] = value;
            }
        }

        public double PushOnTop(double val)
        {
            // index  0 1 2 3 4
            // value  4 5 6 7 8
            //
            //   >> Push(9)
            //
            // index  0 1 2 3 4
            // value  9 4 5 6 7

            index = (index + Count - 1) % Count;
            return buf[index] = val;
        }
    }
}
