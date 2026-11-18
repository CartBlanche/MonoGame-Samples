# Working with agents on a port (what worked on the TPS demo)

- One agent at a time when builds share a folder; ONE build at a time; Sonnet is enough for scoped passes.
- Brief = goal, metric with numeric target, exact repro recipe, files to read (not whole plans), hard caps (tool calls, build/render iterations, stall rule: no >=5 % gain in 3 iterations -> stop), reserved shot numbers (never overwrite milestone shots), "do not commit", docs to update (STATUS, LIGHTING-NOTES, ledger).
- Larger passes (shadows) get larger caps (100 calls / 10 iterations); changes of scope are sent as a message and the budget is raised explicitly.
- **The lead verifies every claim**: rebuild, re-run all self-tests, re-measure the metric, READ the PNGs next to the reference, check the "unchanged" surfaces (menu stats). Agent reports mixed verified facts with guesses; ask what it did not look at.
- The lead's own hypotheses can be wrong (depth precision vs collision proxies): bisect before delegating a fix.
- Keep a token/tool-call ledger row per agent; the harness may not give token counts.
- User screenshots with arrows are the best bug reports; reproduce them with `--sim tp:`.
- Commit after each verified step, single-line "[Project] ..." messages; the lead commits, not the agent.

## Lessons from lighting pass 6 and the contacts work (v0.4)
- The user's own gameplay screenshots/references can supersede earlier baselines; when new captures arrive (different engine version) re-baseline the brief, and say which surfaces keep the old check (menu shadows OFF stats) and which use the new reference (menu shadows ON).
- Ask the user for the position of each reference capture ("walked into the reactor after the robot guard", "ledge, looking left, reactor on the right, teal pillar on the left") and store it with the file in `Documentation/reference/README.md`.
- Order by user intent, not by convenience: bisect shader fault -> lighting pass -> gameplay parity -> optimisation last (optimising before gameplay work means re-measuring).
- Report caps honestly: count renders as well as batches; the lead re-runs tests, bench (3 runs, noise is about +-3 %) and reads at least the milestone PNGs.
- While one agent builds, the lead edits only docs/skill files, never code or the shared build folder.
