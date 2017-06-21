using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SerialEasy;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            SerialManager serialMan = new SerialManager(new byte[] { 0xDE, 0xAD }, typeof(Xy));
            serialMan.SerialObjectReceived += SerialMan_SerialObjectReceived;

            while (true)
            {
                string[] parts = Console.ReadLine().Split(' ');
                if (serialMan.OpenPort(parts[0], int.Parse(parts[1]))) break;
            }

            while (true) ;
        }

        private static void SerialMan_SerialObjectReceived(object obj)
        {
            Console.WriteLine(obj);
        }
    }
}
