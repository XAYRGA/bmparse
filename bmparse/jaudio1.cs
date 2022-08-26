using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.IO;

namespace bmparse
{
    public class JInstrumentEnvelopev1
    {

        public JEnvelopeVector[] points;
        public class JEnvelopeVector
        {
            public ushort Mode;
            public ushort Delay;
            public short Value;
        }

        private void loadFromStream(BeBinaryReader reader)
        {
            var origPos = reader.BaseStream.Position;
            int count = 0;
            while (reader.ReadUInt16() < 0xB)
            {
                reader.ReadUInt32();
                count++;
            }
            count++;
            reader.BaseStream.Position = origPos;
            points = new JEnvelopeVector[count];
            for (int i = 0; i < count; i++)
                points[i] = new JEnvelopeVector { Mode = reader.ReadUInt16(), Delay = reader.ReadUInt16(), Value = reader.ReadInt16() };
        }
        public static JInstrumentEnvelopev1 CreateFromStream(BeBinaryReader reader)
        {
            var b = new JInstrumentEnvelopev1();
            b.loadFromStream(reader);
            return b;
        }
        public void WriteToStream(BeBinaryWriter wr)
        {
            var remainingLength = 32;
            for (int i = 0; i < points.Length; i++)
            {
                remainingLength -= 6;
                wr.Write(points[i].Mode);
                wr.Write(points[i].Delay);
                wr.Write(points[i].Value);
            }
            if (remainingLength > 0)
                wr.Write(new byte[remainingLength]);
            else
                util.padTo(wr, 32);

        }
    }
}
