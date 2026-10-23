using System;
using System.Collections.Generic;
using System.Linq;

namespace IsaChatApp.Services;

/// <summary>
/// In-memory widget configuration, shared app-wide via DI (registered
/// as a singleton in Program.cs). Not persisted to disk -- resets to
/// these defaults on every app restart. Reached only through the
/// /isa-config page (Components/Pages/IsaConfig.razor), which is
/// deliberately not linked anywhere in the site's navigation.
/// </summary>
public class WidgetSettings
{
    /// <summary>
    /// When true, ChatWidget labels AI replies "Isa" instead of the
    /// raw model/host string (e.g. "qwen2.5-coder:1.5b-instruct
    /// (remote)"). Only affects the chat transcript -- the model
    /// picker dropdown is untouched, since that's a testing tool for
    /// the team, not something a chapter leader would see.
    /// </summary>
    public bool UseIsaLabel { get; set; } = true;

    /// <summary>
    /// When true, ChatWidget prints how long a reply took (e.g.
    /// "Isa &middot; 1.4s") right next to the model name / "Isa" label
    /// on each AI message, and the live status message while a request
    /// is in flight also ticks up its own elapsed time. Defaults on --
    /// this is a testing tool most useful while you're actually
    /// comparing models -- flip it off here before showing the widget
    /// to someone who doesn't need to see it.
    /// </summary>
    public bool ShowResponseTime { get; set; } = true;

    /// <summary>
    /// "Show the work" (Willis's request): when true, every reply from the
    /// pipeline has a quiet "Worked for 1.2s" line above it that opens into
    /// the steps Isa took (see Components/Shared/ShowTheWork.razor). While a
    /// question is running, the same line reads "Working..." with the step
    /// just finished. Defaults on. Unlike the response-time label, this is
    /// for chapter leaders too, not only developers.
    /// </summary>
    public bool ShowTheWork { get; set; } = true;

    /// <summary>
    /// "Show the work" pacing for demos (task T9W): the least time, in
    /// milliseconds, each stage stays on screen before the next one
    /// shows. 0 (the default) means no pacing: steps show the moment they
    /// finish. The pacing happens only in the chat; the run and the event
    /// log are never slowed down (stage contract, section 6). Set on the
    /// /isa-config page. 0 to 3000.
    /// </summary>
    public int MinStageMs { get; set; } = 0;

    /// <summary>
    /// "Show the work" view switch (task T10W, for the team only): when true,
    /// the Developer role sees the panel exactly as a chapter leader does
    /// (no gray skipped steps, no red errors, no raw data text). Lets us
    /// check the leader view without switching roles. Only the Developer
    /// role is offered the switch. Defaults to false (developers see the
    /// debug view).
    /// </summary>
    public bool PanelUserView { get; set; } = false;

    /// <summary>
    /// ISACA's full brand palette (Primary + Secondary + Tertiary --
    /// 10 named colors), straight off brand.isaca.org/visual-style.
    /// These are the "100%" base colors; TintPercents below are the
    /// screened tones (10% / 30% / 60%) the same page shows alongside
    /// each one. Offered as the selectable options for chat bubble
    /// colors on the /isa-config page.
    /// </summary>
    public static readonly IReadOnlyList<(string Name, string Hex)> IsacaColors = new (string, string)[]
    {
        // Primary
        ("ISACA Dark Blue", "#1d2d5c"),
        ("ISACA Blue", "#00a4e4"),
        ("ISACA Green", "#6cb33f"),
        // Secondary
        ("ISACA Medium Blue", "#07649d"),
        ("ISACA Teal", "#36ac91"),
        ("ISACA Light Blue", "#e7f0f5"),
        // Tertiary
        ("ISACA Red", "#e31837"),
        ("ISACA Orange", "#e57524"),
        ("ISACA Yellow", "#fbb034"),
        ("ISACA Purple", "#6558b1"),
        // Greyscale (added 16 October 2026 so Isa's answers can be white
        // cards on the grey chat background -- White is the first entry
        // of the brand guide's official greyscale)
        ("White", "#ffffff"),
    };

