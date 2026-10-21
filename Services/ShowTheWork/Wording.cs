namespace IsaChatApp.Services.ShowTheWork;

/// <summary>
/// Every word a chapter leader reads from Isa's pipeline, in ONE place (task T11W).
/// Built from the wording table (docs/Show_the_Work_Wording.md, sections 1 to 6,
/// Matthew Brady) plus the three additions agreed on 21 October 2026:
///   1. wording for AI-side check failures,
///   2. a third hand-off message (a failed answer check or a failed model call is
///      not "ISACA's guidance doesn't cover this"),
///   3. the safety line on the one safety topic that exists today (harassment).
///
/// Used by: the "show the work" panel (WorkStepText), the stages that write a
/// message (input compliance, access control, ticketing), the orchestrator's error
/// message and the chat. To change what a leader reads, change it HERE.
///
/// Every sentence is fixed text. Nothing here comes from the AI model.
/// The AI step is shown as "Diving Deeper".
/// </summary>
public static class Wording
{
    // ---------- Reply bubble messages (wording table, section 4) ----------

    public const string OutsideScope =
        "That's outside what Isa can help with. Isa answers questions about running your chapter, using ISACA's approved guidance.";

    public const string NotSignedIn =
        "Please sign in to ask a question. Use the Sign in button at the top of the Isa home page.";

    public const string NoAccess =
        "Your account doesn't have access to Isa. If you think it should, contact ISACA Support.";

    public const string WentWrong =
        "Something went wrong on our side. Try again, or talk to a person.";

    // ---------- Hand-off messages (section 5 and addition 2) ----------

    private const string KeepItHandy =
        "Keep it handy if you follow up with your Chapter Experience contact or ISACA Support.";

    /// <summary>Added after a hand-off about a safety topic (addition 3).</summary>
    public const string SafetyLine =
        "If anyone is in immediate danger, contact your local emergency services.";

    /// <summary>Why a run was passed to a person. Picks the message.</summary>
    public enum HandoffKind
    {
        Sensitive,      // the Rules check passed it to a person
        NotCovered,     // ISACA's approved guidance doesn't cover the question
        AnswerCheck,    // an answer check failed, or the model call failed
    }

    /// <summary>Which hand-off message fits how the run was escalated.</summary>
    public static HandoffKind KindOf(string? escalatedBy, string? reason) =>
        escalatedBy == "input_compliance" ? HandoffKind.Sensitive
        : reason == "no approved content found" || escalatedBy is "database_search" or "rag" ? HandoffKind.NotCovered
        : HandoffKind.AnswerCheck;

    public static string Handoff(HandoffKind kind, string ticket, bool safetyTopic = false)
    {
        var text = kind switch
        {
            HandoffKind.Sensitive =>
                $"This sounds like something a person should help with, so I've passed it to the right team. Your reference number is {ticket}. {KeepItHandy}",
            HandoffKind.NotCovered =>
                $"ISACA's approved guidance doesn't cover this yet, so I've passed it to a person to follow up. Your reference number is {ticket}. {KeepItHandy}",
            _ =>
                $"I couldn't check this answer well enough to share it, so I've passed it to a person. Your reference number is {ticket}. {KeepItHandy}",
        };
        return safetyTopic ? text + " " + SafetyLine : text;
    }

    // ---------- Panel: the closed line above each reply (section 3) ----------

    public const string ClosedAnswered = "Answered from approved content";
    public const string ClosedOutsideScope = "Outside what Isa covers";
    public const string ClosedNoSignIn = "Couldn't confirm you can use Isa";
    public const string ClosedHandoff = "Passed to a person";
    public const string ClosedWentWrong = "Something went wrong on our side";

    // ---------- Panel: status line while a stage runs (section 1) ----------

