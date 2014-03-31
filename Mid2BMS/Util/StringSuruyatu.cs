using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    /// <summary>
    /// This class is much useful, but much dangerous.
    /// 生きるのが面倒だったのでこのクラスを作りました。
    /// いずれバグを生むと思うので、みなさんは使わないでください。
    /// 
    /// 使い方:
    /// X += "abc";とすると、はじめに X に "abc" が追加された後、 X = X が実行されます。
    /// Y = (X + "cde"); という文は書かないでください。
    /// 書くと例外が起きます。
    /// </summary>
    class StringSuruyatu
    {
        StringBuilder s;
        bool corrupted = false;
        static bool messageshown = false;

        public StringSuruyatu()
        {
            s = new StringBuilder();
        }
        public StringSuruyatu(String s0)
        {
            s = new StringBuilder(s0);
        }
        private StringSuruyatu(StringSuruyatu s0, bool corrupted_)
        {
            s = s0.s;
            corrupted = corrupted_;
        }

        public override string ToString()
        {
            return (String)this;
        }

        public static StringSuruyatu operator +(StringSuruyatu s1, int s2)
        {
            return s1 + s2.ToString();
        }
        public static StringSuruyatu operator +(StringSuruyatu s1, double s2)
        {
            return s1 + s2.ToString();
        }
        public static StringSuruyatu operator +(StringSuruyatu s1, decimal s2)
        {
            return s1 + s2.ToString();
        }
        public static StringSuruyatu operator +(StringSuruyatu s1, String s2)
        {
            if (s1 == null) s1 = new StringSuruyatu();
            try
            {
                if (s1.corrupted) throw new Exception("StringSuruyatuエラー");
            }
            catch (Exception e)  // Releaseモードだった場合は例外を揉み消す
            {
                if (!messageshown)
                {
                    System.Windows.Forms.MessageBox.Show(e.ToString());
                    System.Windows.Forms.MessageBox.Show(s2.ToString());
                    messageshown = true;
                }
            }
            s1.s.Append(s2);
            s1.corrupted = true;
            return new StringSuruyatu(s1, false);
        }

        // え、代入演算子ってオーバーロード出来ないのか
        
        public static implicit operator String(StringSuruyatu s0)
        {
            return s0.s.ToString();
        }
        public static implicit operator StringSuruyatu(String s0)  // StringSuruyatu + Stringの演算でこのメソッドが呼ばれるバグ！？
        {
            return new StringSuruyatu(s0);
        }
    }
}
