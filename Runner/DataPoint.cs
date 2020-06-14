using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runner
{
    // calibrated measurement data
    public class DataPoint
    {
        public ulong timestampUs;

        // rad / sec
        public double angularVelocityX;

        // rad / sec
        public double angularVelocityY;

        // rad / sec
        public double angularVelocityZ;

        // Newtons
        public double forceLeft;

        // Newtons
        public double forceRight;

        // degrees Celcius
        public double temperature;

        public override string ToString()
        {
            const double RAD_TO_DEG = 180.0 / Math.PI;
            return String.Format("{0:0.000}\t{1:0.00}\t{2:0.00}\t{3:0.00}\t{4:0.00}\t{5:0.00}", timestampUs / 1000000.0, angularVelocityX * RAD_TO_DEG, angularVelocityY * RAD_TO_DEG, angularVelocityZ * RAD_TO_DEG,
                                  temperature, forceLeft); 
        }
    }
}
