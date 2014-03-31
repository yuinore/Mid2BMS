using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{

    // IEqualityComparer<T>の実装が面倒なのでセレクタ的なものはこれで賄う
    public class EqualityCompareSelector<T, TKey> : IEqualityComparer<T>
    {
        private Func<T, TKey> selector;

        public EqualityCompareSelector(Func<T, TKey> selector)
        {
            this.selector = selector;
        }

        public bool Equals(T x, T y)
        {
            return selector(x).Equals(selector(y));
        }

        public int GetHashCode(T obj)
        {
            return selector(obj).GetHashCode();
        }
    }

    public static class ExtensionMethods
    {
        /// <summary>
        /// 引用<br></br>
        /// http://neue.cc/2009/08/07_184.html <br></br>
        /// >>Distinctの引数はラムダ式でのselectorを受け付けてくれない。<br></br>
        /// >>IEqualityComparerだけなので、抽出のためにわざわざ外部にIEqualityComparerを実装したクラスを作る必要がある。<br></br>
        /// >>それって、面倒くさいし分かり辛いし、何でここだけ古くさいような仕様なのだろう。C#3.0っぽくない。<br></br>
        /// >>しょうがないので、単純ですけど汎用的に使えるようなものを作ってみた。<br></br>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IEnumerable<T> Distinct<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector)
        {
            return source.Distinct(new EqualityCompareSelector<T, TKey>(selector));
        }


        public class CompareSelector<T> : IComparer<T>
        {
            private Func<T, T, int> predicate;

            public CompareSelector(Func<T, T, int> predicate)
            {
                this.predicate = predicate;
            }

            public int Compare(T x, T y)
            {
                // T でないものは あらゆる T より小さいとする

                if (!(x is T && x is T)) // 例外投げてもいいと思うけど
                {
                    if (!(x is T || x is T))
                    {
                        return 0;
                    }
                    else if (x is T)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }

                return predicate((T)x, (T)y);
            }
        }
        public class CompareSelector<T, TKey> : IComparer<T>
            where TKey : IComparable<TKey>
        {
            private Func<T, TKey> selector;

            public CompareSelector(Func<T, TKey> selector)
            {
                this.selector = selector;
            }

            public int Compare(T x, T y)
            {
                // T でないものは あらゆる T より小さいとする

                if (!(x is T && x is T)) // 例外投げてもいいと思うけど
                {
                    if (!(x is T || x is T))
                    {
                        return 0;
                    }
                    else if (x is T)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }

                return selector((T)x).CompareTo(selector((T)y));
            }
        }

        public static int BinarySearch<T>(this List<T> source, T item, Func<T, T, int> predicate)
        {
            return source.BinarySearch(item, new CompareSelector<T>(predicate));
        }
        /*
        public static int BinarySearch<T>(this List<T> source, T item, int index, int count, Func<T, T, int> predicate)
        {
            return source.BinarySearch(index, count, item, new CompareSelector<T>(predicate));
        }*/
        public static int BinarySearch<T, TKey>(this List<T> source, T item, Func<T, TKey> selector)
            where TKey : IComparable<TKey>
        {
            return source.BinarySearch(item, new CompareSelector<T, TKey>(selector));
        }
    }
 
}
