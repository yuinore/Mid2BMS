using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    class iBMSCClipboardData
    {
        List<double[]> data;

        public iBMSCClipboardData(String s)
        {
            String s2;

            using (var sr = new StringReader(s))
            {

                if (sr.ReadLine() != "iBMSC Clipboard Data xNT")
                {
                    throw new ArgumentException("渡された文字列が、僕の知らない物語 : \n\n" + s);
                }

                while ((s2 = sr.ReadLine()) != null)
                {
                    if (s2 == "") continue;

                    var s3 = s2.Split(' ');
                    if (s3.Length != 5) throw new ArgumentException("渡された文字列が、僕の知らない物語 : \n\n" + s);

                    data.Add(s3.Select(x => Convert.ToDouble(x)).ToArray());

                    // MidiTrack が List<MidiEvent> を継承しているのは間違っているのでは・・・？
                    // ということを Effective C++ を読んで思った

                    // そもそも MidiTrackというクラスが必要なかったという可能性・・・？？
                }
            }
        }

        public override string ToString()
        {
            using (var sr = new StringWriter())
            {
                foreach (var line in data)
                {
                    sr.WriteLine(String.Join(" ", line.Select(x => (int)x)));
                }

                return sr.ToString();
            }
        }
    }
}