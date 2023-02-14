using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xayrga;
using xayrga.byteglider;

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

        private void loadFromStream(bgReader reader)
        {
            var origPos = reader.BaseStream.Position;
            int count = 0;
            while (reader.ReadUInt16BE() < 0xB)
            {
                reader.ReadUInt32BE();
                count++;
            }

            count++;
            reader.BaseStream.Position = origPos;
            points = new JEnvelopeVector[count];
            for (int i = 0; i < count; i++)
                points[i] = new JEnvelopeVector { Mode = reader.ReadUInt16BE(), Delay = reader.ReadUInt16BE(), Value = reader.ReadInt16BE() };
        }
        public static JInstrumentEnvelopev1 CreateFromStream(bgReader reader)
        {
            var b = new JInstrumentEnvelopev1();
            b.loadFromStream(reader);
            return b;
        }
        public void WriteToStream(bgWriter wr)
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
                wr.Pad();

        }
    }
}
