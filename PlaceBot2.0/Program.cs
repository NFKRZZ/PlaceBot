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

            //THESE COORDS ARE WHAT YOU SEE ON r/place in browser
            int startX = 0;
            int startY = 0;
            string userfilePath = "PUT PATH HERE";
            string proxyFilePath = "PUT PATH HERE";
            //List<string[]> userList = getUser(userfilePath);
            //List<string> proxyList = getProxy(proxyFilePath);
            List<RedditPlaceWorker> botlist = new List<RedditPlaceWorker>();
            string imagePath = "image.jpg";
            Bitmap bitmap = new Bitmap(imagePath);
            botlist.Add(new RedditPlaceWorker(1, "Kings55328", "Kthenurse.123", "45.9.16.138:5136",bitmap,startX,startY));
            Console.WriteLine("Botlist Size = "+botlist.Count);
            List<Thread> threadlist = new List<Thread>();
            botlist[0].Init();

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
