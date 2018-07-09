using System;

namespace SS14.Noise
{
    static class EntryPoint
    {
        public static void Main()
        {
            using (var gc = new GameController())
            {
                gc.Run(30);
            }
        }
    }
}