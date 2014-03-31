using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    class WaveFileReaderWithSilence : IDisposable
    {
        WaveFileReader r;
        bool disposed = false;

        public int SamplingRate { get { return r.SamplingRate; } }
        public long SamplesCount { get { return r.SamplesCount; } }
        public int BitDepth { get { return r.BitDepth; } }
        public int ChannelsCount { get { return r.ChannelsCount; } }

        int readpre = 0;
        int readpost = 0;

        int SILENCE_PRECOUNT = 44100 * 5;
        int SILENCE_POSTCOUNT = 44100 * 5;

        ~WaveFileReaderWithSilence()
        {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (this)  // スレッドセーフ・・・？？？？
            {
                if (this.disposed) return;

                this.disposed = true;

                if (disposing)
                {
                    r.Close();
                    r.Dispose();
                    r = null;
                }
            }
        }

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

        public WaveFileReaderWithSilence(String filename) :
            this(neu.IFileStream(filename, FileMode.Open, FileAccess.Read))
        {
        }

        public WaveFileReaderWithSilence(Stream strm)
        {
            r = new WaveFileReader(strm);
        }

        public bool ReadSample(out float val)
        {
            if (readpre < SILENCE_PRECOUNT)
            {
                val = 0;
                readpre++;
                return true;
            }
            else if (r.ReadSample(out val))
            {
                return true;
            }
            else if (readpost < SILENCE_POSTCOUNT)
            {
                val = 0;
                readpost++;
                return true;
            }
            return false;
        }
    }
}
