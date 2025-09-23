---
applyTo: '**'
---
# Code Review Prompt for MiCake Project

## Context
You are reviewing code changes for the **MiCake** project, a modular .NET framework designed for building complex applications with a focus on clean architecture and domain-driven design.

## Project Overview
Please familiarize yourself with the project structure and conventions by referencing `.github/copilot-instructions.md` in the repository.

## Review Focus Areas

### 1. Architecture Compliance
- [ ] Does the code follow Clean Architecture principles?
- [ ] Are domain, application, and presentation layers properly separated?
- [ ] Is business logic correctly placed in the appropriate layer?
- [ ] Are dependencies flowing in the correct direction (inward)?

### 2. Code Quality & Best Practices
- [ ] Is comprehensive logging implemented for important operations?
- [ ] Are large files split into smaller, manageable components?
- [ ] Is the code following PascalCase naming conventions?
- [ ] Does the code exist the potential bugs?
- [ ] Are magic strings/numbers properly const-ified?

### 3. Performance & Optimization
- [ ] Are database queries optimized (proper indexing, N+1 prevention)?
- [ ] Is memory usage optimized for large datasets?
- [ ] Will the code logic cause the performance issue when the data volume is large?

### 4. Documentation & Maintainability
- [ ] Are TODO comments added for future improvements?
- [ ] Is the code following established patterns from similar components?

## Review Output Format

Please provide your review in the following structured format:

### ✅ **APPROVED** or ❌ **REQUIRES CHANGES**

### **Strengths**
- [List of positive aspects and well-implemented patterns]

### **Issues Found**

#### 🔴 **Critical Issues** (Must Fix)
1. **[Issue 1]**: [Description and impact]
   - **Location**: [File:Line or method name]
   - **Suggestion**: [How to fix]

#### 🟡 **Important Issues** (Should Fix)
1. **[Issue 1]**: [Description and impact]
   - **Location**: [File:Line or method name]
   - **Suggestion**: [How to fix]

#### 🟢 **Minor Issues** (Consider Fixing)
1. **[Issue 1]**: [Description and impact]
   - **Location**: [File:Line or method name]
   - **Suggestion**: [How to fix]

### **Security Review**
- [ ] No security concerns identified
- [ ] Security improvements suggested:
  - [List any security-related feedback]

### **Performance Review**
- [ ] No performance concerns identified
- [ ] Performance optimizations suggested:
  - [List any performance-related feedback]


### **Recommendations**
[Additional suggestions for improvement, best practices, or future considerations]