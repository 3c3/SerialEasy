using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SerialEasy;
using System.Threading;
using System.Collections;
using System.IO;

namespace Runner
{
    class Program
    {
        volatile static Calibrator calibrator = new Calibrator();

        volatile static List<DataPacket> packets = new List<DataPacket>();

        static void Main(string[] args)
        {
            SerialManager serialMan = new SerialManager(new byte[] { 0xAA, 0xAA });
            serialMan.AddClass(0, typeof(DataPacket));
            serialMan.SerialObjectReceived += SerialMan_SerialObjectReceived;

            while (true)
            {
                string[] parts = Console.ReadLine().Split(' ');
                if (serialMan.OpenPort(parts[0].ToUpper(), int.Parse(parts[1]))) break;
            }

            while (true)
            {
                String str = Console.ReadLine();
                int samples = int.Parse(str);

                DataPacket mean = new DataPacket();
                DataPacket stdDev = new DataPacket();

                MeanStdDev(samples, mean, stdDev);

                StringBuilder builder = new StringBuilder();
                builder.AppendLine(mean.ToString());
                builder.AppendLine(stdDev.ToString());

                File.WriteAllText("meanStDev.txt", builder.ToString());
            }
            

            serialMan.Stop();
        }

        private static void MeanStdDev(int samples, DataPacket outMean, DataPacket outStdDev)
        {
            // asign now - packets may be added by callback while calculating
            int endIdx = packets.Count();
            int startIdx = endIdx - samples;
            if (startIdx < 0)
            {
                startIdx = 0;
            }
            samples = endIdx - startIdx;

            long forceLeftSum = 0;
            long gyroXSum = 0;
            long gyroYSum = 0;
            long gyroZSum = 0;
            long tempSum = 0;

            // calc means
            for (int i = startIdx; i < endIdx; i++)
            {
                DataPacket packet = packets[i];

                forceLeftSum += packet.forceLeft;
                gyroXSum += packet.gyroX;
                gyroYSum += packet.gyroY;
                gyroZSum += packet.gyroZ;
                tempSum += packet.temperature;
            }

            outMean.forceLeft = (int)(forceLeftSum / samples);
            outMean.gyroX = (short)(gyroXSum / samples);
            outMean.gyroY = (short)(gyroYSum / samples);
            outMean.gyroZ = (short)(gyroZSum / samples);
            outMean.temperature = (short)(tempSum / samples);

            long forceLeftSqSum = 0;
            long gyroXSqSum = 0;
            long gyroYSqSum = 0;
            long gyroZSqSum = 0;
            long tempSqSum = 0;

            for (int i = startIdx; i < endIdx; i++)
            {
                DataPacket packet = packets[i];

                long err = packet.forceLeft - outMean.forceLeft;
                forceLeftSqSum += err * err;

                err = packet.gyroX - outMean.gyroX;
                gyroXSqSum += err * err;

                err = packet.gyroY - outMean.gyroY;
                gyroYSqSum += err * err;

                err = packet.gyroZ - outMean.gyroZ;
                gyroZSqSum += err * err;

                err = packet.temperature - outMean.temperature;
                tempSqSum += err * err;
            }

            outStdDev.forceLeft = (int)Math.Sqrt(forceLeftSqSum / samples);
            outStdDev.gyroX = (short)Math.Sqrt(gyroXSqSum / samples);
            outStdDev.gyroY = (short)Math.Sqrt(gyroYSqSum / samples);
            outStdDev.gyroZ = (short)Math.Sqrt(gyroZSqSum / samples);
            outStdDev.temperature = (short)Math.Sqrt(tempSqSum / samples);
        }

        private static void HandlePacket(DataPacket packet)
        {
            packets.Add(packet);

            DataPoint dataPoint = calibrator.CalibratePacket(packet);
            Console.WriteLine(dataPoint);
        }

        private static void SerialMan_SerialObjectReceived(object obj)
        {
            if (obj is DataPacket)
            {
                DataPacket packet = obj as DataPacket;
                HandlePacket(packet);
            }
            else
            {
                Console.WriteLine(obj);
            }
        }
    }
}
