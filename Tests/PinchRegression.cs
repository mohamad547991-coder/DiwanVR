using System;
using DiwanVR;
public static class PinchRegression
{
    static int passed;
    static void Check(string name,bool value){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);passed++;}
    public static int Main()
    {
        var p=new TrackedPinch();
        Check("Startup with fingers closed does not grab",!p.Update(true,.01f));
        Check("Opening arms without clicking",!p.Update(true,.05f));
        Check("Closing deliberately produces pinch",p.Update(true,.015f));
        bool held=true;for(int i=0;i<100;i++)held &= p.Update(true,i%2==0?.023f:.032f);
        Check("One held pinch survives noisy tracking",held);
        Check("Opening releases immediately",!p.Update(true,.04f));
        Check("Next deliberate pinch works",p.Update(true,.01f));
        Check("Tracking loss releases immediately",!p.Update(false,.01f));
        Check("Reacquired closed fingers do not regrab",!p.Update(true,.01f));
        p.Update(true,.05f);Check("Open then close after recovery works",p.Update(true,.01f));
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1f})
        {
            Check("Invalid distance cancels pinch "+invalid,!p.Update(true,invalid));
            Check("Invalid sample requires rearming "+invalid,!p.Update(true,.01f));
            p.Update(true,.05f);p.Update(true,.01f);
        }
        p.Reset();Check("Explicit reset clears state",!p.IsPinching && !p.Update(true,.01f));
        Console.WriteLine("RESULT: "+passed+" passed; 0 failed (pure C#; no Unity or headset runtime).");
        return 0;
    }
}
