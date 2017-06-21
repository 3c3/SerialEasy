using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace SerialEasy
{
    public class ClassInfo
    {
        Type t;
        int size;

        public Type ClassType
        {
            get { return t; }
        }

        public int Size
        {
            get { return size; }
        }

        public ClassInfo(Type type)
        {
            t = type;

            FieldInfo[] fis = type.GetFields();
            int maxSize = 0;
            foreach (var fi in fis)
            {
                BinaryFieldInfo bfi = fi.GetCustomAttribute(typeof(BinaryFieldInfo)) as BinaryFieldInfo;
                int cand = bfi.pos + bfi.size;
                if (cand > maxSize) maxSize = cand;
            }
            size = maxSize;
        }
    }
}
