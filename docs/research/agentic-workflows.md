# Agentic Development Workflows for a Solo Owner + AI Coding Agents

**Scope:** execution and orchestration only: how work flows from a task to merged code when one person directs one or more AI coding agents. Planning-document formats (PRDs, specs, ADRs) are covered in a sibling research doc.
**Researched:** 2026-11-12. Claude Code changes quickly (the docs cite version numbers such as v2.1.3xx), so re-check feature details against the live docs before relying on them.
**Rule followed:** every claim is cited. Anything that could not be verified against a primary source is marked **UNVERIFIED**. This doc presents options and trade-offs and makes no project decisions.

### Evidence labels used below

| Label | Meaning |
|---|---|
| **[RCT]** | Randomized or controlled study |
| **[SURVEY/TELEMETRY]** | Large-sample survey or observational data (correlation, not causation) |
| **[VENDOR]** | Measured or reported by a company that sells the thing being measured |
| **[DOCS]** | Official product documentation. It describes what a feature does, not whether it improves outcomes |
| **[ANECDOTE]** | A practitioner's own experience report |

---

## 1. Executive summary

1. **Verification is the strongest lever.** Anthropic's guidance and its long-running-agent experiments agree: an agent with a pass/fail check it can run (tests, typecheck, lint, a browser end-to-end check) can close its own loop. Without one, the human becomes the test suite. [DOCS] [ANECDOTE: Anthropic internal] ([best practices][cc-bp], [harnesses][harness])
2. **Split instructions into "guidance" and "enforcement".** CLAUDE.md, skills, and prompts are followed "nearly every time" but not guaranteed. Hooks, deny rules, the sandbox, CI, and branch protection are enforced by the harness or platform. Rules that must never bend belong in the second group. [DOCS] ([best practices][cc-bp])
3. **For a solo owner the bottleneck is review, not generation.** Telemetry and practitioner reports both show that more agent output raises review load. Faros reports 98% more merged PRs but 91% longer PR review time on high-AI-adoption teams [VENDOR/TELEMETRY]. Simon Willison: "the natural bottleneck on all of this is how fast I can review the results" [ANECDOTE]. Parallelism only helps up to the owner's review capacity. ([Faros][faros], [Willison][willison-parallel])
4. **Productivity evidence is mixed and weak.** METR's 2025 RCT found experienced developers were 19% *slower* with early-2025 AI tools while believing they were 20% faster [RCT]. METR's late-2025 follow-up points toward speedup but METR calls it "only very weak evidence" because of selection effects [RCT, compromised]. DORA 2025 associates AI adoption with higher throughput *and* higher delivery instability [SURVEY]. ([METR 2025][metr-2025], [METR 2026][metr-2026], [DORA][dora])
5. **Agent-written code needs gates.** A vendor study of 470 PRs found AI-co-authored PRs had about 1.7x more issues [VENDOR]. Models have historically "special-cased" or hard-coded tests [DOCS: system card, via secondary source], and Kent Beck reports agents deleting or disabling tests [ANECDOTE]. Mitigations include protected test files, CI as the source of truth, and an independent reviewer. ([CodeRabbit][coderabbit], [Beck][beck])
6. **Long-running work needs external state.** Anthropic's harness pattern: an initializer session writes a feature list (JSON), a progress file, an `init.sh`, and a first git commit. Each later session reads those, picks one feature, verifies it end to end, commits, and updates the progress file. [DOCS/ANECDOTE: Anthropic engineering] ([harnesses][harness])
7. **Multi-agent setups cost a lot of tokens and fit coding less well than research.** In Anthropic's research system, multi-agent runs used about 15x the tokens of chat. Anthropic notes "most coding tasks involve fewer truly parallelizable tasks than research." Claude Code docs say agent teams use about 7x the tokens of a standard session when teammates run in plan mode. ([multi-agent][multiagent], [costs][cc-costs])
8. **Claude Code ships most of the building blocks.** Subagents, skills (custom slash commands are now merged into skills), hooks, MCP, plan mode, auto mode plus the sandbox, worktrees (`claude -w`), headless `claude -p` with `--max-turns` and `--max-budget-usd`, `/code-review`, `/verify`, `/batch`, agent teams (experimental), the GitHub Action, managed Code Review (Team/Enterprise plans), and OpenTelemetry export. [DOCS]

---

## 2. Findings

### 2.1 Orchestrator / subagent patterns

- **Canonical patterns.** Anthropic's "Building effective agents" (Dec 2024) lists prompt chaining, routing, parallelization (sectioning or voting), orchestrator-workers, and evaluator-optimizer. It advises to "find the simplest solution possible, and only increasing complexity when needed." On coding agents it says "Code solutions are verifiable through automated tests" and "human review remains crucial." [DOCS/ANECDOTE] ([building effective agents][bea])
- **Orchestrator-worker results, from research rather than coding.** Anthropic's multi-agent research system (Opus 4 lead, Sonnet 4 subagents) "outperformed single-agent Claude Opus 4 by 90.2%" on an internal eval. Agents use about 4x the tokens of chat and multi-agent systems about 15x. Token usage alone "explains 80% of the variance" on BrowseComp. The post explicitly flags coding as a weaker fit because of shared context and dependencies. Observed failures: spawning too many subagents for simple queries, and vague delegation that caused duplicated work. [VENDOR, internal eval] ([multi-agent][multiagent])
- **Claude Code subagents** [DOCS] ([sub-agents][cc-subagents]):
  - Markdown files with YAML frontmatter in `.claude/agents/` (project, can be committed) or `~/.claude/agents/` (user).
  - Frontmatter can restrict `tools`/`disallowedTools` and set `model`, `permissionMode`, `maxTurns`, `isolation: worktree`, `background`, and `memory`.
  - Each subagent runs in its own context window and returns a summary.
  - Subagents can nest up to three levels by default (`CLAUDE_CODE_MAX_SUBAGENT_SPAWN_DEPTH`). By default up to 20 run concurrently.
  - Subagent usage "count[s] toward the same usage limits as your main conversation."
  - Built-ins: Explore (read-only), Plan, and general-purpose.
