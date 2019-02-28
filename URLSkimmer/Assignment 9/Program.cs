using System;
using System.Collections.Generic;
using System.Threading;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

class Program
{

    public struct webLink
    {
        public Uri link;
        public Uri origin;
        public Exception exception;
        public int depth;
    }

    static object Lock;
    static Dictionary<string, webLink> deadLinks;
    static Dictionary<string, webLink> visitedLinks;
    static Queue<webLink> clientLinks;                  //links to test, should try to download
    static Queue<Tuple<string, webLink>> producerLinks; //links to parse for more links
    static List<Thread> Threads;

    static int maxDepth = 0;
    static int numDeadLinks = 0;
    static int currentWorkers = 0;
    static int fileNum = 0;
    static bool poison = false;

    public static void Producer()
    {
        //string regMatch = "<\\s*a\\s+[^>]*href\\s*=\\s*['\"][^'\"]*['\"]";
        string regMatch = "href\\s*=\\s*(?:[\"'](?<1>[^\"']*)[\"']|(?<1>\\S+))"; //this regex is provided on the microsoft C# documentation website
        string data;
        Regex URLmatch = new Regex(regMatch);
        MatchCollection MC;
        Tuple<string, webLink> link = null;
        webLink newLink;
        bool poisoned = false;

        while(!poisoned)
        {
            try
            {
                //Check the Queue for contents, else wait or die if poisoned
                lock(Lock)
                {
                    while (producerLinks.Count == 0 && poison == false)
                        Monitor.Wait(Lock);

                    if (poison)
                    {
                        return;
                    }

                    link = producerLinks.Dequeue();

                    if (visitedLinks.ContainsKey(link.Item2.link.AbsoluteUri))
                        throw new Exception("Error: Link already added to Dictionary!!! Why is it in Queue!?");
                    visitedLinks.Add(link.Item2.link.AbsoluteUri, link.Item2);
                    currentWorkers++;
                    //Console.WriteLine("Producer Working on: {0}, depth: {1}", link.Item2.link.AbsoluteUri, link.Item2.depth);
                }

                //read the entire document and match all instances of an address to a collention
                data = File.ReadAllText(link.Item1);
                MC = URLmatch.Matches(data);

                foreach(Match m in MC)
                {
                    string address = m.Value.Substring(m.Value.IndexOf('=') + 1).Trim();    //get the address after the = and href
                    newLink         = new webLink();
                    newLink.link    = new Uri(link.Item2.link, address);
                    newLink.origin  = link.Item2.link;
                    newLink.depth   = link.Item2.depth + 1;

                    lock (Lock)
                    {
                        if(!visitedLinks.ContainsKey(newLink.link.AbsoluteUri) && !deadLinks.ContainsKey(newLink.link.AbsoluteUri))
                        {
                            clientLinks.Enqueue(newLink);
                            Monitor.PulseAll(Lock);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                lock (Lock)
                {
                    Console.WriteLine("Error This shouldnt happen, Trying to Read from '{0}'\nException: '{1}'", link.Item2.link.AbsoluteUri, e.Message);
                    Console.Read();

                    poison = true;
                    Monitor.PulseAll(Lock);
                    return;
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
        webLink linkToTry = new webLink();
        Tuple<string, webLink> linkToAdd = null;
        int num = 0;
        bool poisoned = false;

        while(!poisoned)
        {
            try
            {
                lock (Lock)
                {
                    if (clientLinks.Count == 0 && producerLinks.Count == 0 && currentWorkers == 0) //last worker poisons everyone else
                    {
                        poison = true;
                        Monitor.PulseAll(Lock);
                        return;
                    }
                    while (clientLinks.Count == 0 && poison == false)                               //no work, no poison? wait
                        Monitor.Wait(Lock);

                    if (poison)                                                                     //Is there poison to partake of and leave?
                        return;

                    linkToTry = clientLinks.Dequeue();                                              //no snack, just work, get item to work on
                    currentWorkers++;                                                               //incriment workers
                    num = fileNum++;                                                                //increment file name for link
                    //Console.WriteLine("Consumer working on: {0}\nDepth: {1}", linkToTry.link.AbsoluteUri, linkToTry.depth);
                }

                Client.DownloadFile(linkToTry.link.AbsoluteUri, num.ToString());                    //Try to download, throws exception if link is dead or doesnt work

                if(linkToTry.depth < maxDepth)                                                      //add links file to queue for more work, and wake everyone up
                {
                    linkToAdd = new Tuple<string, webLink>(num.ToString(), linkToTry);
                    lock(Lock)
                    {
                        producerLinks.Enqueue(linkToAdd);
                        Monitor.PulseAll(Lock);
                    }
                }
            }
            catch (Exception e)                                                                     //link is dead or doesnt respond, add to deadlinks and incriment death count.
            {
                lock (Lock)
                {
                    if(!deadLinks.ContainsKey(linkToTry.link.AbsoluteUri))
                    {
                        linkToTry.exception = e;
                        deadLinks.Add(linkToTry.link.AbsoluteUri, linkToTry);
                        numDeadLinks++;
                    }
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

            visitedLinks    = new Dictionary<string, webLink>();
            deadLinks       = new Dictionary<string, webLink>();
            clientLinks     = new Queue<webLink>();
            producerLinks   = new Queue<Tuple<string, webLink>>();
            Threads         = new List<Thread>();
            Lock            = new object();
            webLink rootLink= new webLink();
            numDeadLinks = 0;
            currentWorkers = 0;
            fileNum = 0;

            rootLink.link = rootAddress;
            rootLink.depth = 0;
            
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

            if (visitedLinks.Count > 0)
            {
                Console.WriteLine("number of links visited: {0}\nnumber of deadLinks:{1}", visitedLinks.Count, numDeadLinks);
                foreach (KeyValuePair<string, webLink> key in deadLinks)
                {
                    Console.WriteLine("DeadLinkOrigin: {0}\nDeadLink: {1}\nDepth: {2}\nException: {3}\n", key.Value.origin, key.Key, key.Value.depth, key.Value.exception.Message);
                }
            }
        }

        
        Console.WriteLine("Done");
        Console.Read();
        

    }
}