    /// <summary>
    /// The screened-tone percentages ISACA's own guide offers for each
    /// palette color (plus 100%, the color as-is). 100% first so it's
    /// the natural default in a dropdown.
    /// </summary>
    public static readonly IReadOnlyList<int> TintPercents = new[] { 100, 60, 30, 10 };

    /// <summary>
    /// Blends a base hex color toward white by the given percent, the
    /// same way brand.isaca.org's own 10%/30%/60% swatches are derived
    /// (percent = ink coverage against white -- 100% is the color
    /// as-is, lower percents are progressively lighter tints).
    /// </summary>
    public static string Tint(string hex, int percent)
    {
        hex = hex.TrimStart('#');
        int r = Convert.ToInt32(hex.Substring(0, 2), 16);
        int g = Convert.ToInt32(hex.Substring(2, 2), 16);
        int b = Convert.ToInt32(hex.Substring(4, 2), 16);
        double p = Math.Clamp(percent, 0, 100) / 100.0;
        int tr = (int)Math.Round(255 - p * (255 - r));
        int tg = (int)Math.Round(255 - p * (255 - g));
        int tb = (int)Math.Round(255 - p * (255 - b));
        return $"#{tr:x2}{tg:x2}{tb:x2}";
    }

    /// <summary>
    /// White or ISACA Grey 10 (#1f2933) text, whichever has the higher
    /// contrast on the given background, using the WCAG 2.1 contrast
    /// formula that BRAND_GUIDELINES.md's accessibility rules are based
    /// on (preferred 7:1, minimum 4.5:1). Used so a light 10%/30% tint
    /// doesn't get white-on-pale text, and so a mid color like ISACA
    /// Teal gets dark text (white on Teal is only about 2.8:1).
    /// </summary>
    public static string ReadableTextColor(string hex)
    {
        const string Dark = "#1f2933";   // ISACA Grey 10
        const string Light = "#ffffff";
        double bg = RelativeLuminance(hex);
        double contrastWithDark = (bg + 0.05) / (RelativeLuminance(Dark) + 0.05);
        double contrastWithLight = (RelativeLuminance(Light) + 0.05) / (bg + 0.05);
        return contrastWithDark >= contrastWithLight ? Dark : Light;
    }