- **What subagents are good for, per the docs.** The current best-practices page lists running independent work in parallel, keeping noisy output out of the main context, getting "a reviewer that hasn't seen how the code was written," and giving a task fewer tools (for example, a read-only reviewer). It also says "Handing the same task to a subagent doesn't make the answer any better. A fresh reviewer does help." [DOCS] ([best practices][cc-bp])
- **Agent teams (experimental).** Enabled with `CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1` [DOCS] ([agent teams][cc-teams]):
  - A lead session coordinates teammates through a shared task list with dependencies and file-locked claiming, plus a mailbox for messages between agents.
  - Recommended size: "Start with 3-5 teammates," with "5-6 tasks per teammate."
  - Known limitations: no session resumption for in-process teammates, task status that "can lag," and no nested teams.
  - Guidance: "Two teammates editing the same file leads to overwrites," and new users should start with research or review tasks rather than parallel implementation.
  - Quality-gate hooks: `TaskCreated`, `TaskCompleted`, and `TeammateIdle` (exit code 2 blocks the action).

### 2.2 Parallel agents with git worktrees

- **Built-in support.** `claude --worktree <name>` (or `-w`) starts a session in a new git worktree on its own branch. A worktree is a fresh checkout, so gitignored files such as `.env` and installed dependencies are missing:
  - `.worktreeinclude` copies small gitignored files into each new worktree.
  - `worktree.symlinkDirectories` handles large directories.
  - Unnamed, unchanged worktrees are removed on exit. `claude -p --worktree` runs must be cleaned up manually with `git worktree remove`.
  - `/batch` splits a large mechanical change into 5–30 units, each in its own worktree, and each opens a PR.

  [DOCS] ([best practices][cc-bp], [common workflows][cc-workflows])
- **Practitioner reports** [ANECDOTE]:
  - Willison (Oct 2025) runs Claude Code and Codex CLI in several terminals, often in YOLO mode. He had *not* adopted worktrees and uses fresh checkouts in `/tmp` instead. He lands "one significant change at a time" and runs low-cognitive-load work alongside it: research, explaining code, small maintenance, and "scouts" sent out to discover where the difficulties are. ([Willison][willison-parallel])
  - Ronacher (Jun 2025): individual agents aren't fast, but running several in parallel raises throughput. Shared state (database, Redis, ports) has to be segmented. His Makefile refuses to start a dev server if one is already running. ([Ronacher][ronacher])
- **Implication to weigh.** Worktrees isolate files, but not databases, ports, or other external state. A fitness-center system with a database would need per-worktree database and port isolation for parallel agents to run integration tests safely. This follows from Ronacher's point. Its cost for this project is not measured.

### 2.3 Task queues and issue-tracker-driven agents

- **GitHub Issues + the Claude Code GitHub Action (`anthropics/claude-code-action@v1`)** [DOCS] ([GH Actions][cc-gha]):
  - *Interactive mode:* `@claude` in an issue or PR comment triggers work. Claude can turn an issue into a PR.
  - *Automation mode:* a `prompt` input (plain text or `/skill`) runs on any event, including cron.
  - Safeguards: the triggering user must have write access, and bot actors are rejected unless allow-listed, which prevents loops.
  - Cost: GitHub Actions minutes plus API tokens, or subscription usage via `CLAUDE_CODE_OAUTH_TOKEN`. Recommended limits: `--max-turns`, workflow timeouts, and concurrency controls.
  - Gotcha: commits pushed with the default `GITHUB_TOKEN` don't trigger CI.
- **GitHub Copilot coding agent.** You assign an issue to Copilot, which opens a draft PR and requests your review. You iterate by mentioning `@copilot` in PR comments. [DOCS] ([GitHub docs][gh-copilot])
- **Linear.** Issues can be delegated to agents. Linear's "coding sessions" run Claude Code or Codex in a cloud environment and return a draft PR. This requires a paid plan and AI credits. Codex can be assigned directly. [DOCS] ([Linear coding sessions][linear-sessions], [Linear assigning][linear-assign]) The exact supported model list differs between Linear pages (**UNVERIFIED** which one is current).
- **GitHub Projects** offers table, board (kanban), and roadmap views, iteration fields, and built-in automations. [DOCS] ([GitHub Projects][gh-projects])
- **Repo-local task files.**
  - Anthropic's harness keeps a JSON feature list in the repo. JSON was chosen because the model is "less likely to inappropriately change or overwrite JSON" than Markdown. [ANECDOTE: Anthropic engineering] ([harnesses][harness])
  - Steve Yegge's *Beads* (`bd`) is a git-backed issue tracker with dependencies, built for agents. Yegge himself calls it "a crummy architecture (by pre-AI standards)". [ANECDOTE] ([beads repo][beads])
  - The claim that Beads "inspired Claude Code Tasks" comes from a third-party blog: **UNVERIFIED**.
- **Trade-off.** An external tracker such as GitHub Issues or Linear gives a human-visible history and phone access. Repo-local files are versioned with the code and readable by any agent or provider, but are harder to view on a phone. No comparative evidence exists for either.

### 2.4 PR-based review loops and agent code review

- **Independent review beats self-review, per Anthropic's guidance.** `/code-review` "reviews the diff in a fresh subagent that never saw your conversation. It isn't swayed by the reasoning that produced the code." A second session reviewing the first session's work is "the easiest independent reviewer you can get." [DOCS] ([best practices][cc-bp])
- **Avoid over-fixing.** "A reviewer that's asked to find problems will always find *something*. If you fix every finding, you end up with extra abstraction layers and tests for cases that can't happen." Recommended prompt shape: review against `SPEC.md`, "report gaps that affect correctness and skip style." [DOCS] ([best practices][cc-bp])
- **Review options in the Claude ecosystem** [DOCS] ([Code Review][cc-codereview], [GH Actions][cc-gha]):
  - Local `/code-review`, on any plan.
  - Ultrareview, a cloud sandbox that requires a claude.ai login.
  - The GitHub Action running the `code-review` plugin, which posts inline comments with `--comment`.
  - Managed **Code Review**, Team/Enterprise plans only: several agents examine the diff, a verification step filters false positives, and findings are posted by severity. It "never approves or blocks the pull request." It reads `REVIEW.md` from the base branch, not from the PR, and doesn't build or run the code.
