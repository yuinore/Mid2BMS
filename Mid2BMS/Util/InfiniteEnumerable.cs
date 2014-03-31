using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    /// <summary>
    /// 最後の要素を返し続ける IEnumerable を生成します。未テスト
    /// </summary>
    class InfiniteEnumerable<T> : IEnumerable<T>
    {
        IEnumerable<T> collection1;
        List<T> collection2;
        T last = default(T);

        public InfiniteEnumerable() // コレクション初期化子を使うために必要
        {
            collection1 = new List<T>();
            collection2 = new List<T>();
        }
        public InfiniteEnumerable(IEnumerable<T> collection_)
        {
            collection1 = collection_;
            collection2 = new List<T>();
        }
        public IEnumerator<T> GetEnumerator()
        {

            foreach (T x in collection1)
            {
                yield return (last = x);  // はじめてyield使った
            }
            foreach (T x in collection2)
            {
                yield return (last = x);
            }
            while (true)
            {
                yield return last;  // collectionが空ならCount()は0
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        // ref: http://yohshiy.blog.fc2.com/blog-entry-211.html#sthash.PYPd33a3.dpuf

        public T Add(T x)
        {
            collection2.Add(x);
            return x;
        }
    }
}
