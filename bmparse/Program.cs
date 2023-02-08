using Be.IO;
using System.IO;
using bmparse.debug;

public static class Program
{
    static void Main()
    {
        var qq = File.OpenRead("luigise.bms");
        var qc = new BeBinaryReader(qq);
        Queue<int[]> ohno = new Queue<int[]>();
        var categoryStops = new int[]
        {
            0x660,
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


        var WLF = new bmparse.BMSDisassembler(qq);
        var WLB = new bmparse.BMSDisassembler(qq);


        var qw = WLF.AnalyzeRootTrack();
        var trackInfos = new int[0];
        for (int i = 0; i < qw.Count; i++)
        {
            qq.Position = qw[i];
            trackInfos = WLF.AnalyzeCategory(categorySizes[i], categoryStops[i]);
            ohno.Enqueue(trackInfos);
            for (int b = 0; b < trackInfos.Length; b++)
            {
                qq.Position = trackInfos[b];
                WLF.AnalyizeSound();
            }
        }

        WLF.fullUnexploredDepth();
        WLF.CalculateReferenceTypes();
        WLF.CalculateGlobalLabels();
        WLF.reader.BaseStream.Position = 0;
        WLF.DisassembleRootTrack();
        var basePath = "./lm_out";

        Directory.CreateDirectory(basePath);

        WLF.reconcileLocalReferences();

        File.WriteAllText($"{basePath}/root.txt", WLF.output.ToString());


        for (int i = 0; i < qw.Count; i++)
        {
            Directory.CreateDirectory($"{basePath}/category{i}");
            WLF.output = new System.Text.StringBuilder();
            WLF.resetLocalStack();
            qq.Position = qw[i];
            WLF.DisassembleCategory(categorySizes[i], categoryStops[i]);
            WLF.reconcileLocalReferences();
            File.WriteAllText($"{basePath}/categoryboot{i}.txt",WLF.output.ToString());
            var qqq = ohno.Dequeue();


            Dictionary<long, long> fuuf = new Dictionary<long, long>();

            for (int x=0; x < qqq.Length; x++)
            {
                var soundID = i << 0xC;
                soundID += 0x800 + x;

                Console.WriteLine($"{soundID:X}");
                qq.Position = qqq[x];
                if (!fuuf.ContainsKey(qq.Position))
                    fuuf[qq.Position] = soundID;


                WLF.output = new System.Text.StringBuilder();
                WLF.resetLocalStack();
                WLF.DisassembleTrack($"Sound {i} - {x}");
                WLF.reconcileLocalReferences();
                File.WriteAllText($"{basePath}/category{i}/sound{x}.txt", WLF.output.ToString());
            }

            //foreach (KeyValuePair<long,long> b in fuuf)
               // Console.WriteLine($"{b.Value:X}");

            
        }
    }
}
