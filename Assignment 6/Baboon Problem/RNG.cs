using System;
using System.Threading;

public class RNG
{
    //xorshift
    private int state;
    private SpinLock L = new SpinLock();

    public RNG(int init=42)
    {
        state = init;
    }
    public int nextInt(int max){
        int v = nextInt();
        Int64 vv = v;
        vv *= max;
        vv /= System.Int32.MaxValue;
        if( vv >= max )
            vv = max-1;
        return (int)vv;
    }
    public int nextInt(){
        bool taken = false;
        try{
            while(!taken)
                L.Enter(ref taken);
            state ^= (state << 13);
            state ^= (state >> 17);
            state ^= (state << 15);
            return state & System.Int32.MaxValue;
        } finally {
            if(taken)
                L.Exit(true);
        }
    }
}

