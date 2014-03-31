using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    /// <summary>
    /// Midiイベント、SysExイベント、又は
    /// </summary>
    abstract class MidiEvent : IComparable
    {
        public int tick;
        public int ch;
        
        public MidiEvent() { }
        public MidiEvent(MidiEvent me)
        {
            this.tick = me.tick;
            this.ch = me.ch;
        }
        public abstract MidiEvent Clone();

        public int CompareTo(object obj)
        {
            if (obj is MidiEvent)
            {
                MidiEvent me2 = (MidiEvent)obj;

                if (this.tick != me2.tick) return this.tick - me2.tick;
                if (this is MidiEventMeta && ((MidiEventMeta)this).val == 0x2F) return 1;
                if (me2 is MidiEventMeta && ((MidiEventMeta)me2).val == 0x2F) return -1;

                if (this is MidiEventNote && me2 is MidiEventNote
                    && ((MidiEventNote)this).v != ((MidiEventNote)me2).v)
                    return ((MidiEventNote)this).v - ((MidiEventNote)me2).v;  // ノートオフが先

                return 0;
            }
            else
            {
                throw new ArgumentException("MidiEventではない何か");
            }
        }
        public abstract override string ToString();  // 改行で終了しなければならない
    }

    class MidiEventNote : MidiEvent
    {
        // 8n, 9n
        public int n;
        public int v;  // v == 0 でノートオフを表すわけではない！！！と思ったけどノートオフは実装やめよう
        public int q;  // gate. 実質的な音符の長さ
        
        public MidiEventNote() { }
        public MidiEventNote(MidiEventNote me)
            : base(me)
        {
            this.n = me.n;
            this.v = me.v;
            this.q = me.q;
        }
        public override MidiEvent Clone() { return new MidiEventNote(this); }

        public override string ToString()
        {
            return "t=" + tick + "\tch=" + (ch + 1) +
                "\tNote\tn=" + n + "\tv=" + v + "\tq=" + q + "\n";
        }

    }

    class MidiEventCC : MidiEvent
    {
        // Bn
        public int cc;
        public int val;
        
        public MidiEventCC() { }
        public MidiEventCC(MidiEventCC me)
            : base(me)
        {
            this.cc = me.cc;
            this.val = me.val;
        }
        public override MidiEvent Clone() { return new MidiEventCC(this); }

        public override string ToString()
        {
            return "t=" + tick + "\tch=" + (ch + 1) +
                "\tCC " + cc + " = " + val + "\n";
        }
    }
    class MidiEventKeyPressure : MidiEvent
    {
        // An
        public int n;
        public int val;
        
        public MidiEventKeyPressure() { }
        public MidiEventKeyPressure(MidiEventKeyPressure me)
            : base(me)
        {
            this.n = me.n;
            this.val = me.val;
        }
        public override MidiEvent Clone() { return new MidiEventKeyPressure(this); }

        public override string ToString()
        {
            return "t=" + tick + "\tch=" + (ch + 1) +
                "\tPolyphonicKeyPressure\tn=" + n + "\tval=" + val + "\n";
        }
    }
    class MidiEventChannelPressure : MidiEvent
    {
        // Dn
        public int val;
       
        public MidiEventChannelPressure() { }
        public MidiEventChannelPressure(MidiEventChannelPressure me)
            : base(me)
        {
            this.val = me.val;
        }
        public override MidiEvent Clone() { return new MidiEventChannelPressure(this); }

        public override string ToString()
        {
            return "t=" + tick + "\tch=" + (ch + 1) +
                "\tChannelPressure\tval=" + val + "\n";
        }
    }
    class MidiEventProgram : MidiEvent
    {
        // Cn
        public int val;
        
        public MidiEventProgram() { }
        public MidiEventProgram(MidiEventProgram me)
            : base(me)
        {
            this.val = me.val;
        }
        public override MidiEvent Clone() { return new MidiEventProgram(this); }

        public override string ToString()
        {
            return "t=" + tick + "\tch=" + (ch + 1) +
                "\tProgramChange\tval=" + val + "\n";
        }
    }
    class MidiEventPB : MidiEvent
    {
        // En
        public int val;
        
        public MidiEventPB() { }
        public MidiEventPB(MidiEventPB me)
            : base(me)
        {
            this.val = me.val;
        }
        public override MidiEvent Clone() { return new MidiEventPB(this); }

        public override string ToString()
        {
            return "t=" + tick + "\tch=" + (ch + 1) +
                "\tPitchBend\tval=" + val + "\n";
        }
    }
    class MidiEventSysEx : MidiEvent
    {
        // 0xF0, 0xF7
        public byte[] bytes;  // 注意：頭のF0を含まない
        public int stbyte;

        public MidiEventSysEx() { }
        public MidiEventSysEx(MidiEventSysEx me)
            : base(me)
        {
            this.bytes = (byte[])me.bytes.Clone();
            int stbyte = me.stbyte;
        }
        public override MidiEvent Clone() { return new MidiEventSysEx(this); }

        public override string ToString()
        {
            return "t=" + tick + "\t" + //"\tch=" + ch +
                "\tSysEx\tbytes = " + BitConverter.ToString(bytes) + "\n";
        }
    }
    class MidiEventMeta : MidiEvent
    {
        // statusbyte == 0xFF
        public int id;

        //public String name;  // ex: "TrackName"
        public byte[] bytes;  // ex: "TrackName" (in byte[])
        public String text;  // ex: "Piano 1"
        public int val;  // tempo, port, etc...

        public MidiEventMeta() { }
        public MidiEventMeta(MidiEventMeta me)
            : base(me)
        {
            //this.name = me.name;
            this.bytes = (byte[])me.bytes.Clone();
            this.text = me.text;
            this.val = me.val;
            this.id = me.id;
        }
        public override MidiEvent Clone() { return new MidiEventMeta(this); }

        public override string ToString()
        {
            String text2 = text;
            switch (id)
            {
                case 0x20:
                case 0x21:
                case 0x51:
                    text = val.ToString();
                    break;
            }
            return "t=" + tick + "\t" + //"\tch=" + ch +
                "\tMeta " + name + " = " + text + "\n";
        }

        public String name
        {
            get
            {
                switch (this.id)
                {
                    case 0x03:  // Track Name
                        return "Track Name";
                    case 0x04:
                        return "Instrument Name";
                    case 0x05:
                        return "Lyric";
                    case 0x06:
                        return "Marker";

                    case 0x21:
                        return "Port";
                    case 0x2F:  // End of Track
                        return "End of Track";
                    case 0x51:
                        return "Tempo (usec per beat)";
                    case 0x54:
                        return "SMPTE Offset";
                    case 0x58:
                        return "Signature";
                    case 0x59:
                        return "Key";
                    default:
                        return "Other (" + this.id + ")";
                }
            }
        }
    }
}
