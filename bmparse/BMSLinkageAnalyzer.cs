﻿using System;
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
        public List<long> Analyzed = new List<long>();
       
        public int[] StopHints = new int[0];


  
        public BMSLinkageAnalyzer(bgReader reader)
        {
            this.reader = reader;  
        }


        private AddressReferenceInfo referenceAddress(long addr, ReferenceType type, long src = 0, int depth = 0)
        {
            AddressReferenceInfo inc = null;
            AddressReferenceAccumulator.TryGetValue(addr, out inc);
            if (inc == null)
                inc = new AddressReferenceInfo()
                {
                    Type = type,
                };
            inc.Depth = depth;
            inc.Address = addr;
            inc.RefCount++;
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


        public void Analyze(long src = 0, int depth = 0)
        {

            Stack<AddressReferenceInfo> toAnalyze = new Stack<AddressReferenceInfo>();

            AddressReferenceInfo bb = null;
            if (AddressReferenceAccumulator.ContainsKey(src))
                bb = AddressReferenceAccumulator[src];
            
            Console.WriteLine($"{new string('-',depth)} {src:X} ({(bb==null ? "UNREF" : bb.Type)}) ");

            bool STOP = false; 
            while (true)
            {
        
                // Console.Write($"{Position:X}");
                if (travelHistory.ContainsKey(Position))     
                    break;

                // Store history position.
                travelHistory[Position] = 1;

                var command = commandFactory.readNextCommand(reader);

               // Console.WriteLine($"{new string('-', depth)} {command.CommandType} ");

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
                        AddressRefInfo = referenceAddress(jmp.Address, ReferenceType.JUMP, src, depth);
                        toAnalyze.Push(AddressRefInfo);
                        if (jmp.Flags == 0) // We need to separate from this address because it's jumped into a new scope.
                            STOP = true;
                        break;                   
                    case BMSCommandType.OPENTRACK:
                        var opentrack = (OpenTrack)command ;             
                        AddressRefInfo = referenceAddress(opentrack.Address, ReferenceType.TRACK, src, depth);
                        toAnalyze.Push(AddressRefInfo);
                        break;
                    case BMSCommandType.SIMPLEENV:
                        var senv = (SimpleEnvelope)command;
                        referenceAddress(senv.Address, ReferenceType.ENVELOPE, src, depth);
                        break;
                    case BMSCommandType.SETINTERRUPT:
                        var sint = (SetInterrupt)command;
                        AddressRefInfo = referenceAddress(sint.Address, ReferenceType.INTERRUPT, src, depth);
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

                        // Need to unroll table into the data!
                        var entries = guesstimateJumptableSize();
                        Console.WriteLine($"{new string('-', depth)} CALLTABLE");
                        for (int i=0; i < entries.Length; i++)
                        {
                           
                            var AddressRefInfo = referenceAddress(entries[i], addrInfo.Type==ReferenceType.CALLTABLE ? ReferenceType.CALLFROMTABLE : ReferenceType.JUMP, src, depth + 1);
                            if (!travelHistory.ContainsKey(addrInfo.Address))
                                toAnalyze.Push(AddressRefInfo);
     
                        }

                        break;
                    default:
                        Position = addrInfo.Address;

                        if (!travelHistory.ContainsKey(Position))
                            Analyze(Position, addrInfo.Depth + 1);

                        break;
                }

                reader.PopAnchor();
            }

        }
    }
}