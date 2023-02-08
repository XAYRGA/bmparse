using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bmparse.bms;
using bmparse.debug;

namespace bmparse
{
    internal partial class BMSDisassembler
    {
        List<long> passedAddresses = new List<long>();

        public List<int> AnalyzeRootTrack(int onlyGreaterThan = 0xff)
        {
            var w = new List<int>();
            var lastAddress = 0;
            while (true)
            {
                var startAddress = reader.BaseStream.Position;
                passedAddresses.Add(reader.BaseStream.Position);
                var bmsEvent = commandFactory.readNextCommand(reader);

                if (bmsEvent is OpenTrack)
                {
                    var ev = (OpenTrack)bmsEvent;


                    //DebugSystem.message($"{ev.Address:X}");
                    referenceAddress(ev.Address, ReferenceType.TRACK);
                    if (!w.Contains((int)ev.Address) && ev.Address > onlyGreaterThan)
                    {
                        w.Add((int)ev.Address);
                        getGlobalLabel("CATEGORY", (int)ev.Address, "OPEN");
                    }
           
                    lastAddress = (int)ev.Address;
                }
                else if (bmsEvent is Jump)
                {
                   
                    var ev = (Jump)bmsEvent;
                    referenceAddress(ev.Address, ReferenceType.JUMP, 0);
                    getGlobalLabel("ROOT_JUMP", (int)ev.Address);
                }
                else if (bmsEvent is Call)
                {
                    var ev = (Call)bmsEvent;
                    getGlobalLabel("ROOT_CALL", (int)ev.Address);
                    referenceAddress(ev.Address, ReferenceType.CALL, startAddress);
                }
                if (isStopEvent(bmsEvent, false))
                    break;
            }
            //Console.ReadLine();
            return w;
        }

        public int[] AnalyzeCategory(int jumptable_size = -1, int stopAt = -1)
        {
            int[] jumptable_data = null;
            var callTableAddress = -1;
            var initialAddress = reader.BaseStream.Position;

            while (true)
            {
                var startAddress = reader.BaseStream.Position;
                passedAddresses.Add(reader.BaseStream.Position);
                var bmsEvent = commandFactory.readNextCommand(reader);
                //DebugSystem.message($"{startAddress:X} {reader.BaseStream.Position:X} {bmsEvent}");
                if (bmsEvent is OpenTrack)
                {
                    var ev = (OpenTrack)bmsEvent;
                    referenceAddress(ev.Address, ReferenceType.TRACK, initialAddress);
                }
                else if (bmsEvent is Jump)
                {
                    var ev = (Jump)bmsEvent;
                    referenceAddress(ev.Address, ReferenceType.JUMP, initialAddress);
                }
                else if (bmsEvent is Call)
                {
                    var ev = (Call)bmsEvent;
                    if (ev.Flags == 0xC0)
                    {
                       //DebugSystem.message($"Found sound jumptable at {reader.BaseStream.Position:X} {ev.Flags:X} -> {ev.Address:X}");
                        var storage = reader.BaseStream.Position;
                        reader.BaseStream.Position = ev.Address;

                        if (jumptable_size > 0)
                        {
                            jumptable_data = new int[jumptable_size];
                            for (int i = 0; i < jumptable_size; i++)
                                jumptable_data[i] += (int)reader.ReadU24();
                        }
                        else
                            jumptable_data = guesstimateJumptableSize();
                        DebugSystem.message($"\tJTable size is {jumptable_data.Length}");
                        reader.BaseStream.Position = storage;
                        callTableAddress = (int)ev.Address;
                    }
                    else
                        referenceAddress(ev.Address, ReferenceType.CALL, initialAddress);
                }
                else if (bmsEvent is SetInterrupt)
                {
                    var ev = (SetInterrupt)bmsEvent;
                    referenceAddress(ev.Address, ReferenceType.INTERRUPT, initialAddress);
                }
                if (reader.BaseStream.Position >= stopAt && stopAt > 0)
                    break;

                if (isStopEvent(bmsEvent, false) || callTableAddress == reader.BaseStream.Position)
                    break;
            }
            DebugSystem.message($"STOP READING AT {reader.BaseStream.Position:X}");
            return jumptable_data;
        }

