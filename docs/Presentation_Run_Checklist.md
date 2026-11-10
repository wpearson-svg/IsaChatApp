# Presentation run checklist

Go through this before every rehearsal and before the final. Tick each box; if one fails, fix it before moving on.
Times are how long before you start. Written November 10, 2026, for the locked build.

## Final only: the day before

- [ ] The locked build is what's on the machine: `git status` shows no changes, and `git log -1` is the tagged lock commit.
- [ ] Decide and write down: Salesforce **on** (cases shown live with Alt+C) or **off** for the final.
- [ ] Close the leftover test Cases in Salesforce, so Alt+C shows only today's.
- [ ] Any numbers typed into the slides locally on the day (the survey figures, the typical wait times) are filled in.
- [ ] Teams: you can share the screen from this machine, and the slides fill it at 16:9.

## 60 minutes before

**The home PC (services)**
- [ ] The home PC is on, awake, and won't sleep (power settings). Docker Desktop is running (started by you, not from a Claude session).
- [ ] Tailscale is connected on both machines.
- [ ] Open the debug launcher. Every light is green: on this computer ("●", for whatever runs locally) and the remote "●R" for Ollama, Keycloak, Voice, Translation and Embeddings.

**Reset the demo data** (Isa must be stopped)
- [ ] Stop Isa (launcher, or `stop-isa.bat`).
- [ ] `python onboarding\prepare_demo.py` runs with no errors. It clears test progress and notebook entries and sets up Sato's entries, Kenji's board introduction and Sato's profile reply.

**Start Isa**
- [ ] Start Isa (launcher, or `start-isa.bat`). The console's start-up line says `Services: Remote (100.89.6.69)` (or Local, if you present from the home PC).
- [ ] `python rules\check_rules.py` ends with **114 checks, 0 problems**.

**Settings** (Dev Dashboard > Compliance policy; Isa restarts don't change these, they're saved in `output-compliance-settings.json`)
- [ ] NLI pass mark **0.40**
- [ ] Answer confidence threshold **0.70**
- [ ] Strong-match mark **0.00**
- [ ] Search pass mark **0.82**
- [ ] Direct-answer match mark **0.75**
- [ ] Isa Configuration (`/isa-config`): demo date **3 July 2026**, "use the demo date" on. (A restart puts both back to these values, so they only go wrong if someone changed them since the last start.)
- [ ] Isa Configuration (`/isa-config`): switch **"Show the presentation slides"** on. It goes **off on every restart**; with it off, `/present` only says "The presentation is switched off".

## 15 minutes before

- [ ] Open Isa in the browser and sign in as **k.watanabe** (the demo account, with the developer role).
- [ ] Press **Ctrl+F5** once after signing in (clears the "Not signed in" glitch).
- [ ] Warm the model: ask "How do I reach the Chapter Experience team?" It should answer in a few seconds, from ISACA's guidance.
- [ ] Warm translation: press Alt+8 and Enter. The answer comes back **in Spanish**. If it comes back in English, ask it once more (the first translation after a quiet spell times out).
- [ ] Clear the chat (reload the page) so the warm-up questions aren't on screen.

## 5 minutes before

- [ ] Open the presentation (`/present`). The opening screen's check says **Ready**: Ollama running, the model loaded, signed in with the developer role, Salesforce ok (or off). If it says the model is loading, wait; don't press on until it says Ready.
- [ ] Close every other tab and app; notifications off (Windows Focus, Teams, Outlook).
- [ ] Browser zoom 100%, full screen (F11).

## During the run

**Live demo: ask Isa** (real questions, each checked through the real pipeline: same ending every run)
- [ ] Alt+1 "How many board meetings must a chapter hold each year?" ISACA's own words, with the page, and Show the work. Instant.
- [ ] Alt+2 "What should the outgoing president hand over to the new president?" An answer written by the AI, every check passed. About 2 seconds.
- [ ] Alt+3 "Where's a good pizza place near the conference?" Stopped: outside what Isa covers.
- [ ] Alt+4 "A board member keeps harassing me at meetings." To a person, with the safety line and a reference number.
- [ ] Alt+5 "Is a compilation fine for the financial review if ISACA approves it in writing?" Isa holds back the AI's answer (rule C-08) and passes it to a person. 2 to 4 seconds.
- [ ] Alt+8 and Alt+9: questions 1 and 2 in Spanish. The same answers, in Spanish. About 20 seconds each, so talk while they run.

**Live demo: Kenji, day 3**
- [ ] The Today note shows day 3 and the "Last year" row.
- [ ] The contact step, Meet your board, Sato's page.
- [ ] The calendar (November shows the compliance window).
- [ ] The notebook: Sato's saved decision and "Ask Sato Yumiko about this".
- [ ] Kenji asks a question and saves the answer to the notebook.

**Shortcuts**
- Alt+1 to Alt+5 type the five demo questions above into Isa; Alt+8 and Alt+9 type questions 1 and 2 in Spanish. Press Enter to send.
- Alt+6 Dev Dashboard, Alt+7 Content Health, Alt+C live Salesforce Cases, Esc closes a panel.

**If something goes wrong**
- An answer takes more than about 20 seconds, or a service drops: say what it would have shown and go to the next slide. The slides themselves need no services.
- Show the work and Kenji's walkthrough don't need the AI model, so they still work if only the model is down.
- A Spanish answer comes back in English: translation was cold; say so and move on.

## After each run

- [ ] Note anything that went wrong, with the time, for the team.
- [ ] Before the next run, start again from "60 minutes before" (prepare_demo.py resets the demo; the warm-ups matter again after a quiet spell).
