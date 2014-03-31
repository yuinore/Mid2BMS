using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    class IIRFilter : DigitalFilter
    {
        protected double[] an;  // yの係数
        protected double[] bn;  // xの係数
        double[] xn;
        double[] yn;

        protected IIRFilter()  // protected と sealed を間違えた
        {
        }

        public IIRFilter(double[] an, double[] bn) {
            this.an = (double[])an.Clone();
            this.bn = (double[])bn.Clone();
            Initialize();
        }

        protected void Initialize()
        {
            xn = new double[bn.Length];
            yn = new double[an.Length];

            if (this.an.Length != this.bn.Length) throw new Exception("うきゅ～");

            double a0_inv = 1.0 / an[0];

            for (int i = 0; i < an.Length; i++)
            {
                an[i] *= a0_inv;
                bn[i] *= a0_inv;
            }
        }

        public double Process(double val)
        {
            //http://www2.ic.sie.dendai.ac.jp/digital/%E8%AC%9B%E7%BE%A913.pdf
            //http://www2.ic.sie.dendai.ac.jp/digital/%E8%AC%9B%E7%BE%A913.pdf


            /*
            // step1 バッファのシフト(delay)を行う
            for (int i = 0; i < an.Length - 1; i++)
            {
                xn[i + 1] = xn[i];
                yn[i + 1] = yn[i];
            }
            xn[0] = val;
            yn[0] = 0.0;  // 重要

            // step2 yn[0] を求める
            double y0 = 0.0;

            for (int i = 0; i < an.Length; i++)
            {
                y0 += bn[i] * xn[i] - an[i] * yn[i];
            }

            yn[0] = y0;

            return y0;
             * */


            double t = val;
            double y = 0;

            for (int i = an.Length - 2; i >= 0; i++)
            {
                xn[i + 1] = xn[i];
            }

            //an[0] = 0;// -1;// -0.4;// 0.04;
            //xn[0] = 0;// 0.1 * val;
            for (int i = 1; i < an.Length; ++i)
            {
                t += -xn[i] * an[i];
                y += xn[i] * bn[i];
            }

            y += bn[0] * t;
            xn[0] = t;

            return y;
        }

        public double CharacteristicCurve(double _2pi_normalized_frequency)
        {
            Polynomial nume = new Polynomial(bn, "zinv");
            Polynomial deno = new Polynomial(an, "zinv");

            Complex zinv = Complex.Exph(-_2pi_normalized_frequency); // z^-1 = e^(-iω)

            Complex amp = nume.代入(zinv) / deno.代入(zinv);

            return amp.Abs();
        }

        public void Reset()
        {
            for (int i = 0; i < xn.Length; i++)
            {
                xn[i] = 0;
                yn[i] = 0;
            }
        }

        /// <summary>
        /// bufはコピーされません
        /// </summary>
        /// <returns></returns>
        public DigitalFilter Clone()
        {
            var f = (IIRFilter)this.MemberwiseClone();
            f.xn = new double[bn.Length];
            f.yn = new double[an.Length];
            return f;
        }
    }
}
