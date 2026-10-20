# ISACA Brand Guidelines — Visual Style (reference)

Source: ISACA Brand Guidelines, "Visual Style" (https://brand.isaca.org/visual-style), saved 2026-09-24.
This file is the authoritative style reference for everything built in this project (IsaChatApp / the
Second Star Collective demo). Check colors, type, and iconography choices against this before adding
new UI — don't introduce a color, font, or graphic motif that isn't traceable back to this doc.

---

## 1. Color

### Primary palette

| Name | Pantone | CMYK | RGB | Hex |
|---|---|---|---|---|
| ISACA Dark Blue | 281C | 100-85-5-36 | 29-45-92 | `#1d2d5c` |
| ISACA Blue | 299C | 90-10-0-0 | 0-164-228 | `#00a4e4` |
| ISACA Green | 369C | 60-0-100-5 | 108-179-63 | `#6cb33f` |

### Secondary palette

| Name | Pantone | CMYK | RGB | Hex |
|---|---|---|---|---|
| ISACA Medium Blue | 647C | 96-54-5-27 | 7-100-157 | `#07649d` |
| ISACA Teal | 7723C | 69-0-54-7 | 54-172-145 | `#36ac91` |
| ISACA Light Blue | 544C | 27-4-1-1 | 231-240-245 | `#e7f0f5` |

### Tertiary palette

| Name | Pantone | CMYK | RGB | Hex |
|---|---|---|---|---|
| ISACA Red | 186C | 0-100-80-4 | 227-24-55 | `#e31837` |
| ISACA Orange | 158C | 0-65-100-0 | 229-117-36 | `#e57524` |
| ISACA Yellow | 137C | 0-35-90-0 | 251-176-52 | `#fbb034` |
| ISACA Purple | 2102C | 72-72-0-0 | 101-88-177 | `#6558b1` |

Screened tones of any palette color should be limited to 10%, 30%, or 60% tints whenever possible
(that's how the guide itself presents each swatch).

### Greyscale (White → Black)

| Name | CMYK | RGB | Hex |
|---|---|---|---|
| White | 0-0-0-0 | 255-255-255 | `#ffffff` |
| ISACA Grey 1 | 2-1-1-0 | 246-247-248 | `#f6f7f8` |
| ISACA Grey 2 | 7-3-4-8 | 229-231-235 | `#e5e7eb` |
| ISACA Grey 3 | 12-6-5-12 | 203-210-217 | `#cbd2d9` |
| ISACA Grey 4 | 21-11-9-22 | 154-165-177 | `#9aa5b1` |
| ISACA Grey 5 | 33-18-13-37 | 123-135-148 | `#7b8794` |
| ISACA Grey 6 | 66-50-40-12 | 96-110-124 | `#606e7c` |
| ISACA Grey 7 | 45-27-17-51 | 82-96-109 | `#52606d` |
| ISACA Grey 8 | 76-63-47-32 | 63-75-89 | `#3f4b59` |
| ISACA Grey 9 | 67-45-27-70 | 50-62-72 | `#323e48` |
| ISACA Grey 10 | 90-69-40-89 | 31-41-51 | `#1f2933` |
| ISACA Black | 40-40-0-100 | 0-0-0 | `#000000` |

### Accessibility (WCAG 2.1)

- Preferred contrast ratio: **7:1** (WCAG AAA) for normal text.
- Minimum contrast ratio: **4.5:1** (WCAG AA), except:
  - **Large text** (14pt/18.66px bold+, or 18pt/24px+): the guide gives large text its own,
    lower minimum than body text — WCAG's general floor for large text is 3:1, but treat **4.5:1** as
    the floor in this project too, since that's the number the guide itself designs to.
  - **Incidental text** (inactive UI, pure decoration, not visible to anyone, or part of an image with other significant visual content): no contrast requirement.
  - **Logotypes**: text that's part of a logo/brand name has no contrast requirement.
- When pairing brand colors for text-on-background, prefer combinations the guide itself sampled as
  passing (dark navy/black on light backgrounds; white or light tints on Dark Blue, Purple, Red, and
  the darker greys).

### Color meaning / symbolism

Color connotation varies by culture/region — the guide includes a full region-by-region table (North
America, South America, Europe, Africa, Middle East, Asia, Oceania) for each palette color. Worth a
second look before using a color to *carry meaning* (e.g., red/green as good/bad) in anything aimed at
an international ISACA audience — not just as decoration.

---

## 2. Typography

| Role | Typeface | Notes |
|---|---|---|
| Primary | **Roboto** | Thin, Light, Regular, Medium, Bold, Black — full weight range available. |
| Enhancement | **Roboto Slab** | Light / Regular / Bold. Used to punctuate hierarchy — headlines. |
| Secondary | **Roboto Condensed** | Light / Regular / Bold. Subordinate to the primary font, used sparingly. |
| System / fallback | **Arial** (incl. Arial Narrow) | For internal-only communications, default-font web body copy, or PowerPoint. |

**Hierarchy rules (from the guide):**
- Main headlines: **Roboto Slab Light**, sentence case (not Title Case, not ALL CAPS).
- Subheads: **Roboto Slab Regular**, sentence case.
- Paragraph titles: **Roboto Bold, ALL CAPS**.
- Body copy: **Roboto Light**, flush left, minimum 2pt leading, one full line-space between paragraphs.
  Body text color is **ISACA Dark Blue** or **Grey 9** (`#1d2d5c` or `#323e48`).
- Footnotes / small text: **Roboto Regular** or a heavier weight (Roboto Medium is their own example).

---

## 3. Visual motif — the "C" graphics

The rotating/interlocking C shapes are the core visual signature (9 Cs locked together = the ISACA
logo itself). Variants used across materials:

**The approved C shape.** Every C in this project uses the single C from the Visual Style page
(the blue C in the "Cropped C graphic" section), measured from ISACA's own artwork. In units of the
outer radius R:

- Inner radius: 0.5655 R (so the band is 0.4345 R thick)
- Opening: a straight slot on the right, 0.498 R tall, with flat top and bottom edges
- All four corners of the slot are rounded, radius 0.033 R

The ready-made SVG path, for a C of outer radius 100.89 centered on (100, 100) in a
`viewBox="-2 -2 204 204"` (the size the throbber and favicon use):

```
M153.26 74.88L193.32 74.88A3.33 3.33 0 0 0 196.5 70.58A100.89 100.89 0 1 0 196.5 129.42A3.33 3.33 0 0 0 193.32 125.12L153.26 125.12A3.33 3.33 0 0 0 150.32 126.88A57.05 57.05 0 1 1 150.32 73.12A3.33 3.33 0 0 0 153.26 74.88Z
```

Draw it as a filled shape (fill), not a stroked circle. Rotate it for other directions; never
stretch it or change its proportions. Do not use the 9C logo's Cs as a source: the logo is
ISACA-supplied artwork only.


- **9C graphic** — the 3×3 grid of Cs, can be centered and cropped as long as 6 full circles stay visible.
- **C graphic strip** — a single row/column of repeating Cs, any palette color/tint.
- **C graphic pattern** — a grid of Cs, all same size, aligned to rows/columns.
- **Cropped C graphic** — one C symbol, heavily cropped, bleeding off at least two edges — used as a
  big, bold background/accent shape (single color).
- **Intersecting C graphics ("the window")** — two same-size Cs overlapped to create a lens-shaped
  "window," used as an image mask/frame. Rules: at least two of the resulting panels must show image;
  at most all panels *plus* the negative space of one C. Color overlays over an image use **normal
  blend mode at 50% opacity** in a brand color (e.g., `#298dcb` "ISACA Blue" tint, or a dark-to-light
  gradient in Orange→Yellow).
- **Base shape / "parts of a whole"** — a full circle (not a C) divided into 4 equal parts that can
  build, intersect, and overlap; edges that aren't touching another piece get rounded like the C is.
- **Graphic "chain" pattern** — Cs interlocked in a chain, used when no photography is appropriate; at
  most 2 brand colors + white.
- **3D abstract curve** — soft gradient blob/curve treatments (not flat), used as an alternate
  background when imagery isn't appropriate. Never combine the gradient-curve asset with the flat C
  graphics — they're separate, mutually exclusive treatments.

---

## 4. Imagery

- Photography: real, natural, well-lit, bright/airy backgrounds. Authentic expressions and poses, not
  stock-photo cliché.
- Prefer **character photography** for tangible/specific/human stories; **abstract/graphic tech
  imagery** for complex ideas or general trend topics (circuit/network/particle style, cool blues +
  occasional magenta accent).
- **Montages** (photo + tech overlay) are fine for humanizing technical concepts — keep the human
  element forward, don't cover eyes/face with overlay elements, avoid busy/cliché combinations.
- **Inclusivity is a hard requirement, not a suggestion** — vary race/ethnicity, skin tone, gender and
  gender expression, relationship status/orientation, culture/customs, disability, language, age,
  income/class, body type, clothing (incl. religious/cultural), hairstyle/head-dress. Avoid
  stereotypical representation.

---

## 5. Illustration

- Style: bold, elegant, sharp — simple geometric shapes (sharp corners) for objects; organic shapes for
  people.
- Stroke: **3px ISACA Dark Blue**, centered, for both shapes and icons.
- Fill: at most two tones/colors per illustration (commonly Green + Blue at different opacities) — never
  randomly mixed, never two colors on the same object.
- Illustration-specific color palette (two tones each):

  | Color | 100% | Reduced |
  |---|---|---|
  | ISACA Dark Blue | `#1d2d5c` | 20% tint |
  | ISACA Blue | `#00a4e4` | 40% tint |
  | ISACA Green | `#6cb33f` | 40% tint |
  | ISACA Yellow | `#fbb034`* | 40% tint |
  | ISACA Purple | `#6558b1` | 30% tint |

  \*the source slide's hex for illustration-Yellow was a copy/paste artifact of Dark Blue — this uses
  the confirmed brand Yellow hex from the main palette instead.

- Use dashed lines/gaps to keep graphics feeling light; don't make gaps too large on an unfilled shape.
- Three "weights" of illustration: **Hero** (full-bleed, complex story, page headers), **Spot hero**
  (simplified hero alongside a text block), **Spot** (single-object, smallest, used as small inline
  blocks/icons).

---

## 6. How this maps onto IsaChatApp today

Some of this is already in place and already correct — worth keeping consistent going forward rather
than re-deriving colors ad hoc:

- `ChatWidget.razor.css`'s `--isa-ink: #1d2d5c` **is** ISACA Dark Blue — correct, keep using it for ink/
  text/primary chrome.
- `--isa-accent: #36ac91` **is** ISACA Teal (secondary palette) — fine as an accent, but note it's a
  *secondary* color in ISACA's system, not primary — don't promote it to a primary action color if a
  primary-palette blue/green would read more "on-brand."
- `--isa-grey-dim` (`#b7bcc9`, used at 0.6 alpha for the panel backdrop) isn't an exact ISACA grey swatch
  — closest official equivalents are ISACA Grey 3 (`#cbd2d9`) or Grey 4 (`#9aa5b1`). Not urgent to
  change, but if the backdrop ever needs revisiting, pull from the table above instead of a one-off value.
- Nothing in the app currently uses the Red/Orange/Yellow/Purple tertiary colors, the C-graphic motif,
  or Roboto Slab/Condensed — all fair game to bring in for future screens (e.g., status colors, empty
  states, a header treatment) rather than inventing new ones.
