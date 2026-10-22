using System.Text.RegularExpressions;
using IsaChatApp.Services.Orchestrator;

namespace IsaChatApp.Services.ShowTheWork;

/// <summary>
/// "Show the work" (requested by Willis): turns each pipeline stage the
/// orchestrator recorded (QueryContext.WorkSteps) into one plain sentence a
/// chapter leader can read, like "Checked your question against Isa's rules",
/// and sums the whole run up in one line, like "Answered from approved content".
///
/// Every sentence comes from a fixed list below, picked by the stage name and
/// its result. Nothing here is written by the AI model, and nothing shows the
/// question text, the leader's user name or roles, or the model's own
/// reasoning: only what each stage did and how it went.
///
/// Design rules (agreed 20 October 2026, see ISA-PROJECT-CONTEXT.md):
/// - Green = passed. Blue = didn't pass but was handled (stopped, passed to a
///   person, or something went wrong on our side).
/// - Leaders see only the steps that ran. A step Isa didn't use (for example
///   the AI model, when ISACA's own text already answers the question) is not
///   mentioned, so no one wonders whether they missed a "better" answer.
/// - Gray (skipped) and red (a real error) appear in developer mode only.
///
/// When a stage changes what it reports, or a new stage is added to the stage
/// contract (docs/stage-contract.md), add or adjust its sentence here.
/// </summary>
public static class WorkStepText
{
    /// <summary>How a step went. The view picks its color and icon from this.</summary>
    public enum StepState
    {
        Passed,     // green check
        NotFound,   // blue "?": a search found nothing (handled, not an error)
        Stopped,    // blue "no entry": Isa declined, as designed
        ToPerson,   // blue person: handed to a person
        Problem,    // a real error: blue "try again" for leaders, red in developer mode
        Skipped,    // gray dash: developer mode only
    }

    /// <summary>One line in the step list.</summary>
    /// <param name="Text">The plain sentence, e.g. "Searched ISACA's approved content".</param>
    /// <param name="Detail">An optional second line, e.g. "Found the answer in ISACA's approved text".</param>
    public record StepLine(WorkStep Step, StepState State, string Text, string? Detail);

    /// <summary>The one-line summary of a whole run, shown when the list is closed.</summary>
    /// <param name="Meta">Small text on the right, e.g. "6 checks" or "TKT-0001".</param>
    public record Outcome(StepState State, string Text, string? Meta);

    // The stages in the order the stage contract runs them (ticketing only on Escalate).
    private static readonly string[] Order =
    {
        "front_end", "access_control", "translation", "input_compliance",
        "database_search", "knowledge_graph", "rag", "complex_ai", "output_compliance",
    };

    /// <summary>The lines to show. Leaders see only steps that ran and matter to them; developers see every stage.</summary>
    public static IReadOnlyList<StepLine> Lines(IReadOnlyList<WorkStep> steps, bool developer)
    {
        var directHit = IsDirectHit(steps);
        var lines = steps.Select(s => Describe(s, directHit)).ToList();
        if (developer) return lines;

        return lines.Where(l =>
            l.State != StepState.Skipped &&
            // These two always pass and tell a leader nothing new.
            !(l.State == StepState.Passed && l.Step.Stage is "front_end" or "translation"))
            .ToList();
    }

    /// <summary>The summary line for a finished run.</summary>
    public static Outcome Summarize(IReadOnlyList<WorkStep> steps, double totalMs, bool developer)
    {
        var time = FormatMs(totalMs);

        if (steps.Any(IsError))
            return new(StepState.Problem, Wording.ClosedWentWrong, developer ? time : null);

        if (steps.Any(s => s.Outcome == "escalate"))
        {
            var ticket = steps.Select(s => Match(s.DataText, @"\bticket=(TKT-\d+)")).FirstOrDefault(t => t is not null);
            return new(StepState.ToPerson, Wording.ClosedHandoff, developer ? time : ticket);
        }

        var stop = steps.FirstOrDefault(s => s.Outcome == "stop");
        if (stop is not null)
            return new(StepState.Stopped,
                stop.Stage == "access_control" ? Wording.ClosedNoSignIn : Wording.ClosedOutsideScope,
                developer ? time : null);

        var checks = Lines(steps, developer: false).Count;
        var bestPractice = steps.Any(s => s.Stage == "database_search" && s.DataText.StartsWith("best_practice"));
        return new(StepState.Passed, bestPractice ? Wording.ClosedAnsweredBestPractice : Wording.ClosedAnswered,
            developer ? time : $"{checks} checks");
    }

    /// <summary>What Isa is doing right now, for the live status line, e.g. "Searching ISACA's approved content".</summary>
    public static string Running(IReadOnlyList<WorkStep> finished)
    {
        if (finished.Any(s => s.Outcome == "escalate")) return Wording.Running("ticketing");
        var last = finished.LastOrDefault();
        var next = last is null ? 0 : Array.IndexOf(Order, last.Stage) + 1;
        return next < Order.Length ? Wording.Running(Order[next]) : Wording.RunningFinishing;
    }

    /// <summary>What Isa is doing right now, from the stage a "started" update names.</summary>
    public static string RunningStage(string stage) => Wording.Running(stage);

    /// <summary>How many stages this run will have, for "Step 5 of 9".</summary>
    public static int TotalStages(IReadOnlyList<WorkStep> finished) =>
        Order.Length + (finished.Any(s => s.Outcome == "escalate") ? 1 : 0);

