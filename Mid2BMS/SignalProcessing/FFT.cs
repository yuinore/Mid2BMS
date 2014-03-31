using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    // さすがに、速度超重視のこの場所でComplex型を使うのはありえないです・・・
    
    // とりあえずdftで実装してからfftで実装
    // こういうのってnullチェックとかするものなの・・・？
    class FFT
    {
        int ex;
        int n;
        int mask;
        double[] cos;
        double[] sin;

        public FFT(int ex)
        {
            if (ex > 30) throw new ArgumentOutOfRangeException();
            this.ex = ex;
            this.n = 1 << ex;
            this.mask = this.n - 1;

            // ここでルックアップテーブルの作成
            cos = new double[n];  // 8*1024*2=16KB これキャッシュに全部載るでしょ
            sin = new double[n];

            for (int i = 0; i < n; i++)
            {
                cos[i] = Math.Cos(-2 * Math.PI * i / n);
                sin[i] = Math.Sin(-2 * Math.PI * i / n);
            }
        }

        public void Process(Complex[] src, Complex[] dst)
        {
            if (src == null) throw new ArgumentNullException();
            if (dst == null) throw new ArgumentNullException();
            if (src.Length != n) throw new ArgumentException();
            if (dst.Length != n) throw new ArgumentException();

            for (int j = 0; j < dst.Length; j++)
            {
                dst[j] = 0;
            }
            //int cnt = src.Length;
            // ↑このような最適化はされるのだろうか？？あ、配列って長さ固定か、なら最適化されそう
            // これがList<>だと話が変わりそうだけどListだと遅いだろうから良いや

            // http://d.hatena.ne.jp/issei_y/20090423/1240495143
            // >>配列はforよりforeachが早い。
            // そうだったのか

            // というか最適化って・・・
            // http://blog.masakura.jp/node/36

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    dst[j] += src[i] * new Complex(cos[(i * j) & mask], sin[(i * j) & mask]);
                }
            }
        }
    }
}
