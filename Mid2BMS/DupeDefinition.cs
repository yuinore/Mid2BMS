using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    /// <summary>
    /// wavのヘッダサイズは44と仮定していますが、大きな問題ではないでしょう
    /// </summary>
    class DupeDefinition
    {
        // これreadonlyじゃないぞ！！！！殺せーーー！！！
        struct DoubleBool : IComparable<DoubleBool>
        {
            public double x;
            public bool y;
            public DoubleBool(double dbl, bool bl)
            {
                x = dbl;
                y = bl;
            }
            public int CompareTo(DoubleBool z)
            {
                if (x > z.x) return 1;
                if (x < z.x) return -1;
                return 0;
            }
        }

        public int Process(String bms, double soundsInterval, String RenamedBasePath, out String[] textNames, out String[] text, ref double ProgressBarValue, double ProgressMin, double ProgressMax)
        {
            textNames = new String[5];
            text = new String[5];

            BMSParser bp = new BMSParser(bms);
            List<BMSObject> obj;
            StringBuilder s = new StringBuilder();
            StringBuilder s2 = new StringBuilder();
            List<int[]> wavidList = new List<int[]>();  // {元のid, 定義個数}
            List<DoubleBool> times;

            if (bp.BPM == null)
            {
                throw new Exception("BMSからBPMを判別できませんでした");
            }

            ProgressBarValue = 10;

            //////////////////////////////////////////////////
            // 重複定義が必要な個数を確認、wavidListに{元のid, 定義個数}を記録
            //////////////////////////////////////////////////
            
            for (int i = 0; i < 36 * 36; i++)
            {
                ProgressBarValue = ProgressMin + (ProgressMax - ProgressMin) * (0.10 + 0.15 * i / (36 * 36));

                if (i == bp.LNOBJ) continue;
                if ((obj = bp.FindAllWavId(i)) == null) continue;

                if (obj.Count != 1 /*&& System.IO.File.Exists(RenamedBasePath + bp.GetWaveFileName(i))*/)
                {
                    times = new List<DoubleBool>();

                    s.Append(BMSParser.IntToHex36Upper(i) + " (" + bp.GetWaveFileName(i) + "):");

                    // 重複定義する必要があるかもしれないなら

                    double waveFileLength = 0;
                    // wavefileの長さを取得する
                    {
                        String filepath = RenamedBasePath + bp.GetWaveFileName(i);
                        String fileExtension = Path.GetExtension(filepath);

                        if (!File.Exists(filepath))
                        {
                            filepath = Path.ChangeExtension(filepath, "wav");
                            fileExtension = ".wav";

                            if (!File.Exists(filepath))
                            {
                                filepath = Path.ChangeExtension(filepath, "ogg");
                                fileExtension = ".ogg";

                                if (!File.Exists(filepath))
                                {
                                    throw new FileNotFoundException("ファイル" + bp.GetWaveFileName(i) + "が見つかりません");
                                }
                            }
                        }

                        if (fileExtension == ".wav")
                        {
                            using (WaveFileReader wfr = new WaveFileReader(filepath))
                            {
                                waveFileLength = wfr.SamplesCount / (double)wfr.SamplingRate;
                            }
                        }
                        else
                        {
                            waveFileLength = VorbisReader.GetTotalSamples(filepath) / (double)VorbisReader.GetSamplingRate(filepath);
                        }
                    }
                    waveFileLength += soundsInterval;

                    //System.IO.FileInfo fi = new System.IO.FileInfo(RenamedBasePath + bp.GetWaveFileName(i));
                    ////ファイルのサイズを取得
                    //int samplesCount = (int)((fi.Length - 44) / 4);  // Headersize 44, Stereo16bit限定
                    //double waveFileLength = samplesCount / 44100.0;

                    s.Append((int)(waveFileLength * 1000) + "\t\t");

                    for (int j = 0; j < obj.Count; j++)
                    {
                        double exactTime = (60.0 / (double)bp.BPM) * (obj[j].t.n * 4.0 / obj[j].t.d);
                        times.Add(new DoubleBool(exactTime, true));  // Start Point
                        times.Add(new DoubleBool(exactTime + waveFileLength, false));  // End Point (Terminal?)
                    }

                    times.Sort();

                    for (int j = 0; j < times.Count; j++)
                    {
                        s.Append((int)(times[j].x * 1000) + (times[j].y ? "[" : "]") + " ");
                    }

                    int maxLayers = 0;
                    int layers = 0;
                    for (int j = 0; j < times.Count; j++)
                    {
                        if (times[j].y) layers++;
                        else layers--;

                        if (layers > maxLayers) maxLayers = layers;
                    }
                    if (maxLayers >= 2)
                    {
                        s2.Append(BMSParser.IntToHex36Upper(i) + "," + maxLayers + "\r\n");
                        wavidList.Add(new int[2] { i, maxLayers });
                    }

                    s.Append("\r\n\r\n");
                }
            }

            textNames[0] = "dupedef_text1_debug.txt";
            text[0] = s.ToString();
            textNames[1] = "dupedef_text2_table.txt";
            text[1] = s2.ToString();

            ProgressBarValue = ProgressMin + (ProgressMax - ProgressMin) * 0.25;

            //////////////////////////////////////////////////
            //////////////////////////////////////////////////
            //////////////////////////////////////////////////
            // wavidList.ToArray();  // {変換したい変換元のid, 重複定義の数(>=2)}
            s = new StringBuilder();
            s2 = new StringBuilder();

            // ここから、再配置を行う。
            List<int[]> wavidShiftTable = new List<int[]>();  // {元のid, 新しいid}
            List<int[]> wavidListShifted = new List<int[]>();  // {変換したい変換【先】のid, 重複定義の数(>=2)}

            for (int i = 0; i < 36 * 36; i++)
            {
                List<BMSObject> obj2 = new List<BMSObject>();
                //if ((obj2 = bp.FindAllWavId(i)) != null)
                if (((obj2 = bp.FindAllWavId(i)) != null) || (bp.GetWaveFileName(i) != ""))  // オブジェ配置が存在するか、wav定義が存在する
                {
                    wavidShiftTable.Add(new int[2] { i, i });
                }
            }
            int shiftCount = 0;
            int wavidList_index = 0;
            for (int i = 0; i < wavidShiftTable.Count; i++)
            {

                wavidShiftTable[i][1] += shiftCount;

                s.Append(BMSParser.IntToHex36Upper(wavidShiftTable[i][0]) + "->" + BMSParser.IntToHex36Upper(wavidShiftTable[i][1]) + "\r\n");
                if (wavidList_index < wavidList.Count && wavidList[wavidList_index][0] == wavidShiftTable[i][0])
                {
                    wavidListShifted.Add(new int[2] { wavidList[wavidList_index][0] + shiftCount, wavidList[wavidList_index][1] });
                    s2.Append(BMSParser.IntToHex36Upper(wavidList[wavidList_index][0] + shiftCount) + "," + wavidList[wavidList_index][1] + "\r\n");
                    shiftCount += wavidList[wavidList_index][1] - 1;
                    wavidList_index++;
                    //if (wavidList_index < wavidList.Count)
                    //{
                    //    wavidList[wavidList_index][0] += shiftCount;
                    //}
                }
            }

            textNames[2] = "dupedef_text3_convtable.txt";
            text[2] = s.ToString();
            textNames[3] = "dupedef_text4_table_shifted.txt";
            text[3] = s2.ToString();

            ProgressBarValue = ProgressMin + (ProgressMax - ProgressMin) * 0.40;

            //////////////////////////////////////////////////
            //////////////////////////////////////////////////
            //////////////////////////////////////////////////

            String[] bmsSplited = bms.Split(new String[] { "\r\n" }, StringSplitOptions.None);

            for (int i = wavidShiftTable.Count - 1; i >= 0; i--)
            {
                BMSRawModifier.WavidReplaceWhole(bmsSplited, wavidShiftTable[i][0], wavidShiftTable[i][1]);

                ProgressBarValue = ProgressMin + (ProgressMax - ProgressMin) * (0.85 - 0.45 * i / wavidShiftTable.Count);
            }

            ProgressBarValue = ProgressMin + (ProgressMax - ProgressMin) * 0.85;

            //////////////////////////////////////////////////
            //////////////////////////////////////////////////
            //////////////////////////////////////////////////
            int x = 0, y = 0;
            int targetDiff = 0;

            for (int i = 0; i < wavidListShifted.Count; i++)
            {
                x = 0;
                y = 0;
                targetDiff = 0;
                while (BMSRawModifier.WavidReplace(bmsSplited, wavidListShifted[i][0], wavidListShifted[i][0] + targetDiff, ref x, ref  y) >= 0)
                {
                    y += 2;
                    targetDiff++;
                    if (targetDiff >= wavidListShifted[i][1]) targetDiff = 0;
                }
                // wavidListShifted[i][0] から (wavidListShifted[i][0] + j) へコピー

                for (int j = 0; j < bmsSplited.Length; j++)
                {
                    if (BMSParser.IsLineOfWAVXX(bmsSplited[j]))
                    {
                        int wavid = BMSParser.IntFromHex36(bmsSplited[j][4], bmsSplited[j][5]);
                        if (wavid == wavidListShifted[i][0])
                        {
                            // コピー開始だ！！！
                            String LineText = bmsSplited[j];

                            bmsSplited[j] = LineText;
                            for (int k = 1; k < wavidListShifted[i][1]; k++)
                            {
                                // この時点で、bmsSplited は行ごとに分割されていない状態になる。
                                bmsSplited[j] += "\r\n" + LineText.Substring(0, 4) + BMSParser.IntToHex36Upper(wavidListShifted[i][0] + k) + LineText.Substring(6);
                            }
                        }
                    }
                }
            }

            ProgressBarValue = ProgressMin + (ProgressMax - ProgressMin) * 1.0;

            //////////////////////////////////////////////////
            //////////////////////////////////////////////////
            //////////////////////////////////////////////////

            textNames[4] = "dupedef_text5_bms.txt";
            text[4] = String.Join("\r\n", bmsSplited);
            return 0;
        }


    }
}
