using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

class Quicksorter<T>{
    
    private static void swap(T[] A, int i, int j){
        T tmp = A[i];
        A[i] = A[j];
        A[j]=tmp;
    }
    
    public static int active=1;
    public static object lck = new object();
    public static bool done=false;
    
    public static void init(){
        active=1;
        done=false;
    }
    
    public static void qs(T[] A, int L, int R, Func<T,T,int> predicate){
        try{
            if( L+1 >= R )
                return;
            int i = L+1;
            int j = R-1;
            while( i<=j ){
                while(i<=j && predicate(A[i],A[L]) <= 0 )    //A[i] <= A[L]
                    i++;
                while(j>L && predicate(A[j],A[L]) >= 0 )     //A[j] >= A[L] )
                    j--;
                if( i<j )
                    swap(A,i,j);
            }
            swap(A,L,j);
            
            Interlocked.Add(ref active,2);
            if( R-L > 5000 ){
                ThreadPool.QueueUserWorkItem( (si) => { qs(A,L,  j, predicate);     } );
                ThreadPool.QueueUserWorkItem( (si) => { qs(A,j+1,R, predicate);   } );
            }
            else {
                qs(A,L,  j, predicate); 
                qs(A,j+1,R, predicate);
            }
        }
        finally{
            if( Interlocked.Add(ref active,-1) == 0 ){
                lock(lck){
                    done=true;
                    Monitor.Pulse(lck);
                }
            }
        }
    }
}

class Prog{
    public static void Main(string[] args){
        Random R = new Random(42);
        for(int i=10000000;;i+=3){
            int[] A = new int[i];
            int[] B = new int[i];
            for(int j=0;j<i;++j){
                int v = R.Next();
                A[j] = (v);
                B[j] = (v);
            }
            
            Stopwatch sw = new Stopwatch(); 
            sw.Start();


            Quicksorter<int>.init();
            
            Quicksorter<int>.qs(A,0,A.Length, 
                (int x, int y) => { return x-y; }
                );

            lock(Quicksorter<int>.lck){
                while( Quicksorter<int>.done == false )
                    Monitor.Wait(Quicksorter<int>.lck);
            }
            
            sw.Stop();
            Console.WriteLine(sw.Elapsed);

            Array.Sort(B,0,B.Length);
            for(int j=0;j<A.Length;++j){
                if(A[j] != B[j]){
                    throw new Exception("Mismatch");
                }
            }
            if(i>0)
                break;
        }
        Console.WriteLine("OK");
    }
}
