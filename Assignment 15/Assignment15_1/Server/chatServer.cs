using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;

namespace chatserver
{
    class ClientInfo{
        public TcpClient c;
        public StreamReader R;
        public StreamWriter W;
        public ClientInfo(TcpClient c){
            this.c = c;
            var strm = c.GetStream();
            this.R = new StreamReader(strm);
            this.W = new StreamWriter(strm);
        }
    }

    class MainClass {
        static async Task<object> accept(TcpListener srv){
            var X =  await srv.AcceptTcpClientAsync();
            Console.WriteLine("ACCEPTED!");
            return X;
        }

        static async Task<object> getMessage(ClientInfo c){
            var X = await c.R.ReadLineAsync();
            Console.WriteLine("GOTTEN!");
            return X;

        }
        static async void sendMessage( ClientInfo c, string s){
            Console.WriteLine("SEND? " + s);
            await c.W.WriteLineAsync(s);
            await c.W.FlushAsync();
            Console.WriteLine("SENT!");
        }

        public static void Main(string[] args)
        {
            const int Port = 8000;
            const string serverIP = "127.0.0.1";
            var srv = new TcpListener(new IPEndPoint(IPAddress.Parse(serverIP), Port));   
            srv.Start();
            var clients = new List<ClientInfo>();
            var T = new List<Task<object>>();
            T.Add( accept(srv) );
            while(true) {
                int idx = Task.WaitAny(T.ToArray());
                if(idx == 0) {
                    object ob = T[0].Result;
                    TcpClient cl = ob as TcpClient;
                    var cinfo = new ClientInfo(cl);
                    clients.Add(cinfo);
                    T[0] = accept(srv);
                    T.Add(getMessage(cinfo));
                } else {
                    object ob = T[idx].Result;
                    string s = ob as string;
                    foreach(var client in clients) {
                        sendMessage(client, s);
                    }
                    T[idx] = getMessage(clients[idx-1]);
                }
            }
        }
    }
}
