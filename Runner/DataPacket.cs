using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SerialEasy;

namespace Runner
{
    public class DataPacket
    {
        [BinaryFieldInfo(0, 2, true)]
        public ushort timestamp;

        [BinaryFieldInfo(2, 2, true, true)]
        public short gyroX;

        [BinaryFieldInfo(4, 2, true, true)]
        public short gyroY;

        [BinaryFieldInfo(6, 2, true, true)]
        public short gyroZ;

        [BinaryFieldInfo(8, 3, true, true)]
        public int forceLeft;

        [BinaryFieldInfo(11, 3, true, true)]
        public int forceRight;

        [BinaryFieldInfo(14, 2, true, true)]
        public short temperature;

        public override string ToString()
        {
            return string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", timestamp, gyroX, gyroY, gyroZ, forceLeft, forceRight, temperature);
        }
    }
}
