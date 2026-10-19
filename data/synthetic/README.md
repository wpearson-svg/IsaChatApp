# Isa Synthetic Test Data (draft, Oct 16, 2026; updated Oct 19, 2026)

**The master copy of this data is now the team's `directory/` folder** (people.csv, role_assignments.csv, chapters.csv). That is the list Isa reads: `directory/build_directory_db.py` builds `data/directory.db` from it, and `directory/build_keycloak_realm.py` builds the sign-in accounts from it. The files in this folder are kept as a matching copy with database-style column names. Make any change in `directory/` first, then copy it here (decided 19 October 2026).

Fictional chapter leaders for the Isa sandbox database. Every person is invented. All emails use example.org, a domain reserved for testing, so no message can ever reach a real person. No real ISACA member data is included.

## Files

- **people.csv**: who someone is. One row per person. Name stored once.
- **role_assignments.csv**: what job someone holds, in which chapter, and when. One row per person, per role, per term.
- **chapters.csv**: the ten test chapters (C-001 to C-010): name, country, region, time zone, and work week. One row per chapter.

The two files link through `person_id`. P-0007 in both files is the same person (Priya Raghunathan). She has two role rows: her past term as President (R-0007) and her current term as Immediate Past President (R-0008). Splitting roles into their own table keeps leadership history when terms rotate.

`role_assignments.csv` links to `chapters.csv` through `chapter_id`. Each chapter's country and time zone match the person who leads it (for example, Watanabe Kenji and Sato Yumiko are both in C-001, Sakura Bay Chapter, Japan).

## Design decisions

- **No separate given-name / family-name fields.** Names do not split the same way worldwide (single names, two surnames, patronymics, Arabic name chains, family-name-first order). The full name is stored as the person writes it, plus a Latin-letter version, a short name for lists, and the form of address the person prefers.
- **Formality** sets formal or informal "you" (vous/tu, Sie/du, usted/tú) in every sentence Isa writes, not just the greeting.
- **Language codes include region** (pt-BR vs pt-PT, es-MX vs es-ES, zh-CN vs zh-TW).
- **Time zones use IANA Region/City names** so deadlines can be shown in local time, including daylight saving.
- **Permission level sits on the role, not the person.** Leadership access ends when the term ends.
- **Dates are stored as YYYY-MM-DD** (the database standard). Isa should always display months as words ("1 November 2026") so day/month order is never ambiguous.
- **Blank title** means the person uses no title.
- **Work week** is stored per chapter (Mon-Fri, or Sun-Thu for Desert Rose Chapter in Saudi Arabia) so business-day deadlines count the right days.
- **Region** is a plain geographic grouping for testing (Asia-Pacific, Europe, Latin America, Middle East, North America), not ISACA's official region list.
- Files are UTF-8 so Japanese, Arabic, and accented letters are preserved.

## What each person tests

- **P-0001 Watanabe Kenji (Japan):** family name first, non-Latin script, "-san."
- **P-0002 Sutrisno (Indonesia):** single name; any required last-name field fails.
- **P-0003 María Fernández López (Mexico):** two surnames, "Lic." title, first-year leader (onboarding questions).
- **P-0004 Guðrún Jónsdóttir (Iceland):** patronymic, first-name address even formally, special letters (ð). Check whether the translation tools support Icelandic; if not, this tests the fallback.
- **P-0005 Andreas Müller (Germany):** academic title in formal address, formal "Sie," ü vs ue.
- **P-0006 Fahad Al-Harbi (Saudi Arabia):** Arabic script, right-to-left text, "bin" in name, Friday–Saturday weekend for business-day counts.
- **P-0007 Priya Raghunathan (India):** English speaker outside the US, UTC+5:30 offset, role history across two terms.
- **P-0008 Sophie Favre (Switzerland):** French in a multilingual country, formal "vous."
- **P-0009 Marcus Johnson (United States):** baseline for comparison. President of C-009, matching the team's directory list.
- **P-0010 Dana Whitfield (United States):** a chapter member (not a leader) in C-009, from the team's directory list. Willis's sign-in test account (d.whitfield) uses this person.
- **P-0012 Sato Yumiko (Japan):** outgoing President of C-001 (term 1 July 2025 to 30 June 2026), now Immediate Past President. Watanabe Kenji's predecessor, so she is the leader who leaves notes in the succession notebook. Added 16 October 2026 for the onboarding feature (first numbered P-0010; renumbered 19 October 2026 because P-0010 was already Dana Whitfield in the team's list).
- **P-0013 Ana Beatriz Souza (Brazil):** President of C-010 since 5 May 2026, so she is on day 60 of her term on the demo date. Also tests Portuguese from Brazil (pt-BR) and a São Paulo time zone. Added 16 October 2026 for the onboarding feature (first numbered P-0011; renumbered 19 October 2026).

## Onboarding demo cast (New Chapter Leader feature)

The onboarding feature works out each president's day of term from their term start date. For the demo, Isa's "demo date" setting is set to 3 July 2026, so the screens look the same whatever day the presentation falls on. (Day 1 is the term start date itself.)

On the demo date:
- **Sato Yumiko (P-0012), C-001:** outgoing president, now Immediate Past President. Saves a decision to the chapter's succession notebook.
- **Watanabe Kenji (P-0001), C-001:** incoming President, term started 1 July 2026, so day 3. Sees the week-one steps, including "Meet your Chapter Experience contact," and finds Sato Yumiko's saved decision in the notebook.
- **Ana Beatriz Souza (P-0013), C-010:** President on day 60. Sees a different next step.

The original nine people are unchanged, except that Marcus Johnson's role now matches the team's list (President of C-009, not Treasurer).

## Open questions

- Do all ISACA chapters start their leadership year on the same date? (Term dates here vary on purpose.)
- Should chapters use real city names or clearly fictional names, so fake data is never mistaken for a real chapter?
- Public holidays per chapter are not included yet.
