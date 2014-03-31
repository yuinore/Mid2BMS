using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    class Polynomial : List<double>
    {
        public String Variable
        {
            get;
            set;
        }

        /// <summary>
        /// degree次の多項式を0で初期化します。
        /// 第一引数が Count ではなくdegree (== Count - 1) なので間違えやすい。
        /// 後で仕様変更しようかなあ・・・？？
        /// </summary>
        /// <param name="degree"></param>
        /// <param name="Variable"></param>
        public Polynomial(int degree, String Variable)
            : base(new double[degree + 1])  // この書き方、すごく間違ってる気がする・・・！！助けて・・・！！
        {
            this.Variable = Variable;
        }           

        public Polynomial(IEnumerable<double> collection, String Variable)
            : base(collection)
        {
            this.Variable = Variable;
        }

        public static Polynomial operator*(Polynomial p1, Polynomial p2)
        {
            if (p1.Variable != p2.Variable)
            {
                // 片方が定数なら例外投げなくても良いかも？
                throw new Exception(p1.ToString() + " and " + p2.ToString() + " have different variable.");
            }

            Polynomial p3 = new Polynomial(p1.Count + p2.Count - 1 - 1, p1.Variable);

            // いわゆる畳み込み
            for (int i = 0; i < p1.Count; i++)
            {
                for (int j = 0; j < p2.Count; j++)
                {
                    p3[i + j] += p1[i] * p2[j];
                }
            }

            return p3;
        }

        public static Polynomial operator *(Polynomial p1, double d)
        {
            return d * p1;
        }
        public static Polynomial operator *(double d, Polynomial p1)
        {
            Polynomial p3 = new Polynomial(p1, p1.Variable);
            for (int i = 0; i < p1.Count; i++)
            {
                p3[i] *= d;
            }
            return p3;
        }

        public static Polynomial operator +(Polynomial p1, Polynomial p2)
        {
            if (p1.Variable != p2.Variable)
            {
                // 片方が定数なら例外投げなくても良いかも？
                throw new Exception(p1.ToString() + " and " + p2.ToString() + " have different variable!");
            }

            Polynomial p3 = new Polynomial(Math.Max(p1.Count, p2.Count) - 1, p1.Variable);

            for (int i = 0; i < p1.Count; i++) p3[i] += p1[i];
            for (int i = 0; i < p2.Count; i++) p3[i] += p2[i];

            return p3;
        }

        public double 代入(double val)
        {
            double ret = 0;

            // 素朴な実装(なんか遅そう)
            for (int i = 0; i < this.Count; i++)
            {
                ret += this[i] * Math.Pow(val, i);
            }

            return ret;
        }

        public Complex 代入(Complex val)
        {
            Complex ret = 0.0;  // キャスト可否の判定ってグラフ探索・・・
            Complex pow = 1.0;

            // 素朴な実装(なんか遅そう)
            for (int i = 0; i < this.Count; i++)
            {
                ret += this[i] * pow;
                pow *= val;
                // 豆餅おいしい
            }

            return ret;
        }

        public Polynomial 代入(Polynomial val)
        {
            Polynomial ret = new Polynomial(new double[] { 0 }, val.Variable);  // キャスト可否の判定ってグラフ探索・・・
            Polynomial pow = new Polynomial(new double[] { 1 }, val.Variable);

            // 素朴な実装(なんか遅そう)
            for (int i = 0; i < this.Count; i++)
            {
                ret += this[i] * pow;
                pow *= val;
            }

            return ret;
        }

        public Polynomial 代入(Polynomial nume, Polynomial deno)
        {
            Polynomial ret = new Polynomial(new double[] { 0 }, nume.Variable);  // キャスト可否の判定ってグラフ探索・・・

            for (int i = 0; i < this.Count; i++)
            {
                ret += this[i] * nume.Pow(i) * deno.Pow(this.Count - i - 1);
            }

            return ret;
        }

        public Polynomial Pow(int n)
        {
            Polynomial ret = new Polynomial(new double[] { 1 }, this.Variable);
            for (int i = 0; i < n; i++)
            {
                ret *= this;
            }
            return ret;
        }

        public override string ToString()
        {
            StringSuruyatu s = "";
            for (int i = this.Count - 1; i >= 0; i--)
            {
                switch (i)
                {
                    case 0:
                        s += this[i].ToString();
                        break;
                    case 1:
                        if (this[i] == 0.0) break;
                        if (this[i] != 1.0)
                        {
                            s += this[i].ToString();
                        }
                        s += Variable;
                        if (i != 0) s += " + ";
                        break;
                    default:
                        if (this[i] == 0.0) break;
                        if (this[i] != 1.0)
                        {
                            s += this[i].ToString();
                        }
                        s += Variable;
                        s += "^";
                        s += i.ToString();
                        if (i != 0) s += " + ";
                        break;
                }
            }

            return s;
        }

    }
}
