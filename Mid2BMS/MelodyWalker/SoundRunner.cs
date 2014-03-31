using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    class SoundRunner
    {
        /// <summary>
        /// 2007年7月に作成されました。
        /// TrackNamesには b_ を含める必要はありません。
        /// なぜかWaveSplitterではなくMelodyWalkerの一部となったようです。
        /// ちなみに音ゲーの凡人は2006年から2007年の3月にかけて作っていたようです。
        /// </summary>
        public int CreateText(String[] TrackNames, out String Text1)
        {
            StringBuilder s2;
            int i;

            s2 = new StringBuilder();
            for (i = 0; i < TrackNames.Length; i++)
            {
                s2.Append(
                        //"b_" + TrackNames[i] + "_0.wav\r\n"+
                        "" + TrackNames[i] + ".wav\r\n"+
                        "b_" + TrackNames[i] + "_3.\r\n"+
                        ".wav\r\n"+
                        "301\r\n"+
                        "300\r\n"+
                        "33075\r\n"+
                        "33075\r\n"+
                        "//\r\n");
            }
            Text1 = s2.ToString();

            return 0;
        }

        public int CreateTextWithExactInputFileNames(String FileName, out String Text1)
        {
            StringBuilder s2;
            int i;

            s2 = new StringBuilder();
            for (i = 0; i < 1; i++)
            {
                s2.Append(
                        "" + FileName + "\r\n" +
                        "b_omni_3.\r\n" +
                        ".wav\r\n" +
                        "301\r\n" +
                        "300\r\n" +
                        "33075\r\n" +
                        "33075\r\n" +
                        "//\r\n");
            }
            Text1 = s2.ToString();

            return 0;
        }
    }
}
