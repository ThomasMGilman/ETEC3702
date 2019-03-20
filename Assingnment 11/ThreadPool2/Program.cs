//Thomas Gilman
//James Hudson
//14th March 2019
//ETEC 3702 OS2
//Assignment 10 ThreadPool
using System;
using System.IO;
using System.Timers;
using System.Drawing;
using System.Net.Http;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;


class Program : Form
{
    public struct webLink
    {
        public Uri link;
        public Uri origin;
        public string exception;
        public int depth;
    }

    static object Lock;
    static Dictionary<string, webLink> deadLinks;
    static Dictionary<string, webLink> visitedLinks;
    static Dictionary<string, webLink> quedLinks;
    static Queue<webLink> clientLinks;                  //links to test, should try to download
    static Queue<Tuple<string, webLink>> producerLinks; //links to parse for more links
    static System.Timers.Timer onTheClock;              //timer for threads to call to tell user they are still working, should do so every 30seconds
    static CancellationTokenSource tokenSource;
    static Stopwatch sw;
    static Uri rootAddress;

    static int maxDepth = 0;
    static int numDeadLinks = 0;
    static int currentWorkers = 0;
    static int fileNum = 0;
    static bool poison = false;

    public static void PoisonTheWater()
    {
        lock(Lock)
        {
            poison = true;
            tokenSource.Cancel();
            Monitor.PulseAll(Lock);
        }
    }