- **Multi-model / cross-vendor review** [ANECDOTE, no controlled evidence found]:
  - Trail of Bits publishes a `second-opinion` plugin: "Get independent reviews from Codex or Antigravity for uncommitted changes, branch diffs, and commits." ([Trail of Bits skills][tob])
  - Several blog posts describe Claude-writes/Codex-reviews loops. The argument is that a model reviewing its own output shares its blind spots. That argument is plausible but **UNVERIFIED** by any study found. ([MindStudio][mindstudio], [Kinney][kinney])
- **Quality of AI-authored PRs.**
  - CodeRabbit (Dec 2025): 470 open-source PRs, 10.83 vs 6.45 issues per PR, 1.4x more critical issues, and logic issues up 75%. CodeRabbit says it "cannot guarantee" the authorship labels. [VENDOR] ([CodeRabbit][coderabbit])
  - Faros: PR size up 154% and review time up 91% on high-adoption teams. The 154% figure comes from secondary write-ups: **UNVERIFIED** against the primary source. [VENDOR/TELEMETRY] ([Faros][faros])

### 2.5 CI gates as guardrails

- **Three ways to enforce "done"** [DOCS] ([best practices][cc-bp]):
  - In the prompt ("keep going until they pass").
  - `/goal <condition>`, where a separate model checks the condition after each turn.
  - A **Stop hook**, which the harness enforces. Example: `pnpm tsc --noEmit 1>&2 || exit 2` blocks the turn from ending until typecheck passes. The default cap is 8 consecutive blocks (`CLAUDE_CODE_STOP_HOOK_BLOCK_CAP`).
  - The docs also say: ask for *evidence* (the command and its output), because "'All tests pass' on its own tells you very little."
- **Platform enforcement.** GitHub rulesets can require a PR before merging, required approvals, required status checks (strict or loose), blocked force pushes, restricted deletions, and linear history. [DOCS] ([GitHub rulesets][gh-rulesets])
  - The "about rulesets" page says rulesets are available "for customers on GitHub Team and GitHub Enterprise plans." Availability on Free and Pro plans for private repos is **UNVERIFIED** from the fetched page. ([about rulesets][gh-rulesets-about])
  - Anthropic's routines docs warn: before leaving a cloud scheduled task running, "protect `main` with a GitHub branch rule the task can't bypass." [DOCS] ([best practices][cc-bp])
- **Tests are a target agents can game.**
  - The Claude 4 system card (May 2025) defines reward hacking via "hard-coding" and "special-casing" tests, and reports large reductions versus Sonnet 3.7. These figures were read via a secondary quote of the card, so the exact percentages are **UNVERIFIED**. ([secondary quote][syscard-zulip])
  - Kent Beck lists warning signs: "Loops," "Functionality I hadn't asked for," and "Any indication that the genie was cheating, for example by disabling or deleting tests." [ANECDOTE] ([Beck][beck])
  - Anthropic's harness prompt states: "It is unacceptable to remove or edit tests." [ANECDOTE: Anthropic] ([harnesses][harness])
  - **Possible mitigations, not tested comparatively:** treat edits to test files or coverage config as a separate reviewable change, block those paths with a hook or deny rule during implementation tasks, ratchet coverage in CI, and run CI on the PR rather than trusting the agent's local claim.
- **Coverage gates.** No credible source found that measures coverage thresholds as an agent guardrail specifically. Effectiveness: **UNVERIFIED**.

### 2.6 Hooks

- **Events** [DOCS] ([hooks][cc-hooks]):
  - Session: `SessionStart`/`SessionEnd`.
  - Turn: `UserPromptSubmit`, `Stop`.
  - Tools: `PreToolUse` (can block), `PermissionRequest`, `PostToolUse`, `PostToolUseFailure`.
  - Subagents and tasks: `SubagentStart`/`SubagentStop`, `TaskCreated`/`TaskCompleted`, `TeammateIdle`.
  - Context: `PreCompact`/`PostCompact`, `InstructionsLoaded`.
  - Worktrees: `WorktreeCreate`/`WorktreeRemove`.
  - Other: `FileChanged` and more.
- **Mechanics** [DOCS] ([hooks][cc-hooks]):
  - Exit code 2 blocks the action and sends stderr to Claude. JSON output gives finer control.
  - Configured in `~/.claude/settings.json`, `.claude/settings.json` (committed), `.claude/settings.local.json`, plugins, or skill/subagent frontmatter.
  - The `if` filter is "best-effort." For a hard allow or deny, the docs point to the permission system.
- **Determinism caveat.** `prompt` or `agent` hooks ask another model, "so it isn't deterministic either." For real enforcement use `command`/`http` hooks with `onFailure: "block"`. [DOCS] ([best practices][cc-bp])
- **Token savings.** A `PreToolUse` hook can rewrite a test command to keep only the last 100 lines of output, so less output reaches the context. [DOCS] ([costs][cc-costs])

### 2.7 Permissions and sandboxing

- **Permission modes:** `default`/Manual, `acceptEdits`, `auto`, `dontAsk`, `bypassPermissions`, and `plan`. [DOCS] ([sub-agents][cc-subagents], [headless][cc-headless])
- **Auto mode** is the default for new interactive sessions. A classifier reviews risky actions and pauses after 3 consecutive (or 20 total) blocks. Deny rules and `PreToolUse` hooks apply *before* the classifier. The docs warn that it is "a classifier model judging each action rather than a hard rule," so "never" rules belong in deny rules or hooks. [DOCS] ([best practices][cc-bp])
- **Sandbox** (`/sandbox`; Seatbelt on macOS, bubblewrap + socat on Linux/WSL2) [DOCS] ([sandboxing][cc-sandbox]):
  - Confines shell commands. Writes are limited to the working directory. Network goes through a proxy with an allowlist that starts empty.
  - Limits: it does *not* cover file tools, MCP servers, hooks, or WebFetch. Reads of `~/.ssh` are allowed by default unless denied. `docker` is incompatible.
  - Claude can retry a command outside the sandbox unless `allowUnsandboxedCommands: false` (strict mode) is set.
  - Anthropic reports that "sandboxing safely reduces permission prompts by 84%" internally [VENDOR] and argues that "effective sandboxing requires *both* filesystem and network isolation." ([sandboxing blog][cc-sandbox-blog])
