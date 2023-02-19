
using System.IO;
using bmparse.debug;
using xayrga;
using xayrga.byteglider;
using Newtonsoft.Json;

namespace bmparse {
    public static class Program
    {
        static void Main()
        {

            var www = File.ReadAllText("lm2/project.json");
            SEBMSProject PROJ = JsonConvert.DeserializeObject<SEBMSProject>(www);
            var WX = new SEBMSAssembler();
            WX.BuildProject(PROJ, "lm2", "out2.bms");


            /*
    
            WX.LoadData("lm2/init.txt");

            var FList = Directory.GetFiles("lm2/common/", "*.txt");
            for (int i = 0; i < FList.Length;i++)
            {
                WX.LoadData(FList[i]);
                WX.ProcBuffer();
            }
            */
            ///*
            ///
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



            WL2.CategoryNames[1] = "JA_CAT_LUIGE";
            var w = File.ReadAllLines("luigie.txt");

            WL2.SoundNames[1] = new Dictionary<int, string>();
            for (int i=0; i < w.Length; i++)
            {
                WL2.SoundNames[1][i] = w[i];
            }


            WL2.CategoryNames[2] = "JA_CAT_VOICE";
            w = File.ReadAllLines("voice.txt");

            WL2.SoundNames[2] = new Dictionary<int, string>();
            for (int i = 0; i < w.Length; i++)
            {
                WL2.SoundNames[2][i] = w[i];
            }


            WL2.CategoryNames[3] = "JA_CAT_ENVIRONMENT";
            w = File.ReadAllLines("env.txt");

            WL2.SoundNames[3] = new Dictionary<int, string>();
            for (int i = 0; i < w.Length; i++)
            {
                WL2.SoundNames[3][i] = w[i];
            }



            WL2.CategoryNames[4] = "JA_CAT_SYSTEM";
            w = File.ReadAllLines("sys.txt");

            WL2.SoundNames[4] = new Dictionary<int, string>();
            for (int i = 0; i < w.Length; i++)
            {
                WL2.SoundNames[4][i] = w[i];
            }




            WL2.CategoryNames[0] = "JA_CAT_ENEMY";
            w = File.ReadAllLines("enemy.txt");

            WL2.SoundNames[0] = new Dictionary<int, string>();
            for (int i = 0; i < w.Length; i++)
            {
                WL2.SoundNames[0][i] = w[i];
            }



            WL2.Disassemble("lm2");


            //*/









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