        public void AnalyzeSingleTrack(long parentAddr=-1)
        {
            var initialAddress = reader.BaseStream.Position;
            if (parentAddr > 0)
                initialAddress = parentAddr;
            while (true)
            {
                var startAddress = reader.BaseStream.Position;
                passedAddresses.Add(reader.BaseStream.Position);
                var bmsEvent = commandFactory.readNextCommand(reader);
                //DebugSystem.message($"SOUND-TRACK {startAddress:X} {reader.BaseStream.Position:X} {bmsEvent}");
                if (bmsEvent is OpenTrack)
                {
                    var ev = (OpenTrack)bmsEvent;
                    referenceAddress(ev.Address, ReferenceType.TRACK, initialAddress);

                }
                else if (bmsEvent is Jump)
                {
                    var ev = (Jump)bmsEvent;
                    referenceAddress(ev.Address, ReferenceType.JUMP, initialAddress);
                }
                else if (bmsEvent is Call)
                {
                    var ev = (Call)bmsEvent;
                    if (ev.Flags == 0xC0)
                    {
                        DebugSystem.message($"WARNING! NESTED JUMPTABLE!!!",MessageLevel.WARNING);
                    }
                    else
                        referenceAddress(ev.Address, ReferenceType.CALL, initialAddress);
                }
                else if (bmsEvent is SetInterrupt)
                {
                    var ev = (SetInterrupt)bmsEvent;
                    referenceAddress(ev.Address, ReferenceType.INTERRUPT, startAddress);
                    DebugSystem.message($"WARNING! NESTED INTERRUPT", MessageLevel.WARNING);
                }
                if (isStopEvent(bmsEvent, true))
                    break;
            }
            DebugSystem.message($"END READING TRACK");

        }


        public void AnalyizeSound()
        {
           
            List<int> TrackAddresses = new List<int>();
            var initialAddress = reader.BaseStream.Position;
            referenceAddress(initialAddress, ReferenceType.SOUND);
            while (true)
            {
                var startAddress = reader.BaseStream.Position;
                passedAddresses.Add(reader.BaseStream.Position);
                var bmsEvent = commandFactory.readNextCommand(reader);
                //DebugSystem.message($"SOUND {startAddress:X} {reader.BaseStream.Position:X} {bmsEvent}");
                if (bmsEvent is OpenTrack)
                {
                    var ev = (OpenTrack)bmsEvent;
                    referenceAddress(ev.Address, ReferenceType.TRACK, initialAddress);
                    TrackAddresses.Add((int)ev.Address);
                }
                else if (bmsEvent is Jump)
                {
                    var ev = (Jump)bmsEvent;
                    referenceAddress(ev.Address, ReferenceType.JUMP, initialAddress);
                }
                else if (bmsEvent is Call)
                {
                    var ev = (Call)bmsEvent;
                    if (ev.Flags == 0xC0)
                    {
                        DebugSystem.message($"WARNING! NESTED JUMPTABLE!!!", MessageLevel.WARNING);
                    }
                    else
                        referenceAddress(ev.Address, ReferenceType.CALL, initialAddress);
                }
                else if (bmsEvent is SetInterrupt)
                {
                    var ev = (SetInterrupt)bmsEvent;
                    referenceAddress(ev.Address, ReferenceType.INTERRUPT, initialAddress);
                }
                if (isStopEvent(bmsEvent, true))
                    break;
            }

            foreach (int b in TrackAddresses)
            {
                reader.BaseStream.Position = b;
                if (!addressReferenceAccumulator.ContainsKey(b)) // this will prevent duplicate reference listings for tracks that get reused. 
                    AnalyzeSingleTrack(initialAddress);
            }
        }

