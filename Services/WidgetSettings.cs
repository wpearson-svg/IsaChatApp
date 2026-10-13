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
        ("ISACA Purple", "#6658b1"),
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
    /// White or near-black text, whichever reads better on top of the
    /// given background -- used so a light 10%/30% tint doesn't end up
    /// with illegible white-on-pale-pastel text.
    /// </summary>
    public static string ReadableTextColor(string hex)
    {
        hex = hex.TrimStart('#');
        int r = Convert.ToInt32(hex.Substring(0, 2), 16);
        int g = Convert.ToInt32(hex.Substring(2, 2), 16);
        int b = Convert.ToInt32(hex.Substring(4, 2), 16);
        double brightness = (r * 299 + g * 587 + b * 114) / 1000.0;
        return brightness > 150 ? "#1d1d1f" : "#ffffff";
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

    /// <summary>Name of the base ISACA color picked for "ai" (Isa) chat bubbles.</summary>
    public string AiBubbleColorName { get; set; } = "ISACA Teal";

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
}
