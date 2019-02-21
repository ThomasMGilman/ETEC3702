using System;
using System.Collections.Generic;
using System.Threading;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

class Program
{
    static object Lock;
    static Dictionary<string, bool> visitedLinks;
    static List<string> linksToTest;
    static List<string> nextSetOfLinks;
    static List<Thread> Threads;
    static int maxDepth;
    static int deadLinks;

    public void Producer(string URL, int distance)
    {
        string regMatch = "<\\s*a\\s+[^>]*href\\s*=\\s*['\"][^'\"]['\"]*";
        Regex URLmatch = new Regex(regMatch);
        WebClient Client = new WebClient();
        Stream data;
        StreamReader reader;
        MatchCollection MC;
        string link = URL;
        bool terminate = false, poisoned = false;

        while(maxDepth > 0 && !poisoned)
        {
            while (!terminate && !poisoned)
            {
                try
                {
                    data = Client.OpenRead(link);
                    reader = new StreamReader(data);

                    lock(Lock)
                    {
                        MC = URLmatch.Matches(reader.ReadToEnd());
                    }
                    

                }
                catch (ArgumentException e)
                {
                    lock (Lock)
                    {
                        Console.WriteLine("Error Trying to Read from '{0}'\nException: '{1}'", URL, e.Message);
                    }
                    terminate = true;
                }
                catch (WebException e)
                {
                    lock (Lock)
                    {
                        Console.WriteLine("Error Trying to Read from '{0}'\nException: '{1}'", URL, e.Message);
                    }
                    terminate = true;
                }
            }
        }
    }

    public void Consumer()
    {

    }

    public void ErrorMessageExit(string message)
    {
        Console.WriteLine(message);
        Console.Read();
        Environment.Exit(-1);
    }

    void Main(string[] args)
    {
        string rootURL = null;
        int maxDistance = 0;

        if (args[0].Length > 0)
            rootURL = args[0];
        else
            ErrorMessageExit("Please Input a URL string!");
        bool gotDistance = Int32.TryParse(args[1], out maxDistance);
        if(gotDistance == false)
            ErrorMessageExit("Please Input a integer for distance!");
        else
        {
            if(maxDepth < 0)
                ErrorMessageExit("Please Input a positive integer for distance!");

            visitedLinks    = new Dictionary<string, bool>();
            linksToTest     = new List<string>();
            nextSetOfLinks  = new List<string>();
            Threads         = new List<Thread>();
            Lock            = new object();
            
            //create 4 producers and consumers
            for(int t = 0; t < 4; t++)
            {
                Thread newConsumer = new Thread(() => Consumer());
                Thread newProducer = new Thread(() => Producer(rootURL, maxDistance));
                newConsumer.Start();
                newProducer.Start();

                Threads.Add(newConsumer);
                Threads.Add(newProducer);
            }
            foreach(Thread t in Threads)
                t.Join();
        }
    }
}