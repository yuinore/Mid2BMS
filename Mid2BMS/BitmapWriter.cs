using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    class BitmapWriter
    {
        BinaryWriter wr;
        int width;
        int height;
        int bytewidth;
        int bytepadding;

        public BitmapWriter(Stream s, int w, int h)
        {
            wr = new BinaryWriter(s);

            width = w;
            height = h;
            bytewidth = ((w * 3 + 3) / 4) * 4;
            bytepadding = bytewidth - width * 3;

            // BMPFILEHEADER
            wr.Write((byte)'B');
            wr.Write((byte)'M');
            wr.Write((uint)(bytewidth * height + 54));
            wr.Write((short)0);
            wr.Write((short)0);
            wr.Write((uint)54);

            // BITMAPINFOHEADER
            wr.Write((uint)40);  // header size
            wr.Write((uint)width);
            wr.Write((uint)height);
            wr.Write((short)1);
            wr.Write((short)24);  // bit depth
            wr.Write((uint)0);  // compression type
            wr.Write((uint)3780);
            wr.Write((uint)3780);
            wr.Write((uint)3780);
            wr.Write((uint)0);
            wr.Write((uint)0);
        }

        public void Write(uint rgb)
        {
            wr.Write((byte)(rgb >> 16));
            wr.Write((byte)(rgb >> 8));
            wr.Write((byte)(rgb));
        }

        public void WriteNewLine()
        {
            for (int i = 0; i < bytepadding; i++)
            {
                wr.Write((byte)0);
            }
        }

        public void Close() {
            wr.Close();
        }
    }
}
