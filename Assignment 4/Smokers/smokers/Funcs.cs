using System;
using System.Threading;
using System.IO;

class Funcs
{
    private static RNG R = new RNG();
    private static StreamWriter outs = new StreamWriter("trace.txt");
    
    public static void Delay(){  
        System.Threading.Thread.Sleep(R.nextInt(100));
    }

    private static object ol = new object();
    public static void Output(string s){
        lock(ol) {
            Console.WriteLine(s);
            outs.WriteLine(s);
        }
    }
    
    public static void flushOutput(){
        lock(ol){
            outs.Flush();
        }
    }
}
