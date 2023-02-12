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
    internal class SEBMSDisassembler
    {

        public Dictionary<long, AddressReferenceInfo> LinkData = new Dictionary<long, AddressReferenceInfo>();

        public Dictionary<long,string> LocalLabels = new Dictionary<long,string>();
        Dictionary<string, int> labelLocalAccumulator = new Dictionary<string, int>();

        public bmsparser commandFactory = new bmsparser();

        public Dictionary<long, string> GlobalLabels = new Dictionary<long, string>();
        Dictionary<string, int> labelGlobalAccumulator = new Dictionary<string, int>();

        Stack<long> commonAddresses = new Stack<long>();
        Stack<long> categoryAddresses = new Stack<long>();

        bgReader reader;

        public SEBMSDisassembler(bgReader reader, Dictionary<long, AddressReferenceInfo> linkData)
        {
            this.reader = reader;
            LinkData = linkData;
            LocalLabels.Clear();
            GlobalLabels.Clear();
            labelLocalAccumulator.Clear();
            labelGlobalAccumulator.Clear();

        }

        private void getBanner(string stackname, bool spacing = false)
        {
            var str = "";
            if (spacing)
                str+= ("\r\n");
            str+=("##################################################");
            str+=($"#{stackname}");
            str+=("##################################################");
        }

        private string getGlobalLabel(string type, long address, string prm = null)
        {
            if (GlobalLabels.ContainsKey(address))
                return GlobalLabels[address];
            var inc = -1;
            labelGlobalAccumulator.TryGetValue(type, out inc);
            inc++;
            labelGlobalAccumulator[type] = inc;
            var lab = $"{type}{(prm == null ? "_" : $"_{prm}_")}{inc}";
            GlobalLabels[address] = lab;
            return lab;
        }

        private string getLocalLabel(string type, long address, string prm = null)
        {
            if (LocalLabels.ContainsKey(address))
                return LocalLabels[address];
            var inc = -1;
            labelLocalAccumulator.TryGetValue(type, out inc);
            inc++;
            labelLocalAccumulator[type] = inc;
            var lab = $"{type}{(prm == null ? "_" : $"_{prm}_")}{inc}";
            LocalLabels[address] = lab;
            return lab;
        }

        private int getExternalReferenceCount(AddressReferenceInfo addrInfo)
        {
            var exRefCnt = -1;
            var exRefLA = 0L;
            for (int i = 0; i < addrInfo.ReferenceStackSources.Count; i++)
            {
                var RSS = addrInfo.ReferenceStackSources[i];
                if (RSS != exRefLA)
                {
                    exRefLA = RSS;
                    exRefCnt++;
                }
            }
            return exRefCnt;
        }


        private void DisassembleRoutine()
        {

            bool STOP = false;
            string line = "";
            while (true)
            {

                var command = commandFactory.readNextCommand(reader);

                AddressReferenceInfo AddressRefInfo;
                switch (command.CommandType)
                {
                    case BMSCommandType.CALLTABLE:
                        var callt = (Call)command;
                        break;
                    case BMSCommandType.CALL:
                        var call = (Call)command;
                        if (call.Flags != 0xC0)
                        {
                            line = call.getAssemblyString(new string[] { getLocalLabel("CALL", call.Address) });
                        }
                        else
                        {
                                           
                        }
                        break;
                    case BMSCommandType.JMP:
                        var jmp = (Jump)command;

                        line = jmp.getAssemblyString(new string[] { getLocalLabel("JUMP", jmp.Address) });
                        if (jmp.Flags == 0) // We need to separate from this address because it's jumped into a new scope.
                            STOP = true;
                        break;
                    case BMSCommandType.OPENTRACK:
                        var trkOpen = (OpenTrack)command;
                        line = trkOpen.getAssemblyString(new string[] { getLocalLabel("OPENTRK", trkOpen.Address) });
                        break;
                    case BMSCommandType.SIMPLEENV:
             
                        break;
                    case BMSCommandType.SETINTERRUPT:
            


                        break;
                    case BMSCommandType.RETURN:
                        var retco = (Return)command;
                        line = retco.getAssemblyString();
                        if (retco.Condition == 0x00)
                            STOP = true;
                        break;

                    case BMSCommandType.FINISH:
                    case BMSCommandType.RETURN_NOARG:
                    case BMSCommandType.RETI:
                        line = command.getAssemblyString();
                        STOP = true;
                        break;
                    default:
                        line = command.getAssemblyString();
                        break;
                }
                Console.WriteLine(line);
                if (STOP)
                    break;
            }

        }

        public void BuildGlobalLabelsFromLinkInfo()
        {
            var cat = 0;
            foreach (KeyValuePair<long, AddressReferenceInfo> iter in LinkData)
            {
                AddressReferenceInfo RefInfo = iter.Value;
                long address = iter.Key;


                var refSources = getExternalReferenceCount(RefInfo);

                // First level tracks are categories. 
                if (RefInfo.Type == ReferenceType.TRACK && RefInfo.Depth == 1) { 
                    getGlobalLabel("CATEGORY", address);
                    categoryAddresses.Push(RefInfo.Address);
                }
                else if (refSources > 0)
                {
                    switch (RefInfo.Type)
                    {
                        case ReferenceType.TRACK:
                            getGlobalLabel("TRACK", address, "COMMON");
                            commonAddresses.Push(RefInfo.Address);
                            break;
                        case ReferenceType.INTERRUPT:
                            getGlobalLabel("INTERRUPT", address, "COMMON");
                            commonAddresses.Push(RefInfo.Address);
                            break;
                        case ReferenceType.CALL:
                            getGlobalLabel("CALL", address, "COMMON");
                            commonAddresses.Push(RefInfo.Address);
                            break;
                        case ReferenceType.JUMP:
                            getGlobalLabel("JUMP", address, "COMMON");
                            commonAddresses.Push(RefInfo.Address);
                            break;
                        case ReferenceType.ENVELOPE:
                            getGlobalLabel("ENVELOPE", address, "COMMON");
                            commonAddresses.Push(RefInfo.Address);
                            break;
                        case ReferenceType.LEADIN:
                            getGlobalLabel("INJUMP", address, "COMMON");
                            commonAddresses.Push(RefInfo.Address);
                            break;
                    }
                }
            }

            foreach (KeyValuePair<long, string> iter in GlobalLabels)
            {
                Console.WriteLine($"0x{iter.Key:X} {iter.Value}");
                reader.BaseStream.Position = iter.Key;
                DisassembleRoutine();
            }
        }
    }
}
