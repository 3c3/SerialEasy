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
        struct PowerPoint
        {
            // power in Watts
            public double power;

            public double rpm;

            // Nm
            public double torque;

            // sampling time in seconds
            public double time;
        }

        struct AveragedStats
        {
            public double power;
            public double torque;
            public double rpm;
            public double duration;
            public double timestamp;
        }

        private static readonly double GRAVITY = 9.81; // m^2 / s
        private static readonly double CRANK_ARM_LENGTH = 0.175; // meters

        volatile static Calibrator calibrator = new Calibrator();

        volatile static List<DataPacket> packets = new List<DataPacket>();

        volatile static List<PowerPoint> powerPoints = new List<PowerPoint>();

        // file dumpers
        volatile static StreamWriter rawDumper = null;
        volatile static StreamWriter calibDumper = null;

        // lock for dumpers - they will be accessed from diff threads
        volatile static object dumpersLock = new object();

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
                String[] parts = str.Split(' ');

                if (parts[0] == "sample")
                {
                    int samples = int.Parse(parts[1]);

                    DataPacket mean = new DataPacket();
                    DataPacket stdDev = new DataPacket();

                    MeanStdDev(samples, mean, stdDev);

                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine(mean.ToString());
                    builder.AppendLine(stdDev.ToString());

                    File.WriteAllText("meanStDev.txt", builder.ToString());
                }
                else if (parts[0] == "rec")
                {
                    lock (dumpersLock)
                    {
                        // they may still be open - close them
                        CloseDumpers();
                        OpenDumpers();
                    }
                }
                else if (parts[0] == "stop")
                {
                    lock (dumpersLock)
                    {
                        CloseDumpers();
                    }
                }
            }
            

            serialMan.Stop();
        }

        static void OpenDumpers()
        {
            const int BUFFER_SIZE = 2048;

            string filenameBase = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            rawDumper = new StreamWriter(new FileStream(filenameBase + "_raw.csv", FileMode.Create), Encoding.ASCII, BUFFER_SIZE, false);
            calibDumper = new StreamWriter(new FileStream(filenameBase + "_calib.csv", FileMode.Create), Encoding.ASCII, BUFFER_SIZE, false);

            // write header lines
            rawDumper.WriteLine("timestamp;gyroX;gyroY;gyroZ;forceLeft;temp");
            calibDumper.WriteLine("timestamp;gyroX;gyroY;gyroZ;forceLeft;temp");
        }

        static void CloseDumpers()
        {
            if (rawDumper != null)
            {
                rawDumper.Flush();
                rawDumper.Close();
                rawDumper.Dispose();
                rawDumper = null;
            }
            if (calibDumper != null)
            {
                calibDumper.Flush();
                calibDumper.Close();
                calibDumper.Dispose();
                calibDumper = null;
            }
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

        // Try to calculate the average power for the revolution that just completed
        private static bool CalcRevolutionAverage(ref AveragedStats outAverage)
        {
            // not enough points to calc average power
            if (powerPoints.Count < 10)
            {
                return false;
            }

            int idxLast = powerPoints.Count - 1;

            // check if the latest point is a zero crossing
            // inverse of (last.power < 0 && prev_to_last.power > 0)
            if (powerPoints[idxLast].power >= 0 || powerPoints[idxLast - 1].power <= 0)
            {
                // not a zero crossing
                return false;
            }

            //Console.WriteLine("Zero crossing @ {0:0.000}", powerPoints[idxLast].time);

            // it is a zero crossing
            // now find a previous zero crossing that is likely the end of the previous revolution
            // angular velocity is in rpm
            double expectedTimeToPrev = 60.0 / powerPoints[idxLast].rpm;

            // sanity check
            if (expectedTimeToPrev > 2.0)
            {
                // probably just noise
                return false;
            }
            
            // allow some tolerance in the revolution time
            const double TIME_TOLERANCE = 0.3;
            double timeThrLow = powerPoints[idxLast].time - (1.0 + TIME_TOLERANCE) * expectedTimeToPrev;
            double timeThrHigh = powerPoints[idxLast].time - (1.0 - TIME_TOLERANCE) * expectedTimeToPrev;

            //Console.WriteLine("Searching for second ZC between {0:0.000} and {1:0.000}", timeThrLow, timeThrHigh);


            int idxPrev = idxLast - 1;
            // iterate back until the time difference is big enough
            while (idxPrev > 1 && powerPoints[idxPrev].time > timeThrHigh)
            {
                //Console.WriteLine("{0}, {1:0.000}", idxPrev, powerPoints[idxLast].time);
                idxPrev--;
            }
            //Console.WriteLine("Start @ idx {0}, time {1:0.000}", idxPrev, powerPoints[idxPrev].time);

            bool foundPrev = false;
            while (idxPrev > 1 && powerPoints[idxLast].time >= timeThrLow)
            {
                //Console.WriteLine("\tCheck idx {0}, time {1:0.000}", idxPrev, powerPoints[idxPrev].time);
                if (powerPoints[idxPrev].power < 0 && powerPoints[idxPrev - 1].power >= 0)
                {
                    foundPrev = true;
                    break;
                }
                idxPrev--;
            }

            if (!foundPrev)
            {
                return false;
            }

            // we have found two zero crossing at an interval that is **close** to the revolution interval
            // to find the average power, we should integrate the power values in the range [idxPrev, idxLast]
            outAverage = CalculateAverage(idxPrev, idxLast);

            return true;
        }

        // Average stats for indices [start, end] by integrating with trapezoid method
        private static AveragedStats CalculateAverage(int startIdx, int endIdx)
        {
            AveragedStats result;
            result.power = 0;
            result.rpm = 0;
            result.torque = 0;
            result.duration = powerPoints[endIdx].time - powerPoints[startIdx].time;
            result.timestamp = powerPoints[endIdx].time;

            // integrate using trapezoid method
            for (int i = startIdx; i < endIdx - 1; i++)
            {
                PowerPoint low = powerPoints[i];
                PowerPoint high = powerPoints[i + 1];
                double deltaTime_2 = 0.5 * (high.time - low.time);

                result.power += deltaTime_2 * (high.power + low.power);
                result.rpm += deltaTime_2 * (high.rpm + low.rpm);
                result.torque += deltaTime_2 * (high.torque + low.torque);
            }

            result.power /= result.duration;
            result.torque /= result.duration;
            result.rpm /= result.duration;

            return result;
        }

        private static void HandlePacket(DataPacket packet)
        {
            packets.Add(packet);

            DataPoint dataPoint = calibrator.CalibratePacket(packet);

            // calculate total rotation - assume that the only rotation is around the bottom bracket axis
            // this will be incorrect if the bike is turning or is shaking left to right (for example during sprints)
            // TODO also add sign - currently it is always positive
            // rad / s
            double totalRotation = Math.Sqrt(dataPoint.angularVelocityX * dataPoint.angularVelocityX + dataPoint.angularVelocityY * dataPoint.angularVelocityY + dataPoint.angularVelocityZ * dataPoint.angularVelocityZ);

            double torque = dataPoint.forceLeft * GRAVITY * CRANK_ARM_LENGTH; // Newtonmeters

            PowerPoint powerPoint;
            powerPoint.power = totalRotation * torque;
            powerPoint.time = dataPoint.timestampUs / 1000000.0;
            powerPoint.rpm = totalRotation * 60 / (2 * Math.PI);
            powerPoint.torque = torque;

            powerPoints.Add(powerPoint);

            AveragedStats revolutionStats;
            revolutionStats.power = 0;
            revolutionStats.torque = 0;
            revolutionStats.rpm = 0;
            revolutionStats.duration = 0;
            revolutionStats.timestamp = 0;
            bool ok = CalcRevolutionAverage(ref revolutionStats);
            if (ok)
            {
                Console.WriteLine("{0:0.0} W\t{1:0.00} rpm\t{2:0.00} Nm\t{3:0.000} s", revolutionStats.power, revolutionStats.rpm, revolutionStats.torque, revolutionStats.duration);
            }

            //Console.WriteLine(dataPoint);

            lock (dumpersLock)
            {
                if (rawDumper != null)
                {
                    rawDumper.WriteLine(string.Format("{0};{1};{2};{3};{4};{5}", packet.timestamp, packet.gyroX, packet.gyroY, packet.gyroZ,
                                                                                 packet.forceLeft, packet.temperature));
                }

                if (calibDumper != null)
                {
                    calibDumper.WriteLine(string.Format("{0};{1};{2};{3};{4};{5}", dataPoint.timestampUs, dataPoint.angularVelocityX, dataPoint.angularVelocityY,
                                                                                   dataPoint.angularVelocityZ, dataPoint.forceLeft, dataPoint.temperature));
                }
            }
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
