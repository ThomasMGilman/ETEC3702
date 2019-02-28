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
    static Dictionary<string, Tuple<Uri, int>> attemptedLinks;
    static Queue<Tuple<Uri, int>> clientLinks;                 //links to test, should try to download
    static Queue<Tuple<string, int, Uri>> producerLinks;       //links to parse for more links
    static List<Thread> Threads;
    static int maxDepth = 0;
    static int deadLinks = 0;
    static int currentWorkers = 0;
    static int fileNum = 0;

    public static void Producer()
    {
        string regMatch = "<\\s*a\\s+[^>]*href\\s*=\\s*['\"][^'\"]*['\"]";
        string data;
        Regex URLmatch = new Regex(regMatch);
        MatchCollection MC;
        Tuple<string, int, Uri> link = null;
        Tuple<Uri, int> linkToQue = null;
        Uri newLink = null;
        bool poisoned = false;

        while(!poisoned)
        {
            try
            {
                //Check the Queue for contents, else wait or die if poisoned
                lock(Lock)
                {
                    while (producerLinks.Count == 0)
                        Monitor.Wait(Lock);

                    link = producerLinks.Dequeue();

                    if(link == null)
                    {
                        poisoned = true;
                        producerLinks.Enqueue(null);
                        break;
                    }

                    if (attemptedLinks.ContainsKey(link.Item1))
                        throw new Exception("Error: Link already added to Dictionary!!! Why is it in Queue!?");
                    attemptedLinks.Add(link.Item1, new Tuple<Uri, int>(link.Item3, link.Item2));
                    currentWorkers++;
                    Console.WriteLine("Producer Working on: {0}, depth: {1}", link.Item1, link.Item2);
                }

                //read the entire document and match all instances of an address to a collention
                data = File.ReadAllText(link.Item1);
                MC = URLmatch.Matches(data);

                foreach(Match m in MC)
                {
                    string address = m.Value.Substring(m.Value.IndexOf('=')+1); //get the address after the = and href
                    lock (Lock)
                    {
                        Console.WriteLine("looking at {0}\naddress: {1}", m.Value, address);
                    }
                    newLink = new Uri(link.Item3, address.Trim());              //create URI of address using host address as the origin link or root
                    linkToQue = new Tuple<Uri, int>(newLink, link.Item2 + 1);   //add new link to be processed or tried and incriment depth.

                    lock(Lock)
                    {
                        clientLinks.Enqueue(linkToQue);
                        Monitor.PulseAll(Lock);
                    }
                }
            }
            catch (Exception e)
            {
                lock (Lock)
                {
                    Console.WriteLine("Error Trying to Read from '{0}'\nException: '{1}'", link.Item3.AbsoluteUri, e.Message);
                    Console.Read();

                    producerLinks.Enqueue(null);
                    clientLinks.Enqueue(null);
                    Monitor.PulseAll(Lock);
                }
            }
            lock (Lock)
            {
                currentWorkers--;
            }
        }
    }

    public static void Consumer()
    {
        WebClient Client = new WebClient();
        Tuple<Uri, int> link = null;
        Tuple<string, int, Uri> linkToAdd = null;
        int num = 0;
        bool poisoned = false;

        while(!poisoned)
        {
            try
            {
                lock (Lock)
                {
                    if (clientLinks.Count == 0 && producerLinks.Count == 0 && currentWorkers == 0)
                    {
                        producerLinks.Enqueue(null);
                        clientLinks.Enqueue(null);
                        Monitor.PulseAll(Lock);
                        return;
                    }
                    while (clientLinks.Count == 0)
                        Monitor.Wait(Lock);

                    link = clientLinks.Dequeue();
                    if (link == null)
                    {
                        poisoned = true;
                        clientLinks.Enqueue(null);
                        return;
                    }
                    currentWorkers++;
                    num = fileNum++;
                    Console.WriteLine("Consumer Working on: {0}, depth: {1}", link.Item1.AbsoluteUri, link.Item2);
                }

                Client.DownloadFile(link.Item1.AbsoluteUri, num.ToString());

                if(link.Item2 + 1 < maxDepth)
                {
                    linkToAdd = new Tuple<string, int, Uri>(num.ToString(), link.Item2, link.Item1);
                    lock(Lock)
                    {
                        producerLinks.Enqueue(linkToAdd);
                        Monitor.PulseAll(Lock);
                    }
                }
            }
            catch (Exception e)
            {
                lock (Lock)
                {
                    Console.WriteLine("Error Dead Link!! Trying to Read from '{0}'\nException: '{1}'", link.Item1.AbsoluteUri, e.Message);
                    Console.Read();
                    deadLinks++;
                }
            }
            lock (Lock)
            {
                currentWorkers--;
            }
        }

    }

    public static void ErrorMessageExit(string message, Exception e = null)
    {
        Console.Write(message);
        if (e != null)
            Console.Write(e);
        Console.WriteLine();
        Console.Read();
        Environment.Exit(-1);
    }

    static void Main(string[] args)
    {
        Uri rootAddress = null;

        if (args[0].Length > 0)
        {
            try
            {
                rootAddress = new Uri(args[0]);
            }
            catch (Exception e)
            {
                ErrorMessageExit("Issue with creating root Uri", e);
            }
        }
        else
            ErrorMessageExit("Please Input a URL string!");
        bool gotDistance = Int32.TryParse(args[1], out maxDepth);
        if(gotDistance == false)
            ErrorMessageExit("Please Input a integer for distance!");
        else
        {
            if(maxDepth < 0)
                ErrorMessageExit("Please Input a positive integer for distance!");
            Console.WriteLine("Args0: {0}, Args1: {1}", args[0], args[1]);
            Console.WriteLine("Address: {0}, maxDepth: {1}", rootAddress.AbsoluteUri, maxDepth);

            attemptedLinks  = new Dictionary<string, Tuple<Uri, int>>();
            clientLinks     = new Queue<Tuple<Uri, int>>();
            producerLinks   = new Queue<Tuple<string, int, Uri>>();
            Threads         = new List<Thread>();
            Lock            = new object();
            Tuple<Uri, int> rootLink = new Tuple<Uri, int>(rootAddress, 0);
            deadLinks = 0;
            currentWorkers = 0;
            fileNum = 0;
            clientLinks.Enqueue(rootLink);
            
            //create 4 producers and consumers
            for(int t = 0; t < 4; t++)
            {
                Thread newConsumer = new Thread(() => Consumer());
                Thread newProducer = new Thread(() => Producer());
                newConsumer.Start();
                newProducer.Start();

                Threads.Add(newConsumer);
                Threads.Add(newProducer);
            }
            foreach(Thread t in Threads)
                t.Join();
        }

        if(attemptedLinks.Count > 0)
        {
            foreach (KeyValuePair<string, Tuple<Uri, int>> key in attemptedLinks)
            {
                Console.WriteLine("Visited: {0}\tDepth: {1}\tAbsolutePath: {2}", key.Key, key.Value.Item2, key.Value.Item1.AbsolutePath);
            }
        }
        Console.Read();

    }
}