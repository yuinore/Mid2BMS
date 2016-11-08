using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Mid2BMS
{
    class TinyTinyRenamer
    {
        public string OriginalBMSDirectory { get; set; }
        public string RenamedBMSDirectory { get; set; }
        public string KeySoundFileExtension { get; set; }

        public string RenameResultMultilineText { get; private set; }

        public void Rename(ref double ProgressBarValue)
        {
            if (OriginalBMSDirectory == null) { throw new ArgumentNullException(); }
            if (RenamedBMSDirectory == null) { throw new ArgumentNullException(); }
            if (KeySoundFileExtension == null) { throw new ArgumentNullException(); }

            SHA512Managed hashComputer = new SHA512Managed();

            Func<string, string> GetFileHash = filename => hashComputer.ComputeHash(new FileStream(filename, FileMode.Open, FileAccess.Read)).Select(x => x.ToString("x2")).Join("");
            // ラムダ式、(技術的に)やばいなあ・・・すごいなあ・・・
            
            var oldName_to_hash = new Dictionary<string, string>();
            var hash_to_newName = new Dictionary<string, string>();
            
            var originalKeySoundFilenames = Directory.GetFiles(OriginalBMSDirectory);

            foreach (var filename in originalKeySoundFilenames)
            {
                if (Path.GetExtension(filename).ToLower() != KeySoundFileExtension) continue;
                
                var hash = GetFileHash(filename);

                oldName_to_hash.Add(Path.GetFileName(filename), hash);

                ProgressBarValue += 0.4 / (double)originalKeySoundFilenames.Length;
            }

            // 並列版selectの使い方がわからなくてブチ切れた（とてもつらい）

            var renamedKeySoundFilenames = Directory.GetFiles(RenamedBMSDirectory);

            foreach (var filename in renamedKeySoundFilenames)
            {
                if (Path.GetExtension(filename).ToLower() != KeySoundFileExtension) continue;

                var hash = GetFileHash(filename);

                try
                {
                    hash_to_newName.Add(hash, Path.GetFileName(filename));
                }
                catch
                {
                    string f1 = hash_to_newName[hash];
                    string f2 = filename;

                    if (MessageBox.Show("中身の等しいキー音が宛先フォルダに2つ存在します。\""
                        + f1 + "\" と \"" + f2 + "\" です。どっちかを適当に選んで続行して宜しいですか？",
                        "Confirm",
                        MessageBoxButtons.OKCancel) != DialogResult.OK)
                    {
                        // 処理中断
                        return;
                    }
                }

                ProgressBarValue += 0.4 / (double)renamedKeySoundFilenames.Length;
            }

            StringBuilder s = new StringBuilder();

            foreach (var kvpair in oldName_to_hash)
            {
                if (!hash_to_newName.ContainsKey(kvpair.Value))
                {
                    string f1 = Path.GetFileName(kvpair.Key);
                    if (MessageBox.Show("消失してしまったキー音が存在するようです。\""
                        + f1 + "\" です。この音を無視して続行しますか？ キャンセルを押すと中断します。",
                        "Confirm",
                        MessageBoxButtons.OKCancel) != DialogResult.OK)
                    {
                        // 処理中断
                        return;
                    }

                    continue;  // 消失したキー音の処遇は後で考える
                }
                s.Append(kvpair.Key + " ---> " + hash_to_newName[kvpair.Value] + "\r\n");
            }

            ProgressBarValue += 0.05;

            RenameResultMultilineText = s.ToString();

            hashComputer.Clear();

            foreach (var bmsfilename in Directory.GetFiles(RenamedBMSDirectory))
            {
                var currentFileExt = Path.GetExtension(bmsfilename).ToLower();
                if (currentFileExt != ".bms" && currentFileExt != ".bme" && currentFileExt != ".bml" && currentFileExt != ".pms") continue;

                int i = 0;
                string movedFileName = bmsfilename + "_old";
                while (File.Exists(movedFileName))
                {
                    i++;
                    movedFileName = bmsfilename + "_old" + i;  // i >= 1
                }
                File.Move(bmsfilename, movedFileName);
            }

            ProgressBarValue += 0.05;

            foreach (var bmsfilename in Directory.GetFiles(OriginalBMSDirectory))
            {
                var currentFileExt = Path.GetExtension(bmsfilename).ToLower();
                if (currentFileExt != ".bms" && currentFileExt != ".bme" && currentFileExt != ".bml" && currentFileExt != ".pms") continue;

                var bmsAllText = File.ReadAllText(bmsfilename, HatoEnc.Encoding).Replace("\r\n", "\n");

                try
                {
                    // http://stackoverflow.com/questions/31326451/replacing-regex-matches-using-lambda-expression
                    var newBMSText = Regex.Replace(bmsAllText, @"^(#WAV[0-9A-Za-z][0-9A-Za-z] )(.+)$", match =>
                    {
                        try
                        {

                            var prefix = match.Groups[1].Captures[0].Value;
                            var old_filename = match.Groups[2].Captures[0].Value;
                            var new_filename = hash_to_newName[oldName_to_hash[old_filename]];

                            if (new_filename == "bass_000.wav")
                            {
                                Console.WriteLine(match.Groups.Count + ", " + match.Groups[1].Captures.Count + ", " + match.Groups[2].Captures.Count + "");
                            }

                            return prefix + new_filename;
                        }
                        catch (KeyNotFoundException)
                        {
                            if (match.Value != "")
                            {
                                if (MessageBox.Show("BMSファイル \"" + bmsfilename + "\" 内で、存在しないキー音 \"" + match.Groups[2].Captures[0].Value + "\" がBMSファイル中で指定されました。" +
                                    "BMSファイル中の拡張子と実際のファイルの拡張子が異なっているなどの可能性が考えられます。" +
                                    "このファイルを無視して続行しますか？",
                                    "Confirm", MessageBoxButtons.YesNoCancel) != DialogResult.Yes)
                                {
                                    // 中断
                                    MessageBox.Show("処理を中断しました。", "Message");
                                    throw new KeyNotFoundException();
                                }
                            }

                            // 全キャッチは良くない
                            return match.Value;
                        }
                    }, RegexOptions.Multiline);

                    string newBMSFilename = Path.Combine(RenamedBMSDirectory, Path.GetFileName(bmsfilename));
                    if (File.Exists(newBMSFilename))
                    {
                        //****************** 何らかの処理 ********************
                        MessageBox.Show("何らかの処理");
                    }
                    else
                    {
                        File.WriteAllText(newBMSFilename, newBMSText.Replace("\n", "\r\n"), HatoEnc.Encoding);
                    }
                }
                catch (KeyNotFoundException)
                {
                    return;
                }
            }

            ProgressBarValue += 0.1;

            //MessageBox.Show("処理が完了しました。", "Finished");
        }
    }
}
