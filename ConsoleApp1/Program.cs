using System;
using System.Runtime.InteropServices;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(RuntimeInformation.OSArchitecture.ToString());
            Console.WriteLine(RuntimeInformation.OSDescription);
            Console.WriteLine(RuntimeInformation.FrameworkDescription);
            var input = string.Empty;
            while ("q" != (input = Console.ReadLine()))
            {
                var now = DateTime.Now;
                Console.WriteLine($"Just Input: [{input}],{now:yyyy-mm-dd HH:mm:ss.fff}", input, now);
            }
        }
    }
}
