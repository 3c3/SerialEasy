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
        byte[] headerCheck;
        int headerBytesRead = 0;

        public SerialManager(byte[] header, Type classType)
        {
            this.header = new byte[header.Length];
            headerCheck = new byte[header.Length];

            Buffer.BlockCopy(header, 0, this.header, 0, header.Length);

            singleClass = new ClassInfo(classType);
            useSingleClass = true;
        }

        public SerialManager(byte[] header)
        {
            this.header = new byte[header.Length];
            headerCheck = new byte[header.Length];

            Buffer.BlockCopy(header, 0, this.header, 0, header.Length);
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

        public void Stop()
        {
            serialThread.Abort();
        }

        void Loop()
        {
            byte[] buff = new byte[1024];
            while (true)
            {
                int headerBytesLeft = header.Length - headerBytesRead;
                while (port.BytesToRead < headerBytesLeft) ;
                port.Read(headerCheck, headerBytesRead, headerBytesLeft);

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
            bool kay = true;
            for (int i = 0; i < header.Length; i++)
                if (header[i] != headerCheck[i])
                {
                    kay = false;
                    break;
                }

            if (!kay)
            {
                for (int i = 0; i < header.Length - 1; i++)
                    headerCheck[i] = headerCheck[i + 1];
                headerBytesRead = header.Length - 1;
            }
            else headerBytesRead = 0;

            return kay;
        }
    }
}
