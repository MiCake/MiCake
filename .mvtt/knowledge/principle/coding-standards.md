# Coding Standards

## Code Comments

- Prefer self-documenting code over explanatory comments.
- Never include decision numbers, requirement IDs, ADR references, or design-document section numbers in comments.
- Capture traceability in commit messages, not in source code.

### Examples

```
// ❌ BAD
// Implements idempotency via exists-or-skip semantics (ADR-06, §12.4)

// ✅ GOOD
function processEventIfNotDuplicate(event: Event) { ... }
```

```
// ❌ BAD
// Set the user's name to the input value
user.Name = input.Name;

// ✅ GOOD
user.Name = input.Name;
```
