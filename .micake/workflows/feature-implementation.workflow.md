# Feature Implementation Workflow

Multi-agent workflow for implementing a new feature in MiCake framework.

## Prerequisites

- Feature requirements defined
- Target module identified

## Workflow Steps

### Step 1: Design Review (architect) - Optional

Agent: `architect`  
Command: `review-architecture`

Required when:
- New public API
- Cross-module changes
- Potential breaking changes

Input:
- Feature requirements
- Proposed implementation approach

Output:
- Design approval or modifications
- Architecture considerations

Skip when: Simple internal change

### Step 2: Implementation (developer)

Agent: `developer`  
Command: `implement-feature`

Input:
- Feature requirements
- Design notes (if Step 1 ran)

Tasks:
1. Identify affected files
2. Implement feature code
3. Add proper validation
4. Include XML documentation
5. Follow coding standards

Output:
- Feature implementation
- Updated/new classes

Proceed when: Code compiles, follows patterns

### Step 3: Testing (tester)

Agent: `tester`  
Command: `create-unit-tests`

Input:
- Implemented code from Step 2

Tasks:
1. Write tests for new functionality
2. Add edge case tests
3. Verify existing tests still pass
4. Check coverage

Output:
- Unit tests for new feature
- Updated existing tests if needed

Proceed when: All tests pass

### Step 4: Review (reviewer)

Agent: `reviewer`  
Command: `review-code`

Input:
- All changes from Steps 2-3

Tasks:
1. Check code quality
2. Verify patterns followed
3. Assess test coverage
4. Check documentation

Output:
- Review report

Proceed when: Approved

## Quick Flow (Minor Features)

For small, isolated changes:

```
developer → tester → reviewer
```

Skip architect unless public API changes.

## Completion Checklist

- [ ] Feature implemented
- [ ] Follows coding standards
- [ ] Unit tests added
- [ ] XML docs updated
- [ ] Code review passed

## Breaking Changes

If feature introduces breaking changes:

1. Document in CHANGELOG
2. Update version number
3. Add migration guide
4. Notify consumers
