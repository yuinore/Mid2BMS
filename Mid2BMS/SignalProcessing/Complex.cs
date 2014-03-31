using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    struct Complex  // まあ・・・structだよね・・・
    {
        // ＞＞変更可能 (mutable) な値型を定義してはいけません。
        // ＞＞.NET のクラスライブラリ設計 (P.83)
        // ＞これは是非従うべき！
        // http://bleis-tift.hatenablog.com/entry/20100117/1263657366

        // double re;
        // double im;
        public readonly double Re;  // 公開されるメンバは大文字から始めるみたいな風潮
        public readonly double Im;
        // [Effective C#] 項目7 値型は不変かつアトミックにすること
        // http://blog.masakura.jp/node/31
        //???????????????

        //**************************************************************
        //*** constructor
        //**************************************************************
        //public Complex()  // エラー：構造体に明示的なパラメーターのないコンストラクターを含めることはできません。
        //{
        //    Re = Im = 0.0;
        //}
        public Complex(double re)
        {
            this.Re = re;
            Im = 0.0;
        }
        public Complex(double re, double im)
        {
            this.Re = re;
            this.Im = im;
        }

        //**************************************************************
        //*** methods
        //**************************************************************
        public double Abs()
        {
            return Math.Sqrt(Re * Re + Im * Im);
        }

        //**************************************************************
        //*** static methods
        //**************************************************************
        public static Complex Exph(double exponent)
        {
            // ハイパボリック(違
            return new Complex(Math.Cos(exponent), Math.Sin(exponent));
        }
        public static Complex Exp(Complex exponent)
        {
            if (exponent.Im == 0.0)
            {
                return new Complex(Math.Cos(exponent.Im), Math.Sin(exponent.Im));
            }
            else
            {
                double amp = Math.Exp(exponent.Re);
                return new Complex(amp * Math.Cos(exponent.Im), amp * Math.Sin(exponent.Im));
            }
        }

        //**************************************************************
        //*** operator
        //**************************************************************
        public static Complex operator +(Complex c1, Complex c2)
        {
            return new Complex(c1.Re + c2.Re, c1.Im + c2.Im);
        }
        public static Complex operator -(Complex c1, Complex c2)
        {
            return new Complex(c1.Re - c2.Re, c1.Im - c2.Im);
        }
        public static Complex operator *(Complex c1, Complex c2)
        {
            return new Complex(c1.Re * c2.Re - c1.Im * c2.Im, c1.Re * c2.Im + c1.Im * c2.Re);
        }
        public static Complex operator /(Complex c1, Complex c2)
        {
            double abs = c2.Re * c2.Re + c2.Im * c2.Im;
            double abs_inv = 1.0 / abs;
            return new Complex(
                abs_inv * (c1.Re * c2.Re + c1.Im * c2.Im),
                abs_inv * (-c1.Re * c2.Im + c1.Im * c2.Re));
        }
        //そして、Object.Equals() メソッドまたは operator== を実装するかどうかなんですが、値型の場合は必ず実装すべきとあります。
        //http://blog.masakura.jp/node/34
        //???????????????????
        public static Complex operator ==(Complex c1, Complex c2)
        {
            throw new NotImplementedException();
        }
        public static Complex operator !=(Complex c1, Complex c2)
        {
            throw new NotImplementedException();
        }

        //**************************************************************
        //*** cast
        //**************************************************************
        public static implicit operator Complex(double d)
        {
            return new Complex(d);
        }

        //**************************************************************
        //*** ToString
        //**************************************************************
        public override string ToString()
        {
            return Re + "+" + Im + "i";
        }
    }
}
