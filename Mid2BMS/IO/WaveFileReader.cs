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
    /// 32bitwavに対応してなかった・・・
    /// </summary>
    class WaveFileReader : IDisposable
    {
        private BinaryReader r;
        private long sample_remain = 0;

        /// <summary>サンプリングレートです。</summary>
        public int SamplingRate { get; private set; }

        /// <summary>ビット深度です。これは8の倍数です。</summary>
        public int BitDepth { get; private set; }

        /// <summary>チャンネル数です</summary>
        public int ChannelsCount { get; private set; }

        /// <summary>1チャンネルあたりのサンプル数です。SamplesCount / (double)SamplingRate が再生時間(秒)です。</summary>
        public long SamplesCount { get; private set; }

        private long headerPosition = 44;
        private Stream strm;

        #region IDisposableっぽいもの

        private bool disposed = false;

        ~WaveFileReader()
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

                this.disposed = true;

                if (disposing)
                {
                    r.Close();
                    r.Dispose();
                    r = null;
                }
                else
                {
                    throw new Exception("激おこ");
                }
            }
        }

        /// <summary>
        /// Dispose
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

        #endregion

        public WaveFileReader(String filename) :
            this(neu.IFileStream(filename, FileMode.Open, FileAccess.Read))
        {
        }

        public WaveFileReader(Stream strm)
        {
            r = new BinaryReader(strm);
            this.strm = strm;

            if (BitConverter.ToUInt32(r.ReadBytes(4), 0) != 0x46464952u)  // RIFF
                throw new Exception("waveファイルのヘッダが誤っている可能性があります");
            r.ReadBytes(4);  // filesize - 8

            if (BitConverter.ToUInt32(r.ReadBytes(4), 0) != 0x45564157u)  // WAVE
                throw new Exception("waveファイルのヘッダが誤っている可能性があります");

            while (true)
            {
                uint chunkName;

                if ((chunkName = BitConverter.ToUInt32(r.ReadBytes(4), 0)) == 0x61746164u) break; // data

                // data以外
                // というか、リトルエンディアンじゃないアーキテクチャだと死ぬのでは？
                int chunksize = (int)BitConverter.ToUInt32(r.ReadBytes(4), 0);  // なんでReadInt32使わないの？？？
                if (chunksize > 0xFFFFFF) throw new Exception("・・・ん？");

                if (chunkName == 0x20746D66u)  // "fmt "
                {
                    int formatid;
                    //if (chunksize != 40) throw new Exception("それは違うよ！");
                    formatid = r.ReadUInt16();
                    if (formatid != 1 && formatid != 0x0003)
                    {
                        //throw new Exception("それは違うよ！ formatid:(dec)" + formatid);  //フォーマットID
                    }
                    // 0x0001 ... PCM
                    // 0xFFFE ... WAVE_FORMAT_EXTENSIBLE  // 間違えた (24bit)
                    // 0x0003 ... WAVE_FORMAT_IEEE_FLOAT
                    ChannelsCount = r.ReadInt16();
                    SamplingRate = r.ReadInt32();
                    r.ReadInt32();  // データ速度
                    r.ReadInt16();  // ブロックサイズ

                    BitDepth = r.ReadInt16();  // ビット深度(16or32)
                    if (BitDepth != 8 && BitDepth != 16 && BitDepth != 24 && BitDepth != 32)
                    {
                        throw new Exception("8bitと16bitと24bitと32bit以外のwavには対応しておりません");
                    }

                    if (chunksize != 16)
                    {
                        int extSize = r.ReadInt16();
                        r.ReadBytes(extSize);
                    }
                }
                else
                {
                    r.ReadBytes(chunksize);
                }
            }
            // data
            int databytes = r.ReadInt32();// Convert.ToInt32(r.ReadBytes(4));  // dataチャンクのバイト数
            sample_remain = databytes / (BitDepth >> 3);
            SamplesCount = sample_remain / ChannelsCount;
            // そもそもWaveファイルのdataチャンクのバイト数を表す部分が

            headerPosition = strm.Seek(0, SeekOrigin.Current);
        }

        /// <summary>
        /// ステレオの場合は、呼び出し回数7が偶数になるよう注意してください。
        /// 読めるデータがなくなるとfalseが返ります
        /// 例外：EndOfStreamException
        /// </summary>
        /// <returns></returns>
        public bool ReadSample(out float val)
        {
            if (this.disposed) { throw new ObjectDisposedException(this.GetType().ToString()); }

            val = 0;

            if (sample_remain <= 0)
            {
                return false;
            }

            int d1;
            float gainrate8 = 1.0f / 128.0f;
            float gainrate16 = 1.0f / 32768.0f;
            float gainrate24 = 1.0f / 8388608.0f;
            switch (BitDepth)
            {
                case 8:
                    d1 = r.ReadByte();
                    val = (float)d1 * gainrate8 - 1.0f;
                    break;

                case 16:
                    d1 = r.ReadInt16();
                    val = (float)d1 * gainrate16;
                    break;

                case 24:
                    d1 = r.ReadByte();  // 符号なし
                    d1 += r.ReadByte() << 8;  // 符号なし
                    d1 += r.ReadByte() << 16;  // 符号なし
                    if ((d1 & 0x800000) != 0) d1 = (int)((uint)d1 | 0xFF000000u);  // 符号拡張
                    val = (float)d1 * gainrate24;
                    break;

                case 32:
                    byte[] indt = r.ReadBytes(4);  // ReadSingleじゃダメなんですか
                    //indt = indt.Reverse().ToArray();
                    val = BitConverter.ToSingle(indt, 0);
                    break;
            }

            sample_remain--;
            return true;
        }

        public static float[][] ReadAllSamples(String filename)
        {
            return ReadAllSamples(neu.IFileStream(filename, FileMode.Open, FileAccess.Read));
        }
        public static float[][] ReadAllSamples(Stream strm)
        {
            WaveFileReader wr = new WaveFileReader(strm);
            float[][] ret = new float[wr.ChannelsCount][];
            float indt;
            int n = 0;

            for (int ch = 0; ch < wr.ChannelsCount; ch++)
            {
                ret[ch] = new float[wr.sample_remain / wr.ChannelsCount];
            }
            while (wr.ReadSample(out indt))
            {
                for (int ch = 0; ch < wr.ChannelsCount; ch++)
                {
                    if (ch != 0) wr.ReadSample(out indt);
                    ret[ch][n] = indt;
                }
                n++;
            }

            wr.Close();  // よくIDEに「激おこ」って言われます

            return ret;
        }

        /// <summary>
        /// サンプル番号で位置を指定
        /// </summary>
        /// <param name="readPosition"></param>
        public void Seek(int readPosition)
        {
            strm.Seek(headerPosition + readPosition * (BitDepth / 8) * ChannelsCount, SeekOrigin.Begin);
        }
    }
}