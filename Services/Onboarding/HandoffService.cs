namespace IsaChatApp.Services.Onboarding;

/// <summary>
/// Onboarding feature, piece A (see ONBOARDING-FEATURE.md): the outgoing
/// president's handoff checklist.
///
/// Answers one question: "Should this person see the handoff checklist
/// today, and where are they in their handover?"
///
/// Who sees it (decided by Matthew, 20 October 2026): a chapter president
/// in the last 30 days of their term, and a former president for the 30
/// days after it ends (the same handoff window in which they can still
/// write to the succession notebook). So on the demo date, 3 July 2026,
/// Sato Yumiko (term ended 30 June) sees it.
///
/// The items are the President Guidebook's own words (p. 8 and the
/// Turnover Procedures on pp. 12-13), all 13 of them, each with a short
/// title. "Mark as done" uses the same progress record as the journey
/// steps (ProgressService), so every change is kept in the history.
/// Nothing here comes from the AI model.
/// </summary>
public class HandoffService
{
    /// <summary>Days before the term ends when the checklist appears, and days after it ends when it goes away.</summary>
    public const int WindowDays = 30;

    private readonly LeaderTermService _terms;
    private readonly JourneyService _journey;
    private readonly WidgetSettings _settings;

    public HandoffService(LeaderTermService terms, JourneyService journey, WidgetSettings settings)
    {
        _terms = terms;
        _journey = journey;
        _settings = settings;
    }

    /// <summary>The handoff checklist for this person today, or null if they shouldn't see it.</summary>
    public HandoffContext? GetFor(string? personId)
    {
        if (string.IsNullOrEmpty(personId)) return null;
        var today = _settings.Today;

        // A current president whose term ends within the next 30 days...
        var term = _terms.GetCurrentTerms(personId, today)
            .FirstOrDefault(t => t.Role == "President" && t.TermEnd.DayNumber - today.DayNumber <= WindowDays);

        // ...or a former president whose term ended in the last 30 days.
        term ??= _terms.GetRecentlyEndedTerms(personId, today, WindowDays)
            .FirstOrDefault(t => t.Role == "President");

        if (term is null) return null;
        var steps = _journey.GetHandoffSteps();
        return steps.Count == 0 ? null : new HandoffContext(personId, today, term, steps);
    }
}

/// <summary>One outgoing president's handover on one day.</summary>
public record HandoffContext(string PersonId, DateOnly Today, LeaderTerm Term, IReadOnlyList<HandoffStep> Steps)
{
    /// <summary>True once the term has ended (the 30 days after it).</summary>
    public bool TermHasEnded => Today > Term.TermEnd;

    /// <summary>The last day the checklist (and writing to the notebook) is available.</summary>
    public DateOnly WindowEnds => Term.TermEnd.AddDays(HandoffService.WindowDays);

    /// <summary>Days until the term ends (before it ends), or until the window closes (after).</summary>
    public int DaysLeft => (TermHasEnded ? WindowEnds : Term.TermEnd).DayNumber - Today.DayNumber;
}
