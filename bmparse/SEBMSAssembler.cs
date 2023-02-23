using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xayrga;
using xayrga.byteglider;
using bmparse.bms;

namespace bmparse
{

    internal enum AddressSize
    {
        U8 = 0,
        U16 = 1,
        U24 = 2,
        U32 = 3
    }

    internal class BMSLabelReference
    {
        public string Filename = "NONE";
        public int Line = 0;
        public int InstructionDepth = 0;
        public long Address = 0;
        public AddressSize Size;
        public string Label;
    }

    internal class SEBMSAssembler
    {
        Dictionary<string,long> LocalLabels = new Dictionary<string,long>();
        Dictionary<string,long> GlobalLabels = new Dictionary<string,long>();

        List<BMSLabelReference> LocalLabelReferences = new List<BMSLabelReference>();
        List<BMSLabelReference> GlobalLabelReferences = new List<BMSLabelReference>();

        Stack<long> categoryCallAddresses = new Stack<long>();

        string[] currentData;

        bgWriter writer;


        private int currentLine;
        private string currentFile;

        private long lastCategoryCallOpcodeOffset = -1;



        private void compileError(string reason)
        {
            throw new Exception($"{reason} [{currentFile}] @ Line {currentLine}");
        }

        public int parseNumber(string num)
        {
            var ns = System.Globalization.NumberStyles.Any;
            if (num.Length >= 2 && num[0] == '0' && num[1] == 'x')
            {
                ns = System.Globalization.NumberStyles.HexNumber;
                num = num.Substring(2);
            }
            else if (num.Length >= 1 && num[num.Length - 1] == 'h')
            {
                ns = System.Globalization.NumberStyles.HexNumber;
                num = num.Substring(0,num.Length - 1);
            }
            var oV = -1;
            if (!Int32.TryParse(num, ns, null, out oV))
                compileError($"Cannot parse argument to number: '{num}'");

            return oV;
        }

        public byte[] parseHexArgument(string argv)
        {
            if (argv == "HEX(X)")
                return new byte[0];

            if (argv.Length < 5 || argv.Substring(0, 4) != "HEX(")
            {
                compileError($"Invalid hex string {argv}");
                return null;
            }

            var ns = System.Globalization.NumberStyles.HexNumber;
            var strX = argv.Substring(4, argv.Length - 5);
            var strings = strX.Split(',');
            var ret = new byte[strings.Length];
            for (int i = 0; i < strings.Length; i++)
                ret[i] = byte.Parse(strings[i], ns);

            return new byte[0];
        }


        public void referenceLabel(long address, byte offset, string label, AddressSize addr)
        {
            var nRef = new BMSLabelReference()
            {
                Line = currentLine,
                Filename = currentFile,
                Address = address,
                InstructionDepth = offset,
                Size = addr,
                Label = label
            };

            if (label[0] == '@')
                GlobalLabelReferences.Add(nRef);
            else 
                LocalLabelReferences.Add(nRef);
        }

        public void defineLabel(long address, string name)
        {

            if (name[0] == '@')
                GlobalLabels[name] = address;
            else
                LocalLabels[name] = address;

        }

        private string checkArgument(string[] instr,int argn )
        {
            argn += 1;
            if (argn >= instr.Length)
                compileError($"{instr[0]} expected argument at position #{argn - 1}");
            return instr[argn];
        }