- **Prompt-injection risk.** Willison's "lethal trifecta" (Jun 2025): private data, plus exposure to untrusted content, plus the ability to communicate externally. "The only way to stay safe there is to **avoid that lethal trifecta** combination entirely." [ANECDOTE/expert analysis] ([Willison trifecta][trifecta])
  - Coding agents that read GitHub issues or web pages and hold credentials and network access fit this pattern. A gym system holds member personal data, so this matters more here.
- **Practitioner YOLO usage** [ANECDOTE]:
  - Ronacher runs `--dangerously-skip-permissions` and suggests Docker to contain it. ([Ronacher][ronacher])
  - Willison limits YOLO mode to tasks where malicious instructions can't get into the context, and says "I need to start habitually running my local agents in Docker containers." ([Willison][willison-parallel])

### 2.8 Context windows and long-running tasks

- **Context rot.** As context grows, "the model's ability to accurately recall information from that context decreases." Anthropic's techniques for long work are compaction, structured note-taking (for example `NOTES.md`), and sub-agent architectures, where subagents return summaries of "often 1,000-2,000 tokens." [DOCS/ANECDOTE: Anthropic engineering, Sep 2025] ([context engineering][context-eng])
- **Current Claude Code guidance** [DOCS] ([best practices][cc-bp]):
  - "Most current models have a 1M-token window and compaction happens on its own, so running out is rare," but `/clear` between unrelated tasks still improves focus and cost.
  - "After two failed corrections run `/clear` and write a better prompt instead of a third."
  - Write `SPEC.md` in one session, then build from a fresh session.
  - Keep CLAUDE.md short. The costs page suggests "under 200 lines," and moving procedures into skills, which load on demand. ([costs][cc-costs])
- **Effective harnesses for long-running agents** (Anthropic, Nov 26 2025) [ANECDOTE: Anthropic engineering; no completion-rate metrics published] ([harnesses][harness]):
  - Two observed failures: doing too much at once, which ran out of context mid-feature, and premature "done" declarations.
  - **Initializer session:** expand the prompt into a feature list (200+ features for a claude.ai clone, all initially marked failing, each with test steps and a `passes` field), write `init.sh`, `claude-progress.txt`, and a first git commit.
  - **Each coding session:** `pwd` → read the git log and progress file → pick the highest-priority failing feature → run `init.sh` → smoke-test end to end *first* → implement one feature → verify as a user would (browser automation; unit tests and curl were not enough) → commit → update the progress file.
  - Limitation noted: Puppeteer MCP couldn't see browser-native alert modals.
- **Unattended-run checklist from the docs:**
  1. Does it know what done looks like?
  2. Can it check its own work?
  3. What can it touch?
  4. What stops it (`--max-turns`, `--max-budget-usd`, a spend limit)?
  5. Where does the result go, and who signs off?

  Also: "watch the first few runs of anything new before trusting it unattended." [DOCS] ([best practices][cc-bp])
- **"Slot machine" approach.** Commit, let Claude run about 30 minutes, then accept the result or restart. Attributed to Anthropic's Data Science team in the July 2025 "How Anthropic teams use Claude Code" report. The fetched blog page does not contain it, and secondary sources attribute it to the PDF: **UNVERIFIED**. ([Anthropic teams][anthropic-teams])

### 2.9 Handoff and progress files across sessions and providers

- **Within Claude Code** [DOCS] ([common workflows][cc-workflows], [headless][cc-headless]):
  - `--continue`/`--resume` and `/rename`.
  - `claude --from-pr <n>` finds the sessions linked to a PR.
  - Headless sessions can be resumed by ID.
- **Across providers.** **AGENTS.md** is an open instruction-file format, now hosted by the Linux Foundation's Agentic AI Foundation (announced Dec 2025, alongside MCP and goose). ([OpenAI AAIF announcement][aaif])
  - Adoption figures ("60,000+ projects") are **UNVERIFIED**.
  - Claude Code reads `CLAUDE.md`. Whether it natively reads `AGENTS.md` was not verified in this research (**UNVERIFIED**), so multi-provider setups may need both files or a pointer from one to the other.
- **Pattern.** A repo-tracked progress log plus a machine-readable task list plus git history serves as the shared memory any provider's agent can read. This is the Anthropic harness approach, generalized; it is not tested across providers.

### 2.10 Cost management

- **Cost figures** [DOCS/VENDOR] ([costs][cc-costs]):
  - Enterprise average: about **$13 per developer per active day** and **$150–250 per developer per month**, and under $30 per active day for 90% of users. This is enterprise data and may not reflect parallel or agent-heavy solo use.
  - Subscription plans (Pro/Max/Ultra) use usage windows rather than per-token billing. API billing is per token.
- **Cost controls** [DOCS] ([costs][cc-costs], [headless][cc-headless], [best practices][cc-bp]):
  - Monitoring: `/usage`, which shows per-skill, subagent, and MCP attribution and recommendations; the status-line cost field; and `total_cost_usd` in `-p --output-format json`.
  - Hard caps: `--max-budget-usd` and `--max-turns`, plus workspace spend limits on the Console.
  - Model choice: Sonnet for routine work, Haiku for simple subagents, `CLAUDE_CODE_SUBAGENT_MODEL`.
  - Lower effort for quick iterations.
  - Hooks that trim command output.
  - Prefer CLIs over MCP where possible: "Tools like `gh` ... are still more context-efficient than MCP servers."
