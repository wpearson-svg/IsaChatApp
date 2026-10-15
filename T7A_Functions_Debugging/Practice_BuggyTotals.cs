// T7A PRACTICE — ISA-style metrics (buggy on purpose)
// 1. Refactor repeated blocks into functions
// 2. Fix the bugs using Debug Ritual
// 3. Commit: fix: root cause ...

namespace IsaChatApp.T7A_Practice;

public class BuggyTotals
{
    // BUG 1: repeated logic (refactor into CalculateRate)
    // BUG 2: divides by zero when total is 0
    // BUG 3: wrong variable name on purpose in one place

    public static void Main()
    {
        // Self-service rate (repeated block #1)
        int resolved = 750;
        int totalQ = 1000;
        double rate1 = resolved * 100.0 / totalQ;
        Console.WriteLine("Self-service: " + rate1);

        // Accuracy (repeated block #2 — same pattern)
        int correct = 920;
        int audited = 1000;
        double rate2 = correct * 100.0 / audited;
        Console.WriteLine("Accuracy: " + rate2);

        // BUG: totalTickets is 0 → crash without guard
        int closed = 10;
        int totalTickets = 0;
        double rate3 = closed * 100.0 / totalTickets;  // boom
        Console.WriteLine("Closed rate: " + rate3);

        // BUG: typo variable (uncomment to practice)
        // int questionsAsked = 50;
        // Console.WriteLine(questionsAskd);
    }

    // TODO: add CalculateRate(int part, int whole) with zero-guard
    // TODO: add Log(string level, string message) with timestamp
}
