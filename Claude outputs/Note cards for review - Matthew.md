# Matthew's note cards: for review

Matthew Brady's speaking notes for the Sprint 2 presentation of **Isa**, by the Second Star Collective team. Eight cards: one for each of Matthew's slides, plus a reference card. Each card shows the slide, a target time, the words Matthew plans to say, stage directions in brackets, and when to click to the next slide.

## About the presentation

- **What it is:** a sprint review. Sprint 2's goal was "Pipeline built": the path a chapter leader's question travels through Isa, from the moment it's typed to an answer, or to a hand-off to a human. The talk covers that pipeline and what it does today.
- **The audience:** i.c.stars staff (the program running the team's internship), not the client. They know the project but not the technical details.
- **Isa:** an AI assistant for the volunteer leaders who run a professional association's local chapters around the world. It answers only from the association's approved documents, shows its sources and its steps, and hands a question to a human (with a reference number) when it can't answer well. The association is always called "the client" in this talk; it is never named.
- **Format:** about 12 minutes for the whole team, over Microsoft Teams, from a slide deck that runs inside the Isa app itself. Isa's chat bubble sits in the bottom-right corner of every slide.
- **Who presents what:** Matthew opens (intro animation, slides 1–5, about 3 minutes); Willis presents what's built and a live demo (slides 6–10); Eliyas presents next steps (slide 11); Matthew returns for slide 12 and the closing (slide 13, about 1 minute).
- **Team rules for the talk:**
  - Say "the client", never the client's name.
  - No AI timing figures (how many seconds an answer takes).
  - Call anything not built "planned".
  - Don't read the slides aloud: explain, give the reason, tell the story.
- **Facts to keep in mind when checking the cards:**
  - Some pipeline stages are real and some are still simple placeholders (Willis's slide 6 shows which).
  - In tests on 26 October, Isa answered 2 of 5 sample questions correctly; the others were passed to a person or were weak. So the cards should not claim that "most" questions are answered easily, or anything else the team can't back up.

---

## Card 1 · Intro + Slide 1: "Meet Isa" (~25 sec)

**On screen:** an opening animation (two C shapes roll in and form a ring), then "Meet Isa / Your chapter support assistant", with a large blue-and-green ring.

**Card:**

> *Let the intro animation play in silence. Start once "Meet Isa" is on screen.*
>
> Good afternoon, everyone. We're Second Star Collective. I'm Matthew, and with me are Willis and Eliyas.
>
> We'd like to introduce someone. Isa helps the volunteers who run the client's local chapters find answers they can trust, any time of day.
>
> And she's already in the room. *(Point to the bubble, bottom right.)* That little bubble in the corner is Isa, live, not a picture of her. She'll stay there the whole time, and in a few minutes Willis is going to put her to work.
>
> **CLICK**

## Card 2 · Slide 2: "Pipeline built" (~25 sec)

**On screen:** "Pipeline built", a row of ten dots lighting up left to right, "Every question now runs end to end, through all ten stages.", "Second Star Collective".

**Card:**

> This sprint had one goal, and that's it up there.
>
> Think of it as the plumbing behind Isa. Last sprint, we had good pipes lying on the workbench. Now they're fitted together, and the water runs: a leader pours a question in at one end, and it flows all the way through until it comes out as an answer, or gets piped to a real person. You'll see that happen live in a few minutes.
>
> And to be honest from the start: not every part is finished. Some are simple placeholders for now, and Willis will show you exactly which.
>
> **CLICK**

## Card 3 · Slide 3: "The problem" (~35 sec)

**On screen:** "The problem"; a large "6" with "people on the support team"; "Chapter leaders are volunteers. They need approved guidance *fast*, and a six-person support team cannot answer every question."; two panels: "Isa answers from approved content." and "Isa hands off to a person when it should."

**Card:**

> So why build this at all?
>
> Picture yourself as a chapter president. You've got a full-time job, it's nine at night, and you need to know an election rule before tomorrow's board meeting.
>
> *(Point to the 6.)* Now look at who you can call. That's the whole team, for chapters all over the world. The math just doesn't work.
>
> *(Point to the two panels.)* So Isa splits the work. If the answer is already written down in the client's documents, she gives it to you, right then. If it needs a human being, she makes sure it gets to one.
>
> She's there to take the pressure off that team, not to take their place.
>
> **CLICK**

## Card 4 · Slide 4: "What Isa promises" (~35 sec)

**On screen:** "What Isa promises / Three rules, shown on every answer." Three cards: "Approved content only: answers come only from approved content, in the Client's own words, with the source shown." · "Isa shows its work: every answer shows its steps, one by one, in plain language." · "A person takes over: when Isa can't answer well, a person takes over and the leader gets a reference number."

**Card:**

> These three are what make Isa trustworthy.
>
> *(First card.)* If it isn't in the client's own documents, Isa won't say it. No guessing, no internet, and every answer tells you which page it came from.
>
> *(Second card.)* Nothing happens behind a curtain. A leader can open up any answer and follow exactly how Isa got there.
>
> *(Third card.)* And Isa knows her limits. When she can't help, she says so, passes the question on, and the leader walks away with a number to quote when they follow up.
>
> **CLICK**

## Card 5 · Slide 5: "One question, ten stages" (~50 sec)

**On screen:** "One question, ten stages / Every question moves through the stages in order. The AI stages run only when they're needed." Ten numbered circles lighting up in turn: Front end, Access control, Translation, Input compliance, Database search, Knowledge graph, RAG, Complex AI, Output compliance, Ticketing. Under them, three bars: "Cheap path: a direct text match answers straight from the approved text. No AI." (stages 1–5 and 9) · "AI path: runs only when it's needed, which keeps cost down." (stages 1–9) · "To a person: ticketing runs when a question goes to a person." (stage 10).

**Card:**

> *Let the stages light up for a moment first.*
>
> Let's follow one question through.
>
> It arrives. Isa checks who's asking and makes sure it's something she should be answering. She goes looking in the client's documents. Before anything goes back to the leader, she checks her own work. And if a person needs to step in, she opens a ticket for them.
>
> *(Blue bar.)* Here's the clever part. Most questions are easy: the answer is sitting right there in the documents, word for word, so Isa just hands it over. The AI never even wakes up.
>
> *(Dark blue bar.)* The trickier ones, where the answer is spread across a few pages, take the longer road. That's where the AI pieces it together, and only from what Isa found.
>
> *(Green bar.)* And on either road, if it needs a human, it ends with one.
>
> Enough from me. Willis is going to show you what's actually working, and then let Isa prove it live.
>
> **DON'T CLICK: hand over to Willis**

## Card 6 · Slide 12: "Additional features" (~45 sec)

**On screen:** "Additional features". Left, labelled "In the build, not finished": Onboarding (the Today panel and the first-90-days journey) and Succession notebook (leaders leave notes for the person who takes over). Right, labelled "Planned": Peer content panel, Offline packages, Android app wrapper, Chrome extension, Multi-model testing.

**Card:**

> *After Eliyas. Take the clicker back.*
>
> Thanks, Eliyas.
>
> The pipeline was this sprint's job, but we've already started building on top of it.
>
> *(Left side.)* Every year, chapter leadership turns over, and someone new starts from zero. These two are for that moment. One meets a brand-new president on day one and tells them what to focus on this week. The other lets the outgoing president leave behind what they learned. Both work in the app today; we're still polishing them.
>
> *(Right side.)* And this side is our wish list: getting Isa to leaders wherever they are, on a phone, in a browser, even with a weak connection, plus letting them learn from other chapters, and tools to help us choose the best AI model. Those are ideas for now. None of them are built.
>
> **CLICK**

## Card 7 · Slide 13: Closing (~20 sec)

**On screen:** "Isa only answers from approved content, shows every step, and hands off to a person with a reference number whenever it can't." "Thank you." "Second Star Collective."

**Card:**

> *Let them read the line for a second first.*
>
> That's sprint 2. What we set out to build is built, and it's working: ask Isa a question, and you get an answer you can check, or a person you can talk to.
>
> Thank you for your time and your support this sprint. We'd love to take your questions.

## Card 8 · Keep in mind (reference)

**Remember**
- Say "the client", never the client's name.
- No model timing (how many seconds an answer takes).
- Say "planned" for anything not built.
- Don't restart Isa during the talk: ticket numbers start again.
- Look at the room, not the card. Don't read the slide.

**If you're running long**
- Slide 3: skip the "Picture yourself" story; start at the 6.
- Slide 5: skip the walk through the stages; keep the three bars.
- Slide 12: "The two on the left work today and we're still polishing them; the right side is planned." Then click.

**Your timing**
- Opening (intro, slides 1 to 5): about 3 minutes.
- Slides 12 and 13: about 1 minute.
