using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bmparse
{

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
        CALLTABLE = 9,
        CALLFROMTABLE = 10,
    }

    public class AddressReferenceInfo
    {
        public long Address;
        public long SourceStack = 0;
        public long SourceAddress = 0;
        public int RefCount = 0;
        public ReferenceType Type;

        public bool ImplicitCallTermination = false;
        public List<long> ReferenceStackSources = new List<long>();
        public long MetaData = -1;
        public string Name;
        public int Depth;
    }

}
