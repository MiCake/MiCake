# New Module Workflow

Multi-agent workflow for adding a new module to MiCake framework.

## Prerequisites

- Clear understanding of module purpose
- Identified target layer (Core/DDD/AspNetCore/EntityFrameworkCore)

## Workflow Steps

### Step 1: Architecture Design (architect)

Agent: `architect`  
Command: `design-module`

Input required:
- Module name
- Module purpose
- Target layer

Output:
- Module design document
- Dependency list
- Public API surface
- Internal structure plan

Proceed when: Design document approved

### Step 2: Implementation (developer)

Agent: `developer`  
Command: `implement-feature`

Input:
- Design document from Step 1

Tasks:
1. Create project file (.csproj)
2. Add to solution
3. Implement module class
4. Implement core types
5. Add extension methods
6. Configure lifecycle hooks

Output:
- Module implementation code
- Extension methods
- Service registrations

Proceed when: Code compiles, follows patterns

### Step 3: Testing (tester)

Agent: `tester`  
Command: `create-unit-tests`

Input:
- Implemented code from Step 2

Tasks:
1. Create test project
2. Write unit tests for public API
3. Add lifecycle hook tests
4. Verify edge cases

Output:
- Test project
- Unit tests with good coverage

Proceed when: All tests pass

### Step 4: Documentation (documenter)

Agent: `documenter`  
Commands: `write-xml-docs`, `write-readme`

Input:
- Implemented code from Step 2

Tasks:
1. Add XML documentation to all public APIs
2. Create module README
3. Add usage examples

Output:
- XML documented code
- README.md for module

Proceed when: Documentation complete

### Step 5: Review (reviewer)

Agent: `reviewer`  
Command: `review-code`

Input:
- All artifacts from previous steps

Tasks:
1. Review architecture compliance
2. Check coding standards
3. Verify test coverage
4. Validate documentation

Output:
- Review report
- Approval or change requests

Proceed when: Approved

## Completion Checklist

- [ ] Module design documented
- [ ] Code implemented following patterns
- [ ] Unit tests pass
- [ ] XML documentation complete
- [ ] README created
- [ ] Code review approved
- [ ] Added to CI pipeline

## Rollback

If any step fails:
1. Address feedback
2. Re-run from failed step
3. Do not proceed until step passes
