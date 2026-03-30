# Repository Instructions for GitHub Copilot

## Auto Commit Policy

- When code changes are requested and completed, run build/tests to verify changes.
- If verification passes, create a commit in the same turn.
- Do not wait for explicit commit confirmation unless the user says not to commit.
- Commit each logical feature or fix separately — do not bundle unrelated changes into a single commit.
- Use focused staging for only the files relevant to each individual change.
- Do not commit when verification fails.
- If verification cannot be run because of environment constraints, report that and ask before committing.

## Commit Message Style

- Use concise imperative English commit messages.
- Summarize the user-requested outcome.

## Safety

- Never use destructive git commands.
- Never revert unrelated local changes.
- Respect explicit user override instructions in chat.