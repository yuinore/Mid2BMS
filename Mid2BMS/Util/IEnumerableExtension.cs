using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    static class IEnumerableExtension
    {
        // 遅延評価・・・？？
        // x+y+z と (x+y)+z は異なる。意味は同じかもしれないがデータ構造は明らかに異なる。
        /// <summary>
        /// ２つの配列の直和を求めます。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static IEnumerable<ArrTuple<int, T>> DirectSum<T>(this IEnumerable<T> x, IEnumerable<T> y)
        {
            foreach (var element in x)
            {
                yield return Arr.ay(0, element);
            }
            foreach (var element in y)
            {
                yield return Arr.ay(1, element);
            }
        }

        /// <summary>
        /// n個の配列の直和を求めます。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        /// <returns></returns>
        public static IEnumerable<ArrTuple<int, T>> DirectSum<T>(this IEnumerable<IEnumerable<T>> x)
        {
            int i = 0;
            foreach (var x1 in x)
            {
                foreach (var element in x1)
                {
                    yield return Arr.ay(i, element);
                }
                i++;
            }
        }

        /// <summary>
        /// 直和を配列に復元します。
        /// 遅延評価ではありません。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> DirectDifference<T>(this IEnumerable<ArrTuple<int, T>> x)
        {
            List<List<T>> y = new List<List<T>>();
            foreach (var tpl in x)
            {
                while (y.Count <= tpl.Item1)
                {
                    y.Add(new List<T>());
                }

                y[tpl.Item1].Add(tpl.Item2);
            }

            return y;
        }

        /// <summary>
        /// object.ToString() またはそれを継承したメソッド T.ToString() を使用して
        /// IEnumerable&lt;T&gt; を System.String に変換します。 
        /// </summary>
        /// <typeparam name="T">ToString()を備えた配列の要素の型です</typeparam>
        /// <param name="arr">結合したい要素を格納したIEnumerable&lt;T&gt;</param>
        /// <param name="separator">要素と要素の間に挿入する文字列</param>
        /// <returns>結合された文字列</returns>
        public static String Join<T>(this IEnumerable<T> arr, String separator)
        {
            return String.Join(separator, arr.Select(x => x.ToString()));
        }

        // C#3.0(Linq)はschemeに近い？
        // http://karetta.jp/book-node/gauche-hacks/023107
        // >「Lisp脳」の謎に迫る - Schemeプログラマの発想
    }
}
