#if SILVERLIGHT
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Mid2BMS
{
    public static class FileStreamFactory
    {
        static Dictionary<string, Stream> readfiles = new Dictionary<string, Stream>();
        static Dictionary<string, MemoryStream> wrotefiles = new Dictionary<string, MemoryStream>();

        public static List<String> SavedFileList
        {
            get
            {
                // dictionaryはIEnumerableじゃない！？
                //return wrotefiles.Select(x => x.Key);
                List<String> s2 = new List<string>();
                foreach (var x in wrotefiles)
                {
                    s2.Add(x.Key);
                }
                return s2;
            }
        }

        public static void ShowOpenFileDialog(String filename)
        {
            OpenFileDialog opener = new OpenFileDialog();
            opener.Filter = "Files (*" + System.IO.Path.GetExtension(filename) + ")|*" + System.IO.Path.GetExtension(filename);
            if (opener.ShowDialog() != false)
            {
                Stream src = opener.File.OpenRead();
                Stream dst = new MemoryStream();
                int dt;
                while ((dt = src.ReadByte()) != -1)
                {
                    dst.WriteByte((byte)dt);
                }
                readfiles[filename] = dst;
                src.Close();
            }
        }
        public static void ShowSaveFileDialog(String filename)
        {
            SaveFileDialog opener = new SaveFileDialog();
            opener.Filter = "Files (*" + System.IO.Path.GetExtension(filename) + ")|*" + System.IO.Path.GetExtension(filename);
            opener.DefaultExt = System.IO.Path.GetExtension(filename).Substring(1);
            opener.DefaultFileName = filename;
            if (opener.ShowDialog() != false)
            {
                // CloseしたMemoryStreamからデータを読み出すにはToArray()メソッドを使います
                // http://xptn.dtiblog.com/blog-entry-19.html
                // >> MemoryStreamの困った仕様 - Mr.Exception
                Stream dst = opener.OpenFile();
                wrotefiles[filename].Close(); // Closeは何度呼ばれても良い
                byte[] buf = wrotefiles[filename].ToArray();
                dst.Write(buf, 0, buf.Length);
                dst.Close();
            }
        }

        public static Stream Create(String filename, FileMode mode, FileAccess access) {
            if (access == FileAccess.Read)
            {
                Stream src = readfiles[filename];
                Stream dst = new MemoryStream();
                src.Seek(0, SeekOrigin.Begin);
                int dt;
                while ((dt = src.ReadByte()) != -1)
                {
                    dst.WriteByte((byte)dt);
                }
                dst.Seek(0, SeekOrigin.Begin);
                return dst;
            }
            else if (access == FileAccess.Write)
            {
                return wrotefiles[filename] = new MemoryStream();
            }
            return null;
        }

        public static void WriteAllText(String filename, String text)
        {
            StreamWriter wr = new StreamWriter(FileStreamFactory.Create(filename, FileMode.Create, FileAccess.Write), HatoEnc.Encoding);

            wr.Write(text);

            wr.Close();
        }

        public static String ReadAllText(string filename)
        {
            throw NotImplementedException();
        }
    }
}
#else

using System;
using System.Collections.Generic;
using System.IO;

namespace Mid2BMS
{
    public static class FileIO
    {
        static Dictionary<string, Stream> readfiles = new Dictionary<string, Stream>();
        static Dictionary<string, MemoryStream> wrotefiles = new Dictionary<string, MemoryStream>();

        public static List<String> SavedFileList
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public static void ShowOpenFileDialog(String filename)
        {
            throw new PlatformNotSupportedException();
        }
        public static void ShowSaveFileDialog(String filename)
        {
            throw new PlatformNotSupportedException();
        }

        public static void WriteAllText(String filename, String text)
        {
            File.WriteAllText(filename, text, HatoEnc.Encoding);
        }

        public static String ReadAllText(string filename)
        {
            return File.ReadAllText(filename, HatoEnc.Encoding);
        }
    }

    public static partial class neu
    {
        public static Stream IFileStream(String filename, FileMode mode, FileAccess access)
        {
            return new FileStream(filename, mode, access);
        }
    }
}

#endif