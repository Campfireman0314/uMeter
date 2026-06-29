# Tasks

## Task 1: Bump release metadata

**Requirement Trace:** [R1, R4] -> [U1]  
**Execution Mode:** [direct]

**Acceptance Criteria:**
- [x] .NET projects report `2.3.0`.
- [x] Setup constant reports `2.3.0`.
- [x] JUCE CMake projects report `2.3.0`.
- [x] Installer script defaults to `2.3.0`.

## Task 2: Update release documentation

**Requirement Trace:** [R2, R5] -> [U2]  
**Execution Mode:** [direct]

**Acceptance Criteria:**
- [x] README active version and packaging artifact point to `2.3.0`.
- [x] Changelog has a `v2.3.0` entry.
- [x] Current status describes the new release and known boundaries.
- [x] `docs/v2.3.0-release.md` exists.

## Task 3: Run convergence verification

**Requirement Trace:** [R3, R4, R5] -> [U3]  
**Execution Mode:** [direct]

**Acceptance Criteria:**
- [x] Managed unit tests pass.
- [x] Solution or app build passes.
- [x] JUCE sender tests pass.
- [x] Installer packaging is run or explicitly recorded as not run.
