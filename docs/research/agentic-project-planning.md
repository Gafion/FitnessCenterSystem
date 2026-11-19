# Planning Software Projects for Agentic Coding

**Research note for:** FitnessCenterSystem (integrated member and employee administration for a fitness center)
**Date:** 2026-11-12
**Status:** Research input only. This note sets out options and trade-offs. It does not make project decisions.
**Convention:** Every factual claim has an inline source URL. A claim I could not confirm against a primary source is marked **UNVERIFIED**. Text labelled *Synthesis* is my own combination of the sourced findings, not a claim from a source.

---

## 1. Executive summary

1. **Every major tool and practitioner says to plan in writing before coding, but they disagree on how much.** GitHub Spec Kit, Amazon Kiro, BMAD, Taskmaster, Harper Reed's workflow and Addy Osmani all share one pipeline: idea, then spec/PRD, then plan/design, then a task list, then incremental implementation ([Spec Kit](https://github.com/github/spec-kit/blob/main/spec-driven.md), [Kiro](https://kiro.dev/docs/specs/), [Harper Reed](https://harper.blog/2025/02/16/my-llm-codegen-workflow-atm/), [Osmani](https://addyosmani.com/blog/good-spec/)). Critics warn about overhead. Thoughtworks' Birgitta Böckeler found Kiro turned a small bug into "four user stories with 16 acceptance criteria" and said "I'd rather review code than all these markdown files" ([martinfowler.com](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html)).
2. **Anthropic's current Claude Code guidance comes down to three things:** tell Claude what it cannot see, give it a check to run, and enforce rules that cannot have exceptions ([best practices](https://code.claude.com/docs/en/best-practices)). CLAUDE.md should stay short (the docs say to target under 200 lines) and should hold only what cannot be inferred from the repo ([memory docs](https://code.claude.com/docs/en/memory)). Hard rules belong in hooks or permission rules, because CLAUDE.md is guidance the model may judge not to apply ([best practices](https://code.claude.com/docs/en/best-practices)).
3. **Context is a finite "attention budget."** The recommended techniques are just-in-time retrieval, compaction, structured note-taking and sub-agents ([Anthropic, context engineering](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents)). For planning documents this argues for many small, linkable files over one large spec ([Osmani](https://addyosmani.com/blog/good-spec/), [Nygard](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)).
4. **Agents do best on small, verifiable tasks.** METR measured near-100% success on tasks that take humans under about 4 minutes and under 10% on tasks over about 4 hours (March 2025 data, since marked partly outdated) ([METR](https://metr.org/blog/2025-03-19-measuring-ai-ability-to-complete-long-tasks/)). Anthropic's long-running-agent harness works on one feature per session against a JSON feature list with a `passes` flag ([Anthropic, harnesses](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)).
5. **Tests are the main control.** Kent Beck calls agents "unpredictable genies" and lists "disabling or deleting tests" as a warning sign ([Beck](https://newsletter.kentbeck.com/p/augmented-coding-beyond-the-vibes)). Willison: "the one thing you absolutely cannot outsource to the machine is testing that the code actually works" ([Willison](https://simonwillison.net/2025/Mar/11/using-llms-for-code/)).
6. **Main failure modes:**
   - Doing too much at once, and declaring victory early ([Anthropic, harnesses](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)).
   - Building unrequested functionality ([Beck](https://newsletter.kentbeck.com/p/augmented-coding-beyond-the-vibes)).
   - Ignoring the spec or duplicating existing code ([Böckeler](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html)).
   - Spec drift ([Thoughtworks](https://www.thoughtworks.com/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices)).
   - Hallucinated packages: at least 5.2% of packages suggested by commercial models and 21.7% by open-source models did not exist ([Spracklen et al., USENIX Security 2025](https://arxiv.org/abs/2406.10279)).
7. **For this project specifically:** member data in a fitness center may include health and biometric data, which are GDPR Art. 9 special categories ([GDPR Art. 9](https://gdpr-info.eu/art-9-gdpr/)). That makes a privacy/compliance document a likely early need. Whether such data is in scope is an open question for the owner.

---

## 2. Findings

### 2.1 Spec-driven development (SDD) tools and methods

#### GitHub Spec Kit
- **Philosophy:** "Specifications don't serve code—code serves specifications." Maintenance means evolving specs ([spec-driven.md](https://github.com/github/spec-kit/blob/main/spec-driven.md)).
- **Commands:** `constitution`, `specify`, `plan`, `tasks`, `implement`. The current README also lists `converge`, run repeatedly until the output reports "Converged" ([README](https://github.com/github/spec-kit)). spec-driven.md mentions `analyze` as a compliance check against the constitution. I could not confirm a `/clarify` command on the pages I fetched (**UNVERIFIED**). Command naming has changed between versions (`/speckit.plan` vs `/speckit-plan`).
- **Artifacts per feature:**
  - `spec.md`: user stories, requirements and acceptance criteria, with no implementation detail.
  - `plan.md`: the technical plan and its rationale.
  - `research.md`: technical research, such as library comparisons.
  - `data-model.md`: entities and schemas.
  - `contracts/`: API and event contracts.
  - `quickstart.md`: key validation scenarios.
  - `tasks.md`: the task list. `[P]` marks tasks that can run in parallel.
  - A project-wide `memory/constitution.md` ([spec-driven.md](https://github.com/github/spec-kit/blob/main/spec-driven.md)).
- **Guards against guessing:** templates require ambiguities to be marked `[NEEDS CLARIFICATION: …]` rather than guessed, and a checklist requires that none remain. This matches the owner's "no guesses" rule closely.
- **Guards against over-engineering:**
  - "No future-proofing" phase gates, with exceptions logged in a Complexity Tracking section.
  - Every feature must trace back to a user story.
  - Test-first ordering.
  - Example constitution articles: Test-First ("NON-NEGOTIABLE"), Simplicity, Anti-Abstraction, Integration-First Testing ([spec-driven.md](https://github.com/github/spec-kit/blob/main/spec-driven.md)).

#### Amazon Kiro specs
- **Three files per spec:**
  - `requirements.md` (or `bugfix.md` for bugs).
  - `design.md`: architecture, sequence diagrams, error handling and testing strategy.
  - `tasks.md`: discrete, trackable tasks. Running all tasks builds a dependency graph and executes independent tasks in parallel "waves."
- **Variants:** Requirements-First, Design-First and "Quick Spec." A bugfix spec records current, expected and unchanged behaviour ([Kiro docs](https://kiro.dev/docs/specs/)).
- **Acceptance criteria:** Kiro writes them in EARS form ("WHEN … THE SYSTEM SHALL …"). I found this only in third-party and mirrored pages, not on the official page I fetched, so the exact template wording is **UNVERIFIED** ([CodeMySpec](https://codemyspec.com/blog/kiro-specs-explained), [promptz](https://www.promptz.dev/rules/general/kiro-specs/)).

#### BMAD Method
- **What it is now:** the official docs describe BMad as named "skills" for Claude Code, Cursor and similar tools. It runs a delivery loop of Clarify, Plan, Build and verify, then Learn and adjust. "Small changes go straight to build. Complex work gets the depth it needs" ([BMAD docs](https://docs.bmad-method.org/), [GitHub](https://github.com/bmad-code-org/BMAD-METHOD)).
- **Versions:** v6.12 is stable and v7 is in alpha ([GitHub](https://github.com/bmad-code-org/BMAD-METHOD)).
- **Older persona model:** the widely reported pipeline of personas (Analyst, PM, Architect, Scrum Master, Dev, QA) producing brief, PRD, architecture, epics and story files did not appear on the current pages I fetched (**UNVERIFIED** for the current version).

#### Taskmaster (claude-task-master)
- **Workflow:** write a PRD (recommended at `.taskmaster/docs/prd.txt`), then run `task-master parse-prd` to generate tasks. Other commands include `task-master next`, `expand_task`/`expand_all` for subtasks, `analyze_project_complexity` and dependency handling.
- **Integration:** works with Claude Code via MCP ([GitHub](https://github.com/eyaltoledano/claude-task-master)).
- **Input quality matters:** the README notes that a more detailed PRD produces better tasks.

#### "PRD → tasks" practitioner workflow (Harper Reed)
1. Hone the idea with a conversational LLM: "Ask me one question at a time so we can develop a thorough, step-by-step spec". The output is `spec.md`.
2. Have a reasoning model produce `prompt_plan.md`, a blueprint of small, test-driven prompts.
3. Produce `todo.md` as a checklist the agent ticks off, which carries state across sessions.
4. Execute the prompts one at a time.

Caveats from the author: "over my skis" risk, Claude defaulting to React unless the stack is set up first, and the workflow being single-player ([Harper Reed](https://harper.blog/2025/02/16/my-llm-codegen-workflow-atm/)).

#### Critical perspective (Thoughtworks)
- **Böckeler's three levels:**
  - *spec-first*: write a spec before AI-assisted coding.
  - *spec-anchored*: keep the spec after the task and maintain it.
  - *spec-as-source*: humans edit only the spec.

  She found the tools mostly spec-first. She compares spec-as-source to model-driven development, which never took off for business apps ([martinfowler.com](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html)).
- **Liu Shangqi (Thoughtworks, 2025-12-04):** treats specs as drivers while executable code stays the source of truth. Recommends domain-oriented language, Given/When/Then scenarios and specifying pre/postconditions and invariants. Warns of spec drift and that over-formalised specs slow feedback ([Thoughtworks](https://www.thoughtworks.com/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices)).
- **Radar status:** spec-driven development was placed in "Assess" on the Thoughtworks Radar in November 2025 ([Radar](https://www.thoughtworks.com/radar/techniques/spec-driven-development)). The current ring is **UNVERIFIED**.

*Synthesis:* what the tools share is (a) *what/why* kept separate from *how*, (b) explicit task lists with status, (c) test-first ordering and (d) human review gates between phases. They differ mainly in ceremony and in how many files they generate.

### 2.2 Anthropic / Claude Code practices

| Topic | What the current docs say | Source |
|---|---|---|
| Core framing | Instructions are either non-deterministic (prompt, CLAUDE.md, skills: "Claude decides") or deterministic (hooks, deny rules, managed settings: "Claude Code enforces"). "If you keep adding emphasis to a CLAUDE.md line and Claude still misses it, that's a sign the line belongs in a hook." | [best-practices](https://code.claude.com/docs/en/best-practices) |
| What to write down | "Only write down what Claude can't find by reading the repo." The test: could it find this by reading the code, and would it actually look? | same |
| CLAUDE.md content | Include exact test, typecheck and lint commands; places where the code is misleading; decisions that aren't in the repo; "anything you've had to correct twice." Describing architecture and file layout is listed under "What you can stop doing." Target under 200 lines. `@path` imports organise a file but don't reduce its context cost. Path-scoped rules go in `.claude/rules/` with a `paths:` filter. | [best-practices](https://code.claude.com/docs/en/best-practices), [memory](https://code.claude.com/docs/en/memory) |
| AGENTS.md | Claude Code reads `AGENTS.md` when there is no CLAUDE.md, or when a CLAUDE.md imports it. AGENTS.md is an open format stewarded by the Agentic AI Foundation (Linux Foundation) and used by Codex, Cursor, Gemini CLI, Copilot and others. | [memory](https://code.claude.com/docs/en/memory), [agents.md](https://agents.md/) |
| Spec via interview | "Interview me until you could write the spec, then write it to SPEC.md." Then start a fresh session to build from it. | [best-practices](https://code.claude.com/docs/en/best-practices) |
| Plan mode | Claude reads files and proposes a plan but makes no edits until you approve. Start with `--permission-mode plan` or press Shift+Tab. | [common-workflows](https://code.claude.com/docs/en/common-workflows) |
| Explore → plan → code → commit | Anthropic's 2025 best-practices post presented this sequence. The old URL now redirects to the docs page, which no longer uses that name, so the current status of the exact wording is **UNVERIFIED**. The ingredients are still there: plan mode, a spec, a check to run, and commit/PR workflows. | [best-practices](https://code.claude.com/docs/en/best-practices) |
| Verification | "Give Claude a check to run": tests, build exit codes, linters, screenshots. Strictness can be set in the prompt, with `/goal`, with a `Stop` hook (exit code 2 blocks the stop) or by an independent `/code-review`. Ask for *evidence*, not "all tests pass." | [best-practices](https://code.claude.com/docs/en/best-practices) |
| Review against spec | "use a subagent to review the … diff against SPEC.md … report gaps that affect correctness and skip style". Without "skip style" you end up with "extra abstraction layers and tests for cases that can't happen." | same |
| Subagents | Defined in `.claude/agents/*.md`. Each has its own context window, a tool allowlist and its own permissions. Good for isolating noisy output, parallel research and read-only review. The docs now say subagents are *not* mainly for saving context, and a fresh reviewer helps "because it hasn't seen your reasoning." | [sub-agents](https://code.claude.com/docs/en/sub-agents), [best-practices](https://code.claude.com/docs/en/best-practices) |
| Skills | Team procedures go in `.claude/skills/<name>/SKILL.md`. Claude sees only the name and description until a task needs the skill. `disable-model-invocation: true` makes a skill run only when the user invokes it, for workflows with side effects. | [best-practices](https://code.claude.com/docs/en/best-practices) |
| Hooks and permissions | Use for rules that "can't have exceptions", e.g. never edit generated code, never read `.env`. | same |
| Steering | "Watch the first minute, press Esc the moment Claude drifts, and after two failed corrections run /clear and write a better prompt." | same |
| Unattended runs | Before running unattended, make sure the run knows what done looks like, can check its own work, can touch only what it should, has a turn or spend cap, and ends with a human approval. | same |
| Parallel work | Git worktrees (`claude --worktree <name>`) for isolated parallel sessions. | [common-workflows](https://code.claude.com/docs/en/common-workflows) |

### 2.3 Context engineering
- **Definition:** context engineering is "the set of strategies for curating and maintaining the optimal set of tokens." The goal is "the smallest set of high-signal tokens that maximize the likelihood of your desired outcome" ([Anthropic](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents)).
- **Techniques:**
  - Context rot and the attention budget: recall gets worse as context grows.
  - Just-in-time retrieval: keep references such as file paths and load the content only when needed.
  - Compaction: summarise the conversation near the limit and continue in a new window.
  - Structured note-taking: write notes outside the context window and read them back later.
  - Sub-agents: return condensed results to a lead agent.

  All from the same source.
- **Osmani:** "small, focused context beats one giant prompt." Feed one task plus the relevant spec section to avoid the "curse of instructions," where too many directives lead the model to follow few of them well ([Osmani](https://addyosmani.com/blog/good-spec/)).
- **Willison:** "Most of the craft of getting good results out of an LLM comes down to managing its context" ([Willison](https://simonwillison.net/2025/Mar/11/using-llms-for-code/)).
- **Anthropic now says the context window is rarely the binding constraint** on current models (1M tokens, auto-compaction). It still recommends `/clear` between tasks for focus and cost ([best-practices](https://code.claude.com/docs/en/best-practices)).

*Synthesis for documents:* keep each planning doc focused on one concern and cross-link with stable IDs, so an agent can load only the relevant pieces just in time. This also keeps CLAUDE.md small, because it can point to `docs/` instead of containing it. Imports cost context, so prefer references the agent reads on demand.

### 2.4 Sizing and decomposing tasks
- **METR:** the "50% time horizon" doubled roughly every 7 months (March 2025 data). Models were near 100% on tasks under about 4 minutes and under 10% on tasks over about 4 hours. METR now marks some of these figures as outdated ([METR](https://metr.org/blog/2025-03-19-measuring-ai-ability-to-complete-long-tasks/)). Treat these as trend evidence, not a current sizing rule.
- **Anthropic long-running harness** ([Anthropic](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)):
  - An initializer agent writes `init.sh`, `claude-progress.txt` and a JSON feature list. The example had over 200 features, all starting as failing.
  - Each later session takes one feature, commits, and updates the progress file.
  - JSON was chosen because the model is "less likely to inappropriately overwrite" it than Markdown.
  - Each session starts by reading the git log and progress file, then runs a basic end-to-end test before new work.
- **Harper Reed:** blueprint steps should be "small, test-driven chunks" that build on each other ([Harper Reed](https://harper.blog/2025/02/16/my-llm-codegen-workflow-atm/)).
- **Osmani:** small iterative chunks, with a commit after each task as a save point. The commit detail comes from a secondary summary (**UNVERIFIED** in the original) ([Osmani workflow](https://addyosmani.com/blog/ai-coding-workflow/)).
- **Classic heuristics still apply:** INVEST for stories (Independent, Negotiable, Valuable, Estimable, Small, Testable) and SMART for tasks ([Bill Wake, 2003](https://xp123.com/invest-in-good-stories-and-smart-tasks/)).
- **Spec Kit and Kiro** both mark parallelisable tasks (`[P]`, dependency "waves") ([Spec Kit](https://github.com/github/spec-kit/blob/main/spec-driven.md), [Kiro](https://kiro.dev/docs/specs/)).

*Synthesis, as options:* a task an agent can finish in one session with a pass/fail check. Option A is a per-feature `tasks.md` (Spec Kit/Kiro style). Option B is one project-level JSON feature list with `passes` flags (Anthropic harness style). Option C is a Taskmaster-managed task graph.

### 2.5 Acceptance-criteria formats

**EARS (Easy Approach to Requirements Syntax)** was developed at Rolls-Royce and first published in 2009. Its generic form is `[While <precondition>,] [When <trigger>,] the <system> shall <response>` ([Mavin](https://alistairmavin.com/ears/)).

| EARS pattern | Keyword | Template |
|---|---|---|
| Ubiquitous | none | The `<system>` shall `<response>` |
| State-driven | While | While `<state>`, the `<system>` shall `<response>` |
| Event-driven | When | When `<trigger>`, the `<system>` shall `<response>` |
| Optional feature | Where | Where `<feature is present>`, the `<system>` shall `<response>` |
| Unwanted behaviour | If/Then | If `<unwanted trigger>`, then the `<system>` shall `<response>` |
| Complex | combined | While …, when …, the `<system>` shall … |

**Gherkin** uses the keywords Feature, Rule, Example/Scenario, Given/When/Then, And/But, Background, and Scenario Outline with Examples. "Then" outcomes "should be observable from outside the system" ([Cucumber reference](https://cucumber.io/docs/gherkin/reference/)). Gherkin can be executed with Cucumber-family tools. EARS is plain text and needs separate tests. One commentator notes EARS criteria "do not make the acceptance criteria executable on their own" ([CodeMySpec](https://codemyspec.com/blog/kiro-specs-explained)).

*Options:* (a) EARS for requirements and Gherkin for scenarios/tests, (b) Gherkin only, (c) EARS only plus hand-written tests. All three are sourced patterns. The trade-off is precision of requirement statements versus direct executability.

Example (illustrative, not a requirement):
- EARS: `When a member's membership expires, the system shall deny check-in and show the renewal option.`
- Gherkin:
  ```gherkin
  Scenario: Expired member is denied check-in
    Given a member whose membership expired yesterday
    When the member scans their card at the entrance
    Then check-in is denied
    And the renewal option is shown
  ```

### 2.6 Test-driven development with agents
- **Kent Beck (2025-06-25):**
  - His system prompt asks the agent to always follow `plan.md`, write the next unmarked test, add only the code needed to pass it, refactor only when green, and never mix structural and behavioural changes in one commit.
  - His warning signs are loops, "Functionality I hadn't asked for," and "disabling or deleting tests" ([Beck](https://newsletter.kentbeck.com/p/augmented-coding-beyond-the-vibes)).
- **Spec Kit** makes Test-First "NON-NEGOTIABLE". Tests must be written, approved, and confirmed to fail before implementation, ordered contract → integration → e2e → unit ([spec-driven.md](https://github.com/github/spec-kit/blob/main/spec-driven.md)).
- **Anthropic's harness** found agents marked features done after unit tests or `curl` checks while end-to-end flows failed. Browser automation testing "as a human user would" improved results sharply ([Anthropic](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)). Its coding agents were also told not to remove or edit tests.
- **Osmani** suggests conformance tests derived from the spec, often YAML-based and language-independent ([Osmani](https://addyosmani.com/blog/good-spec/)).
- **Mitigation for test tampering (options):** a hook or permission rule that protects test files or acceptance fixtures, plus independent review ([best-practices](https://code.claude.com/docs/en/best-practices)).

### 2.7 Architecture Decision Records
- **Nygard (2011):** Title, Context, Decision ("We will…"), Status (proposed, accepted, deprecated, superseded), Consequences. Records are numbered, never reused, kept in the repo, and superseded rather than edited. Keep them small, because large docs go stale and go unread ([Nygard](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)).
- **MADR 4.0.0 (2024-09-17):**
  - Front matter: status, date, decision-makers, consulted, informed.
  - Title.
  - Context and Problem Statement.
  - Decision Drivers.
  - Considered Options.
  - Decision Outcome, with Consequences and Confirmation.
  - Pros and Cons of the Options.
  - More Information.

  MADR offers full, minimal and bare variants ([MADR](https://adr.github.io/madr/)).
- **Fit with the owner's working style:** MADR's "Considered Options" and "Pros and Cons" sections match the owner's options-first style. ADRs are also exactly the "decisions that aren't in the repo" Anthropic says agents need ([best-practices](https://code.claude.com/docs/en/best-practices)).

### 2.8 Keeping docs as the source of truth, in sync
- **Where the source of truth lives is contested.** Spec Kit makes specs primary ([spec-driven.md](https://github.com/github/spec-kit/blob/main/spec-driven.md)). Thoughtworks' Liu keeps code as the source of truth ([Thoughtworks](https://www.thoughtworks.com/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices)). Böckeler found the tools only reach spec-first in practice ([Böckeler](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html)).
- **Sourced mechanisms:**
  - Living spec in version control, with the agent re-synced when it changes ([Osmani](https://addyosmani.com/blog/good-spec/)).
  - Spec Kit's `analyze` consistency check ([spec-driven.md](https://github.com/github/spec-kit/blob/main/spec-driven.md)).
  - A subagent review of the diff against SPEC.md ([best-practices](https://code.claude.com/docs/en/best-practices)).
  - `/doctor prompt-audit`, which flags instruction files that point at missing files or contradict each other ([best-practices](https://code.claude.com/docs/en/best-practices)).
  - ADR supersession rather than editing ([Nygard](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)).
  - A progress file plus git log as session memory ([Anthropic harness](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)).
  - Deterministic CI to catch drift ([Thoughtworks](https://www.thoughtworks.com/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices)).
- *Synthesis, as options:*
  - (A) Definition of Done includes "docs updated", checked by a review subagent.
  - (B) Stable requirement IDs (e.g. `REQ-MEM-012`) referenced in test names, so a script can report requirements without tests.
  - (C) Generate some docs from code (e.g. an OpenAPI spec as the contract, schema dumps), so they cannot drift.
  - (D) A Stop hook or CI job that fails when code under a path changes but its linked doc does not. A naive version will be noisy.

### 2.9 Failure modes and mitigations

| Failure mode | Evidence | Sourced mitigations |
|---|---|---|
| Doing too much at once / one-shotting | Ran out of context mid-feature, leaving work half-done and undocumented ([Anthropic harness](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)) | One feature per session, progress file, commits (same) |
| Premature "done" | Declared victory early; marked features done without end-to-end testing (same) | Checks with evidence, `/goal`, Stop hook ([best-practices](https://code.claude.com/docs/en/best-practices)); e2e testing via browser automation ([harness](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)) |
| Over-engineering / scope creep | "Functionality I hadn't asked for" ([Beck](https://newsletter.kentbeck.com/p/augmented-coding-beyond-the-vibes)); reviewers that "always find *something*" ([best-practices](https://code.claude.com/docs/en/best-practices)) | Simplicity and Anti-Abstraction gates, "No future-proofing?", trace to user story ([Spec Kit](https://github.com/github/spec-kit/blob/main/spec-driven.md)); "skip style" in reviews; out-of-scope lists in specs |
| Under-scoping | Claude "tends to read a request conservatively and build the smallest thing" ([best-practices](https://code.claude.com/docs/en/best-practices)) | State scope explicitly ("be ambitious: empty and error states…") (same) |
| Ignoring the spec / duplicating code | Spec Kit agent recreated existing classes; over-applied constitution rules ([Böckeler](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html)) | Small, focused context; review against spec; human gates |
| Spec drift | "hard to avoid" ([Thoughtworks](https://www.thoughtworks.com/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices)) | See §2.8 |
| Hallucinated APIs and packages | ≥5.2% (commercial) / 21.7% (open-source) of suggested packages did not exist; 205,474 unique fake names ([Spracklen et al.](https://arxiv.org/abs/2406.10279)); training cut-off issues ([Willison](https://simonwillison.net/2025/Mar/11/using-llms-for-code/)) | Pin approved dependencies in docs; prefer stable, popular libraries (Willison); type checks and tests; code-intelligence plugins ([best-practices](https://code.claude.com/docs/en/best-practices)); dependency allowlist via hook *(option)* |
| Test tampering | "disabling or deleting tests" ([Beck](https://newsletter.kentbeck.com/p/augmented-coding-beyond-the-vibes)) | Instruct not to edit tests ([harness](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)); enforce with hooks/permissions ([best-practices](https://code.claude.com/docs/en/best-practices)) |
| Wrong defaults (stack) | Claude defaulting to React ([Harper Reed](https://harper.blog/2025/02/16/my-llm-codegen-workflow-atm/)) | Decide the stack in ADRs before coding; scaffold first |
| Process overhead | "sledgehammer to crack a nut"; review burden of generated markdown ([Böckeler](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html)) | Scale ceremony to the size of the work ([BMAD](https://docs.bmad-method.org/), Kiro Quick Spec) |
| Instruction overload | "curse of instructions" ([Osmani](https://addyosmani.com/blog/good-spec/)); long CLAUDE.md reduces adherence ([memory](https://code.claude.com/docs/en/memory)) | Under 200 lines; path-scoped rules; skills |
| Secrets and destructive actions | "Never commit secrets" was the most common useful constraint in the agent files Osmani cites ([Osmani](https://addyosmani.com/blog/good-spec/)) | Always / Ask first / Never boundaries (same); deny rules and sandbox ([best-practices](https://code.claude.com/docs/en/best-practices)) |

### 2.10 Practitioner notes (brief)
- **Simon Willison (2025-03-11):**
  - Treat the LLM as an "over-confident pair programmer" or "digital intern."
  - Account for training cut-offs.
  - Use the LLM to research options, then give it exact instructions.
  - Always test the code yourself.
  - Vibe coding is fine for exploration, not production ([Willison](https://simonwillison.net/2025/Mar/11/using-llms-for-code/)).
  - He also tags posts "agentic-engineering". The phrase "vibe engineering" did not appear in my search (**UNVERIFIED**).
- **Addy Osmani (Jan 2026):**
  - Start with a high-level vision and have the AI expand it.
  - Structure the spec like a PRD.
  - Cover six areas: commands, testing, project structure, code style, git workflow, boundaries.
  - Use three-tier boundaries (Always / Ask first / Never).
  - Add self-checks and conformance tests, and keep the spec as a living document ([Osmani](https://addyosmani.com/blog/good-spec/)).
  - The "analysis of over 2,500 agent files" figure comes from secondary summaries (**UNVERIFIED**).
- **Anthropic, "Building effective agents" (2024-12-19):** "finding the simplest solution possible, and only increasing complexity when needed" ([Anthropic](https://www.anthropic.com/research/building-effective-agents)).

---

## 3. Catalog of candidate planning documents

Each entry gives the document's purpose, the questions it forces, a skeleton template and the source that justifies it. **All are options.** The "Sizing" note suggests when a document might be worth having (*Synthesis*). Anthropic's rule of writing down only what the agent cannot infer applies to agent-facing files ([best-practices](https://code.claude.com/docs/en/best-practices)). Human-facing planning docs can be more complete.

Suggested layout (one option):

```
docs/
  vision.md
  prd.md
  personas.md
  glossary.md
  requirements/<area>.md     # stories + EARS/Gherkin criteria
  nfr.md
  domain/data-model.md
  architecture.md
  api/openapi.yaml (or contracts/)
  security.md
  privacy-compliance.md
  ui-ux.md
  testing.md
  decisions/NNNN-title.md    # ADRs (MADR)
  roadmap.md
  risks.md
  research/                  # this note and others
specs/<feature>/{spec,plan,tasks}.md   # per-feature, if SDD option chosen
CLAUDE.md (or AGENTS.md)
.claude/{rules,skills,agents}/
```

---

### 3.1 `vision.md` (product vision / brief)
- **Purpose:** a one-page "why" that anchors every later trade-off.
- **Source:** Osmani's "high-level vision first" ([Osmani](https://addyosmani.com/blog/good-spec/)); BMAD's Clarify stage ([BMAD](https://docs.bmad-method.org/)).
- **Forces:** Who is this for? What problem? What does success look like? What will we explicitly *not* do?
```markdown
# Vision
## Problem statement
## Target users (link personas.md)
## Value proposition
## Goals / success metrics (measurable)
## Non-goals / out of scope
## Constraints (budget, time, regulation, team = 1 person + agents)
## Open questions
```

### 3.2 `prd.md` (product requirements)
- **Purpose:** the functional scope in *what/why* terms, with no implementation detail.
- **Source:** Spec Kit's `spec.md` ([spec-driven.md](https://github.com/github/spec-kit/blob/main/spec-driven.md)); Taskmaster's PRD as the input to task generation ([Taskmaster](https://github.com/eyaltoledano/claude-task-master)); Osmani's "structure like a PRD."
- **Forces:** Which capabilities are in v1? How does each trace to a user need? What is ambiguous? Mark ambiguity `[NEEDS CLARIFICATION]` rather than guessing.
```markdown
# PRD: <product>
## Background & context
## Goals / non-goals
## Personas in scope
## Feature areas
### F-<n> <name>
- Problem / user need
- User stories (link requirements/)
- Acceptance criteria (EARS or Gherkin, IDs)
- Out of scope
- [NEEDS CLARIFICATION: …]
## Success metrics
## Dependencies & assumptions (each assumption flagged)
## Review checklist (no open clarifications; every feature traces to a story)
```

### 3.3 `personas.md`
- **Purpose:** who uses the system and what they need. For this project the candidates are members, front-desk staff, trainers, managers/owners and payroll/HR (illustrative, not decided).
- **Source:** Spec Kit specs are built from user stories, which need actors ([spec-driven.md](https://github.com/github/spec-kit/blob/main/spec-driven.md)); Kiro's requirements.md uses user stories ([Kiro](https://kiro.dev/docs/specs/)).
- **Forces:** What roles exist? What permissions does each role imply? What are their goals and pains? What devices and contexts do they use?
```markdown
# Personas
## <Persona name / role>
- Description & context
- Goals
- Frustrations / pains
- Key tasks (link stories)
- Access level / permissions implied
- Devices / environment
- Accessibility needs
```

### 3.4 `requirements/<area>.md` or `user-stories.md`
- **Purpose:** stories with testable acceptance criteria and stable IDs.
- **Source:** Kiro requirements.md ([Kiro](https://kiro.dev/docs/specs/)); EARS ([Mavin](https://alistairmavin.com/ears/)); Gherkin ([Cucumber](https://cucumber.io/docs/gherkin/reference/)); INVEST ([Wake](https://xp123.com/invest-in-good-stories-and-smart-tasks/)).
- **Forces:** Is each story INVEST-compliant? What are the unwanted-behaviour cases (EARS If/Then)? What is observable from outside the system?
```markdown
# Requirements: <area, e.g. Membership>
## REQ-MEM-001 <title>
**Story:** As a <persona>, I want <capability>, so that <benefit>.
**Priority:** Must/Should/Could
**Acceptance criteria (EARS):**
- When <trigger>, the system shall <response>.
- If <unwanted condition>, then the system shall <response>.
**Scenarios (Gherkin, optional):** Given/When/Then
**Out of scope:**
**Open questions:**
**Traces to:** PRD F-n; tests: <test ids>
```

### 3.5 `glossary.md` (domain glossary / ubiquitous language)
- **Purpose:** one meaning per term, shared by humans, agents, code and database names.
- **Source:** arc42 §12 Glossary, "ubiquitous language" ([arc42](https://arc42.org/overview)); Thoughtworks recommends domain-oriented ubiquitous language in specs ([Thoughtworks](https://www.thoughtworks.com/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices)).
- **Forces:** What exactly is a "member" vs a "customer" vs a "guest"? A "membership" vs a "subscription" vs a "contract"? A "shift" vs a "booking"?
```markdown
# Glossary
| Term | Definition | Synonyms to avoid | Code name | Example | Source/owner |
```

### 3.6 `nfr.md` (non-functional requirements / quality attributes)
- **Purpose:** measurable quality targets.
- **Source:** arc42 §1 quality goals and §10 quality scenarios ([arc42](https://arc42.org/overview)); ISO/IEC 25010 quality model. The page I fetched listed only some of its characteristics, so the full list is **UNVERIFIED** here ([iso25000](https://iso25000.com/en/iso-25000-standards/iso-25010)).
- **Forces:** How many concurrent users? Uptime during opening hours? Response times? Offline tolerance at the front desk? Backup and restore objectives? Accessibility level? Supported languages (e.g. Danish/English)?
```markdown
# Non-functional requirements
| ID | Quality attribute | Scenario (source, stimulus, environment, response, measure) | Target | Verification method |
## Performance
## Availability & backup/restore (RPO/RTO)
## Security (link security.md)
## Privacy (link privacy-compliance.md)
## Usability & accessibility
## Maintainability & testability
## Portability / deployment
## Localization
```

### 3.7 `domain/data-model.md`
- **Purpose:** entities, relationships, invariants and data lifecycle.
- **Source:** Spec Kit `data-model.md` ([spec-driven.md](https://github.com/github/spec-kit/blob/main/spec-driven.md)); Thoughtworks recommends specifying invariants ([Thoughtworks](https://www.thoughtworks.com/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices)).
- **Forces:** What are the aggregates? What is the source of truth for each field? What are the retention and deletion rules? Which fields are personal data or special-category data?
```markdown
# Data model
## Entity overview (diagram, e.g. Mermaid ER)
## Entities
### <Entity>
| Field | Type | Required | Constraints | Personal data? (category) | Retention |
- Invariants
- Lifecycle / states
- Relationships
## Cross-entity invariants
## Migration policy
```

### 3.8 `architecture.md` / `design.md`
- **Purpose:** how the system is built, and why at a high level.
- **Source:** Spec Kit `plan.md` ([spec-driven.md](https://github.com/github/spec-kit/blob/main/spec-driven.md)); Kiro `design.md` ([Kiro](https://kiro.dev/docs/specs/)); arc42's 12 sections ([arc42](https://arc42.org/overview)).
- **Note:** this is human-facing. Anthropic advises *against* repeating architecture or layout in CLAUDE.md, because Claude reads the repo ([best-practices](https://code.claude.com/docs/en/best-practices)).
- **Forces:** What runs where? Monolith or services? Which external systems (payments, access control hardware, accounting, email/SMS)? How does it deploy?
```markdown
# Architecture (arc42-lite option)
1. Goals & quality priorities (link nfr.md)
2. Constraints
3. Context & scope (external systems diagram)
4. Solution strategy (link ADRs)
5. Building blocks / modules
6. Runtime scenarios (key sequences)
7. Deployment view
8. Crosscutting concepts (auth, logging, errors, i18n)
9. Decisions → docs/decisions/
10. Quality scenarios → nfr.md
11. Risks & tech debt → risks.md
12. Glossary → glossary.md
```

### 3.9 `api/openapi.yaml` or `contracts/`
- **Purpose:** machine-checkable interface contracts. Contract tests are written before implementation.
- **Source:** Spec Kit `contracts/` and its ordering of contract tests first ([spec-driven.md](https://github.com/github/spec-kit/blob/main/spec-driven.md)); Osmani's conformance tests ([Osmani](https://addyosmani.com/blog/good-spec/)); the Claude docs' rules example requiring "an entry in openapi.yaml" ([best-practices](https://code.claude.com/docs/en/best-practices)).
- **Forces:** What are the resource names, error formats, auth scheme, versioning and pagination?
```yaml
# Skeleton (OpenAPI-style; format choice is open)
info: {title, version}
security: …
paths:
  /members: {get, post}
components:
  schemas: …
  responses: {Error: …}
```

### 3.10 `security.md`
- **Purpose:** threat model, security requirements and verification level.
- **Source:** OWASP ASVS 5.0.0 (30 May 2025) as a requirements framework and procurement baseline. ASVS levels were not described on the page I fetched (**UNVERIFIED** here) ([ASVS](https://asvs.owasp.org/)). Osmani's "Never commit secrets" boundary ([Osmani](https://addyosmani.com/blog/good-spec/)). Deny rules and sandbox ([best-practices](https://code.claude.com/docs/en/best-practices)).
- **Forces:** Who can see whose data? How is staff authentication done (MFA)? Where do secrets live? What gets audit-logged? Which ASVS level is the target?
```markdown
# Security
## Assets & data classification
## Threat model (actors, entry points, STRIDE-style list)
## AuthN / AuthZ model (roles → permissions matrix)
## Security requirements (ASVS refs, e.g. v5.0.0-x.y.z)
## Secrets management
## Audit logging
## Dependency policy (approved packages, hallucinated-package defence)
## Agent boundaries (Always / Ask first / Never; hooks & deny rules)
## Incident response
```

### 3.11 `privacy-compliance.md`
- **Purpose:** the legal basis and data-protection design for member and employee personal data.
- **Source:** GDPR Art. 9 lists "data concerning health" and "biometric data for the purpose of uniquely identifying a natural person" as special categories, whose processing is prohibited unless an Art. 9(2) exception applies ([GDPR Art. 9](https://gdpr-info.eu/art-9-gdpr/)). Which further GDPR articles and national rules (e.g. employee data, bookkeeping retention) apply is **not researched here**. Flag it for a dedicated compliance research note.
- **Forces:** Is any health, injury or biometric (e.g. fingerprint entry) data processed? What is the lawful basis per purpose? What are the retention periods? How are data-subject requests handled? Who are the processors (payment, hosting)?
```markdown
# Privacy & compliance
## Jurisdiction(s) & applicable law (to be researched)
## Data inventory (link data-model.md) – category, purpose, lawful basis, retention
## Special-category data (Art. 9) – present? exception relied on?
## Data-subject rights handling (access, erasure, portability)
## Processors / sub-processors
## Employee-data specifics
## DPIA needed? (open)
## Logging & audit
```

### 3.12 `ui-ux.md`
- **Purpose:** screens, flows and design rules the agent can follow and verify against.
- **Source:** the Claude docs suggest checking a "browser screenshot next to the mockup," pasting design images, and being explicit about empty and error states ([best-practices](https://code.claude.com/docs/en/best-practices)); Anthropic's harness found browser-based e2e testing improved results ([harness](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)).
- **Forces:** Which surfaces exist (member app or web, front-desk kiosk, admin back-office)? What are the key flows? Accessibility? Design system?
```markdown
# UI/UX
## Surfaces & platforms
## Navigation map
## Key flows (per persona; link stories)
## Screen inventory (wireframe/mock links; states: empty, loading, error, success)
## Design system / component library choice (ADR link)
## Accessibility target
## Copy & localization rules
```

### 3.13 `testing.md` (test strategy)
- **Purpose:** what "verified" means, and the exact commands.
- **Source:** "Give Claude a check to run" ([best-practices](https://code.claude.com/docs/en/best-practices)); Spec Kit's Test-First and Integration-First articles ([spec-driven.md](https://github.com/github/spec-kit/blob/main/spec-driven.md)); Beck's TDD loop ([Beck](https://newsletter.kentbeck.com/p/augmented-coding-beyond-the-vibes)); e2e lessons ([harness](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)).
- **Forces:** What is the test pyramid? Real database or mocks? How do acceptance criteria map to tests? Which tests may an agent never edit? What runs in CI and what runs as a Stop hook?
```markdown
# Testing strategy
## Commands (all, single file, typecheck, lint, e2e)
## Levels: unit / integration / contract / e2e – what each covers
## Mapping: REQ-ID → test naming convention
## TDD workflow for agents (red → green → refactor; structural vs behavioural commits)
## Protected tests/fixtures (and how enforced)
## Test data & fixtures (no real personal data)
## CI gates & Definition of Done
```

### 3.14 `decisions/NNNN-<title>.md` (ADRs)
- **Purpose:** an immutable record of significant decisions and the options considered.
- **Source:** Nygard ([Cognitect](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)); MADR 4.0.0 ([MADR](https://adr.github.io/madr/)).
- **Forces:** What options were there? Which drivers decided it? What are the consequences? How will compliance be confirmed?
```markdown
---
status: proposed | accepted | deprecated | superseded by ADR-NNNN
date: YYYY-MM-DD
decision-makers:
consulted:
informed:
---
# <short title of problem and solution>
## Context and Problem Statement
## Decision Drivers
## Considered Options
## Decision Outcome
Chosen option: "<option>", because …
### Consequences
- Good, because …
- Bad, because …
### Confirmation
## Pros and Cons of the Options
### <Option>
- Good, because …
- Bad, because …
## More Information
```

### 3.15 `roadmap.md` / milestones
- **Purpose:** ordering and slicing of features into shippable increments.
- **Source:** Harper Reed's incremental blueprint ([Harper Reed](https://harper.blog/2025/02/16/my-llm-codegen-workflow-atm/)); Kiro and Spec Kit task dependency ordering ([Kiro](https://kiro.dev/docs/specs/), [Spec Kit](https://github.com/github/spec-kit/blob/main/spec-driven.md)).
- **Forces:** What is the minimal useful first release? What depends on what? What is deferred?
```markdown
# Roadmap
## Milestone M<n>: <name>
- Goal / user-visible outcome
- Included requirements (IDs)
- Exit criteria (tests/demos)
- Dependencies
- Explicitly deferred
```

### 3.16 `risks.md`
- **Purpose:** known risks, assumptions and technical debt.
- **Source:** arc42 §11 "Risks & Technical Debt" ([arc42](https://arc42.org/overview)); the failure-mode evidence in §2.9.
- **Forces:** What could sink the project? Which assumptions are unvalidated? Which agent-specific risks apply (drift, dependency hallucination)?
```markdown
# Risks
| ID | Risk / assumption | Likelihood | Impact | Mitigation | Owner | Status |
## Agentic-development risks (drift, test tampering, hallucinated deps, scope creep)
## Technical debt log
```

### 3.17 `CLAUDE.md` / `AGENTS.md`
- **Purpose:** a minimal always-loaded agent briefing.
- **Source:** [best-practices](https://code.claude.com/docs/en/best-practices), [memory](https://code.claude.com/docs/en/memory), [agents.md](https://agents.md/), Osmani's six areas ([Osmani](https://addyosmani.com/blog/good-spec/)).
- **Forces:** What can't Claude infer? What has been corrected twice? Which rules must instead be hooks?
- **Option:** keep a single AGENTS.md and have CLAUDE.md import it, for tool portability ([memory](https://code.claude.com/docs/en/memory)).
```markdown
# Commands
- Install / run / test one file / typecheck / lint / e2e
# Conventions (only non-obvious ones)
# Gotchas
# Where planning docs live (paths only; read on demand)
- Requirements: docs/requirements/ ; ADRs: docs/decisions/ ; Glossary: docs/glossary.md
# Boundaries
- Always: …  - Ask first: … (new dependency, schema change)  - Never: … (enforced via hooks where it matters)
```
Companion options: `.claude/rules/*.md` with `paths:` for area-specific rules, `.claude/skills/` for procedures such as "write ADR" or "new feature spec", and `.claude/agents/` for reviewers such as "spec-conformance reviewer" (read-only tools) ([best-practices](https://code.claude.com/docs/en/best-practices), [sub-agents](https://code.claude.com/docs/en/sub-agents)).

### 3.18 Task-breakdown files (three alternatives)
- **Option A, per-feature `specs/<feature>/tasks.md`:** Spec Kit/Kiro style, with `[P]` parallel markers and checkboxes ([spec-driven.md](https://github.com/github/spec-kit/blob/main/spec-driven.md), [Kiro](https://kiro.dev/docs/specs/)).
- **Option B, `features.json` + `progress.md`:** Anthropic harness style. Each entry has `passes: false`, and agents may change only that field ([harness](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)).
- **Option C, Taskmaster-managed tasks** generated from the PRD ([Taskmaster](https://github.com/eyaltoledano/claude-task-master)).
- **Option D, `todo.md` / `prompt_plan.md`:** Harper Reed style ([Harper Reed](https://harper.blog/2025/02/16/my-llm-codegen-workflow-atm/)).
```markdown
# Tasks: <feature>
- [ ] T001 Write failing contract test for POST /members (REQ-MEM-001)
- [ ] T002 [P] Write failing test for duplicate email rejection (REQ-MEM-002)
- [ ] T003 Implement … (depends: T001)
Each task: goal · files likely touched · check command · done-evidence
```
```json
[{"id":"REQ-MEM-001","description":"…","steps":["…"],"passes":false}]
```

### 3.19 Optional: `constitution.md` (project principles)
- **Purpose:** a few non-negotiable engineering principles that the plan is checked against.
- **Source:** Spec Kit ([spec-driven.md](https://github.com/github/spec-kit/blob/main/spec-driven.md)).
- **Trade-off:** it overlaps with CLAUDE.md, ADRs and testing.md. Böckeler observed agents *over-applying* constitution rules ([Böckeler](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html)).

---

## 4. Candidate planning workflows (options, not decisions)

| Option | Shape | Pros | Cons | Sources |
|---|---|---|---|---|
| **A. Full Spec Kit** | constitution → per-feature specify → plan → tasks → implement, using Spec Kit tooling | Templates with clarification markers and anti-over-engineering gates; many agents supported | Heavy; a branch and many markdown files per feature; review burden; command churn between versions | [Spec Kit](https://github.com/github/spec-kit), [Böckeler](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html) |
| **B. Kiro-style three files, tool-agnostic** | requirements (EARS) → design → tasks per feature, hand-run in Claude Code with plan mode | Lightweight; well-known format; EARS precision | No native tooling in Claude Code; EARS not executable | [Kiro](https://kiro.dev/docs/specs/), [EARS](https://alistairmavin.com/ears/) |
| **C. BMAD skills** | Scale-adaptive loop of Clarify → Plan → Build and verify | Explicit scaling by work size; installs into Claude Code | v7 in alpha with breaking changes; another framework to learn | [BMAD](https://docs.bmad-method.org/) |
| **D. PRD → Taskmaster** | One project PRD, parsed into a task graph with complexity analysis | Automated decomposition and `next` task | Depends on PRD quality; extra MCP tool | [Taskmaster](https://github.com/eyaltoledano/claude-task-master) |
| **E. Lightweight native Claude Code** | Interview → SPEC.md → fresh session → plan mode → TDD → subagent review vs spec → commit; feature list + progress file for multi-session work | Minimal tooling; matches the vendor's own guidance; easy to scale per task | You must design the doc conventions yourself; less guard-rail templating | [best-practices](https://code.claude.com/docs/en/best-practices), [harness](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents), [Harper Reed](https://harper.blog/2025/02/16/my-llm-codegen-workflow-atm/) |
| **F. Hybrid** | Project-level docs from §3 (vision, PRD, glossary, data model, ADRs, NFR, security/privacy) up front; per-feature lightweight spec/tasks; ceremony scaled by size | Front-loads cross-cutting decisions (stack, privacy) that agents can't infer; per-feature cost stays low | Needs discipline to keep the two levels in sync | *Synthesis* of the above |

Phases the sources tend to agree on, whichever option is chosen:
1. **Clarify:** use LLM interview-style questioning ([Harper Reed](https://harper.blog/2025/02/16/my-llm-codegen-workflow-atm/), [best-practices](https://code.claude.com/docs/en/best-practices)). Research options and record ADRs ([Willison](https://simonwillison.net/2025/Mar/11/using-llms-for-code/), [MADR](https://adr.github.io/madr/)).
2. **Specify:** write *what/why* with testable criteria and no implementation detail ([Spec Kit](https://github.com/github/spec-kit/blob/main/spec-driven.md)).
3. **Design and plan:** use plan mode, check against principles and NFRs ([common-workflows](https://code.claude.com/docs/en/common-workflows)).
4. **Decompose:** break work into small tasks, each with a check command ([METR](https://metr.org/blog/2025-03-19-measuring-ai-ability-to-complete-long-tasks/), [harness](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)).
5. **Implement:** test first, one task at a time, committing after each ([Beck](https://newsletter.kentbeck.com/p/augmented-coding-beyond-the-vibes)).
6. **Verify:** run checks and collect evidence, review against the spec with a fresh subagent, and add e2e browser tests for UI ([best-practices](https://code.claude.com/docs/en/best-practices)).
7. **Sync:** update docs and ADRs and the progress file, and prune CLAUDE.md ([best-practices](https://code.claude.com/docs/en/best-practices), [Osmani](https://addyosmani.com/blog/good-spec/)).

---

## 5. Sources

Anthropic / Claude Code
- https://code.claude.com/docs/en/best-practices (redirect target of https://www.anthropic.com/engineering/claude-code-best-practices)
- https://code.claude.com/docs/en/common-workflows
- https://code.claude.com/docs/en/memory
- https://code.claude.com/docs/en/sub-agents
- https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents
- https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents (2025-11-26)
- https://www.anthropic.com/research/building-effective-agents (2024-12-19)

SDD tools and methods
- https://github.com/github/spec-kit
- https://github.com/github/spec-kit/blob/main/spec-driven.md
- https://kiro.dev/docs/specs/
- https://github.com/bmad-code-org/BMAD-METHOD
- https://docs.bmad-method.org/
- https://github.com/eyaltoledano/claude-task-master
- https://agents.md/

Practitioners and analysis
- https://harper.blog/2025/02/16/my-llm-codegen-workflow-atm/
- https://simonwillison.net/2025/Mar/11/using-llms-for-code/
- https://addyosmani.com/blog/good-spec/
- https://addyosmani.com/blog/ai-coding-workflow/
- https://newsletter.kentbeck.com/p/augmented-coding-beyond-the-vibes (2025-06-25)
- https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html
- https://www.thoughtworks.com/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices (2025-12-04)
- https://www.thoughtworks.com/radar/techniques/spec-driven-development
- https://metr.org/blog/2025-03-19-measuring-ai-ability-to-complete-long-tasks/
- https://arxiv.org/abs/2406.10279 and https://www.usenix.org/conference/usenixsecurity25/presentation/spracklen

Formats and standards
- https://alistairmavin.com/ears/
- https://cucumber.io/docs/gherkin/reference/
- https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions
- https://adr.github.io/madr/
- https://arc42.org/overview
- https://xp123.com/invest-in-good-stories-and-smart-tasks/
- https://asvs.owasp.org/
- https://iso25000.com/en/iso-25000-standards/iso-25010
- https://gdpr-info.eu/art-9-gdpr/

Secondary (used only for items marked UNVERIFIED)
- https://codemyspec.com/blog/kiro-specs-explained
- https://www.promptz.dev/rules/general/kiro-specs/

---

## 6. Open questions for the project owner

1. **Ceremony level:** which workflow shape do you want to evaluate: full Spec Kit (A), Kiro-style files (B), BMAD (C), PRD → Taskmaster (D), lightweight native Claude Code (E), or a hybrid (F)?
2. **Source of truth:** should specs be *spec-first* (written, then code becomes the truth) or *spec-anchored* (specs maintained alongside code for the system's lifetime)?
3. **Acceptance-criteria format:** EARS, Gherkin, both (EARS requirements + Gherkin scenarios), or plain checklists? Do you want executable BDD tests?
4. **Task-tracking format:** per-feature `tasks.md`, a single JSON feature list with `passes` flags, Taskmaster, or GitHub Issues?
5. **Agent instruction file:** CLAUDE.md only, or AGENTS.md (portable to other agents) imported by CLAUDE.md? Will other agents besides Claude Code ever work on this repo?
6. **Which catalog documents are in scope for the planning phase,** and which can wait until they are needed?
7. **ADR template:** MADR full, MADR minimal, or Nygard's original? Should research notes like this one feed directly into ADR "Considered Options"?
8. **Enforcement:** which rules must be deterministic (hooks or deny rules: e.g. protected tests, no `.env` reads, dependency allowlist, typecheck Stop hook) versus guidance in CLAUDE.md?
9. **Sensitive data:** will the system store health, injury, medical-clearance or biometric (e.g. fingerprint or face entry) data about members, or sensitive employee data? This decides how much privacy/compliance work comes before design ([GDPR Art. 9](https://gdpr-info.eu/art-9-gdpr/)).
10. **Jurisdiction:** which country or countries will the fitness center operate in? This decides which national rules beyond GDPR need researching.
11. **Security baseline:** do you want to adopt OWASP ASVS 5.0 as the security requirements reference, and if so at what level?
12. **Doc-sync mechanism:** review subagent, requirement IDs in test names, generated docs (e.g. OpenAPI), CI drift checks, or a combination?
13. **Unattended runs:** do you plan to let agents run unattended or in parallel (worktrees, scheduled runs)? If so, what turn or spend caps and human approval gates do you want?
14. **Definition of Done:** what evidence must an agent produce before a task counts as done (test output, e2e screenshot, docs updated, review passed)?