        public BMSLabelReference linkLabelScope(List<BMSLabelReference> scope)
        { 
            foreach (BMSLabelReference Ref in scope)
            {
                long labDestinationAddress = -1;
                LocalLabels.TryGetValue(Ref.Label, out labDestinationAddress);
                //Console.WriteLine($"{Ref.Label} {labDestinationAddress}");
                if (labDestinationAddress<=0)
                    GlobalLabels.TryGetValue(Ref.Label, out labDestinationAddress);
    
                if (labDestinationAddress <= 0)
                    return Ref; // Can't link labels any more, found an undefined.
                //Console.WriteLine($"{Ref.Label} {labDestinationAddress}");
                writer.PushAnchor();
                writer.BaseStream.Position = Ref.Address + Ref.InstructionDepth;
                switch (Ref.Size)
                {
                    case AddressSize.U8:
                        writer.WriteBE((byte)labDestinationAddress);
                        break;
                    case AddressSize.U16:
                        writer.WriteBE((ushort)labDestinationAddress);
                        break;
                    case AddressSize.U24:
                        writer.WriteBE((uint)labDestinationAddress, true);
                        break;
                    case AddressSize.U32:
                        writer.WriteBE((uint)labDestinationAddress);
                        break;
                }
                writer.PopAnchor();
            }
            return null;
        }

        
        public void BuildProject(SEBMSProject Project,string projectBase, string fileOut)
        {
            var outFile = File.OpenWrite(fileOut);
            var writer = new bgWriter(outFile);
            SetOutput(writer);

            LoadData($"{projectBase}/{Project.InitSection}");
            if (!ProcBuffer())
                return;
            if (!LinkLocals())
                return;

            for (int i=0; i < Project.CommonLib.Length; i++)
            {
                var comCurrent = Project.CommonLib[i];
                Console.WriteLine($"Assembling common... {comCurrent}");
                LoadData($"{projectBase}/{comCurrent}");

                if (!ProcBuffer())
                    return;
                if (!LinkLocals())
                    return;
                writer.Pad(4);
            }


            for (int i = 0; i < Project.Categories.Length; i++)
            {
                var currentCategory = Project.Categories[i];

                Console.WriteLine($"Assembling CatSys {i}... {currentCategory.LogicFile}");

                
                LoadData($"{projectBase}/{currentCategory.LogicFile}");

                if (!ProcBuffer())
                    return;

                if (!LinkLocals())
                    return;

                writer.Pad(32);
      
                var sndAddresses = new long[currentCategory.Sounds.Length];

                Dictionary<string, long> unDupe = new Dictionary<string, long>();

                for (int sndNum = 0; sndNum  < currentCategory.Sounds.Length; sndNum++)
                {
                    var snd = currentCategory.Sounds[sndNum];
                    if (unDupe.ContainsKey(snd)) // Check if we've already referenced this binary, if so link instead of creating binary content
                    {
                        sndAddresses[sndNum] = unDupe[snd];
                        continue;
                    }
                    sndAddresses[sndNum] = writer.BaseStream.Position; 
                    unDupe[snd] = writer.BaseStream.Position; 
                    Console.WriteLine($"\tAssembling sound... {snd}");
                    LoadData($"{projectBase}/{snd}"); // Load data, resets locals and cref's
         
                    if (!ProcBuffer()) // Process buffer
                        return;

                    if (!LinkLocals()) // Try to link local label references
                        return;

                    writer.Pad(4);
                }

                writer.Pad(32);                
                var JumptableOffset = writer.BaseStream.Position;
 
                // Dump addresses into reference table
                for (int b=0; b < sndAddresses.Length; b++)
                    writer.WriteBE((uint)sndAddresses[b],true);

                writer.PushAnchor(); // Anchor at stream pos
                while (categoryCallAddresses.Count > 0)
                {
                    writer.BaseStream.Position = categoryCallAddresses.Pop();
                    writer.WriteBE((uint)JumptableOffset, true);
                }
                writer.PopAnchor(); // Pop anchor off the stack, return to old position

                writer.Pad(32);
                writer.Flush();

            }

   
            LinkGlobals();
            var fc = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("BMS Rebuild successful");
            Console.ForegroundColor = fc;
        }

