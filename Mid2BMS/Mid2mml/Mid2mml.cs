using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

// NoteGateReducって何！？
namespace Mid2BMS
{
    /// <summary>
    /// midファイルを指定するとmmlファイルを書き出します。
    /// NoteGateReducを 0 にしましたので不都合が無いか確認してください
    /// ゴミみたいなコードだ
    /// </summary>
    class Mid2mml
    {
        int NoteBufSize = 1048576;//65536;
        int NoteGateReduc = 0;  // gateを減らす。負の数を指定すると増加。単位(unit)は？

        int ReadBinDeltaTime(BinaryReader fp)
        {
            // Long形だけど大丈夫だよね！むしろjava scriptのfloat（？）のほうが心配だお
            byte ret;
            int retS = 0;
            while (true)
            {
                ret = fp.ReadByte();
                retS = (retS << 7) + (ret & 0x7F);
                if ((ret & 0x80) == 0) break;
            }
            return retS;
        }
        int SeekBinForward(BinaryReader fp, int siz)
        {
            for (int i = 0; i < siz; i++)
            {
                fp.ReadByte();
            }
            return 0;
        }
        int SeekBinBackward(ref BinaryReader fp, int siz)
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
        }
        byte[] ReadBinStr(BinaryReader fp, int cnt)
        {
            return fp.ReadBytes(cnt);
        }
        int ReadBinInt32(BinaryReader fp)
        {
            uint ret;
            ret = fp.ReadUInt32();
            return (int)(((ret & (0xFF000000)) >> 24) + ((ret & 0x00FF0000) >> 8) + ((ret & 0x0000FF00) << 8) + ((ret & 0x000000FF) << 24));
        }


