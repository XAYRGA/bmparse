using Be.IO;
using System.IO;
using bmparse.debug;

public static class Program
{
    static void Main()
    {
        var qq = File.OpenRead("luigise.bms");
        var qc = new BeBinaryReader(qq);
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

        var qw = WLF.AnalyzeRootTrack();
        var trackInfos = new int[0];
        for (int i = 0; i < qw.Count; i++)
        {
            qq.Position = qw[i];
            DebugSystem.message($"{qq.Position:X}");
            trackInfos = WLF.AnalyzeCategory(categorySizes[i], categoryStops[i]);
            for (int b = 0; b < trackInfos.Length; b++)
            {
                qq.Position = trackInfos[b];
                WLF.AnalyizeSound();
            }
        }

        WLF.fullUnexploredDepth();
        WLF.CalculateReferenceTypes();

        foreach (KeyValuePair<long, bmparse.BMSDisassembler.AddressReferenceInfo> kvp in WLF.addressReferenceAccumulator)
        {
            DebugSystem.message($"\t{kvp.Value.count:X}\t{kvp.Value.type}\t{kvp.Key:X}: ");
            foreach (long ree in kvp.Value.referenceSource)
                DebugSystem.message($"\t\t\t\t{ree:X}");
            DebugSystem.message("");
        }

        WLF.CalculateGlobalLabels();
        foreach (KeyValuePair<long, string> kvp in WLF.globalLabels)
        {
            DebugSystem.message(kvp.Value);
        }
    }
}
