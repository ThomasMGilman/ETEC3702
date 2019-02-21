//Thomas Gilman
//James Hudson
//ETEC 3702 OS2
//16th February, 2019
using System.Threading;

public class Baboon
{
    static public void onRope(){
        lock(Globals.RopeLock)
        {
            while(Globals.numBab > 2 || Globals.numMac > 0)
                Monitor.Wait(Globals.RopeLock);
            Globals.numBab++;
        }
    }

    public static void offRope(){
        lock(Globals.RopeLock)
        {
            while(Globals.numBab == 0)
                Monitor.Wait(Globals.RopeLock);

            Globals.numBab--;
            Monitor.Pulse(Globals.RopeLock);
        }
    }
}