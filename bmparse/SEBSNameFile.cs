using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xayrga.byteglider;

namespace bmparse
{
    internal class SEBSNameFile
    {
        const uint NAME = 0x4E414D45;

        public Dictionary<int, Dictionary<int, string>> SoundNames = new Dictionary<int, Dictionary<int, string>>();
        public Dictionary<int, string> CategoryNames = new Dictionary<int, string>();

        public void Read(bgReader file)
        {
            var W = file.ReadUInt32();
            if (W != NAME)
                throw new InvalidDataException("Not a NAM file");
            var version = file.ReadUInt32();
            var sectionCount = file.ReadUInt32();
            var sect1Offset = file.ReadUInt32();

            file.BaseStream.Position = sect1Offset;

            var count = file.ReadUInt32();                   
            for (int i=0; i < count; i++)
            {
                var key = file.ReadInt32();
                var name = file.ReadString();
                SoundNames[key] = new Dictionary<int, string>();
                CategoryNames[key] = name;
                var cnt = file.ReadInt32();
                for (int j=0; j < cnt; j++)
                {
                    var sKey = file.ReadInt32();
                    var varSName = file.ReadString();
                    
                    SoundNames[key][sKey] = varSName;
                }
            }
        }

        public void Write(bgWriter file)
        {
            file.Write(NAME);
            file.Write(0); // Version
            file.Write(1); // Section count
            file.SavePosition("SECT1_OPEN");
            file.Write(0); // Offset, section 1
            file.Pad();


            var w = (int)file.BaseStream.Position;
            file.PushAnchor();
            file.GoPosition("SECT1_OPEN");
            file.Write(w);
            file.PopAnchor();
            file.Write(SoundNames.Count);
            foreach (KeyValuePair<int, Dictionary<int, string>> KVP in SoundNames)
            {
                var catName = $"{KVP.Key}";
                if (CategoryNames.ContainsKey(KVP.Key))
                    catName = CategoryNames[KVP.Key];
                
                file.Write(KVP.Key);
                file.Write(catName);
                file.Write(KVP.Value.Count);
                foreach (KeyValuePair<int,string> sndNam in KVP.Value)
                {
                    file.Write(sndNam.Key);
                    file.Write(sndNam.Value);
                }
            }
        }
    }
}
