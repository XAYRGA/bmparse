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
    internal partial class BMSDisassembler
    {
        public static bmsparser commandFactory = new bmsparser();
        public bgReader reader;

        public Dictionary<long, AddressReferenceInfo> addressReferenceAccumulator = new Dictionary<long, AddressReferenceInfo>();
        public Dictionary<long, int> travelHistory = new Dictionary<long, int>();
       
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
            public long Address;
            public long SourceStack = 0;
            public long SourceAddress = 0;
            public int RefCount = 0;
            public ReferenceType Type;
            public long MetaData = -1;
            public string Name; 
        }

        public BMSDisassembler(bgReader reader)
        {
            this.reader = reader;  
        }

        private AddressReferenceInfo referenceAddress(long addr, ReferenceType type, long src = 0)
        {

            AddressReferenceInfo inc = null;
            addressReferenceAccumulator.TryGetValue(addr, out inc);
            if (inc == null)
                inc = new AddressReferenceInfo()
                {
                    Type = type,
                };

            inc.RefCount++;
            //inc.referenceSource.Add(src);
            addressReferenceAccumulator[addr] = inc;
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

        public void Analyze(long src = 0, int depth = 0)
        {

            Stack<long> toAnalyze = new Stack<long>();

            while (true)
            {
                // Store history position.
                travelHistory[Position] = 1;
                var command = commandFactory.readNextCommand(reader);

                switch (command.CommandType)
                {
                    case BMSCommandType.CALL:
        
                        break;
                }
            }
           




        }

       


    }
}
