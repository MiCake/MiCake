# Bug Fix Workflow

Multi-agent workflow for fixing bugs in MiCake framework.

## Prerequisites

- Bug report with reproduction steps
- Identified affected module

## Workflow Steps

### Step 1: Analysis & Fix (developer)

Agent: `developer`  
Command: `fix-bug`

Input:
- Bug report
- Reproduction steps

Tasks:
1. Reproduce the bug
2. Identify root cause
3. Implement fix
4. Verify fix resolves issue

Output:
- Bug fix code
- Root cause analysis

Proceed when: Bug no longer reproducible

### Step 2: Regression Test (tester)

Agent: `tester`  
Command: `add-regression-test`

Input:
- Bug details
- Fix from Step 1

Tasks:
1. Write test that reproduces bug
2. Verify test fails without fix
3. Verify test passes with fix
4. Add to test suite

Output:
- Regression test

Proceed when: Regression test passes

### Step 3: Review (reviewer)

Agent: `reviewer`  
Command: `review-code`

Input:
- Fix from Step 1
- Test from Step 2

Tasks:
1. Verify fix addresses root cause
2. Check for side effects
3. Review test quality
4. Confirm patterns followed

Output:
- Review approval

Proceed when: Approved

## Quick Flow (Critical Bugs)

For production-critical bugs:

```
developer (fix + test) → reviewer
```

Combine fix and regression test in single step.

## Completion Checklist

- [ ] Bug reproduced and understood
- [ ] Root cause identified
- [ ] Fix implemented
- [ ] Regression test added
- [ ] Code review passed
- [ ] CHANGELOG updated

## Bug Categories

| Category | Priority | Response |
|----------|----------|----------|
| Security | Critical | Immediate fix, hotfix release |
| Data Loss | Critical | Immediate fix, hotfix release |
| Functionality | High | Fix in next release |
| Performance | Medium | Scheduled fix |
| Documentation | Low | Backlog |

## Post-Fix

1. Update CHANGELOG with fix description
2. Reference issue number in commit
3. Close issue after merge
4. Consider if similar bugs exist elsewhere
