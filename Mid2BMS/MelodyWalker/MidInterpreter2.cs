using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Mid2BMS
{
    /// <summary>
    /// 2008年の9月に作成されました。
    /// 2009年の9月に少し修正されました。
    /// 値リスト(Q,V,N,L)を与えると、擬似mmlを返します。
    /// </summary>
    class MidInterpreter2
    {
        /// <summary>
        /// Hint：tpm == ticks/bar == TimeBase*4
        /// </summary>
        int tpm = 15360 * 4;

        /// <summary>
        /// 最大分解能
        /// </summary>
        int MaxDenominator = 384;//384;
        int MaxTimeDenominator = 384;

        public int SetTimeBase(int timebase)
        {
            MaxDenominator = MaxTimeDenominator = timebase * 4;  // 信頼性に不安

            tpm = timebase * 4;
            return timebase;
        }

        public String walkOnAMelody_Godo(String s0, out StringSuruyatu ErrMsg)
        {  // sがstr、t1/t2小節がオフセ
            // 出力は
            ErrMsg = "";

            String s2pre = s0;
            //MessageBox.Show("s2\n\n" + ((s2pre.Length > 1000) ? s2pre.Substring(0, 1000) : s2pre));
            StringBuilder s3 = new StringBuilder("");
            int i, j;
            Frac Le;
            List<String> s2 = new List<String>(s2pre.Split(','));
            s2.RemoveAt(s2.Count - 1);
            //alert(s2);
            // Q V N L
            for (i = 0; i < s2.Count; i += 4)
            {
                Le = new Frac(Convert.ToInt32(s2[i + 0]), tpm);  // Qの値
                Le.Reduce();
                if (Le.d > MaxDenominator)
                {
                    //MessageBox.Show("ゲートが曖昧かもしれませんよ。");
                    //ErrMsg += "\r\n" + @"Not Quantized After : """ + s3.ToString().Substring(Math.Max(s3.Length - 300, 0)) + @"""";

                    // もうなんか良いや
                }
                Le.LimitDenominator(MaxDenominator);
                s3.Append("{ l" + (Le.d));  // Q 分母
                s3.Append(" v" + (s2[i + 1]));  // V
                if (Convert.ToInt32(s2[i + 2]) <= 0)
                {
                    s3.Append(" r");
                }
                else
                {
                    s3.Append(" o" + (Convert.ToInt32(s2[i + 2]) / 12));  // N /12
                    s3.Append("ccddeffggaab"[(Convert.ToInt32(s2[i + 2]) % 12)]);  // N %12
                    s3.Append(" + +  + + + "[(Convert.ToInt32(s2[i + 2]) % 12)]);
                }
                for (j = 1; j < Le.n; j++)
                {
                    s3.Append("^"); // Q 分子
                }
                s3.Append(" }");
                if (Convert.ToInt32(s2[i + 3]) > 0)
                {
                    Le = new Frac(Convert.ToInt32(s2[i + 3]), tpm);  // Lの値
                    Le.Reduce();  // この方法では誤差が蓄積する[要検証]
                    // だけどこのバグは直すのめんどい

                    if (Le.d > MaxTimeDenominator)
                    {
                        //MessageBox.Show("発音時間が曖昧かもしれませんよ。");
                        //ErrMsg += "\r\n" + "Not Quantized at : " + s3.ToString().Substring(Math.Max(s3.Length - 100, 0)) + "\r\n";

                        // もうなんか良いや
                    }
                    Le.LimitDenominator(MaxTimeDenominator);
                    s3.Append(" l" + (Le.d));  // L 分母
                    s3.Append(" r");
                    for (j = 1; j < Le.n; j++)
                    {
                        s3.Append("^"); // Q 分子
                    }
                }
            }
            //MessageBox.Show("s3\n\n" + ((s3.Length > 1000) ? s3.Substring(0, 1000) : s3));
            //document.all["falk"].pb2.value="参考までに：\n\r"+s3;
            return s3.ToString();//walkOnAMelodyV2(s3,t1,t2);
        }
    }
}