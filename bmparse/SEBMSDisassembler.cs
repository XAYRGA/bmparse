using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xayrga;
using xayrga.byteglider;
using bmparse.bms;
using Newtonsoft.Json;

namespace bmparse
{
    internal class SEBMSDisassembler
    {

        public SEBMSProject Project;

        public Dictionary<long, AddressReferenceInfo> LinkData = new Dictionary<long, AddressReferenceInfo>();

        public Dictionary<long,string> LocalLabels = new Dictionary<long,string>();
        Dictionary<string, int> labelLocalAccumulator = new Dictionary<string, int>();

        public bmsparser commandFactory = new bmsparser();

        public Dictionary<long, string> GlobalLabels = new Dictionary<long, string>();
        Dictionary<string, int> labelGlobalAccumulator = new Dictionary<string, int>();

        Queue<AddressReferenceInfo> commonAddresses = new Queue<AddressReferenceInfo>();
        Queue<AddressReferenceInfo> categoryAddresses = new Queue<AddressReferenceInfo> ();

        Dictionary<long, byte> traveled = new Dictionary<long, byte>();

        bgReader reader;

        public string ProjFolder = "lm_out";

        StringBuilder output = new StringBuilder();

        public SEBMSDisassembler(bgReader reader, Dictionary<long, AddressReferenceInfo> linkData)
        {
            this.reader = reader;
            LinkData = linkData;
            LocalLabels.Clear();
            GlobalLabels.Clear();
            labelLocalAccumulator.Clear();
            labelGlobalAccumulator.Clear();

         

        }

