using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialEasy
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BinaryFieldInfo : Attribute
    {
        public readonly int pos, size;
        public readonly bool msbFirst;
        public readonly bool signed;

        public BinaryFieldInfo(int pos, int size, bool msbFirst = false, bool signed = false)
        {
            this.pos = pos;
            this.size = size;
            this.msbFirst = msbFirst;
            this.signed = signed;
        }
    }
}
