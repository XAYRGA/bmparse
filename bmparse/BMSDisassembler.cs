using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using bmparse.bms;
using Be.IO;
using bmparse.debug;

namespace bmparse
{
    internal partial class BMSDisassembler
    {
        public static bmsparser commandFactory = new bmsparser();
        public BeBinaryReader reader;
        public StringBuilder output = new StringBuilder();
        public string OutFolder = "";

        public Dictionary<long, string> globalLabels = new Dictionary<long, string>();
        Dictionary<string, int> labelGlobalAccumulator = new Dictionary<string, int>();
        public Dictionary<long, AddressReferenceInfo> addressReferenceAccumulator = new Dictionary<long, AddressReferenceInfo>();
       

        public enum ReferenceType
        {
            CALL = 1, 
            JUMP = 2, 
            INTERRUPT = 3, 
            TRACK = 4,
            LEADIN = 5,
            SOUND = 6,
            ENVELOPE = 7,
            JUMPTABLE = 8,

        }

        public class AddressReferenceInfo
        {
            public int count = 0;
            public ReferenceType type;
            public List<long> referenceSource = new List<long>();
            public bool singleSource = true;
            public long metaData = -1;
        }

        private AddressReferenceInfo referenceAddress(long addr, ReferenceType type, long src = 0)
        {
          
            AddressReferenceInfo inc = null;
            addressReferenceAccumulator.TryGetValue(addr, out inc);
            if (inc==null)
                inc = new AddressReferenceInfo()
                {
                    type = type,
                };

            inc.count++;
            inc.referenceSource.Add(src);
            addressReferenceAccumulator[addr] = inc;
            return inc;
        }
        
        private string getGlobalLabel(string type, int address, string prm = null)
        {
            if (globalLabels.ContainsKey(address))
                return globalLabels[address];
            var inc = -1;
            labelGlobalAccumulator.TryGetValue(type, out inc);
            inc++;
            labelGlobalAccumulator[type] = inc;
            var lab = $"{type}{(prm == null ? "_" : $"_{prm}_")}{inc}";
            globalLabels[address] = lab;
            return lab;
        }

        private void writeBanner(string stackname, bool spacing = false)
        {
            if (spacing)
                output.AppendLine("\r\n");
            output.AppendLine("##################################################");
            output.AppendLine($"#{stackname}");
            output.AppendLine("##################################################");
        }



        private void clearLabelAccumulator()
        {
            labelGlobalAccumulator.Clear();
        }

        public BMSDisassembler(Stream bmsStream)
        {
            reader = new BeBinaryReader(bmsStream);
        }



        private bool isStopEvent(bmscommand command, bool implicitJumpTerm = true)
        {
            if (command is Finish || command is Return || command is ReturnNoArg)
                return true;
            if (command is Jump)
                if (((Jump)command).Flags == 0 && implicitJumpTerm)
                    return true;
            return false;
        }

        private int[] guesstimateJumptableSize()
        {
            Queue<int> addrtable = new Queue<int>();
            while (true)
            {
                var address = (int)reader.ReadU24();
                if ((address >> 16) > 0x20) // This is arbitrary.  But i think if the SE.BMS is > 2MB , or you know, 1/12 of the gamecube's RAM that it should be invalid. 
                    break;
                addrtable.Enqueue(address);
            }
            var ret = new int[addrtable.Count];
            var i = 0;
            while (addrtable.Count > 0)
            {
                ret[i] = addrtable.Dequeue();
                i++;
            }
            return ret;
        }

        Dictionary<int, string> localLabelDeduplicator = new Dictionary<int, string>();
        Dictionary<string, int> localLabelAccumulator = new Dictionary<string, int>();
        Dictionary<long, bool> localAddressHistory = new Dictionary<long, bool>();
        Dictionary<int, ReferenceType> localLabelType = new Dictionary<int, ReferenceType>();

