using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    public class ArrTuple<T1, T2>
    {
        public ArrTuple(T1 v1, T2 v2) { Item1 = v1; Item2 = v2; }  //8段
        public T1 Item1;
        public T2 Item2;
        public object this[int index] {
            get
            {
                switch (index)
                {
                    case 0: return Item1;
                    case 1: return Item2;
                }
                return null;  // こんなところで例外投げてもアレ
            }
        }
    }
    // ＞C#3.0ってのは、もっとライトウェイトじゃなきゃダメなんだ。推論！型推論！
    // それな
    // http://neue.cc/2009/08/07_184.html
    /// <summary>
    /// タイプセーフかつ型推論なn組を生成します。一種のファクトリメソッド。
    /// </summary>
    public static class Arr
    {
        /*public static Tuple<T1> ay<T1>(T1 v1)
        {
            return new Tuple<T1>(v1);
        }*/
        public static ArrTuple<T1, T2> ay<T1, T2>(T1 v1, T2 v2)
        {
            return new ArrTuple<T1, T2>(v1, v2);
        }
        /*public static Tuple<T1, T2, T3> ay<T1, T2, T3>(T1 v1, T2 v2, T3 v3)
        {
            return new Tuple<T1, T2, T3>(v1, v2, v3);
        }
        public static Tuple<T1, T2, T3, T4> ay<T1, T2, T3, T4>(T1 v1, T2 v2, T3 v3, T4 v4)
        {
            return new Tuple<T1, T2, T3, T4>(v1, v2, v3, v4);
        }*/
    }
}