    /// <summary>How one step went, by its result. A stage that crashed is a Problem, not a Stop.</summary>
    public static StepState StateOf(WorkStep step) =>
        IsError(step) ? StepState.Problem
        : step.Skipped || DidNothing(step) ? StepState.Skipped
        : step.Outcome == "not_found" ? StepState.NotFound
        : step.Outcome == "stop" ? StepState.Stopped
        : step.Outcome == "escalate" ? StepState.ToPerson
        : StepState.Passed;

    private static StepLine Describe(WorkStep step, bool directHit)
    {
        var state = StateOf(step);
        StepLine L(string text, string? detail = null) => new(step, state, text, detail);

        // A stage that crashed (the orchestrator records error=<type>).
        if (state == StepState.Problem)
            return L(Wording.Doing(step.Stage), Wording.WentWrongHere);

        // Developer mode only: a stage passed over, with why.
        if (state == StepState.Skipped && step.Skipped)
        {
            var why = step.DataText == "escalated" ? Wording.NotNeededEscalated
                : step.Stage == "translation" ? Wording.NotNeededEnglish
                : Wording.NotNeededDirectHit;
            return L(Wording.Skipped(step.Stage), why);
        }

        switch (step.Stage)
        {
            case "front_end":
                return L(Wording.StepFrontEnd);

            case "access_control":
                return state == StepState.Stopped
                    ? L(Wording.StepAccessStopped,
                        step.DataText.Contains("no_chat_user_role") ? Wording.SignInStoppedNoAccess : Wording.SignInStoppedNotSignedIn)
                    : L(Wording.StepAccess);

            case "translation":
                return L(Wording.StepTranslation);

            case "input_compliance":
                return state switch
                {
                    StepState.Stopped => L(Wording.StepRules, Wording.RulesStopped),
                    StepState.ToPerson => L(Wording.StepRules, Wording.RulesToPerson),
                    _ => L(Wording.StepRules),
                };

            case "database_search":
            {
                var count = Number(step.DataText, "count");
                return L(Wording.StepSearch,
                    step.DataText.StartsWith("direct_hit") ? Wording.FoundAnswer
                    : step.DataText.StartsWith("best_practice") ? Wording.FoundBestPractice
                    : step.DataText.StartsWith("meaning_map") ? Wording.FoundTopic(count ?? 0)
                    : step.DataText.StartsWith("no content db") ? Wording.ContentNotLoaded
                    : Wording.FoundNothing);
            }

            case "knowledge_graph":
                return L(Wording.StepRelated);

            case "rag":
                if (state == StepState.ToPerson) return L(Wording.StepPassages, Wording.AnswerToPersonNoContent);
                if (step.DataText.StartsWith("no_context")) return L(Wording.StepPassages, Wording.NothingToGather);
                return L(Wording.StepPassages, Number(step.DataText, "passages") is int p ? Wording.Sections(p) : null);

            case "complex_ai":
                if (state == StepState.ToPerson)
                    return L(Wording.StepDivingDeeper,
                        step.Reason == "no approved content found" ? Wording.AnswerToPersonNoContent : Wording.AnswerToPersonCheckFailed);
                if (Number(step.DataText, "passages") == 0) return L(Wording.StepDivingDeeper, Wording.DivingDeeperNothing);
                return L(Wording.StepDivingDeeper, Wording.DivingDeeperDone);

            case "output_compliance":
            {
                if (state == StepState.ToPerson)
                    return L(Wording.StepAnswerCheck,
                        step.Reason == "no approved content found" ? Wording.AnswerToPersonNoContent
                        : step.Reason == "answer has no approved source" ? Wording.AnswerToPersonNoSource
                        : Wording.AnswerToPersonCheckFailed);
                var backed = Number(step.DataText, "sources") is int s ? Wording.BackedBy(s) : null;
                return L(Wording.StepAnswerCheck,
                    directHit ? Wording.ExactWording + (backed is null ? "" : " · " + backed)
                    : Capitalize(backed));
            }

            case "ticketing":
                return L(Wording.StepTicket, Match(step.DataText, @"\bticket=(TKT-\d+)") is string t ? Wording.Reference(t) : null);

            default:
                // A stage this file doesn't know yet: still show it, honestly.
                return L($"Ran the {ShortName(step.Stage)} step", step.Reason);
        }
    }

    // A stage that crashed: the orchestrator turns the exception into a Stop with data text error=<type>.
    private static bool IsError(WorkStep step) => step.DataText.StartsWith("error=");

    // A stage that ran but had nothing to work with (nothing was found to
    // gather or write from). Like a skipped stage, Isa didn't really use it,
    // so leaders aren't told about it; developers see it in gray.
    private static bool DidNothing(WorkStep step) =>
        step.Outcome == "pass" &&
        ((step.Stage == "rag" && step.DataText.StartsWith("no_context")) ||
         (step.Stage == "complex_ai" && Number(step.DataText, "passages") == 0));

    // True when ISACA's own text answered the question word for word.
    private static bool IsDirectHit(IReadOnlyList<WorkStep> steps) =>
        steps.Any(s => s.Stage == "database_search" && s.DataText.StartsWith("direct_hit"));

    public static string FormatMs(double ms) => ms < 1000 ? $"{ms:0}ms" : $"{ms / 1000:0.0}s";

    private static string ShortName(string stage) => stage.Replace('_', ' ');

    private static string? Capitalize(string? s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];

    // Reads a number like "count=3" out of a stage's data text.
    private static int? Number(string dataText, string key) =>
        Match(dataText, $@"\b{Regex.Escape(key)}=(\d+)") is string v ? int.Parse(v) : null;

    private static string? Match(string text, string pattern)
    {
        var m = Regex.Match(text, pattern);
        return m.Success ? m.Groups[1].Value : null;
    }
}
