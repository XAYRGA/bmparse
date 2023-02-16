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

        public int[] StopHints = new int[0];

        public Dictionary<long, long> CodePageMapping = new Dictionary<long, long>();
       
        List<int[]> CategoryTableAddrs = new List<int[]>();

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
            var exRefCount = -1;
            var exRefLastAddress = 0L;
            for (int i = 0; i < addrInfo.ReferenceStackSources.Count; i++)
            {
                var RSS = addrInfo.ReferenceStackSources[i];

                if (RSS != exRefLastAddress)
                {
                    exRefLastAddress = RSS;
                    exRefCount++;
                }
            }

            //if (addrInfo.Type!=ReferenceType.CALLTABLE && addrInfo.Type!=ReferenceType.ENVELOPE && CodePageMapping[addrInfo.Address] != CodePageMapping[addrInfo.SourceAddress])
            //    exRefCount++;



            return exRefCount;
        }

        private void D_Out(string ou)
        {
            //Console.WriteLine(ou)
            output.AppendLine(ou);
        }

        private void flushOutput(string file)
        {
            var str = output.ToString();
            File.WriteAllText(file, str);
        }

        private void D_resetOutput()
        {
            output = new StringBuilder();
        }

        private void L_resetLocalScope()
        {
            LocalLabels.Clear();
            labelLocalAccumulator.Clear();
        }

        public void Disassemble(string SynthsAreSubbies)
        {

            Project = new SEBMSProject();

            BuildGlobalLabelsFromLinkInfo();

            this.ProjFolder = SynthsAreSubbies;

            Directory.CreateDirectory($"{SynthsAreSubbies}");
            Directory.CreateDirectory($"{SynthsAreSubbies}/cat");
            Directory.CreateDirectory($"{SynthsAreSubbies}/common");
            Directory.CreateDirectory($"{SynthsAreSubbies}/sounds/");

            D_DisassembleCategories();
            D_DisassembleCommon();
            D_DisassembleSounds();

            reader.GoPosition("ROOT_OPEN");

            L_resetLocalScope();
            D_resetOutput();

            D_Out(getBanner("ROOT\n#SEBS By Xayrga!"));

            DisassembleRoutine(new AddressReferenceInfo() {
                Address = 0,
                Depth = 0,
                ReferenceStackSources = { 
                    1,
                    2,
                    3,
                    69, 
                    420 }, 
                MetaData = 0, 
                Name = "ROOT", 
                ImplicitCallTermination = true, 
                RefCount = 50000, 
                SourceAddress = 0,
                SourceStack = 0 });

            Project.InitSection = "init.txt";
            flushOutput($"{SynthsAreSubbies}/init.txt");

            File.WriteAllText($"{SynthsAreSubbies}/project.json", JsonConvert.SerializeObject(Project, Formatting.Indented));
        }



        private void D_DisassembleSounds()
        {
                
            Dictionary<long, string> filesAtAddress = new Dictionary<long, string>();
            for (int i=0; i < CategoryTableAddrs.Count; i++)
            {
                var projCat = Project.SoundLists[i];

                Directory.CreateDirectory($"{ProjFolder}/sounds/{i}");
                var ls = CategoryTableAddrs[i];

                projCat.Sounds = new string[ls.Length];

                for (int x=0; x < ls.Length; x++)
                {

                    xayrga.cmdl.consoleHelpers.consoleProgress($"Category {i} disassembling sounds", x + 1, ls.Length, true);
                    if (filesAtAddress.ContainsKey(ls[x]))
                    {
                        projCat.Sounds[x] = filesAtAddress[ls[x]];
                        continue;
                    }

                    L_resetLocalScope();
                    D_resetOutput();
                    reader.BaseStream.Position = ls[x];
                    var ld = LinkData[reader.BaseStream.Position];
             
                    DisassembleRoutine(ld, false, true);
                    var filename = $"sounds/{i}/{x}.txt";
                    projCat.Sounds[x] = filename;
                    flushOutput($"{ProjFolder}/{filename}");
                    filesAtAddress[ls[x]] = filename;

                }
                Console.WriteLine();
            }               
        }


        private void D_DisassembleCategories()
        {
     
            var catNum = 0;
            Project.CategoryLogics = new string[categoryAddresses.Count];
            Project.SoundLists = new SEBMSProjectCategory[categoryAddresses.Count];
           
            while (categoryAddresses.Count > 0)
            {
                xayrga.cmdl.consoleHelpers.consoleProgress("Decompiling categories...", catNum + 1, Project.SoundLists.Length,true);
               
                Project.SoundLists[catNum] = new SEBMSProjectCategory();
                D_resetOutput();
                var AddrInfo = categoryAddresses.Dequeue();
                L_resetLocalScope();
                switch (AddrInfo.Type)
                {
                    default:
                        reader.BaseStream.Position = AddrInfo.Address;
                        var name = AddrInfo.Name;
                        int[] soundS = DisassembleRoutine(AddrInfo,true);
                        CategoryTableAddrs.Add(soundS);
                        flushOutput($"{ProjFolder}/cat/{name}.txt");
                        Project.CategoryLogics[catNum] = $"cat/{name}.txt";
                  
                        break;
                }
                catNum++;
            }
            Console.WriteLine();
        }


        private bool checkStopHint(long pos)
        {

            foreach (int p in StopHints)
                if (p != 0 && p == pos)
                    return true;
            return false;
        }

        private int[] guesstimateJumptableSize()
        {
            Queue<int> addrtable = new Queue<int>();
            while (true)
            {
                if (checkStopHint(reader.BaseStream.Position)) // does hint data say we should stop?
                    break;

                var address = (int)reader.ReadUInt24BE();

                if ((address >> 16) > 0x20) // oops, magic number
                    break;

                addrtable.Enqueue(address);
            }
            var ret = new int[addrtable.Count];
            var i = 0;
            // Unroll Queue: Need to preserve order!            
            while (addrtable.Count > 0)
            {
                ret[i] = addrtable.Dequeue();
                i++;
            }
            return ret;
        }

        private void D_DisassembleCommon()
        {

            Project.CommonLib = new string[commonAddresses.Count + 1];
            var comNum = 0;
            while (commonAddresses.Count > 0)
            {
                D_resetOutput();
                var AddrInfo = commonAddresses.Dequeue();
                L_resetLocalScope();
                switch (AddrInfo.Type)
                {
                    case ReferenceType.ENVELOPE:
                        D_Out(getBanner(AddrInfo.Name));
                        reader.BaseStream.Position = AddrInfo.Address;
                        DisassembleEnvelope();
                        flushOutput($"{ProjFolder}/common/{AddrInfo.Name}.txt");
                        Project.CommonLib[comNum] = $"common/{AddrInfo.Name}.txt";
                        break;
                    default:
                        reader.BaseStream.Position = AddrInfo.Address;
                        DisassembleRoutine(AddrInfo);
                        flushOutput($"{ProjFolder}/common/{AddrInfo.Name}.txt");
                        Project.CommonLib[comNum] = $"common/{AddrInfo.Name}.txt";
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
                D_Out(":" + getLabelGeneric(ld.Type.ToString(), ld.Address, out nBool));
            }

            var env = JInstrumentEnvelopev1.CreateFromStream(reader);
            for (int i = 0; i < env.points.Length; i++)
            {
                var point = env.points[i];
                D_Out($"ENVPOINT {point.Mode} {point.Delay} {point.Value}");
            }

            D_Out("STOP #Envelope Termination");
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

        
        private int[] DisassembleRoutine(AddressReferenceInfo RefInfo, bool isCategory = false, bool skipFirstLabel = false )
        {
            var LocalReference = new Queue<AddressReferenceInfo>();


     
            int[] ret = null;
            bool STOP = false;
            string line = "";
            bool skpFst = skipFirstLabel;
            while (true)
            {
                if (skpFst == false)
                {
                    if (LinkData.ContainsKey(reader.BaseStream.Position))
                    {
                        var ld = LinkData[reader.BaseStream.Position];
                        if (ld.SourceAddress != RefInfo.SourceAddress)
                        {
                            //Console.WriteLine($"{ld.SourceAddress:X} {RefInfo.SourceAddress}");
                            D_Out(":@" + getGlobalLabel("LEADIN", reader.BaseStream.Position, "INLINE")); // I think sunshine did this...
                           // Console.WriteLine(":@" + getGlobalLabel("LEADIN", reader.BaseStream.Position, "INLINE"));
                        }
                        else
                        {
                            bool nBool = false;
                            D_Out(":" + getLabelGeneric(ld.Type.ToString(), ld.Address, out nBool));
                        }
                    }
                    skpFst = false;
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
                            if (isCategory)
                            {
                                reader.PushAnchor();
                                reader.BaseStream.Position = call.Address;
                                ret = guesstimateJumptableSize();
                                reader.PopAnchor();
                                line = "%CATEGORY_CALLTABLE";
                            }  else
                            {
                                bool newCreated = false;
                                LocalReference.Enqueue(LinkData[call.Address]);
                                line = "CALLTABLE " + getLabelGeneric("CALLTABLE", call.Address, out newCreated);
                            }                 
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
                        {
                            var env = (SimpleEnvelope)command;
                            bool newCreated = false;
                            line = env.getAssemblyString(new string[] { getLabelGeneric("ENVELOPE", env.Address, out newCreated) });
                            if (newCreated)
                                LocalReference.Enqueue(LinkData[env.Address]);
                        }
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
                D_Out(line);
                if (STOP)
                    break;
            }
            while (LocalReference.Count > 0)
            {
                var rf = LocalReference.Dequeue();
                switch (rf.Type)
                {
                    case ReferenceType.CALLTABLE:
                        throw new Exception("Nested calltable not supported.");
                        break;
                    case ReferenceType.ENVELOPE:
                        D_Out(getBanner("ENVELOPE",true));
                        reader.BaseStream.Position = rf.Address;
                        DisassembleEnvelope();
                        break;

                    default:
                        D_Out("");
                        D_Out("");
                        reader.BaseStream.Position = rf.Address;
                        if (!traveled.ContainsKey(rf.Address))
                        {
                            var vrf = DisassembleRoutine(rf, isCategory);
                            if (vrf != null)
                                ret = vrf;
                        }
                        break;

                }
            }
  
            return ret;
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
