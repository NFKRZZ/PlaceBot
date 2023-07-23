using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace PlaceBot2._0
{
    public class RedditPlaceWorker
    {
        private string proxy;
        private SemaphoreSlim semaphore;
        private bool canvasConfigReceived;
        private WebSocket ws;
        private List<Tuple<int, Bitmap>> imgs;
        private JObject canvas_details;
        private List<int> canvas_sockets;
        private HttpClient client;
        private HttpClientHandler handler;
        private long access_token_expires_at_timestamp = 0;
        private bool repeatForever = true;
        private bool unverifiedPlaceFrequency = false;
        private readonly int pixelPlaceFrequency;
        private bool firstTime = true;
        private int nextPixelPlacementTime;
        private int currentR;
        private int currentC;
        private readonly int index;
        private readonly string name;
        private readonly string password;
        private readonly string accessToken;
        private readonly Dictionary<int, int> accessTokenExpiresAtTimestamp;
        private readonly Dictionary<int, int[]> pixelMap;
        private int firstRunCounter;
        private readonly int[] imageCoords;
        private readonly int[] startCoords;

        public RedditPlaceWorker(int index, string name, string password,string proxy)
        {
            this.index = index;
            this.name = name;
            this.password = password;
            this.accessTokenExpiresAtTimestamp = new Dictionary<int, int>();
            this.proxy = proxy;
            this.pixelPlaceFrequency = unverifiedPlaceFrequency ? 1230 : 330;
            this.nextPixelPlacementTime = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds + this.pixelPlaceFrequency;

        }

        public void Init()
        {

            handler = new HttpClientHandler
            {
                Proxy = new WebProxy("http://45.9.16.138:5136"), // Replace "proxy-ip" and "proxy-port" with the actual proxy IP and port
                UseProxy = true
            };
            client = new HttpClient(handler);
            Login(name, password);
            Bitmap p =  getBoard(accessToken).Result;
            p.Save("hello.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
        }


        void Login(string username, string password)
        {
            while (true)
            {
                try
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.84 Safari/537.36");
                    client.DefaultRequestHeaders.Add("sec-ch-ua", "Not.A/Brand\";v=\"8\", \"Chromium\";v=\"114\", \"Google Chrome\";v=\"114\"");

                    // Access the Reddit website to get CSRF token
                    client.GetAsync("https://www.reddit.com").Wait();

                    // Access the login page to obtain the CSRF token
                    var loginPageResponse = client.GetAsync("https://www.reddit.com/login").Result;
                    var loginPageContent = loginPageResponse.Content.ReadAsStringAsync().Result;
                    var loginPageDocument = new HtmlDocument();
                    loginPageDocument.LoadHtml(loginPageContent);
                    var csrfTokenNode = loginPageDocument.DocumentNode.SelectSingleNode("//input[@name='csrf_token']");
                    string csrfToken = csrfTokenNode.GetAttributeValue("value", "");

                    // Check if password contains OTP
                    string otp = "";
                    if (password.Contains(":"))
                    {
                        otp = password.Split(":")[1];
                        password = password.Split(":")[0];
                    }

                    // Prepare login data
                    var loginData = new FormUrlEncodedContent(new[]
                    {
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("password", password),
                    new KeyValuePair<string, string>("dest", "https://new.reddit.com/"),
                    new KeyValuePair<string, string>("csrf_token", csrfToken),
                    new KeyValuePair<string, string>("otp", otp),
                });

                    // Post login data to authenticate using the proxy
                    var loginPostResponse = client.PostAsync("https://www.reddit.com/login", loginData).Result;
                    var loginPostContent = loginPostResponse.Content.ReadAsStringAsync().Result;

                    if (loginPostResponse.StatusCode != HttpStatusCode.OK)
                    {
                        // Password is probably invalid
                        Console.WriteLine($"{username} - Authorization failed!");
                        Console.WriteLine($"Response: {(int)loginPostResponse.StatusCode} - {loginPostContent}");
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"{username} - Authorization successful!");
                    }

                    // Obtain access token
                    Console.WriteLine("Obtaining access token...");
                    var accessTokenResponse = client.GetAsync("https://new.reddit.com/").Result;
                    var accessTokenContent = accessTokenResponse.Content.ReadAsStringAsync().Result;
                    //File.WriteAllText("access_token_response.txt", accessTokenContent);

                    string startTag = "<script id=\"data\">window.___r = ";
                    string endTag = ";</script>";


                    // var accessTokenStartIndex = accessTokenContent.IndexOf("window.__r = ") + "window.__r = ".Length;
                    // var accessTokenEndIndex = accessTokenContent.LastIndexOf(';');

                    int accessTokenStartIndex = accessTokenContent.IndexOf(startTag);
                    int accessTokenEndIndex = accessTokenContent.IndexOf(endTag, accessTokenStartIndex);
                    Console.WriteLine("THIS IS START: " + accessTokenStartIndex + " THIS IS END " + accessTokenEndIndex);
                    if (accessTokenStartIndex != -1 && accessTokenEndIndex != -1)
                    {
                       // Console.WriteLine("Great!");
                    }
                    else
                    {
                        throw new Exception("BIG BAD");
                    }
                        accessTokenStartIndex += startTag.Length;
                        string accessTokenJson = accessTokenContent.Substring(accessTokenStartIndex, accessTokenEndIndex - accessTokenStartIndex);
                    //File.WriteAllText("big.txt", accessTokenJson);
                        // Now you have the JSON data as a string, you can parse it into a JSON object
                        dynamic accessTokenData = JsonConvert.DeserializeObject<dynamic>(accessTokenJson);
                        var response_data = accessTokenData.user.session;
                    
                    if (response_data.error != null)
                    {
                        Console.WriteLine($"An error occurred. Make sure you have the correct credentials. Response data: {response_data}");
                        return;
                    }

                    string accessToken = response_data.accessToken;
                    // Do whatever you want with the access token here...

                    // Store the access token and expiration timestamp
                    accessToken = response_data.accessToken;
                    var access_token_expires_in_seconds = (long)response_data.expiresIn; // This is usually "3600"
                    var current_timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    access_token_expires_at_timestamp = current_timestamp + access_token_expires_in_seconds;
                    
                        Console.WriteLine($"Received new access token: {accessToken.Substring(0, 5)}************");
                    

                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to connect, trying again in 30 seconds...");
                    Console.WriteLine("this is the exception " + e);
                    Thread.Sleep(30000);
                }
            }
        }


        async Task<Bitmap> getBoard(string accessToken)
        {
            ClientWebSocket ws = null;
            try
            {
                while(true)
                {
                    try
                    {
                        ws = new ClientWebSocket();
                        ws.Options.Proxy = new WebProxy(proxy);
                        ws.Options.RemoteCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                        await ws.ConnectAsync(new Uri("wss://gql-realtime-2.reddit.com/query"), CancellationToken.None);
                        Console.WriteLine("Connection Succeeded");
                        string payload = $"{{\"type\":\"connection_init\",\"payload\":{{\"Authorization\":\"Bearer {accessToken}\"}}}}";
                        await ws.SendAsync(Encoding.UTF8.GetBytes(payload), WebSocketMessageType.Text, true, CancellationToken.None);
                        break;
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Failed to Connect to WebSocket, trying again in 30 seconds.. "+e);
                        await Task.Delay(30000);
                    }
                }
                while(true)
                {
                    try
                    {
                        var buffer = new byte[1024];
                        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        Console.WriteLine(result.Count);
                        break;
                    }
                    catch(Exception e)
                    {

                    }
                }



            }
            catch(Exception e)
            {

            }


            return null;
        }

        private string ParseDataFromHtml(string htmlContent)
        {
            // Implement the logic to parse the 'data' value from the HTML content
            // You may use regular expressions or HTML parsing libraries like AngleSharp to do this.
            // For simplicity, I'm leaving it out in this example.
            return string.Empty;
        }

        private int GetUnsetPixel(int currentR, int currentC, int index)
        {
            // Implement the logic to get the next unset pixel coordinates from the pixelMap
            // For simplicity, I'm leaving it out in this example.
            return 0;
        }

        private int SetPixelAndCheckRateLimit(string accessToken, int pixelX, int pixelY, string accountName, int pixelColorIndex, int canvas, int index)
        {
            // Implement the logic to make a POST request to Reddit API to set the pixel
            // For simplicity, I'm leaving it out in this example.
            return 0;
        }
    }
}
