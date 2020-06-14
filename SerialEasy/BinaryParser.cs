using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace SerialEasy
{
    public class BinaryParser
    {
        public static object Parse(Type type, byte[] buff, int offset)
        {
            Object obj = Activator.CreateInstance(type);

            FieldInfo[] fis = type.GetFields();

            foreach (var fi in fis)
            {
                BinaryFieldInfo bfi = fi.GetCustomAttribute(typeof(BinaryFieldInfo)) as BinaryFieldInfo;
                fi.SetValue(obj, ParseNum(bfi, buff, offset));
            }

            return obj;
        }

        private static object ParseNum(BinaryFieldInfo bfi, byte[] buff, int offset)
        {
            Object num = null;
            int start = bfi.pos + offset;

            if (bfi.signed)
            {
                if (bfi.size == 1)
                {
                    num = (sbyte)buff[start];
                }
                else if (bfi.size == 2)
                {
                    if (bfi.msbFirst) num = (short)(buff[start] << 8 | buff[start + 1]);
                    else num = (short)(buff[start + 1] << 8 | buff[start]);
                }
                else if (bfi.size == 3)
                {
                    if (bfi.msbFirst) num = (buff[start] >= 128 ? 255 : 0) << 24 | buff[start] << 16 | buff[start + 1] << 8 | buff[start + 2];
                    else num = (buff[start + 2] >= 128 ? 255 : 0) << 24 | buff[start + 2] << 16 | buff[start + 1] << 8 | buff[start];
                }
                else if (bfi.size == 4)
                {
                    if (bfi.msbFirst) num = buff[start] << 24 | buff[start + 1] << 16 | buff[start + 2] << 8 | buff[start + 3];
                    else num = buff[start + 3] << 24 | buff[start + 2] << 16 | buff[start + 1] << 8 | buff[start];
                }
            }
            else
            {
                if (bfi.size == 1)
                {
                    num = buff[start];
                }
                else if (bfi.size == 2)
                {
                    if (bfi.msbFirst) num = (ushort)(buff[start] << 8 | buff[start + 1]);
                    else num = (ushort)(buff[start + 1] << 8 | buff[start]);
                }
                else if (bfi.size == 3)
                {
                    if (bfi.msbFirst) num = buff[start] << 16 | buff[start + 1] << 8 | buff[start + 2];
                    else num = buff[start + 2] << 16 | buff[start + 1] << 8 | buff[start];
                }
                else if (bfi.size == 4)
                {
                    if (bfi.msbFirst) num = (uint)(buff[start] << 24 | buff[start + 1] << 16 | buff[start + 2] << 8 | buff[start + 3]);
                    else num = (uint)(buff[start + 3] << 24 | buff[start + 2] << 16 | buff[start + 1] << 8 | buff[start]);
                }
            }

            return num;
        }
    }
}
