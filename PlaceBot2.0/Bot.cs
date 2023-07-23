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
using System.Text.Json;
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
        private List<Tuple<int, Bitmap>> imgs = new List<Tuple<int, Bitmap>>();
        private JObject canvas_details;
        private List<int> canvas_sockets = new List<int>();
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
        private string accessToken;
        private readonly Dictionary<int, int> accessTokenExpiresAtTimestamp;
        private readonly Dictionary<int, int[]> pixelMap;
        private int firstRunCounter;
        private readonly int[] imageCoords;
        private readonly int[] startCoords;
        private int cX = 0, cY = 0;
        private Bitmap boardImage;
        private Bitmap sourceImage;
        private List<Color> cArray;
        private int x_length;
        private int y_length;
        private int timeOut = 0;
        private bool running = false;



        public RedditPlaceWorker(int index, string name, string password,string proxy,Bitmap targetImage,int x, int y)
        {
            this.index = index;
            this.name = name;
            this.password = password;
            this.accessTokenExpiresAtTimestamp = new Dictionary<int, int>();
            this.proxy = proxy;
            this.pixelPlaceFrequency = unverifiedPlaceFrequency ? 1230 : 330;
            this.nextPixelPlacementTime = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds + this.pixelPlaceFrequency;
            cX = x;
            cY = y;
            sourceImage = targetImage;

        }

        public void Init()
        {

           cArray =  ColorMapperz.GenerateRgbColorsArray();
            x_length = sourceImage.Width; 
            y_length = sourceImage.Height;

            handler = new HttpClientHandler
            {
                Proxy = new WebProxy(proxy), // Replace "proxy-ip" and "proxy-port" with the actual proxy IP and port
                UseProxy = true
            };


            client = new HttpClient(handler);
            Login(name, password);
            boardImage =  getBoard(accessToken).Result;
          //  boardImage.Save("hello.jpg",System.Drawing.Imaging.ImageFormat.Jpeg);


            Loop();



        }


        async void Loop()
        {

            int localX = 0;
            int localY = 0;

            while (true)
            {
                Color currentLocationColor = sourceImage.GetPixel(localX, localY);
                if (checkifPlacePixel(localX, localY, currentLocationColor))
                {
                    Task.Run(() => placePixel(currentLocationColor, localX, localY));
                    running = true;
                    while(running)
                    {
                        if(!running)
                        {
                            //THIS FORCES PLACEPIXEL TO COMPLETE ITS EXECUTION BEFORE MOVING ON SINCE RUNNING = FALSE AT THE END OF PLACEPIXEL()
                            break;
                        }
                    }
                   // Console.WriteLine("Done");
                    localX++;
                    Thread.Sleep(timeOut * 1000);
                    timeOut = 0;
                }
                else
                {
                    localX++;
                }

                if(localX>x_length)
                {
                    localX = 0;
                    localY++;
                }
                if(localY>y_length)
                {
                    localY = 0;
                }

                
                Thread.Sleep(500);
                //Console.WriteLine("Checking Board");
                boardImage = getBoard(accessToken).Result;
            }




        }


        async void placePixel(Color color, int localX, int localY)
        {
          

            int colorID = ColorMapperz.getIntFromColor(color);
            string colorstr = ColorMapperz.ColorIdToName(colorID);
            string r_url = "https://gql-realtime-2.reddit.com/query";
            
            int redditX = cX + localX;
            int redditY = cY + localY;

            int canvas_index = getCanvas(cX + localX + 1000, cY + localY + 1000);
            Console.WriteLine(name + " is attempting to place pixel at [" + redditX + "," + redditY + "] with color "+colorstr);

            var payloadObj = new
            {
                operationName = "setPixel",
                variables = new
                {
                    input = new
                    {
                        actionName = "r/replace:set_pixel",
                        PixelMessageData = new
                        {
                            coordinate = new { x = redditX, y = redditY },
                            colorIndex = colorID,
                            canvasIndex = canvas_index
                        }
                    }
                },
                query = @"mutation setPixel($input: ActInput!) {
                act(input: $input) {
                    data {
                        ... on BasicMessage {
                            id
                            data {
                                ... on GetUserCooldownResponseMessageData {
                                    nextAvailablePixelTimestamp
                                    __typename
                                }
                                ... on SetPixelResponseMessageData {
                                    timestamp
                                    __typename
                                }
                                __typename
                            }
                            __typename
                        }
                        __typename
                    }
                    __typename
                }
            }"
            };
            string payload = System.Text.Json.JsonSerializer.Serialize(payloadObj);

            var headers = new
            {
                origin = "https://garlic-bread.reddit.com",
                referer = "https://garlic-bread.reddit.com/",
                apollographql_client_name = "garlic-bread",
                Authorization = "Bearer " + accessToken,
                Content_Type = "application/json"
            };
            string header = System.Text.Json.JsonSerializer.Serialize(headers);
            var headerz = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(header);

            var httpClient = new HttpClient();

            foreach (var h in headerz)
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation(h.Key, h.Value);
            }

            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var respons = await httpClient.PostAsync(r_url, content);

            if (respons.IsSuccessStatusCode)
            {
                // Handle successful response

            }
            else
            {
                // Handle error response
                Console.WriteLine("Error: " + respons.StatusCode);
            }

            string responseContent = await respons.Content.ReadAsStringAsync();
            //Console.WriteLine("Response: " + responseContent);

            JObject responseJson = JObject.Parse(responseContent);

            if (responseJson["data"].Type==JTokenType.Null)
            {
                try
                {
                    int waitTime = (int)Math.Floor(responseJson["errors"][0]["extensions"]["nextAvailablePixelTs"].Value<double>());
                    Console.WriteLine(name + "couldnt place, ratelimited for " + waitTime + " seconds");
                    timeOut = Math.Abs(waitTime);
                }
                catch (Exception ex)
                {
                    if (ex is KeyNotFoundException)
                    {
                        Console.WriteLine(name + " : " + " Access Token is expired, waiting for updating..");
                        timeOut = 30000;
                    }
                }
            }
            else
            {
               // Console.WriteLine(responseJson["data"].ToString());
                File.WriteAllText("fuck.txt",responseJson.ToString());
                int waitTime = (int)Math.Floor(responseJson["data"]["act"]["data"][0]["data"]["nextAvailablePixelTimestamp"].Value<double>());
                timeOut = 310;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(name + " succeeded in placing the pixel");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("this is timeout: " + timeOut + " seconds");

            }
            running = false;
        }
        bool checkifPlacePixel(int x, int y, Color color)
        {
            int imgX = cX+x + 1000;
            int imgY = cY+y + 1000;

            Color pixelColor = boardImage.GetPixel(imgX, imgY);


            Color sourcePicturePixelColor = ColorMapperz.ClosestColor(color, cArray, true);

            bool isSame = (string.Equals(pixelColor.Name, sourcePicturePixelColor.Name, StringComparison.OrdinalIgnoreCase));
          //  Console.WriteLine("They are the same: " + isSame);
            //need to implement transparency 


            return !isSame;
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
                   // Console.WriteLine("Obtaining access token...");
                    var accessTokenResponse = client.GetAsync("https://new.reddit.com/").Result;
                    var accessTokenContent = accessTokenResponse.Content.ReadAsStringAsync().Result;
                    //File.WriteAllText("access_token_response.txt", accessTokenContent);

                    string startTag = "<script id=\"data\">window.___r = ";
                    string endTag = ";</script>";


                    // var accessTokenStartIndex = accessTokenContent.IndexOf("window.__r = ") + "window.__r = ".Length;
                    // var accessTokenEndIndex = accessTokenContent.LastIndexOf(';');

                    int accessTokenStartIndex = accessTokenContent.IndexOf(startTag);
                    int accessTokenEndIndex = accessTokenContent.IndexOf(endTag, accessTokenStartIndex);
                  // Console.WriteLine("THIS IS START: " + accessTokenStartIndex + " THIS IS END " + accessTokenEndIndex);
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

                    string accessTokena = response_data.accessToken;
                    // Do whatever you want with the access token here...

                    // Store the access token and expiration timestamp
                    accessToken = response_data.accessToken;
                    var access_token_expires_in_seconds = (long)response_data.expiresIn; // This is usually "3600"
                    var current_timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    access_token_expires_at_timestamp = current_timestamp + access_token_expires_in_seconds;
                    
                     //   Console.WriteLine($"Received new access token: {accessToken.Substring(0, 5)}************");
                    

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
            Bitmap finalImg = null;
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
                        ws.Options.SetRequestHeader("Origin", "https://hot-potato.reddit.com");
                        await ws.ConnectAsync(new Uri("wss://gql-realtime-2.reddit.com/query"), CancellationToken.None);
                      //  Console.WriteLine("Connection Succeeded");

                        var jsonToken = new
                        {
                            type = "connection_init",
                            payload = new { Authorization = "Bearer " + accessToken }
                        };
                        string payload = System.Text.Json.JsonSerializer.Serialize(jsonToken);
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
                      //  Console.WriteLine("Result size "+result.Count);
                       // Console.WriteLine("This is result: "+result.ToString());
                        if(result.MessageType == WebSocketMessageType.Close)
                        {
                            Console.WriteLine("Web Socket Closed");
                            throw new Exception("WEB SOCKET CLOSED");
                        }
                        var messagee = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        //Console.WriteLine("Recieved message " + messagee);
                        break;
                    }
                    catch(Exception e)
                    {

                    }
                }



                ///////////////////////////////////////////////\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
                ///


                var jsonTokena = new
                {
                    id = "1",
                    type = "start",
                    payload = new
                    {
                        variables = new
                        {
                            input = new
                            {
                                channel = new
                                {
                                    teamOwner = "GARLICBREAD",
                                    category = "CONFIG"
                                }
                            }
                        },
                        extensions = new { },
                        operationName = "configuration",
                        query = "subscription configuration($input: SubscribeInput!) {\n  subscribe(input: $input) {\n    id\n    ... on BasicMessage {\n      data {\n        __typename\n        ... on ConfigurationMessageData {\n          colorPalette {\n            colors {\n              hex\n              index\n              __typename\n            }\n            __typename\n          }\n          canvasConfigurations {\n            index\n            dx\n            dy\n            __typename\n          }\n          canvasWidth\n          canvasHeight\n          __typename\n        }\n      }\n      __typename\n    }\n    __typename\n  }\n}\n"
                    }
                };
                string message = System.Text.Json.JsonSerializer.Serialize(jsonTokena);


                var buf = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                await ws.SendAsync(buf,WebSocketMessageType.Text,true,CancellationToken.None);
          
                JObject canva_details = null;
                while(true)
                {
                    var buffer = new byte[8192];
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                  //  Console.WriteLine("result is size "+result.Count);
                    var mseg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    JObject l = JObject.Parse(mseg);
                    //Console.WriteLine("This is l "+l.ToString());
                    //Console.WriteLine("This is size " + mseg.Length);
                    if(l["type"].ToString()=="data")
                    {
                        canvas_details = l["payload"]["data"]["subscribe"]["data"].ToObject<JObject>();
                        //Console.WriteLine("Canvas config: " + l.ToString());
                        break;
                    }
                    else
                    {
                        //Console.WriteLine("Waiting");
                    }
                }

                int canvas_count = ((JArray)canvas_details["canvasConfigurations"]).Count;
                
                for(int i = 0;i<canvas_count;i++)
                {
                    canvas_sockets.Add(2 + i);

                    var jsonToken = new
                    {
                        id = (2 + i).ToString(),
                        type = "start",
                        payload = new
                        {
                            variables = new
                            {
                                input = new
                                {
                                    channel = new
                                    {
                                        teamOwner = "GARLICBREAD",
                                        category = "CANVAS",
                                        tag = i.ToString()
                                    }
                                }
                            },
                            extensions = new object(),
                            operationName = "replace",
                            query = "subscription replace($input: SubscribeInput!) {\n  subscribe(input: $input) {\n    id\n    ... on BasicMessage {\n      data {\n        __typename\n        ... on FullFrameMessageData {\n          __typename\n          name\n          timestamp\n        }\n        ... on DiffFrameMessageData {\n          __typename\n          name\n          currentTimestamp\n          previousTimestamp\n        }\n      }\n      __typename\n    }\n    __typename\n  }\n}\n"
                        }
                    };


                    string payload = System.Text.Json.JsonSerializer.Serialize(jsonToken);
                    var buaf = new ArraySegment<byte>(Encoding.UTF8.GetBytes(payload));
                    await ws.SendAsync(buaf, WebSocketMessageType.Text, true, CancellationToken.None);



                }

                //Console.WriteLine("A total of " + canvas_sockets.Count + " canvas sockets openend");

                while (canvas_sockets.Count > 0)
                {
                    var buffer = new byte[1024];
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var messy = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    JObject temp = JObject.Parse(messy);

                    if (temp["type"].ToString() == "data")
                    {
                        JObject msg = temp["payload"]["data"]["subscribe"] as JObject;

                        if (msg["data"]["__typename"].ToString() == "FullFrameMessageData")
                        {
                           
                            int img_id = int.Parse(temp["id"].ToString());
                            

                            if (canvas_sockets.Contains(img_id))
                            {
                                
                                string imgUrl = msg["data"]["name"].ToString();

                                // Implement your own method to download the image from the imgUrl
                                // Here's an example using HttpClient to download the image
                                using (HttpClient httpClient = new HttpClient())
                                {
                                    HttpResponseMessage response = await httpClient.GetAsync(imgUrl);
                                    if (response.IsSuccessStatusCode)
                                    {
                                        byte[] imageData = await httpClient.GetByteArrayAsync(imgUrl);
                                        Bitmap bitmap;
                                        using (MemoryStream ms = new MemoryStream(imageData))
                                        {
                                            // Use the MemoryStream to create a Bitmap
                                            bitmap = new Bitmap(ms);

                                            // Return the Bitmap
                                        }
                                        Bitmap imgBitmap = new Bitmap(bitmap);
                                        string path = "hello" + img_id + ".jpg";
                                        //imgBitmap.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
                                        imgs.Add(new Tuple<int, Bitmap>(img_id, imgBitmap));
                                        canvas_sockets.Remove(img_id);
                                      
                                    }
                                    else
                                    {
                                        
                                        canvas_sockets.Remove(img_id);
                                    }
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < canvas_count - 1; i++)
                {
                    var stopMessage = new
                    {
                        id = (2 + i).ToString(),
                        type = "stop"
                    };
                    string stopMessageJson = System.Text.Json.JsonSerializer.Serialize(stopMessage);
                    byte[] stopMessageBytes = System.Text.Encoding.UTF8.GetBytes(stopMessageJson);
                    await ws.SendAsync(new ArraySegment<byte>(stopMessageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                int new_img_width = canvas_details["canvasConfigurations"].Max(x => (int)x["dx"]) + (int)canvas_details["canvasWidth"];
                //Console.WriteLine($"New image width: {new_img_width}");

                int new_img_height = canvas_details["canvasConfigurations"].Max(x => (int)x["dy"]) + (int)canvas_details["canvasHeight"];
                //Console.WriteLine($"New image height: {new_img_height}");
                Bitmap new_img = new Bitmap(new_img_width, new_img_height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                foreach (var img in imgs.OrderBy(x => x.Item1))
                {
                    int img_id = img.Item1;
                    Bitmap bitmap = img.Item2;
                    int dx_offset = (int)canvas_details["canvasConfigurations"][img_id - 2]["dx"];
                    int dy_offset = (int)canvas_details["canvasConfigurations"][img_id - 2]["dy"];

                    using (Graphics g = Graphics.FromImage(new_img))
                    {
                        g.DrawImage(bitmap, dx_offset, dy_offset);
                    }
                }

                finalImg = new_img;

            }
            catch (Exception e)
            {
                Console.WriteLine("EXCEPTION " + e);
            }

            //Console.WriteLine("Getting Canvas");
            Rectangle cropArea = new Rectangle(500, 0, 2500 - 500, 1500 - 0);
            Bitmap cropped = finalImg.Clone(cropArea,finalImg.PixelFormat);
            return cropped;
        }
        
        int getCanvas(int x, int y)//X AND Y BASED OFF BITMAP COORDS OF CROPPED IMAGE
        {
            int canvas = 0;

            if(x<=499 && y<=999)
            {
                canvas = 0;
            }
            else if(x>499&&x<=1499 && y<=999)
            {
                canvas = 1;
            }
            else if(x>1499&&y<=999)
            {
                canvas = 2;
            }    
            else if(x<=499 && y>999)
            {
                canvas = 3; 
            }    
            else if(x>499&&x<=1499 && y>999)
            {
                canvas = 4;
            }
            else if(x>1499 && y>999)
            {
                canvas = 5;
            }
            else
            {
                Console.WriteLine("THIS IS NOT SUPPOSED TO PRINT IF IT DOES SOMETHING IS WRONG IN getCanvas()");
            }
            Console.WriteLine("GET CANVAS CALLED RETURNING: " + canvas);
            return canvas;
        }

    }
}