    public static string Running(string stage) => stage switch
    {
        "front_end" => "Reading your question",
        "access_control" => "Checking you can use Isa",
        "translation" => "Checking the language",
        "input_compliance" => "Checking your question",
        "database_search" => "Searching ISACA's approved content",
        "knowledge_graph" => "Looking for related topics",
        "rag" => "Gathering passages",
        "complex_ai" => "Diving Deeper",
        "output_compliance" => "Checking the answer against its sources",
        "ticketing" => "Passing your question to a person",
        _ => "Working on your question",
    };

    public const string RunningFinishing = "Finishing up";

    // ---------- Panel: step lines when a stage passed (section 1) ----------

    public const string StepFrontEnd = "Received your question";
    public const string StepAccess = "Checked that you can use Isa";
    public const string StepTranslation = "Checked the language of your question";
    public const string StepRules = "Checked your question against Isa's rules";
    public const string StepSearch = "Searched ISACA's approved content";
    public const string StepRelated = "Looked for related topics";
    public const string StepPassages = "Gathered the passages to answer from";
    public const string StepDivingDeeper = "Diving Deeper";
    public const string StepAnswerCheck = "Checked the answer against its sources";
    public const string StepTicket = "Created your ticket";
    public const string StepAccessStopped = "Couldn't confirm you can use Isa";

    // second lines
    public const string FoundAnswer = "Found the answer in ISACA's approved text";
    public const string FoundNothing = "Found nothing that matches";
    public const string ContentNotLoaded = "The approved content isn't loaded yet";
    public static string FoundTopic(int n) => $"Found {Passages(n)} on the same topic";
    public static string Passages(int? n) => n == 1 ? "1 passage" : $"{n ?? 0} passages";
    public const string NothingToGather = "There was nothing to gather";
    public const string DivingDeeperDone = "Wrote the answer using only the passages found";
    public const string DivingDeeperNothing = "There was nothing approved to write from";
    public const string ExactWording = "Answer uses ISACA's exact wording";
    public static string BackedBy(int n) => $"backed by {n} {(n == 1 ? "source" : "sources")}";
    public static string Reference(string ticket) => $"Reference {ticket}";

    // ---------- Panel: when a stage does not pass (section 2) ----------

    public const string SignInStoppedNotSignedIn = "Please sign in to ask a question.";
    public const string SignInStoppedNoAccess = "Your account doesn't have access to Isa.";
    public const string RulesStopped = "It's outside what Isa can help with, so she stopped here.";
    public const string RulesToPerson = "This needs a person, so Isa won't answer it herself.";
    public const string AnswerToPersonNoContent = "No approved content found, so it's going to a person.";
    public const string AnswerToPersonNoSource = "The answer didn't have an approved source, so it's going to a person.";
    public const string AnswerToPersonCheckFailed = "The answer didn't pass a check, so it's going to a person.";
    public const string WentWrongHere = "Something went wrong here.";

    // Developer mode only: skipped steps.
    public static string Skipped(string stage) => "Skipped " + stage switch
    {
        "translation" => "checking the language",
        "input_compliance" => "checking the question",
        "database_search" => "searching approved content",
        "knowledge_graph" => "looking for related topics",
        "rag" => "gathering passages",
        "complex_ai" => "Diving Deeper",
        "output_compliance" => "checking an answer",
        _ => "the " + stage.Replace('_', ' ') + " step",
    };
    public const string NotNeededDirectHit = "Not needed: the exact text answers it";
    public const string NotNeededEnglish = "Not needed: your question is in English";
    public const string NotNeededEscalated = "Not needed, because the question is going to a person";

    /// <summary>What a stage was doing, for a stage that crashed.</summary>
    public static string Doing(string stage) => stage switch
    {
        "access_control" => "Checking that you can use Isa",
        "translation" => "Checking the language",
        "input_compliance" => "Checking your question",
        "database_search" => "Searching ISACA's approved content",
        "knowledge_graph" => "Looking for related topics",
        "rag" => "Gathering passages",
        "complex_ai" => "Diving Deeper",
        "output_compliance" => "Checking the answer",
        "ticketing" => "Creating your ticket",
        _ => $"The {stage.Replace('_', ' ')} step",
    };
}