    public static void Producer()
    {
        //string regMatch = "<\\s*a\\s+[^>]*href\\s*=\\s*['\"][^'\"]*['\"]";
        string regMatch = "href\\s*=\\s*(?:[\"'](?<1>[^\"']*)[\"']|(?<1>\\S+))"; //this regex is provided on the microsoft C# documentation website
        string data, address = "";
        Regex URLmatch = new Regex(regMatch);
        MatchCollection MC;
        Tuple<string, webLink> link = null;         //fileName, weblink struct
        webLink newLink;
        Uri newAddress;

        try
        {
            //Check the Queue for contents, else wait or die if poisoned
            lock (Lock)
            {
                if (producerLinks.Count > 0 && !poison)
                {
                    link = producerLinks.Dequeue();
                    if (visitedLinks.ContainsKey(link.Item2.link.AbsoluteUri))
                    {
                        PoisonTheWater();
                        throw new Exception("Error: Link already added to Dictionary!!! Why is it in Queue!?");
                    }
                    visitedLinks.Add(link.Item2.link.AbsoluteUri, link.Item2);
                    currentWorkers++;
                }
                else
                {
                    PoisonTheWater();
                    return;
                }
            }

            //read the entire document and match all instances of an address to a collention
            data = File.ReadAllText(link.Item1);
            if (data.Length > 0)
            {
                MC = URLmatch.Matches(data);

                foreach (Match m in MC)
                {
                    int testEqPos = m.Value.IndexOf('=');
                    if (testEqPos > 0)
                    {
                        address = m.Value.Substring(m.Value.IndexOf('=') + 1).Trim();    //get the address after the = and href
                        if (address.Length > 3)
                        {
                            address = address.Substring(1, address.Length - 2);                 //remove the " " from the address

                            if (address.Contains(link.Item2.link.Host) || address.StartsWith("http") || address.StartsWith("https"))
                                newAddress = new Uri(address);
                            else
                                newAddress = new Uri(link.Item2.link, address);

                            if(newAddress.Host == link.Item2.link.Host)
                            {
                                newLink = new webLink();
                                newLink.link = newAddress;
                                newLink.origin = link.Item2.link;
                                newLink.depth = link.Item2.depth + 1;

                                lock (Lock)
                                {
                                    if (!visitedLinks.ContainsKey(newLink.link.AbsoluteUri) && !deadLinks.ContainsKey(newLink.link.AbsoluteUri) && !quedLinks.ContainsKey(newLink.link.AbsoluteUri))
                                    {
                                        quedLinks.Add(newLink.link.AbsoluteUri, newLink);
                                        clientLinks.Enqueue(newLink);
                                        ThreadPool.QueueUserWorkItem((si) => Consumer(), tokenSource.Token);
                                        //Task.Run(() => Consumer(), tokenSource.Token);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            lock (Lock)
            {
                Console.WriteLine("Error!!! This shouldnt happen; Trying to Read from File:'{0}' for : '{1}'\nAddress:'{2}', Origin:'{3}'\nException: '{4}'\n"
                    , link.Item1, link.Item2.link.AbsoluteUri, address, link.Item2.link.AbsoluteUri, e.Message);
                PoisonTheWater();
                return;
            }
        }
        lock (Lock)
        {
            currentWorkers--;
            if (clientLinks.Count == 0 && producerLinks.Count == 0 && currentWorkers == 0) //last worker poisons everyone else
            {
                PoisonTheWater();
                return;
            }
        }
    }

    public static void Consumer()
    {
        HttpClient Client = new HttpClient();
        webLink linkToTry = new webLink();
        Tuple<string, webLink> linkToAdd = null;
        FileStream newFile;
        int num;
        string FileName;

        try
        {
            lock (Lock)
            {
                if (clientLinks.Count > 0 && !poison)
                {
                    linkToTry = clientLinks.Dequeue();                                          //no snack, just work, get item to work on
                    currentWorkers++;                                                           //incriment workers
                    num = fileNum++;                                                            //increment file name for link
                }
                else
                    return;
            }

            var result = Client.GetAsync(linkToTry.link.AbsoluteUri, tokenSource.Token);        //Try to download, throws exception if link is dead or doesnt work                 
            if(result.Result.IsSuccessStatusCode)
            {
                if (linkToTry.link.IsFile)
                    FileName = Path.GetFileName(linkToTry.link.LocalPath) + '_' + num.ToString();
                else
                    FileName = num.ToString();
                newFile = new FileStream(FileName, FileMode.Append);
                byte[] webData = result.Result.Content.ReadAsByteArrayAsync().Result;

                newFile.Write(webData, 0, webData.Length);
                newFile.Close();

                if (linkToTry.depth < maxDepth)                                                     //add links file to queue for more work, and wake everyone up
                {
                    if (linkToTry.link.Host == linkToTry.origin.Host)
                    {
                        linkToAdd = new Tuple<string, webLink>(FileName, linkToTry);
                        lock (Lock)
                        {
                            producerLinks.Enqueue(linkToAdd);
                        }
                        ThreadPool.QueueUserWorkItem((si) => Producer(), tokenSource.Token);
                        //Task.Run(() => Producer(), tokenSource.Token);
                    }
                }
            }
            else                    //link is dead or doesnt respond, add to deadlinks and incriment death count.
            {
                lock (Lock)
                {
                    if (!deadLinks.ContainsKey(linkToTry.link.AbsoluteUri))
                    {
                        linkToTry.exception = result.Result.StatusCode.ToString();
                        deadLinks.Add(linkToTry.link.AbsoluteUri, linkToTry);
                        numDeadLinks++;
                    }
                }
            }
        }
        catch (Exception e)                                                                     
        {
            lock (Lock)
            {
                if (!deadLinks.ContainsKey(linkToTry.link.AbsoluteUri))
                {
                    linkToTry.exception = e.Message.ToString();
                    deadLinks.Add(linkToTry.link.AbsoluteUri, linkToTry);
                    numDeadLinks++;
                }
            }
        }
        lock (Lock)
        {
            currentWorkers--;
            if (clientLinks.Count == 0 && producerLinks.Count == 0 && currentWorkers == 0) //last worker poisons everyone else
            {
                PoisonTheWater();
                return;
            }
        }
    }

    //Timer Elapsed lets user know the application is still working every 30 seconds
    private static void OnTimedEvent(Object source, ElapsedEventArgs e)
    {
        lock (Lock)
        {
            Console.WriteLine("Working...\n");
        }
    }

    public static void setTimer()
    {
        onTheClock = new System.Timers.Timer(30000);
        onTheClock.Elapsed += OnTimedEvent;
        onTheClock.AutoReset = true;
        onTheClock.Enabled = true;
    }

    //Print error and Exit if exception on startup
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
        rootAddress = null;
        bool gotDistance = false;

        if (args.Length != 0)
        {
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

                gotDistance = Int32.TryParse(args[1], out maxDepth);
                if (gotDistance == false)
                {
                    ErrorMessageExit("Please Input a integer for distance!");
                }
            }


        }
        else if (rootAddress == null)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "All files|*.*";
            dlg.ShowDialog();
            rootAddress = new Uri(dlg.FileName.Trim());
            if (rootAddress == null)
                return;
            dlg.Dispose();

            maxDepth = 1;
        }
        else
            ErrorMessageExit("Please Input a URL string!");

        if (gotDistance == false)
        {
            maxDepth = 1;
            //ErrorMessageExit("Please Input a integer for distance!");
        }
        if (maxDepth < 0)
            ErrorMessageExit("Please Input a positive integer for distance!");

        //INITIALLIZE EVERYTHING
        visitedLinks        = new Dictionary<string, webLink>();
        deadLinks           = new Dictionary<string, webLink>();
        quedLinks           = new Dictionary<string, webLink>();
        clientLinks         = new Queue<webLink>();
        producerLinks       = new Queue<Tuple<string, webLink>>();
        Lock                = new object();
        webLink rootLink    = new webLink();
        sw                  = new Stopwatch();
        tokenSource         = new CancellationTokenSource();
        
        numDeadLinks        = 0;
        currentWorkers      = 0;
        fileNum             = 0;

        rootLink.link       = rootAddress;
        rootLink.origin     = rootAddress;
        rootLink.depth      = 0;

        clientLinks.Enqueue(rootLink);
        setTimer();

        Task winTask = new Task(() => Application.Run(new Program()), tokenSource.Token);
        winTask.Start();

        //start thread pool and wait till finish
        Console.WriteLine("Working...\n");
        sw.Start();
        Task.Run(() => Consumer(), tokenSource.Token);

        lock (Lock)
        {
            while (!poison)
                Monitor.Wait(Lock);
        }

        //get ride of timer and stop stopwatch
        sw.Stop();
        onTheClock.Stop();
        onTheClock.Dispose();

        //print out the bad links
        if (visitedLinks.Count > 0)
        {
            foreach (KeyValuePair<string, webLink> key in deadLinks)
            {
                Console.WriteLine("DeadLinkOrigin: {0}\nDeadLink: {1}\nDepth: {2}\nException: {3}\n", key.Value.origin, key.Key, key.Value.depth, key.Value.exception);
            }
            Console.WriteLine("number of links visited: {0}\nnumber of deadLinks: {1}\nElapsedTime: {2}", visitedLinks.Count, numDeadLinks, sw.Elapsed);
        }
        else
            Console.WriteLine("No Links Visited, RootAddressGiven: '{0}', DepthGiven: '{1}'", rootAddress, maxDepth);

        Console.WriteLine("Done");
        Console.Read();
    }

    Program()
    {
        this.Size   = new Size(300, 200);
        var b       = new Button();
        b.Parent    = this;
        b.Text      = "Cancel";
        b.Anchor    = AnchorStyles.None;
        b.Click     += (object sender, EventArgs e) =>
        {
            tokenSource.Cancel();
            lock(Lock)
            {
                poison = true;
                Monitor.PulseAll(Lock);
            }
            this.Close();
        };
        b.Left      = b.Parent.ClientSize.Width / 2 - b.Width / 2;
        b.Top       = b.Parent.ClientSize.Height / 2 - b.Height / 2;
        this.Show();
    }
}