# Foundation content + test database (T7 / T8)

Source: the 4 Chapter Leader Processes documents + the President Guidebook PDF (provided to the team for this project).
Chapter 360 (6 docs) is deferred to Pipeline Built.

## Files (put in the repo under content/ as shown)
- content/content.json - 81 passages (document, section, page, text). Word files have no pages, so page = "n/a"; the PDF keeps real page numbers.
- content/meaning_map.json - plain-language questions mapped to the right passage (AI-fallback path).
- content/rechunk_pdf.py - re-splits the PDF guidebook on sentence boundaries (no mid-sentence starts, no page numbers, no table of contents). Run before build_content_db.py if content.json is ever regenerated from the PDF.
- content/build_content_db.py - rebuilds data/content.db from empty every run. Python 3 only, no packages.
  It skips table-of-contents passages (any passage that says "Table of Contents" or has two or more dot-leader lines like "Finances ........ 3"), so the database holds 80 passages. Those passages matched many searches and showed up in answers as rows of dots.

## Build
    python content\build_content_db.py
Output: data\content.db (tables: documents, sections, passages [FTS5], meaning_map).

## Change needed in DatabaseSearchStage (StubStages.cs)
Raw question text breaks FTS5 ("?" is a syntax error and errors are swallowed). Build the query like this:
1. Lower-case the question, keep letters/digits only.
2. Drop filler words: the how many what does must each year have our and who are for when can with that this you is do we to a of in chapter hold policy period annual
3. If terms remain: SELECT document, section, page, text FROM passages WHERE passages MATCH $q ORDER BY rank LIMIT 5, with $q = terms joined by " AND ".
4. If no rows: the meaning_map fallback, unchanged (it already joins on passages.id, which now exists).
5. Still nothing: return the "no content" result.

## Test questions (checked against the built database)
| Path | Question | Expected |
|---|---|---|
| Direct hit | How many board meetings must a chapter hold each year? | Guidebook p.7: minimum of four |
| Direct hit | How many educational events must a chapter hold? | Common Tasks guide: at least four |
| AI fallback | Who writes down what the board decided? | Guidebook p.7: documented by the Secretary |
| AI fallback | Do we have to show members how we spent money? | Compliance Framework: annual reporting of activities, revenues, expenditures |
| AI fallback | When do we send in our yearly paperwork? | Common Tasks guide: 1 Nov - 31 Dec |
| No content | What is the cryptocurrency policy? | nothing found -> Escalate path |
| Stopped | (any prompt-injection style text, per T11a) | Stop at Input Compliance |

Known gap: "When is the annual compliance review period?" does NOT hit directly (the dates sit in a heading). Use the yearly-paperwork wording above.
