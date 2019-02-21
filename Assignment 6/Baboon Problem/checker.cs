using System;
using System.IO;

class X{
    static int lineNum=0;
    static string lastLine;
    static int nb=0;
    static int nm=0;
    static int maxb=0;
    static int maxm=0;
    static int notb=0;
    static int notm=0;
    static int mstarve=0;
    static int bstarve=0;
    
    static void fail(string msg){
        Console.WriteLine("At line "+lineNum+": "+lastLine);
        Console.WriteLine("Baboons on rope: " +nb);
        Console.WriteLine("Macaques on rope: " +nm);
        Console.WriteLine("Max baboons on rope: " +maxb);
        Console.WriteLine("Max macaques on rope: " +maxm);
        Console.WriteLine(msg);
        throw new Exception("Error");
    }
    
    public static void Main(string[] args){
 
        var lines = File.ReadLines("trace.txt");
        foreach(string line in lines){
            lastLine=line;
            ++lineNum;
            switch(line){
                case "Baboon on rope":
                    nb++;
                    break;
                case "Baboon off rope":
                    nb--;
                    break;
                case "Macaque on rope":
                    nm++;
                    break;
                case "Macaque off rope":
                    nm--;
                    break;
                default:
                    Console.WriteLine("Unexpected: "+line);
                    fail("Unexpected: "+line);
                    break;
            }
                
            if(nb > maxb){
                maxb=nb;
            }
            if(nm > maxm){
                maxm = nm;
            }
            
            if(nb == 0){
                notb++;
            }
            if(nm == 0){
                notm++;
            }
            if(nb > 0){
                notb=0;
            }
            if(nm > 0){
                notm=0;
            }
            
            if(nb !=0 && nm != 0){
                fail("Both kinds on rope at once");
            }
            if(nb > 3 || nm > 3){
                fail("Too many on rope");
            }
            if(nb < 0 || nm < 0){
                fail("Negative number on rope?");
            }
            if(notb > bstarve){
                bstarve = notb;
            }
            if(notm > mstarve){
                mstarve = notm;
            }
        }
        
        if(maxb != 3){
            fail("Never allowed 3 baboons on rope");
        }
        if(maxm != 3){
            fail("Never allowed 3 macaques on rope");
        }
        
        Console.WriteLine("Maxb: "+maxb+" Maxm: "+maxm+" Lines: "+lines+" Bstarve: "+bstarve+
            " Mstarve: "+mstarve);
        
        Console.WriteLine("Done");
    }
}
