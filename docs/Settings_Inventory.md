# Isa settings inventory

Written 4 November 2026 from the code on branch Willis-Pearson. Every setting the code reads, where its value lives today, and where it should live. Nothing in the code was changed to write this.

The idea (Willis): much of Isa should be tunable by the client. In the demo we can say, for example, "Because this setting is 0.85, a failed check on a weak search match goes to a person. You can tune it on the Dev Dashboard to suit you: fewer AI calls, fewer escalations, or anything else you need."

## Where settings can live

| Place | What belongs there | Who changes it |
|---|---|---|
| **Dev Dashboard, tuning** | Thresholds and switches a client tunes to their own taste. Applies to the next question, no restart. | The client's admin (dashboard_dev role) |
| **Dev Dashboard, connections and keys** | Service addresses, accounts and secrets. Secrets are write-only on screen ("Saved. Leave blank to keep"). May need a restart. | The client's admin |
| **Config file** (appsettings.json) | Where files and models sit on the server, logging. Set once when Isa is installed. | Whoever installs Isa |
| **Code or test only** | Switches that bypass Isa's controls or exist only to test it. Never on the dashboard of a real deployment. | The team |

## The settings

**On the dashboard** means it is a working control there today. "Shown only" means the value is displayed but can't be changed.

### Answer quality and hand-offs (tuning)

| Setting | Value now | On the dashboard | Should be | What it does |
|---|---|---|---|---|
| OutputCompliance:NliThreshold | 0.50 default (0.40 in use) | Yes, slider | Dashboard, tuning | Lowest support score a sentence of an AI answer may have. Lower: fewer answers trimmed or replaced. |
| OutputCompliance:ConfidenceThreshold | 0.70 | Yes, slider | Dashboard, tuning | Pass mark for the confidence score (search strength and share of supported sentences). |
| OutputCompliance:StrongMatch | 0.0 (not set) | Yes, slider "Strong-match mark" (added 4 November) | Dashboard, tuning | After a failed check: at or above this search score, show ISACA's own words; below it, pass the question to a person. At 0.0 Isa never escalates after a failed check. Tested at 0.85: questions scoring 0.81 to 0.84 went to a person, 0.85 and up got ISACA's words. |
| VectorSearch:MinScore | 0.80 | Yes, slider "Search pass mark" (added 4 November) | Dashboard, tuning | How close a passage must be to the question for Isa to use it. Higher: more "not covered" hand-offs, fewer AI calls. |
| VectorSearch:TopK | 3 | No | Dashboard, tuning | How many passages the AI is given. |
| OutputCompliance:FixedChecks | true | No | Dashboard, tuning (one switch per check) | Turns on the numbers, must-stays-must, contact and answer-shape checks. |
| AnswerWording:Enabled | true | No | Dashboard, tuning | Joins broken PDF lines into whole sentences in direct answers. |
| Rules:Engine | true | No (the rule tester is read-only) | Dashboard, tuning | Uses ISACA's rules files; false falls back to the old keyword stub. |
| Ticketing:Salesforce:Enabled | true | Partly (the Salesforce section) | Dashboard, tuning | Sends hand-offs to Salesforce as Cases. |

### The AI model (tuning)

| Setting | Value now | On the dashboard | Should be | What it does |
|---|---|---|---|---|
| Ollama:Model | phi4-mini:3.8b | No | Dashboard, tuning | Which model writes answers. |
| Ollama:TimeoutSeconds | 45 | No | Dashboard, tuning | How long Isa waits before passing the question to a person. |
| Ollama:NumPredict | 300 | No | Dashboard, tuning | Longest answer the model may write. |
| Ollama:KeepAlive / IdleKeepAlive | 60m / 2m | No | Dashboard, tuning | How long the model stays loaded (presentation mode / normal). |
| Presentation:Mode | false | On the Isa Configuration page | Dashboard, tuning | Keeps the model loaded for a talk. |

### Voice and translation

| Setting | Value now | On the dashboard | Should be |
|---|---|---|---|
| Translation:Enabled | true | Yes | Dashboard, tuning |
| Tts:Enabled | true | Yes | Dashboard, tuning |
| Tts:Voice | bf_lily | No | Dashboard, tuning |
| Tts:Pronunciations | ISACA → Eyesocka | No | Dashboard, tuning (a small table) |
| Tts:PreGenerate, Tts:WarmOnStart | true | No | Dashboard, tuning |
| Translation:TimeoutSeconds, Embeddings:TimeoutSeconds | 30 | No | Dashboard, connections |

