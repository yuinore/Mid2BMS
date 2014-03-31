using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    /// <summary>
    /// いまさら聞けない、IDisposableインターフェイス
    /// http://d.hatena.ne.jp/zecl/20080226/p2
    /// </summary>
    class WaveFileWriter : IDisposable
    {
        private BinaryWriter w;
        private bool disposed = false;

        private int bitDepth = 16;
        private int channelsCount = 2;
        
        int wrotesamples = 0;

        byte[] HeaderData = new byte[44] {
            (byte)'R',(byte)'I',(byte)'F',(byte)'F',
            0, 0, 0, 0,
            (byte)'W',(byte)'A',(byte)'V',(byte)'E',
            (byte)'f',(byte)'m',(byte)'t',(byte)' ',
            16, 0, 0, 0,
            1, 0, 2, 0,
            0x44, 0xAC, 0x00, 0x00,
            0x10, 0xB1, 0x02, 0x00,
            0x04, 0x00, 0x10, 0x00,
            (byte)'d',(byte)'a',(byte)'t',(byte)'a',
            0, 0, 0, 0,
        };

        ~WaveFileWriter()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// なんでvirtualなんだろう・・・？
        /// 
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            lock (this)  // スレッドセーフ・・・？？？？
            {
                if (this.disposed) return;

                //WriteWaveFileSizeAndClose();  // 他の参照オブジェクトにはアクセスするなって書いてあるだろ！！！（？）

                this.disposed = true;

                if (disposing)
                {
                    WriteWaveFileSizeAndClose();
                    w.Dispose();
                    w = null;
                }
#if DEBUG
                else
                {
                    throw new Exception("激おこ");
                }
#endif
            }
        }

        /// <summary>
        /// Dispose
        /// 何度呼ばれても問題ない
        /// </summary>
        public void Dispose()
        {
            //マネージリソースおよびアンマネージリソースの解放
            this.Dispose(true);

            //ガベコレから、このオブジェクトのデストラクタを対象外とする
            GC.SuppressFinalize(this);
        }

        public void Close()
        {
            Dispose();
        }

        public WaveFileWriter(String filename) :
            this(neu.IFileStream(filename, FileMode.Create, FileAccess.Write))
        {
        }

        /// <summary>
        /// File.Open(nm, FileMode.Create, FileAccess.Write)
        /// </summary>
        public WaveFileWriter(Stream strm)
        {
            w = new BinaryWriter(strm);
            w.Write(HeaderData);
        }

        public WaveFileWriter(String filename, int channelsCount, int samplingRate, int bitDepth) :
            this(
                neu.IFileStream(filename, FileMode.Create, FileAccess.Write),
                channelsCount, samplingRate, bitDepth)
        {
        }

        public WaveFileWriter(Stream strm, int channelsCount, int samplingRate, int bitDepth)
        {
            this.bitDepth = bitDepth;
            this.channelsCount = channelsCount;

            if (bitDepth != 8 && bitDepth != 16 && bitDepth != 24 && bitDepth != 32) throw new Exception(bitDepth + "bit wavの書き出しは対応しておりません");

            w = new BinaryWriter(strm);
            w.Write(HeaderData, 0, 16);
            w.Write((Int32)(bitDepth <= 24 ? 16 : 18));  // chunk size
            w.Write((Int16)(bitDepth <= 24 ? 0x0001 : 0x0003));  // format id                    
            // 0x0001 ... PCM
            // 0x0003 ... WAVE_FORMAT_IEEE_FLOAT
            w.Write((Int16)channelsCount);
            w.Write((Int32)samplingRate);
            w.Write((Int32)(samplingRate * (bitDepth >> 3) * channelsCount));  // data speed (bytes/sec)
            w.Write((Int16)((bitDepth >> 3) * channelsCount));  // block size
            w.Write((Int16)bitDepth);
            if (bitDepth > 24)
            {
                w.Write((Int16)0);
            }
            w.Write(HeaderData, 36, 8);
        }


        /// <summary>
        /// Dispose(bool disposing) から呼び出されます。
        /// </summary>
        private void WriteWaveFileSizeAndClose()
        {
            if (bitDepth <= 24)
            {
                UInt32 filesize_bytes = (UInt32)(wrotesamples * (bitDepth >> 3)) + 44u;

                w.Seek(4, SeekOrigin.Begin);
                w.Write((UInt32)(filesize_bytes - 8u));
                w.Seek(40, SeekOrigin.Begin);
                w.Write((UInt32)(filesize_bytes - 44u));
            }
            else
            {
                UInt32 filesize_bytes = (UInt32)(wrotesamples * (bitDepth >> 3)) + 46u;

                w.Seek(4, SeekOrigin.Begin);
                w.Write((UInt32)(filesize_bytes - 8u));
                w.Seek(42, SeekOrigin.Begin);
                w.Write((UInt32)(filesize_bytes - 44u));
            }
            w.Close();
        }

        /// <summary>
        /// ステレオの場合は、呼び出し回数が偶数になるよう注意してください。
        /// 例外：EndOfStreamException
        /// </summary>
        /// <returns></returns>
        public float WriteSample(float val)
        {
            if (this.disposed) { throw new ObjectDisposedException(this.GetType().ToString()); }

            wrotesamples++;

            switch (bitDepth)
            {
                case 8:
                    {
                        int intval = (int)((val + 1.0f) * 128.0);

                        if (intval > 255) intval = 255;
                        if (intval < 0) intval = 0;

                        w.Write((SByte)intval);
                    }
                    break;
                case 16:
                    {
                        int intval = (int)(val * 32768.0f);

                        if (intval > 32767) intval = 32767;
                        if (intval < -32768) intval = -32768;

                        w.Write((Int16)intval);
                    }
                    break;
                case 24:
                    {
                        float floatval = val;
                        if (floatval > 1.0f) floatval = 1.0f;
                        if (floatval < -1.0f) floatval = -1.0f;

                        int intval = (int)(floatval * 8388608.0f);
                        if (intval > 8388607) intval = 8388607;
                        if (intval < -8388608) intval = -8388608;

                        w.Write((Byte)intval);
                        w.Write((Byte)(intval >> 8));
                        w.Write((Byte)(intval >> 16));
                    }
                    break;
                case 32:
                    w.Write((Single)val);  // これじゃダメなんですか
                    //w.Write(BitConverter.GetBytes((Single)val));
                    break;
            }

            return val;
        }

        public static void WriteAllSamples(String filename, float[][] buf)
        {
            WriteAllSamples(neu.IFileStream(filename, FileMode.Create, FileAccess.Write), buf);
        }
        public static void WriteAllSamples(Stream strm, float[][] buf)
        {
            WaveFileWriter ww = new WaveFileWriter(strm);

            for (int i = 0; i < buf[0].Length; i++)
            {
                for (int ch = 0; ch < buf.Length; ch++)
                {
                    ww.WriteSample(buf[ch][i]);
                }
            }

            ww.Close();  // よくIDEに「激おこ」って言われます
        }

        public static void WriteAllSamples(String filename, float[][] buf,
            int channelsCount, int samplingRate, int bitDepth)
        {
            WriteAllSamples(neu.IFileStream(filename, FileMode.Create, FileAccess.Write), buf,
                channelsCount, samplingRate, bitDepth);
        }
        public static void WriteAllSamples(Stream strm, float[][] buf,
            int channelsCount, int samplingRate, int bitDepth)
        {
            WaveFileWriter ww = new WaveFileWriter(strm, channelsCount, samplingRate, bitDepth);

            for (int i = 0; i < buf[0].Length; i++)
            {
                for (int ch = 0; ch < buf.Length; ch++)
                {
                    ww.WriteSample(buf[ch][i]);
                }
            }

            ww.Close();  // よくIDEに「激おこ」って言われます
        }
    }
}
