using System;

public interface Smoker
{
    void wants();
    void done();
}

class PaperSmoker : Smoker {
    public void wants(){
    }
    public void done(){
    }
}
class TobaccoSmoker : Smoker {
    public void wants(){
    }
    public void done(){
    }
}
class MatchSmoker : Smoker {
    public void wants(){
    }
    public void done(){
    }
}