### Connections and keys

| Setting | Value now | On the dashboard | Should be |
|---|---|---|---|
| Services:Location, Services:RemoteHost | Local / 100.89.6.69 | Yes (needs a restart) | Dashboard, connections |
| Ollama:Endpoint, Tts:Endpoint, Translation:Endpoint, Embeddings:BaseUrl, Keycloak:Authority | localhost addresses | Only through the Local/Remote switch | Dashboard, connections: one field each, with a "Check" button (the "Addresses in use" list is already there) |
| Salesforce org URL, API version, client ID, secret | in dev-keys.json | Yes | Dashboard, connections (done) |
| Keycloak:ClientId, Keycloak:ClientSecret | isa-web / in the config file | No | Dashboard, connections, secret write-only like Salesforce |
| Keycloak:AdditionalIssuers, Keycloak:AllowedClients | lists | No | Dashboard, connections |
| Ticketing:Salesforce:TimeoutSeconds | 10 | No | Dashboard, connections |

### Isa Configuration page (WidgetSettings)

Demo date and "use the demo date", show the work, user/debug panel view, minimum time per stage (MinStageMs), response time, bubble colours, the "Isa" label, presentation switch. They work, but **they are kept in memory only: a restart puts them back to their defaults** (the demo date goes back to 3 July 2026). Recommendation: save them to a file the same way output-compliance-settings.json is saved.

### Config file only (set at install)

ContentDb:Path, DirectoryDb:Path, EventLog:Path, NotebookDb:Path, OnboardingDb:Path, ProgressDb:Path, PersonRequestsDb:Path, Usage:Path, VectorSearch:Path, Rules:Folder, Hhem:ModelPath, Hhem:TokenizerPath, Hhem:MaxTokens, Logging, AllowedHosts. These say where things are on the server. Moving them from the dashboard would break Isa, so they stay in the file.

### Code or test only (never on a real deployment's dashboard)

| Setting | Why |
|---|---|
| AccessControl:DevBypass | Skips sign-in. |
| OutputCompliance:ForceFail | Forces a check to fail, for tests. |
| Testing:AllowForcedFailures | Lets the test header turn the model off. |
| AnswerWording:ExemptQuestions | The old rehearsed-question list. Empty now; can be removed. |
| Hhem:SelfTest, Rules:SelfTest | Start-up self-checks. |
| Translation:DemoFile | Predetermined Amharic replies for the demo. |
| VectorSearch:Enabled | Turning search off would send most questions to a person. Keep for testing only. |

## Dashboard controls that looked real but weren't (fixed Nov 5)

The Compliance policy page used to carry controls that did nothing. They are now real or gone:
- **Always route to a person** (topic chips): replaced by the real list of rules that hand a question to a person or stop it, read from rules\ (read-only; change the rules files).
- **Output checks** (three switches): replaced by a read-only list of the checks as they run, with the live settings.
- **Role access by content tier**: Isa has no content tiers. The Role access tab now lists the Keycloak roles Isa actually checks (chat_user, dashboard_dev).
- **The change log**: now real. Saves and resets of the compliance numbers, the Services settings and the Salesforce settings are written to settings-change-log.jsonl (who, when, old value, new value; secrets are never written).
- **The simulated testing console** on the Compliance page: removed. The Testing console tab runs the real pipeline.

Still not built, and labelled "Not built yet": Front end and Health alerts.

## Before settings move to the dashboard

1. **Saved, not just in memory.** Use the pattern output-compliance-settings.json already follows: a git-ignored file, "Reset to defaults" deletes it.
2. **Logged.** Each change is written with who changed it, when, the old value and the new value (SettingsChangeLog; done for the settings already on the dashboard).
3. **Limited to the dashboard_dev role.** Secrets are write-only.
4. **Each control says what it trades off.** For example, StrongMatch: "Higher: more questions go to a person when a check fails. Lower: more answers use ISACA's own words."

## Suggested order

1. Done 4 November: StrongMatch and VectorSearch:MinScore are sliders beside the two that existed, saved in output-compliance-settings.json. Test path 14 (escalated after a failed check) works with the strong-match mark at 0.85: "What should I know about running events?" (search score 0.82, fails the must-stays-must check) goes to a person.
2. Save the Isa Configuration page settings, so a restart doesn't reset the demo date.
3. Make the change log real.
4. Add the model settings (model, timeout, answer length) and the per-service endpoints with a Check button.
5. Move the Keycloak client secret into the dev-keys store.
