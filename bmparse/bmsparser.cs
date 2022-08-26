using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;

namespace bmparse.bms
{
    internal class bmsparser
    {
        public static Type[] OpcodeLookup = new Type[0x100];
        public bmsparser()
        {
            OpcodeLookup[(byte)BMSCommandType.CALL] = typeof(Call);
            OpcodeLookup[(byte)BMSCommandType.JMP] = typeof(Jump);
            OpcodeLookup[(byte)BMSCommandType.RETURN] = typeof(Return);
            OpcodeLookup[(byte)BMSCommandType.RETURN_NOARG] = typeof(ReturnNoArg);
            OpcodeLookup[(byte)BMSCommandType.FINISH] = typeof(Finish);
            OpcodeLookup[(byte)BMSCommandType.LOOP_S] = typeof(LoopStart);
            OpcodeLookup[(byte)BMSCommandType.LOOP_E] = typeof(LoopEnd);
            OpcodeLookup[(byte)BMSCommandType.OPENTRACK] = typeof(OpenTrack);
            OpcodeLookup[(byte)BMSCommandType.CLOSETRACK] = typeof(CloseTrack);
            OpcodeLookup[(byte)BMSCommandType.IIRCUTOFF] = typeof(IIRCutoff);
            OpcodeLookup[(byte)BMSCommandType.CMD_WAIT8] = typeof(WaitCommand8);
            OpcodeLookup[(byte)BMSCommandType.CMD_WAIT16] = typeof(WaitCommand16);
            OpcodeLookup[(byte)BMSCommandType.CMD_WAITR] = typeof(WaitRegister);
            OpcodeLookup[(byte)BMSCommandType.PARAM_SET_16] = typeof(ParamSet16);
            OpcodeLookup[(byte)BMSCommandType.PARAM_ADD_16] = typeof(ParamAdd16);
            OpcodeLookup[(byte)BMSCommandType.SIMPLEENV] = typeof(SimpleEnvelope);
            OpcodeLookup[(byte)BMSCommandType.SETINTERRUPT] = typeof(SetInterrupt);
            OpcodeLookup[(byte)BMSCommandType.OPOVERRIDE_4] = typeof(OpOverride4);
            OpcodeLookup[(byte)BMSCommandType.OPOVERRIDE_1] = typeof(OpOverride1);
            OpcodeLookup[(byte)BMSCommandType.PRINTF] = typeof(PrintF);
            OpcodeLookup[(byte)BMSCommandType.SIMPLEOSC] = typeof(SimpleOsc);
            OpcodeLookup[(byte)BMSCommandType.TRANSPOSE] = typeof(Transpose);
            OpcodeLookup[(byte)BMSCommandType.OSCROUTE] = typeof(OscillatorRoute);
            OpcodeLookup[(byte)BMSCommandType.VIBDEPTH] = typeof(VibratoDepth);
            OpcodeLookup[(byte)BMSCommandType.VIBDEPTHMIDI] = typeof(VibratoDepthMidi);
            OpcodeLookup[(byte)BMSCommandType.VIBPITCH] = typeof(VibratoPitch);
            OpcodeLookup[(byte)BMSCommandType.SIMPLEADSR] = typeof(SimpleADSR);
            OpcodeLookup[(byte)BMSCommandType.CLRI] = typeof(ClearInterrupt);
            OpcodeLookup[(byte)BMSCommandType.RETI] = typeof(ReturnInterrupt);
            OpcodeLookup[(byte)BMSCommandType.FLUSHALL] = typeof(FlushAll);
            OpcodeLookup[(byte)BMSCommandType.READPORT] = typeof(ReadPort);
            OpcodeLookup[(byte)BMSCommandType.WRITEPORT] = typeof(WritePort);
            OpcodeLookup[(byte)BMSCommandType.CHILDWRITEPORT] = typeof(ChildWritePort);
            OpcodeLookup[(byte)BMSCommandType.PERF_S8_DUR_U16] = typeof(PERFS8DURU16);
            OpcodeLookup[(byte)BMSCommandType.PERF_S16_NODUR] = typeof(PERFS16);
            OpcodeLookup[(byte)BMSCommandType.PERF_S16_DUR_U8_9E] = typeof(PERFS16U89E);
            OpcodeLookup[(byte)BMSCommandType.PERF_S8_DUR_U8] = typeof(PERFS8DURU8);
            OpcodeLookup[(byte)BMSCommandType.PERF_S8_NODUR] = typeof(PERFS8);
            OpcodeLookup[(byte)BMSCommandType.PERF_U8_NODUR] = typeof(PERFU8);
            OpcodeLookup[(byte)BMSCommandType.PARAM_SET_R] = typeof(ParameterSetRegister);
            OpcodeLookup[(byte)BMSCommandType.PARAM_ADD_R] = typeof(ParameterAddRegister);
            OpcodeLookup[(byte)BMSCommandType.PARAM_SET_8] = typeof(ParameterSet8);
            OpcodeLookup[(byte)BMSCommandType.PARAM_ADD_8] = typeof(ParameterAdd8);
            OpcodeLookup[(byte)BMSCommandType.PARAM_MUL_8] = typeof(ParameterMultiply8);
            OpcodeLookup[(byte)BMSCommandType.PARAM_CMP_8] = typeof(ParameterCompare8);
            OpcodeLookup[(byte)BMSCommandType.PARAM_CMP_R] = typeof(ParameterComparerRegister);
            OpcodeLookup[(byte)BMSCommandType.SETPARAM_90] = typeof(ParameterSet8_90);
            OpcodeLookup[(byte)BMSCommandType.SETPARAM_92] = typeof(ParameterSet16_92);
            OpcodeLookup[(byte)BMSCommandType.SETLASTNOTE] = typeof(SetLastNote);
            OpcodeLookup[(byte)BMSCommandType.PARAM_BITWISE] = typeof(ParamBitwise);
            OpcodeLookup[(byte)BMSCommandType.SYNCCPU] = typeof(SyncCpu);
            OpcodeLookup[(byte)BMSCommandType.TEMPO] = typeof(Tempo);
            OpcodeLookup[(byte)BMSCommandType.TIMEBASE] = typeof(Timebase);
            OpcodeLookup[(byte)BMSCommandType.PANSWSET] = typeof(PanSweepSet);
            OpcodeLookup[(byte)BMSCommandType.PANPOWSET] = typeof(PanPowerSet);
            OpcodeLookup[(byte)BMSCommandType.BUSCONNECT] = typeof(BusConnect);
            OpcodeLookup[(byte)BMSCommandType.OUTSWITCH] = typeof(OutSwitch);
            OpcodeLookup[(byte)BMSCommandType.PARAM_SUBTRACT] = typeof(ParameterSubtract);
        }

        public bmscommand readNextCommand(BeBinaryReader reader)
        {
            var opcode = reader.ReadByte();
            bmscommand outputCommand;

            if (opcode < 0x80)
            {
                var cmd = new NoteOnCommand();
                cmd.Note = opcode;
                cmd.read(reader);
                outputCommand = cmd;
            }
            else if (opcode >= 0x81 && opcode < 0x88)
            {
                var cmd = new NoteOffCommand();
                cmd.Voice = (byte)(opcode & 0xF); // -1;
                cmd.read(reader);
                outputCommand = cmd;
            } else
            {
                var opcodeType = OpcodeLookup[opcode];
                if (opcodeType == null)
                    throw new Exception($"0x{reader.BaseStream.Position:X5} Opcode not implemented 0x{opcode:X} {(BMSCommandType)opcode}");

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                outputCommand = (bmscommand)Activator.CreateInstance(opcodeType);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                if (outputCommand == null)
                    throw new Exception($"Failed to create instance of 0x{opcode:X} {(BMSCommandType)opcode}");
                outputCommand.read(reader);
            }
            return outputCommand;
        }
    }
}
