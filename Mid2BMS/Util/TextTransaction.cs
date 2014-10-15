using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    static class TextTransaction
    {
        /// <summary>
        /// 改行だけの要素は空とみなされますが、空白とタブ文字は空とは見なされません。2014/08/05に変更。<br></br>
        /// 使用例： SplitString("ABC\r\nDEF\r\n//\r\nGHI\r\n//\r\n\r\n//\r\n//\r\nJ\tK\tL\r\n\t\r\nMNO\r\n//\r\n", "\r\n", "\r\n//\r\n", StringSplitOptions.RemoveEmptyEntries)<br></br>
        /// 結果： {"ABC", "DEF"}, {"GHI"}, {"//", "J\tK\tL", "MNO"}
        /// </summary>
        public static String[][] SplitString(String s, String separatorInner, String separatorOuter, StringSplitOptions option)
        {
            List<String[]> s2 = new List<String[]>();
            {
                List<String> r3 = new List<String>(s.Split(new String[] { separatorOuter }, StringSplitOptions.None));
                if (option == StringSplitOptions.RemoveEmptyEntries)
                {
                    for (int i = 0; i < r3.Count; i++)
                    {
                        if (r3[i].Replace("\n", "").Replace("\r", "") /*.Replace(" ", "").Replace("\t", "")*/ == "")
                        {
                            r3.RemoveAt(i);
                            i--;
                        }
                    }
                }

                for (int i = 0; i < r3.Count; i++)
                {
                    List<String> s2i = new List<String>(r3[i].Split(new String[] { separatorInner }, StringSplitOptions.None));
                    if (option == StringSplitOptions.RemoveEmptyEntries)
                    {
                        for (int j = 0; j < s2i.Count; j++)  // 不要な要素の削除
                        {
                            if (s2i[j].Replace("\n", "").Replace("\r", "") /*.Replace(" ", "").Replace("\t", "")*/ == "")
                            {
                                s2i.RemoveAt(j);
                                j--;
                            }
                        }
                    }
                    s2.Add(s2i.ToArray());
                }
            }
            return s2.ToArray();
        }

        public static String JoinString(String[][] s, String separatorInner, String separatorOuter)
        {
            StringBuilder s2 = new StringBuilder();
            int i;

            for (i = 0; i < s.Length - 1; i++)
            {
                s2.Append(String.Join(separatorInner, s[i]));
                s2.Append(separatorOuter);
            }
            if (i < s.Length)
            {
                s2.Append(String.Join(separatorInner, s[i++]));
            }

            return s2.ToString();
        }
    }
}
