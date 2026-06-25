# Agent Verification & Self-Review — Zoo World

> The standing discipline against an AI assistant's two worst failure modes: **a flawed first
> attempt presented as final**, and the **confident false claim** ("done", "passed", "not the
> cause") stated without checking. (Portable template — applies as-is.)

## 0. Assume your first attempt is probably wrong — self-review before you present

A first pass at anything non-trivial almost always carries an error/omission/over-reach. Treat your
own output as a draft to audit, not a finished answer:

> **produce → adversarially self-review → cross-check against reality → fix → re-verify → only then present.**

- **Audit as a skeptic would** — assume a bug/inconsistency is in there and go find it (§3), instead
  of confirming it looks fine.
- **Check it both ways:** does it do what was intended **and** not break anything else? Edge cases,
  cross-references, numbers, names — did the change create a *new* inconsistency somewhere you didn't look?
- **Cross-check against the real thing, not memory/docs:** read the Unity **Console** (compile/errors),
  **run the tests**, read the actual **editor/scene/git** state (via MCP). Reality outranks every doc.
- **Be adequate:** right-sized (not over-engineered), no invented facts, solves the real need.
- **Iterate to clean.** Stopping at "first attempt done" is the single most common failure — don't.

## 1. Sources of truth — precedence (higher wins)

1. **Reality** — test output, the compiler/Console, git/the filesystem, a real run. Beats every doc + memory.
2. **Task brief Status** (`docs/tasks/Tnn-*.md`). 3. **Task matrix** (`docs/tasks/README.md`).
4. **`docs/handoff/current-status.md`**. 5. **Memory / chat history** — lowest; never the sole basis for a claim.

## 2. Before claiming "done / passed / blocked / not the cause"

- Read the actual file / Status row, not your memory of it.
- **done/passed:** cite evidence from **this session** (the test run, the Console read, the diff).
  *Never claim a test passed unless it ran this session.*
- **"X is the bug / X isn't done":** read the real artifact (the diff content, the log line) — not a guess.

## 3. Contradiction-seeking step (before every non-trivial claim)

1. State your hypothesis in one line. 2. Ask **"what would have to be true for the opposite to hold?"**
3. Verify against that opposite *before* you assert. Red flags you're biased: the claim rests on an
**absence** (a missing file/function — easy to misjudge); you spent < 1 minute; it contradicts what
the user just told you.

## 4. When to drop to git / build / tests

Trust the brief Status by default; verify against reality when: Status is ✅ but another section
mentions deferred/partial work; acceptance criteria name files you can't find; two docs disagree;
you're about to contradict the user or a recent commit. Read the **content** (the real diff, the real
test output), not a file count.

## 5. Keep the docs consistent (status transitions)

A status change rides with the work — no status-only commits. On close, in the same pass: the brief
Status (+ *"what was actually done"*), the matrix row, and `current-status.md`. If two locations
disagree, the most recent change wins and the drift is fixed the same session.

## 6. When in doubt — ask, don't invent

If you still can't tell, **say so and ask**, quoting what you found and where it conflicts. Honest
*"I'm not sure, let's verify X"* beats a confident wrong answer — confident-wrong is the single
biggest source of lost trust.
