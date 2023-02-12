
using System.IO;
using bmparse.debug;
using xayrga;
using xayrga.byteglider;

namespace bmparse {
    public static class Program
    {
        static void Main()
        {
            var fileStream = File.OpenRead("luigise.bms");
            var binaryReader = new bgReader(fileStream);

            Queue<int[]> ohno = new Queue<int[]>();
            var StopHints = new int[]
            {
            0x294,
            0x13E3,
            0x3F7B,
            0x5FAD,
            0x675E,
            0xFFFFFFF
            };

            var categorySizes = new int[]
            {
            -1,
            -1,
            -1,
            42,
            -1,
            -1
            };

            binaryReader.SavePosition("ROOT_OPEN");

            var WLF = new bmparse.BMSLinkageAnalyzer(binaryReader);
            WLF.Analyze(0, 0, ReferenceType.ROOT);

            var LinkageInfo = WLF.AddressReferenceAccumulator;
            foreach (KeyValuePair<long,AddressReferenceInfo> iter in LinkageInfo)
            {
                AddressReferenceInfo RefInfo = iter.Value;
                long address = iter.Key;

                var exRefCnt = -1;
                var exRefLA = 0L;
                for (int i=0; i < RefInfo.ReferenceStackSources.Count; i++)
                {
                    var RSS = RefInfo.ReferenceStackSources[i];
                    if (RSS!=exRefLA)
                    {
                        exRefLA= RSS;
                        exRefCnt++;
                    }
                }
                if (exRefCnt > 0)
                {
                    Console.WriteLine($"{RefInfo.Type} A:0x{address:X} Extern Refs:{exRefCnt}  ");

                    for (int i = 0; i < RefInfo.ReferenceStackSources.Count; i++)
                    {
                        var RSS = RefInfo.ReferenceStackSources[i];
                        Console.WriteLine($"\t\t\tR-Stack 0x{RSS:X}");
                    }
                }
            }

              

        }
    }
}