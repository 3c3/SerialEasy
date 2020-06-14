using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runner
{
    public class Calibrator
    {
        private static readonly uint F_CPU = 8000000;
        private static readonly uint PRESCALER = 1024;
        private static readonly uint US_PER_TICK = PRESCALER * 1000000 / F_CPU;

        private static readonly int GYRO_OFFSET_X = -128;
        private static readonly int GYRO_OFFSET_Y = 17;
        private static readonly int GYRO_OFFSET_Z = 292;
        private static readonly int GYRO_RANGE_DEG = 2000;
        private static readonly double GYRO_DEG_CONST = (double)GYRO_RANGE_DEG / ((1 << 15) - 1);

        private static readonly int FORCE_LEFT_OFFSET = 356235;

        // conversion constant to newtons
        private static readonly double FORCE_LEFT_CONSTANT = 5.0 / (393280 - FORCE_LEFT_OFFSET);

        // temperature
        private static readonly int TEMPERATURE_OFFSET = -12420;
        private static readonly double TEMPERATURE_SCALE = 1.0 / 340;

        private ulong previuosTimestampUs = 0;
        private int previousTimestampTick = -1;

        public DataPoint CalibratePacket(DataPacket packet)
        {
            DataPoint result = new DataPoint();

            // timestamp
            if (previousTimestampTick == -1)
            {
                // this is the first packet -> time is zero
                result.timestampUs = 0;
            }
            else
            {
                // there is a previous packet
                uint tPacket = packet.timestamp;
                if (tPacket < previousTimestampTick)
                {
                    // timer has overflowed, assume it has happened just once
                    tPacket += 1 << 16; // timer is 16 bit
                }

                ulong elapsedTicks = tPacket - (uint)previousTimestampTick;
                result.timestampUs = previuosTimestampUs + elapsedTicks * US_PER_TICK;
            }

            previousTimestampTick = packet.timestamp;
            previuosTimestampUs = result.timestampUs;

            // gyro
            const double DEG_TO_RAD = Math.PI / 180.0;
            result.angularVelocityX = (packet.gyroX - GYRO_OFFSET_X) * GYRO_DEG_CONST * DEG_TO_RAD;
            result.angularVelocityY = (packet.gyroY - GYRO_OFFSET_Y) * GYRO_DEG_CONST * DEG_TO_RAD;
            result.angularVelocityZ = (packet.gyroZ - GYRO_OFFSET_Z) * GYRO_DEG_CONST * DEG_TO_RAD;

            // temperature
            result.temperature = (packet.temperature - TEMPERATURE_OFFSET) * TEMPERATURE_SCALE;

            result.forceLeft = (packet.forceLeft - FORCE_LEFT_OFFSET) * FORCE_LEFT_CONSTANT;

            return result;
        }
    }
}
