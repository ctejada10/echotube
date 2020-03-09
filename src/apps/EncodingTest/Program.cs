using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EchoTube.Net;

namespace EncodingTest
{
    class Program
    {
        static void PrintArray(byte[] array, string name)
        {
            Console.Write(name + ": ");
            for (int i = 0; i < array.Length; i++)
            {
                Console.Write(array[i] + "  ");
            }
            Console.WriteLine();
        }

        static void Main(string[] args)
        {
            byte[] before = { 0, 12, 0, 13, 0, 14 };
            byte[] after = COBSEncoding.Encode(before);
            byte[] final = COBSEncoding.Decode(after);

            PrintArray(before, "BEFORE");
            PrintArray(after, " AFTER");
            PrintArray(final, " FINAL");

            Console.WriteLine("--------------------------------------------");

            byte[] veryLongArray = new byte[10000];
            for (int i = 0; i < veryLongArray.Length; i++)
            {
                veryLongArray[i] = (byte)(255 * (new Random().NextDouble()));
            }

            byte[] encoded = COBSEncoding.Encode(veryLongArray);
            byte[] decoded = COBSEncoding.Encode(encoded);

            Console.WriteLine("BEFORE: " + veryLongArray.Length);
            Console.WriteLine(" AFTER: " + encoded.Length);
            Console.WriteLine(" FINAL: " + decoded.Length);

            Console.WriteLine("--------------------------------------------");

            Udp.Receiver receiver = new Udp.Receiver();
            receiver.PacketReceived += ReceiverPacketReceived;
            receiver.BytesReceived += ReceiverBytesReceived;
            receiver.Start();

            Udp.Sender sender = new Udp.Sender();

            List<byte[]> packets = new List<byte[]>();
            for (int i = 0; i < 100; i++)
            {
                // packets.Add(before);
                sender.Send(before);
            }

            // sender.Send(packets);

            Console.WriteLine("Press any key to continue ...");
            Console.ReadKey();
        }

        private static void ReceiverBytesReceived(object sender, byte[] bytes)
        {
            Debug.WriteLine("Bytes: " + bytes.Length);
        }

        static int counter = 0;

        private static void ReceiverPacketReceived(object sender, byte[] bytes)
        {
            PrintArray(bytes, "RECEIVED #" + (counter++));
            Console.WriteLine("--------------------------------------------");
        }
    }
}