        public string ProcInstruction(string[] ASMLine)
        {
            if (ASMLine.Length == 0)
                return null;

            var Instruction = ASMLine[0];    
            
            if (Instruction.Length < 1 || Instruction[0] == '#' || Instruction[0] == '\r' || Instruction[0] == '\n' ) // Comment, blank line, etc
                return null;

            if (Instruction[0] == ':')  // Label definition 
            {
                defineLabel(writer.BaseStream.Position, Instruction.Substring(1));
                return null; 
            }

            Instruction = Instruction.ToUpper();

            bmscommand command;
            switch (Instruction)
            {
                case "%CATEGORY_CALLTABLE":
             
                    writer.WriteBE((byte)0xC4);
                    writer.WriteBE((byte)0xC0);
                    writer.WriteBE((byte)0x04);
                    categoryCallAddresses.Push(writer.BaseStream.Position);
                    //lastCategoryCallOpcodeOffset = writer.BaseStream.Position;
                    writer.WriteBE(0x000000,true);
                    break;
                case "ALIGN4":
                    writer.Pad(4);
                    break;
                case "ENVPOINT":
                    {
                        var p1 = checkArgument(ASMLine, 0);
                        var p2 = checkArgument(ASMLine, 1);
                        var p3 = checkArgument(ASMLine, 2);

                        writer.WriteBE((ushort)parseNumber(p1));
                        writer.WriteBE((ushort)parseNumber(p2));
                        writer.WriteBE((ushort)parseNumber(p3));
                    }
                    break;
                case "REF24":
                    {
                        var lbl = checkArgument(ASMLine, 0);
                        referenceLabel(writer.BaseStream.Position, 0, lbl, AddressSize.U24);
                    }
                    break;
                case "OPENTRACK":
                    {
                        var trkFlgs = checkArgument(ASMLine, 0);
                        var lbl = checkArgument(ASMLine, 1);
                        referenceLabel(writer.BaseStream.Position, 2, lbl, AddressSize.U24);
                        var inst = new OpenTrack();
                        inst.TrackID = (byte)parseNumber(trkFlgs);
                        inst.Address = 0; // Will get filled by label ref later.
                        inst.write(writer);
                        break;
                    }
                case "SETINTER":
                    {
                        var interruptLevel = checkArgument(ASMLine, 0);
                        var lbl = checkArgument(ASMLine, 1);
                        referenceLabel(writer.BaseStream.Position, 2, lbl, AddressSize.U24);
                        var inst = new SetInterrupt();
                        inst.InterruptLevel = (byte)parseNumber(interruptLevel);
                        inst.Address = 0; // Will get filled by label ref later.
                        inst.write(writer);
                        break;
                    }
                case "SIMPLENV":
                    {
                        var envID = checkArgument(ASMLine, 0);
                        var lbl = checkArgument(ASMLine, 1);
                        referenceLabel(writer.BaseStream.Position, 2, lbl, AddressSize.U24);
                        var inst = new SimpleEnvelope();
                        inst.Flags = (byte)parseNumber(envID);
                        inst.Address = 0; // Will get filled by label ref later.
                        inst.write(writer);
                    }
                    break;
                case "PARAM8":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new ParameterSet8();
                        inst.TargetParameter = (byte)parseNumber(a1);
                        inst.Value = (byte)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }
                case "SETPARAM90":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new ParameterSet8_90();
                        inst.Source = (byte)parseNumber(a1);
                        inst.Value = (byte)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }
                case "TPRMS16_DU8_9E":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var a3 = checkArgument(ASMLine, 2);

                        var inst = new PERFS16U89E();
                        inst.Parameter= (byte)parseNumber(a1);
                        inst.Value = (short)parseNumber(a2);
                        inst.Unknown = (byte)parseNumber(a3);

