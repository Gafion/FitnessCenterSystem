# Agent Research Methodology

How to make LLM agents (for example Claude Code subagents) research subjects well, and how to structure their outputs so later planning agents can use them.

- **Status:** Research output. It contains no project decisions. It sets out evidence, options and trade-offs.
- **Date:** 2026-11-12
- **Scope:** Briefing and prompting research agents, multi-agent architectures, search strategy, source evaluation and citation, hallucination and confirmation bias, verification, conflicting sources, synthesis, when to stop, and output structure.
- **Convention:** Every factual claim has an inline URL. Some statements are this document's own recommendations rather than sourced facts, and they are labelled **[Synthesis]**. Claims that could not be checked against a primary source are labelled **UNVERIFIED**.

---

## 1. Executive summary

1. **Orchestrator-worker is the documented pattern for broad research, and it is expensive.** Anthropic's research system uses a lead agent that plans and spawns parallel subagents. On Anthropic's internal research eval, this setup beat a single agent by 90.2%. Multi-agent runs used about 15x the tokens of a chat, and token usage alone explained 80% of the performance variance on BrowseComp. ([Anthropic, multi-agent research system](https://www.anthropic.com/engineering/multi-agent-research-system)) A documented counter-position says multi-agent setups are fragile when subagents must make interdependent decisions, because context is not fully shared. ([Cognition, "Don't Build Multi-Agents"](https://cognition.com/blog/dont-build-multi-agents)) Research is a good fit because it parallelises well. Synthesis and decisions are a poor fit for splitting across agents.
2. **The brief matters most.** Anthropic found that vague delegation caused duplicated work and gaps. A good brief gives the agent one objective, an output format, guidance on tools and sources, and clear task boundaries. ([Anthropic](https://www.anthropic.com/engineering/multi-agent-research-system); [research_lead_agent.md](https://github.com/anthropics/claude-cookbooks/blob/main/patterns/agents/prompts/research_lead_agent.md))
3. **Search broad first, then narrow.** Short, broad queries come first, then narrower ones. Agents left alone tend to write long, over-specific queries that return little. ([Anthropic](https://www.anthropic.com/engineering/multi-agent-research-system)) STORM widens coverage by asking questions from several simulated perspectives. ([Shao et al., STORM](https://arxiv.org/abs/2402.14207))
4. **A citation is not proof.** In a human evaluation of generative search engines, only 51.5% of generated sentences were fully supported by their citations, and 74.5% of citations supported the sentence they were attached to. ([Liu et al. 2023](https://arxiv.org/abs/2304.09848)) OpenAI's deep research model still had a 0.13 hallucination rate on PersonQA. ([OpenAI deep research system card](https://cdn.openai.com/deep-research-system-card.pdf)) Every output therefore needs a separate check that each claim is supported by its source.
5. **Use verification passes that work.** Documented techniques include:
   - letting the agent say "I don't know"
   - extracting direct quotes before analysing
   - finding a supporting quote for each claim after drafting, and retracting claims that have none ([Anthropic docs](https://platform.claude.com/docs/en/test-and-evaluate/strengthen-guardrails/reduce-hallucinations))
   - answering verification questions independently of the draft ([Chain-of-Verification](https://arxiv.org/abs/2309.11495))
   - splitting a report into atomic facts and checking each one by search ([SAFE](https://arxiv.org/abs/2403.18802))
6. **Watch for bias and omission, not only fabrication.**
   - Models are sycophantic: they lean towards the user's stated view. ([Sharma et al.](https://arxiv.org/abs/2310.13548)) Briefs should therefore not presuppose answers.
   - Agents preferred SEO content farms over authoritative sources until they were given source-quality heuristics. ([Anthropic](https://www.anthropic.com/engineering/multi-agent-research-system))
   - Information that is missing is hard to spot, because "it's very possible for the tool to miss out on crucial details" that never appeared in its searches. ([Willison](https://simonwillison.net/2025/Feb/25/deep-research-system-card/))
7. **Set explicit stopping rules.** Anthropic's prompts give a tool-call budget that scales with task difficulty and an instruction to stop at diminishing returns. Without them, agents searched endlessly for sources that did not exist or kept going after they had enough. ([research_subagent.md](https://github.com/anthropics/claude-cookbooks/blob/main/patterns/agents/prompts/research_subagent.md); [Anthropic](https://www.anthropic.com/engineering/multi-agent-research-system))
8. **Write outputs to files and pass references.** Subagents should save their outputs to the filesystem and pass short references back, which avoids a "game of telephone" through the lead agent. ([Anthropic](https://www.anthropic.com/engineering/multi-agent-research-system)) Each returned summary should be condensed, typically 1,000–2,000 tokens. ([Anthropic, context engineering](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents))

---

## 2. Detailed findings

### 2.1 Architectures: single agent, workflows, orchestrator-workers

- **Start simple.** Anthropic advises "finding the simplest solution possible, and only increasing complexity when needed." It names the orchestrator-workers pattern, in which a central LLM "breaks down tasks, delegates them to worker LLMs, and synthesizes their results." It also names the evaluator-optimizer pattern, in which one LLM generates and another critiques in a loop, and says this suits tasks with clear evaluation criteria. ([Building effective agents](https://www.anthropic.com/engineering/building-effective-agents))
- **Anthropic's production research system.**
  - A lead agent plans, saves its plan to memory (because context beyond 200k tokens gets truncated), spawns 3–5 parallel subagents, and synthesizes their results.
  - A separate CitationAgent attaches sources to claims at the end.
  - Parallelisation cut research time by up to 90% on complex queries.
  - Multi-agent systems fit tasks with heavy parallelisation, information that exceeds one context window, and many complex tools. They fit coding tasks poorly.
  - "The value of the task" must justify the token cost.
  - ([Anthropic](https://www.anthropic.com/engineering/multi-agent-research-system))
- **Effort-scaling rules embedded in prompts.**
  - Simple fact-finding: 1 agent with 3–10 tool calls.
  - Comparisons: 2–4 subagents with 10–15 calls each.
  - Complex research: more than 10 subagents.
  - Observed failure: 50 subagents spawned for a simple query.
  - ([Anthropic](https://www.anthropic.com/engineering/multi-agent-research-system))
- **Query typing in the lead prompt.** The open-sourced lead prompt classifies each query as one of three types:
  - *depth-first*: many perspectives on one issue
  - *breadth-first*: independent sub-questions
  - *straightforward*: a single focused lookup

  It then plans subagents to match the type. It also tells the lead to coordinate and synthesize rather than do primary research itself. ([research_lead_agent.md](https://github.com/anthropics/claude-cookbooks/blob/main/patterns/agents/prompts/research_lead_agent.md))
- **Google Gemini Deep Research** describes the same structure in its own terms ([Gemini Deep Research overview](https://gemini.google/overview/deep-research/); the fetched page was a localised version, so wording varies by locale):
  - it plans first and breaks the problem into sub-tasks, and the user can review the plan
  - at each step it reasons over what it has gathered so far to find missing information and discrepancies
  - an asynchronous task manager shares state between planner and task models
  - it runs multiple self-critique passes before the final report
- **The counter-argument.** Cognition states two principles: "Share context, and share full agent traces, not just individual messages" and "Actions carry implicit decisions, and conflicting decisions carry bad results". It argues that parallel subagents with partial context make conflicting assumptions. ([Cognition](https://cognition.com/blog/dont-build-multi-agents))
- **Failure taxonomy.** MAST analysed more than 1,600 traces from 7 multi-agent frameworks. It found 14 failure modes in 3 categories: system design issues, inter-agent misalignment, and task verification. ([Cemri et al.](https://arxiv.org/abs/2503.13657))
- **Claude Code subagents** work as follows ([Claude Code docs](https://code.claude.com/docs/en/sub-agents)):
  - Each subagent "runs in its own context window."
  - It starts fresh, without the parent's conversation, so the brief must stand alone.
  - Only its summary returns to the parent.
  - Running many subagents that each return detailed results "can consume significant context."
- **Overuse.** Anthropic's prompting guide warns that recent Claude models may spawn subagents where a direct approach would suffice. It suggests limiting subagents to tasks that "can run in parallel, require isolated context, or involve independent workstreams." ([Prompting best practices](https://platform.claude.com/docs/en/build-with-claude/prompt-engineering/claude-prompting-best-practices))

**Options and trade-offs [Synthesis]:**

| Option | Benefits | Costs and risks | Evidence |
|---|---|---|---|
| A. One agent per topic, run in parallel, orchestrator synthesizes (the current setup) | Parallel, isolated contexts; matches Anthropic's pattern | High token use; risk of overlaps or gaps if briefs are vague; orchestrator sees only summaries | Anthropic; Claude Code docs |
| B. A + a separate verification agent per output | Catches unsupported citations before planning starts | More tokens and time | Liu et al.; CoVe; SAFE; Anthropic CitationAgent |
| C. A single sequential agent doing all research | Full shared context, no conflicting assumptions | Context overflow on a broad domain; slower | Cognition; Lost in the Middle |
| D. A + a cross-topic consistency review before planning docs | Catches contradictions between topic reports | Extra step | MAST "inter-agent misalignment"; Cognition |

### 2.2 Briefing and prompting research agents

- **Required elements.** The lead prompt says each subagent brief should include ([research_lead_agent.md](https://github.com/anthropics/claude-cookbooks/blob/main/patterns/agents/prompts/research_lead_agent.md)):
  - ideally 1 core objective
  - the expected output format
  - background context and how this task fits the plan
  - key questions to answer
  - suggested starting sources, what counts as a reliable source, and which sources to avoid
  - which tools to use
  - "precise scope boundaries to prevent research drift"
- **Explain why.** Context or motivation behind instructions helps the model "deliver more targeted responses." ([Prompting best practices](https://platform.claude.com/docs/en/build-with-claude/prompt-engineering/claude-prompting-best-practices))
- **Define success criteria and require cross-source checking.** For complex research, Anthropic's sample prompt says: "develop several competing hypotheses. Track your confidence levels in your progress notes … Regularly self-critique your approach and plan. Update a hypothesis tree or research notes file." ([Prompting best practices](https://platform.claude.com/docs/en/build-with-claude/prompt-engineering/claude-prompting-best-practices))
- **Pitch the prompt at the "right altitude".** It should sit between brittle hard-coded rules and vague guidance. Start minimal and add instructions as failures are observed. ([Context engineering](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents))
- **Give heuristics, not rigid scripts.** Teach agents how to break down questions, judge source quality, and balance depth against breadth, and add guardrails against runaway behaviour. ([Anthropic](https://www.anthropic.com/engineering/multi-agent-research-system))
- **Test the prompts.** "Think like your agents": run the actual prompts and watch the agents step by step. Anthropic found about 20 test queries were enough early on to detect large effects. ([Anthropic](https://www.anthropic.com/engineering/multi-agent-research-system))
- **Avoid leading framing.** Assistants "consistently exhibit sycophancy," and a response that "matches a user's views" is more likely to be preferred. ([Sharma et al.](https://arxiv.org/abs/2310.13548)) **[Synthesis]** Briefs should ask open questions ("what options exist and what are the trade-offs") rather than state a hypothesis to confirm ("confirm that X is best").

### 2.3 Search strategy

- **Broad first, then narrow.** Anthropic advises starting with "short, broad queries," then narrowing. Its subagent prompt advises queries "under 5 words", broadening when results are sparse and narrowing when they are plentiful. ([Anthropic](https://www.anthropic.com/engineering/multi-agent-research-system); [research_subagent.md](https://github.com/anthropics/claude-cookbooks/blob/main/patterns/agents/prompts/research_subagent.md))
- **Fetch full pages.** The subagent prompt makes search-then-fetch the core loop and says to fetch full pages rather than rely on snippets. It also says "NEVER repeatedly use the exact same queries." ([research_subagent.md](https://github.com/anthropics/claude-cookbooks/blob/main/patterns/agents/prompts/research_subagent.md))
- **Use an OODA loop with thinking between steps.** The agent observes, orients, decides and acts in a loop, with interleaved thinking after each tool result to judge quality and gaps. ([research_subagent.md](https://github.com/anthropics/claude-cookbooks/blob/main/patterns/agents/prompts/research_subagent.md); [Anthropic](https://www.anthropic.com/engineering/multi-agent-research-system)) ReAct showed that interleaving reasoning with retrieval reduces hallucination and error propagation compared with reasoning alone. ([Yao et al.](https://arxiv.org/abs/2210.03629))
- **Use perspectives to widen coverage.** STORM finds perspectives from related articles and then simulates perspective-guided Q&A with a grounded "expert". Compared with an outline-driven retrieval-augmented baseline, its articles were rated 25% (absolute) more often as organised and 10% more often as broad in coverage. ([STORM](https://arxiv.org/abs/2402.14207)) **[Synthesis]** For this project, perspectives could be stakeholder roles: member, front-desk staff, trainer, manager, owner, accountant, regulator.
- **Parallel tool calls.** Claude models run independent searches in parallel, and prompts can push this towards about 100%. ([Prompting best practices](https://platform.claude.com/docs/en/build-with-claude/prompt-engineering/claude-prompting-best-practices))

### 2.4 Source evaluation and citation discipline

- **Indicators of a problematic source.** The subagent prompt lists:
  - aggregators instead of original sources
  - false authority
  - passive voice paired with unnamed sources
  - qualifiers without specifics
  - unconfirmed reports
  - marketing language and spin
  - speculation written as fact, including "could/may" or future-tense claims
  - cherry-picked data

  It tells the agent to flag these issues rather than present the claims as established facts. ([research_subagent.md](https://github.com/anthropics/claude-cookbooks/blob/main/patterns/agents/prompts/research_subagent.md))
- **Content-farm bias** was found by human testers and fixed with source-quality heuristics. ([Anthropic](https://www.anthropic.com/engineering/multi-agent-research-system))
- **Human fact-checking practice (SIFT).** Mike Caulfield's four moves ([Caulfield, SIFT](https://hapgood.us/2019/06/19/sift-the-four-moves/)):
  1. Stop.
  2. Investigate the source.
  3. Find better coverage.
  4. Trace claims, quotes and media back to their original context.

  **[Synthesis]** Step 4 corresponds to "prefer primary sources." Step 3 corresponds to corroborating a claim in independent sources.
- **Grading evidence strength.** GRADE rates certainty as high, moderate, low or very low. It rates down for risk of bias, inconsistency, indirectness, imprecision and publication bias. ([Cochrane Handbook ch. 14](https://www.cochrane.org/authors/handbooks-and-manuals/handbook/current/chapter-14)) **[Synthesis]** A simplified version could be used per claim. GRADE was designed for clinical evidence, so applying it to software or domain research is an adaptation, not an established practice.
- **Citation discipline.** "ONLY add citations where the source documents directly support claims in the text." Cite complete claims, not fragments. ([citations_agent.md](https://github.com/anthropics/claude-cookbooks/blob/main/patterns/agents/prompts/citations_agent.md)) Anthropic's docs suggest making Claude's response auditable "by having it cite quotes and sources for each of its claims." ([Anthropic docs](https://platform.claude.com/docs/en/test-and-evaluate/strengthen-guardrails/reduce-hallucinations))

### 2.5 Hallucination and confirmation bias

- **How often citations fail.** Only 51.5% of sentences were fully supported by their citations, and citation precision was 74.5%. ([Liu et al.](https://arxiv.org/abs/2304.09848))
- **Deep research products still hallucinate.**
  - OpenAI's deep research scored 0.86 accuracy and a 0.13 hallucination rate on PersonQA, compared with 0.30 for GPT-4o. OpenAI notes the rate overstates the problem because some test data was out of date.
  - Red teamers saw the model's chain-of-thought hallucinate access to tools it did not have.
  - ([OpenAI system card PDF](https://cdn.openai.com/deep-research-system-card.pdf), section 3.3.6)
- **Expert feedback on STORM** flagged that bias on the Internet carries into articles, and that LLMs fabricate connections between unrelated facts. ([STORM](https://arxiv.org/abs/2402.14207)) **[Synthesis]** Inferences that connect sources should be labelled as inferences.
- **Position in long context.** Models use information at the start or end of a long input better than information in the middle. ([Lost in the Middle](https://arxiv.org/abs/2307.03172)) **[Synthesis]** Keep subagent returns short and put the key findings first.
- **Sycophancy and confirmation bias.** See 2.2. ([Sharma et al.](https://arxiv.org/abs/2310.13548)) Later 2026 preprints report that sycophancy rises with the user's expressed certainty and is higher for statements than for questions. **UNVERIFIED** (seen only in search snippets and not read in full): [arXiv 2602.23971](https://arxiv.org/abs/2602.23971).

### 2.6 Verification and fact-checking passes

| Technique | How it works | Source |
|---|---|---|
| Permission to say "I don't know" | Explicitly allow the model to admit uncertainty | [Anthropic docs](https://platform.claude.com/docs/en/test-and-evaluate/strengthen-guardrails/reduce-hallucinations) |
| Quote-first grounding | Extract word-for-word quotes before analysing | same |
| Post-hoc citation check | For each claim, find a supporting quote. If there is none, retract the claim | same |
| Best-of-N | Run the same prompt several times and treat inconsistencies as a warning sign | same |
| Chain-of-Verification | Draft → plan verification questions → answer them independently, so the draft does not bias the answers → revise | [Dhuliawala et al.](https://arxiv.org/abs/2309.11495) |
| SAFE | Split the text into atomic facts → search for each → judge support. Agreed with crowd annotators 72% of the time; was right in 76% of 100 sampled disagreements; more than 20x cheaper than humans | [Wei et al.](https://arxiv.org/abs/2403.18802) |
| Multi-agent debate | Several instances debate over rounds; improves factual validity | [Du et al.](https://arxiv.org/abs/2305.14325) |
| Dedicated citation agent | A separate pass maps claims to sources | [Anthropic](https://www.anthropic.com/engineering/multi-agent-research-system) |
| LLM-as-judge rubric | Scores factual accuracy, citation accuracy, completeness, source quality and tool efficiency. A single call returning 0.0–1.0 scores plus pass/fail matched human judgment best | [Anthropic](https://www.anthropic.com/engineering/multi-agent-research-system) |
| Human spot checks | Caught biases that automation missed, such as the content-farm preference | same |

Anthropic's docs note that these techniques reduce hallucinations but do not eliminate them, and advise: "Always validate critical information, especially for high-stakes decisions." ([Anthropic docs](https://platform.claude.com/docs/en/test-and-evaluate/strengthen-guardrails/reduce-hallucinations))

### 2.7 Conflicting sources

- **Anthropic's subagent rule.** Prioritise "based on recency, consistency with other facts, the quality of the sources." If facts cannot be reconciled, include the conflicting information in the report for the lead to resolve. ([research_subagent.md](https://github.com/anthropics/claude-cookbooks/blob/main/patterns/agents/prompts/research_subagent.md))
- **Gemini** explicitly looks for "discrepancies" to explore. ([Gemini](https://gemini.google/overview/deep-research/))
- **[Synthesis]** Under this project's no-guessing rule, a research agent should record a conflict (both claims, both sources, a possible explanation) rather than silently pick a winner. Resolving it becomes an open question or a separate, documented decision.

### 2.8 Synthesis

- **Separation of roles.** The lead's job is to "coordinate, guide, and synthesize" and to find gaps and deploy new subagents to fill them. ([research_lead_agent.md](https://github.com/anthropics/claude-cookbooks/blob/main/patterns/agents/prompts/research_lead_agent.md))
- **Gaps from missing searches.** Omissions caused by searches that were never run are hard to detect. ([Willison](https://simonwillison.net/2025/Feb/25/deep-research-system-card/)) **[Synthesis]** Ask each agent to list what it searched for and did not find, and what it did not search.
- **Contradictions between agents.** Subagents working on related parts can make conflicting implicit assumptions. ([Cognition](https://cognition.com/blog/dont-build-multi-agents)) **[Synthesis]** A cross-report consistency pass before planning docs are written addresses this.

### 2.9 When to stop

- **Budgets by difficulty.** The subagent prompt sets them as follows ([research_subagent.md](https://github.com/anthropics/claude-cookbooks/blob/main/patterns/agents/prompts/research_subagent.md)):
  - under 5 tool calls for simple tasks
  - about 5 for medium tasks
  - about 10 for hard tasks
  - up to 15 for very difficult tasks
  - a hard cap of 20 calls or about 100 sources
  - "when you are no longer finding new relevant information … STOP"
- **The lead prompt** says to stop "when you have reached the point where further research has diminishing returns." ([research_lead_agent.md](https://github.com/anthropics/claude-cookbooks/blob/main/patterns/agents/prompts/research_lead_agent.md))
- **Observed failures without such rules:** endless searching for sources that do not exist, and continuing after results were already sufficient. ([Anthropic](https://www.anthropic.com/engineering/multi-agent-research-system))
- **Applying those numbers here.** They were tuned for Anthropic's product, not for Claude Code subagents with WebSearch/WebFetch. Whether they transfer to this project is **UNVERIFIED**. **[Synthesis]** A testable stopping rule: stop when every key question is either answered by at least 2 independent sources (or 1 authoritative primary source), or marked as an evidence gap after a recorded set of search attempts.

### 2.10 Structuring outputs for later agents

- **Persist artifacts.** Subagents should store outputs externally and pass "lightweight references" back. Structured note-taking outside the context window keeps progress across resets. ([Anthropic](https://www.anthropic.com/engineering/multi-agent-research-system); [Context engineering](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents))
- **Use metadata.** File naming, folder location and timestamps help agents judge relevance when they retrieve context just in time. ([Context engineering](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents))
- **Keep returns short.** A condensed return of about 1,000–2,000 tokens. ([Context engineering](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents))
- **Report densely.** "Be detailed in your internal process, but more concise and information-dense in reporting." ([research_subagent.md](https://github.com/anthropics/claude-cookbooks/blob/main/patterns/agents/prompts/research_subagent.md))
- **[Synthesis]** Give each claim a stable ID (for example `PAY-007`) so planning docs such as `decisions.md` can cite research claims precisely, and verification agents can check them one by one in the SAFE style.

---

## 3. Reusable research agent brief template

**[Synthesis]** This template is built from the required elements in [research_lead_agent.md](https://github.com/anthropics/claude-cookbooks/blob/main/patterns/agents/prompts/research_lead_agent.md) and the techniques cited above. Fill in every `{{…}}` field and delete any that do not apply.

```markdown
## Context
Personal project: integrated software system for a fitness center to administer members and
employees. Research phase; no code yet. The project owner is new to this domain.
Your output feeds later planning docs (prd.md, design.md, testing.md, decisions.md).
Other agents are researching in parallel: {{list sibling topics}} — do NOT cover those.
Why this topic matters: {{1–2 sentences on what decision this research will inform}}

## Objective (one core objective)
{{e.g. "Map how fitness centers handle membership billing: models, payment methods, failure handling."}}

## Key questions (answer each explicitly, or mark as an evidence gap)
1. {{open question — do not presuppose an answer}}
2. ...

## Scope boundaries
- In scope: {{...}}
- Out of scope: {{...}}
- Geography / jurisdiction: {{...}}   Time window: prefer sources from {{year}} onward unless the topic is stable.

## Source requirements
- Prefer primary sources: {{official regulators, standards bodies, vendor docs for feature claims, peer-reviewed papers}}.
- Acceptable with care: {{industry associations, established trade press}} — label them as such.
- Avoid or flag: SEO content farms, aggregators, undated pages, marketing claims presented as fact, speculation.
- Trace claims back to their original source (SIFT). Do not cite an article that only repeats another source; cite the original.
- Fetch and read the page itself. Do not cite from search snippets.

## Method
1. Plan: list sub-questions and perspectives ({{e.g. member, front desk, trainer, manager, owner, accountant, regulator}}).
2. Search broad first (short queries), then narrow. Vary query wording; never repeat an identical query.
3. After each result, judge its quality and note what is still missing.
4. Look actively for evidence AGAINST emerging conclusions and for alternative options.
5. Budget: about {{10–20}} searches/fetches. Stop when every key question is answered by ≥2 independent
   sources (or 1 authoritative primary source), or recorded as a gap after the searches you list.
   Stop early if new searches stop producing new relevant information.

## Honesty rules
- No guesses. Every factual claim needs an inline URL to a page you actually read.
- If you cannot verify a claim, either drop it or mark it UNVERIFIED with the reason.
- Saying "not found" is an acceptable result. Fabricating a source is not.
- Label your own inferences as [Inference]. Never present an inference as a sourced fact.
- Where sources conflict, record both claims with their sources and a possible explanation. Do not pick one silently.
- You do NOT make project decisions. Present options with trade-offs and raise questions.

## Self-verification before finishing
For each claim: re-open the cited source and confirm it supports the claim as written (find the quote).
Remove or downgrade any claim that fails. Check that numbers, dates and names match the source exactly.

## Output
Write to: docs/research/{{topic-slug}}.md using the structure below. Then reply to the orchestrator in
≤{{300}} words: key findings, the file path, and the open questions verbatim.

### File structure
1. Header: topic, date, agent brief summary, scope, status ("research — no decisions").
2. Executive summary (≤10 bullets, most important first).
3. Findings — each claim on its own line with a stable ID and a confidence level:
   `[{{PREFIX}}-001] <claim>. Source: <URL>. Confidence: High/Medium/Low. (Primary/Secondary)`
4. Options and trade-offs (table) — where the topic implies choices.
5. Conflicts between sources (claim A vs claim B, sources, possible explanation).
6. Evidence gaps and UNVERIFIED items (with what you searched).
7. Open questions for the project owner — only questions whose answer would change what we do.
8. Sources (URL, title, publisher, date accessed, primary or secondary).
```

**Confidence levels.** These are a simplified adaptation of GRADE; the adaptation is **[Synthesis]**, not an established standard.

- **High:** an authoritative primary source, or 2 or more independent reliable sources that agree.
- **Medium:** one reliable secondary source, or primary sources that agree but only indirectly.
- **Low:** a weak or single non-authoritative source, or sources that partly conflict. Such claims should be checked before any decision relies on them.

---

## 4. Checklist for verifying research outputs

This checklist is for a separate verification agent or a human reviewer. It is **[Synthesis]**, drawn from CoVe, SAFE, Anthropic's citation and judge rubric, and SIFT as cited above.

**Structure**
- [ ] The file follows the template sections, and claims have IDs and confidence levels.
- [ ] Every key question in the brief is answered or listed as a gap.
- [ ] The output stays within scope and does not duplicate sibling topics.

**Citation support (do this per claim, by sampling or in full)**
- [ ] Each factual claim has a URL.
- [ ] The URL resolves, and the page actually supports the claim as worded. Re-fetch it and find the quote. If no quote supports the claim, flag or retract it.
- [ ] Numbers, dates, names and quotes match the source exactly.
- [ ] Claims are not traceable only to an aggregator, a content farm or marketing copy unless labelled as such.
- [ ] Speculative or forward-looking source language is not reported as fact.

**Honesty and bias**
- [ ] Inferences are labelled as inferences. No fabricated links between unrelated facts.
- [ ] UNVERIFIED items are marked, and nothing unverified is presented as fact.
- [ ] Evidence against the leading options was looked for and is reported.
- [ ] Conflicts are shown with both sides, not silently resolved.
- [ ] No project decisions are made. Options are given with trade-offs.

**Coverage**
- [ ] The agent searched from several perspectives (stakeholder roles, jurisdictions, vendor versus independent).
- [ ] Omissions are considered: what obvious subtopic is missing? (Omission is the hardest error to spot.)
- [ ] Sources are recent enough for the topic, and dates are recorded.

**Cross-report consistency (after all agents finish)**
- [ ] No contradictions between research files. If there are any, they are listed as conflicts.
- [ ] Shared terms (for example "member", "membership plan", "employee role") are used consistently.

**Open questions**
- [ ] Each question would change what we do if answered. Questions that are only curiosity have been removed.

---

## 5. Open questions for the project owner

1. **Budget versus rigour.** Multi-agent research uses about 15x the tokens of a chat. Is it acceptable to run a separate verification agent on every research file, or only on files that feed decisions? This decides between options A and B in section 2.1.
2. **Geography and jurisdiction.** Which country or countries will the fitness center operate in? Every domain brief (law, payments, data protection, employment rules) needs a jurisdiction in scope, or agents will mix rules from different jurisdictions.
3. **Source languages.** May agents use and cite non-English sources (for example local-language regulations), with translated quotes? This changes which primary sources they can use.
4. **Vendor material as evidence.** Can existing gym-management software vendors' own documentation count as evidence for "feature X exists in the market"? It is primary evidence of the feature but biased about its value.
5. **Human review.** Will you personally spot-check a sample of citations, and who resolves source conflicts and open questions: you, or the orchestrator with documented reasoning? The verification checklist and conflict handling depend on this.

---

## 6. Sources

**Anthropic**
- Anthropic Engineering, "How we built our multi-agent research system": https://www.anthropic.com/engineering/multi-agent-research-system
- Anthropic open-sourced research prompts (claude-cookbooks), all under https://github.com/anthropics/claude-cookbooks/blob/main/patterns/agents/prompts/:
  - research_lead_agent.md
  - research_subagent.md
  - citations_agent.md
- Anthropic, "Building effective agents": https://www.anthropic.com/engineering/building-effective-agents
- Anthropic, "Effective context engineering for AI agents": https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents
- Anthropic docs, "Reduce hallucinations": https://platform.claude.com/docs/en/test-and-evaluate/strengthen-guardrails/reduce-hallucinations
- Anthropic docs, "Prompting best practices": https://platform.claude.com/docs/en/build-with-claude/prompt-engineering/claude-prompting-best-practices
- Claude Code docs, "Subagents": https://code.claude.com/docs/en/sub-agents

**Other industry sources**
- OpenAI, Deep research system card (Feb 2025), PDF: https://cdn.openai.com/deep-research-system-card.pdf
- Google, Gemini Deep Research overview: https://gemini.google/overview/deep-research/
- Cognition (Walden Yan), "Don't Build Multi-Agents" (2025-06-12): https://cognition.com/blog/dont-build-multi-agents
- Simon Willison on the deep research system card: https://simonwillison.net/2025/Feb/25/deep-research-system-card/

**Papers**
- Shao et al., STORM (arXiv:2402.14207): https://arxiv.org/abs/2402.14207
- Liu, Zhang, Liang, "Evaluating Verifiability in Generative Search Engines" (arXiv:2304.09848): https://arxiv.org/abs/2304.09848
- Dhuliawala et al., "Chain-of-Verification Reduces Hallucination in LLMs" (arXiv:2309.11495): https://arxiv.org/abs/2309.11495
- Wei et al., "Long-form factuality in large language models" (SAFE) (arXiv:2403.18802): https://arxiv.org/abs/2403.18802
- Du et al., "Improving Factuality and Reasoning … through Multiagent Debate" (arXiv:2305.14325): https://arxiv.org/abs/2305.14325
- Yao et al., "ReAct" (arXiv:2210.03629): https://arxiv.org/abs/2210.03629
- Liu et al., "Lost in the Middle" (arXiv:2307.03172): https://arxiv.org/abs/2307.03172
- Sharma et al., "Towards Understanding Sycophancy in Language Models" (arXiv:2310.13548): https://arxiv.org/abs/2310.13548
- Cemri et al., "Why Do Multi-Agent LLM Systems Fail?" (MAST) (arXiv:2503.13657): https://arxiv.org/abs/2503.13657
- Du et al., "DeepResearch Bench" (arXiv:2506.11763): https://arxiv.org/abs/2506.11763
  - Covers 100 PhD-level tasks across 22 fields and reports evaluation methods with "strong alignment with human judgment."
  - The per-product citation-accuracy rankings come only from secondary summaries and are **UNVERIFIED**.

**Fact-checking and evidence grading**
- Mike Caulfield, "SIFT (The Four Moves)": https://hapgood.us/2019/06/19/sift-the-four-moves/
- Cochrane Handbook, Chapter 14 (GRADE certainty of evidence): https://www.cochrane.org/authors/handbooks-and-manuals/handbook/current/chapter-14
  - Only seen via search results (a localised URL); the page itself was not fetched, so the exact wording is **UNVERIFIED**.
