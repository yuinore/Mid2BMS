using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Mid2BMS
{
    /// <summary>
    /// 参考資料
    /// http://en.wikipedia.org/wiki/Ogg
    /// http://www.xiph.org/vorbis/doc/framing.html
    /// </summary>
    static class VorbisReader
    {
        /// <summary>
        /// 全サンプル数を64ビット符号付き整数で返します。
        /// </summary>
        public static long GetTotalSamples(String filename)
        {
            byte[] buf;
            buf = System.IO.File.ReadAllBytes(filename);  // 例外が起きるかもしれないね
            // なんかもうめんどくさいから全部読み込んでいいよね(クズ
            int i = 0;  // reading point(byte)

            while (buf[i] == 'O' && buf[i + 1] == 'g' && buf[i + 2] == 'g' && buf[i + 3] == 'S' && buf[i + 4] == '\0')
            {
                if ((buf[i + 5] & 4) == 4)  // [header_type_flag] last page of logical bitstream (eos)
                {
                    // end
                    long len = 0L;
                    len += ((long)buf[i + 6 + 0] << 0);
                    len += ((long)buf[i + 6 + 1] << 8);
                    len += ((long)buf[i + 6 + 2] << 16);
                    len += ((long)buf[i + 6 + 3] << 24);
                    len += ((long)buf[i + 6 + 4] << 32);
                    len += ((long)buf[i + 6 + 5] << 40);
                    len += ((long)buf[i + 6 + 6] << 48);
                    len += ((long)buf[i + 6 + 7] << 56);
                    return len;
                }
                // not end

                i += 0x1A;
                int page_segments = (int)buf[i++];  // [page_segments]
                int segment_table__sum = 0;
                while ((page_segments--) != 0)
                {
                    segment_table__sum += buf[i++];
                }
                i += segment_table__sum;  // skip to next "OggS"
            }
            throw new Exception("oggファイルが不正です");
        }

        /// <summary>
        /// サンプリングレートをかえします。
        /// </summary>
        public static int GetSamplingRate(String filename)
        {
            BinaryReader br = new BinaryReader(neu.IFileStream(filename, FileMode.Open, FileAccess.Read));

            byte[] buf = br.ReadBytes(0x28);
            if (buf[0] == 'O' && buf[1] == 'g' && buf[2] == 'g' && buf[3] == 'S' && buf[4] == '\0')
            {
                return br.ReadInt32();
            }
            throw new Exception("oggファイルが不正です");
        }
    }
}