        Queue<long> localLabelReferences = new Queue<long>();  


        public void referenceLocalLabel(long addr)
        {
            localLabelReferences.Enqueue(addr);
        }

        public string disassembleGetLabel(long addr, ReferenceType type)
        {
            if (globalLabels.ContainsKey(addr))
                return "!" + globalLabels[addr];
            else
            {

                referenceLocalLabel(addr);
                return "$" + getLocalLabel(type, (int)addr);
            }
        }

        public void resetLocalStack()
        {
            localLabelDeduplicator.Clear();
            localLabelAccumulator.Clear();
            localAddressHistory.Clear();
            localLabelReferences.Clear();
        }

        private string getLocalLabel(ReferenceType type, int address, string prm = null)
        {
            if (localLabelDeduplicator.ContainsKey(address))
                return localLabelDeduplicator[address];

            localLabelType[address] = type;
            var inc = -1;
            localLabelAccumulator.TryGetValue(type.ToString(), out inc);
            inc++;
            localLabelAccumulator[type.ToString()] = inc;
            var lab = $"{type}{(prm == null ? "_" : $"_{prm}_")}{inc}";
            localLabelDeduplicator[address] = lab;
            return lab;
        }

        public void reconcileLocalReferences()
        {
            while (localLabelReferences.Count > 0)
            {
                var addr = localLabelReferences.Dequeue();
                var type = localLabelType[(int)addr];
                var lbl = localLabelDeduplicator[(int)addr];
                if (localAddressHistory.ContainsKey(addr)) // We already crossed this path, so no need ;)
                {
                    //DebugSystem.message($"RECONCILER: Label at 0x{addr:X} already satisied by {localLabelDeduplicator[(int)addr]}");
                    continue;
                }

                //DebugSystem.message($"RECONCILER: Label at 0x{addr:X} not satisfied for type {type}! evaluating......{lbl}");

                reader.BaseStream.Position = addr; 
                switch(type)
                {
                    case ReferenceType.JUMP:
                        {
                            writeBanner(lbl, true);
                            while (true)
                            {
                             
                                var startAddress = reader.BaseStream.Position;

                                if (addressReferenceAccumulator.ContainsKey(startAddress))
                                {
                                    var accumReference = addressReferenceAccumulator[startAddress];
                                    if (globalLabels.ContainsKey(startAddress))
                                        output.AppendLine($"GOTO &{globalLabels[startAddress]}");
                                    else
                                    {
                                        var lblx = getLocalLabel(accumReference.type, (int)startAddress);
                                        output.AppendLine($"GOTO {lblx}");
                                    }
  
                                    break;
                                }

                                passedAddresses.Add(reader.BaseStream.Position);
                                var bmsEvent = commandFactory.readNextCommand(reader);
                                disassembleBMSCommand(bmsEvent);
                                if (isStopEvent(bmsEvent, false))
                                    break;
                            }
                            break;

                        }

                    case ReferenceType.CALL:
                        {
                            writeBanner(lbl, true);
                            while (true)
                            {
                          
                                var startAddress = reader.BaseStream.Position;
                                passedAddresses.Add(reader.BaseStream.Position);
                                var bmsEvent = commandFactory.readNextCommand(reader);
                                disassembleBMSCommand(bmsEvent);
                                if (isStopEvent(bmsEvent, true))
                                    break;
                            }
                            break;

                        }
                    case ReferenceType.TRACK:
                        {
                            writeBanner(lbl, true);
                            while (true)
                            {
                       
                                var startAddress = reader.BaseStream.Position;
                                passedAddresses.Add(reader.BaseStream.Position);
                                var bmsEvent = commandFactory.readNextCommand(reader);
                                disassembleBMSCommand(bmsEvent);
                                if (isStopEvent(bmsEvent, true))
                                    break;
                            }
                            break;

                        }

                }
            }
        }


