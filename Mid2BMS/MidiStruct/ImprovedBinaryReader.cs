using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Mid2BMS
{
    // 2013/06/19
    // Expand BinaryReader
    // C#で継承使ったの初めてかもしれない・・・
    class ImprovedBinaryReader : BinaryReader, IDisposable
    {
        public ImprovedBinaryReader(Stream stream_)
            : base(stream_)
        {
        }

        public int ReadDeltaTime()  // midiの読み込みではこちら。（間違いの可能性あり）
        {
            // Long形だけど大丈夫だよね！むしろjava scriptのfloat（？）のほうが心配だお
            byte ret;
            int retS = 0;
            while (true)
            {
                ret = base.ReadByte();
                retS = (retS << 7) + (ret & 0x7F);
                if ((ret & 0x80) == 0) break;
            }
            return retS;
        }
        public int ReadDeltaTimeBigEndian() // flpの読み込みではこちら。
        {
            // Long形だけど大丈夫だよね！むしろjava scriptのfloat（？）のほうが心配だお
            byte ret;
            int retS = 0;
            int shiftbit = 0;
            while (true)
            {
                ret = base.ReadByte();
                retS = retS + ((ret & 0x7F) << shiftbit);
                shiftbit += 7;
                if ((ret & 0x80) == 0) break;
            }
            return retS;
        }
        public int SeekForward(int siz)
        {
            for (int i = 0; i < siz; i++)
            {
                base.ReadByte();
            }
            return 0;
        }

        /*public int SeekBackward(int siz)
        {
            // MSDNより：
            //   読み取り中または BinaryReader の使用中に基になるストリームを使用すると、
            //   データの損失や破損の原因になることがあります。
            //   たとえば、同じバイトが 2 回以上読み取られたり、バイトが読み飛ばされたり、
            //   文字の読み取りが予期しない結果になることがあります。
            FileStream b = (FileStream)fp.BaseStream;
            b.Seek(-siz, SeekOrigin.Current);
            fp = new BinaryReader(b);
            return 0;
        }*/

        /// <summary>
        /// Int32 を ビッグエンディアン で読み込む
        /// </summary>
        /// <returns></returns>
        public int ReadBigInt32()
        {
            uint ret;
            ret = base.ReadUInt32();
            return (int)(((ret & (0xFF000000u)) >> 24) + ((ret & 0x00FF0000u) >> 8) + ((ret & 0x0000FF00u) << 8) + ((ret & 0x000000FFu) << 24));
        }
    }
}
