using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static int numDownloads;
    static int index;
    static object Lock;

    public static async Task<HttpResponseMessage> getHttpResponse(string address)
    {
        Uri link = new Uri(address);
        HttpClient client = new HttpClient();
        return await client.GetAsync(link);           //get response from address;
    }

    public static async void downloadLink(string address)
    {
        string fileName;
        FileStream file;

        //create FileName based on relative path of link
        int indexOfRelativ = address.LastIndexOf('/');
        if (indexOfRelativ != -1 && indexOfRelativ < address.Length - 1) //does address contain a path?
        {
            fileName = address.Substring(indexOfRelativ + 1);
            if (!address.Contains(".html"))
                fileName += ".html";           //get relative path from index starting after last '/'
        }
        else
            fileName = index++.ToString()+".html"; //no path found

        var msg = getHttpResponse(address);
        HttpResponseMessage response = msg.Result;

        if(response.IsSuccessStatusCode)                          //Http Connection Success
        {
            if (File.Exists(fileName))   //if FileName already exists append number to it
            {
                int fileNum = 0;
                while (File.Exists(fileName + fileNum))
                    fileNum++;

                fileName += fileNum;
            }
            foreach (char c in Path.GetInvalidFileNameChars())  //remove invalid filename chars
                fileName = fileName.Replace(c, ' ');

            var dataTsk = response.Content.ReadAsByteArrayAsync();  //Read contents of webdata to byte array to be written to a file
            file = new FileStream(fileName, FileMode.Create);
            await file.WriteAsync(dataTsk.Result, 0, dataTsk.Result.Length);
            file.Close();
            //Console.WriteLine("wrote {0} to {1}", address, fileName);
        }
        else
        {
            //Console.WriteLine("Failed to get ' {0} ' message", address);
        }
        numDownloads--;
    }

    static void Main(string[] args)
    {
        if (args.Length != 0)
        {
            Lock = new object();
            string[] data = File.ReadAllLines(args[0]); //read in File content
            numDownloads = data.Length;
            index = 0;
            //Console.WriteLine("links:{0}, numDownloads:{1}", data.Length, numDownloads);
            for(int i = 0; i < data.Length; i++)
            {
                string address = data[i].Trim();
                try
                {
                    downloadLink(address);
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error: {0} failed with ' {1} '", address, e.Message);
                }
            }

            while (numDownloads > 0)
            {
                Thread.Sleep(1000);
            }

            Console.WriteLine("All Done!!");
        }
        else
            throw new Exception("Input Error!!! Need to pass the name of a file of links to download in \n'link newline link newline etc...' format");
    }
}