        public void AnalyzeCallOrJump(AddressReferenceInfo addrInfo)
        {
            var initialAddress = reader.BaseStream.Position;
            var trueBeginningAddress = initialAddress;
            if (addrInfo.referenceSource.Count == 1)
                initialAddress = addrInfo.referenceSource[0];
            List<int> TrackAddresses = new List<int>();
            while (true)
            {

                var startAddress = reader.BaseStream.Position;
                if (passedAddresses.Contains(startAddress))
                {
                    referenceAddress(startAddress, ReferenceType.LEADIN, initialAddress).metaData = trueBeginningAddress;
                    break; // We have already been here. 
                }

                passedAddresses.Add(reader.BaseStream.Position);
                var bmsEvent = commandFactory.readNextCommand(reader);
                //DebugSystem.message($"UNDISP BRANCH {startAddress:X} {reader.BaseStream.Position:X} {bmsEvent}");
                if (bmsEvent is OpenTrack)
                {
                    var ev = (OpenTrack)bmsEvent;
                    referenceAddress(ev.Address, ReferenceType.TRACK, initialAddress);
                    TrackAddresses.Add((int)ev.Address);
                }
                else if (bmsEvent is Jump)
                {
                    var ev = (Jump)bmsEvent;
                    referenceAddress(ev.Address, ReferenceType.JUMP, initialAddress);
                    TrackAddresses.Add((int)ev.Address);
                }
                else if (bmsEvent is Call)
                {
                    var ev = (Call)bmsEvent;
                    if (ev.Flags == 0xC0)
                    {
                        DebugSystem.message($"WARNING! NESTED JUMPTABLE!!!", MessageLevel.WARNING);
                    }
                    else
                    {
                        referenceAddress(ev.Address, ReferenceType.CALL, initialAddress);
                        TrackAddresses.Add((int)ev.Address);
                    }
                }
                else if (bmsEvent is SetInterrupt)
                {
                    var ev = (SetInterrupt)bmsEvent;
                    referenceAddress(ev.Address, ReferenceType.INTERRUPT, initialAddress);
                }
                if (isStopEvent(bmsEvent, true))
                    break;
            }

            foreach (int b in TrackAddresses)
            {
                reader.BaseStream.Position = b;
                if (!addressReferenceAccumulator.ContainsKey(b)) // this will prevent duplicate reference listings for tracks that get reused. 
                    AnalyzeCallOrJump(addrInfo);
            }
        }


        public void fullUnexploredDepth()
        {
            var had_unexplored_jumps = false;
            foreach (KeyValuePair<long, AddressReferenceInfo> kvp in addressReferenceAccumulator.ToArray<KeyValuePair<long, AddressReferenceInfo>>())
            {
                if (!passedAddresses.Contains(kvp.Key) && kvp.Value.type!=ReferenceType.LEADIN)
                {
                    reader.BaseStream.Position = kvp.Key;
                    AnalyzeCallOrJump(kvp.Value);
                    had_unexplored_jumps = true;
                }
            }
            if (had_unexplored_jumps)
                fullUnexploredDepth(); // Loop until we've explored the entire BMS file.
        }

        public void CalculateReferenceTypes()
        {
            foreach (KeyValuePair<long,AddressReferenceInfo> kvp in addressReferenceAccumulator.ToArray<KeyValuePair<long,AddressReferenceInfo>>())
            {
                var ref_diff = false;
                var last_src = 0L;
                foreach (long src in kvp.Value.referenceSource)
                {
                    if (last_src == 0)
                        last_src = src;
                    if (last_src != src)
                    {
                        kvp.Value.singleSource = false;
                        break;
                    } 
                }
            }
        }

        public void CalculateGlobalLabels()
        {
            foreach (KeyValuePair<long, AddressReferenceInfo> kvp in addressReferenceAccumulator.ToArray<KeyValuePair<long, AddressReferenceInfo>>())
                if (kvp.Value.count > 1 && kvp.Value.singleSource == false)
                    getGlobalLabel(kvp.Value.type.ToString(), (int)kvp.Key, "COMMON");
                else if (kvp.Value.type==ReferenceType.SOUND)
                    getGlobalLabel(kvp.Value.type.ToString(), (int)kvp.Key);
        }
    }
}
