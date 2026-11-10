# Lane 11: accessibility check (promise R12)

November 9 to 10, 2026 · Matt (with Claude Code) · local setup, signed in as Watanabe Kenji (day 3)

R12 promises: reduced motion, contrast, 44px tap targets, screen-reader labels. This is a prototype check, not a formal audit.

## How it was checked

- **Automated scan:** axe-core 4.9.1 loaded in the browser, rules for WCAG 2.1 A and AA plus best practice.
- **Contrast by measurement:** where the scanner couldn't decide (text over panels and overlays), a script compared each text color with the background behind it (4.5:1 for normal text, 3:1 for large text).
- **Tap targets:** a script measured every button, link and input in Isa's panels.
- **Keyboard:** real Tab, Enter and Escape key presses on the landing page and the chat; the tab order read from the page.
- **Zoom and phone:** the page at 640 x 480 (the same as 200% zoom on a 1280 screen) and at 375 x 812 (phone).
- **Reduced motion:** every stylesheet that animates was checked for a `prefers-reduced-motion` rule.

## Results

| Check | Screens | Result |
|---|---|---|
| Automated scan | Sign-in page; landing page; chat with an answer, Show the work open, a hand-off reply and a "can't help" reply; Today, a journey step (contact), calendar, My progress; notebook, an entry, Add a note; Library, a document, Terms explained; Help; Talk to a person | **0 violations after the fixes** (M-019, M-020). Before: 1 critical (calendar), 1 moderate on every chat screen (landmarks), 2 moderate on the landing page |
| Contrast | The 12 chat and panel screens (72 to 191 pieces of text each) | Pass. Only the notebook's decorative "›" arrow is below 4.5:1 (2.5:1); it carries no information |
| Keyboard: focus ring | Landing page, chat | Pass: an orange outline shows on the focused control |
| Keyboard: order | Landing page, chat | Pass with a note: "Open chat" is first; in the open chat the order is header, suggestions, Today note, question box, Talk to a person, then the menu on the left (it looks first but comes last) |
| Keyboard: open and close | Chat, Today, notebook | Pass after the fix (M-021): opens with Enter and focus moves into the chat; Escape closes the right-hand panel, then Today, then the chat, and focus returns each time. A half-written note keeps its panel open. Still open: the page behind the chat can be reached with Tab |
| 200% zoom | Chat, Today, calendar | Pass: one panel at a time, no sideways scrolling, nothing cut off |
| Phone (375 wide) | Chat | Pass after the fix (M-023): the header's role line shortens with "..." instead of running under "Help" |
| Reduced motion | All of Isa's own stylesheets and scripts | Pass. Two files have no rule: the framework's reconnect box and the Content Health Dashboard (not leader screens) |
| Tap targets (44px) | Chat, Help, contact step, notebook, Library reader | Pass after the fix (M-022): 22 kinds of small button and link have an invisible 44px tap area. Still under 44px: the question box (31px high inside its pill), calendar days (34px wide in a third-of-screen panel), the notebook's search box (40px high) |
| New answers announced | Chat | Fixed, not yet heard (M-024): a hidden status line holds the newest reply for screen readers |

## Not checked

- **Screen reader pass** (by hand, from a checklist): is a new answer read out once and not twice (M-024), does the newest-first order of the message list confuse, are panel changes announced, do labels make sense when heard.
- **Keyboard through every panel:** only the landing page and chat were walked with real keys. Still to do: Today, notebook forms, Library player, Help, where focus goes when a panel closes, and Sign out with the keyboard.
- Right-to-left (Fahad Al-Harbi's Arabic view), the sample Engage and Chapter 360 pages, the dashboards and the /present slides.
