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
/// The AI step is shown as "Diving deeper".
///
/// Updated 22 October 2026 with Matthew's warmer wording ("Wording changes for Willis,
/// October 21"): one voice (Isa says "I" only in reply bubbles and hand-offs), everyday
/// words ("ISACA's guidance", "sections"), every "no" says what to do next.
/// </summary>
public static class Wording
{
    // ---------- Reply bubble messages (wording table, section 4) ----------

    public const string OutsideScope =
        "I can only help with running your chapter, using ISACA's guidance. Try asking about board meetings, elections or your yearly paperwork.";

    public const string NotSignedIn =
        "Please sign in so I can help. The Sign in button is at the top of the home page.";

    public const string NoAccess =
        "Your account doesn't have access to Isa yet. If you think it should, ISACA Support can help.";

    /// <summary>How an answer from ISACA's guidance opens (the chat message text).</summary>
    public const string AnswerOpening = "Here's what ISACA's guidance says: ";

    public const string WentWrong =
        "Sorry, something went wrong on my side. Please try again in a moment. If it keeps happening, you can talk to a person.";

    // ---------- Hand-off messages (section 5 and addition 2) ----------

    private const string KeepItHandy =
        "Keep it handy if you follow up with ISACA's Chapter Experience team or ISACA Support.";

    /// <summary>Added after a hand-off about a safety topic (addition 3).</summary>
    public const string SafetyLine =
        "If you or anyone else is in immediate danger, please contact your local emergency services.";

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
                $"Thank you for telling me. This is something a person should help with, so I've passed it to someone who can help. Your reference number is {ticket}. {KeepItHandy}",
            HandoffKind.NotCovered =>
                $"ISACA's guidance doesn't cover this yet, so I've passed your question to a person who can help. Your reference number is {ticket}. {KeepItHandy}",
            _ =>
                $"I couldn't double-check this answer well enough to share it, so I've passed your question to a person who can help. Your reference number is {ticket}. {KeepItHandy}",
        };
        return safetyTopic ? text + " " + SafetyLine : text;
    }

    // ---------- Panel: the closed line above each reply (section 3) ----------

    public const string ClosedAnswered = "Answered from ISACA's guidance";
    public const string ClosedOutsideScope = "Not something Isa can help with";
    public const string ClosedNoSignIn = "Please sign in";
    public const string ClosedHandoff = "Passed to a person";
    public const string ClosedWentWrong = "Something went wrong on our side";

    // ---------- Panel: status line while a stage runs (section 1) ----------

    public static string Running(string stage) => stage switch
    {
        "front_end" => "Reading your question",
        "access_control" => "Checking your sign-in",
        "translation" => "Checking the language",
        "input_compliance" => "Checking your question",
        "database_search" => "Searching ISACA's guidance",
        "knowledge_graph" => "Looking for related topics",
        "rag" => "Gathering the right sections",
        "complex_ai" => "Diving deeper",
        "output_compliance" => "Double-checking the answer against ISACA's guidance",
        "ticketing" => "Finding the right person to help",
        _ => "Working on your question",
    };

    public const string RunningFinishing = "Almost there";

    // ---------- Panel: step lines when a stage passed (section 1) ----------

    public const string StepFrontEnd = "Received your question";
    public const string StepAccess = "Confirmed your sign-in";
    public const string StepTranslation = "Checked the language of your question";
    public const string StepRules = "Made sure Isa can help with this";
    public const string StepSearch = "Searched ISACA's guidance";
    public const string StepRelated = "Looked for related topics";
    public const string StepPassages = "Gathered the sections to answer from";
    public const string StepDivingDeeper = "Diving deeper";
    public const string StepAnswerCheck = "Double-checked the answer";
    public const string StepTicket = "Passed your question on";
    public const string StepAccessStopped = "Couldn't confirm your sign-in";

    // second lines
    public const string FoundAnswer = "Found the answer in ISACA's own words";
    public const string FoundNothing = "Couldn't find this in ISACA's guidance";
    public const string ContentNotLoaded = "The approved content isn't loaded yet";
    public static string FoundTopic(int n) => $"Found {Sections(n)} on this topic";
    public static string Sections(int? n) => n == 1 ? "1 section" : $"{n ?? 0} sections";
    public const string NothingToGather = "There was nothing to gather";
    public const string DivingDeeperDone = "Put the answer together using only ISACA's guidance";
    public const string DivingDeeperNothing = "There was nothing approved to write from";
    public const string ExactWording = "Uses ISACA's exact wording";
    public static string BackedBy(int n) => $"{n} {(n == 1 ? "source" : "sources")}";
    public static string Reference(string ticket) => $"Reference {ticket}";

    // ---------- Panel: when a stage does not pass (section 2) ----------

    public const string SignInStoppedNotSignedIn = "Please sign in to continue.";
    public const string SignInStoppedNoAccess = "Your account doesn't have access to Isa.";
    public const string RulesStopped = "This one's outside what Isa can help with";
    public const string RulesToPerson = "This is best answered by a person";
    public const string AnswerToPersonNoContent = "ISACA's guidance doesn't cover this yet, so a person will help";
    public const string AnswerToPersonNoSource = "ISACA's guidance doesn't cover this yet, so a person will help";
    public const string AnswerToPersonCheckFailed = "The answer couldn't be fully checked, so a person will help";
    public const string WentWrongHere = "Something didn't work on our side";

    // Developer mode only: skipped steps.
    public static string Skipped(string stage) => "Skipped " + stage switch
    {
        "translation" => "checking the language",
        "input_compliance" => "checking the question",
        "database_search" => "searching approved content",
        "knowledge_graph" => "looking for related topics",
        "rag" => "gathering passages",
        "complex_ai" => "Diving deeper",
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
        "complex_ai" => "Diving deeper",
        "output_compliance" => "Checking the answer",
        "ticketing" => "Creating your ticket",
        _ => $"The {stage.Replace('_', ' ')} step",
    };
}
