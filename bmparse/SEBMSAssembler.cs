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

        string[] currentData;

        bgWriter writer;


        private int currentLine;
        private string currentFile;



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

                if (labDestinationAddress<=0)
                    GlobalLabels.TryGetValue(Ref.Label, out labDestinationAddress);
     
                if (labDestinationAddress <= 0)
                    return Ref; // Can't link labels any more, found an undefined.

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

        


        public string ProcInstruction(string[] ASMLine)
        {
            if (ASMLine.Length == 0)
                return null;

            var Instruction = ASMLine[0];    
            
            

            if (Instruction.Length < 1 || Instruction[0] == '#' || Instruction[0] == '\r' || Instruction[0] == '\n' ) // Comment, blank line, etc
                return null;

            if (Instruction[0] == ':') // Label definition
                defineLabel(writer.BaseStream.Position, Instruction.Substring(1));

            Instruction = Instruction.ToUpper();

            bmscommand command;
            switch (Instruction)
            {
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
                case "WAIT16":
                    {
                        var a1 = checkArgument(ASMLine, 0);
                        var inst = new WaitCommand16();
                        inst.Delay = (ushort)parseNumber(a1);
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
        }
        public void ProcBuffer()
        {
            
            while (currentLine < currentData.Length)
            {
                var lin = currentData[currentLine];
                var spl = lin.Split(' ');
                ProcInstruction(spl);
                currentLine++;
            }
            linkLabelScope(LocalLabelReferences);
            var res = linkLabelScope(GlobalLabelReferences);
            if (res!=null)
            {
                var fg = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Link failure: Unsatisfied label resolution! ");
                Console.ForegroundColor = fg;
                Console.WriteLine($"\tCould not link undefined global label {res.Label}");
                Console.WriteLine($"\t{res.Filename} @ Line {res.Line}");
                Console.Write($"\tAddress: 0x{res.Address:X} Text: ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{currentData[res.Line]}");
                Console.ForegroundColor = fg;

            }


         

        ;
        }

    }
}
