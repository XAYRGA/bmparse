using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;

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

        public abstract void read(BeBinaryReader read);
        public abstract void write(BeBinaryWriter write);

    }

    public class NoteOffCommand : bmscommand
    {
        public byte Voice = 0;

        public NoteOffCommand()
        {
            CommandType = BMSCommandType.NOTE_OFF;
        }

        public override void read(BeBinaryReader read)
        {
            

        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)((byte)BMSCommandType.NOTE_OFF + Voice));
        
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

        public override void read(BeBinaryReader read)
        {

            var flags = read.ReadByte();
            Voice = (byte)(flags & 0x7);
            Velocity = read.ReadByte();
     
            Type = (byte)(flags >> 3);
            Console.WriteLine(Type);

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

        public override void write(BeBinaryWriter write)
        {
            write.Write(Note);
            byte voiceFlags = (byte)(Voice | (Type << 3));

            write.Write(voiceFlags);

            write.Write(Velocity);

            if ((Type & 1) > 0)
            {
                write.Write((byte)Release);
               write.Write((byte)Delay);
               // write.Write((byte)Length);
            }
            else if ((Type & 2) > 0)
            {
                write.Write((byte)Release);
                write.Write((byte)Delay);
                write.Write((byte)Length);
            }
        }
    }

    public class WaitCommand8 : bmscommand
    {
        public byte Delay; 

        public WaitCommand8()
        {
            CommandType = BMSCommandType.CMD_WAIT8;
        }

        public override void read(BeBinaryReader read)
        {
            Delay = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.CMD_WAIT8);
            write.Write(Delay);
        }
    }

    public class WaitRegister : bmscommand
    {
        public byte Register;

        public WaitRegister()
        {
            CommandType = BMSCommandType.CMD_WAITR;
        }

        public override void read(BeBinaryReader read)
        {
            Register = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.CMD_WAIT8);
            write.Write(Register);
        }
    }


    public class OutSwitch : bmscommand
    {
        public byte Register;

        public OutSwitch()
        {
            CommandType = BMSCommandType.OUTSWITCH;
        }

        public override void read(BeBinaryReader read)
        {
            Register = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Register);
        }
    }



    public class WaitCommand16 : bmscommand
    {
        public short Delay;

        public WaitCommand16()
        {
            CommandType = BMSCommandType.CMD_WAIT16;
        }

        public override void read(BeBinaryReader read)
        {
            Delay = read.ReadInt16();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.CMD_WAIT16);
            write.Write(Delay);
        }
    }



    public class ParamSet16 : bmscommand
    {
        public byte TargetParameter;
        public short Value;

        public ParamSet16()
        {
            CommandType = BMSCommandType.PARAM_SET_16;
        }

        public override void read(BeBinaryReader read)
        {
            TargetParameter = read.ReadByte();
            Value = read.ReadInt16();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.PARAM_SET_16);
            write.Write(TargetParameter);
            write.Write(Value);
        }
    }

    public class ParamAdd16 : bmscommand
    {
        public byte TargetParameter;
        public short Value;

        public ParamAdd16()
        {
            CommandType = BMSCommandType.PARAM_ADD_16;
        }

        public override void read(BeBinaryReader read)
        {
            TargetParameter = read.ReadByte();
            Value = read.ReadInt16();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(TargetParameter);
            write.Write(Value);
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

        public override void read(BeBinaryReader read)
        {
            TrackID = read.ReadByte();
            Address = read.ReadU24();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.OPENTRACK);
            write.Write(TrackID);
            write.Write(Address,true);
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

        public override void read(BeBinaryReader read)
        {
            Flags = read.ReadByte();
            Address = read.ReadU24();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.JMP);
            write.Write(Flags);
            write.Write(Address,true);
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

        public override void read(BeBinaryReader read)
        {
            Flags = read.ReadByte();

            if (Flags == 0xC0)
                TargetRegister = read.ReadByte();

            Address = read.ReadU24();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.CALL);
            write.Write(Flags);
            if (Flags == 0xC0)
                write.Write(TargetRegister);
            write.Write(Address,true);
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

        public override void read(BeBinaryReader read)
        {
            Flags = read.ReadByte();
            Address = read.ReadU24();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.SIMPLEENV);
            write.Write(Flags);
            write.Write(Address, true);
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

        public override void read(BeBinaryReader read)
        {
            InterruptLevel = read.ReadByte();
            Address = read.ReadU24();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.SETINTERRUPT);
            write.Write(InterruptLevel);
            write.Write(Address, true);
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

        public override void read(BeBinaryReader read)
        {
            Instruction = read.ReadByte();
            ArgumentMask = read.ReadByte();
            var stupid_size = 0;
            // todo: get your free hardcoded sizes
            switch (Instruction)
            {
                default:
                    throw new Exception($"oof 0x{Instruction:X}");
            }
            Stupid = new byte[stupid_size];
            ArgumentMaskLookup = new byte[4]
            {
                  read.ReadByte(),
                  read.ReadByte(),
                  read.ReadByte(),
                  read.ReadByte(),
            };
        }


        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.OPOVERRIDE_4);
            write.Write(Instruction);
            write.Write(ArgumentMask);
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

        public override void read(BeBinaryReader read)
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


        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.OPOVERRIDE_4);
            write.Write(Instruction);
            write.Write(ArgumentMask);
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

        public override void read(BeBinaryReader read)
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

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.PRINTF);
            write.Write(Encoding.ASCII.GetBytes(Message));
            write.Write((byte)0x00);
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

        public override void read(BeBinaryReader read)
        {
            TrackID = read.ReadByte();       
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.CLOSETRACK);
            write.Write(TrackID);
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

        public override void read(BeBinaryReader read)
        {
            A = read.ReadByte();
            B = read.ReadByte();
            C = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(A);
            write.Write(B);
            write.Write(C);

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

        public override void read(BeBinaryReader read)
        {
            A = read.ReadByte();
            B = read.ReadByte();
            C = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(A);
            write.Write(B);
            write.Write(C);

        }
    }

    public class SimpleOsc : bmscommand
    {
        public byte OscID;

        public SimpleOsc()
        {
            CommandType = BMSCommandType.CLOSETRACK;
        }

        public override void read(BeBinaryReader read)
        {
            OscID = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.CLOSETRACK);
            write.Write(OscID);
        }

    }

    public class Transpose : bmscommand
    {
        public byte Transp;

        public Transpose()
        {
            CommandType = BMSCommandType.TRANSPOSE;
        }

        public override void read(BeBinaryReader read)
        {
            Transp = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.TRANSPOSE);
            write.Write(Transp);
        }

    }

    public class OscillatorRoute : bmscommand
    {
        public byte Switch;

        public OscillatorRoute()
        {
            CommandType = BMSCommandType.OSCROUTE;
        }

        public override void read(BeBinaryReader read)
        {
            Switch = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.OSCROUTE);
            write.Write(Switch);
        }
    }

    public class VibratoDepth: bmscommand
    {
        public byte Depth;

        public VibratoDepth()
        {
            CommandType = BMSCommandType.VIBDEPTH;
        }

        public override void read(BeBinaryReader read)
        {
            Depth = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.VIBDEPTH);
            write.Write(Depth);
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

        public override void read(BeBinaryReader read)
        {
            Depth = read.ReadByte();
            Unk = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.VIBDEPTHMIDI);
            write.Write(Depth);
        }
    }

    public class VibratoPitch : bmscommand
    {
        public byte Pitch;

        public VibratoPitch()
        {
            CommandType = BMSCommandType.VIBPITCH;
        }

        public override void read(BeBinaryReader read)
        {
            Pitch = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.VIBPITCH);
            write.Write(Pitch);
        }
    }

    public class IIRCutoff : bmscommand
    {
        public byte Cutoff;

        public IIRCutoff()
        {
            CommandType = BMSCommandType.IIRCUTOFF;
        }

        public override void read(BeBinaryReader read)
        {
            Cutoff = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)BMSCommandType.IIRCUTOFF);
            write.Write(Cutoff);
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

        public override void read(BeBinaryReader read)
        {
            Attack = read.ReadInt16();
            Decay = read.ReadInt16();
            Sustain = read.ReadInt16();
            Release = read.ReadInt16();
            Unknown = read.ReadInt16();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Attack);
            write.Write(Decay);
            write.Write(Sustain);
            write.Write(Release);
            write.Write(Unknown);
        }
    }


    public class ClearInterrupt : bmscommand
    {
        public ClearInterrupt()
        {
            CommandType = BMSCommandType.CLRI;
        }

        public override void read(BeBinaryReader read)
        {
  
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
        }
    }

    public class ReturnInterrupt : bmscommand
    {
        public ReturnInterrupt()
        {
            CommandType = BMSCommandType.RETI;
        }

        public override void read(BeBinaryReader read)
        {

        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
        }
    }

    public class FlushAll : bmscommand
    {
        public FlushAll()
        {
            CommandType = BMSCommandType.FLUSHALL;
        }

        public override void read(BeBinaryReader read)
        {

        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
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

        public override void read(BeBinaryReader read)
        {
            Source = read.ReadByte();
            Destination = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Source);
            write.Write(Destination);
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

        public override void read(BeBinaryReader read)
        {
            Source = read.ReadByte();
            Destination = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Source);
            write.Write(Destination);
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

        public override void read(BeBinaryReader read)
        {
            Source = read.ReadByte();
            Destination = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Source);
            write.Write(Destination);
        }
    }


    public class PERFS8DURU16 : bmscommand
    {
        public byte Parameter;
        public sbyte Value;
        public ushort Duration;

        public PERFS8DURU16()
        {
            CommandType = BMSCommandType.PERF_S16_DUR_U16;
        }

        public override void read(BeBinaryReader read)
        {
            Parameter = read.ReadByte();
            Value = read.ReadSByte();
            Duration = read.ReadUInt16();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Parameter);
            write.Write(Value);
            write.Write(Duration);
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

        public override void read(BeBinaryReader read)
        {
            Parameter = read.ReadByte();
            Value = read.ReadInt16(); 
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Parameter);
            write.Write(Value);
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

        public override void read(BeBinaryReader read)
        {
            Parameter = read.ReadByte();
            Value = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Parameter);
            write.Write(Value);
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

        public override void read(BeBinaryReader read)
        {
            Parameter = read.ReadByte();
            Value = read.ReadInt16();
            Unknown = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Parameter);
            write.Write(Value);
            write.Write(Unknown);
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

        public override void read(BeBinaryReader read)
        {
            Parameter = read.ReadByte();
            Value = read.ReadInt16();
            Duration = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Parameter);
            write.Write(Value);
            write.Write(Duration);
        }
    }

    public class PERFS8 : bmscommand
    {
        public byte Parameter;
        public sbyte Value;


        public PERFS8()
        {
            CommandType = BMSCommandType.PERF_S16_NODUR;
        }

        public override void read(BeBinaryReader read)
        {
            Parameter = read.ReadByte();
            Value = read.ReadSByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Parameter);
            write.Write(Value);
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

        public override void read(BeBinaryReader read)
        {
            Parameter = read.ReadByte();
            Value = read.ReadSByte();
            Duration = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Parameter);
            write.Write(Value);
            write.Write(Duration);
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

        public override void read(BeBinaryReader read)
        {
            Source = read.ReadByte();
            Destination = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Source);
            write.Write(Destination);
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

        public override void read(BeBinaryReader read)
        {
            Source = read.ReadByte();
            Destination = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Source);
            write.Write(Destination);
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

        public override void read(BeBinaryReader read)
        {
            Source = read.ReadByte();
            Destination = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Source);
            write.Write(Destination);
        }
    }

    public class ParameterSet8 : bmscommand
    {
        public byte Source;
        public byte Value;

        public ParameterSet8()
        {
            CommandType = BMSCommandType.PARAM_SET_8;
        }

        public override void read(BeBinaryReader read)
        {
            Source = read.ReadByte();
            Value = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Source);
            write.Write(Value);
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

        public override void read(BeBinaryReader read)
        {
            Source = read.ReadByte();
            Value = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Source);
            write.Write(Value);
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

        public override void read(BeBinaryReader read)
        {
            Source = read.ReadByte();
            Value = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Source);
            write.Write(Value);
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

        public override void read(BeBinaryReader read)
        {
            Source = read.ReadByte();
            Value = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Source);
            write.Write(Value);
        }
    }

    public class ParameterComparerRegister : bmscommand
    {
        public byte Source;
        public byte Register;

        public ParameterComparerRegister()
        {
            CommandType = BMSCommandType.PARAM_CMP_R;
        }

        public override void read(BeBinaryReader read)
        {
            Source = read.ReadByte();
            Register = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Source);
            write.Write(Register);
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

        public override void read(BeBinaryReader read)
        {
            Source = read.ReadByte();
            Value = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Source);
            write.Write(Value);
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

        public override void read(BeBinaryReader read)
        {
            Source = read.ReadByte();
            Value = read.ReadInt16();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Source);
            write.Write(Value);
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

        public override void read(BeBinaryReader read)
        {
            A = read.ReadByte();
            B = read.ReadByte();
            C = read.ReadByte();
            D = read.ReadByte();
            E = read.ReadByte();

        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(A);
            write.Write(B);
            write.Write(C);
            write.Write(D);
            write.Write(E);        
        }
    }



    public class SetLastNote : bmscommand
    {
        public byte Note;
   

        public SetLastNote()
        {
            CommandType = BMSCommandType.SETLASTNOTE;
        }

        public override void read(BeBinaryReader read)
        {
            Note = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Note);
        }
    }

    public class LoopStart : bmscommand
    {
        public byte Count;


        public LoopStart()
        {
            CommandType = BMSCommandType.LOOP_S;
        }

        public override void read(BeBinaryReader read)
        {
            Count = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Count);
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

        public override void read(BeBinaryReader read)
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

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Flags);

            if ((Flags & 0xF) == 0xC)
            {
                write.Write(A);
                write.Write(B);
                write.Write(C);
            }
            else if ((Flags & 0xF) == 0x8)
            {
                write.Write(A);
            }
            else
            {
                write.Write(A);
                write.Write(B);
            }
        }
    }


    public class LoopEnd : bmscommand
    {
        public LoopEnd()
        {
            CommandType = BMSCommandType.LOOP_E;
        }

        public override void read(BeBinaryReader read)
        {
  
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
        }
    }


    public class SyncCpu : bmscommand
    {
        public ushort Value;

        public SyncCpu()
        {
            CommandType = BMSCommandType.SYNCCPU;
        }

        public override void read(BeBinaryReader read)
        {
            Value = read.ReadUInt16();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Value);
        }
    }

    public class Tempo : bmscommand
    {
        public ushort BeatsPerMinute;

        public Tempo()
        {
            CommandType = BMSCommandType.TEMPO;
        }

        public override void read(BeBinaryReader read)
        {
            BeatsPerMinute = read.ReadUInt16();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(BeatsPerMinute);
        }
    }

    public class Timebase : bmscommand
    {
        public ushort PulsesPerQuarterNote;

        public Timebase()
        {
            CommandType = BMSCommandType.TIMEBASE;
        }

        public override void read(BeBinaryReader read)
        {
            PulsesPerQuarterNote = read.ReadUInt16();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(PulsesPerQuarterNote);
        }
    }

    public class Return : bmscommand
    {
        public byte Condition;


        public Return()
        {
            CommandType = BMSCommandType.RETURN;
        }

        public override void read(BeBinaryReader read)
        {
            Condition = read.ReadByte();
        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
            write.Write(Condition);
        }
    }

    public class ReturnNoArg  : bmscommand
    {
        public ReturnNoArg()
        {
            CommandType = BMSCommandType.RETURN_NOARG;
        }

        public override void read(BeBinaryReader read)
        {

        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
        }
    }

    public class Finish : bmscommand
    {
        public Finish()
        {
            CommandType = BMSCommandType.FINISH;
        }

        public override void read(BeBinaryReader read)
        {

        }

        public override void write(BeBinaryWriter write)
        {
            write.Write((byte)CommandType);
        }
    }
}
