//Thomas Gilman
//James Hudson
//ETEC 3702 OS2
//Assignment 2 Threads
// 1/17/2019
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Generic;

namespace Threads
{
    class Program
    {
        static int fileIndex = 0;
        public static void DownloadFileCallback(object sender, AsyncCompletedEventArgs e)   //Webclient complete callback
        {
            object com = new object();
            string fileName = ((System.Net.WebClient)(sender)).QueryString["fileName"];
            if(e.Cancelled)
            {
                lock(com)
                {
                    Console.WriteLine("Thread cancelled download of: {0}\n", fileName);
                }
            }
            else if(e.Error != null)
            {
                lock (com)
                {
                    Console.WriteLine("Thread download Error: {0}\n", fileName);
                }
            }
            else
            {
                lock (com)
                {
                    Console.WriteLine("Thread successfully downloaded file: {0}\n", fileName);
                }
            }
        }

        private static async void downloadFile(string address)   //Function for downloading files, should set as thread starting function
        {
            string[] StringToParse = address.Split('/');
            string name;
            object M = new object();
            Uri pageUri;
            WebClient client;
            if (StringToParse.Length > 1 && StringToParse[StringToParse.Length-1].Length > 0)
            {
                name = StringToParse[StringToParse.Length - 1].Trim() + ".html";
                var invalids = System.IO.Path.GetInvalidFileNameChars();
                name = String.Join("_", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            }
            else
            {
                //Interlocked.Increment(ref fileIndex);
                //name = fileIndex.ToString() + ".html";
                name = "index.html";
            }
                

            lock(M)
            {
                Console.WriteLine("Name of File to Download: {0}\nName given to file: {1}\nname length: {2}\n", address, name, name.Length);
            }
            try
            {
                pageUri = new Uri(address, UriKind.Absolute);

                try
                {
                    client = new WebClient();
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadFileCallback);
                    client.QueryString.Add("fileName", address);
                    await client.DownloadFileTaskAsync(pageUri, name);
                }
                catch (Exception e)
                {
                    lock (M)
                    {
                        Console.WriteLine("CLIENT ERROR!!!: failed with exception {0}\n\taddress: {1}\n\tname: {2}\n", e.GetType(), address, name);
                        Console.Read();
                        System.Environment.Exit(-1);
                    }
                }
            }
            catch(Exception e)
            {
                lock (M)
                {
                    Console.WriteLine("URI ERROR!!!: failed with exception {0}\n\taddress: {1}\n\tname: {2}\n",e.GetType(), address, name);
                    Console.Read();
                    System.Environment.Exit(-1);
                }
            }
        }

        public static void Main(string[] args)
        {
            if(args[0].Length == 0)
            {
                Console.WriteLine("Please input a command line argument for a input_file");
                Console.Read();
                System.Environment.Exit(-1);
            }

            object com = new object();
            string infile = args[0];
            string[] lines = System.IO.File.ReadAllLines(@infile);
            List<Thread> currentThreads = new List<Thread>();
            Thread mainThread = Thread.CurrentThread;
            mainThread.Name = "MainThread";

            foreach(var line in lines)
            {
                Thread tmpThread = new Thread(() => downloadFile(line));
                currentThreads.Add(tmpThread);
                tmpThread.Start();
            }
            foreach(Thread t in currentThreads)
            {
                t.Join();
            }
            Console.Read();
        }
    }
}
