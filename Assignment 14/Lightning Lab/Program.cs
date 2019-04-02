//Thomas Gilman
//James Hudson
//Operating Systems 2
//Lightning Lab Assignment 14
//28th March, 2019
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

class Program
{
    static int numDownloads;
    static Dictionary<string, download> downloads;
    static int index;

    public class download
    {
        public string fileName;
        public HttpResponseMessage response;
        public float progress;
        public download()
        {
            progress = 0;
        }
    }

    public static async Task<HttpResponseMessage> getHttpResponse(string address)
    {
        Uri link = new Uri(address);
        HttpClient client = new HttpClient();
        return await client.GetAsync(link);           //get response from address;
    }

    public static async Task writeToFile(string fileName, string address, byte[] data)
    {
        try
        {
            FileStream file = new FileStream(fileName, FileMode.Create);
            int amountWritten = 0;
            int amountToWrite = data.Length / 100;
            if (amountToWrite == 0)
                amountToWrite = 200;

            while (amountWritten < data.Length)
            {
                int toWrite;
                if (amountToWrite <= (data.Length - amountWritten))
                {
                    toWrite = amountToWrite;
                }
                else
                    toWrite = data.Length - amountWritten;
                await file.WriteAsync(data, amountWritten, toWrite);
                amountWritten += toWrite;
                downloads[address].progress = (amountWritten / data.Length) * 100f;
                //Console.WriteLine("{0} Size: {1} Wrote: {2}\nNow: {3} progress: {4}", address, data.Length, toWrite, amountWritten, downloads[address].progress);
            }

            file.Close();
        }
        catch(Exception e)
        {
        }
    }

    public static async Task downloadLink(string address)
    {
        string fileName;

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

        HttpResponseMessage response = await getHttpResponse(address);
        downloads[address].response = response;

        if(response.IsSuccessStatusCode)                          //Http Connection Success
        {
            foreach (char c in Path.GetInvalidFileNameChars())  //remove invalid filename chars
                fileName = fileName.Replace(c, ' ');
            downloads[address].fileName = fileName;

            byte[] data = await response.Content.ReadAsByteArrayAsync();  //Read contents of webdata to byte array to be written to a file
            await writeToFile(fileName, address, data);
        }
        numDownloads--;
    }

    static void Main(string[] args)
    {
        if (args.Length != 0)
        {
            downloads = new Dictionary<string, download>();
            string[] data = File.ReadAllLines(args[0]); //read in File content
            numDownloads = data.Length;
            index = 0;
            //Console.WriteLine("links:{0}, numDownloads:{1}", data.Length, numDownloads);
            for(int i = 0; i < data.Length; i++)
            {
                string address = data[i].Trim();
                try
                {
                    if (!downloads.ContainsKey(address)) //dont download the same link twice
                    {
                        download newDownload = new download();
                        downloads.Add(address, newDownload);
                        downloadLink(address);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error: {0} failed with ' {1} '", address, e.Message);
                }
            }

            bool notFin = true;
            while (notFin)
            {
                notFin = false;
                Thread.Sleep(1000);
                foreach(KeyValuePair<string, download> pair in downloads)
                {
                    download d = pair.Value;
                    string progress;
                    if(d.response != null)
                    {
                        if(d.response.IsSuccessStatusCode)
                        {
                            if (d.progress != 100)
                                notFin = true;
                            progress = d.progress.ToString() + "%";
                        }
                        else
                            progress = "DeadLink!!";
                    }
                    else
                    {
                        notFin = true;
                        progress = "Waiting on response!!";
                    }
                    Console.WriteLine("{0} : {1}", pair.Key, progress);
                }
                Console.WriteLine("------------------------------------");
            }

            Console.WriteLine("All Done!!");
            Console.Read();
        }
        else
            throw new Exception("Input Error!!! Need to pass the name of a file of links to download in \n'link newline link newline etc...' format");
    }
}