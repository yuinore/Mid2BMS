using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Mid2BMS
{
    static class HatoEnc
    {
        public static readonly String EncodingName = "Shift_JIS";
        public static readonly Encoding Encoding = null;
        private static readonly String encoding_file = @"encoding.ini";

        static HatoEnc()  // 静的コンストラクタ(publicはいらない)
        {
            // どうやら静的コンストラクタは、最初に静的メソッド(か何か)が呼ばれるときに実行されるらしい。
            // なるほど。

#if !SILVERLIGHT
            // 静的コンストラクタでは例外が起きてほしくない（ような気がする）
            try
            {
                if (File.Exists(encoding_file))
                { // これってSilverlightでも使えるのか（使えないでしょ
                    try
                    {
                        EncodingName = File.ReadAllText(encoding_file, System.Text.Encoding.ASCII);
                        Encoding = Encoding.GetEncoding(EncodingName);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(
                            "encoding.ini ファイルで指定されているエンコーディングが不正です。\n"+
                            "正しいエンコーディングに書き換えるか、該当ファイルを削除してください。\n\n" + e.ToString(),
                            "Invalid Text Encoding (see encoding.ini)");
                    }
                }
                else
                {
                    File.WriteAllText(encoding_file, "Shift_JIS", System.Text.Encoding.ASCII);
                }
            }
            catch
            {
            }
            finally
            {
                if (Encoding == null)
                {
                    EncodingName = "Shift_JIS";
                    Encoding = Encoding.GetEncoding(EncodingName);
                }
            }
#endif
        }

        public static String Encode(byte[] buf)
        {
#if SILVERLIGHT
            return Soramimi.Jcode.sjis_to_wstr(buf);
#else
            if (buf.Length >= 1 && buf[buf.Length - 1] == (byte)0)
            {
                byte[] buf2 = new byte[buf.Length - 1];
                Array.Copy(buf, buf2, buf2.Length);
                buf = buf2;
            }
            return Encoding.GetString(buf, 0, buf.Length);
#endif
        }

        public static byte[] Encode(String s)
        {
#if SILVERLIGHT
            return Soramimi.Jcode.wstr_to_sjis(s);
#else
            return Encoding.GetBytes(s);
#endif
        }
    }
}
