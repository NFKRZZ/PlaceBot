
# Reddit Place Bot 2023 r/place

 Reddit Place Bot in C Sharp <br />
 This supports multithreading and proxies  <br />
 It is for placing pixels on r/place  <br />
BUGS: The Place Pixel method works but I'm not sure where the pixels are being placed.<br />
TODO:Deal with Transparency in the source image. 
Possible fix to RAM issue. I don't see a point in fixing it now, but a way to remedy the ram issue would be to localize the bitmap that is being saved when each account's thread requests the canvas.
Currently, each account thread has a bitmap of the entire canvas, but you the user are most likely only concerned with a small part of the canvas that you are botting over, as such the program could just crop down the bitmap to what covers the botted zone which would reduce the size in ram and heavily reduce ram consumption. 
However, this would be something for someone else to implement.

# How to Load Proxies

To load proxies simply reference the path the txt file is in and make sure they are formatted like the following

ip:port 

for ex:

127.0.0.1:9999

# How to load Bots

Same as above with the following format username:password

Put the file name of the txt files in the code as shown in the following picture

![image](https://github.com/NFKRZZ/PlaceBot/assets/43969824/0cc327a9-5326-4312-988c-9ed4f049da80)

# Warning
This may take up a lot of RAM, 450 accounts =~ 20 GB of RAM usage
