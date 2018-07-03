using System;

namespace SS14.Noise
{
    static class EntryPoint
    {
        public static void Main()
        {
            Console.WriteLine("hello world!");

            using (var gc = new GameController())
            {
                gc.Run(30);
            }
        }
    }
}