
using System.IO;
using bmparse.debug;
using xayrga;
using xayrga.byteglider;

public static class Program
{
    static void Main()
    {
        var fileStream = File.OpenRead("luigise.bms");
        var binaryReader = new bgReader(fileStream);

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

        binaryReader.SavePosition("ROOT_OPEN");

        var WLF = new bmparse.BMSDisassembler(binaryReader);
        
    }
}
