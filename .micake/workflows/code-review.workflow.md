# Code Review Workflow

Multi-agent workflow for reviewing pull requests and code changes.

## Prerequisites

- Code changes ready for review
- Tests passing in CI

## Workflow Steps

### Step 1: Automated Checks (reviewer)

Agent: `reviewer`  
Command: `check-patterns`

Input:
- Changed files

Tasks:
1. Verify coding patterns
2. Check naming conventions
3. Validate async patterns
4. Check dispose patterns

Output:
- Pattern compliance report

Proceed when: No critical violations

### Step 2: Architecture Review (architect) - Optional

Agent: `architect`  
Command: `review-architecture`

Required when:
- New public APIs
- Layer changes
- Module dependencies modified

Input:
- Changed files
- PR description

Output:
- Architecture assessment

Skip when: Internal changes only

### Step 3: Full Review (reviewer)

Agent: `reviewer`  
Command: `review-pr`

Input:
- All changed files
- Pattern report from Step 1
- Architecture notes from Step 2 (if applicable)

Tasks:
1. Review code quality
2. Check test coverage
3. Verify documentation
4. Assess performance
5. Check security

Output:
- Full review report
- Approval or change requests

Proceed when: Approved

### Step 4: Documentation Check (documenter) - Optional

Agent: `documenter`  
Command: `review-docs`

Required when:
- Public API changes
- New features

Input:
- Changed files

Output:
- Documentation assessment

## Review Criteria

### Must Have

- [ ] No critical issues
- [ ] Tests pass
- [ ] No architecture violations
- [ ] Patterns followed

### Should Have

- [ ] XML documentation
- [ ] Good test coverage
- [ ] Performance considered
- [ ] Clean code

### Nice to Have

- [ ] Optimizations
- [ ] Additional tests
- [ ] Enhanced documentation

## Review Output

```markdown
## PR Review: #{number}

### Summary
{Brief description}

### Verdict
[ ] APPROVED
[ ] CHANGES REQUESTED

### Issues Found
{List of issues by severity}

### Suggestions
{Improvement recommendations}
```

## Completion Checklist

- [ ] Automated checks pass
- [ ] Architecture reviewed (if needed)
- [ ] Full review complete
- [ ] Documentation reviewed (if needed)
- [ ] All issues addressed
- [ ] Final approval given
