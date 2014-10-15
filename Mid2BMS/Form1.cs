using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Codeplex.Data;
using System.Text.RegularExpressions;  // DynamicJson
//using System.Runtime.Serialization.Json;
using System.Web;
#if SILVERLIGHT
using System.Windows;
#else
using System.Windows.Forms;
#endif

namespace Mid2BMS
{
    public partial class Form1 : Form
    {
        MyForm MyFormInstance = new MyForm();

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ファイルがドラッグ＆ドロップされたときのsenderとeを使用して、
        /// ディレクトリ名とファイル名を返します。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="directory"></param>
        /// <param name="filename"></param>
        private void GetDirAndFilenameByEvent(object sender, DragEventArgs e, out String directory, out String filename)
        {
            directory = filename = null;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (string fileName in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    if (Directory.Exists(fileName))
                    {
                        char c = fileName[fileName.Length - 1];
                        if (c == '\\' || c == '/')
                        {
                            directory = fileName;
                        }
                        else
                        {
                            directory = fileName + "\\";
                        }
                    }
                    else if (File.Exists(fileName))
                    {
                        for (int i = fileName.Length - 1; i >= 0; i--)
                        {
                            if (fileName[i] == '\\' || fileName[i] == '/')
                            {
                                directory = fileName.Substring(0, i) + fileName[i];
                                filename = fileName.Substring(i + 1);
                                break;
                            }
                        }

                        break;
                    }
                }
            }
        }


        double ProgressBarValue = 0;
        bool ProgressBarFinished = true;
        Stopwatch SW;
        Frac dummyref = new Frac();

        private void InitializeProgressBar()
        {
            lock (dummyref)
            {
                SetAllButtonsEnabled(this.Controls, false);
            }

            if (SW != null && !ProgressBarFinished)
            {
                MessageBox.Show("実行ボタンを２回すばやく連続でクリックしないでください");
                while (!ProgressBarFinished)
                {
                    Thread.Sleep(100);
                }
            }
            ProgressBarValue = 0;
            ProgressBarFinished = false;
            timer1.Enabled = true;
            SW = new Stopwatch();
            SW.Reset();
            SW.Start();
        }


        // Control.ControlCollection と Forms.ControlCollection というのがある・・・？？
        // 同じ名前のクラスがたくさんある・・・
        // http://msdn.microsoft.com/ja-jp/library/system.windows.forms.control.controlcollection(v=vs.110).aspx

        // I need a noun to describe the state of being enabled/disabled. Do any exist?
        // http://english.stackexchange.com/questions/31878/noun-for-enable-enability-enabliness
        private void SetAllButtonsEnabled(Control.ControlCollection coll, bool enabled)
        {
            foreach (Control ctrl in coll)
            {
                if (ctrl.Controls.Count > 0)
                {
                    SetAllButtonsEnabled(ctrl.Controls, enabled);
                }
                if (ctrl.GetType() == button1.GetType())
                {
                    ctrl.Enabled = enabled;
                }
            }
        }

