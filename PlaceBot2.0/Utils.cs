using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Drawing;

namespace PlaceBot2._0
{

    class PlaceClient
    {
        private dynamic json_data;
        private string image_path;
        private Image img;
        private dynamic pix;
        private Size image_size;

        public dynamic GetJsonData(string config_path)
        {
            string configFilePath = Path.Combine(Directory.GetCurrentDirectory(), config_path);

            if (!File.Exists(configFilePath))
            {
                Console.WriteLine("No config.json file found. Read the README");
                Environment.Exit(1);
            }

            string jsonContent = File.ReadAllText(configFilePath);
            json_data = JsonConvert.DeserializeObject(jsonContent);
            return json_data;
        }

        public void LoadImage()
        {
            // Read and load the image to draw and get its dimensions
            try
            {
                img = Image.FromFile(image_path);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Failed to load image: File not found");
                Environment.Exit(1);
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine("Failed to load image: Invalid image format");
                Environment.Exit(1);
            }

            // Convert all images to RGBA - Transparency should only be supported with PNG
            if (img.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            {
                Bitmap bmp = new Bitmap(img.Width, img.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.DrawImage(img, 0, 0);
                }
                img.Dispose();
                img = bmp;
                Console.WriteLine("Converted to rgba");
            }

            pix = new dynamic[img.Width, img.Height];
            Bitmap bmpImg = new Bitmap(img);
            for (int x = 0; x < img.Width; x++)
            {
                for (int y = 0; y < img.Height; y++)
                {
                    pix[x, y] = bmpImg.GetPixel(x, y);
                }
            }

            Console.WriteLine("Loaded image size: " + img.Size);
            image_size = img.Size;
        }
    }
}
