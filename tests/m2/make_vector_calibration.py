"""Writes tests/m2/test-vector-calibration.csv (kind,question,expected_document,expected_section,note).

in_scope rows name the passage(s) that answer them (note: pids=...); document and section are filled from
data/content.db. Every in_scope question was checked against its passage text (tests/m2 lane 4, November 9).
Questions an input rule stops or hands off never reach search, so they are not in this set.
"""
import csv, os, sqlite3

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
db = sqlite3.connect(f"file:{os.path.join(REPO, 'data', 'content.db')}?mode=ro", uri=True)
P = {i: (d, s) for i, d, s in db.execute("select id, document, section from passages")}

IN = [
 # Annual Chapter Compliance Framework
 ("What's the compliance framework for?", [1]),
 ("What do we have to keep doing all year to stay compliant?", [2]),
 ("What happens in the annual compliance review at the end of the year?", [3]),
 ("What counts as proof that we report our activities and finances to members?", [4]),
 ("Our audit for this year won't be finished by the compliance deadline. What do we submit instead?", [5]),
 ("Is a privacy policy required?", [6]),
 ("Do we need insurance to be compliant?", [6]),
 # Common Tasks and Navigation Guide
 ("We just elected new officers. How do I get the leader roster updated?", [8]),
 ("Can ISACA host our chapter website?", [9]),
 ("How do I set up registration for a multi-day conference?", [10]),
 ("Why does my chapter website know who I am when I log in to Engage?", [11]),
 ("Can we put content on our website that only members can see?", [12]),
 ("How many requirements do we confirm in the yearly compliance report?", [13]),
 ("How do I get my compliance documents together and submitted?", [14]),
 ("What should we be doing in the third and fourth quarters for the audit and compliance?", [15]),
 ("How many educational events does our chapter need to run?", [16]),
 ("Where do our events have to be posted for compliance?", [16]),
 ("How do I get CPE credit recorded for people who attended our event?", [17]),
 # Core Chapter Leader Roles and Responsibilities
 ("What is the chapter president mainly responsible for?", [20]),
 ("What does the president usually do from week to week?", [21]),
 ("What is the vice president responsible for?", [22]),
 ("What does a vice president normally get involved in?", [23]),
 ("What is our secretary responsible for?", [24]),
 ("Who takes the minutes?", [25]),
 ("What is the treasurer responsible for?", [26]),
 ("What does the treasurer do during the year?", [27]),
 # ISACA Chapter Systems Overview
 ("What is the Chapter Leader Portal?", [29]),
 ("What do I use the leader portal for?", [30]),
 ("What is C360?", [31]),
 ("What can Chapter 360 show me about my chapter?", [32]),
 ("What is Engage?", [33]),
 ("What can I do on Engage as a chapter leader?", [34]),
 ("What is the compliance report used for?", [36]),
 ("Why do we report our events?", [37]),
 ("What does the CPE upload tool do?", [38]),
 ("Where are our board and leader contacts kept up to date?", [39]),
 ("Which system do I use for what?", [40]),
 ("How do the portal, C360 and Engage fit together?", [41]),
 # Chapter Leader Guidebook - President (August 2026)
 ("What does it mean to be the chief elected officer of the chapter?", [42]),
 ("Our chapter is struggling to meet ISACA's standards. What should I do?", [44]),
 ("What is the Strategic Advisory Council and am I on it?", [45]),
 ("Is our chapter its own legal entity?", [46]),
 ("What is my role in the chapter's strategic plan?", [47]),
 ("Who is responsible for running our elections properly?", [48]),
 ("Is governance my job or the whole board's?", [49]),
 ("Who should review the bylaws?", [51]),
 ("How should I work with the treasurer on the chapter's money?", [53]),
 ("Should we require two signatures on large payments?", [54]),
 ("What should I do if I spot an odd transaction in the bank statements?", [55]),
 ("Why should financial reports be produced on time?", [56]),
 ("Who is the chapter's main contact with ISACA Global?", [57]),
 ("What do we have to send ISACA after our fiscal year ends?", [58]),
 ("How should I keep our members informed?", [59]),
 ("What is my role in running board and membership meetings?", [60]),
 ("What should I go over with the treasurer before the board meets?", [61]),
 ("What am I responsible for when we hold a board meeting?", [63]),
 ("Who decides when the next board meeting is and follows up on action items?", [64]),
 ("Do we have to hold membership meetings and who runs them?", [65]),
 ("How do I handle motions at a members' meeting?", [66]),
 ("How is a membership meeting different from a board meeting?", [67]),
 ("Who tells ISACA about our new leaders after the AGM?", [68]),
 ("What else should the outgoing president take care of during the handover?", [69]),
 ("What are the options for showing our leaders on the chapter website?", [70, 71]),
 ("Can we just post a photo of the board on the website?", [71]),
 ("Why aren't our leaders' profiles showing on the website?", [72]),
 ("Should our chapter have a succession plan?", [74]),
 ("Can we add a President-Elect role?", [75]),
 ("What has to be handed over to the new officers?", [76]),
 ("Which unfinished items should be passed to the next president?", [77]),
 ("What happens to the bank accounts and chapter property when leadership changes?", [78]),
 ("What should the incoming president receive from the outgoing one?", [79]),
 ("What should we cover when we orient incoming chapter leaders?", [80]),
 ("When should we start recruiting volunteers for next year?", [81]),
 # from test-paths.ps1 / test-questions-candidates.csv whose answer is clearly in a passage
 ("When does the compliance review start and end?", [3, 13]),
 ("How should the Chapter President prepare the agenda for a board meeting?", [60, 63]),
 ("What should the outgoing president hand over to the new president?", [69, 76, 79]),
 ("What financial review evidence does ISACA accept?", [5]),
 ("What insurance must we have?", [6]),
 ("How should the president work with the treasurer on chapter finances?", [53, 61]),
 ("Can we submit last year's audit if this year's isn't done?", [5]),
]

