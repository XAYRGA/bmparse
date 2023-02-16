
using System.IO;
using bmparse.debug;
using xayrga;
using xayrga.byteglider;

namespace bmparse {
    public static class Program
    {
        static void Main()
        {

            var outFile = File.OpenWrite("out.bms");
            var writer = new bgWriter(outFile);

            var WX = new SEBMSAssembler();
            WX.LoadData("lm2/init.txt");
            WX.SetOutput(writer);
            WX.ProcBuffer();

            /*
            var fileStream = File.OpenRead("luigise.bms");
            var binaryReader = new bgReader(fileStream);

            binaryReader.SavePosition("ROOT_OPEN");
            var WLF = new bmparse.BMSLinkageAnalyzer(binaryReader);
            WLF.Analyze(0, 0, ReferenceType.ROOT);
            var LinkageInfo = WLF.AddressReferenceAccumulator;


            binaryReader.GoPosition("ROOT_OPEN");
            var WL2 = new bmparse.SEBMSDisassembler(binaryReader,LinkageInfo);
            WL2.CodePageMapping = WLF.CodePageMapping;
            WL2.Disassemble("lm2");
           // */

            /*
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
            */



        }
    }
}