using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using bmparse.bms;
using xayrga;
using xayrga.byteglider;
using bmparse.debug;

namespace bmparse
{
    internal partial class BMSLinkageAnalyzer
    {
        public static bmsparser commandFactory = new bmsparser();
        public bgReader reader;

        public Dictionary<long, AddressReferenceInfo> AddressReferenceAccumulator = new Dictionary<long, AddressReferenceInfo>();
        public Dictionary<long, int> travelHistory = new Dictionary<long, int>();
        public Dictionary<long, long> CodePageMapping = new Dictionary<long, long>();

        public List<long> Analyzed = new List<long>();
       
        public int[] StopHints = new int[0];

        public BMSLinkageAnalyzer(bgReader reader)
        {
            this.reader = reader;  
        }


        private AddressReferenceInfo referenceAddress(long addr, ReferenceType type, long src = 0, int depth = 0, bool overrideType = false)
        {
            AddressReferenceInfo inc = null;
            AddressReferenceAccumulator.TryGetValue(addr, out inc);
            if (inc == null)
                inc = new AddressReferenceInfo()
                {
                    Type = type,
                    SourceStack = src
                };
            inc.Depth = depth;
            inc.Address = addr;
            inc.RefCount++;
            if (overrideType)
                inc.Type = type;

            inc.ReferenceStackSources.Add(src);
                
            AddressReferenceAccumulator[addr] = inc;
            return inc;
        }

  

        private long Position
        {
            get {
                return reader.BaseStream.Position;
            }

            set
            {
                reader.BaseStream.Position = value;
            }
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
                if (checkStopHint(Position)) // does hint data say we should stop?
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


        public Dictionary<long,AddressReferenceInfo> Analyze(long src , int depth,  ReferenceType currentType )
        {

            Stack<AddressReferenceInfo> toAnalyze = new Stack<AddressReferenceInfo>();

            AddressReferenceInfo bb = null;
            if (AddressReferenceAccumulator.ContainsKey(src))
                bb = AddressReferenceAccumulator[src];

            var originalPosition = Position;

            bool STOP = false; 
            while (true)
            {

                travelHistory[Position] = 1;
                CodePageMapping[Position] = src;
               
                var command = commandFactory.readNextCommand(reader);
        
                AddressReferenceInfo AddressRefInfo;
                switch (command.CommandType)
                {
                    case BMSCommandType.CALLTABLE:
                        var callt = (Call)command;
                        AddressRefInfo = referenceAddress(callt.Address, ReferenceType.CALLTABLE, src, depth);
                        toAnalyze.Push(AddressRefInfo);
                        break;
                    case BMSCommandType.CALL:
                        var call = (Call)command;
                        if (call.Flags != 0xC0)
                        {
                            AddressRefInfo =  referenceAddress(call.Address, ReferenceType.CALL, src, depth);
                            toAnalyze.Push(AddressRefInfo);
                        } else
                        {
                            AddressRefInfo = referenceAddress(call.Address, ReferenceType.CALLTABLE, src, depth);
                            toAnalyze.Push(AddressRefInfo);
                        }
                        break;
                    case BMSCommandType.JMP:
                        var jmp = (Jump)command ;

                        if (travelHistory.ContainsKey(jmp.Address))
                            AddressRefInfo = referenceAddress(jmp.Address, ReferenceType.LEADIN, src, depth);
                        else
                            AddressRefInfo = referenceAddress(jmp.Address, ReferenceType.JUMP, src, depth);

                        toAnalyze.Push(AddressRefInfo);
                        if (jmp.Flags == 0) // We need to separate from this address because it's jumped into a new scope.
                            STOP = true;
                        break;                   
                    case BMSCommandType.OPENTRACK:
                        var opentrack = (OpenTrack)command;

                        AddressRefInfo = referenceAddress(opentrack.Address, ReferenceType.TRACK, src, depth);
                    
                        toAnalyze.Push(AddressRefInfo);
                        break;
                    case BMSCommandType.SIMPLEENV:
                        var senv = (SimpleEnvelope)command;
                        referenceAddress(senv.Address, ReferenceType.ENVELOPE, src, depth);
                        break;
                    case BMSCommandType.SETINTERRUPT:
                        var sint = (SetInterrupt)command;
                        AddressRefInfo = referenceAddress(sint.Address, ReferenceType.INTERRUPT, originalPosition, depth);
                     
                        toAnalyze.Push(AddressRefInfo);
         
                        break;
                    case BMSCommandType.RETURN:
                        var retco = (Return)command;
                        if (retco.Condition == 0x00)
                            STOP = true;
                        break;

                    case BMSCommandType.FINISH:
                    case BMSCommandType.RETURN_NOARG:
                    case BMSCommandType.RETI:
                        STOP = true;
                        break;
                }
                if (STOP)
                    break;
            }

            // Increment stack depth for next call 
            depth++;
            while (toAnalyze.Count > 0)
            {
                // Save position
                reader.PushAnchor();
                var addrInfo = toAnalyze.Pop();

                switch (addrInfo.Type)
                {
                    case ReferenceType.JUMPTABLE:
                    case ReferenceType.CALLTABLE:
                        Position = addrInfo.Address;
                        // Need to unroll table into the addrinfo!
                        var entries = guesstimateJumptableSize();
                        //Console.WriteLine($"{new string('-', depth)} CALLTABLE");
                        for (int i=0; i < entries.Length; i++)
                        {
                            var AddressRefInfo = referenceAddress(entries[i], addrInfo.Type==ReferenceType.CALLTABLE ? ReferenceType.CALLFROMTABLE : ReferenceType.JUMP, entries[i], depth + 1,true );
                            if (!travelHistory.ContainsKey(addrInfo.Address))
                                toAnalyze.Push(AddressRefInfo);
                        }
                        break;
                    case ReferenceType.TRACK:
                        Position = addrInfo.Address;
                        if (!travelHistory.ContainsKey(Position))
                            if (currentType==ReferenceType.CALLFROMTABLE)
                                Analyze(src, addrInfo.Depth + 1, addrInfo.Type);
                            else if (depth < 2)
                                Analyze(Position, addrInfo.Depth + 1, addrInfo.Type);
                            else
                                Analyze(src, addrInfo.Depth + 1, addrInfo.Type);
                            
                       
                        break;
                    case ReferenceType.CALLFROMTABLE:
                        Position = addrInfo.Address;
                            Analyze(Position, addrInfo.Depth + 1, addrInfo.Type);
                        break;

                    default:
                        Position = addrInfo.Address;

                        if (!travelHistory.ContainsKey(Position))
                            Analyze(src, addrInfo.Depth + 1, addrInfo.Type);
                        break;
                }

                reader.PopAnchor();
            }
            return AddressReferenceAccumulator;
        }
    }
}
