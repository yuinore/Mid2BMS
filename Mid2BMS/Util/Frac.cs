using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    /// <summary>
    /// 分数の計算をします。
    /// Add, Reduce などを呼ばない限り、自動で約分をしないようになっています←超重要
    /// 　言い換えると .n と .d はフィールドでありプロパティでは無いということです（←言い換えではない）
    /// 　
    /// この型は、不変型ではなく、参照型(class型)であり、かつ ==, !=, &lt;, &gt; 演算子をオーバーロードします。
    /// 参照の等価性を調べるには、ReferenceEqualsみたいな関数を使用してください。
    /// </summary>
    [Serializable]
    class Frac : IComparable<Frac>
    {
        public long n = 0;//ume;
        public long d = 1;//eno;

        public Frac()
        {
            n = 0;
            d = 1;
        }
        public Frac(long nume, long deno)
        {
            n = nume;
            d = deno;
            if (d == 0)
            {
                throw new Exception("分母が0です at Frac(int nume, int deno)");
            }
            if (d < 0)
            {
                throw new Exception("分母が0未満です at Frac(int nume, int deno)");
                //n = -n;
                //d = -d;
            }
        }
        public Frac(long nume)
        {
            n = nume;
            d = 1;
        }
        public Frac(double real_number)
        {
            // 2^23 = 8388608 (単精度)なのでこれ以下にしたい感はある

            // 15360 == 3 * 5 * 1024
            // 10644480 == 27 * 5 * 7 * 11 * 1024
            // 4838400 = 1024 * 27 * 25 * 7
            if (real_number == Math.Round(real_number))
            {
                n = (int)real_number;
                d = 1;
            }
            else
            {
                double seisuu_part = Math.Floor(real_number);
                double shousuu_part = real_number - seisuu_part;

                double gosa1 = 0, gosa2 = 0;
                Frac fr1, fr2;

                {
                    fr1 = new Frac(
                        (long)Math.Round(shousuu_part * 4838400),
                        4838400);
                    gosa1 = Math.Abs((double)fr1 - shousuu_part);
                }
                {
                    fr2 = new Frac(
                        (long)Math.Round(shousuu_part * 1000000),
                        1000000);
                    gosa2 = Math.Abs((double)fr2 - shousuu_part);
                }
                if (gosa1 <= gosa2)
                {
                    this.n = fr1.n;
                    this.d = fr1.d;
                }
                else
                {
                    this.n = fr2.n;
                    this.d = fr2.d;
                }

                this.Reduce();
                this.Add((long)seisuu_part);
            }
        }
        /// <summary>
        /// オブジェクト t の複製が生成されます
        /// </summary>
        /// <param name="t"></param>
        public Frac(Frac t)
        {
            n = t.n;
            d = t.d;
        }

        public static implicit operator Frac(long n)
        {
            return new Frac(n);
        }
        public static explicit operator long(Frac fr)
        {
            return fr.n / fr.d;
        }
        public static explicit operator double(Frac fr)
        {
            return fr.n / (double)fr.d;
        }

        public override String ToString()
        {
            return n + " / " + d;
        }

        public long SetValue(Frac t)
        {
            n = t.n;
            d = t.d;
            return 0;
        }

        /// <summary>
        /// 約分します。
        /// </summary>
        public long Reduce()
        {
            long j;
            long n2 = n, d2 = d;

            if (n == 0)
            {
                d = 1;
                return 0;
            }
            if (n < 0)
            {
                n2 = -n;
            }
            if (d <= 0)
            {
                throw new Exception("分母が不正です at Frac(int nume, int deno)");
            }

            while ((d2 & 1) == 0 && (n2 & 1) == 0) { n >>= 1; d >>= 1; n2 >>= 1; d2 >>= 1; }
            while ((n2 & 1) == 0) { n2 >>= 1; }
            while ((d2 & 1) == 0) { d2 >>= 1; }
            
            j = 3;
            while ((d2 % j) == 0 && (n2 % j) == 0) { n /= j; d /= j; n2 /= j; d2 /= j; }
            while ((n2 % j) == 0) { n2 /= j; }
            while ((d2 % j) == 0) { d2 /= j; }
            
            j = 5;
            while ((d2 % j) == 0 && (n2 % j) == 0) { n /= j; d /= j; n2 /= j; d2 /= j; }
            while ((n2 % j) == 0) { n2 /= j; }
            while ((d2 % j) == 0) { d2 /= j; }
            
            for (j = d2; j >= 7; j--)
            {
                while ((d2 % j) == 0 && (n2 % j) == 0) { n /= j; d /= j; n2 /= j; d2 /= j; }
            }
            return 0;
        }

        public long Add(Frac b)
        {  // 加算する
            if (d != b.d)
            {
                n = n * b.d + d * b.n;
                d = d * b.d;
                Reduce();
            }
            else
            {
                n += b.n;
            }
            return 0;
        }

        /// <summary>
        /// 分母を maxDeno 以下に制限します。
        /// </summary>
        public long LimitDenominator(long maxDeno)
        {
            if (maxDeno <= 0)
            {
                throw new Exception("maxDenoが不正な値です at Frac(int nume, int deno)");
            }

            if (d > maxDeno)
            {
                n = (int)Math.Round(n * maxDeno / (double)d);
                d = maxDeno;
            }
            return 0;
        }

        /// <summary>
        /// 正なら正の整数を、負なら負の整数を、0なら0を返します。
        /// </summary>
        public long ToSign()
        {
            return (d > 0) ? n : -n;
        }

        public int CompareTo(Frac b)
        {
            long dif = n * b.d - b.n * d;
            if (dif > 0) return 1;
            if (dif < 0) return -1;
            return 0;
        }


        /*
        public static Frac operator -(Frac a, Frac b)
        {
            // staticならnullチェックも不要なのに・・・
            Frac c = new Frac(-b.n, b.d);
            c.Add(a);
            return c;
        }*/

        /// <summary>
        /// 同じ有理数を表すときに、真を返します。
        /// ＞変更不可能な型以外で演算子 == をオーバーライドすることはお勧めしません。
        /// ＞変更不可能な型以外で演算子 == をオーバーライドすることはお勧めしません。
        /// ＞変更不可能な型以外で演算子 == をオーバーライドすることはお勧めしません。
        /// http://msdn.microsoft.com/ja-jp/library/ms173147(v=vs.90).aspx
        /// </summary>
        public static bool operator ==(Frac a, Frac b)
        {
            if ((object)a == null && (object)b == null) return true; // これが無いのは重大なバグだった可能性？いや、そうでもないか？
            if ((object)a == null || (object)b == null) return false;
            return a.n * b.d == a.d * b.n;
        }
        /// <summary>
        /// 異なる有理数を表すときに、真を返します。
        /// </summary>
        public static bool operator !=(Frac a, Frac b)
        {
            if ((object)a == null && (object)b == null) return false; // これが無いのは重大なバグだった可能性
            if ((object)a == null || (object)b == null) return true;
            return a.n * b.d != a.d * b.n;
        }
        /// <summary>
        /// オペランドが共にFrac型であり、なおかつ同じ有理数を表すときに、真を返します。
        /// (new Frac(3)).Equals(3) は多分falseを返します(適当
        /// </summary>
        public override bool Equals(object obj)
        {
            if (this.GetType() != obj.GetType())
                return false;
            Frac b = (Frac)obj;
            if ((object)b == null) return false; // これが無いのは重大なバグだった可能性？いや、そうでもないか？
            return this.n * b.d == this.d * b.n;
        }
        public override int GetHashCode()
        {
            //return (int)(n * 932187.0 / d);  // これってバグらないですか？(精度的な意味で
            return (int)(n * 47 / d);  // オーバーフローしたらバグるかも！！！！
        }



        public static bool operator >(Frac a, Frac b)
        {
            return a.n * b.d > b.n * a.d;
        }
        public static bool operator <(Frac a, Frac b)
        {
            return a.n * b.d < b.n * a.d;
        }


        /*public static Frac operator -(Frac a, Frac b)
        {
            Frac c = new Frac(b);
            b.n = -b.n;
            c.Add(b);
            return c;
        }*/

    }
}
