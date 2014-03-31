using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Mid2BMS
{
    /// <summary>
    /// 2008年の9月に作成されました。
    /// mmlを与えると、
    /// Q V N L の繰り返しの配列を文字列にしたものを返します。
    /// 多分、ステップ指定(%記号の付いたq及びL)しか対応してない
    /// </summary>
    class MidInterpreter
    {
        // こいつら、フィールドというよりローカル変数だからな。まあJavaScriptはオブジェクト指向じゃなかったから仕方ない
        bool ImaSuji = false;
        //bool ImaKazu = false;  // qの後とか
        char KazuType = 'l';  // l,q,v,o,n
        int MMLq = 0;
        int MMLv = 60;  // fixed
        int MMLo = 3;
        int MMLl = 0;  // NumTaihiを忘れずに
        int MMLw = 0;  // 真のLである。MMLlは、音符の長さを表し、Lで指定された値ではない。
        int MMLn = -1;  // n is 0 to 11 or -1 (usual)
        int tpm = 15360 * 4;
        int NumTaihi = 0;
        StringBuilder s2 = new StringBuilder();
        //s3 = "";
        bool trigger = false;

        /// <summary>
        /// 多分、数値の逆数指定かどうか。
        /// </summary>
        bool ExcEnabled = true;

        /// <summary>
        /// 多分、ゲート型の指定タイプで、falseならステップ指定、trueなら％指定。
        /// </summary>
        bool Mujirushi = false;

        //int NTime = 0;  /////////////////////

        char c, d, f;

        public int SetTimeBase(int timebase)
        {
            tpm = timebase * 4;
            return timebase;
        }

        int SetMMLValue(char key, int scale, int offset)
        {
            switch (key)
            {
                case 'q':
                    MMLq = MMLq * scale + offset;
                    break;
                case 'v':
                    MMLv = MMLv * scale + offset;
                    break;
                case 'o':
                    MMLo = MMLo * scale + offset;
                    break;
                case 'l':
                    MMLl = MMLl * scale + offset;
                    break;
                case 'w':
                    MMLw = MMLw * scale + offset;
                    break;
                case 'n':
                    MMLn = MMLn * scale + offset;
                    break;
                default:
                    throw new Exception("存在しないMML命令です : " + key);
            }
            return 0;
        }

        int GetMMLValue(char key)
        {
            switch (key)
            {
                case 'q':
                    return MMLq;
                case 'v':
                    return MMLv;
                case 'o':
                    return MMLo;
                case 'l':
                    return MMLl;
                case 'w':
                    return MMLw;
                case 'n':
                    return MMLn;
                default:
                    throw new Exception("存在しないMML命令です : " + key);
            }
        }

        /// <summary>
        /// is Next Number?
        /// </summary>
        bool NXN()
        {
            bool condition_a = ('0' <= d && d <= '9') || d == '%';
            bool condition_b = (d == '+' || d == '-') && (('0' <= f && f <= '9') || f == '%');

            return condition_a || condition_b;
        }

        //bool aler_gate = false;

        int fin()
        {

            // 通常s
            /*
              if(1<=MMLq&&MMLq<=200) {
              s2+=MMLn+" ____ "+MMLl+" ____ *"+(MMLq*MMLl/100)+" ____ "+MMLv+"\r\n";

              s3+="Sub{ l%"+parseInt(MMLq*MMLl/100)+" v60 n("+(MMLn)+") }"+(MMLl?(" l%"+MMLl+" r "):"")+"\r\n";
              }else {
              s2+=MMLn+" ____ "+MMLl+" ____ "+MMLq+" ____ "+MMLv+"\r\n";
              s3+="Sub{ l%"+MMLq+" v60 n("+(MMLn)+") }"+(MMLl?(" l%"+MMLl+" r "):"")+"\r\n";
              }
            */

            // 通常配列版
            //if (!aler_gate) {
            //    aler_gate = true; 
                //MessageBox.Show("ゲート型はステップ指定のみ有効");
            //}

            //if(MMLn<0)return;

            // 【この部分がコメント化されています。不具合があるかもしれません】21:08 2012/05/17
            //if (1 <= MMLq && MMLq <= 200 && false)
            //{
            //    // Q V N L
            //    s2.Append("" + (int)(MMLq * MMLl / 100) + "," + MMLv + "," + MMLn + "," + MMLl + ", ");
            //}
            //else
            //{
                s2.Append(MMLq);
                s2.Append(",");
                s2.Append(MMLv);
                s2.Append(",");
                s2.Append(MMLn);
                s2.Append("," );
                s2.Append(MMLl);
                s2.Append(",");
            //}

            // Arppegio版
            /*
              if(1<=MMLq&&MMLq<=200) {
              s2+=MMLn+" ____ "+MMLl+" ____ *"+(MMLq*MMLl/100)+" ____ "+MMLv+"\r\n";

              s3+="Sub{ v60 "+Arpeggio(NTime,NTime+parseInt(MMLq*MMLl/100),MMLn)+" }"+(MMLl?(" l%"+MMLl+" r "):"")+"\r\n";

              }else {
              s2+=MMLn+" ____ "+MMLl+" ____ "+MMLq+" ____ "+MMLv+"\r\n";
              s3+="Sub{ v60 "+Arpeggio(NTime,NTime+MMLq,MMLn)+" }"+(MMLl?(" l%"+MMLl+" r "):"")+"\r\n";
              }
            */


            //NTime += MMLl;
            trigger = false;

            return 0;
        }

        public String Process(String s)
        {
            //if(s2)return s2;

            s = s + "  r  ";
            for (int i = 0; i < s.Length - 2; i++)
            {
                c = s[i];
                d = s[i + 1];
                f = s[i + 2];

                if (c == '>')
                {
                    MMLo++;
                }
                if (c == '<')
                {
                    MMLo--;
                }

                if ('0' <= c && c <= '9')
                {
                    // c == '1', '2', ..., '9'
                    if (!ImaSuji)
                    {
                        SetMMLValue(KazuType, 0, (int)(c - '0'));
                        ImaSuji = true;
                    }
                    else
                    {
                        SetMMLValue(KazuType, 10, (int)(c - '0'));
                    }
                }
                else
                {
                    if (ImaSuji && ExcEnabled
                                       && (KazuType == 'l' || KazuType == 'q' || KazuType == 'w')
                                       && (GetMMLValue(KazuType) != 0)
                                       && (KazuType != 'q' || Mujirushi == false)
                                  ) { SetMMLValue(KazuType, 0, tpm / GetMMLValue(KazuType)); }
                    ImaSuji = false;
                }

                if (c == '^')
                {
                    NumTaihi += GetMMLValue(KazuType);
                }
                if (c == '!')
                {
                    ExcEnabled = true; Mujirushi = false;
                }
                if (c == '%')
                {
                    ExcEnabled = false; Mujirushi = false;
                }
                if (c == '+')
                {
                    MMLn++;
                }
                if (c == '-')
                {
                    MMLn--;
                }


                if (c == 'r')
                {
                    SetMMLValue(KazuType, 1, NumTaihi);
                    if (trigger) fin();
                    trigger = true;
                    ExcEnabled = true;
                    NumTaihi = 0;
                    MMLn = -1;  // kyufu
                    if (NXN())
                    {
                        KazuType = 'l';
                    }
                    else
                    {
                        MMLl = MMLw;
                    }
                }
                if (c == 'c') { SetMMLValue(KazuType, 1, NumTaihi); if (trigger)fin(); trigger = true; ExcEnabled = true; NumTaihi = 0; MMLn = 0 + MMLo * 12; if (NXN()) { KazuType = 'l'; } else { MMLl = MMLw; } }
                if (c == 'd') { SetMMLValue(KazuType, 1, NumTaihi); if (trigger)fin(); trigger = true; ExcEnabled = true; NumTaihi = 0; MMLn = 2 + MMLo * 12; if (NXN()) { KazuType = 'l'; } else { MMLl = MMLw; } }
                if (c == 'e') { SetMMLValue(KazuType, 1, NumTaihi); if (trigger)fin(); trigger = true; ExcEnabled = true; NumTaihi = 0; MMLn = 4 + MMLo * 12; if (NXN()) { KazuType = 'l'; } else { MMLl = MMLw; } }
                if (c == 'f') { SetMMLValue(KazuType, 1, NumTaihi); if (trigger)fin(); trigger = true; ExcEnabled = true; NumTaihi = 0; MMLn = 5 + MMLo * 12; if (NXN()) { KazuType = 'l'; } else { MMLl = MMLw; } }
                if (c == 'g') { SetMMLValue(KazuType, 1, NumTaihi); if (trigger)fin(); trigger = true; ExcEnabled = true; NumTaihi = 0; MMLn = 7 + MMLo * 12; if (NXN()) { KazuType = 'l'; } else { MMLl = MMLw; } }
                if (c == 'a') { SetMMLValue(KazuType, 1, NumTaihi); if (trigger)fin(); trigger = true; ExcEnabled = true; NumTaihi = 0; MMLn = 9 + MMLo * 12; if (NXN()) { KazuType = 'l'; } else { MMLl = MMLw; } }
                if (c == 'b') { SetMMLValue(KazuType, 1, NumTaihi); if (trigger)fin(); trigger = true; ExcEnabled = true; NumTaihi = 0; MMLn = 11 + MMLo * 12; if (NXN()) { KazuType = 'l'; } else { MMLl = MMLw; } }

                if (c == 'q')
                {
                    SetMMLValue(KazuType, 1, NumTaihi); if (trigger) fin();
                    ExcEnabled = true; NumTaihi = 0; Mujirushi = true;
                    KazuType = 'q';
                }
                if (c == 'o')
                {
                    SetMMLValue(KazuType, 1, NumTaihi); if (trigger) fin();
                    NumTaihi = 0;
                    KazuType = 'o';
                }
                if (c == 'l')
                {
                    SetMMLValue(KazuType, 1, NumTaihi); if (trigger) fin();
                    ExcEnabled = true; NumTaihi = 0;
                    KazuType = 'w';
                }
                if (c == 'v')
                {
                    SetMMLValue(KazuType, 1, NumTaihi); if (trigger) fin();
                    NumTaihi = 0;
                    KazuType = 'v';
                }
                if (c == '.')
                {
                    SetMMLValue(KazuType, 0, GetMMLValue(KazuType) * 3 / 2);
                }

            }
            return s2.ToString();
        }
    }
}