        private string getBanner(string stackname, bool spacing = false)
        {
            var str = "";
            if (spacing)
                str+= ("\r\n");
            str+=("##################################################\r\n");
            str+=($"#{stackname}\r\n");
            str+=("##################################################\r\n");
            return str;
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

        private void wOut(string ou)
        {
            output.AppendLine(ou);
        }

        private void flushOutput(string file)
        {
            var str = output.ToString();
            File.WriteAllText(file, str);
        }

        private void resetOutput()
        {
            output = new StringBuilder();
        }

        public void Disassemble(string ProjFolder)
        {

            Project = new SEBMSProject();
         

            BuildGlobalLabelsFromLinkInfo();

            this.ProjFolder = ProjFolder;
         
            Directory.CreateDirectory($"{ProjFolder}");
            Directory.CreateDirectory($"{ProjFolder}/cat");
            Directory.CreateDirectory($"{ProjFolder}/common");
            Directory.CreateDirectory($"{ProjFolder}/sounds/");

            D_DisassembleCategories();
            D_DisassembleCommon();

            File.WriteAllText($"{ProjFolder}/project.json",JsonConvert.SerializeObject(Project,Formatting.Indented));
        }
        private void D_DisassembleCategories()
        {
     
            var catNum = 0;

            Project.CategoryLogics = new string[categoryAddresses.Count];

            while (categoryAddresses.Count > 0)
            {
                resetOutput();
                var AddrInfo = categoryAddresses.Dequeue();
                LocalLabels.Clear();
                labelLocalAccumulator.Clear();
                switch (AddrInfo.Type)
                {
                    default:
                        reader.BaseStream.Position = AddrInfo.Address;
                        var name = AddrInfo.Name;
                        DisassembleRoutine(AddrInfo, AddrInfo.Name);
                        flushOutput($"{ProjFolder}/cat/{name}.txt");
                        Project.CategoryLogics[catNum] = $"cat/{name}.txt";
                        break;
                }
                catNum++;
            }
        }

        private void D_DisassembleCommon()
        {

            Project.CommonLib = new string[commonAddresses.Count + 1];
            var comNum = 0;
            while (commonAddresses.Count > 0)
            {

                resetOutput();
                var AddrInfo = commonAddresses.Dequeue();
                LocalLabels.Clear();
                labelLocalAccumulator.Clear();
                switch (AddrInfo.Type)
                {
                    case ReferenceType.ENVELOPE:
                        wOut(getBanner(AddrInfo.Name));
                        reader.BaseStream.Position = AddrInfo.Address;
                        DisassembleEnvelope();
                        flushOutput($"{ProjFolder}/common/{AddrInfo.Name}.txt");
                        Project.CommonLib[comNum] = $"cat/{AddrInfo.Name}.txt";
                        break;
                    default:
                        reader.BaseStream.Position = AddrInfo.Address;
                        DisassembleRoutine(AddrInfo, AddrInfo.Name);
                        flushOutput($"{ProjFolder}/common/{AddrInfo.Name}.txt");
                        Project.CommonLib[comNum] = $"cat/{AddrInfo.Name}.txt";
                        break;
                }

                comNum++;
            }
        }


        private void DisassembleEnvelope()
        {
            if (LinkData.ContainsKey(reader.BaseStream.Position))
            {
                var ld = LinkData[reader.BaseStream.Position];
                bool nBool = false;
                wOut(":" + getLabelGeneric(ld.Type.ToString(), ld.Address, out nBool));
            }

            var env = JInstrumentEnvelopev1.CreateFromStream(reader);
            for (int i = 0; i < env.points.Length; i++)
            {
                var point = env.points[i];
                wOut($"ENVPOINT {point.Mode} {point.Delay} {point.Value}");
            }

            wOut("STOP #Envelope Termination");
        }

        private string getLabelGeneric(string type, long address, out bool newCreated, string prm = null)
        {
            newCreated = false;
            if (LocalLabels.ContainsKey(address))
                return LocalLabels[address];

            if (GlobalLabels.ContainsKey(address))
                return "@" + GlobalLabels[address];

            newCreated = true;
            return getLocalLabel(type, address, prm);
        }

        
        private void DisassembleRoutine(AddressReferenceInfo RefInfo, string stackname = "SOUND" )
        {
            var LocalReference = new Queue<AddressReferenceInfo>();

            bool STOP = false;
            string line = "";
            while (true)
            {
                if (LinkData.ContainsKey(reader.BaseStream.Position))
                {
                 
                    var ld = LinkData[reader.BaseStream.Position];
                    if (ld.SourceAddress != RefInfo.SourceAddress)
                        wOut(":" + getGlobalLabel("LEADIN", reader.BaseStream.Position, "COMMON")); // I think sunshine did this...
                    else
                    {
                        bool nBool = false;
                        wOut(":" + getLabelGeneric(ld.Type.ToString(), ld.Address, out nBool));
                    }
                }

                traveled[reader.BaseStream.Position] = 1;
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
                            bool newCreated = false;
                            line = call.getAssemblyString(new string[] { getLabelGeneric("CALL", call.Address, out newCreated) });
                            if (newCreated)
                                LocalReference.Enqueue(LinkData[call.Address]);
                        }
                        else
                        {
                                            
                        }
                        break;
                    case BMSCommandType.JMP:
                        {
                            var jmp = (Jump)command;

                            bool newCreated = false;
                            line = jmp.getAssemblyString(new string[] { getLabelGeneric("JUMP", jmp.Address, out newCreated) });
                            if (jmp.Flags == 0) // We need to separate from this address because it's jumped into a new scope.
                                STOP = true;
                            if (newCreated)
                                LocalReference.Enqueue(LinkData[jmp.Address]);
                        }
                        break;
                    case BMSCommandType.OPENTRACK:
                        {
                            bool newCreated = false;
                            var trkOpen = (OpenTrack)command;
                            line = trkOpen.getAssemblyString(new string[] { getLabelGeneric("OPENTRACK", trkOpen.Address, out newCreated) });
                            if (newCreated)
                                LocalReference.Enqueue(LinkData[trkOpen.Address]);
                        }
                        break;
                    case BMSCommandType.SIMPLEENV:
             
                        break;
                    case BMSCommandType.SETINTERRUPT:
                        {
                            bool newCreated = false;
                            var setInterrupt = (SetInterrupt)command;
                            line = setInterrupt.getAssemblyString(new string[] { getLabelGeneric("INTERRUPT", setInterrupt.Address, out newCreated) });
                        }
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
                wOut(line);
                if (STOP)
                    break;
            }
            while (LocalReference.Count > 0)
            {
                var rf = LocalReference.Dequeue();
                reader.BaseStream.Position = rf.Address;
                if (!traveled.ContainsKey(rf.Address))
                    DisassembleRoutine(rf);
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
                    RefInfo.Name = getGlobalLabel("CATEGORY", address);
                    categoryAddresses.Enqueue(RefInfo);
                }
                else if (refSources > 0)
                {
                    switch (RefInfo.Type)
                    {
                        case ReferenceType.TRACK:
                            RefInfo.Name = getGlobalLabel("TRACK", address, "COMMON");
                            commonAddresses.Enqueue(RefInfo);
                            break;
                        case ReferenceType.INTERRUPT:
                            RefInfo.Name = getGlobalLabel("INTERRUPT", address, "COMMON");
                            commonAddresses.Enqueue(RefInfo);
                            break;
                        case ReferenceType.CALL:
                            RefInfo.Name = getGlobalLabel("CALL", address, "COMMON");
                            commonAddresses.Enqueue(RefInfo);
                            break;
                        case ReferenceType.JUMP:
                            RefInfo.Name = getGlobalLabel("JUMP", address, "COMMON");
                            commonAddresses.Enqueue(RefInfo);
                            break;
                        case ReferenceType.ENVELOPE:
                            RefInfo.Name = getGlobalLabel("ENVELOPE", address, "COMMON");
                            commonAddresses.Enqueue(RefInfo);
                            break;
                        case ReferenceType.LEADIN:
                            RefInfo.Name = getGlobalLabel("LEADIN", address, "COMMON");
                            //commonAddresses.Push(RefInfo);
                            break;
                    }
                }
            }

        }
    }
}