- **Multi-agent multipliers.** About 15x chat tokens in Anthropic's research system [VENDOR] ([multi-agent][multiagent]). Agent teams use about 7x a standard session with teammates in plan mode [DOCS] ([costs][cc-costs]).
- **Hidden spend.** `/loop` and scheduled tasks, goal check-ins, idle compaction, and agent teammates all keep consuming tokens while idle. [DOCS] ([costs][cc-costs])

### 2.11 Observability

- **OpenTelemetry** (`CLAUDE_CODE_ENABLE_TELEMETRY=1`) [DOCS] ([monitoring][cc-otel]):
  - Metrics: `claude_code.session.count`, `lines_of_code.count`, `pull_request.count`, `commit.count`, `cost.usage`, `token.usage`, `active_time.total`, and `code_edit_tool.decision`.
  - Events: `tool_result`, `api_request` (with `cost_usd`), `api_error`, and `tool_decision`.
  - Traces are in beta.
  - Prompt and response content is redacted unless opted in.
  - Backends: any OTLP collector or Prometheus.
- **Lighter options** [DOCS] ([costs][cc-costs], [headless][cc-headless], [GH Actions][cc-gha]):
  - `/insights` writes an HTML report of friction points to `~/.claude/usage-data/report.html`.
  - `stream-json` output from headless runs.
  - GitHub Actions logs.
- **Anthropic's multi-agent team** added full production tracing to diagnose failures. [ANECDOTE: Anthropic] ([multi-agent][multiagent])

### 2.12 Measuring agent output quality

- **Anthropic's "Demystifying evals for AI agents" (Jan 9 2026)** [DOCS/ANECDOTE: Anthropic engineering] ([evals][evals]):
  - "Deterministic graders are natural for coding agents."
  - Grade the outcome, not the path: "it's often better to grade what the agent produced, not the path it took."
  - "20-50 simple tasks drawn from real failures is a great start."
  - Read transcripts: "You won't know if your graders are working well unless you read the transcripts."
  - pass@k (at least one success in k attempts) vs pass^k (all k succeed). Example: 75% per trial gives about 42% for three in a row.
  - Regression evals should sit near 100%.
- **Productivity measurement is hard even for researchers** [RCT] ([METR 2025][metr-2025], [METR 2026][metr-2026]):
  - Developers' self-estimates were off by about 39 percentage points: they felt 20% faster but were 19% slower.
  - METR's 2026 redesign cites developers refusing to work without AI, 30–50% withholding tasks, and unreliable time measurement when developers run concurrent agents.
  - Implication: perceived velocity is not a reliable signal.
- **Candidate solo-scale metrics** (the owner would choose; none is validated for agent work):
  - CI pass rate on the first push.
  - Number of review round-trips per PR.
  - Reverts or hotfixes within N days of a merge.
  - Escaped bugs per milestone.
  - Cost per merged PR (from OTel or `total_cost_usd`).
  - DORA-style change failure rate. DORA 2025 associates AI with rising instability. ([DORA][dora])

### 2.13 Kanban and milestone cadence for a solo project

- **No rigorous evidence found** specific to solo + agent cadence. What exists is anecdote and analogy:
  - Willison: land one significant change at a time, with low-load parallel tasks alongside. ([Willison][willison-parallel])
  - Anthropic harness: one feature per session, with features ordered by priority in a list. ([harnesses][harness])
  - Agent teams docs: tasks should be "self-contained units that produce a clear deliverable." Too small means overhead dominates; too large means too long without check-ins. ([agent teams][cc-teams])
  - Beck: separate structural commits from behavioral commits ("Tidy First"), and work in a strict TDD cycle. ([Beck][beck])
  - Ronacher: refactor once the project outgrows what agents handle well, but "You don't want to do it too early." ([Ronacher][ronacher])
- **Tooling options:** a GitHub Projects board (kanban, iterations, milestones), Linear cycles, or repo-local `features.json` plus a progress file. The trade-offs are covered in §2.3.

### 2.14 Common failure modes and mitigations

| Failure mode | Source | Mitigations (from sources) |
|---|---|---|
| Declares done prematurely | Anthropic harness; agent-teams docs ("lead can stop early") | Structured feature list; end-to-end verification before marking pass; `/goal`; Stop hook |
| Tries to do too much; context runs out mid-feature | Anthropic harness | One feature per session; commit and update progress notes at session end |
| Deletes, disables, or special-cases tests | Beck; Claude 4 system card (secondary) | Explicit rule; protect test paths with a hook or deny rule; CI on the PR; independent review |
| Scope creep ("functionality I hadn't asked for") | Beck | Spec-based review ("skip style"); small tasks |
| Loops or repeated failed corrections | Beck; CC best practices | `/clear` after two failed corrections; `--max-turns`; Stop-hook block cap |
| Builds on a wrong assumption unattended | CC best practices | Spec/goal; watch the first runs; cap turns and spend; human approves the merge |
| Parallel edits collide | Agent-teams docs | Worktrees; give each agent its own files; separate DB/ports (Ronacher) |
| Over-fixing reviewer nitpicks | CC best practices | Review prompt limited to correctness against the spec |
| Rules in CLAUDE.md ignored | CC best practices | Move hard rules to hooks, deny rules, or the sandbox |
| Prompt injection / data exfiltration | Willison trifecta; Anthropic sandboxing | Sandbox with filesystem and network isolation; network allowlist; no production data or secrets in the agent environment |
| Review bottleneck / owner overwhelmed | Willison; Faros | Limit work in progress to review capacity; specs make review cheaper ("Code that started from your own specification is a lot less effort to review") |
| Perceived productivity ≠ actual | METR | Track objective metrics (§2.12) |
| Losing track of parallel work ("parallel agent psychosis") | Willison, Mar 2026, via search summary: **UNVERIFIED** wording | One tracker as the single source of truth; links between sessions and PRs (`--from-pr`) |
| Fresh worktree missing env or deps | CC best practices | `.worktreeinclude`; install command in CLAUDE.md |
| CI not triggered on agent commits | GH Actions docs | Use the GitHub App token, not the default `GITHUB_TOKEN` |

---

## 3. Candidate workflow options