        // Mid2BMS
        private void button1_Click(object sender, EventArgs e)
        {
            //button1.Enabled = false;
            //button2.Enabled = false;
            //button3.Enabled = false;

            int VacantWavid;

            try
            {
                VacantWavid = BMSParser.IntFromHex36(textBox_vacantWavid.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                VacantWavid = 1;
            }

            bool isRedMode = radioButton_red.Checked;
            bool isPurpleMode = radioButton_purple.Checked;
            String margintime_beats = textBox_margintime.Text;
            int DefVacantBMSChIdx = checkBox_NoPlace11to29.Checked ? 16 : 0;
            bool LookAtInstrumentName = radioButton2.Checked;
            bool createExFiles = checkBox_createExtraFiles.Checked;
            bool sequenceLayer = checkBox_seqLayer.Checked;

            Convert.ToDouble(margintime_beats);  // 例外チェック

            String trackCsv = null;
            List<String> MidiTrackNames = null;
            List<String> MidiInstrumentNames = null;
            List<bool> isDrumsList = null;
            List<bool> ignoreList = null;
            List<bool> isChordList = null;

            // ラムダ式を濫用している感じある
            // そんなことよりラムダ式の中からローカル変数にアクセスできるのがやばい
            // へーなに？もうすぐJavaでもラムダ式が使えるようになるんだって？そうなんだ、すごいね
            // http://www.infoq.com/jp/news/2011/09/java-lambda
            // インターフェースが実装を持てる？ああ、拡張メソッドのことでしょ、知ってるよ。

            // ... JavaとMicrosoftェ・・・
            // http://ja.wikipedia.org/wiki/Java#.E3.83.97.E3.83.A9.E3.83.83.E3.83.88.E3.83.95.E3.82.A9.E3.83.BC.E3.83.A0.E9.9D.9E.E4.BE.9D.E5.AD.98

            // Aqua'n Beatsとかいうmac用のBMSプレイヤーがあるらしいですね。
            // 移植性の話が出たから聞きたいんだけれど、
            // macでBMSを作る人って居るんでしょうか？
            // いや、普通にVMWare使いますよねはい
            // C#をJavaに書き換えるのは大変なのだろうか？
            // (JavaをC#に書き換えるのはちょろそう)

            // 昔J#とかいうのもありましたね・・・

            // Javaって組み込みとかで頑張ってそうだからC#とはいい感じに住み分けが出来てるのかな？

            Action mid2bms_proc = () =>
                MyFormInstance.Mid2BMS_Process(
                    isRedMode, isPurpleMode, createExFiles, ref VacantWavid, ref DefVacantBMSChIdx,
                    LookAtInstrumentName, margintime_beats, out trackCsv, ref MidiTrackNames, out MidiInstrumentNames,
                    isDrumsList, ignoreList, isChordList, sequenceLayer,
                    ref ProgressBarValue, ref ProgressBarFinished);

            InitializeProgressBar();  // これを実行したら必ずanotherThreadが走るようにする

            Thread anotherThread = new Thread(new ThreadStart(() =>
            {
                //throw new Exception("あ～～～～");  // ←ハンドルされない
                try
                {
                    try
                    {
                        //for (int iii = 0; iii < 100; iii++)
                        //{
                        //    VacantWavid = 1;
                        //    DefVacantBMSChIdx = 16;
                        //    MidiTrackNames = null;
                        mid2bms_proc();
                        //}
                        //ProgressBarValue = 1.00;
                        //                        ProgressBarFinished = true;
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(exc.ToString());
                    }
                    finally
                    {
                        ProgressBarValue = 1.0;
                        ProgressBarFinished = true;
                    }
                    //textBox_vacantWavidUpdated.Text = BMSParser.IntToHex36Upper(VacantWavid);  // 地味にめんどい

                    //http://msdn.microsoft.com/ja-jp/library/ms171728(v=vs.110).aspx
                    // InvokeRequired required compares the thread ID of the
                    // calling thread to the thread ID of the creating thread.
                    // If these threads are different, it returns true.

                    //SetVacantWavidUpdated(BMSParser.IntToHex36Upper(VacantWavid));

                    if (trackCsv != null)
                    {
                        this.Invoke(
                            new Action(() =>
                            {
                                // トラック名の確認ダイアログを表示
                                Form2 f = new Form2();
                                {
                                    f.TrackName_csv = trackCsv;
                                    f.TrackNames = MidiTrackNames;
                                    f.InstrumentNames = MidiInstrumentNames;
                                    f.IsDrumsList = null;
                                    f.IgnoreList = null;
                                    f.IsChordList = null;
                                }
                                f.ShowDialog(this);
                                if (f.RedoRequired)
                                {
                                    // 処理のやりなおし
                                    VacantWavid = BMSParser.IntFromHex36(textBox_vacantWavid.Text);
                                    DefVacantBMSChIdx = checkBox_NoPlace11to29.Checked ? 16 : 0;
                                    MidiTrackNames = f.TrackNames;  // フォーム2から値を受け取る
                                    isDrumsList = f.IsDrumsList;
                                    ignoreList = f.IgnoreList;
                                    isChordList = f.IsChordList;
                                    f.Dispose();

                                    this.Invoke(new Action(() => InitializeProgressBar()), new object[] { });
                                    Thread anotherThread2 = new Thread(new ThreadStart(() =>
                                    {
                                        try
                                        {
                                            mid2bms_proc();
                                        }
                                        catch (Exception exc)
                                        {
                                            MessageBox.Show(exc.ToString());
                                        }
                                        finally
                                        {
                                            ProgressBarValue = 1.0;
                                            ProgressBarFinished = true;
                                            try
                                            {
                                                this.Invoke(
                                                    new Action<String>(t2 => { textBox_vacantWavidUpdated.Text = t2; }),
                                                    new object[] { BMSParser.IntToHex36Upper(VacantWavid) });  // ←タイプセーフではない？
                                            }
                                            catch (Exception exc)
                                            {
                                                MessageBox.Show(exc.ToString());  // 固有のwav数が多すぎて変換出来ない例外はよく発生しますね
                                            }
                                        }
                                    }));
                                    anotherThread2.Start();
                                }
                                else
                                {
                                    try
                                    {
                                        this.Invoke(
                                            new Action<String>(t2 => { textBox_vacantWavidUpdated.Text = t2; }),
                                            new object[] { BMSParser.IntToHex36Upper(VacantWavid) });  // ←タイプセーフではない？
                                    }
                                    catch (Exception exc)
                                    {
                                        MessageBox.Show(exc.ToString());  // 固有のwav数が多すぎて変換出来ない例外はよく発生しますね
                                    }
                                }
                            }),
                            new object[] { });
                    }
                    else
                    {
                        throw new Exception("Midiファイルの解析中に処理が中断されたため、概要画面を表示できませんでした。");
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString());
                }
            }));

            anotherThread.Start();
        }

        // Splitter
        private void button2_Click(object sender, EventArgs e)
        {
            double threshold = Convert.ToDouble(textBox_tailCutThreshold.Text);
            int fadein = Convert.ToInt32(textBox_fadeInTime.Text);
            int fadeout = Convert.ToInt32(textBox_fadeOutTime.Text);
            //bool useold = checkBox_oldSplitter.Checked;
            //double silence_threshold = Convert.ToDouble(textBox_silenceThreshold.Text);
            double silence_time = Convert.ToDouble(textBox_silenceTime.Text);
            bool inputFileIndicated = checkBox1.Checked;
            bool renamingEnabled = !checkBox2.Checked;
            string renamingFilename = textBox_serialWavFileName.Text;

            if (threshold > 0)
            {
                MessageBox.Show("Threshold には0以下の値を入力してください。-60 くらいが良いと思います。");
                return;
            }

            if (MyFormInstance.PathBase != MyFormInstance.WavePathBase)
            {
                if (MessageBox.Show(
                    "wavファイルのあるフォルダと、text5_renamer_array.txtファイルのあるフォルダが違っています。気にせずこのまま続けていいですか。", "",
                    MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                {
                    return;
                }
            }

            InitializeProgressBar();  // これを実行したら必ずanotherThreadが走るようにする

            Thread anotherThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    MyFormInstance.WaveSplit_Process(
                        threshold, fadein, fadeout, silence_time, inputFileIndicated, renamingEnabled, renamingFilename,
                        ref ProgressBarValue, ref ProgressBarFinished);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString());
                }
                finally
                {
                    ProgressBarValue = 1.0;
                    ProgressBarFinished = true;
                }
            }));
            anotherThread.Start();
        }

        // dupedef
        private void button3_Click(object sender, EventArgs e)
        {
            double intervaltime = Convert.ToDouble(textBox_intervaltime.Text) / 1000.0;

            InitializeProgressBar();  // これを実行したら必ずanotherThreadが走るようにする

            //http://oshiete.goo.ne.jp/qa/5772117.html
            // >>しかし、カートゥーン ネットワーク（CARTOON NETWORK）で放映中ですので
            // >>放送順さえ気にせず見続ければ、いつかは全エピソード制覇も夢じゃないかも？？？
            // まじかー

            Thread anotherThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    MyFormInstance.DupeDef_Process(intervaltime, ref ProgressBarValue, ref ProgressBarFinished);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString());
                }
                finally
                {
                    ProgressBarValue = 1.0;
                    ProgressBarFinished = true;
                }
            }));
            anotherThread.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (ProgressBarFinished)
            {
                lock (dummyref)
                {
                    SetAllButtonsEnabled(this.Controls, true);
                }

                //button1.Enabled = true;
                //button2.Enabled = true;
                //button3.Enabled = true;
                progressBar1.Value = progressBar1.Maximum;
                label1.Text = "Finished";
                timer1.Enabled = false;

                SW.Stop();
                label2.Text = (((double)SW.ElapsedTicks) / Stopwatch.Frequency).ToString("f3") + "sec required";
            }
            else
            {
                // ん、なんかお絵描きがしたくなってきた
                progressBar1.Value = Math.Min(progressBar1.Maximum, (int)(progressBar1.Maximum * ProgressBarValue));
                label1.Text = (int)(1000 * ProgressBarValue) + "‰";
                label2.Text = (((double)SW.ElapsedTicks) / Stopwatch.Frequency).ToString("f3") + "sec elapsed";
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {

            _tabPageManager = new TabPageManager(tabControl1);

            Load_Click(sender, e);  // 上級者モードにおける設定を含めたすべての設定の読み込み

            if (checkBox_advanced.Checked == false)
            {
                checkBox_advanced_CheckedChanged(null, null);
                Load_Click(sender, e);  // 上級者モードではなかった場合の設定の再読み込み
            }

            // 上級者モードを解除して、プログラムを終了すると、上級者モード専用の設定はすべて削除されます。

            //tabControl1.SelectedIndex = 2;

            textBox7_TextChanged(sender, e);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //checkBox_advanced.Checked = true;
            //checkBox_advanced_CheckedChanged(null, null);

            if (checkBox_InitForm.Checked)
            {
                if (File.Exists("ctrl.json"))
                {
                    File.Delete("ctrl.json");
                }
            }
            else
            {
                Save_Click(sender, e);
            }
        }

        //***************************************************************************
        //*** タブページ４
        //***************************************************************************

        private void button4_Click(object sender, EventArgs e)
        {
            char c = textBox_BasePath.Text[textBox_BasePath.Text.Length - 1];
            if (c == '\\' || c == '/')
            {
                MyFormInstance.PathBase = textBox_BasePath.Text;
            }
            else
            {
                MyFormInstance.PathBase = textBox_BasePath.Text + "\\";
            }
            MyFormInstance.FileName_MidiFile = textBox_MidiFileName.Text;
            button1_Click(sender, e);
        }

        private void textBox_BasePath_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void textBox_BasePath_DragDrop(object sender, DragEventArgs e)
        {
            String directory, filename;
            GetDirAndFilenameByEvent(sender, e, out directory, out filename);
            if (directory == null || filename == null) return;

            textBox_BasePath.Text = directory;
            textBox_MidiFileName.Text = filename;
        }

        private void textBox_MidiFileName_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void textBox_MidiFileName_DragDrop(object sender, DragEventArgs e)
        {
            textBox_BasePath_DragDrop(sender, e);
        }

        //***************************************************************************
        //*** タブページ５
        //***************************************************************************

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                char c = textBox_BasePath2.Text[textBox_BasePath2.Text.Length - 1];
                if (c == '\\' || c == '/')
                {
                    MyFormInstance.PathBase = textBox_BasePath2.Text;
                }
                else
                {
                    MyFormInstance.PathBase = textBox_BasePath2.Text + "\\";
                }

                c = textBox_WaveBasePath.Text[textBox_WaveBasePath.Text.Length - 1];
                if (c == '\\' || c == '/')
                {
                    MyFormInstance.WavePathBase = textBox_WaveBasePath.Text;
                }
                else
                {
                    MyFormInstance.WavePathBase = textBox_WaveBasePath.Text + "\\";
                }

                MyFormInstance.FileName_WaveFile = textBox_WaveFileName.Text;

                button2_Click(sender, e);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        private void textBox_BasePath2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void textBox_BasePath1_DragDrop(object sender, DragEventArgs e)
        {
            String directory, filename;
            GetDirAndFilenameByEvent(sender, e, out directory, out filename);
            if (directory == null || filename == null) return;

            textBox_BasePath2.Text = directory;
            //textBox_MidiFileName.Text = filename;
        }

        private void textBox_BasePath2_DragDrop(object sender, DragEventArgs e)
        {
            String directory, filename;
            GetDirAndFilenameByEvent(sender, e, out directory, out filename);
            if (directory == null || filename == null) return;

            textBox_WaveBasePath.Text = directory;
            textBox_WaveFileName.Text = filename;
        }

        private void textBox_WaveFileName_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void textBox_WaveFileName_DragDrop(object sender, DragEventArgs e)
        {
            textBox_BasePath2_DragDrop(sender, e);
        }

        private void textBox2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void textBox2_DragDrop(object sender, DragEventArgs e)
        {
            textBox_BasePath1_DragDrop(sender, e);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox_WaveFileName.Enabled = checkBox1.Checked;  // ？？？？
        }

        //***************************************************************************
        //*** タブページ６
        //***************************************************************************


        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                char c = textBox_BasePath3.Text[textBox_BasePath3.Text.Length - 1];
                if (c == '\\' || c == '/')
                {
                    MyFormInstance.RenamedPathBase = textBox_BasePath3.Text;
                }
                else
                {
                    MyFormInstance.RenamedPathBase = textBox_BasePath3.Text + "\\";
                }

                MyFormInstance.FileName_BMSFile = textBox_BMSFilePath.Text;

                button3_Click(sender, e);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        private void textBox_BasePath3_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (string fileName in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    if (Directory.Exists(fileName))
                    {
                        char c = fileName[fileName.Length - 1];
                        if (c == '\\' || c == '/')
                        {
                            textBox_BasePath3.Text = fileName;
                        }
                        else
                        {
                            textBox_BasePath3.Text = fileName + "\\";
                        }
                    }
                    else if (File.Exists(fileName))
                    {
                        for (int i = fileName.Length - 1; i >= 0; i--)
                        {
                            if (fileName[i] == '\\' || fileName[i] == '/')
                            {
                                textBox_BasePath3.Text = fileName.Substring(0, i) + "\\";
                                textBox_BMSFilePath.Text = fileName.Substring(i + 1);
                                break;
                            }
                        }

                        break;
                    }
                }
            }
        }


        private void textBox_BasePath3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void textBox_BMSFilePath_DragDrop(object sender, DragEventArgs e)
        {
            textBox_BasePath3_DragDrop(sender, e);
        }

        private void textBox_BMSFilePath_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        //***************************************************************************
        //*** タブページ７
        //***************************************************************************

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                WaveKnife k = new WaveKnife();
                k.crossfadebeats = Convert.ToDouble(textBox_crossfadebeats.Text);
                k.Knife(
                    textBox_BasePath4.Text,
                    textBox_InputFile4.Text,
                    textBox_outfnformat.Text,
                    Convert.ToDouble(textBox_BPM4.Text),
                    Convert.ToDouble(textBox_prebeats.Text),
                    Convert.ToDouble(textBox_intervalbeats.Text)
                );

            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        private void textBox_BasePath4_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (string fileName in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    if (Directory.Exists(fileName))
                    {
                        char c = fileName[fileName.Length - 1];
                        if (c == '\\' || c == '/')
                        {
                            textBox_BasePath4.Text = fileName;
                        }
                        else
                        {
                            textBox_BasePath4.Text = fileName + "\\";
                        }
                    }
                    else if (File.Exists(fileName))
                    {
                        for (int i = fileName.Length - 1; i >= 0; i--)
                        {
                            if (fileName[i] == '\\' || fileName[i] == '/')
                            {
                                textBox_BasePath4.Text = fileName.Substring(0, i) + "\\";
                                textBox_InputFile4.Text = fileName.Substring(i + 1);
                                break;
                            }
                        }

                        break;
                    }
                }
            }
        }

        private void textBox_BasePath4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }


        //***************************************************************************
        //*** タブページ８
        //***************************************************************************

        private void textBox_MidiInput5_DragDrop(object sender, DragEventArgs e)
        {
            String directory, filename;
            GetDirAndFilenameByEvent(sender, e, out directory, out filename);

            if (directory == null || filename == null) return;


            String[] fnsplit = filename.Split('.');
            if (fnsplit.Length == 1) fnsplit = new String[] { fnsplit[0], "" };
            fnsplit[fnsplit.Length - 1] = "";
            String filenameHD = String.Join(".", fnsplit);
            String filename2 = filenameHD.Substring(0, filenameHD.Length - 1) + "_analyzed.txt";
            String filename3 = filenameHD.Substring(0, filenameHD.Length - 1) + "_separated.mid";
            // filename == "foobar_2013.06.23.mid"
            //   then
            // filename2 == "foobar_2013.06.23_analyzed.txt"

            textBox_MidiInput5.Text = directory + filename;
            textBox_TextOut5.Text = directory + filename2;
            textBox_MidiOut5.Text = directory + filename3;
        }

        private void textBox_MidiInput5_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Stream rf = neu.IFileStream(textBox_MidiInput5.Text, FileMode.Open, FileAccess.Read);

            MidiStruct ms = new MidiStruct(rf);
            // ていうか MidiStruct は IDisposable じゃないのか

            rf.Close();  // ファッ！？

            FileIO.WriteAllText(
                textBox_TextOut5.Text,
                ms.ToString().Replace("\n", "\r\n"));
        }

        private void button9_Click(object sender, EventArgs e)
        {
            try
            {
                Stream rf = neu.IFileStream(textBox_MidiInput5.Text, FileMode.Open, FileAccess.Read);

                MidiStruct ms = new MidiStruct(rf, true);

                //ms.resolution = 15360;  // こんなものいらなかったんや！！
                /*
                for (int i = 0; i < ms.tracks.Count; i++)
                {
                    // MidiTrackクラスはマネージドだからこんなことも出来る（適当）
                    MidiTrack mt = ms.tracks[i];
                    ms.tracks[i] = mt.SplitNotes(ms);  // 最後のノートがとても長い場合にEnd of Trackが最後に来ないバグ
                }
                */
                ms.tracks = ms.tracks.Select(miditrack => miditrack.SplitNotes(ms, false)).ToList();

                Stream wf = neu.IFileStream(textBox_MidiOut5.Text, FileMode.Create, FileAccess.Write);

                ms.Export(wf, true);

                rf.Close();
                //wf.Close();  // ここでCloseを呼ばなきゃいけないのはコードデザイン的におかしい気がする
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            /*MidiStruct ms = new MidiStruct(15360);

            MidiTrack conductortrack = new MidiTrack("My Song 01");
            conductortrack.AddTempo(120.0, ms);
            conductortrack.AddEndOfTrack(ms);
            ms.tracks.Add(conductortrack);

            MidiTrack mt = new MidiTrack("Mugicha Lead");
            {
                MidiTrackWriter mtw = new MidiTrackWriter(mt, ms);
                mtw.AddNote(72 + 7, 90, new Frac(1, 4), new Frac(1, 4));
                mtw.AddNote(72 + 4, 90, new Frac(1, 4), new Frac(1, 4));
                mtw.AddNote(72 + 4, 90, new Frac(3, 8), new Frac(1, 2));
                mtw.AddNote(72 + 5, 90, new Frac(1, 4), new Frac(1, 4));
                mtw.AddNote(72 + 2, 90, new Frac(1, 4), new Frac(1, 4));
                mtw.AddNote(72 + 2, 90, new Frac(3, 8), new Frac(1, 2));
                mtw.Close();
            }
            mt.AddEndOfTrack(ms);
            ms.tracks.Add(mt);

            ms.Export(FileStreamFactory.Create(@"C:\テスト用フォルダ\choucho.mid", FileMode.Create, FileAccess.Write));
             */

            MessageBox.Show(VorbisReader.GetTotalSamples(@"D:\01.ogg") + " samples, sampling rate = " + VorbisReader.GetSamplingRate(@"D:\01.ogg"));
        }

        private void button11_Click(object sender, EventArgs e)
        {
            textBox_vacantWavid.Text = textBox_vacantWavidUpdated.Text;
        }

        private void button12_Click(object sender, EventArgs e)
        {
            /*
            Form2 f = new Form2();
            f.ShowDialog(this);
            f.Dispose();
             */
            //openFileDialog1.ShowDialog(this);

            SmallCanvas c = new SmallCanvas(1920, 1080, 0xFFFFFF);
            SmallCanvas d = new SmallCanvas(1920, 1080, 0xFFFFFF);

            //c.Plot2d(-10, 10, -2, 2, (x) => Math.Sin(x));  // ラムダ式書くときのIDEはクソ？
            //c.Plot2d(-10, 10, -2, 2, (x) => Math.Atan(x));  // ラムダ式書くときのIDEはクソ？
            c.PlotBode(Math.PI / 10000, Math.PI, -60, 6, (x) => 1.0);  // ラムダ式書くときのIDEはクソ？
            c.PlotBode(Math.PI / 10000, Math.PI, -60, 6, (x) => 0.1);  // ラムダ式書くときのIDEはクソ？
            c.PlotBode(Math.PI / 10000, Math.PI, -60, 6, (x) => 0.01);  // ラムダ式書くときのIDEはクソ？
            d.Plot2d(0, Math.PI, 0.0, 1.5, (x) => 1.0);  // ラムダ式書くときのIDEはクソ？
            //c.Plot2d(-10, 10, -2, 2, (x) => 0.5 * Math.PI);  // ラムダ式書くときのIDEはクソ？
            //c.Plot2d(-10, 10, -2, 2, (x) => -0.5 * Math.PI);  // ラムダ式書くときのIDEはクソ？
            //c.Plot2d(-10, 10, -2, 2, (x) => x);  // ラムダ式書くときのIDEはクソ？

            /*
            Polynomial poly = new Polynomial(2, "x");
            poly[0] = -3;
            poly[1] = 3;
            poly[2] = 1;
            Console.WriteLine(poly);
            c.Plot2d(-4, 4, -20, 20, poly.Eval);

            Polynomial poly2 = new Polynomial((new double[] { 2, 0, -2 }).Reverse(), "x");
            Console.WriteLine(poly2);
            c.Plot2d(-4, 4, -20, 20, poly2.Eval);

            Polynomial product = poly * poly2;
            Console.WriteLine(product);
            c.Plot2d(-4, 4, -20, 20, product.Eval);
             */

            /*SimpleFilter s;
            s = new SimpleFilter(0.001);
            c.PlotBode(Math.PI / 10000, Math.PI, -60, 6, s.CharacteristicCurve);
            d.Plot2d(0, Math.PI, 0.0, 1.5, s.CharacteristicCurve);
            s = new SimpleFilter(0.01);
            c.PlotBode(Math.PI / 10000, Math.PI, -60, 6, s.CharacteristicCurve);
            d.Plot2d(0, Math.PI, 0.0, 1.5, s.CharacteristicCurve);
            s = new SimpleFilter(0.1);
            c.PlotBode(Math.PI / 10000, Math.PI, -60, 6, s.CharacteristicCurve);
            d.Plot2d(0, Math.PI, 0.0, 1.5, s.CharacteristicCurve);
            s = new SimpleFilter(0.5);
            c.PlotBode(Math.PI / 10000, Math.PI, -60, 6, s.CharacteristicCurve);
            d.Plot2d(0, Math.PI, 0.0, 1.5, s.CharacteristicCurve);
            */

            //for (int j = 0; j < 100; j++)
            {
                /*
                var bLPF = new ButterworthFilter(FilterType.LowPass, 3, 1.25663706);
                c.PlotBode(Math.PI / 10000, Math.PI, -60, 6, bLPF.CharacteristicCurve);
                d.Plot2d(0, Math.PI, 0.0, 1.5, bLPF.CharacteristicCurve);

                bLPF = new ButterworthFilter(FilterType.LowPass, 10, 1.25663706);
                c.PlotBode(Math.PI / 10000, Math.PI, -60, 6, bLPF.CharacteristicCurve);
                d.Plot2d(0, Math.PI, 0.0, 1.5, bLPF.CharacteristicCurve);

                bLPF = new ButterworthFilter(FilterType.LowPass, 3, Math.PI * 0.1);
                c.PlotBode(Math.PI / 10000, Math.PI, -60, 6, bLPF.CharacteristicCurve);
                d.Plot2d(0, Math.PI, 0.0, 1.5, bLPF.CharacteristicCurve);

                bLPF = new ButterworthFilter(FilterType.LowPass, 10, Math.PI * 0.1);
                c.PlotBode(Math.PI / 10000, Math.PI, -60, 6, bLPF.CharacteristicCurve);
                d.Plot2d(0, Math.PI, 0.0, 1.5, bLPF.CharacteristicCurve);
                
                 * */
            }

            for (int j = 0; j < 5; j++)
            {
                var bLPF = new ButterworthFilter(FilterType.LowPass, 9, Math.PI * 0.1);
                c.PlotBode(Math.PI / 10000, Math.PI, -60, 6, bLPF.cascadeIIR[j].CharacteristicCurve);
                d.Plot2d(0, Math.PI, 0.0, 1.5, bLPF.cascadeIIR[j].CharacteristicCurve);
            }

            c.Export(neu.IFileStream("gazou.bmp", FileMode.Create, FileAccess.Write));
            d.Export(neu.IFileStream("gazou2.bmp", FileMode.Create, FileAccess.Write));

            if (true)
            {
                //FIRFilter sL = new FIRFilter(neu.IFileStream(@"D:\asdfruhito\impulse_response_hipass_x2.wav", FileMode.Open, FileAccess.Read));
                //FIRFilter sR = new FIRFilter(neu.IFileStream(@"D:\asdfruhito\impulse_response_hipass_x2.wav", FileMode.Open, FileAccess.Read));
                //SimpleFilter sL = new SimpleFilter(0.01);
                //SimpleFilter sR = new SimpleFilter(0.01);
                var sL = new ButterworthFilter(FilterType.LowPass, 9, Math.PI * 0.125);
                var sR = new ButterworthFilter(FilterType.LowPass, 9, Math.PI * 0.125);
                //var sL = new IIRFilter(new double[] { 1, -1 }, new double[] { 0.1, 0 });//積分
                //var sR = new IIRFilter(new double[] { 1, -1 }, new double[] { 0.1, 0 });
                /*var sL = new IIRFilter(
                    new double[] {
                        1,
                        -1.79825972655,
                        0.81682296735508
		            }, new double[] {
                        0.0046408102012674,
                        0.0092816204025348,
                        0.0046408102012674 });
                var sR = new IIRFilter(
                    new double[] {
                        1,
                        -1.79825972655,
                        0.81682296735508
		            }, new double[] {
                        0.0046408102012674,
                        0.0092816204025348,
                        0.0046408102012674 });*/
                WaveFileReader wr = new WaveFileReader(neu.IFileStream(@"D:\asdfruhito\gjbuop2.wav", FileMode.Open, FileAccess.Read));
                WaveFileWriter ww = new WaveFileWriter(neu.IFileStream(@"D:\asdfruhito\wavout.wav", FileMode.Create, FileAccess.Write), 
                    wr.ChannelsCount, wr.SamplingRate, wr.BitDepth);

                float indtL, indtR;
                int n = 0;
                while (wr.ReadSample(out indtL))
                {
                    indtL = (float)sL.Process(indtL);
                    if (n == 0) ww.WriteSample(indtL);

                    wr.ReadSample(out indtR);
                    indtR = (float)sR.Process(indtR);
                    if (n == 0) ww.WriteSample(indtR);

                    //n = ((n + 1) & 1);
                    n = 0;
                }
                wr.Close();  // usingを使わないからCloseを忘れるんだよ！！！
                ww.Close();
            }


            if (false)
            {

                // 適応的ダウンサンプリング
                MessageBox.Show("ファイルを上書き保存します。bmsフォルダのバックアップを取ってください。OKを押すと続行します。");

                // ファイルを上書き保存します。ちょっと良くないですね。
                string[] files = Directory.GetFiles(@"D:\asdfruhito\test", "*.wav", SearchOption.TopDirectoryOnly);
                foreach (string s in files)
                {
                    AdaptiveDownsampler.DownSample(s, s, -42);
                }
            }


            {
                Polynomial a = new Polynomial(new double[] { 3, 1, 2 }, "x");
                Polynomial a2 = a.代入(
                new Polynomial(new double[] { 1, -1 }, "y"),
                new Polynomial(new double[] { 1, 1 }, "y"));  // s = 1 - z^-1 を代入

                Console.WriteLine(a);
                Console.WriteLine(a2);
            }


            // 0
            // 1
            // x
            // 3x
            // x^2
            // 3x^2


            // 豆餅がおいしそうに焼けました
            // ・・・きっとうまい
            if (false)
            {
                var keisuu = new Dictionary<int, double>();
                string input = "x^3+3x^2-x-1";
                string pattern = @"(^|\+-|\+|-)("
                  + @"((\d*)([a-zA-Z_]+)(\^\d+)?)|"  // 論理和では左にあるものの方が優先的にマッチする
                  + @"(\d+)"
                  + ")";
                foreach (Match match in Regex.Matches(input, pattern))
                {
                    Console.WriteLine("Match: {0}", match.Value);
                    for (int groupCtr = 0; groupCtr < match.Groups.Count; groupCtr++)
                    {
                        Group group = match.Groups[groupCtr];
                        Console.WriteLine("   Group {0}: {1}", groupCtr, group.Value);
                        for (int captureCtr = 0; captureCtr < group.Captures.Count; captureCtr++)
                            Console.WriteLine("      Capture {0}: {1}", captureCtr,
                                              group.Captures[captureCtr].Value);

                        int TERM_1 = 3;  // 1次以上の項
                        int COEFFI = 4;  // 係数
                        int VARIABLE = 5;  // 変数
                        int EXPONENT = 6;  // "^" + 指数
                        int TERM_0 = 7;  // 0次の項
                        String vari = null;  // varは「文脈キーワード」のひとつ


                        if (match.Groups[TERM_1].Captures.Count >= 1)
                        {
                            // 1次以上の項
                        }
                        else
                        {
                            // 0次の項
                        }
                    }

                }
            }

            // https://www.google.co.jp/search?q=豆餅+栃木&tbm=isch


        }



        private void button13_Click(object sender, EventArgs e)
        {
            try
            {
                Stream rf = neu.IFileStream(textBox_MidiInput5.Text, FileMode.Open, FileAccess.Read);

                MidiStruct ms = new MidiStruct(rf, true);

                foreach (MidiTrack mt in ms.tracks)
                {
                    mt.ApplyHoldPedal();
                }

                ms.Export(neu.IFileStream(textBox_MidiOut5.Text, FileMode.Create, FileAccess.Write), true);

                rf.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }

        }

        private void button14_Click(object sender, EventArgs e)
        {
            try
            {
                Stream rf = neu.IFileStream(textBox_MidiInput5.Text, FileMode.Open, FileAccess.Read);

                MidiStruct ms = new MidiStruct(rf, true);

                long newTimeBase = Convert.ToInt32(textBox_newTimeBase.Text);
                long oldTimeBase = ms.resolution ?? 480;

                for (int i = 0; i < ms.tracks.Count; i++)
                {
                    MidiTrack mt = ms.tracks[i];

                    for (int j = 0; j < mt.Count; j++)
                    {
                        mt[j].tick = (int)((mt[j].tick * newTimeBase) / oldTimeBase);  // 切り捨て
                        if (mt[j] is MidiEventNote)
                        {
                            MidiEventNote me = (MidiEventNote)mt[j];
                            me.q = (int)((me.q * newTimeBase) / oldTimeBase);  // 切り捨て
                            me.q = Math.Max(1, me.q);  // ただし1以上
                        }
                    }
                }

                ms.resolution = (int)newTimeBase;

                ms.Export(neu.IFileStream(textBox_MidiOut5.Text, FileMode.Create, FileAccess.Write), true);

                rf.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        //***********************************************************************
        //***********************************************************************
        //***********************************************************************
        // フォームの内容を保存する
        //
        // [C#]コントロールの値をずばっとまるごと保存、展開する。
        // http://kimux.net/?p=360
        //
        // このコードではDynamicJsonを使用しています
        // http://dynamicjson.codeplex.com/

        /// <summary>
        /// Save all control value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Click(object sender, EventArgs e)
        {
            /// Get all control value by ControlProperty method.
            var ctrlList = ControlProperty.Get(this.Controls);

            /// Write all control value use JSON file.
            File.WriteAllText("ctrl.json", DynamicJson.Serialize(ctrlList.ToArray()));  // Unicodeのまま書き込む
        }

        /// <summary>
        /// Set all Contorl value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Load_Click(object sender, EventArgs e)
        {
            if (!File.Exists("ctrl.json"))
            {
                // IDEからデフォルト値を指定してしまうと、selectedIndexが-1のときに正しく読み込まれない（←日本語
                comboBox1.Text = "-42 (normal)";
                comboBox2.Text = "-24 (normal)";
                return;
            }

            /// Read all control value by JSON file.
            ControlProperty.Property[] val = DynamicJson.Parse(System.IO.File.ReadAllText("ctrl.json"));  // Unicodeのまま読み込む

            /// Set all control value by ControProperty method.
            ControlProperty.Set(this.Controls, val);
        }

        private void listBox1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void listBox1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                listBox1.Items.AddRange((string[])e.Data.GetData(DataFormats.FileDrop));
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        private void button15_Click(object sender, EventArgs e)
        {
            AdaptiveProcess(sender, e, AdaptiveProcessType.DownSampling, comboBox1, listBox1);
        }

        enum AdaptiveProcessType
        {
            None = 0,
            DownSampling,
            Monoauralize
        };

        private void AdaptiveProcess(object sender, EventArgs e, AdaptiveProcessType pType,
            ComboBox theComboBox, ListBox theListBox)
        {
            // 無ければ作るタイプのファイルは相対パスでOK
            // 無いと困る読み専用のファイルは「実行時にコピー」orリソースファイル

            List<String> filelist = new List<String>();

            double threshold = -42;
            try
            {
                threshold = Convert.ToDouble(Regex.Match(theComboBox.Text, @"-\d+(\.\d+)?").ToString());  // -0 はパスするけどまあいいよね
            }
            catch
            {
                MessageBox.Show("正しいしきい値(threshold)を入力してください。単位はdBで、値は0未満の整数です。");
                return;
            }

            // 適応的ダウンサンプリング
            if (DialogResult.Cancel == MessageBox.Show("ファイルを上書き保存します。bmsフォルダのバックアップを取ってください。OKを押すと続行します。", "Confirm to proceed", MessageBoxButtons.OKCancel))
            {
                return;
            }

            foreach (String s2 in theListBox.Items)
            {
                if (Directory.Exists(s2))
                {
                    // ファイルを上書き保存します。ちょっと良くないですね。
                    string[] files = Directory.GetFiles(s2, "*.wav", SearchOption.TopDirectoryOnly);
                    filelist.AddRange(files);
                }
                else
                {
                    if (Path.GetExtension(s2) == ".wav")
                    {
                        filelist.Add(s2);
                    }
                }
            }

            InitializeProgressBar();

            // todo
            //   .wavファイル以外を無視する
            //   プログレスが1単位で増加するようにしたい
            //   threshold読み込み
            //   インパルス応答の立ち上がりが遅いようですが、遅延に関してはどのようにお考えですか？（群遅延？）
            //     => 35サンプル程度の遅延ですので、実用には問題無いかと思われます。
            //     => それよりも疑問なのは、頭の10サンプルくらいの無音部分ですね
            //     => インパルス応答の最初の10sampleくらいのほぼ無音の部分って削ったらダメなんですかね？？？
            //       => あ、どうやら次数を20次にしていたようです。5次に変更しておきますね。

            // 頑張ってみた結果ThinkPadのエッジモーション機能はクソという結論に達した

            // Hashtable は O(1) (もしかしたらO(log n)かもしれない ) (そこそこ速い(多分))
            // Where().First() は O(n) (遅い)
            // なぜなのか。
            // Linqとかいう神なのかゴミなのか分からない機能な
            // え、Where().First() は O(1)でしょ？？？？(遅延評価)

            Thread anotherThread = new Thread(new ThreadStart(() =>
            {
                int halfI = filelist.Count / 2;
                var finishedCount = new List<int>();
                finishedCount.Add(0);

                int threadN = 4;  // 設定可能項目
                // コア数と同じくらいが良いと思います
                // スレッド数を増やしたいなら.Net Framework 4.0のTaskを使おう

                if (threadN >= 2)
                {
                    Random rnd = new System.Random();
                    filelist = filelist.OrderBy(_ => rnd.Next()).ToList();
                }

                Action<int> ithProc = (i) =>
                {
                    String s = filelist[i];
                    switch (pType)
                    {
                        case AdaptiveProcessType.DownSampling:
                            Console.WriteLine(i + s);
                            AdaptiveDownsampler.DownSample(s, s, threshold);
                            break;

                        case AdaptiveProcessType.Monoauralize:
                            Monoauralizer.Monoauralize(s, s, threshold);
                            break;

                        default: throw new Exception("wwwwwwwww");
                    }
                    ProgressBarValue += 1.0 / (double)filelist.Count;  // アトミックじゃないからもしかしたら死ぬかも
                };
                Action finished = () =>
                {
                    lock (finishedCount)
                    {
                        if (++finishedCount[0] == threadN)
                        {
                            ProgressBarValue = 1;
                            ProgressBarFinished = true;
                        }
                    }
                };


                //for (int i = 0; i < filelist.Count; i++)
                //{
                //String s = filelist[i];
                //AdaptiveDownsampler.DownSample(s, s, threshold,
                //    ref  ProgressBarValue, i / (double)filelist.Count, (i + 1) / (double)filelist.Count);
                //ProgressBarValue = (i + 1) / (double)filelist.Count;
                //}


                for (int j = 0; j < threadN; j++)
                {
                    int j2 = j;  // これでいけますかね
                    Thread multiThread = new Thread(new ThreadStart(() =>
                    {
                        for (int i = (filelist.Count * j2 / threadN); i < (filelist.Count * (j2 + 1) / threadN); i++)
                        {
                            ithProc(i);
                        }
                        finished();
                    }));
                    multiThread.Start();
                }


                //ProgressBarValue = 1;
                //ProgressBarFinished = true;
            }));
            anotherThread.Start();
        }

        private void button17_Click(object sender, EventArgs e)
        {
            int ProcessN = 60 * 140;
            double second_per_frame = 1.0 / 60.0;
            int FFTex = 10;
            int FFTn = 1 << FFTex;

            InitializeProgressBar();

            Thread anotherThread = new Thread(new ThreadStart(() =>
            {
                WaveFileReader wr = new WaveFileReader(@"D:\asdfruhito\akari.wav");
                if (wr.ChannelsCount != 1) throw new Exception("モノラルで頼む");
                FFT fft = new FFT(FFTex);

                Complex[] waveform = new Complex[FFTn];
                Complex[] spectrum = new Complex[FFTn];

                for (int frame = 0; frame < ProcessN; frame++)
                {
                    SmallCanvas sc = new SmallCanvas(64, 32, 0xFFFFFFu);

                    float indt;
                    int readPosition = (int)(wr.SamplingRate * frame * second_per_frame);
                    wr.Seek(readPosition);
                    for (int i = 0; i < waveform.Length; i++)
                    {
                        wr.ReadSample(out indt);
                        waveform[i] = indt * (0.5 + 0.5 - Math.Cos(2 * Math.PI * i / (double)waveform.Length));  // 窓関数
                    }
                    fft.Process(waveform, spectrum);
                    sc.PlotBodeFill(
                        50.0 * FFTn / wr.SamplingRate,
                        10000.0 * FFTn / wr.SamplingRate,
                        10, 80, (freq) =>
                    {
                        double position = freq;
                        int index = (int)Math.Floor(position);
                        double offset = position - index;
                        return (1 - offset) * spectrum[index].Abs() + offset * spectrum[index + 1].Abs();
                    });

                    sc.Export(neu.IFileStream(@"D:\asdfruhito\bmp\image_" + frame.ToString("D5") + @".bmp", FileMode.Create, FileAccess.Write));

                    ProgressBarValue = (frame + 1) / (double)ProcessN;
                }

                wr.Close();

                ProgressBarValue = 1;
                ProgressBarFinished = true;
            }));
            anotherThread.Start();
            // エクストリームしにたい
        }

        private void button18_Click(object sender, EventArgs e)
        {
            int[] x = { 1, 2, 3 };

            IEnumerable<int> y = x.Select(n => n * n);

            x[2] = 10;

            MessageBox.Show(y.Sum().ToString());

            InitializeProgressBar();

            var xxx = File.ReadAllLines(@"D:\bms_package\_yuinore\a.txt", HatoEnc.Encoding);
            var yyy = File.ReadAllLines(@"D:\bms_package\_yuinore\n.txt", HatoEnc.Encoding);

            var s =
                "M = " + xxx.Length + "\n" +
                "N = " + yyy.Length + "\n" +
                String.Join("\n", Diff.ShortestCommonSuperstring(xxx, yyy));

            ProgressBarValue = 1.0;
            ProgressBarFinished = true;

            FileIO.WriteAllText(@"D:\bms_package\_yuinore\_union.txt", s);

            // SCSを求める目的は、2つのBMSの間でオブジェのデータをやりとりすること。
            // ・・・であれば、3つ以上の文字列のSCSを求める必要はないのでは。
        }

        private void button19_Click(object sender, EventArgs e)
        {
            InitializeProgressBar();//スレッドを立ててないから意味ないけど一応

            BMSParser pX = new BMSParser(FileIO.ReadAllText(textBox_BMS_X.Text));
            BMSParser pY = new BMSParser(FileIO.ReadAllText(textBox_BMS_Y.Text));

            textBox_DiffResult.Text = BMSParser.Differentiate(pX, pY);

            ProgressBarValue = 1.0;
            ProgressBarFinished = true;
        }

        private void textBox_BMS_X_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void textBox_BMS_X_DragDrop(object sender, DragEventArgs e)
        {
            String[] filenames = ((string[])e.Data.GetData(DataFormats.FileDrop));
            textBox_BMS_X.Text = filenames[0];
            if (filenames.Length >= 2)
            {
                textBox_BMS_Y.Text = filenames[1];
            }
        }

        private void textBox_BMS_Y_DragDrop(object sender, DragEventArgs e)
        {
            String[] filenames = ((string[])e.Data.GetData(DataFormats.FileDrop));
            textBox_BMS_Y.Text = filenames[0];
            if (filenames.Length >= 2)
            {
                textBox_BMS_X.Text = filenames[0];
                textBox_BMS_Y.Text = filenames[1];
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            textBox_DiffResult.Text = "";
        }


        private void button21_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("フォームの内容をすべてリセットします。よろしいですか。", "confirm", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
            {
                checkBox_InitForm.Checked = true;
                Application.Restart();
                // 再起動するとデバッグが終了するらしい
            }
        }

        private void button22_Click(object sender, EventArgs e)
        {
            double d;

            d = 1.0 / 3.0;
            Console.WriteLine(d + "\n" + new Frac(d));
            d = 100.0 / 3.0;
            Console.WriteLine(d + "\n" + new Frac(d));
            d = 10000.0 / 7.0;
            Console.WriteLine(d + "\n" + new Frac(d));
            d = 1 / 5.0;
            Console.WriteLine(d + "\n" + new Frac(d));
            d = 0.11111111111;
            Console.WriteLine(d + "\n" + new Frac(d));
            d = 0.000001;
            Console.WriteLine(d + "\n" + new Frac(d));
            d = 0.11111111;
            Console.WriteLine(d + "\n" + new Frac(d));
            d = 0.11111;
            Console.WriteLine(d + "\n" + new Frac(d));
            d = 0.111;
            Console.WriteLine(d + "\n" + new Frac(d));
            d = 0.11;
            Console.WriteLine(d + "\n" + new Frac(d));
            d = 0.1;
            Console.WriteLine(d + "\n" + new Frac(d));
            d = 111111111.111;
            Console.WriteLine(d + "\n" + new Frac(d));
            d = 11111.1111111;
            Console.WriteLine(d + "\n" + new Frac(d));
            /*
             * 0.333333333333333
             * 1 / 3
             * 
             * 33.3333333333333
             * 100 / 3
             * 
             * 1428.57142857143
             * 10000 / 7
             * 
             * 0.2
             * 1 / 5
             * 
             * 0.11111111111
             * 1 / 9
             * 
             * 0.11111111
             * 1 / 9
             * 
             * 0.11111
             * 107519 / 967680
             * 
             * 0.111
             * 268531 / 2419200
             * 
             * 0.11
             * 11 / 100
             * 
             * 0.1
             * 1 / 10
             * 
             * 111111111.111
             * 268799999999731 / 2419200
             * 
             * 11111.1111111
             * 100000 / 9
             */
            /*
             * 改良版
             * 
             * 0.333333333333333
             * 1 / 3
             * 
             * 33.3333333333333
             * 100 / 3
             * 
             * 1428.57142857143
             * 10000 / 7
             * 
             * 0.2
             * 1 / 5
             * 
             * 0.11111111111
             * 1 / 9
             * 
             * 1E-06
             * 1 / 1000000
             * 
             * 0.11111111
             * 1 / 9
             * 
             * 0.11111
             * 11111 / 100000
             * 
             * 0.111
             * 111 / 1000
             * 
             * 0.11
             * 11 / 100
             * 
             * 0.1
             * 1 / 10
             * 
             * 111111111.111
             * 111111111111 / 1000
             * 
             * 11111.1111111
             * 100000 / 9
             * 
             */
        }

        private void button23_Click(object sender, EventArgs e)
        {
            {
                var x = WaveFileReader.ReadAllSamples(@"D:\asdfruhito\wav\orig.wav");

                var k = 5.0f;  // scaling factor

                for (int i = 0; i < x[0].Length; i++)
                {
                    var L = x[0][i];
                    var R = x[1][i];

                    x[0][i] = ((1 + k) * L + (1 - k) * R) * 0.5f;
                    x[1][i] = ((1 - k) * L + (1 + k) * R) * 0.5f;
                }

                WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\wav\orig_" + k + ".wav", x);
            }
            {
                var x = WaveFileReader.ReadAllSamples(@"D:\asdfruhito\wav\orig.wav");

                for (int i = 0; i < x[0].Length - 1; i++)
                {
                    x[0][i] = (x[0][i] * x[0][i + 1] < 0) ? 1 : 0;
                    x[1][i] = (x[1][i] * x[1][i + 1] < 0) ? 1 : 0;
                }

                WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\wav\zerocross_.wav", x);
            }
        }

        private double BPMAverageOfTwo(double bpm1, double bpm2)
        {
            double dif = Math.Abs(bpm1 - bpm2);
            double sum = bpm1 + bpm2;
            bool linear = (bpm1 * bpm2 > 0) && Math.Abs(dif / sum) < 0.00001;

            double average = linear
                ? (sum / 2.0)
                : ((bpm1 == bpm2) ? bpm1 : ((bpm2 - bpm1) / Math.Log(Math.Abs(bpm2 / bpm1))));

            return average;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double bpm1 = Convert.ToDouble(textBox2.Text);
                double bpm2 = Convert.ToDouble(textBox3.Text);

                double average = BPMAverageOfTwo(bpm1, bpm2);

                textBox4.Text = average.ToString("0.00000");
            }
            catch
            {
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            textBox2_TextChanged(sender, e);
        }


        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            try
            {
                // 恐らく初めてLinkedList使った
                var bpms_ = new LinkedList<double>(textBox5.Text.Split(new[] { "\r\n" }, StringSplitOptions.None).Select(x =>
                {
                    if (x == "") return 0;
                    try
                    {
                        double d = Convert.ToDouble(x);
                        if (d < 0) return 0;
                        return d;
                    }
                    catch { }
                    return 0;
                }));
                while (bpms_.Count != 0 && bpms_.First.Value == 0) bpms_.RemoveFirst();
                while (bpms_.Count != 0 && bpms_.Last.Value == 0) bpms_.RemoveLast();

                var bpms = bpms_.ToArray();

                int i;
                for (i = 0; i < bpms.Length; i++)
                {
                    if (bpms[i] == 0)
                    {
                        int j;
                        for (j = i; j < bpms.Length; j++)
                        {
                            if (bpms[j] != 0)
                            {
                                // index :       i               j
                                // value : x y z 0 0 0 ... 0 0 0 a b c
                                for (int k = i; k < j; k++)
                                {
                                    bpms[k] = (k - i + 1) * (bpms[j] - bpms[i - 1]) / (double)(j - i + 1) + bpms[i - 1];
                                }
                                break;
                            }
                        }
                        i = j - 1; // まあ引かなくても良い
                    }
                }


                List<String> averages = new List<String>();
                i = 0;
                double prev = 0;
                foreach (var bpm in bpms)
                {
                    if (i++ == 0)
                    {
                        prev = bpm;
                        continue;
                    }

                    //double average = (bpm == prev) ? bpm : ((bpm - prev) / Math.Log(bpm / prev));
                    //averages.Add(average.ToString("0.00000"));

                    averages.Add(BPMAverageOfTwo(bpm, prev).ToString("0.00000"));

                    prev = bpm;
                }
                textBox6.Text = averages.Join("\r\n");
            }
            catch (Exception ex)
            {
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //linkLabel1.LinkVisited = true;
            System.Diagnostics.Process.Start("https://www.google.com/search?q=シーケンスレイヤー&tbm=isch");
        }

        private void checkBox_advanced_CheckedChanged(object sender, EventArgs e)
        {
            //tabControl1.Hide();
            //tabPage1.Hide();
            /*tabPage2.Text = "";
            tabPage7.Text = "";
            tabPage8.Text = "";
            tabPage9.Text = "";
            tabPage10.Text = "";
            tabPage11.Text = "";*/

            bool magicalsoundshower = checkBox_advanced.Checked;
            _tabPageManager.ChangeTabPageVisible(1, magicalsoundshower);
            _tabPageManager.ChangeTabPageVisible(6, magicalsoundshower);
            _tabPageManager.ChangeTabPageVisible(7, magicalsoundshower);
            _tabPageManager.ChangeTabPageVisible(8, magicalsoundshower);
            _tabPageManager.ChangeTabPageVisible(9, magicalsoundshower);
            _tabPageManager.ChangeTabPageVisible(10, magicalsoundshower);
            _tabPageManager.ChangeTabPageVisible(11, magicalsoundshower);
            _tabPageManager.ChangeTabPageVisible(12, magicalsoundshower);

            panel_advancedsettings1.Visible = magicalsoundshower;
            panel_advancedsettings2.Visible = magicalsoundshower;

            if (comboBox1.Text == "") comboBox1.Text = "-42 (normal)";
            if (comboBox2.Text == "") comboBox2.Text = "-24 (normal)";
        }

        // http://dobon.net/vb/dotnet/control/tabpagehide.html
        // TabControlのTabPageを非表示にする
        TabPageManager _tabPageManager = null;
        public class TabPageManager
        {
            private class TabPageInfo
            {
                public TabPage TabPage;
                public bool Visible;
                public TabPageInfo(TabPage page, bool v)
                {
                    TabPage = page;
                    Visible = v;
                }
            }
            private TabPageInfo[] _tabPageInfos = null;
            private TabControl _tabControl = null;

            /// <summary>
            /// TabPageManagerクラスのインスタンスを作成する
            /// </summary>
            /// <param name="crl">基になるTabControlオブジェクト</param>
            public TabPageManager(TabControl crl)
            {
                _tabControl = crl;
                _tabPageInfos = new TabPageInfo[_tabControl.TabPages.Count];
                for (int i = 0; i < _tabControl.TabPages.Count; i++)
                    _tabPageInfos[i] =
                        new TabPageInfo(_tabControl.TabPages[i], true);
            }

            /// <summary>
            /// TabPageの表示・非表示を変更する
            /// </summary>
            /// <param name="index">変更するTabPageのIndex番号</param>
            /// <param name="v">表示するときはTrue。
            /// 非表示にするときはFalse。</param>
            public void ChangeTabPageVisible(int index, bool v)
            {
                if (_tabPageInfos[index].Visible == v)
                    return;

                _tabPageInfos[index].Visible = v;
                _tabControl.SuspendLayout();
                _tabControl.TabPages.Clear();
                for (int i = 0; i < _tabPageInfos.Length; i++)
                {
                    if (_tabPageInfos[i].Visible)
                        _tabControl.TabPages.Add(_tabPageInfos[i].TabPage);
                }
                _tabControl.ResumeLayout();
            }
        }

        private void button24_Click(object sender, EventArgs e)
        {
            var data = WaveFileReader.ReadAllSamples(@"D:\asdfruhito\bit\16m.wav");

            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_16_8m.wav", data, 1, 44100, 8);
            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_16_16m.wav", data, 1, 44100, 16);
            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_16_24m.wav", data, 1, 44100, 24);
            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_16_32m.wav", data, 1, 44100, 32);

            data = WaveFileReader.ReadAllSamples(@"D:\asdfruhito\bit\32m.wav");

            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_32_8m.wav", data, 1, 44100, 8);
            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_32_16m.wav", data, 1, 44100, 16);
            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_32_24m.wav", data, 1, 44100, 24);
            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_32_32m.wav", data, 1, 44100, 32);

            data = WaveFileReader.ReadAllSamples(@"D:\asdfruhito\bit\24m.wav");

            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_24_8m.wav", data, 1, 44100, 8);
            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_24_16m.wav", data, 1, 44100, 16);
            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_24_24m.wav", data, 1, 44100, 24);
            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_24_32m.wav", data, 1, 44100, 32);

            data = WaveFileReader.ReadAllSamples(@"D:\asdfruhito\bit\8m.wav");

            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_8_8m.wav", data, 1, 44100, 8);
            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_8_16m.wav", data, 1, 44100, 16);
            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_8_24m.wav", data, 1, 44100, 24);
            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_8_32m.wav", data, 1, 44100, 32);


            data = WaveFileReader.ReadAllSamples(@"D:\asdfruhito\bit\24om.wav");

            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_24o_8m.wav", data, 1, 44100, 8);
            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_24o_16m.wav", data, 1, 44100, 16);
            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_24o_24m.wav", data, 1, 44100, 24);
            WaveFileWriter.WriteAllSamples(@"D:\asdfruhito\bit\_24o_32m.wav", data, 1, 44100, 32);

            //NVorbis.VorbisReader
            using (var r = new NVorbis.VorbisReader(@"D:\asdfruhito\bit\ogg.ogg"))
            using (var w = new WaveFileWriter(@"D:\asdfruhito\bit\_ogg_wav.wav", r.Channels, r.SampleRate, 16))
            {
                float[] buf = new float[1];
                while (true)
                {
                    var ret = r.ReadSamples(buf, 0, 1);
                    w.WriteSample(buf[0]);
                    if (ret == 0) break;
                }
            }

        }

        private void listBox2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void listBox2_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                listBox2.Items.AddRange((string[])e.Data.GetData(DataFormats.FileDrop));
            }
        }

        private void button25_Click(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
        }

        private void button26_Click(object sender, EventArgs e)
        {
            AdaptiveProcess(sender, e, AdaptiveProcessType.Monoauralize, comboBox2, listBox2);
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            String text = "@yuinore 【アプデ希望】" + textBox7.Text;
            String preview = text + " #Mid2BMSupd";
            textBox8.Text = preview;
            webBrowser1.DocumentText =
                "<html><head></head><body>"
                + "<a href=\"https://twitter.com/intent/tweet?button_hashtag=Mid2BMSupd&text=" + HttpUtility.UrlEncode(text) + "\" class=\"twitter-hashtag-button\" data-lang=\"ja\">Tweet #Mid2BMSupd</a>"
                + "<script>!function(d,s,id){var js,fjs=d.getElementsByTagName(s)[0],p=/^http:/.test(d.location)?'http':'https';if(!d.getElementById(id)){js=d.createElement(s);js.id=id;js.src=p+'://platform.twitter.com/widgets.js';fjs.parentNode.insertBefore(js,fjs);}}(document, 'script', 'twitter-wjs');</script>"
                + "</body></html>";
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            textBox_serialWavFileName.Enabled = checkBox2.Checked;
            textBox_BasePath2.Enabled = !checkBox2.Checked;
        }

        private void button27_Click(object sender, EventArgs e)
        {
            //String path1 = @"D:\bms_package\Programming\mid2mml_v2\MidiSeparatorのテスト用の音楽_960.mid";
            String path1 = @"D:\bms_package\Programming\mid2mml_v2\fam.003.23.mid";
            String path2 = @"D:\bms_package\Programming\mid2mml_v2\fam.003.23.mid.mml";
            //String path1 = @"D:\bms_package\Programming\mid2mml_v2\vc25_fmt1.mid";
            //String path2 = @"D:\bms_package\Programming\mid2mml_v2\vc25_fmt1.mid.mml";
            //String path1 = @"D:\bms_package\Programming\mid2mml_v2\Chobits - Opening - Let Me Be With You.mid";
            //String path2 = @"D:\bms_package\Programming\mid2mml_v2\Chobits - Opening - Let Me Be With You.mid.mml";

            FileIO.WriteAllText(path2, (new Mid2mml2(new MidiStruct(neu.IFileStream(path1, FileMode.Open, FileAccess.Read))).ToString()));
        }

        private void button28_Click(object sender, EventArgs e)
        {
            try
            {
                String path1 = textBox_MidiInput5.Text;
                String path2 = Path.ChangeExtension(textBox_MidiOut5.Text, "mml");
                FileIO.WriteAllText(path2, (new Mid2mml2(new MidiStruct(neu.IFileStream(path1, FileMode.Open, FileAccess.Read))).ToString()));
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }


        private void listBox3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }
        private void listBox3_DragDrop(object sender, DragEventArgs e)
        {
            var flist = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                listBox3.Items.AddRange(flist);
            }

            if (flist.Length >= 1)
            {
                textBox_orderTextOut.Text =
                    Path.Combine(
                        Path.GetDirectoryName(flist[0]),
                        "__ordering_result.txt");
            }
        }


        private void button29_Click(object sender, EventArgs e)
        {
            var order = new Order();

            var result = new List<ArrTuple<string,double>>();
            var textoutpath = textBox_orderTextOut.Text;

            List<String> filelist = new List<String>();

            var theListBox = listBox3;

            foreach (String s2 in theListBox.Items)
            {
                if (Directory.Exists(s2))
                {
                    string[] files = Directory.GetFiles(s2, "*.wav", SearchOption.TopDirectoryOnly);
                    filelist.AddRange(files);
                }
                else
                {
                    if (Path.GetExtension(s2) == ".wav")
                    {
                        filelist.Add(s2);
                    }
                }
            }

            InitializeProgressBar();

            Thread anotherThread = new Thread(new ThreadStart(() =>
            {
                var finishedCount = new List<int>();
                finishedCount.Add(0);

                int threadN = 4;  // 設定可能項目
                // コア数と同じくらいが良いと思います
                // スレッド数を増やしたいなら.Net Framework 4.0のTaskを使おう

                if (threadN >= 2)
                {
                    Random rnd = new System.Random();
                    filelist = filelist.OrderBy(_ => rnd.Next()).ToList();
                }

                Action<int> ithProc = (i) =>
                {
                    String s = filelist[i];
                    lock (result) { result.Add(Arr.ay(s, order.Evaluate(s))); }
                    ProgressBarValue += 1.0 / (double)filelist.Count;  // アトミックじゃないからもしかしたら死ぬかも
                };
                Action finished = () =>
                {
                    lock (finishedCount)
                    {
                        if (++finishedCount[0] == threadN)
                        {
                            ProgressBarValue = 1;
                            ProgressBarFinished = true;

                            // 終了処理
                            var ord =
                                result.OrderBy(x => x.Item2).Select(x => Arr.ay(x.Item1, x.Item2, 0.0)).ToArray();
                            for (int i = 1; i < ord.Length; i++)
                            {
                                ord[i] = Arr.ay(
                                    ord[i].Item1,
                                    ord[i].Item2,
                                    Math.Min(ord[i].Item2, ord[i - 1].Item2) / Math.Max(ord[i].Item2, ord[i - 1].Item2)
                                    );
                            }
                            String resulttext =
                                ord
                                .Select(x => x.Item2.ToString("F17") + " \t" + x.Item3.ToString("F17") + " \t" + Path.GetFileName(x.Item1))
                                .Join("\n");
                            File.WriteAllText(textoutpath, resulttext);

                            String resultcsv =
                                ord
                                .Select(x => x.Item2.ToString("F17") + "," + x.Item3.ToString("F17") + "," + Path.GetFileName(x.Item1))
                                .Join("\n");
                            File.WriteAllText(Path.ChangeExtension(textoutpath, "csv"), resultcsv);

                            System.Diagnostics.Process p =
                                System.Diagnostics.Process.Start(textoutpath);
                        }
                    }
                };

                for (int j = 0; j < threadN; j++)
                {
                    int j2 = j;  // これでいけますかね
                    Thread multiThread = new Thread(new ThreadStart(() =>
                    {
                        for (int i = (filelist.Count * j2 / threadN); i < (filelist.Count * (j2 + 1) / threadN); i++)
                        {
                            ithProc(i);
                        }
                        finished();
                    }));
                    multiThread.Start();
                }
            }));

            anotherThread.Start();
        }

        private void button30_Click(object sender, EventArgs e)
        {
            using (Stream rf = neu.IFileStream(textBox_MidiInput5.Text, FileMode.Open, FileAccess.Read))
            {

                ImprovedBinaryReader r = new ImprovedBinaryReader(rf);

                StringSuruyatuSafe s = new StringSuruyatuSafe();

                byte[] data;
                int dword;
                long longdata;
                uint uintdata;


                String BR = "\r\n";

                data = r.ReadBytes(4);
                s += "Chunk Name (must be FLhd) : " + HatoEnc.Encode(data) + BR;

                dword = r.ReadInt32();
                s += "Header Size (must be 6) : " + dword + BR;

                data = r.ReadBytes(dword);
                s += "Header Value : " + data.Select(x => x.ToString()).Join(" ") + BR + BR;

                data = r.ReadBytes(4);
                s += "Chunk Name (must be FLdt) : " + HatoEnc.Encode(data) + BR;
                try
                {
                    while (true)  // 二度手間っぽくてクソ
                    {
                        dword = r.ReadByte();
                        switch (dword & 0xC0)
                        {
                            case 0x00:
                                s += "  ";
                                s += String.Format("{0:X}", dword);
                                s += " : ";
                                uintdata = r.ReadByte();
                                s += String.Format("{0:X} ({0})", uintdata);
                                s += BR;
                                break;

                            case 0x40:
                                s += "  ";
                                s += String.Format("{0:X}", dword);
                                s += " : ";
                                uintdata = r.ReadUInt16();
                                s += String.Format("{0:X} ({0})", uintdata);
                                s += BR;
                                break;

                            case 0x80:
                                s += "  ";
                                s += String.Format("{0:X}", dword);
                                s += " : ";
                                uintdata = r.ReadUInt32();
                                s += String.Format("{0:X} ({0})", uintdata);
                                s += BR;
                                break;

                            case 0xC0:
                                s += "  ";
                                s += String.Format("{0:X}", dword);

                                longdata = r.ReadDeltaTimeBigEndian();
                                data = r.ReadBytes((int)longdata);
                                s += " : " + String.Format("0x{0:X} bytes", longdata) + BR + "    ";

                                s += data.Select(x => String.Format("{0:X}", x)).Join(" ");
                                s += BR + "    ";

                                //s += Regex.Replace(HatoEnc.Encode(data), @"\p{Cc}", str => string.Format("[{0:X2}]", (byte)str.Value[0]));
                                s += Regex.Replace(HatoEnc.Encode(data), @"\p{Cc}", str => "?");
                                // http://nanoappli.com/blog/archives/4841
                                // [C#]文字列中の制御文字を、[CR][LF]や[0D][0A]のように可視化する / nanoblog

                                s += BR;
                                break;

                            default:
                                throw new Exception("あれれ～～～おかしいぞ～～～～");
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                }

                FileIO.WriteAllText(
                    textBox_TextOut5.Text,
                    s.ToString());
            }
        }

        private void button31_Click(object sender, EventArgs e)
        {
            try
            {
                double max_beats = Convert.ToDouble(textBox_LimitLenBeats.Text);

                Stream rf = neu.IFileStream(textBox_MidiInput5.Text, FileMode.Open, FileAccess.Read);

                MidiStruct ms = new MidiStruct(rf, true);

                int max_tick = (int)(max_beats * ms.resolution);

                foreach (MidiTrack mt in ms.tracks)
                {
                    foreach (MidiEvent me_ in mt)
                    {
                        MidiEventNote me = me_ as MidiEventNote;
                        if (me != null)
                        {
                            me.q = Math.Min(max_tick, me.q);  // 破壊的変更
                        }
                    }
                }

                ms.Export(neu.IFileStream(textBox_MidiOut5.Text, FileMode.Create, FileAccess.Write), true);

                rf.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        private void button32_Click(object sender, EventArgs e)
        {
            try
            {
                int velQuantInt = Convert.ToInt32(textBox_velQuantInt.Text);
                if (velQuantInt < 1) throw new Exception("Velocity Quantization Interval は 1以上である必要があります");

                Stream rf = neu.IFileStream(textBox_MidiInput5.Text, FileMode.Open, FileAccess.Read);

                MidiStruct ms = new MidiStruct(rf, true);
                
                foreach (MidiTrack mt in ms.tracks)
                {
                    foreach (MidiEvent me_ in mt)
                    {
                        MidiEventNote me = me_ as MidiEventNote;
                        if (me != null)
                        {
                            if (me.v >= 1)
                            {
                                me.v = ((int)Math.Round((double)me.v / (double)velQuantInt)) * velQuantInt;
                                if (me.v < 1) me.v = 1;
                                else if (me.v > 127) me.v = 127;
                            }
                        }
                    }
                }

                ms.Export(neu.IFileStream(textBox_MidiOut5.Text, FileMode.Create, FileAccess.Write), true);

                rf.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }


    }
}
