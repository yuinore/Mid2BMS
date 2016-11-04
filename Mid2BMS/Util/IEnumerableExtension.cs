using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    static class IEnumerableExtension
    {
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