        public int Process(Stream MidFileReadStream, String MmlFilePath, out List<String> MMLs,
            out List<String> MidiTrackNames,
            out List<String> MidiInstrumentNames,
            bool createExFiles,
            ref double progressValue, double progressMin, double progressMax)
        {
            int siz = 0;
            int val = 0;
            //int nxt = 0;
            int t = 0;
            int j, k, q;
            int isEnd = 0;
            int evCnt = 0;
            int lastEv = 0;
            int mmlGate;
            int mmlLen;

            // 何がクソってコレクションに配列使ってるあたりがクソ。あまりにクソすぎてクソ。
            int[] Times = new int[NoteBufSize];   // midi tick
            int[] Values = new int[NoteBufSize];
            int[] Notes = new int[NoteBufSize];
            int[] EType = new int[NoteBufSize];  // 0 8 9 B C F
            byte[] buffe;

            String noteOnkaiA = "ccddeffggaab";
            String noteOnkaiB = " + +  + + + ";


            MMLs = new List<String>();
            MidiTrackNames = new List<String>();
            MidiInstrumentNames = new List<String>();

            StreamWriter streamW = null;
            BinaryReader fpR = null;
            try  // using streamW
            {
                if (createExFiles)
                {
                    streamW = new StreamWriter(neu.IFileStream(MmlFilePath, FileMode.Create, FileAccess.Write), HatoEnc.Encoding);//@"stdout_part1.txt");
                }
                else
                {
                    streamW = new StreamWriter(new MemoryStream());  // 読み捨て用(NullObjectStream？)
                }
                fpR = new BinaryReader(MidFileReadStream);//@"stdread.mid"));

                streamW.Write("\r\n\r\n\r\nTempo(160)TimeBase=(384*40);\r\n// Hello.\r\n// 「トラック番号修正」や「@118付加」を適宜\r\n\r\n");

                streamW.Write("\r\n\r\n// Chunk {0} (1297377380 is header chunk)", ReadBinInt32(fpR)); // ヘッダチャンクと仮定
                siz = ReadBinInt32(fpR);
                streamW.Write("\r\n// Chunk Size {0} (header chunk)", siz);

                fpR.ReadInt16();

                int MidiTrackCount = fpR.ReadInt16();
                MidiTrackCount = ((MidiTrackCount & 0x00FF) << 8) | ((MidiTrackCount & 0xFF00) >> 8);

                SeekBinForward(fpR, siz - 4);

                for (j = 0; MidiTrackCount-- != 0; j++)
                {
                    streamW.Write("\r\n");

                    progressValue = progressMin + Math.Min(j, 32) * (progressMax - progressMin) / 32;

                    try
                    {
                        val = ReadBinInt32(fpR); // データチャンクと仮定
                        if (val != 1297379947) break;
                    }
                    catch (Exception)
                    {
                        break;
                    }
                    //printf("\r\n// Chunk %d (1297379947 is data chunk)", val );

                    siz = ReadBinInt32(fpR);
                    streamW.Write("\r\n// Chunk Size {0} (data chunk)", siz);

                    streamW.Write("\r\nTrack({0})  // チャンネルはお任せ", j);

                    t = 0;
                    isEnd = 0;
                    for (k = 0; k < NoteBufSize; k++)
                    {  // ●▼●▼●（１）配列へ読み込み

                        t += ReadBinDeltaTime(fpR);
                        Times[k] = t;


                        val = fpR.ReadByte();
                        if (val < 0x80)
                        {
                            val = lastEv;
                            SeekBinBackward(ref fpR, 1);
                        }

                        switch (val / 16)
                        {
                            case 0xF:  // ●▼●SysEx or メタイベント
                                EType[k] = 0xF;
                                if (val == 0xF0 || val == 0xF7)
                                {  // SysEx
                                    siz = ReadBinDeltaTime(fpR);
                                    SeekBinForward(fpR, siz);
                                    break;
                                }
                                switch (fpR.ReadByte())
                                {  // メタ
                                    case 0x03: //Track Name
                                        siz = fpR.ReadByte();
                                        buffe = ReadBinStr(fpR, siz);
                                        streamW.Write("\r\n  // TrackName = {0}", HatoEnc.Encode(buffe));

                                        //if (!LookAtInstrumentName)
                                        //{
                                        //    while (MidiTrackNames.Count <= j) { MidiTrackNames.Add("untitled " + MidiTrackNames.Count); }  // いろいろファイル名の衝突とかのバグを生みそうなので修正した
                                        //    MidiTrackNames[j] = HatoEnc.Encode(buffe);
                                        //}
                                        while (MidiTrackNames.Count <= j) { MidiTrackNames.Add(""); }  // 不等号に等号を含める必要がある
                                        MidiTrackNames[j] = HatoEnc.Encode(buffe);
                                        break;

                                    case 0x04: //Instrument Name
                                        siz = fpR.ReadByte();
                                        buffe = ReadBinStr(fpR, siz);
                                        streamW.Write("\r\n  // InstrumentName = {0}", HatoEnc.Encode(buffe));

                                        //if (LookAtInstrumentName)
                                        //{
                                        //    while (MidiTrackNames.Count <= j) { MidiTrackNames.Add("untitled " + MidiTrackNames.Count); }  // いろいろファイル名の衝突とかのバグを生みそうなので修正した
                                        //    MidiTrackNames[j] = HatoEnc.Encode(buffe);
                                        //}
                                        while (MidiInstrumentNames.Count <= j) { MidiInstrumentNames.Add(""); }
                                        MidiInstrumentNames[j] = HatoEnc.Encode(buffe);
                                        break;
                                    case 0x2F:  // End of Track
                                        isEnd = 1;
                                        SeekBinForward(fpR, 1);
                                        break;
                                    default:
                                        siz = fpR.ReadByte();
                                        SeekBinForward(fpR, siz);
                                        break;
                                }
                                break;

                            case 0x8:  // ●▼●チャンネル０ノートoff
                                Notes[k] = fpR.ReadByte();
                                Values[k] = fpR.ReadByte();
                                EType[k] = 8;
                                lastEv = val;
                                //printf("\r\n  // note off t=%d n=%d, v=%d",t,Notes[k],Values[k]);
                                break;

                            case 0x9:  // ●▼●チャンネル０ノートon
                                Notes[k] = fpR.ReadByte();
                                Values[k] = fpR.ReadByte();
                                EType[k] = (Values[k] == 0) ? 8 : 9;
                                lastEv = val;
                                //printf("\r\n  // note on? t=%d n=%d, v=%d",t,Notes[k],Values[k]);
                                break;

                            case 0xA:
                            case 0xB:
                            case 0xE:  // ●▼●チャンネル０のCCなどなど
                                Notes[k] = fpR.ReadByte();
                                Values[k] = fpR.ReadByte();
                                EType[k] = 0xB;
                                lastEv = val;
                                //printf("\r\n  // note oth t=%d n=%d, v=%d",t,Notes[k],Values[k]);
                                break;

                            case 0xC:
                            case 0xD:  // ●▼●チャンネル０のCCなどなど２
                                Values[k] = fpR.ReadByte();
                                EType[k] = 0xC;
                                lastEv = val;
                                //printf("\r\n  // note coth t=%d v=%d",t,Values[k]);
                                break;

                            /*default:  // ●▼●前回と同じ
                              if(val>=0x80)break;//じゃなかった
                              Notes[k] = val;
                              Values[k] = ReadBinUChar(fpR);
                              if(lastEv==9) { EType[k] = (Values[k]==0) ? 8 : 9; } else { EType[k] = lastEv; }
                              if(lastEv==8||lastEv==9) {
                                printf("\r\n  // note run t=%d n=%d, v=%d",t,Notes[k],Values[k]);
                              }
                              break;*/
                        }

                        if (isEnd != 0) break;
                    }
                    evCnt = k;
                    //streamW.Write("\r\n  //[$$$BEGINMMLSECTION$$$]\r\n", j);
                    streamW.Write("\r\n");

                    StringBuilder s9 = new StringBuilder();

                    // 最初の休符
                    mmlLen = -1;
                    for (q = 0; q < evCnt; q++)
                    {
                        if (EType[q] == 9) { mmlLen = Times[q]; break; }
                    }
                    if (mmlLen == -1) mmlLen = Times[0];
                    s9.Append("q%" + (mmlLen - NoteGateReduc));
                    s9.Append("l%" + mmlLen);
                    s9.Append("r ");

                    for (k = 0; k < evCnt; k++)
                    {  // ●▼●▼●（２）配列へ読み込み
                        if (EType[k] == 9)
                        {  // 問題はnoteOnだ
                            //get T of 「Notesの値が等しい直後の EType==8 」
                            mmlGate = 0;
                            for (q = k + 1; q < evCnt; q++)
                            {
                                if (EType[q] == 8 && Notes[q] == Notes[k]) { mmlGate = Times[q] - Times[k]; break; }
                            }
                            //get T of 「次回の EType==9 それがなければ 次のイベント」
                            mmlLen = -1;
                            for (q = k + 1; q < evCnt; q++)
                            {

                                if (EType[q] == 9) { mmlLen = Times[q] - Times[k]; break; }
                            }
                            if (mmlLen == -1) mmlLen = Times[k + 1];
                            //というわけで書き出し
                            s9.Append("q%" + (mmlGate - NoteGateReduc));
                            s9.Append("l%" + mmlLen);
                            s9.Append("v" + Values[k]);
                            s9.Append("o" + (Notes[k] / 12) + (noteOnkaiA[Notes[k] % 12]) + (noteOnkaiB[Notes[k] % 12]) + " ");
                        }
                    }

                    streamW.Write(s9.ToString());
                    while (MMLs.Count <= j) { MMLs.Add(""); }
                    MMLs[j] = s9.ToString();

                    //streamW.Write("\r\n  //[$$$ENDMMLSECTION$$$]\r\n");
                    streamW.Write("\r\n");
                }

                streamW.Write("\r\n\r\n// End Analysis");

                //_getch();//scanf("%d",&a);

            }
            finally
            {
                // If the finally block was executing during the handling of another exception, then that first exception is lost.
                // http://stackoverflow.com/questions/2911215/what-happens-in-c-sharp-if-a-finally-block-throws-an-exception

                if (streamW != null) streamW.Close();
                if (fpR != null) fpR.Close();
            }

            //while (MidiTrackNames.Count <= j) { MidiTrackNames.Add("untitled " + MidiTrackNames.Count); }  // いろいろファイル名の衝突とかのバグを生みそうなので修正した
            while (MidiTrackNames.Count < j) { MidiTrackNames.Add("untitled"); }  // 不等号に等号を含めてはならない
            while (MidiInstrumentNames.Count < j) { MidiInstrumentNames.Add("untitled"); }
            return 0;
        }
    }
}