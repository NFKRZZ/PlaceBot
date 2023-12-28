using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
namespace PlaceBot2._0
{
    class Program
    {
        static void Main(string[] args)
        {
            int thread_delay = 2501;
            //THESE COORDS ARE WHAT YOU SEE ON r/ place in browser
            int startX = 1300;//1300 or 1277
            int startY = -6;//-6
            string userfilePath = "accounts.txt"; //create your own
            string proxyFilePath = "proxy.txt"; //create your own
            List<string[]> userList = getUser(userfilePath);
            List<string> proxyList = getProxy(proxyFilePath);
            List<RedditPlaceWorker> botlist = new List<RedditPlaceWorker>();
            string imagePath = "smallaf.png";//smallaf
            Bitmap bitmap = new Bitmap(imagePath);
            int i = 0;
            int k = 0;
            foreach (string[] a in userList)
            {
                if (k >= proxyList.Count - 1)
                {
                    k = 0;
                }
                botlist.Add(new RedditPlaceWorker(i, a[0], a[1], proxyList[k], bitmap, startX, startY));
                k++;
                i++;
            }
            Console.WriteLine("Botlist Size = " + botlist.Count);
            List<Thread> threadlist = new List<Thread>();
            foreach (RedditPlaceWorker worker in botlist)
            {
                Thread t = new Thread(() => worker.Init());
                t.Start();
                threadlist.Add(t);
                Thread.Sleep(new Random().Next(1500,thread_delay));
            }
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("FINISHED LOGGING INTO ALL THE REDDIT ACCOUNTS!!!!!!!!!!!!!!!!!!!!!!!!!");
            Console.WriteLine("FINISHED LOGGING INTO ALL THE REDDIT ACCOUNTS!!!!!!!!!!!!!!!!!!!!!!!!!");
            Console.WriteLine("FINISHED LOGGING INTO ALL THE REDDIT ACCOUNTS!!!!!!!!!!!!!!!!!!!!!!!!!");
            Console.WriteLine("FINISHED LOGGING INTO ALL THE REDDIT ACCOUNTS!!!!!!!!!!!!!!!!!!!!!!!!!");
            Console.WriteLine("FINISHED LOGGING INTO ALL THE REDDIT ACCOUNTS!!!!!!!!!!!!!!!!!!!!!!!!!");
            Console.WriteLine("FINISHED LOGGING INTO ALL THE REDDIT ACCOUNTS!!!!!!!!!!!!!!!!!!!!!!!!!");
            Console.WriteLine("FINISHED LOGGING INTO ALL THE REDDIT ACCOUNTS!!!!!!!!!!!!!!!!!!!!!!!!!");
            Console.ForegroundColor = ConsoleColor.Gray;
            Thread.Sleep(10000);
            List<string> bannedAccounts = new List<string>();
            bool fin = false;
            while (true)
            {
                if (fin is false)
                {
                    foreach (RedditPlaceWorker bot in botlist)
                    {
                        if (bot.isThreadBanned() && !bot.getBan())
                        {
                            try
                            {
                                int index = bot.getIndex();
                                //Console.WriteLine("Aborted Thread: #" + index + " user = " + bot.getName() + " due to rateLimit");
                                //threadlist[index].Abort();
                                Console.WriteLine("Aborted");
                                bot.setBan(true);
                                bannedAccounts.Add(bot.getName());
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.ToString());
                            }

                        }
                        Thread.Sleep(400);

                    }

                    using(StreamWriter writer = new StreamWriter("banned.txt"))
                    {
                        foreach (string s in bannedAccounts)
                        {
                            writer.WriteLine(s);
                        }
                    }
                    fin = true;

                }
                Thread.Sleep(500);
            }


        }

        static List<string[]> getUser(string filePath)
        {
             

            // Initialize the list to store username and password pairs
            List<string[]> userList = new List<string[]>();

            // Read all lines from the text file
            string[] lines = File.ReadAllLines(filePath);

            // Split each line into username and password and add to the list
            foreach (string line in lines)
            {
                string[] parts = line.Split(':');
                if (parts.Length == 2)
                {
                    string username = parts[0].Trim();
                    string password = parts[1].Trim();
                    userList.Add(new string[] { username, password });
                }
                else
                {
                    Console.WriteLine($"Invalid format on line: {line}");
                }
            }
            return userList;
        }
        static List<string> getProxy(string filePath)
        {
            List<string> proxiesList = new List<string>();

            string[] lines = File.ReadAllLines(filePath);
           
            foreach (string line in lines)
            {
                string proxy = $"http://{line.Trim()}";
                proxiesList.Add(proxy);
            }

            return proxiesList;
        }

    }
}
