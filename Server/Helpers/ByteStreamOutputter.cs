using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Helpers
{
    class ByteStreamOutputter
    {
        private byte[] arr;

        public ByteStreamOutputter()
        {

        }
        
        public static void Print(byte[] arr)
        {
            Console.WriteLine($"Byte arr len: {arr.Length}\n");
            
            foreach(var b in arr)
            {
                Console.Write(b + "\t");
            }
        }

        public static void PrintString(byte[] arr)
        {
            string str = Encoding.Default.GetString(arr);

            Console.WriteLine(str);
        }
    }
}
