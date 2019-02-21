using System;
using System.Threading;

class MainClass
{
    private static void smoker(string who, Smoker sm){
        while(true) {
            Funcs.Delay();
            Funcs.Output(who + " wants");
            sm.wants();
            Funcs.Output(who + " smokes");
            Funcs.Delay();
            Funcs.Output(who + " done");
            sm.done();
        }
    }

    private delegate void putFunc();   

    public static void Main (string[] args)
    {

        Smoker paper = new PaperSmoker();
        Smoker tob = new TobaccoSmoker();
        Smoker matches = new MatchSmoker();
        Agent agent = new Agent();

        RNG rng = new RNG();

        putFunc pp = (() => { agent.putPaper(); });
        putFunc pm = (() => { agent.putMatches(); });
        putFunc pt = (() => { agent.putTobacco(); });

        new Thread(() => {
            smoker("Paper", paper);
        }).Start();
        new Thread(() => {
            smoker("Tobacco", tob);
        }).Start();
        new Thread(() => {
            smoker("Matches", matches);
        }).Start();
        new Thread(() => {

            //pt pm tp tm mp mt
            Tuple<putFunc,putFunc,string,string>[] X = new Tuple<putFunc, putFunc,string,string>[6]{
                new Tuple<putFunc,putFunc,string,string>(pp,pt,"paper","tobacco"),
                new Tuple<putFunc,putFunc,string,string>(pp,pm,"paper","matches"),
                new Tuple<putFunc,putFunc,string,string>(pt,pp,"tobacco","paper"),
                new Tuple<putFunc,putFunc,string,string>(pt,pm,"tobacco","matches"),
                new Tuple<putFunc,putFunc,string,string>(pm,pp,"matches","paper"),
                new Tuple<putFunc,putFunc,string,string>(pm,pt,"matches","tobacco")
            };

            while(true) {
                agent.waitForSignal();
                Funcs.Delay();
                int which = rng.nextInt(X.Length);
                var tmp = X[which];
                Funcs.Output("Agent puts "+tmp.Item3);
                tmp.Item1();
                Funcs.Delay();
                Funcs.Output("Agent puts "+tmp.Item4);
                tmp.Item2();
                Funcs.Delay();
            }
        }).Start();
        
        System.Threading.Thread.Sleep(4000);
        System.Environment.Exit(0);
    }
}
