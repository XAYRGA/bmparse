using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xayrga;
using xayrga.byteglider;

namespace bmparse.bms
{
    public enum BMSPerfTarget
    { 
        VOLUME = 0,
        PITCH_WHEEL = 0x1,
        VIBRATO_FREQ = 0x9,
        VIBRATO_DELAY = 0x0A,
    }

    public enum BMSRegisterTarget
    {
        COMPARATOR = 0x03,
        GPR1 = 0x04,
        GPR2 = 0x05,
        PITCH_WHEEL_OCTAVES = 0x07,
        INSTRUMENT_BANK = 0x20,
        INSTRUMENT_PROGRAM = 0x21,
        GPR1_2 = 0x23, // (( GPR2 << 8 | GPR1)))
        LOOP_OFFSET = 0x30,

    }
    public enum BMSCommandType
    {
        INVALID = 0x00, 
        NOTE_ON = 0x01, 
        CMD_WAIT8 = 0x80,
        NOTE_OFF = 0x81, 
        CMD_WAIT16 = 0x88,
        SETPARAM_90 = 0x90, 
        SETPARAM_91 = 0x91,
        SETPARAM_92 = 0x92,
        PERF_U8_NODUR = 0x94, 
        PERF_U8_DUR_U8 = 0x96,
        PERF_U8_DUR_U16 = 0x97,
        PERF_S8_NODUR = 0x98,
        PERF_S8_DUR_U8 = 0x9A,
        PERF_S8_DUR_U16 = 0x9B,
        PERF_S16_NODUR = 0x9C,
        PERF_S16_DUR_U8 = 0x9D,
        PERF_S16_DUR_U8_9E = 0x9E,
        PERF_S16_DUR_U16 = 0x9F,
        PARAM_SET_R = 0xA0, 
        PARAM_ADD_R = 0xA1, 
        PARAM_MUL_R = 0xA2,
        PARAM_CMP_R = 0xA3,
        PARAM_SET_8 = 0xA4,
        PARAM_ADD_8 = 0xA5,
        PARAM_MUL_8 = 0xA6,
        PARAM_CMP_8 = 0xA7,
        PARAM_LOAD_UNK = 0xA8,
        PARAM_BITWISE = 0xA9,
        PARAM_LOADTBL = 0xAA,
        PARAM_SUBTRACT = 0xAB,
        PARAM_SET_16 = 0xAC,
        PARAM_ADD_16 = 0xAD,
        PARAM_MUL_16 = 0xAE,
        PARAM_CMP_16 = 0xAF,
        OPOVERRIDE_1 = 0xB0, 
        OPOVERRIDE_2 = 0xB1,
        OPOVERRIDE_4 = 0xB4,
        OPOVERRIDE_R = 0xB8,
        OPENTRACK = 0xC1,
        OPENTRACKBROS = 0xC2,
        CALL = 0xC4,
        CALLTABLE = 0xC44,
        RETURN_NOARG = 0xC5,
        RETURN = 0xC6,
        JMP = 0xC8,
        LOOP_S = 0xC9,
        LOOP_E = 0xCA,
        READPORT = 0xCB,
        WRITEPORT = 0xCC,
        CHECKPORTIMPORT = 0xCD,
        CHECKPORTEXPORT = 0xCE,
        CMD_WAITR = 0xCF,
        PARENTWRITEPORT = 0xD1,
        CHILDWRITEPORT = 0xD2,
        SETLASTNOTE = 0xD4,
        TIMERELATE = 0xD5,
        SIMPLEOSC = 0xD6,
        SIMPLEENV = 0xD7,
        SIMPLEADSR = 0xD8,
        TRANSPOSE = 0xD9,
        CLOSETRACK = 0xDA,
        OUTSWITCH = 0xDB,
        UPDATESYNC = 0xDC,
        BUSCONNECT = 0xDD,
        PAUSESTATUS = 0xDE,
        SETINTERRUPT = 0xDF,
        DISINTERRUPT = 0xE0,
        CLRI = 0xE1,
        SETI = 0xE2,
        RETI = 0xE3,
        INTTIMER = 0xE4,
        VIBDEPTH = 0xE5,
        VIBDEPTHMIDI = 0xE6,
        SYNCCPU = 0xE7,
        FLUSHALL = 0xE8,
        FLUSHRELEASE = 0xE9,
        WAIT_VLQ = 0xEA,
        PANPOWSET = 0xEB,
        IIRSET = 0xEC,
        FIRSET = 0xED,
        EXTSET = 0xEE,
        PANSWSET = 0xEF,
        OSCROUTE = 0xF0,
        IIRCUTOFF = 0xF1,
        OSCFULL = 0xF2,
        VOLUMEMODE = 0xF3,
        VIBPITCH = 0xF4,
        CHECKWAVE = 0xFA,
        PRINTF = 0xFB,
        NOP = 0xFC,
        TEMPO = 0xFD,
        TIMEBASE = 0xFE,
        FINISH = 0xFF
    }

 
    public abstract class bmscommand
    {
        public int OriginalAddress;
        public BMSCommandType CommandType;