                        inst.write(writer);
                        break;
                    }
                case "TPRMS16":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);


                        var inst = new PERFS16();
                        inst.Parameter = (byte)parseNumber(a1);
                        inst.Value = (short)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }

                case "SETPARAM92":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new ParameterSet16_92();
                        inst.Source = (byte)parseNumber(a1);
                        inst.Value = (short)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }
                case "PARAM16":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new ParameterSet16();
                        inst.TargetParameter = (byte)parseNumber(a1);
                        inst.Value = (short)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }
                case "SET_BANK_INS":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new ParameterSet16();
                        inst.TargetParameter = 0x06;
                        inst.Value = (short)(parseNumber(a1) << 8 | parseNumber(a2));
                        inst.write(writer);
                        break;
                    }
                case "WRITEPORT":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new WritePort();
                        inst.Source = (byte)parseNumber(a1);
                        inst.Destination = (byte)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }
                case "READPORT":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new ReadPort();
                        inst.Source = (byte)parseNumber(a1);
                        inst.Destination = (byte)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }
                case "TIMEBASE":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var inst = new Timebase();
                        inst.PulsesPerQuarterNote = (ushort)parseNumber(a1);                        
                        inst.write(writer);
                        break;
                    }
                case "TEMPO":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var inst = new Tempo();
                        inst.BeatsPerMinute = (ushort)parseNumber(a1);
                        inst.write(writer);
                        break;
                    }
                case "WAIT8":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var inst = new WaitCommand8();
                        inst.Delay = (byte)parseNumber(a1);
                        inst.write(writer);
                        break;
                    }
                case "WAIT16":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var inst = new WaitCommand16();
                        inst.Delay = (ushort)parseNumber(a1);
                        inst.write(writer);
                        break;
                    }
                case "WAITRE":
                    {
                        var aregister = checkArgument(ASMLine, 0);
                        var inst = new WaitRegister();
                        inst.Register = (byte)parseNumber(aregister);
                        inst.write(writer);
                        break;
                    }

                case "JMP":
                    {
                        var cond = checkArgument(ASMLine, 0);
                        var lbl = checkArgument(ASMLine, 1);
                        referenceLabel(writer.BaseStream.Position, 2, lbl, AddressSize.U24);
                        var inst = new Jump();
                        inst.Flags = (byte)parseNumber(cond);
                        inst.Address = 0; // Will get filled by label ref later.
                        inst.write(writer);
                        break;
                    }
                case "CALL":
                    {
                        var cond = checkArgument(ASMLine, 0);
                        var lbl = checkArgument(ASMLine, 1);
                        referenceLabel(writer.BaseStream.Position, 2, lbl, AddressSize.U24);
                        var inst = new Call();
                        inst.Flags = (byte)parseNumber(cond);
                        inst.Address = 0; // Will get filled by label ref later.
                        inst.write(writer);
                        break;
                    }
                case "RETURN":
                    {
                        var cond = checkArgument(ASMLine, 0);
                        var inst = new Return();
                        inst.Condition = (byte)parseNumber(cond);
                        inst.write(writer);
                        break;
                    }
                case "CLOSETRACK":
                    {
                        var trk = checkArgument(ASMLine, 0);
                        var inst = new CloseTrack();
                        inst.TrackID = (byte)parseNumber(trk);
                        inst.write(writer);
                        break;
                    }

                case "LOOPSTART":
                    {
                        var trk = checkArgument(ASMLine, 0);
                        var trk2 = checkArgument(ASMLine, 1);
                        var inst = new LoopStart();
                        inst.Count = (byte)parseNumber(trk);
                        inst.Unknown = (byte)parseNumber(trk2);
                        inst.write(writer);
                        break;
                    }
                case "IIRC":
                    {
                        var trk = checkArgument(ASMLine, 0);
                        var inst = new IIRCutoff();
                        inst.Cutoff = (byte)parseNumber(trk);
                        inst.write(writer);
                        break;
                    }
                case "LOOPEND":
                    {
                        var inst = new LoopEnd();
                        inst.write(writer);
                        break;
                    }
                case "SYNC":
                    {
                        var val = checkArgument(ASMLine, 0);
                        var inst = new SyncCpu();
                        inst.Value = (ushort)parseNumber(val);
                        inst.write(writer);
                        break;
                    }

                case "TPRMS8":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new PERFS8();
                        inst.Parameter= (byte)parseNumber(a1);
                        inst.Value = (sbyte)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }
                case "TPRMU8":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new PERFU8();
                        inst.Parameter = (byte)parseNumber(a1);
                        inst.Value = (byte)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }
                case "CHILDWP":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new ChildWritePort();
                        inst.Source = (byte)parseNumber(a1);
                        inst.Destination = (byte)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }
                case "TPRMS8_DU16":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var a3 = checkArgument(ASMLine, 2);
                        var inst = new PERFS8DURU16();
                        inst.Parameter = (byte)parseNumber(a1);
                        inst.Value = (sbyte)parseNumber(a2);
                        inst.Duration = (ushort)parseNumber(a3);
                        inst.write(writer);
                        break;
                    }
                case "TPRMS8_DU8":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var a3 = checkArgument(ASMLine, 2);
                        var inst = new PERFS8DURU8();
                        inst.Parameter = (byte)parseNumber(a1);
                        inst.Value = (sbyte)parseNumber(a2);
                        inst.Duration = (byte)parseNumber(a3);
                        inst.write(writer);
                        break;
                    }
                case "CMP8":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new ParameterCompare8();
                        inst.Source = (byte)parseNumber(a1);
                        inst.Value = (byte)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }
                case "CMPR":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new ParameterCompareRegister();
                        inst.Source = (byte)parseNumber(a1);
                        inst.Register = (byte)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }
                case "OSCROUTE":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var inst = new OscillatorRoute();
                        inst.Switch = (byte)parseNumber(a1);
                        inst.write(writer);
                        break;
                    }
                case "SIMPLEOSC":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var inst = new SimpleOscillator();
                        inst.OscID = (byte)parseNumber(a1);
                        inst.write(writer);
                        break;
                    }
                case "ADDR":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new ParameterAddRegister();
                        inst.Source = (byte)parseNumber(a1);
                        inst.Destination = (byte)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }
                case "SIMADSR":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var a3 = checkArgument(ASMLine, 2);
                        var a4 = checkArgument(ASMLine, 3);
                        var a5 = checkArgument(ASMLine, 4);

                        var inst = new SimpleADSR();
                        inst.Attack= (short)parseNumber(a1);
                        inst.Decay = (short)parseNumber(a2);
                        inst.Sustain = (short)parseNumber(a3);
                        inst.Release = (short)parseNumber(a4);
                        inst.Unknown = (short)parseNumber(a5);
                        inst.write(writer);
                        break;
                    }
                case "OUTSWITCH":
                    {
                        var val = checkArgument(ASMLine, 0);
                        var inst = new OutSwitch();
                        inst.Register = (byte)parseNumber(val);
                        inst.write(writer);
                        break;
                    }
                case "TRANSPOSE":
                    {
                        var val = checkArgument(ASMLine, 0);
                        var inst = new Transpose();
                        inst.Transposition = (byte)parseNumber(val);
                        inst.write(writer);
                        break;
                    }
                case "SETLAST":
                    {
                        var val = checkArgument(ASMLine, 0);
                        var inst = new SetLastNote();
                        inst.Note = (byte)parseNumber(val);
                        inst.write(writer);
                        break;
                    }
                case "BUSCONNECT":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var a3 = checkArgument(ASMLine, 2);

                        var inst = new BusConnect();
                        inst.A = (byte)parseNumber(a1);
                        inst.B = (byte)parseNumber(a2);
                        inst.C = (byte)parseNumber(a3);

                        inst.write(writer);
                        break;
                    }
                case "ADD8":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new ParameterAdd8();
                        inst.Source = (byte)parseNumber(a1);
                        inst.Value = (byte)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }
                case "MUL8":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new ParameterMultiply8();
                        inst.Source = (byte)parseNumber(a1);
                        inst.Value = (byte)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }
                case "SUB8":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new ParameterSubtract();
                        inst.Source = (byte)parseNumber(a1);
                        inst.Destination = (byte)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }
                case "ADD16":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new ParameterAdd16();
                        inst.TargetParameter = (byte)parseNumber(a1);
                        inst.Value = (short)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }
                case "BITWZ":
                    {
                        var flg = checkArgument(ASMLine, 0);
                        var a1 = checkArgument(ASMLine, 1);                  
                        var a2 = checkArgument(ASMLine, 2);
                        var inst = new ParamBitwise();
                        inst.Flags = (byte)parseNumber(flg);
                        inst.A = (byte)parseNumber(a1);
                        inst.B = (byte)parseNumber(a2);                        
                        inst.write(writer);
                        break;
                    }
                case "BITWZ8":
                    {
                        var flg = checkArgument(ASMLine, 0);
                        var a1 = checkArgument(ASMLine, 1);

                        var inst = new ParamBitwise();
                        inst.Flags = (byte)parseNumber(flg);
                        inst.A = (byte)parseNumber(a1);
                        inst.write(writer);
                        break;
                    }
                case "BITWZC":
                    {
                        var flg = checkArgument(ASMLine, 0);
                        var a1 = checkArgument(ASMLine, 1);
                        var a2 = checkArgument(ASMLine, 2);
                        var a3 = checkArgument(ASMLine, 3);

                        var inst = new ParamBitwise();
                        inst.Flags = (byte)parseNumber(flg);
                        inst.A = (byte)parseNumber(a1);
                        inst.B = (byte)parseNumber(a2);
                        inst.C = (byte)parseNumber(a3);
                        inst.write(writer);
                        break;
                    }

                case "PANPOWSET":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var a3 = checkArgument(ASMLine, 2);
                        var a4 = checkArgument(ASMLine, 3);
                        var a5 = checkArgument(ASMLine, 4);
                        var inst = new PanPowerSet();

                   
                        inst.A= (byte)parseNumber(a1);
                        inst.B = (byte)parseNumber(a2);
                        inst.C = (byte)parseNumber(a3);
                        inst.D = (byte)parseNumber(a4);
                        inst.E = (byte)parseNumber(a5);
                        inst.write(writer);
                        break;
                    }
                case "PARAMREG":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var inst = new ParameterSetRegister();
                        inst.Source = (byte)parseNumber(a1);
                        inst.Destination = (byte)parseNumber(a2);
                        inst.write(writer);
                        break;
                    }
                case "NOTEONRD":
                    {
                        var a1 = checkArgument(ASMLine, 0);

                        var aBeh = checkArgument(ASMLine, 1);
                        var a2 = checkArgument(ASMLine, 2);
   
                        var a3 = checkArgument(ASMLine, 3);
                        var a4 = checkArgument(ASMLine, 4);
                        var a5 = checkArgument(ASMLine, 5);
                        var inst = new NoteOnCommand();

                        inst.Type = 1;
                        inst.Note = (byte)parseNumber(a1);
                        inst.Voice = (byte)parseNumber(a2);
                        inst.Velocity = (byte)parseNumber(a3);
                        inst.Release = (byte)parseNumber(a4);
                        inst.Delay = (byte)parseNumber(a5);
                        //Console.WriteLine(aBeh);
                        inst.Behavior = (byte)parseNumber(aBeh);
                        inst.write(writer);
                        break;
                    }
                case "NOTEONRDL":
                    {
                    
                        var a1 = checkArgument(ASMLine, 0);
                        var aBeh = checkArgument(ASMLine, 1);
                        var a2 = checkArgument(ASMLine, 2);
                        var a3 = checkArgument(ASMLine, 3);
                        var a4 = checkArgument(ASMLine, 4);
                        var a5 = checkArgument(ASMLine, 5);
                        var a6 = checkArgument(ASMLine, 6);
                        var inst = new NoteOnCommand();

                        inst.Type = 2;
                        inst.Note = (byte)parseNumber(a1);
                        inst.Voice = (byte)parseNumber(a2);
                        inst.Velocity = (byte)parseNumber(a3);
                        inst.Release = (byte)parseNumber(a4);
                        inst.Delay = (byte)parseNumber(a5);
                        inst.Length = (byte)parseNumber(a6);
                        inst.Behavior = (byte)parseNumber(aBeh);
                        inst.write(writer);
                        break;
                    }
                case "NOTEON":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var aBeh = checkArgument(ASMLine, 1);
                        var a2 = checkArgument(ASMLine, 2);
                        var a3 = checkArgument(ASMLine, 3);

                        var inst = new NoteOnCommand();
                        inst.Type = 0;

                        inst.Note = (byte)parseNumber(a1);
                        inst.Voice = (byte)parseNumber(a2);
                        inst.Velocity = (byte)parseNumber(a3);
                        inst.Behavior = (byte)parseNumber(aBeh);
                        inst.write(writer);

                        break;
                    }
                case "NOTEOFF":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var inst = new NoteOffCommand();
                        inst.Voice = (byte)parseNumber(a1);
                        inst.write(writer);
                        break;
                    }
                case "CLOSETRK":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var inst = new CloseTrack();
                        inst.TrackID = (byte)parseNumber(a1);
                        inst.write(writer);
                        break;
                    }
                case "!ALIGN":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        writer.Pad((byte)parseNumber(a1));
                        break;
                    }
                case "FINISH":
                    {
                        var inst = new Finish();
                        inst.write(writer);
                        break;
                    }
                case "CLEINT":
                    {
                        var inst = new ClearInterrupt();
                        inst.write(writer);
                        break;
                    }
                case "RETINT":
                    {
                        var inst = new ReturnInterrupt();
                        inst.write(writer);
                        break;
                    }
                case "STOP":// finishes envelope
                    {
                        writer.Pad(4);
                        break;
                    }
                case "CRINGE1":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var a2 = checkArgument(ASMLine, 1);
                        var a3 = checkArgument(ASMLine, 2);
                        var a4 = checkArgument(ASMLine, 3);
                        var inst = new OpOverride1();
                        inst.Instruction = (byte)parseNumber(a1);
                        inst.ArgumentMask = (byte)parseNumber(a2);
                        inst.ArgumentMaskLookup = parseHexArgument(a3);
                        inst.Stupid = parseHexArgument(a4);
                        inst.write(writer);
                        break;
                    }
           


                default:
                    {
                        compileError($"Syntax Error: Unknown Instruction '{Instruction}'");
                        break;
                    }




            }

            return null;
        }

        public void SetOutput(bgWriter wrt)
        {
            writer = wrt;
            
        }
        public void LoadData(string path)
        {
            currentLine = 0;
            currentData = File.ReadAllLines(path);
            currentFile = path;
            LocalLabelReferences.Clear();
            LocalLabels.Clear();
        }

        private bool LinkLocals()
        {
            var res = linkLabelScope(LocalLabelReferences);
            if (res != null)
            {
                var fg = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Link failure: Unsatisfied label resolution! ");
                Console.ForegroundColor = fg;
                Console.WriteLine($"\tCould not link undefined LOCAL label {res.Label}");
                Console.WriteLine($"\t{res.Filename} @ Line {res.Line}");
                Console.Write($"\tAddress: 0x{res.Address:X} Text: ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{currentData[res.Line]}");
                Console.ForegroundColor = fg;
                return false;

            }
            return true;
        }
        private bool LinkGlobals()
        {
            var res = linkLabelScope(GlobalLabelReferences);
  
            if (res != null)
            {
                var fg = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Link failure: Unsatisfied label resolution! ");
                Console.ForegroundColor = fg;
                Console.WriteLine($"\tCould not link undefined global label {res.Label}");
                Console.WriteLine($"\t{res.Filename} @ Line {res.Line}");
                Console.Write($"\tAddress: 0x{res.Address:X} Text: ");
                Console.ForegroundColor = ConsoleColor.Yellow;

                Console.ForegroundColor = fg;
                return false;
            }
            return true;
        }

        public bool ProcBuffer()
        {
            while (currentLine < currentData.Length)
            {
                var lin = currentData[currentLine];
                var spl = lin.Split(' ');
                try
                {
                    ProcInstruction(spl);
                } catch (Exception E)
                {
                    var fg = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Compile failure: Syntax error:");
                    Console.ForegroundColor = fg;
                    Console.WriteLine($"{E.Message}");

                    return false;
                }
                currentLine++;
            }
            return true;
        }

    }
}
