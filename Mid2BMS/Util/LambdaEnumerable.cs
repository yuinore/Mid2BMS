using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    /// <summary>
    ///  ラムダ式を用いた無限シーケンスを生成します。コレクション初期化子は使用できません。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class LambdaEnumerable<T> : IEnumerable<T>
    {
        Func<int, T> pred;

        public LambdaEnumerable(Func<int, T> pred)
        {
            this.pred = pred;
        }

        public IEnumerator<T> GetEnumerator()
        {
            int i = 0;
            while (true)
            {
                yield return pred(i++);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
