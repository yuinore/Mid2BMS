using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    class Wos
    {
        public Wos(string filename)
            : this(new FileStream(filename, FileMode.Open, FileAccess.Read))
        {
        }

        const int LENGTH_WOS2 = 49160;
        const int LENGTH_WOS3 = 6168;

        public int Version;
        public double BPM;
        public int[] CutPoints;  // 小節を192に分割
        
        public Wos(Stream stream)
        {
            var cutPointsList = new List<int>();

            using (BinaryReader r = new BinaryReader(stream))
            {
                if (stream.Length == LENGTH_WOS2)
                {
                    // WoslicerII
                    Version = 2;

                    BPM = r.ReadByte() * 256;
                    BPM = BPM + r.ReadByte();
                    BPM = BPM + r.ReadByte() * 0.01;

                    r.ReadBytes(4);

                    for (int i = 0; i < (LENGTH_WOS2 - 7); i++)
                    {
                        if (r.ReadByte() != 0)
                        {
                            cutPointsList.Add(i);
                        }
                    }
                }
                else if (stream.Length == LENGTH_WOS3)
                {
                    // WoslicerIII
                    Version = 3;

                    BPM = r.ReadDouble();
                    r.ReadDouble();  // ずらし
                    r.ReadInt32();   // フェード
                    r.ReadInt32();   // ノイズゲート量(dB)

                    for (int i = 0; i < (LENGTH_WOS3 - 24); i++)
                    {
                        var data = r.ReadByte();
                        if ((data & 128) > 0) cutPointsList.Add(i * 8 + 0);
                        if ((data & 64) > 0) cutPointsList.Add(i * 8 + 1);
                        if ((data & 32) > 0) cutPointsList.Add(i * 8 + 2);
                        if ((data & 16) > 0) cutPointsList.Add(i * 8 + 3);
                        if ((data & 8) > 0) cutPointsList.Add(i * 8 + 4);
                        if ((data & 4) > 0) cutPointsList.Add(i * 8 + 5);
                        if ((data & 2) > 0) cutPointsList.Add(i * 8 + 6);
                        if ((data & 1) > 0) cutPointsList.Add(i * 8 + 7);
                    }
                }
                else
                {
                    throw new Exception(".wosファイルが対応していない形式です。");
                }
            }

            CutPoints = cutPointsList.ToArray();
        }
    }
}
