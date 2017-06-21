using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;

namespace SerialEasy
{
    public delegate void SerialObjectReceivedHandler(object obj);

    public class SerialManager
    {
        public event SerialObjectReceivedHandler SerialObjectReceived;

        SerialPort port;
        Thread serialThread;

        ClassInfo singleClass;
        ClassInfo[] classes = new ClassInfo[256];
        bool useSingleClass;

        byte[] header;

        public SerialManager(byte[] header, Type classType)
        {
            this.header = header;
            singleClass = new ClassInfo(classType);
            useSingleClass = true;
        }

        public SerialManager(byte[] header)
        {
            this.header = header;
        }

        public void AddClass(int id, Type classType)
        {
            classes[id] = new ClassInfo(classType);
        }

        public bool OpenPort(string name, int baud)
        {
            port = new SerialPort(name, baud);
            try
            {
                port.Open();
            }
            catch (Exception e)
            {
                return false;
            }

            serialThread = new Thread(new ThreadStart(Loop));
            serialThread.Start();

            return true;
        }

        void Loop()
        {
            byte[] buff = new byte[1024];
            while (true)
            {
                while (port.BytesToRead < header.Length) ;
                port.Read(buff, 0, header.Length);
                if (!CheckHeader(buff)) continue;

                if (useSingleClass)
                {
                    while (port.BytesToRead < singleClass.Size) ;
                    port.Read(buff, 0, singleClass.Size);

                    Object obj = BinaryParser.Parse(singleClass.ClassType, buff, 0);
                    SerialObjectReceived?.Invoke(obj);
                }
                else
                {
                    byte classId = 0;
                    while (port.BytesToRead < 1) ;
                    port.Read(buff, 0, 1);
                    classId = buff[0];

                    ClassInfo ci = classes[classId];
                    if (ci == null) continue;

                    while (port.BytesToRead < ci.Size) ;
                    port.Read(buff, 0, ci.Size);

                    Object obj = BinaryParser.Parse(ci.ClassType, buff, 0);
                    SerialObjectReceived?.Invoke(obj);
                }
            }
        }

        bool CheckHeader(byte[] buff)
        {
            for (int i = 0; i < header.Length; i++)
                if (header[i] != buff[i]) return false;
            return true;
        }
    }
}
