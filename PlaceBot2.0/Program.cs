using System;
using System.Collections.Generic;
using System.Threading;
namespace PlaceBot2._0
{
    class Program
    {
        static void Main(string[] args)
        {

            List<RedditPlaceWorker> botlist = new List<RedditPlaceWorker>();
            botlist.Add(new RedditPlaceWorker(1, "Kings55328", "Kthenurse.123", "45.9.16.138:5136"));
            Console.WriteLine("Botlist Size = "+botlist.Count);
            List<Thread> threadlist = new List<Thread>();
            foreach(RedditPlaceWorker b in botlist)
            {
                Thread t = new Thread(() => b.Init());
                threadlist.Add(t);
                t.Start();
            }

        }
    }
}
