using SerialEasy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runner
{
    public class Xy
    {
        [BinaryFieldInfo(0, 1)]
        public int x1;
        [BinaryFieldInfo(1, 1)]
        public int y1;
        [BinaryFieldInfo(2, 1)]
        public int x2;
        [BinaryFieldInfo(3, 1)]
        public int y2;

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3}", x1, y1, x2, y2);
        }
    }
}
