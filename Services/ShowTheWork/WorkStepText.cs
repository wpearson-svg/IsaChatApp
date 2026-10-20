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
            return new(StepState.Problem, "Something went wrong on our side", developer ? time : null);

        if (steps.Any(s => s.Outcome == "escalate"))
        {
            var ticket = steps.Select(s => Match(s.DataText, @"\bticket=(TKT-\d+)")).FirstOrDefault(t => t is not null);
            return new(StepState.ToPerson, "Passed to a person", developer ? time : ticket);
        }

        var stop = steps.FirstOrDefault(s => s.Outcome == "stop");
        if (stop is not null)
            return new(StepState.Stopped,
                stop.Stage == "access_control" ? "Couldn't confirm you can use Isa" : "Outside what Isa covers",
                developer ? time : null);

        var checks = Lines(steps, developer: false).Count;
        return new(StepState.Passed, "Answered from approved content", developer ? time : $"{checks} checks");
    }

    /// <summary>What Isa is doing right now, for the live status line, e.g. "Searching ISACA's approved content".</summary>
    public static string Running(IReadOnlyList<WorkStep> finished)
    {
        if (finished.Any(s => s.Outcome == "escalate")) return "Passing your question to a person";
        var last = finished.LastOrDefault();
        var next = last is null ? 0 : Array.IndexOf(Order, last.Stage) + 1;
        return next switch
        {
            0 => "Reading your question",
            1 => "Checking you can use Isa",
            2 => "Checking the language",
            3 => "Checking your question",
            4 => "Searching ISACA's approved content",
            5 => "Looking for related topics",
            6 => "Gathering passages",
            7 => "Writing the answer",
            8 => "Checking the answer against its sources",
            _ => "Finishing up",
        };
    }

    /// <summary>What Isa is doing right now, from the stage a "started" update names.</summary>
    public static string RunningStage(string stage) => stage switch
    {
        "front_end" => "Reading your question",
        "access_control" => "Checking you can use Isa",
        "translation" => "Checking the language",
        "input_compliance" => "Checking your question",
        "database_search" => "Searching ISACA's approved content",
        "knowledge_graph" => "Looking for related topics",
        "rag" => "Gathering passages",
        "complex_ai" => "Writing the answer",
        "output_compliance" => "Checking the answer against its sources",
        "ticketing" => "Passing your question to a person",
        _ => "Working on your question",
    };

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
            return L(Doing(step.Stage), "Something went wrong here");

        // Developer mode only: a stage passed over after an escalation.
        if (step.Skipped && step.DataText == "escalated")
            return L($"Skipped {SkippedName(step.Stage)}", "Not needed, because the question is going to a person");

        switch (step.Stage)
        {
            case "front_end":
                return L("Received your question");

            case "access_control":
                return state == StepState.Stopped
                    ? L("Couldn't confirm you can use Isa", step.Reason)
                    : L("Checked that you can use Isa");

            case "translation":
                return L("Checked the language of your question");

            case "input_compliance":
                return state switch
                {
                    StepState.Stopped => L("Checked your question against Isa's rules", "It's outside chapter support, so Isa stopped here and didn't search"),
                    StepState.ToPerson => L("Checked your question against Isa's rules", "This needs a person, so Isa won't answer it herself"),
                    _ => L("Checked your question against Isa's rules"),
                };

            case "database_search":
            {
                var count = Number(step.DataText, "count");
                return L("Searched ISACA's approved content",
                    step.DataText.StartsWith("direct_hit") ? "Found the answer in ISACA's approved text"
                    : step.DataText.StartsWith("meaning_map") ? $"Found {Passages(count)} on the same topic"
                    : step.DataText.StartsWith("no content db") ? "The approved content isn't loaded yet"
                    : "Found nothing that matches");
            }

            case "knowledge_graph":
                return L("Looked for related topics");

            case "rag":
                if (step.Skipped) return L("Skipped gathering passages", "Direct hit: the exact text answers it");
                if (state == StepState.ToPerson) return L("Gathered the passages to answer from", "Nothing approved to answer from, so it's going to a person");
                if (step.DataText.StartsWith("no_context")) return L("Gathered the passages to answer from", "There was nothing to gather");
                return L("Gathered the passages to answer from", Number(step.DataText, "passages") is int p ? Passages(p) : null);

            case "complex_ai":
                if (step.Skipped) return L("Skipped the AI model", "Direct hit: the exact text answers it");
                if (Number(step.DataText, "passages") == 0) return L("Wrote an answer", "There was nothing approved to write from");
                return L("Wrote the answer using only those passages");

            case "output_compliance":
            {
                if (state == StepState.ToPerson)
                    return L("Checked the answer against its sources", Capitalize(step.Reason) + ", so it's going to a person");
                var backed = Number(step.DataText, "sources") is int s ? $"backed by {s} {(s == 1 ? "source" : "sources")}" : null;
                return L("Checked the answer against its sources",
                    directHit ? "Answer uses ISACA's exact wording" + (backed is null ? "" : " · " + backed)
                    : Capitalize(backed));
            }

            case "ticketing":
                return L("Created your ticket", Match(step.DataText, @"\bticket=(TKT-\d+)") is string t ? $"Reference {t}" : null);

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
        (step.Stage == "rag" && step.DataText.StartsWith("no_context")) ||
        (step.Stage == "complex_ai" && Number(step.DataText, "passages") == 0);

    // True when ISACA's own text answered the question word for word.
    private static bool IsDirectHit(IReadOnlyList<WorkStep> steps) =>
        steps.Any(s => s.Stage == "database_search" && s.DataText.StartsWith("direct_hit"));

    // What a stage does, for a stage that crashed.
    private static string Doing(string stage) => stage switch
    {
        "access_control" => "Checking that you can use Isa",
        "translation" => "Checking the language",
        "input_compliance" => "Checking your question",
        "database_search" => "Searching ISACA's approved content",
        "knowledge_graph" => "Looking for related topics",
        "rag" => "Gathering passages",
        "complex_ai" => "Writing the answer",
        "output_compliance" => "Checking the answer",
        "ticketing" => "Creating your ticket",
        _ => $"The {ShortName(stage)} step",
    };

    private static string SkippedName(string stage) => stage switch
    {
        "translation" => "checking the language",
        "input_compliance" => "checking the question",
        "database_search" => "searching approved content",
        "knowledge_graph" => "looking for related topics",
        "rag" => "gathering passages",
        "complex_ai" => "the AI model",
        "output_compliance" => "checking an answer",
        _ => "the " + ShortName(stage) + " step",
    };

    public static string FormatMs(double ms) => ms < 1000 ? $"{ms:0}ms" : $"{ms / 1000:0.0}s";

    private static string ShortName(string stage) => stage.Replace('_', ' ');

    private static string Passages(int? n) => n == 1 ? "1 passage" : $"{n ?? 0} passages";

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
