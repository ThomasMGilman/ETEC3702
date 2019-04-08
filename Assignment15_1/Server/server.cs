using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;

class ClientInfo
{
    public TcpClient c;
    public StreamReader R;
    public StreamWriter W;
    public ClientInfo(TcpClient c)
    {
        this.c = c;
        var strm = c.GetStream();
        this.R = new StreamReader(strm);
        this.W = new StreamWriter(strm);
    }
}

class MainClass
{
    const int Port = 8000;
    const int clientCount = 8;
    static int numAccepts;
    static IPAddress localAdd = IPAddress.Parse("127.0.0.1");

    static async Task<object> accept(TcpListener srv)
    {
        var X = await srv.AcceptTcpClientAsync();
        Console.WriteLine("ACCEPTED!");
        return X;
    }

    static async Task<object> getMessage(ClientInfo c)
    {
        var X = await c.R.ReadLineAsync();
        Console.WriteLine("GOTTEN!");
        return X;

    }
    static async Task<bool> sendMessage(ClientInfo c, string s)
    {
        Console.WriteLine("SEND? " + s);
        try
        {
            await c.W.WriteLineAsync(s);
            await c.W.FlushAsync();
            Console.WriteLine("SENT!");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    static void disconnectClient(ClientInfo c)
    {
        c.c.Close();
    }

    static void attemptSend(ref List<ClientInfo> clients, string s)
    {
        List<ClientInfo> unResponsive = new List<ClientInfo>();
        foreach (var client in clients)
        {
            if (!sendMessage(client, s).Result)
                unResponsive.Add(client);
        }
        if (unResponsive.Count > 0)
        {
            foreach (var client in unResponsive)
            {
                disconnectClient(client);
                clients.Remove(client);
                numAccepts++;
            }
        }
    }

    public static void Main(string[] args)
    {
        var srv = new TcpListener(localAdd, Port);
        numAccepts = clientCount;
        srv.Start();
        var clients = new List<ClientInfo>();
        var T = new List<Task<object>>();
        T.Add(accept(srv));
        while (true)
        {
            int idx = Task.WaitAny(T.ToArray());
            if (T[idx].IsFaulted)
            {
                if (clients.Count > 1)
                    attemptSend(ref clients, "User Left Chat!!");
                else
                {
                    disconnectClient(clients[0]);
                    clients.Remove(clients[0]);
                    numAccepts++;
                }
            }
            else
            {
                if (idx == 0)
                {
                    object ob = T[0].Result;
                    TcpClient cl = ob as TcpClient;
                    var cinfo = new ClientInfo(cl);
                    clients.Add(cinfo);
                    T[0] = accept(srv);
                    T.Add(getMessage(cinfo));
                }
                else
                {
                    object ob = T[idx].Result;
                    string s = ob as string;
                    attemptSend(ref clients, s);
                    T[idx] = getMessage(clients[idx - 1]);
                    //Console.WriteLine(T[idx].Result);
                }
            }
            if (numAccepts > 0)
            {
                T.Add(accept(srv));
                numAccepts--;
            }
        }
    }
}