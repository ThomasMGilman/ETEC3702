using System;
using System.Threading;
using System.IO;

class MainClass
{
    private static RNG R = new RNG();
    private static StreamWriter outs = new StreamWriter("trace.txt");

    public static void Delay(){  
        System.Threading.Thread.Sleep(R.nextInt(100));
    }

    private static object ol = new object();
    private static void Output(string s){
        lock(ol) {
            Console.WriteLine(s);
            outs.WriteLine(s);
            outs.Flush();
        }
    }

    public static void Main (string[] args)
    {
        int count = 0;
        object L = new object();

        new Thread( () => {
            System.Threading.Thread.Sleep(4000);
            Environment.Exit(0);
        }).Start();
        
        while(true) {
            lock(L) {
                while(count > 10)
                    Monitor.Wait(L);
            }

            if((R.nextInt() & 1) != 0) {
                lock(L) {
                    count++;
                }
                new Thread(() => {
                    Delay();
                    Baboon.onRope();
                    Output("Baboon on rope");
                    Delay();
                    MainClass.Output("Baboon off rope");
                    Baboon.offRope();
                    lock(L){
                        count--;
                        Monitor.Pulse(L);
                    }
                }).Start();
            } else {
                lock(L) {
                    count++;
                }
                new Thread(() => {
                    Delay();
                    Macaque.onRope();
                    Output("Macaque on rope");
                    Delay();
                    MainClass.Output("Macaque off rope");
                    Macaque.offRope();
                    lock(L){
                        count--;
                        Monitor.Pulse(L);
                    }
                }).Start();
            }
        }
    }
}
