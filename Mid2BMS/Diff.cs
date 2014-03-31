using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    class Diff
    {
        // diffの動作原理を知る～どのようにして差分を導き出すのか http://gihyo.jp/dev/column/01/prog/2011/diff_sd200906
        // 最長共通部分列問題 (Longest Common Subsequence) http://d.hatena.ne.jp/naoya/20090328/1238251033
        // 文書比較（diff）アルゴリズム http://hp.vector.co.jp/authors/VA007799/viviProg/doc5.htm
        //
        // O(MN)のアルゴリズム(遅いらしい)
        // メモリ量がO(M+N)のアルゴリズムを書くのすらめんどい
        // 
        // ・・・と思ったらクソみたいに計算時間が早くて草
        // N=M=1000程度じゃ何でもないのか・・・
        // MN = 442,127,424 だとさすがに遅かった。というかメモリが足りてない。
        // というわけで、せめて配列はO(M+N)で確保しましょう
        //
        // 計測の誤差は0.005秒くらいで意外と誤差無かった。
        //
        // MN = 207,501,248 だと 5.142 秒かかった。(LongestCommonSubsequence == 19)
        //
        // メモリ量減らしても 4.911 秒にしかならなかった。&1が遅いのかもしれない
        //
        // 配列を１次元にしたら 2.626秒 になった。多次元配列(ジャグ配列ではない)は遅い、ちぃ覚えた。
        //
        // table1[j + 1] = table1[j] > table0[j + 1] ? table1[j] : table0[j + 1];
        // にしたら 3.190 秒になった。比較は遅くて Math.Max()は速い。インライン化されるし。
        //
        // String型の比較をint型の比較に置き換えたら 2.028秒になった。文字列の比較は数値の比較より遅い。①
        //
        // 配列の数を2本から1本に変えたら 2.249秒 になった。長くなったぞ。
        // 連続した変数(u, v)への代入はパイプライン処理が出来ないから遅いってことだろうか。②
        //
        // ①に対してforeachを使ったところ 1.898秒になった。tableのswapを追加した。foreachというよりx[i]が遅いっぽい。 ③
        // ②に対してforeachを使ったところ 2.087秒になった。ちょっとだけ遅い。
        //
        // ③に対して for (int j = 0; j < y.Length; j++) を
        // for (int j = 1; j < table1.Length; j++)
        // に修正したら、境界チェックの回数が減って 1.842秒になった。
        //
        // y[j + 1] を y[j] にしたら 1.823秒 になった。そろそろ計測誤差が怪しくなってくる。
        // それから初期化処理の時間も気になってくる。
        //
        // ちょっと高速化が楽しくなってしまって、こんな無駄なことに時間を費やしてしまった
        // どうせもっと高速なアルゴリズム使うのにー
        //
        // ここまで書いてから、LCS長を求めただけではLCSを求めることが出来ないことに気付いた
        //
        // たった3万回の再帰も出来ないとか・・・無いわーーー
        private static List<String> PrintLCS(String[] x, String[] y, int[,] table, int i, int j)
        {
            if (i == 0 || j == 0) { return new List<String>(); }

            if (x[i - 1] == y[j - 1])
            {
                var seq = PrintLCS(x, y, table, i - 1, j - 1);
                seq.Add(x[i - 1]);
                return seq;
            }
            else
            {
                if (table[i - 1, j] >= table[i, j - 1])
                {
                    return PrintLCS(x, y, table, i - 1, j);
                }
                else
                {
                    return PrintLCS(x, y, table, i, j - 1);
                }
            }
        }

        public static String[] LongestCommonSubsequence(String[] x, String[] y)
        {
            int[,] table = new int[x.Length + 1, y.Length + 1];

            for (int i = 0; i < x.Length; i++)
            {
                for (int j = 0; j < y.Length; j++)
                {
                    if (x[i] == y[j])
                    {
                        table[(i + 1), j + 1] = table[i, j] + 1;
                    }
                    else
                    {
                        table[(i + 1), j + 1] = Math.Max(
                            table[(i + 1), j],
                            table[i, j + 1]);
                    }
                }
            }

            return PrintLCS(x, y, table, x.Length, y.Length).ToArray();
        }

        // >Shortest Common Superstring: find shortest string that contains all given string fragments
        // http://programmers.stackexchange.com/questions/166094/shortest-common-superstring-find-shortest-string-that-contains-all-given-strin
        // >What you're asking about is the Shortest Common Superstring problem, for which there is no algorithm that works for all cases.
        // SCSを求めるアルゴリズムは無いらしい！！！！
        /// <summary>
        /// 最短共通超文字列(Shortest Common Superstring)を返します。
        /// ただし SCS(SCS(x,y),z) は SCS(x,y,z) と一致するとは限りません。
        /// </summary>
        /// <returns></returns>
        public static String[] ShortestCommonSuperstring(String[] x, String[] y)
        {
            var LCS = LongestCommonSubsequence(x, y);
            int i = 0, j = 0, k = 0;
            x = x.Concat(new String[1] { null }).ToArray(); // メモリの無駄
            y = y.Concat(new String[1] { null }).ToArray();
            LCS = LCS.Concat(new String[1] { null }).ToArray();
            List<String> SCS = new List<String>();

            while (i < x.Length || j < y.Length)
            {
                if (x[i] == LCS[k] && y[j] == LCS[k])
                {
                    SCS.Add(LCS[k]);
                    i++; j++; k++;
                }
                else if (x[i] == LCS[k])
                {
                    SCS.Add(y[j]);
                    j++;
                }
                else if (y[j] == LCS[k])
                {
                    SCS.Add(x[i]);
                    i++;
                }
                else
                {
                    SCS.Add(x[i]);
                    i++;
                }
            }

            return SCS.ToArray();
        }

        /*public static int LongestCommonSubsequence(String[] x2, String[] y2)
        {
            int[] table0 = new int[y2.Length + 1];
            int[] table1 = new int[y2.Length + 1];
            int[] temp;

            Dictionary<String, int> dict = new Dictionary<String, int>();
            int id = 0;
            foreach (var a in x2.Concat(y2).Distinct()) { dict.Add(a, id++); }
            int[] x = new int[x2.Length];
            int[] y = new int[y2.Length + 1];
            for (int i = 0; i < x.Length; i++) { x[i] = dict[x2[i]]; }
            for (int i = 1; i < y.Length; i++) { y[i] = dict[y2[i - 1]]; }
            x2 = y2 = null;
            dict = null;

            foreach (int xi in x)
            {
                for (int j = 1; j < table1.Length; j++)
                {
                    if (xi == y[j])
                    {
                        table1[j] = table0[j - 1] + 1;
                    }
                    else
                    {
                        table1[j] = Math.Max(table1[j - 1], table0[j]);
                    }
                }

                temp = table0;
                table0 = table1;
                table1 = temp;
            }

            return table0[y.Length - 1];
        }*/


        /*
        public static int LongestCommonSubsequence(String[] x2, String[] y2)
        {
            int[] table0 = new int[y2.Length + 1];
            int[] table1 = new int[y2.Length + 1];
            int[] temp;

            Dictionary<String, int> dict = new Dictionary<String, int>();
            int id = 0;
            foreach (var a in x2.Concat(y2).Distinct()) { dict.Add(a, id++); }
            int[] x = new int[x2.Length];
            int[] y = new int[y2.Length];
            for (int i = 0; i < x.Length; i++) { x[i] = dict[x2[i]]; }
            for (int i = 0; i < y.Length; i++) { y[i] = dict[y2[i]]; }
            x2 = y2 = null;
            dict = null;

            foreach (int xi in x)
            {
                for (int j = 0; j < y.Length; j++)
                {
                    if (xi == y[j])
                    {
                        table1[j + 1] = table0[j] + 1;
                    }
                    else
                    {
                        table1[j + 1] = Math.Max(table1[j], table0[j + 1]);
                    }
                }

                temp = table0;
                table0 = table1;
                table1 = temp;
            }
        
            return table0[y.Length - 1];
        }*/


        /*public static int LongestCommonSubsequence(String[] x2, String[] y2)
        {
            int[] table = new int[y2.Length + 1];

            Dictionary<String, int> dict = new Dictionary<String, int>();
            int id = 0;
            foreach (var a in x2.Concat(y2).Distinct()) { dict.Add(a, id++); }
            int[] x = new int[x2.Length];
            int[] y = new int[y2.Length];
            for (int i = 0; i < x.Length; i++) { x[i] = dict[x2[i]]; }
            for (int i = 0; i < y.Length; i++) { y[i] = dict[y2[i]]; }
            x2 = y2 = null;

            int u = 0, v = 0;

            //for (int i = 0; i < x.Length; i++)
            foreach (int xi in x)
            {
                for (int j = 0; j < y.Length; j++)
                {
                    u = v;  // this is table0[j]
                    v = table[j + 1];  // this is table0[j]

                    if (xi == y[j])
                    {
                        table[j + 1] = u + 1;
                    }
                    else
                    {
                        table[j + 1] = Math.Max(table[j], v);
                    }
                }
            }

            return table[y.Length];
        }*/
        

        /*
        public static int LongestCommonSubsequence(String[] x, String[] y)
        {
            int[,] table = new int[2, y.Length + 1];

            for (int i = 0; i < x.Length; i++)
            {
                for (int j = 0; j < y.Length; j++)
                {
                    if (x[i] == y[j])
                    {
                        table[(i + 1) & 1, j + 1] = table[i & 1, j] + 1;
                    }
                    else
                    {
                        table[(i + 1) & 1, j + 1] = Math.Max(
                            table[(i + 1) & 1, j],
                            table[i & 1, j + 1]);
                    }
                }
            }

            return table[x.Length & 1, y.Length];
        }
         */
    }
}
