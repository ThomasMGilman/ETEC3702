//Thomas Gilman
//James Hudson
//ETEC 3702 OS2
//16th February, 2019
using System.Threading;

public class Macaque
{
    public static void onRope(){
        lock (Globals.RopeLock)
        {
            while (Globals.numMac > 2 || Globals.numBab > 0)
                Monitor.Wait(Globals.RopeLock);
            Globals.numMac++;
        }
    }

    public static void offRope(){
        lock (Globals.RopeLock)
        {
            while (Globals.numMac == 0)
                Monitor.Wait(Globals.RopeLock);

            Globals.numMac--;
            Monitor.Pulse(Globals.RopeLock);
        }
    }
}