OUT = [
 "What is the cryptocurrency policy?", "What's the weather going to be like in Chicago next week?",
 "Can you recommend a good hotel near the conference venue?", "Where's a good pizza place near the conference?",
 "What is IEEE's policy on conference sponsorship?", "How does the Rotary Club elect its president?",
 "What are the Toastmasters dues this year?", "What is Bitcoin trading at today?",
 "Should I invest in Ethereum?", "Which airline flies direct to Singapore?",
 "How do I renew my passport?", "What's the best sushi restaurant in Tokyo?",
 "How long is the flight from London to Chicago?", "Who won the World Cup in 2022?",
 "What is the capital of Australia?", "How many moons does Jupiter have?",
 "What year did the Berlin Wall fall?", "How do I lose weight before the conference?",
 "I'm stressed about my job, any advice?", "How do I ask my manager for a raise?",
 "Should I buy or rent a house?", "How do I fix a Python import error?",
 "How do I make a pivot table in Excel?", "My Outlook keeps crashing, what do I do?",
 "How do I reset my Windows password?", "What's the best project management software?",
 "How do I set up a Zoom webinar?", "What does the Paris chapter do for its holiday party?",
 "Who is the president of the Mumbai chapter?", "How did the Toronto chapter grow its membership?",
 "What events is the Sydney chapter running next month?", "Can you write a LinkedIn post about our event?",
 "Translate this paragraph into French for me.", "What's a good name for my dog?",
 "How do I cook rice in a microwave?", "What are the rules of cricket?",
 "What is the speed of light?", "Recommend a good book on leadership.",
 "What does the PMI chapter in Dallas charge for membership?", "How do I train for a marathon?",
]

NEAR = [
 "How much does the CISA exam cost?", "When is the next CISM exam window?",
 "How many CPE hours do I need to keep my CISA?", "What is ISACA's annual membership fee?",
 "How much are chapter dues?", "What is ISACA's global data retention policy?",
 "What is ISACA's code of professional ethics?", "How do I become an ISACA certified trainer?",
 "What is the CPE audit process for certification holders?", "How much rebate does ISACA give each chapter?",
 "What is ISACA's policy on chapter mergers?", "What is the penalty for a late compliance submission?",
 "What happens to a chapter that holds fewer than four board meetings in a year?",
 "What penalty does ISACA apply if a chapter does not hold its board meetings?",
 "How long should each board meeting last and who has to attend?",
 "How many members must attend each educational event?", "What quorum does ISACA require for our board?",
 "What is the maximum term a chapter president can serve?", "How do I apply for an ISACA scholarship?",
 "What is the ISACA Foundation?", "Where do I get help with Engage?", "How do I get help with Engage?",
 "How do I reach the Chapter Experience team?",
]

out = os.path.join(HERE, "test-vector-calibration.csv")
with open(out, "w", newline="", encoding="utf-8") as f:
    w = csv.writer(f); w.writerow(["kind", "question", "expected_document", "expected_section", "note"])
    for q, pids in IN:
        d, s = P[pids[0]]
        w.writerow(["in_scope", q, d, s, "pids=" + "+".join(map(str, pids))])
    for q in OUT: w.writerow(["out_of_scope", q, "", "", ""])
    for q in NEAR: w.writerow(["near", q, "", "", ""])
print(f"in_scope {len(IN)}, out_of_scope {len(OUT)}, near {len(NEAR)} -> {out}")
