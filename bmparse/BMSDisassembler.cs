using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using bmparse.bms;
using Be.IO;

namespace bmparse
{
    internal partial class BMSDisassembler
    {
        public static bmsparser commandFactory = new bmsparser();
        public BeBinaryReader reader;
        public StringBuilder output = new StringBuilder();
        public string OutFolder = "";

        Dictionary<long, string> globalLabels = new Dictionary<long, string>();
        Dictionary<string, int> labelGlobalAccumulator = new Dictionary<string, int>();
        public Dictionary<long, AddressReferenceInfo> addressReferenceAccumulator = new Dictionary<long, AddressReferenceInfo>();
        public Dictionary<long, long> discreetGotoLookup = new Dictionary<long, long>();    

        public enum ReferenceType
        {
            CALL = 1, 
            JUMP = 2, 
            INTERRUPT = 3, 
            TRACK = 4,
            LEADIN = 5,
        }

        public class AddressReferenceInfo
        {
            public int count = 0;
            public ReferenceType type;
            public List<long> referenceSource = new List<long>();
            public bool singleSource = true;
        }

        private void referenceAddress(long addr, ReferenceType type, long src = 0)
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

        private void writeBanner(string stackname)
        {
            output.AppendLine("##################################################");
            output.AppendLine($"{stackname}");
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

    }
}
