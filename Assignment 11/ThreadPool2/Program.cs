//Thomas Gilman
//James Hudson
//14th March 2019
//ETEC 3702 OS2
//Assignment 11 ThreadPool_2 GUI
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
    static Dictionary<string, webLink> deadLinks, visitedLinks;
    static System.Timers.Timer onTheClock;              //timer for threads to call to tell user they are still working, should do so every 30seconds
    static CancellationTokenSource tokenSource;
    static Stopwatch sw;
    static Uri rootAddress;

    static int maxDepth, numDeadLinks, currentWorkers, fileNum;
    static bool poison = false;

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

    public static void PoisonTheWater()
    {
        lock(Lock)
        {
            poison = true;
            tokenSource.Cancel();
            Monitor.PulseAll(Lock);
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
        Environment.Exit(0);
    }

    public static void startThreadTask(webLink link)
    {
        lock (Lock)
        {
            currentWorkers++;
        }
        Task.Run(() =>
        {
            Consumer(link);
            lock (Lock)
            {
                currentWorkers--;
                if (currentWorkers == 0)    //last worker poisons everyone else
                {
                    PoisonTheWater();
                    return;
                }
            }
        }, tokenSource.Token);
    }

    public static void Producer(Tuple<string, webLink> link)
    {
        //string regMatch = "<\\s*a\\s+[^>]*href\\s*=\\s*['\"][^'\"]*['\"]";
        string regMatch = "href\\s*=\\s*(?:[\"'](?<1>[^\"']*)[\"']|(?<1>\\S+))"; //this regex is provided on the microsoft C# documentation website
        string data, address = "";
        Regex URLmatch = new Regex(regMatch);
        webLink newLink;
        Uri newAddress;

        try
        {
            //Check the Queue for contents, else wait or die if poisoned
            lock (Lock)
            {
                if (poison)
                    return;
            }

            //read the entire document and match all instances of an address to a collention
            data = File.ReadAllText(link.Item1);
            if (data.Length > 0)
            {
                foreach (Match m in URLmatch.Matches(data))
                {
                    if (tokenSource.IsCancellationRequested)
                        tokenSource.Token.ThrowIfCancellationRequested();

                    int testEqPos = m.Value.IndexOf('=');
                    address = m.Value.Substring(m.Value.IndexOf('=') + 1).Trim();       //get the address after the = and href
                    address = address.Substring(1, address.Length - 2);                 //remove the " " from the address

                    if (address.Contains(link.Item2.link.Host) || address.StartsWith("http") || address.StartsWith("https"))
                        newAddress = new Uri(address);
                    else
                        newAddress = new Uri(link.Item2.link, address);

                    if (newAddress.Host == link.Item2.link.Host)
                    {
                        newLink = new webLink();
                        newLink.link = newAddress;
                        newLink.origin = link.Item2.link;
                        newLink.depth = link.Item2.depth + 1;

                        lock (Lock)
                        {
                            if (!visitedLinks.ContainsKey(newAddress.AbsoluteUri) && !deadLinks.ContainsKey(newAddress.AbsoluteUri))
                            {
                                visitedLinks.Add(newAddress.AbsoluteUri, newLink);
                                startThreadTask(newLink);
                            }
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException e)
        {
            PoisonTheWater();
            return;
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
    }

    public static void Consumer(webLink linkToTry)
    {
        HttpClient Client = new HttpClient();
        FileStream newFile;
        int num;
        string FileName;

        try
        {
            lock (Lock)
            {
                if (poison)
                    return;

                num = fileNum++;                                                            //increment file name for link
            }

            var result = Client.GetAsync(linkToTry.link.AbsoluteUri, tokenSource.Token);  //Try to download, throws exception if link is dead or doesnt work            
            if(tokenSource.IsCancellationRequested)
                tokenSource.Token.ThrowIfCancellationRequested();

            if(result.Result.IsSuccessStatusCode)           //Http connection success
            {
                if (linkToTry.link.IsFile)
                    FileName = Path.GetFileName(linkToTry.link.LocalPath) + '_' + num.ToString();
                else
                    FileName = num.ToString();
                newFile = new FileStream(FileName, FileMode.Create);
                byte[] webData = result.Result.Content.ReadAsByteArrayAsync().Result;

                newFile.Write(webData, 0, webData.Length);
                newFile.Close();

                if (linkToTry.depth < maxDepth)                                                 //start producing more consumer tasks if maxDepth not reached
                    Producer(new Tuple<string, webLink>(num.ToString(), linkToTry));
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
        catch(OperationCanceledException e)
        {
            PoisonTheWater();
            return;
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
        Lock                = new object();
        
        sw                  = new Stopwatch();
        tokenSource         = new CancellationTokenSource();
        
        numDeadLinks        = 0;
        currentWorkers      = 0;
        fileNum             = 0;

        Application.Run(new Program());
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
                PoisonTheWater();
                Monitor.PulseAll(Lock);
            }
            this.Close();
        };
        b.Left      = b.Parent.ClientSize.Width / 2 - b.Width / 2;
        b.Top       = b.Parent.ClientSize.Height / 2 - b.Height / 2;
        this.Show();

        webLink rootLink = new webLink();
        rootLink.link = rootAddress;
        rootLink.origin = rootAddress;
        rootLink.depth = 0;

        //start thread pool and wait till finish
        Console.WriteLine("Working...\n");
        setTimer();
        sw.Start();
        startThreadTask(rootLink);
        return;
    }
}