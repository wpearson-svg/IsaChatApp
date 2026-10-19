namespace IsaChatApp.Services.Onboarding;

/// <summary>
/// Onboarding feature (see ONBOARDING-FEATURE.md): puts together
/// everything the journey screens need about one leader on "today".
///
/// Used by both the slim "Your first 90 days" strip in the chat
/// (Components/Shared/JourneyCard.razor) and the Today window
/// (Components/Shared/TodayPanel.razor), so the two always agree.
///
///   1. LeaderTermService: the person's role and day of term on today.
///   2. JourneyService: the steps for that role and day.
///   "Today" is WidgetSettings.Today: the demo date (3 July 2026) unless
///   it is switched off on the Config page.
///
/// Returns null (so the screens show nothing) for nobody signed in, a
/// staff account with no person ID, a role with no journey (for example
/// Immediate Past President), or a leader past day 90.
/// </summary>
public class JourneyContextService
{
    /// <summary>
    /// The journey step that opens the "Meet your Chapter Experience
    /// contact" screen (journey_steps.csv, J-03). Every other step opens
    /// the plain step screen with "Mark as done".
    /// </summary>
    public const string ContactStepId = "J-03";

    private readonly LeaderTermService _terms;
    private readonly JourneyService _journey;
    private readonly WidgetSettings _settings;

    public JourneyContextService(LeaderTermService terms, JourneyService journey, WidgetSettings settings)
    {
        _terms = terms;
        _journey = journey;
        _settings = settings;
    }

    public JourneyContext? GetFor(string? personId)
    {
        if (string.IsNullOrEmpty(personId)) return null;
        var today = _settings.Today;

        // A person can hold more than one role at once. Use the first one
        // that has a journey and is inside its first 90 days.
        foreach (var term in _terms.GetCurrentTerms(personId, today))
        {
            if (term.DayOfTerm < 1 || term.DayOfTerm > 90) continue;

            var allSteps = _journey.GetAllSteps(term.Role);
            if (allSteps.Count == 0) continue;

            var todaySteps = _journey.GetStepsForDay(term.Role, term.DayOfTerm);
            int? nextStepDay = todaySteps.Count > 0 ? null : allSteps
                .Where(s => s.DayFrom > term.DayOfTerm)
                .Select(s => (int?)s.DayFrom)
                .FirstOrDefault();

            return new JourneyContext(personId, today, term, todaySteps, allSteps, nextStepDay);
        }
        return null;
    }
}

/// <summary>One leader's place in the journey on one day.</summary>
public record JourneyContext(
    string PersonId,
    DateOnly Today,
    LeaderTerm Term,
    IReadOnlyList<JourneyStep> TodaySteps,
    IReadOnlyList<JourneyStep> AllSteps,   // every step of the 90 days, for "My progress"
    int? NextStepDay)
{
    /// <summary>Day 1 to 7 is week 1, day 8 to 14 is week 2, and so on.</summary>
    public int Week => (Term.DayOfTerm - 1) / 7 + 1;

    /// <summary>How far through the 90 days, as a whole-number percent (for the progress bar).</summary>
    public int PercentThrough => (int)Math.Round(Term.DayOfTerm * 100.0 / 90);

    /// <summary>"President" becomes "Chapter president", as in the approved mockup.</summary>
    public string BadgeText => $"Chapter {Term.Role.ToLowerInvariant()}";

    /// <summary>
    /// Steps written for Isa's own onboarding plan say so in their source
    /// ("Isa onboarding plan (not ISACA content)"). Only steps from ISACA
    /// documents show a source line.
    /// </summary>
    public static bool IsFromIsacaContent(JourneyStep step) =>
        !step.Source.StartsWith("Isa onboarding plan", StringComparison.OrdinalIgnoreCase);
}