        public abstract void read(bgReader read);
        public abstract void write(bgWriter write);
        public abstract string getAssemblyString(string[] data = null);

        internal string checkArgOverride(byte position,string @default, string[] data)
        {
            if (data==null || (data.Length <= position) || data[position] == null)                 
                return @default; ;           
            return data[position]; ;
        }

        internal string getByteString(byte[] hx)
        {
            string x = "HEX(";
            for (int i = 0; i < hx.Length; i++)
                x += $"{hx[i]:X},";
            x += ")";
            return x;
        }

    }

    public class NoteOffCommand : bmscommand
    {
        public byte Voice = 0;

        public NoteOffCommand()
        {
            CommandType = BMSCommandType.NOTE_OFF;
        }

        public override void read(bgReader read)
        {

        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)((byte)BMSCommandType.NOTE_OFF + Voice));
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"NOTEOFF {Voice:X}h");
        }
    }

    public class NoteOnCommand : bmscommand
    {


        public byte Note = 0;
        public byte Voice = 0;
        public byte Velocity = 0;
        public byte Type = 0;

        // Datatypes are actually bytes! 
        // Need this so we can signal if it's set or not without adding bools to class ;)
        public short Release = -1;
        public short Delay = -1;
        public short Length = -1;

        public NoteOnCommand()
        {
            CommandType = BMSCommandType.NOTE_ON;
        }

        public override void read(bgReader read)
        {

            var flags = read.ReadByte();
            Voice = (byte)(flags & 0x7);
            Velocity = read.ReadByte();
     
            Type = (byte)(flags >> 3);
            //Console.WriteLine(Type);

            if ((Type & 1) > 0)
            {
                Release = read.ReadByte();
                Delay = read.ReadByte();
               // Length = read.ReadByte();
            } 
            else if ((Type & 2) > 0)
            {
                Release = read.ReadByte();
                Delay = read.ReadByte();
                Length = read.ReadByte();
            }

        }

        public override void write(bgWriter write)
        {
            write.WriteBE(Note);
            byte voiceFlags = (byte)(Voice | (Type << 3));

            write.WriteBE(voiceFlags);

            write.WriteBE(Velocity);

            if ((Type & 1) > 0)
            {
               write.WriteBE((byte)Release);
               write.WriteBE((byte)Delay);
               // write.WriteBE((byte)Length);
            }
            else if ((Type & 2) > 0)
            {
                write.WriteBE((byte)Release);
                write.WriteBE((byte)Delay);
                write.WriteBE((byte)Length);
            }
        }

        public override string getAssemblyString(string[] data = null)
        {
            if ((Type & 1) > 0)
                return ($"NOTEONRD {Note:X}h {Voice:X}h {Velocity:X}h {Release:X}h {Delay:X}h");
            else if ((Type & 2) > 0)
                return ($"NOTEONRDL {Note:X}h {Voice:X}h {Velocity:X}h {Release:X}h {Delay:X}h {Length:X}h");
            else
                return ($"NOTEON {Note:X}h {Voice:X}h {Velocity:X}h");
        }
    }


    public class WaitCommand8 : bmscommand
    {
        public byte Delay; 

        public WaitCommand8()
        {
            CommandType = BMSCommandType.CMD_WAIT8;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"WAIT8 {Delay}");
        }

        public override void read(bgReader read)
        {
            Delay = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.CMD_WAIT8);
            write.WriteBE(Delay);
        }
    }

    public class WaitRegister : bmscommand
    {
        public byte Register;

        public WaitRegister()
        {
            CommandType = BMSCommandType.CMD_WAITR;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"WAITRE {Register}");
        }

        public override void read(bgReader read)
        {
            Register = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.CMD_WAIT8);
            write.WriteBE(Register);
        }
    }


    public class OutSwitch : bmscommand
    {
        public byte Register;

        public OutSwitch()
        {
            CommandType = BMSCommandType.OUTSWITCH;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"OUTSWITCH {Register}");
        }

        public override void read(bgReader read)
        {
            Register = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Register);
        }
    }



    public class WaitCommand16 : bmscommand
    {
        public ushort Delay;

        public WaitCommand16()
        {
            CommandType = BMSCommandType.CMD_WAIT16;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"WAIT16 {Delay}");
        }

        public override void read(bgReader read)
        {
            Delay = read.ReadUInt16();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.CMD_WAIT16);
            write.WriteBE(Delay);
        }
    }



    public class ParameterSet16 : bmscommand
    {
        public byte TargetParameter;
        public short Value;

        public ParameterSet16()
        {
            CommandType = BMSCommandType.PARAM_SET_16;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"PARAM16 {TargetParameter:X}h {Value}");
        }

        public override void read(bgReader read)
        {
            TargetParameter = read.ReadByte();
            Value = read.ReadInt16();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.PARAM_SET_16);
            write.WriteBE(TargetParameter);
            write.WriteBE(Value);
        }
    }

    public class ParameterAdd16 : bmscommand
    {
        public byte TargetParameter;
        public short Value;

        public ParameterAdd16()
        {
            CommandType = BMSCommandType.PARAM_ADD_16;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"ADD16 {TargetParameter:X}h {Value:X}");
        }

        public override void read(bgReader read)
        {
            TargetParameter = read.ReadByte();
            Value = read.ReadInt16();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(TargetParameter);
            write.WriteBE(Value);
        }
    }


    public class OpenTrack : bmscommand
    {
        public byte TrackID;
        public uint Address;

        public OpenTrack()
        {
            CommandType = BMSCommandType.OPENTRACK;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"OPENTRACK {TrackID:X}h {checkArgOverride(0,Address.ToString() + 'h',data)}");
        }

        public override void read(bgReader read)
        {
            TrackID = read.ReadByte();
            Address = read.ReadUInt24BE();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.OPENTRACK);
            write.WriteBE(TrackID);
            write.WriteBE(Address,true);
        }
    }


    public class Jump  : bmscommand
    {
        public byte Flags;
        public uint Address;

        public Jump()
        {
            CommandType = BMSCommandType.JMP;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"JMP {Flags:X}h {checkArgOverride(0, Address.ToString() + 'h', data)}");
        }

        public override void read(bgReader read)
        {
            Flags = read.ReadByte();
            Address = read.ReadUInt24BE();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.JMP);
            write.WriteBE(Flags);
            write.WriteBE(Address,true);
        }
    }


    public class Call : bmscommand
    {
        public byte Flags;
        public uint Address;
        public byte TargetRegister;

        public Call()
        {
            CommandType = BMSCommandType.CALL;
        }

        public override string getAssemblyString(string[] data = null)
        {
           if (Flags!=0xC0)
                return($"CALL {Flags:X}h {checkArgOverride(0, Address.ToString() + 'h',data)}");
           else
                return ($"CALLTABLE {TargetRegister:X}h {checkArgOverride(0, Address.ToString() + 'h', data)}");
        }

        public override void read(bgReader read)
        {
            Flags = read.ReadByte();

            if (Flags == 0xC0)
                TargetRegister = read.ReadByte();

            Address = read.ReadUInt24BE();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.CALL);
            write.WriteBE(Flags);
            if (Flags == 0xC0)
                write.WriteBE(TargetRegister);
            write.WriteBE(Address,true);
        }
    }

    public class SimpleEnvelope : bmscommand
    {
        public byte Flags;
        public uint Address;

        public SimpleEnvelope()
        {
            CommandType = BMSCommandType.SIMPLEENV;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"SIMPLENV {Flags:X}h {checkArgOverride(0, Address.ToString() + 'h', data)}");
        }

        public override void read(bgReader read)
        {
            Flags = read.ReadByte();
            Address = read.ReadUInt24BE();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.SIMPLEENV);
            write.WriteBE(Flags);
            write.WriteBE(Address, true);
        }
    }


    public class SetInterrupt : bmscommand
    {
        public byte InterruptLevel;
        public uint Address;

        public SetInterrupt()
        {
            CommandType = BMSCommandType.SETINTERRUPT;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"SETINTER {InterruptLevel:X}h {checkArgOverride(0, Address.ToString() + 'h', data)}");
        }

        public override void read(bgReader read)
        {
            InterruptLevel = read.ReadByte();
            Address = read.ReadUInt24BE();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.SETINTERRUPT);
            write.WriteBE(InterruptLevel);
            write.WriteBE(Address, true);
        }
    }

    public class  OpOverride4 : bmscommand
    {
        public byte Instruction;
        public byte ArgumentMask;
        public byte[] Stupid;
        public byte[] ArgumentMaskLookup;

        public OpOverride4()
        {
            CommandType = BMSCommandType.OPOVERRIDE_4;


        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"CRINGE4 {Instruction:X}h {ArgumentMask:X}h {getByteString(ArgumentMaskLookup)} {getByteString(Stupid)}");
        }

        public override void read(bgReader read)
        {
            Instruction = read.ReadByte();
            ArgumentMask = read.ReadByte();
            var stupid_size = 0;
            // todo: get your free hardcoded sizes
            // fuck you , by the way. 
            switch (Instruction)
            {
                default:
                    throw new Exception($"oof 0x{Instruction:X}");
            }
            Stupid = new byte[stupid_size];
            ArgumentMaskLookup = new byte[4] // fuck this in particular
            {
                  read.ReadByte(),
                  read.ReadByte(), 
                  read.ReadByte(),
                  read.ReadByte(),
            };
        }


        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.OPOVERRIDE_4);
            write.WriteBE(Instruction);
            write.WriteBE(ArgumentMask);
            write.Write(Stupid);
            write.Write(ArgumentMaskLookup);
        }
    }


    public class OpOverride1 : bmscommand
    {
        public byte Instruction;
        public byte ArgumentMask;
        public byte[] Stupid;
        public byte[] ArgumentMaskLookup;

        public OpOverride1()
        {
            CommandType = BMSCommandType.OPOVERRIDE_1;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"CRINGE1 {Instruction:X}h {ArgumentMask:X}h {getByteString(ArgumentMaskLookup)} {getByteString(Stupid)}");
        }

        public override void read(bgReader read)
        {
            Instruction = read.ReadByte();
            ArgumentMask = read.ReadByte();
            var stupid_size = 0;
            // todo: get your free hardcoded sizes
            switch (Instruction)
            {
                case 0xC9:
                case 0xD4:
                    stupid_size = 1;
                    break;
                default:
                    throw new Exception($"oof 0x{Instruction:X}");
            }
            Stupid = new byte[stupid_size];
            ArgumentMaskLookup = new byte[1]
            {
                  read.ReadByte(),
    
            };
        }


        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.OPOVERRIDE_4);
            write.WriteBE(Instruction);
            write.WriteBE(ArgumentMask);
            write.Write(Stupid);
            write.Write(ArgumentMaskLookup);
        }
    }

    public class PrintF : bmscommand
    {
        public string Message = "";
        public byte[] RegisterReferences;

        public PrintF()
        {
            CommandType = BMSCommandType.PRINTF;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"PRINT \"{Message}\" {getByteString(RegisterReferences)}");
        }

        public override void read(bgReader read)
        {
            var references = 0;
            char last;
            while ((last = (char)read.ReadByte()) != 0x00)
            {
                if (last == '%')
                    references++;
                Message += last;
            }
            RegisterReferences = read.ReadBytes(references);
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.PRINTF);
            write.Write(Encoding.ASCII.GetBytes(Message));
            write.WriteBE((byte)0x00);
            write.Write(RegisterReferences); 
        }
    }


    public class CloseTrack : bmscommand
    {
        public byte TrackID; 

        public CloseTrack()
        {
            CommandType = BMSCommandType.CLOSETRACK;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"CLOSETRK {TrackID:X}h");
        }

        public override void read(bgReader read)
        {
            TrackID = read.ReadByte();       
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.CLOSETRACK);
            write.WriteBE(TrackID);
        }
    }


    public class PanSweepSet: bmscommand
    {
        public byte A;
        public byte B;
        public byte C;

        public PanSweepSet()
        {
            CommandType = BMSCommandType.PANSWSET;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"PANSWEEP {A:X}h {B:X}h {C:X}h");
        }

        public override void read(bgReader read)
        {
            A = read.ReadByte();
            B = read.ReadByte();
            C = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(A);
            write.WriteBE(B);
            write.WriteBE(C);

        }
    }


    public class BusConnect : bmscommand
    {
        public byte A;
        public byte B;
        public byte C;

        public BusConnect()
        {
            CommandType = BMSCommandType.BUSCONNECT;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"BUSCONNECT {A:X}h {B:X}h {C:X}h ");
        }

        public override void read(bgReader read)
        {
            A = read.ReadByte();
            B = read.ReadByte();
            C = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(A);
            write.WriteBE(B);
            write.WriteBE(C);

        }
    }

    public class SimpleOscillator : bmscommand
    {
        public byte OscID;

        public SimpleOscillator()
        {
            CommandType = BMSCommandType.SIMPLEOSC;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"SIMPLEOSC {OscID:X}h");
        }

        public override void read(bgReader read)
        {
            OscID = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(OscID);
        }

    }

    public class Transpose : bmscommand
    {
        public byte Transposition;

        public Transpose()
        {
            CommandType = BMSCommandType.TRANSPOSE;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"TRANSPOSE {Transposition:X}h");
        }

        public override void read(bgReader read)
        {
            Transposition = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.TRANSPOSE);
            write.WriteBE(Transposition);
        }

    }

    public class OscillatorRoute : bmscommand
    {
        public byte Switch;

        public OscillatorRoute()
        {
            CommandType = BMSCommandType.OSCROUTE;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"OSCROUTE {Switch:X}h");
        }

        public override void read(bgReader read)
        {
            Switch = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.OSCROUTE);
            write.WriteBE(Switch);
        }
    }

    public class VibratoDepth: bmscommand
    {
        public byte Depth;

        public VibratoDepth()
        {
            CommandType = BMSCommandType.VIBDEPTH;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"VIBDEPTH {Depth:X}h");
        }

        public override void read(bgReader read)
        {
            Depth = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.VIBDEPTH);
            write.WriteBE(Depth);
        }
    }

    public class VibratoDepthMidi : bmscommand
    {
        public byte Depth;
        public byte Unk;

        public VibratoDepthMidi()
        {
            CommandType = BMSCommandType.VIBDEPTHMIDI;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"VIBDEPTHMIDI {Depth:X}h {Unk:X}h");
        }

        public override void read(bgReader read)
        {
            Depth = read.ReadByte();
            Unk = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.VIBDEPTHMIDI);
            write.WriteBE(Depth);
        }
    }

    public class VibratoPitch : bmscommand
    {
        public byte Pitch;

        public VibratoPitch()
        {
            CommandType = BMSCommandType.VIBPITCH;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"VIBPITCH {Pitch:X}h");
        }

        public override void read(bgReader read)
        {
            Pitch = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.VIBPITCH);
            write.WriteBE(Pitch);
        }
    }

    public class IIRCutoff : bmscommand
    {
        public byte Cutoff;

        public IIRCutoff()
        {
            CommandType = BMSCommandType.IIRCUTOFF;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"IIRC {Cutoff:X}h");
        }

        public override void read(bgReader read)
        {
            Cutoff = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)BMSCommandType.IIRCUTOFF);
            write.WriteBE(Cutoff);
        }
    }

    public class SimpleADSR : bmscommand
    {
        public short Attack;
        public short Decay;
        public short Sustain;
        public short Release;
        public short Unknown;

        public SimpleADSR()
        {
            CommandType = BMSCommandType.SIMPLEADSR;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"SIMADSR {Attack} {Decay} {Sustain} {Release} {Unknown}");
        }

        public override void read(bgReader read)
        {
            Attack = read.ReadInt16();
            Decay = read.ReadInt16();
            Sustain = read.ReadInt16();
            Release = read.ReadInt16();
            Unknown = read.ReadInt16();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Attack);
            write.WriteBE(Decay);
            write.WriteBE(Sustain);
            write.WriteBE(Release);
            write.WriteBE(Unknown);
        }
    }


    public class ClearInterrupt : bmscommand
    {
        public ClearInterrupt()
        {
            CommandType = BMSCommandType.CLRI;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"CLEINT");
        }

        public override void read(bgReader read)
        {
  
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
        }
    }

    public class ReturnInterrupt : bmscommand
    {
        public ReturnInterrupt()
        {
            CommandType = BMSCommandType.RETI;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"RETINT");
        }

        public override void read(bgReader read)
        {

        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
        }
    }

    public class FlushAll : bmscommand
    {
        public FlushAll()
        {
            CommandType = BMSCommandType.FLUSHALL;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"FLUSHALL");
        }

        public override void read(bgReader read)
        {

        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
        }
    }

    public class ReadPort : bmscommand
    {
        public byte Source;
        public byte Destination; 

        public ReadPort()
        {
            CommandType = BMSCommandType.READPORT;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"READPORT {Source}h {Destination:x}h");
        }

        public override void read(bgReader read)
        {
            Source = read.ReadByte();
            Destination = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Source);
            write.WriteBE(Destination);
        }
    }


    public class WritePort : bmscommand
    {
        public byte Source;
        public byte Destination;

        public WritePort()
        {
            CommandType = BMSCommandType.WRITEPORT;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"WRITEPORT {Source}h {Destination:x}h");
        }

        public override void read(bgReader read)
        {
            Source = read.ReadByte();
            Destination = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Source);
            write.WriteBE(Destination);
        }
    }

    public class ChildWritePort : bmscommand
    {
        public byte Source;
        public byte Destination;

        public ChildWritePort()
        {
            CommandType = BMSCommandType.CHILDWRITEPORT;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"CHILDWP {Source:X}h {Destination:X}h");
        }

        public override void read(bgReader read)
        {
            Source = read.ReadByte();
            Destination = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Source);
            write.WriteBE(Destination);
        }
    }


    public class PERFS8DURU16 : bmscommand
    {
        public byte Parameter;
        public sbyte Value;
        public ushort Duration;

        public PERFS8DURU16()
        {
            CommandType = BMSCommandType.PERF_S8_DUR_U16;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"TPRMS8_DU16 {Parameter:X}h {Value} {Duration:X}h");
        }

        public override void read(bgReader read)
        {
            Parameter = read.ReadByte();
            Value = read.ReadSByte();
            Duration = read.ReadUInt16();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Parameter);
            write.WriteBE(Value);
            write.WriteBE(Duration);
        }
    }

    public class PERFS16 : bmscommand
    {
        public byte Parameter;
        public short Value;


        public PERFS16()
        {
            CommandType = BMSCommandType.PERF_S16_NODUR;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"TPRMU8 {Parameter:X}h {Value}");
        }

        public override void read(bgReader read)
        {
            Parameter = read.ReadByte();
            Value = read.ReadInt16(); 
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Parameter);
            write.WriteBE(Value);
        }
    }


    public class PERFU8 : bmscommand
    {
        public byte Parameter;
        public byte Value;


        public PERFU8()
        {
            CommandType = BMSCommandType.PERF_U8_NODUR;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"TPRMU8 {Parameter:X}h {Value}");
        }

        public override void read(bgReader read)
        {
            Parameter = read.ReadByte();
            Value = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Parameter);
            write.WriteBE(Value);
        }
    }

    public class PERFS16U89E : bmscommand
    {
        public byte Parameter;
        public short Value;
        public byte Unknown;

        public PERFS16U89E()
        {
            CommandType = BMSCommandType.PERF_S16_DUR_U8_9E;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"TPRMS16_DU8_9E {Parameter:X}h {Value} {Unknown:X}h");
        }

        public override void read(bgReader read)
        {
            Parameter = read.ReadByte();
            Value = read.ReadInt16();
            Unknown = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Parameter);
            write.WriteBE(Value);
            write.WriteBE(Unknown);
        }
    }

    public class PERFS16DURU8 : bmscommand
    {
        public byte Parameter;
        public short Value;
        public byte Duration;

        public PERFS16DURU8()
        {
            CommandType = BMSCommandType.PERF_S16_DUR_U8;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"TPRMS16_DU8 {Parameter:X}h {Value} {Duration:X}h");
        }

        public override void read(bgReader read)
        {
            Parameter = read.ReadByte();
            Value = read.ReadInt16();
            Duration = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Parameter);
            write.WriteBE(Value);
            write.WriteBE(Duration);
        }
    }

    public class PERFS8 : bmscommand
    {
        public byte Parameter;
        public sbyte Value;


        public PERFS8()
        {
            CommandType = BMSCommandType.PERF_S8_NODUR;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"TPRMS8 {Parameter:X}h {Value}");
        }

        public override void read(bgReader read)
        {
            Parameter = read.ReadByte();
            Value = read.ReadSByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Parameter);
            write.WriteBE(Value);
        }
    }

    public class PERFS8DURU8 : bmscommand
    {
        public byte Parameter;
        public sbyte Value;
        public byte Duration;


        public PERFS8DURU8()
        {
            CommandType = BMSCommandType.PERF_S8_DUR_U8;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"TPRMS8_DU8 {Parameter:X}h {Value} {Duration:X}h");
        }

        public override void read(bgReader read)
        {
            Parameter = read.ReadByte();
            Value = read.ReadSByte();
            Duration = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Parameter);
            write.WriteBE(Value);
            write.WriteBE(Duration);
        }
    }

    public class ParameterSetRegister : bmscommand
    {
        public byte Source;
        public byte Destination;

        public ParameterSetRegister()
        {
            CommandType = BMSCommandType.PARAM_SET_R;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"PARAMREG {Source:X}h {Destination}");
        }

        public override void read(bgReader read)
        {
            Source = read.ReadByte();
            Destination = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Source);
            write.WriteBE(Destination);
        }
    }


    public class ParameterAddRegister : bmscommand
    {
        public byte Source;
        public byte Destination;

        public ParameterAddRegister()
        {
            CommandType = BMSCommandType.PARAM_ADD_R;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"ADDR {Source:X}h {Destination:X}h");
        }

        public override void read(bgReader read)
        {
            Source = read.ReadByte();
            Destination = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Source);
            write.WriteBE(Destination);
        }
    }

    public class ParameterSubtract : bmscommand
    {
        public byte Source;
        public byte Destination;

        public ParameterSubtract()
        {
            CommandType = BMSCommandType.PARAM_SUBTRACT;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"SUB8 {Source:X}h {Destination:X}h");
        }

        public override void read(bgReader read)
        {
            Source = read.ReadByte();
            Destination = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Source);
            write.WriteBE(Destination);
        }
    }

    public class ParameterSet8 : bmscommand
    {
        public byte TargetParameter;
        public byte Value;

        public ParameterSet8()
        {
            CommandType = BMSCommandType.PARAM_SET_8;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"PARAM8 {TargetParameter:X}h {Value}");
        }

        public override void read(bgReader read)
        {
            TargetParameter = read.ReadByte();
            Value = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(TargetParameter);
            write.WriteBE(Value);
        }
    }



    public class ParameterAdd8 : bmscommand
    {
        public byte Source;
        public byte Value;

        public ParameterAdd8()
        {
            CommandType = BMSCommandType.PARAM_ADD_8;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"ADD8 {Source:X}h {Value:X}h");
        }

        public override void read(bgReader read)
        {
            Source = read.ReadByte();
            Value = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Source);
            write.WriteBE(Value);
        }
    }


    public class ParameterMultiply8 : bmscommand
    {
        public byte Source;
        public byte Value;

        public ParameterMultiply8()
        {
            CommandType = BMSCommandType.PARAM_MUL_8;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"MUL8 {Source:X}h {Value:X}h");
        }

        public override void read(bgReader read)
        {
            Source = read.ReadByte();
            Value = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Source);
            write.WriteBE(Value);
        }
    }

    public class ParameterCompare8 : bmscommand
    {
        public byte Source;
        public byte Value;

        public ParameterCompare8()
        {
            CommandType = BMSCommandType.PARAM_CMP_8;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"CMP8 {Source:X}h {Value:X}h");
        }

        public override void read(bgReader read)
        {
            Source = read.ReadByte();
            Value = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Source);
            write.WriteBE(Value);
        }
    }

    public class ParameterCompareRegister : bmscommand
    {
        public byte Source;
        public byte Register;

        public ParameterCompareRegister()
        {
            CommandType = BMSCommandType.PARAM_CMP_R;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"CMPR {Source:X}h {Register:X}h");
        }

        public override void read(bgReader read)
        {
            Source = read.ReadByte();
            Register = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Source);
            write.WriteBE(Register);
        }
    }

    public class ParameterSet8_90 : bmscommand
    {
        public byte Source;
        public byte Value;

        public ParameterSet8_90()
        {
            CommandType = BMSCommandType.SETPARAM_90;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"SETPARAM90 {Source:X}h {Value:X}h");
        }

        public override void read(bgReader read)
        {
            Source = read.ReadByte();
            Value = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Source);
            write.WriteBE(Value);
        }
    }

    public class ParameterSet16_92 : bmscommand
    {
        public byte Source;
        public short Value;

        public ParameterSet16_92()
        {
            CommandType = BMSCommandType.SETPARAM_92;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"SETPARAM92 {Source:X}h {Value:X}h");
        }

        public override void read(bgReader read)
        {
            Source = read.ReadByte();
            Value = read.ReadInt16();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Source);
            write.WriteBE(Value);
        }
    }

    public class PanPowerSet : bmscommand
    {
        public byte A;
        public byte B;
        public byte C;
        public byte D;
        public byte E;

        public PanPowerSet()
        {
            CommandType = BMSCommandType.PANPOWSET;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"PANPOWSET {A} {B} {C} {D} {E}");
        }

        public override void read(bgReader read)
        {
            A = read.ReadByte();
            B = read.ReadByte();
            C = read.ReadByte();
            D = read.ReadByte();
            E = read.ReadByte();

        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(A);
            write.WriteBE(B);
            write.WriteBE(C);
            write.WriteBE(D);
            write.WriteBE(E);        
        }
    }



    public class SetLastNote : bmscommand
    {
        public byte Note;
   

        public SetLastNote()
        {
            CommandType = BMSCommandType.SETLASTNOTE;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"SETLAST {Note:X}h");
        }

        public override void read(bgReader read)
        {
            Note = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Note);
        }
    }

    public class LoopStart : bmscommand
    {
        public byte Count;


        public LoopStart()
        {
            CommandType = BMSCommandType.LOOP_S;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"LOOPSTART {Count:X}h");
        }

        public override void read(bgReader read)
        {
            Count = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Count);
        }
    }


    public class ParamBitwise : bmscommand
    {
        public byte Flags;
        public byte A;
        public byte B;
        public byte C;
           


        public ParamBitwise()
        {
            CommandType = BMSCommandType.PARAM_BITWISE;
        }

        public override string getAssemblyString(string[] data = null)
        {
            if ((Flags & 0xF) == 0xC)
                return ($"BITWZC {A:X}h {B:X}h {C:X}h");
            else if ((Flags & 0xF) == 0x8)
                return ($"BITWZ8 {A:X}h ");
            else
                return ($"BITWZ {A:X}h {B:X}h");
        }

        public override void read(bgReader read)
        {
            Flags = read.ReadByte();

            if ((Flags & 0xF) == 0xC)
            {
                A = read.ReadByte();
                B = read.ReadByte();
                C = read.ReadByte();
            }
            else if ((Flags & 0xF) == 0x8)
            {
                A = read.ReadByte();
            }
            else
            {
                A = read.ReadByte();
                B = read.ReadByte();
            }
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Flags);

            if ((Flags & 0xF) == 0xC)
            {
                write.WriteBE(A);
                write.WriteBE(B);
                write.WriteBE(C);
            }
            else if ((Flags & 0xF) == 0x8)
            {
                write.WriteBE(A);
            }
            else
            {
                write.WriteBE(A);
                write.WriteBE(B);
            }
        }
    }


    public class LoopEnd : bmscommand
    {
        public LoopEnd()
        {
            CommandType = BMSCommandType.LOOP_E;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"LOOPEND");
        }

        public override void read(bgReader read)
        {
  
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
        }
    }


    public class SyncCpu : bmscommand
    {
        public ushort Value;

        public SyncCpu()
        {
            CommandType = BMSCommandType.SYNCCPU;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"SYNC {Value:X}h");
        }

        public override void read(bgReader read)
        {
            Value = read.ReadUInt16();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Value);
        }
    }

    public class Tempo : bmscommand
    {
        public ushort BeatsPerMinute;

        public Tempo()
        {
            CommandType = BMSCommandType.TEMPO;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"TEMPO {BeatsPerMinute}");
        }

        public override void read(bgReader read)
        {
            BeatsPerMinute = read.ReadUInt16();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(BeatsPerMinute);
        }
    }

    public class Timebase : bmscommand
    {
        public ushort PulsesPerQuarterNote;

        public Timebase()
        {
            CommandType = BMSCommandType.TIMEBASE;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"TIMEBASE {PulsesPerQuarterNote}");
        }

        public override void read(bgReader read)
        {
            PulsesPerQuarterNote = read.ReadUInt16();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(PulsesPerQuarterNote);
        }
    }

    public class Return : bmscommand
    {
        public byte Condition;


        public Return()
        {
            CommandType = BMSCommandType.RETURN;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"RETURN {Condition:X}h");
        }

        public override void read(bgReader read)
        {
            Condition = read.ReadByte();
        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
            write.WriteBE(Condition);
        }
    }

    public class ReturnNoArg : bmscommand
    {
        public ReturnNoArg()
        {
            CommandType = BMSCommandType.RETURN_NOARG;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"RETIMM");
        }

        public override void read(bgReader read)
        {

        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
        }
    }

    public class Finish : bmscommand
    {
        public Finish()
        {
            CommandType = BMSCommandType.FINISH;
        }

        public override string getAssemblyString(string[] data = null)
        {
            return ($"FINISH");
        }

        public override void read(bgReader read)
        {

        }

        public override void write(bgWriter write)
        {
            write.WriteBE((byte)CommandType);
        }
    }
}
