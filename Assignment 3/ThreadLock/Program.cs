//Thomas Gilman
//James Hudson
//ETEC 3702 OS2
//Assignment 3 ThreadLocks
// 1/24/2019
using System;
using System.Net;
using System.Threading;
using System.ComponentModel;
using System.Collections.Generic;

namespace ThreadLock
{
    /// <summary>
    /// Class object with Thread attached that creates a webclient and attempts to download the string passed to it.
    /// The string passed should be a valid URL address otherwise an exception will be thrown.
    /// setThreadJoin must be called after being created inorder to wait alongside any other threads that have been created when finished.
    /// </summary>
    public class ClientDownloader
    {
        string fileName, pageAddress;
        object comLock = new object();
        int progress = 0;
        Uri pageUri;
        WebClient client;
        Thread downloadingThread;

        public ClientDownloader(string address)
        {
            pageAddress = address;
            setFileName();
            downloadingThread = new Thread(() => downloadFile());
            downloadingThread.Start();
        }
        public int returnDownloadProgress() //return the progress of the assigned threads webclient download
        {
            return progress;
        }
        public string getFileName()         //return name given to be assigned to the file download
        {
            return fileName;
        }
        public string getFileAddress()      //return page Address given at creation of class object
        {
            return pageAddress;
        }
        public Thread getRunningThread()    //return the current thread assigned to this task
        {
            return downloadingThread;
        }
        public void setThreadJoin()         //Must be called by the main thread after created
        {
            downloadingThread.Join();
        }
        //File callback for state of download if stopped, only if 'Cancelled, Error, or Finished'
        private void DownloadFileCallback(object sender, AsyncCompletedEventArgs e)
        {
            object com = new object();
            if (e.Cancelled)
            {
                //lock (com)
                //{
                //    Console.WriteLine("Thread Client cancelled download of: {0}\n", fileName);
                //}
            }
            else if (e.Error != null)
            {
                //lock (com)
                //{
                //    Console.WriteLine("Thread Client download Error: {0}\n", fileName);
                //}
                progress = -1;
            }
            else
            {
                //lock (com)
                //{
                //    Console.WriteLine("Thread Client successfully downloaded file: {0}\n", fileName);
                //}
            }
        }
        //update clients download progress if it has not errored out
        private void DownloadFileProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            if(progress >= 0)
                progress = e.ProgressPercentage;
        }
        //Setup webclient for provided string and download it to a file
        private async void downloadFile()
        {
            try
            {
                pageUri = new Uri(pageAddress, UriKind.Absolute);
                try
                {
                    client = new WebClient();
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadFileProgress);
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadFileCallback);
                    client.QueryString.Add("fileName", pageAddress);
                    await client.DownloadFileTaskAsync(pageUri, fileName);
                }
                catch (Exception e)
                {
                    lock (comLock)
                    {
                        //Console.WriteLine("CLIENT ERROR!!!: {0}\n\taddress: {1}\n\tname: {2}\n", e.GetType(), pageAddress, fileName);
                        //Console.Read();
                        progress = -1;
                    }
                }
            }
            catch (Exception e)
            {
                lock (comLock)
                {
                    //Console.WriteLine("URI ERROR!!!: {0}\n\taddress: {1}\n\tname: {2}\n", e.GetType(), pageAddress, fileName);
                    //Console.Read();
                    progress = -1;
                }
            }
        }
        //get filename after last '/' if there is one, otherwise name it 'index.html'
        private void setFileName()
        {
            string[] StringToParse = pageAddress.Split('/');
            if (StringToParse.Length > 1 && StringToParse[StringToParse.Length - 1].Length > 0)
            {
                fileName = StringToParse[StringToParse.Length - 1].Trim() + ".html";
                var invalids = System.IO.Path.GetInvalidFileNameChars();
                fileName = String.Join("_", fileName.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            }
            else
            {
                fileName = "index.html";
            }
        }
    }

    class Program
    {
        public static bool checkDownloadProcess(List<ClientDownloader> currentDownloads) //IMPLIMENT TIMER HERE TO PRINT EVERY HALF SECOND UNTIL ALL THREADS ARE DONE
        {
            bool allDoneStatus = true;
            object comLock = new object();
            int progress;

            lock (comLock)
            {
                Console.WriteLine("\n------------------------------------------------------------\n");
            }

            foreach (ClientDownloader cd in currentDownloads)
            {
                lock(comLock)
                    progress = cd.returnDownloadProgress();

                if (progress == 100)
                {
                    lock (comLock)
                    {
                        Console.WriteLine("{0}\n\tstatus: Complete", cd.getFileAddress());
                    }
                }
                else if (progress < 0)
                {
                    lock (comLock)
                    {
                        Console.WriteLine("{0}\n\tstatus: Error", cd.getFileAddress());
                    }
                }
                else
                {
                    lock (comLock)
                    {
                        Console.WriteLine("{0}\n\tstatus: In progress", cd.getFileAddress());
                    }
                    allDoneStatus = false;
                }
            }
            return allDoneStatus;
        }

 ////////////////////////////////////////////////////////////////////////////////////MAIN///////////////////////////////////////////////
        public static void Main(string[] args)
        {
            if (args[0].Length == 0 || args == null)
            {
                Console.WriteLine("Please input a command line argument for a input_file");
                Console.Read();
                System.Environment.Exit(-1);
            }

            //Variables
            string infile = args[0];
            string[] lines = System.IO.File.ReadAllLines(@infile);
            bool allDone = false;
            List<ClientDownloader> currentDownloads = new List<ClientDownloader>();

            //Setup
            foreach (var line in lines)
            {
                ClientDownloader newDownloader = new ClientDownloader(line);//Start all new threads in their own class's
                currentDownloads.Add(newDownloader);                        //Put each class into list to access progress
            }
            foreach (ClientDownloader cd in currentDownloads)
            {
                cd.setThreadJoin();                                         //Join all Threads, must be called after all threads created and started in previous foreach loop
            }
            while(!allDone)
            {
                Thread.Sleep(500);                                          //Main Thread should wait every half second
                allDone = checkDownloadProcess(currentDownloads);
            }

            //Fin
            Console.WriteLine("allDone");
            Console.Read();
        }
    }
}