    /// <summary>WCAG 2.1 relative luminance of a #rrggbb color (0 = black, 1 = white).</summary>
    private static double RelativeLuminance(string hex)
    {
        hex = hex.TrimStart('#');
        double Channel(int start)
        {
            double c = Convert.ToInt32(hex.Substring(start, 2), 16) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(0) + 0.7152 * Channel(2) + 0.0722 * Channel(4);
    }

    /// <summary>
    /// Color for the small "ISA" label on a bubble: ISACA Medium Blue
    /// (#07649d) when it meets the brand guide's 4.5:1 minimum contrast
    /// on that background (it does on ISACA Light Blue, the default),
    /// otherwise the same color as the bubble's text.
    /// </summary>
    public static string LabelColor(string hex)
    {
        const string MediumBlue = "#07649d";
        double bg = RelativeLuminance(hex);
        double label = RelativeLuminance(MediumBlue);
        double contrast = (Math.Max(bg, label) + 0.05) / (Math.Min(bg, label) + 0.05);
        return contrast >= 4.5 ? MediumBlue : ReadableTextColor(hex);
    }

    /// <summary>Name of the base ISACA color picked for "you" (user) chat bubbles -- must match an IsacaColors.Name.</summary>
    public string UserBubbleColorName { get; set; } = "ISACA Dark Blue";

    /// <summary>Tint percent applied to UserBubbleColorName -- must be one of TintPercents.</summary>
    public int UserBubbleColorPercent { get; set; } = 100;

    /// <summary>
    /// Resolved hex for the "you" bubble background (base color tinted
    /// to the chosen percent). Applied per-message via an inline style
    /// attribute in ChatWidget.razor (setting a --bubble-bg custom
    /// property directly on the element), not a :root rule -- a :root
    /// block inside a scoped .razor.css file doesn't actually match
    /// anything under Blazor's CSS isolation.
    /// </summary>
    public string UserBubbleColor =>
        Tint(IsacaColors.First(c => c.Name == UserBubbleColorName).Hex, UserBubbleColorPercent);

    /// <summary>
    /// Name of the base ISACA color picked for "ai" (Isa) chat bubbles.
    /// Default is White: Isa's answers are white cards on the chat's
    /// ISACA Grey 1 background, with sources shown in ISACA Light Blue
    /// (Matthew's approved redesign, 16 October 2026; earlier the
    /// default was ISACA Teal). Still changeable on /isa-config.
    /// </summary>
    public string AiBubbleColorName { get; set; } = "White";

    /// <summary>Tint percent applied to AiBubbleColorName -- must be one of TintPercents.</summary>
    public int AiBubbleColorPercent { get; set; } = 100;

    /// <summary>Resolved hex for the "ai" bubble background. Same mechanism as UserBubbleColor above.</summary>
    public string AiBubbleColor =>
        Tint(IsacaColors.First(c => c.Name == AiBubbleColorName).Hex, AiBubbleColorPercent);

    // ---- Roles / dashboard access --------------------------------------
    //
    // There's no real login here -- this is a demo, so instead of an
    // actual auth system, /isa-config just lets you pick "who you are"
    // for the whole app (WidgetSettings is a singleton), and the chat
    // widget's header icons + the dashboard pages themselves both check
    // against that. Swap this for real authentication/authorization
    // before this goes anywhere near production.

    /// <summary>The four roles the demo distinguishes, and what each can reach.</summary>
    public enum UserRole { User, Developer, Executive, Manager }

    /// <summary>Which role is "logged in" right now, app-wide. Defaults to the least-privileged role.</summary>
    public UserRole CurrentRole { get; set; } = UserRole.User;

    /// <summary>
    /// Dev Dashboard access, per the role matrix from /isa-config:
    /// User: no, Developer: yes, Executive: yes, Manager: no.
    /// </summary>
    public bool CanAccessDevDashboard => CurrentRole is UserRole.Developer or UserRole.Executive;

    /// <summary>
    /// Content Health Dashboard access, per the role matrix from
    /// /isa-config: User: no, Developer: no, Executive: yes, Manager: yes.
    /// </summary>
    public bool CanAccessContentHealthDashboard => CurrentRole is UserRole.Executive or UserRole.Manager;

    // ---- Onboarding feature: demo date (see ONBOARDING-FEATURE.md) ----
    // The 90-day journey counts each leader's day of term from "today".
    // For the demo, "today" is a fixed date, so the screens look the same
    // whatever day the presentation falls on. Like every setting here, it
    // resets to these defaults when the app restarts.

    /// <summary>The date the demo pretends it is. Decided 16 October 2026: 3 July 2026.</summary>
    public DateOnly DemoDate { get; set; } = new DateOnly(2026, 7, 3);

    /// <summary>When true (the default), days of term are counted from DemoDate instead of the real date.</summary>
    public bool UseDemoDate { get; set; } = true;

    /// <summary>The date the onboarding feature treats as today: DemoDate, or the real date when the demo date is off.</summary>
    public DateOnly Today => UseDemoDate ? DemoDate : DateOnly.FromDateTime(DateTime.Today);

    // ---- Presentation page (/present) ----------------------------------
    // The presentation slides live inside Isa (Components/Pages/Present.razor),
    // so the talk moves into the live demo without switching windows.
    // Off by default, so chapter leaders never come across the page; the
    // team switches it on at /isa-config before presenting. Resets to off
    // when Isa restarts, like every setting here.

    /// <summary>When true, /present shows the slides. When false, it says the page is switched off.</summary>
    public bool PresentationEnabled { get; set; }
}
