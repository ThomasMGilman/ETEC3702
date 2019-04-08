using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Drawing;

class MainClass : Form
{
    const int Port = 8000;
    const string serverIP = "127.0.0.1";
    bool done = false;
    ClientInfo client;

    public static void Main()
    {
        Application.Run(new MainClass());
    }

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

    static async Task sendMsg(string msg, ClientInfo client)
    {
        await client.W.WriteLineAsync(msg);
        await client.W.FlushAsync();
    }

    static async Task<object> getMessage(ClientInfo c)
    {
        return await c.R.ReadLineAsync();
    }

    private void Form_Closing(object sender, FormClosingEventArgs e)
    {
        closeConnection();
    }

    private void closeConnection()
    {
        done = true;
        sendMsg("EXIT", client);
        client.c.GetStream().Close();
        client.c.Close();
        Environment.Exit(0);
    }

    public MainClass()
    {
        TcpClient c = new TcpClient(serverIP, Port);
        client = new ClientInfo(c);
        

        this.Size = new Size(350, 400);
        this.CenterToScreen();

        var textEntry = new TextBox();
        textEntry.Multiline = true;
        textEntry.ScrollBars = ScrollBars.Both;
        textEntry.Parent = this;
        textEntry.Dock = DockStyle.Fill;
        textEntry.ReadOnly = true;
        textEntry.Text = "Welcome to Datcord!\n";

        var pan2 = new TableLayoutPanel();
        pan2.RowCount = 1;
        pan2.ColumnCount = 2;
        pan2.AutoSize = true;
        pan2.Parent = this;
        pan2.Dock = DockStyle.Top;

        var lineEntry = new TextBox();
        lineEntry.Size = new Size(250, lineEntry.Size.Height);
        lineEntry.Dock = DockStyle.Left;
        lineEntry.Parent = pan2;

        var button = new Button();
        button.Text = "Send";
        button.Parent = pan2;
        button.Dock = DockStyle.Right;
        button.Click += (s, e) => {
            sendMsg(lineEntry.Text, client);
            lineEntry.Text = "";
            this.ActiveControl = lineEntry;
        };

        MainMenu mbar = new MainMenu();
        MenuItem file = new MenuItem("File");
        MenuItem quit = new MenuItem("Quit", (s, e) => {
            closeConnection();
        });
        file.MenuItems.Add(quit);
        mbar.MenuItems.Add(file);
        this.Menu = mbar;

        this.ActiveControl = lineEntry;
        this.AcceptButton = button;

        var msgTsk = new Task(() =>
        {
            while(!done)
            {
                var msg = getMessage(client);
                string message = msg.Result.ToString();
                try
                {
                    this.Invoke((MethodInvoker)delegate ()
                    {
                        textEntry.AppendText(message + Environment.NewLine);
                    });
                }
                catch(Exception)
                {
                    return;
                }
            }
        });
        msgTsk.Start();
    }
}