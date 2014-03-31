using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/* 
-----------------------------------
BMS Channels

01	[objs/wav] BGM
02	[number] Measure Length
03	[objs/number] BPM
04	[objs/bmp] BGA-BASE
05
06	[objs/bmp] BGA-POOR
07	[objs/bmp] BGA-LAYER
08	[objs/tempo] Extended BPM
09	[objs/stop] Stop Sequence

1x	[objs/wav] 1 Player
2x	[objs/wav] 2 Player
3x	[objs/wav] 1 Player Invisible
4x	[objs/wav] 2 Player Invisible
5x	[objs/wav] 1 Player Long Note
6x	[objs/wav] 2 Player Long Note

99	[objs/text]
A0	[objs/exrank]
Dx	[objs/wav] 1 Player Mine
Ex	[objs/wav] 2 Player Mine
-----------------------------------
BMS Definitions

#WAVxx	[xx = obj/wav] [filename]
#BMPxx	[xx = obj/bmp] [filename]  // 動画も可

#BPMxx	[xx = obj/bpm] [number]
// #BGAxx	[xx = obj/bmp??] [filename]
#STOPxx	[xx = onj/stop] [number]  // 1は192分音符相当

#LNOBJ	[obj/wav]

*/

namespace Mid2BMS
{
    /// <summary>
    /// BMSにおけるオブジェを表します。これは、BMSに配置された、２桁の３６進数です。
    /// ロングノートは始点と終点を個別に持ちます。
    /// ん！？これはreadonlyじゃないぞ！！！！殺せーーー！！！！
    /// コピーコンストラクタの呼び出し（代入のこと）を禁止できればいいんだけれど・・・
    /// なぜ値型にしたのかというと、ヒープにメモリを確保するオーバーヘッドを回避したかったから。
    /// でもstructってスタックに確保してくれるんでしょうか？不変型にするとnewの回数が増えると思うのですが
    /// ＞ただし、構造体をインスタンス化した場合は、スタックに作成されます。これによりパフォーマンスが向上します。
    /// ＞http://msdn.microsoft.com/ja-jp/library/aa288471(v=vs.71).aspx
    /// あーそりゃそうか
    /// </summary>
    struct BMSObject : IComparable<BMSObject>
    {
        public BMSObject(int channel, int wavid, Frac time)
        {
            ch = channel;
            wav = wavid;
            t = time;
        }

        public int CompareTo(BMSObject b)
        {
            return t.CompareTo(b.t);
        }

        public int ch;  // in Hex (ex. Lane26(2PSC) is 38 )
        public int wav;  // in 36th
        public Frac t;

        public BMSObjectType Type
        {
            get
            {
                return BMSParser.BMSChannelToObjectType(ch);
            }
        }

        public override string ToString()
        {
            long n2 = t.n;
            long d2 = t.d;
            while (d2 < 16)
            {
                n2 *= 2;
                d2 *= 2;
            }
            return "#" + (n2 / d2).ToString("D3") + "\t" + (n2 % d2) + "/" + d2 + "\t#WAV" + BMSParser.IntToHex36Upper(wav);
        }
    }
}
