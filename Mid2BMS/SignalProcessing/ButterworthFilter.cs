using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    class ButterworthFilter :DigitalFilter//: IIRFilter  // クラスの継承やりたかった（小学生並みの悔しさ）
    {
        // 先生！２次のフィルタを縦続接続するのが正しい実装方法だと思います！！
        List<Polynomial> cascadeH;
        List<double[]> cascadeAn;
        List<double[]> cascadeBn;
        public List<IIRFilter> cascadeIIR;////////////////////////////////////////////private

        double[] an;  // yの係数
        double[] bn;  // xの係数

        // 双一次変換 - 和歌山大学
        // http://www.wakayama-u.ac.jp/~kawahara/signalproc/TeXfiles/bilinearTrans.pdf

        public ButterworthFilter(FilterType ftype, int degree, double _2pi_normalized_cutoff)
            : base()
        {
            //degree = 10;

            an = null;// new double[] { 1, 0.5 };  // ynの係数
            bn = null;// new double[] { 0.5, 0 };  // xnの係数
            
            double analog_cutoff = Math.Tan(_2pi_normalized_cutoff / 2);  // プリワーピング
            Polynomial new_s = new Polynomial(new double[] { 0, 1.0 / analog_cutoff }, "s");

            cascadeH = new List<Polynomial>();

            if(ftype != FilterType.LowPass) throw new NotImplementedException();

            Polynomial Bn_s = null;  // H_an(s)の分母
            if (degree % 2 == 0)
            {
                Bn_s = new Polynomial(new double[] { 1 }, "s");  // Bn_s = (Polynomial)"1";
            }
            else
            {
                Bn_s = new Polynomial(new double[] { 1, 1 }, "s");  // Bn_s = (Polynomial)"s+1";
                cascadeH.Add(Bn_s.代入(new_s));
            }
            for (int k = 1; k <= degree / 2; k++)
            {
                double keisuu = -2 * Math.Cos((2 * k + degree - 1) * Math.PI / (2 * degree));
                Polynomial factor = new Polynomial(new double[] { 1, keisuu, 1 }, "s");
                cascadeH.Add(factor.代入(new_s));  // H_a(s) の逆数);

                Bn_s *= factor;
            }

            Polynomial Ha_s = Bn_s.代入(new_s);  // H_a(s) の逆数

            Polynomial Hd_z_nume = (new Polynomial(new double[] { 1, 1 }, "zinv")).Pow(degree);  // (1 + z^-1)^n
            Polynomial Hd_z_deno = Ha_s.代入(  // ここに誤りがあるらしいのだが・・・？
                new Polynomial(new double[] { 1, -1 }, "zinv"),
                new Polynomial(new double[] { 1, 1 }, "zinv"));  // s = 1 - z^-1 を代入

            //Hd_z_deno *= 0.11344;
            //Hd_z_nume *= 0.11344;

            Console.WriteLine(Hd_z_nume.ToString());
            Console.WriteLine("-------------------------");
            Console.WriteLine(Hd_z_deno.ToString());

            //***********縦続接続version4
            cascadeAn = new List<double[]>();  // 分母、ynの係数
            cascadeBn = new List<double[]>();

            cascadeIIR = new List<IIRFilter>();
            for (int i = 0; i < cascadeH.Count;i++)

            {
                cascadeAn.Add(cascadeH[i].代入(  // ここに誤りがある・・・？
                new Polynomial(new double[] { 1, -1 }, "zinv"),
                new Polynomial(new double[] { 1, 1 }, "zinv")).ToArray());
                cascadeBn.Add((new Polynomial(new double[] { 1, 1 }, "zinv")).Pow(cascadeH[i].Count - 1).ToArray());



                //cascadeIIR.Add(new IIRFilter(cascadeAn[i], cascadeBn[i]));
            }

            /*
cascadeAn[0][0] = 1;
cascadeAn[0][1] = -1.9364514230418;
cascadeAn[0][2] = 0.95640307027397;
cascadeBn[0][0] = 0.0049879118080443;
cascadeBn[0][1] = 0.0099758236160886;
cascadeBn[0][2] = 0.0049879118080443;
cascadeAn[1][0] = 1;    
cascadeAn[1][1] = -1.859356650305;
cascadeAn[1][2] = 0.87851397463768;
cascadeBn[1][0] = 0.0047893310831696;
cascadeBn[1][1] = 0.0095786621663391;
cascadeBn[1][2] = 0.0047893310831696;
cascadeAn[2][0] = 1;
cascadeAn[2][1] = -1.7984495526753;
cascadeAn[2][2] = 0.81697933897044;
cascadeBn[2][0] = 0.0046324465737799;
cascadeBn[2][1] = 0.0092648931475598;
cascadeBn[2][2] = 0.0046324465737799;
cascadeAn[3][0] = 1;
cascadeAn[3][1] = -1.7566424926409;
cascadeAn[3][2] = 0.774741532415;
cascadeBn[3][0] = 0.0045247599435221;
cascadeBn[3][1] = 0.0090495198870443;
cascadeBn[3][2] = 0.0045247599435221;
cascadeAn[4][0] = 1;
cascadeAn[4][1] = -1.7354333561354;
cascadeAn[4][2] = 0.75331387392403;
cascadeBn[4][0] = 0.0044701294471639;
cascadeBn[4][1] = 0.0089402588943278;
cascadeBn[4][2] = 0.0044701294471639;
            */

for (int i = 0; i < cascadeH.Count; i++)
{
    cascadeIIR.Add(new IIRFilter(cascadeAn[i], cascadeBn[i]));
}

            cascadeAn = cascadeBn = null;

            //***********

            //base.an = Hd_z_deno.ToArray();
            //base.bn = Hd_z_nume.ToArray();
            //base.Initialize();  // this.Initializeでも良いのか
        }

        public double Process(double val)
        {
            for (int i = 1; i <2+0* cascadeH.Count; i++)
            {
                val = cascadeIIR[i].Process(val);
            }
            return val;
        }

        public double CharacteristicCurve(double _2pi_normalized_frequency)
        {
            double ret = 1.0;
            for (int i = 0; i < cascadeH.Count; i++)
            {
                ret *= cascadeIIR[i].CharacteristicCurve(_2pi_normalized_frequency);
            }
            return ret;
        }

        public void Reset()
        {
            throw new InvalidOperationException();
        }
        public DigitalFilter Clone()
        {
            throw new InvalidOperationException();
        }
    }
}