        public void DisassembleRootTrack(int onlyGreaterThan = 0xff)
        {
            writeBanner("BMS ROOT");
            output.AppendLine("# Dissasembled with SEBS by Xayrga");
            while (true)
            {
                var bmsEvent = commandFactory.readNextCommand(reader);
                disassembleBMSCommand(bmsEvent);
                if (isStopEvent(bmsEvent, false))
                    break;
            }
        }



        public void DisassembleTrack(string name = "SOUND")
        {
            writeBanner(name);
            while (true)
            {
                var bmsEvent = commandFactory.readNextCommand(reader);
                disassembleBMSCommand(bmsEvent);
                if (isStopEvent(bmsEvent, false))
                    break;
            }
        }




        public void DisassembleCategory(int jumptable_size = -1, int stopAt = -1)
        {

            var callTableAddress = -1;
            while (true)
            {
                var startAddress = reader.BaseStream.Position;
                passedAddresses.Add(reader.BaseStream.Position);
                var bmsEvent = commandFactory.readNextCommand(reader);
                if (bmsEvent is Call)
                {
                    var ev = (Call)bmsEvent;
                    if (ev.Flags == 0xC0)
                    {
                        callTableAddress = (int)ev.Address;
                        output.AppendLine("%%CATEGORY_JUMPTABLE_COMMAND%%");
                    }
                    else
                        disassembleBMSCommand(bmsEvent);
                }
                else
                    disassembleBMSCommand(bmsEvent);

                if (reader.BaseStream.Position >= stopAt && stopAt > 0)
                    break;

                if (isStopEvent(bmsEvent, false) || callTableAddress == reader.BaseStream.Position)
                    break;
            }
        }

        private string disassemblerProcess(bmscommand cmd)
        {

            switch (cmd.CommandType)
            {
                case BMSCommandType.OPENTRACK:
                    {
                        var command = (OpenTrack)cmd;
                        var label = disassembleGetLabel(command.Address, ReferenceType.TRACK);
                        return cmd.getAssemblyString(new string[] { label });
                    }
                case BMSCommandType.JMP:
                    {
                        var command = (Jump)cmd;
                        var label = disassembleGetLabel(command.Address, ReferenceType.JUMP);
                        return cmd.getAssemblyString(new string[] { label });
                    };
                case BMSCommandType.CALL:
                    {
                        var command = (Call)cmd;
                        var label = disassembleGetLabel(command.Address, ReferenceType.CALL);
                        return cmd.getAssemblyString(new string[] { label });
                    };

                case BMSCommandType.SIMPLEENV:
                    {
                        var command = (SimpleEnvelope)cmd;
                        var label = disassembleGetLabel(command.Address, ReferenceType.ENVELOPE);
                        return cmd.getAssemblyString(new string[] { label });
                    };
                case BMSCommandType.SETINTERRUPT:
                case BMSCommandType.SETI:
                    {
                        var command = (SetInterrupt)cmd;
                        var label = disassembleGetLabel(command.Address, ReferenceType.INTERRUPT);
                        return cmd.getAssemblyString(new string[] { label });
                    };
                default:
                    return cmd.getAssemblyString();
            }
        }

        public string disassembleBMSCommand(bmscommand cmd)
        { 
            if (addressReferenceAccumulator.ContainsKey(cmd.OriginalAddress))
            {
                var accumReference = addressReferenceAccumulator[cmd.OriginalAddress];
                if (globalLabels.ContainsKey(cmd.OriginalAddress))
                    output.AppendLine($"&{globalLabels[cmd.OriginalAddress]}");
                else
                {
                    var lbl = getLocalLabel(accumReference.type, (int)cmd.OriginalAddress);
                    output.AppendLine($":{lbl}");
                }
            }
            localAddressHistory[cmd.OriginalAddress] = true;
            var proc = disassemblerProcess(cmd);
            //DebugSystem.message(proc);
            output.AppendLine(proc);
            return "ERROR";
        }



    }
}
