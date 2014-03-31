using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    /// <summary>
    /// StringBuilderを簡単に使うためのクラスです。
    /// StringBuilderとの違いは、Stringとのキャスト演算子が存在しないことです。
    /// s += s1 + s2; より s = s + s1 + s2; の方がいいと思いますが分かりません。
    /// </summary>
    class StringSuruyatuSafe
    {
        StringBuilder s;
        bool corrupted = false;
        static bool messageshown = false;

        public StringSuruyatuSafe()
        {
            s = new StringBuilder();
        }
        public StringSuruyatuSafe(String s0)
        {
            s = new StringBuilder(s0);
        }
        private StringSuruyatuSafe(StringSuruyatuSafe s0, bool corrupted_)
        {
            s = s0.s;
            corrupted = corrupted_;
        }

        public override string ToString()
        {
            return this.s.ToString();
        }

        public static StringSuruyatuSafe operator +(StringSuruyatuSafe s1, int s2)
        {
            return s1 + s2.ToString();
        }
        public static StringSuruyatuSafe operator +(StringSuruyatuSafe s1, double s2)
        {
            return s1 + s2.ToString();
        }
        public static StringSuruyatuSafe operator +(StringSuruyatuSafe s1, decimal s2)
        {
            return s1 + s2.ToString();
        }
        public static StringSuruyatuSafe operator +(StringSuruyatuSafe s1, String s2)
        {
            if (s1 == null) s1 = new StringSuruyatuSafe();
            try
            {
                if (s1.corrupted) throw new Exception("StringSuruyatu2エラー");
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
            return new StringSuruyatuSafe(s1, false);
        }

        // え、代入演算子ってオーバーロード出来ないのか

        /*
        public static explicit operator String(StringSuruyatu2 s0)
        {
            return s0.s.ToString();
        }
        public static explicit operator StringSuruyatu2(String s0)  // StringSuruyatu2 + Stringの演算でこのメソッドが呼ばれるバグ！？
        {
            return new StringSuruyatu2(s0);
        }*/
    }
}
