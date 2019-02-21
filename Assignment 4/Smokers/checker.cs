using System;
using System.IO;

public class X{
    static int lineNum=0;
    static string lastLine;
    static bool paperOnTable=false;
    static bool tobaccoOnTable=false;
    static bool matchesOnTable=false;
    static bool paperSmoking=false;
    static bool tobaccoSmoking=false;
    static bool matchesSmoking=false;
    static bool tobaccoWants=false;
    static bool paperWants=false;
    static bool matchesWants=false;
    
    static void assert(bool b){
        if(!b){
            Console.WriteLine("Bad state at line "+lineNum);
            Console.WriteLine(lastLine);
            Console.WriteLine("paperOnTable "+paperOnTable);
            Console.WriteLine("tobaccoOnTable "+tobaccoOnTable);
            Console.WriteLine("matchesOnTable "+matchesOnTable);
            Console.WriteLine("paperSmoking "+paperSmoking);
            Console.WriteLine("tobaccoSmoking "+tobaccoSmoking);
            Console.WriteLine("matchesSmoking "+matchesSmoking);
            Console.WriteLine("tobaccoWants "+tobaccoWants);
            Console.WriteLine("paperWants "+paperWants);
            Console.WriteLine("matchesWants "+matchesWants);
            throw new Exception("Error");
        }
    }
    public static void Main(string[] args){
        var lines = File.ReadLines("trace.txt");
        
        foreach(string line in lines){
            lastLine=line;
            ++lineNum;
            switch(line){
                case "Tobacco wants":
                    assert(!tobaccoSmoking);
                    tobaccoWants=true;
                    break;
                case "Paper wants":
                    assert(!paperSmoking);
                    paperWants=true;
                    break;
                case "Matches wants":
                    assert(!matchesSmoking);
                    matchesWants=true;
                    break;
                case "Tobacco smokes":
                    assert(tobaccoWants);
                    assert(!paperSmoking);
                    assert(!matchesSmoking);
                    assert(!tobaccoSmoking);
                    assert(paperOnTable);
                    assert(matchesOnTable);
                    paperOnTable=false;
                    matchesOnTable=false;
                    tobaccoSmoking=true;
                    tobaccoWants=false;
                    break;
                case "Matches smokes":
                    assert(matchesWants);
                    assert(!paperSmoking);
                    assert(!matchesSmoking);
                    assert(!tobaccoSmoking);
                    assert(tobaccoOnTable);
                    assert(paperOnTable);
                    tobaccoOnTable=false;
                    paperOnTable=false;
                    matchesSmoking=true;
                    matchesWants=false;
                    break;
                case "Paper smokes":
                    assert(paperWants);
                    assert(!paperSmoking);
                    assert(!matchesSmoking);
                    assert(!tobaccoSmoking);
                    assert(tobaccoOnTable);
                    assert(matchesOnTable);
                    matchesOnTable=false;
                    tobaccoOnTable=false;
                    paperSmoking=true;
                    paperWants=false;
                    break;
                case "Agent puts matches":
                    assert(!paperSmoking);
                    assert(!matchesSmoking);
                    assert(!tobaccoSmoking);
                    matchesOnTable=true;
                    break;
                case "Agent puts paper":
                    assert(!paperSmoking);
                    assert(!matchesSmoking);
                    assert(!tobaccoSmoking);
                    paperOnTable=true;
                    break;
                case "Agent puts tobacco":
                    assert(!paperSmoking);
                    assert(!matchesSmoking);
                    assert(!tobaccoSmoking);
                    tobaccoOnTable=true;
                    break;
                case "Tobacco done":
                    tobaccoSmoking=false;
                    break;
                case "Matches done":
                    matchesSmoking=false;
                    break;
                case "Paper done":
                    paperSmoking=false;
                    break;
                default:
                    assert(false);
                    break;
            }
        }
        Console.WriteLine(lineNum+" lines OK");
    }
}