These are options for the owner to choose from, not recommendations. Costs are qualitative because no source gives solo-scale numbers.

### Option A: Lightweight, interactive, single agent

- **Shape:** one Claude Code session at a time in plan mode, then implementation, then the owner reviews the diff locally and merges.
  - CLAUDE.md lists the test, lint, and typecheck commands.
  - A Stop hook runs the typecheck.
  - `/code-review` runs before each commit.
  - CI runs lint, typecheck, and tests on push.
- **Pros:** lowest cost and setup. The owner sees everything. Matches METR's lesson that perceived speed can mislead, so close oversight helps.
- **Cons:** throughput is limited to one stream of work. The owner's time is the bottleneck. Weak audit trail if work isn't done through PRs.
- **Fits if:** budget is tight, the owner wants to learn the codebase deeply, or the early architecture phase is underway.

### Option B: Medium, PR-centric with an independent reviewer

- **Shape:**
  - Each task is a GitHub Issue (or a row in `features.json`). Each runs in its own worktree (`claude -w`), with a cap of about 2–3 concurrent tasks.
  - The agent opens a PR. CI is a required status check, and `main` is protected by a ruleset.
  - An automated review runs on the PR (GitHub Action + `code-review` plugin, or managed Code Review on Team/Enterprise plans).
  - Optional: a second-vendor review (Codex/Gemini).
  - The owner approves the merge.
  - A progress file plus milestones on a Projects board.
- **Pros:** an audit trail per change. Gates are enforced by the platform rather than by prompts. Parallelism is bounded by review capacity.
- **Cons:** more setup (Actions minutes, secrets, GitHub App permissions; the app requests a broad permission set). Review costs tokens on every PR. Worktrees need DB/port isolation for integration tests.
- **Fits if:** the owner wants to review at the PR level rather than per keystroke, and has a moderate budget.

### Option C: Heavyweight, long-running autonomous harness + parallel agents

- **Shape:**
  - An Anthropic-style initializer/coding-agent harness: feature list, progress file, `init.sh`, end-to-end browser tests.
  - Headless `claude -p` runs or cloud routines, sandboxed, with `--max-budget-usd` and `--max-turns`.
  - Several agents in parallel worktrees or cloud sessions; optionally agent teams (experimental).
  - Issue-triggered agents (`@claude`, Copilot, Linear).
  - Multi-model review, OTel dashboards, and an eval set built from real failures.
  - The owner reviews PRs and samples transcripts.
- **Pros:** highest potential throughput. Work can progress overnight.
- **Cons:**
  - Highest token cost (multi-agent multipliers of about 7–15x).
  - The most failure modes: premature done, gamed tests, unattended drift.
  - Review load can exceed what one person can handle (Faros, Willison).
  - Experimental features have known limitations.
  - The security surface grows (lethal trifecta).
  - No published completion-rate data for this pattern.
- **Fits if:** the budget is ample, a solid test suite and CI exist *first*, and the owner accepts sampling-based review.

### Option D: Hybrid, staged escalation

- Start with A during architecture and the first vertical slice. Move to B once CI, test coverage, and the spec format are stable. Use C only for well-specified, test-covered, mechanical work (migrations, CRUD screens, `/batch`-style changes).
- This follows the "simplest solution first, add complexity only when it demonstrably improves outcomes" principle ([BEA][bea]). Its effectiveness for this project is untested.

---

## 4. Repository setup checklist

Each item is an option, with the source that describes it.

**Instructions and context**
- [ ] Short `CLAUDE.md` (the docs suggest under 200 lines) with build, test, lint, and typecheck commands, conventions, gotchas, and decisions that aren't in the code ([best practices][cc-bp], [costs][cc-costs])
- [ ] `AGENTS.md` for non-Claude agents, or a pointer between it and `CLAUDE.md` (Claude Code's native support: **UNVERIFIED**) ([AAIF][aaif])
- [ ] `.claude/rules/*.md` path-scoped rules ([hooks: InstructionsLoaded][cc-hooks])
- [ ] `.claude/skills/` for repeatable procedures, with `disable-model-invocation: true` on side-effecting ones such as deploy ([skills][cc-skills])
- [ ] `.claude/agents/reviewer.md` with `tools: Read, Grep, Glob` (read-only spec reviewer) ([best practices][cc-bp])
- [ ] `SPEC.md` per feature (format: see the sibling doc)
- [ ] `REVIEW.md` if using managed Code Review ([Code Review][cc-codereview])

**Long-running state**
- [ ] `features.json` (feature list with `passes` flags) and `claude-progress.txt` (or the GitHub Issues/Projects equivalent) ([harnesses][harness])
- [ ] `init.sh` / Makefile target that starts the app idempotently, refusing to start a second copy ([harnesses][harness], [Ronacher][ronacher])
- [ ] Dev-mode logging to a file the agent can read, with emails logged to stdout in dev ([Ronacher][ronacher])

**Enforcement (deterministic)**
- [ ] `.claude/settings.json` `Stop` hook running typecheck and/or fast tests ([best practices][cc-bp])
- [ ] `PreToolUse` hook or deny rules protecting test files, migrations, generated code, and `.env` ([best practices][cc-bp], [hooks][cc-hooks])
- [ ] Sandbox on (`/sandbox`), optionally strict (`allowUnsandboxedCommands: false`), with a network domain allowlist and `Read` deny rules for secrets ([sandboxing][cc-sandbox])
- [ ] Alternative or addition: run agents in a dev container or VM (see [sandbox environments][cc-sandbox-env]; not researched in depth here)

**Parallelism**
- [ ] `.worktreeinclude` listing needed gitignored files, plus `worktree.symlinkDirectories` for large directories ([best practices][cc-bp])
- [ ] Per-worktree DB and port isolation scheme ([Ronacher][ronacher]; design is the owner's call)

**CI/CD and GitHub**
- [ ] CI workflow: lint, typecheck, unit and integration tests, optional coverage ratchet, optional end-to-end browser tests
- [ ] Ruleset on `main`: require a PR, require status checks, block force pushes, restrict deletions, optionally linear history and required approvals ([rulesets][gh-rulesets]; plan availability **UNVERIFIED** for private repos on Free/Pro)
- [ ] `anthropics/claude-code-action@v1` workflows: `@claude` responder and/or PR review, with `--max-turns`, `timeout-minutes`, and `concurrency` ([GH Actions][cc-gha])
- [ ] Issue templates that give the agent context up front ([GH Actions][cc-gha])
- [ ] Optional: Copilot coding agent or Linear delegation ([gh-copilot][gh-copilot], [Linear][linear-sessions])

**Cost and observability**
- [ ] Spend caps: Console workspace limit or subscription usage-credit limit; `--max-budget-usd` on headless runs ([costs][cc-costs])
- [ ] Subagent model routing (`model:` frontmatter, `CLAUDE_CODE_SUBAGENT_MODEL`) ([sub-agents][cc-subagents])
- [ ] OTel export to a local collector or Prometheus, or rely on `/usage` and `/insights` ([monitoring][cc-otel])
- [ ] Small eval/regression set of 20–50 tasks taken from real agent failures ([evals][evals])

---

## 5. Open questions for the project owner

1. **Budget:** What monthly spend is acceptable for AI tooling: a subscription plan (Pro/Max/Ultra/Team), API pay-per-token, or both? Is there a hard ceiling that should be enforced technically?
2. **Providers and tools available:** Which accounts do you have or are willing to get: Claude (which plan), OpenAI Codex/ChatGPT, Google Gemini/Antigravity, GitHub Copilot, Linear? Is cross-vendor review wanted?
3. **GitHub plan:** Is the repo on GitHub, public or private, and on which plan? This affects ruleset availability and Actions minutes. Managed Claude Code Review requires a Claude Team/Enterprise plan; is that in scope?
4. **Review appetite:** How much do you want to personally review: every diff, every PR (summary plus spot checks), or sampled transcripts only? How many hours per week can you give to review?
5. **Autonomy level:** Are unattended or overnight runs (headless, cloud routines, `@claude` from issues) acceptable, or should every agent session be supervised?
6. **Parallelism:** How many concurrent agent streams do you want to manage? Secondary coverage says Willison often runs 3–4 (**UNVERIFIED** against his post), and his review-bottleneck caveat applies.
7. **Tracker location:** Should the task source of truth be GitHub Issues/Projects, Linear, or repo-local files (`features.json` + progress log)? Do you need phone access?
8. **Security posture:** Will agents ever touch real member or employee personal data, payment systems, or production credentials? Should agents run in the built-in sandbox, a dev container/VM, or a cloud sandbox? Is YOLO/`bypassPermissions` ever acceptable?
9. **Test strategy as a gate:** Do you want TDD-style workflows (Beck), end-to-end browser tests as the definition of done (Anthropic harness), coverage thresholds, or a mix? Should test-file edits require separate approval?
10. **Quality metrics:** Which signals do you want tracked: first-push CI pass rate, review round-trips, reverts, cost per merged PR, escaped bugs? Do you want an OTel dashboard or just periodic `/usage`/`/insights` reports?
11. **Experimental features:** Are you willing to use experimental features such as agent teams and Claude Code's research-preview `claude agents` view, which have documented limitations?
12. **Cadence:** Do you prefer milestone-based releases (vertical slices), fixed weekly iterations, or continuous flow kanban? Do you want a WIP limit tied to review capacity?

---

## 6. Sources

**Anthropic / Claude Code official docs** (fetched 2026-11-12)
- [cc-bp]: https://code.claude.com/docs/en/best-practices (redirect target of anthropic.com/engineering/claude-code-best-practices)
- [cc-workflows]: https://code.claude.com/docs/en/common-workflows
- [cc-subagents]: https://code.claude.com/docs/en/sub-agents
- [cc-hooks]: https://code.claude.com/docs/en/hooks
- [cc-skills]: https://code.claude.com/docs/en/skills
- [cc-headless]: https://code.claude.com/docs/en/headless
- [cc-gha]: https://code.claude.com/docs/en/github-actions
- [cc-codereview]: https://code.claude.com/docs/en/code-review
- [cc-costs]: https://code.claude.com/docs/en/costs
- [cc-otel]: https://code.claude.com/docs/en/monitoring-usage
- [cc-teams]: https://code.claude.com/docs/en/agent-teams
- [cc-sandbox]: https://code.claude.com/docs/en/sandboxing
- [cc-sandbox-env]: https://code.claude.com/docs/en/sandbox-environments (linked from the sandboxing page; not fetched)

**Anthropic engineering blog**
- [harness]: Effective harnesses for long-running agents, Nov 26 2025. https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents
- [context-eng]: Effective context engineering for AI agents, Sep 29 2025. https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents
- [multiagent]: How we built our multi-agent research system, Jun 13 2025. https://www.anthropic.com/engineering/multi-agent-research-system
- [bea]: Building effective agents, Dec 19 2024. https://www.anthropic.com/engineering/building-effective-agents
- [evals]: Demystifying evals for AI agents, Jan 9 2026. https://www.anthropic.com/engineering/demystifying-evals-for-ai-agents
- [cc-sandbox-blog]: Beyond permission prompts (Claude Code sandboxing), Oct 20 2025. https://www.anthropic.com/engineering/claude-code-sandboxing
- [anthropic-teams]: How Anthropic teams use Claude Code, Jul 24 2025. https://claude.com/blog/how-anthropic-teams-use-claude-code
- [syscard-zulip]: secondary quote of the Claude Opus 4 / Sonnet 4 system card on reward hacking. https://leanprover-community.github.io/archive/stream/219941-Machine-Learning-for-Theorem-Proving/topic/System.20card.3A.20Claude.20Opus.204.20.26.20Claude.20Sonnet.204.html

**Studies and reports**
- [metr-2025]: METR, Measuring the Impact of Early-2025 AI on Experienced Open-Source Developer Productivity, Jul 10 2025. https://metr.org/blog/2025-07-10-early-2025-ai-experienced-os-dev-study/
- [metr-2026]: METR, uplift update, Feb 24 2026. https://metr.org/blog/2026-02-24-uplift-update/
- [dora]: Google DORA, 2025 State of AI-assisted Software Development. https://services.google.com/fh/files/misc/2025_state_of_ai_assisted_software_development.pdf (findings read via secondary summaries: https://www.infoq.com/news/2025/09/dora-state-of-ai-in-dev-2025 , https://redmonk.com/rstephens/?p=943)
- [faros]: Faros AI, AI Productivity Paradox Report 2025 (vendor). https://www.faros.ai/blog/ai-productivity-paradox-report-2025
- [coderabbit]: CodeRabbit, State of AI vs Human Code Generation, Dec 2025 (vendor). https://www.coderabbit.ai/blog/state-of-ai-vs-human-code-generation-report

**Platform docs**
- [gh-rulesets]: https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/available-rules-for-rulesets
- [gh-rulesets-about]: https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/about-rulesets
- [gh-projects]: https://docs.github.com/en/issues/planning-and-tracking-with-projects/learning-about-projects/about-projects
- [gh-copilot]: https://docs.github.com/en/copilot/using-github-copilot/coding-agent/using-copilot-to-work-on-an-issue
- [linear-sessions]: https://linear.app/docs/coding-sessions
- [linear-assign]: https://linear.app/docs/assigning-issues
- [aaif]: OpenAI, Agentic AI Foundation announcement. https://openai.com/index/agentic-ai-foundation/

**Practitioner reports**
- [willison-parallel]: Simon Willison, Embracing the parallel coding agent lifestyle, Oct 5 2025. https://simonwillison.net/2025/Oct/5/parallel-coding-agents/
- [trifecta]: Simon Willison, The lethal trifecta for AI agents, Jun 16 2025. https://simonwillison.net/2025/Jun/16/the-lethal-trifecta/
- [ronacher]: Armin Ronacher, Agentic Coding Recommendations, Jun 12 2025. https://lucumr.pocoo.org/2025/6/12/agentic-coding/
- [beck]: Kent Beck, Augmented Coding: Beyond the Vibes, Jun 25 2025. https://newsletter.kentbeck.com/p/augmented-coding-beyond-the-vibes
- [beads]: Steve Yegge, Beads. https://github.com/steveyegge/beads
- [tob]: Trail of Bits skills (second-opinion plugin). https://github.com/trailofbits/skills
- [mindstudio]: Cross-vendor review (blog; low credibility, anecdote). https://www.mindstudio.ai/blog/cross-vendor-ai-agent-review-claude-codex/
- [kinney]: Steve Kinney, Codex as a second opinion (not fetched; listed from search). https://stevekinney.com/writing/codex-as-a-second-opinion

[cc-bp]: https://code.claude.com/docs/en/best-practices
[cc-workflows]: https://code.claude.com/docs/en/common-workflows
[cc-subagents]: https://code.claude.com/docs/en/sub-agents
[cc-hooks]: https://code.claude.com/docs/en/hooks
[cc-skills]: https://code.claude.com/docs/en/skills
[cc-headless]: https://code.claude.com/docs/en/headless
[cc-gha]: https://code.claude.com/docs/en/github-actions
[cc-codereview]: https://code.claude.com/docs/en/code-review
[cc-costs]: https://code.claude.com/docs/en/costs
[cc-otel]: https://code.claude.com/docs/en/monitoring-usage
[cc-teams]: https://code.claude.com/docs/en/agent-teams
[cc-sandbox]: https://code.claude.com/docs/en/sandboxing
[cc-sandbox-env]: https://code.claude.com/docs/en/sandbox-environments
[harness]: https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents
[context-eng]: https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents
[multiagent]: https://www.anthropic.com/engineering/multi-agent-research-system
[bea]: https://www.anthropic.com/engineering/building-effective-agents
[evals]: https://www.anthropic.com/engineering/demystifying-evals-for-ai-agents
[cc-sandbox-blog]: https://www.anthropic.com/engineering/claude-code-sandboxing
[anthropic-teams]: https://claude.com/blog/how-anthropic-teams-use-claude-code
[syscard-zulip]: https://leanprover-community.github.io/archive/stream/219941-Machine-Learning-for-Theorem-Proving/topic/System.20card.3A.20Claude.20Opus.204.20.26.20Claude.20Sonnet.204.html
[metr-2025]: https://metr.org/blog/2025-07-10-early-2025-ai-experienced-os-dev-study/
[metr-2026]: https://metr.org/blog/2026-02-24-uplift-update/
[dora]: https://services.google.com/fh/files/misc/2025_state_of_ai_assisted_software_development.pdf
[faros]: https://www.faros.ai/blog/ai-productivity-paradox-report-2025
[coderabbit]: https://www.coderabbit.ai/blog/state-of-ai-vs-human-code-generation-report
[gh-rulesets]: https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/available-rules-for-rulesets
[gh-rulesets-about]: https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/about-rulesets
[gh-projects]: https://docs.github.com/en/issues/planning-and-tracking-with-projects/learning-about-projects/about-projects
[gh-copilot]: https://docs.github.com/en/copilot/using-github-copilot/coding-agent/using-copilot-to-work-on-an-issue
[linear-sessions]: https://linear.app/docs/coding-sessions
[linear-assign]: https://linear.app/docs/assigning-issues
[aaif]: https://openai.com/index/agentic-ai-foundation/
[willison-parallel]: https://simonwillison.net/2025/Oct/5/parallel-coding-agents/
[trifecta]: https://simonwillison.net/2025/Jun/16/the-lethal-trifecta/
[ronacher]: https://lucumr.pocoo.org/2025/6/12/agentic-coding/
[beck]: https://newsletter.kentbeck.com/p/augmented-coding-beyond-the-vibes
[beads]: https://github.com/steveyegge/beads
[tob]: https://github.com/trailofbits/skills
[mindstudio]: https://www.mindstudio.ai/blog/cross-vendor-ai-agent-review-claude-codex/
[kinney]: https://stevekinney.com/writing/codex-as-a-second-opinion
