# Work Packet: 2026-06-30-completion-audit

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保持原文。

## 元数据

- profile: strict-change
- source_type: feature
- status: drafting
- created: 2026-06-30

## 切分结论

### 总判断

- 结论：KEEP_ONE_CHANGE
- 理由：当前推荐实施序列已经全部归档，但总目标明确要求不要把单个模块完成当作 goal 完成，必须做 completion audit。这个 change 的交付单元是一个完整审计闭环：把最终态要求拆成可证伪检查项，读取当前仓库、codemap、OpenSpec、代码和验证命令，产出 completion verdict 与 remaining gap map。若审计发现缺口，只记录并路由到后续 implementation change，不在本 change 内混入产品修复。

## 意图

验证 uMeter 的 “WebView UI + C++ Core” 跨平台架构是否已经真实落地，并给出可追溯证据。若证据不足或发现未完成项，本 change 必须保持 goal active，并输出后续 change map，而不是缩小目标或宣布完成。

## 范围

### 范围内

- 逐条审计用户目标中的最终态要求和硬性流程要求。
- 读取当前仓库、归档 OpenSpec change、`.aiknowledge/codemap/**`、关键产品源码和测试命令输出。
- 区分 `proven`、`incomplete`、`weak evidence`、`contradicted` 和 `not applicable by scope`。
- 输出 completion verdict、remaining gaps、建议后续 top-level changes。
- 仅修改当前 OpenSpec change artifacts；如需知识沉淀，归档后由 knowledge follow-up 处理。

### 范围外

- 不实现新的产品功能。
- 不修复审计发现的 gap。
- 不修改 app/Web/native/JUCE/product docs，除非后续单独 implementation change 通过 plan/readiness。
- 不调用 `update_goal complete`，除非 audit 证据逐条证明所有最终态要求均满足。

## 成功标准

- audit matrix 覆盖所有显式最终态要求。
- 每个结论都引用当前状态证据，而不是历史记忆或推断。
- audit 能明确说明 goal 是否完成；若未完成，给出阻塞完成的 gap 和后续 change 建议。
- Windows 主路径仍通过当前可运行验证命令。
- OpenSpec 生命周期完整走完 readiness、implement、verify、review、archive。

## 需求 / 验收摘要

- [R1] 审计必须覆盖用户目标中的每条最终态要求。
- [R2] 审计结论必须基于当前仓库证据，并区分事实、推断和弱证据。
- [R3] 审计必须运行或引用当前可运行的验证命令，确认 Windows WebView 主路径没有因审计过程退化。
- [R4] 若目标未完成，审计必须输出 remaining gap map 和后续 change 建议。
- [R5] 审计必须保持 OpsX/OpenSpec 生命周期，不把 audit 本身当作 goal 完成。

## 方案摘要

采用“只读 completion audit + evidence ledger”方案。实现阶段读取归档证据和源码，运行 Web/.NET/native 可用验证命令，构建 requirements-to-evidence matrix。结论只允许由证据驱动：如果任何最终态要求缺乏强证据，则 verdict 为 `not_complete` 并列出后续 change。

## 文件 / Ownership

### 允许修改

- `openspec/changes/2026-06-30-completion-audit/**`

### 禁止修改

- `src/**`
- `tests/**`
- `docs/**`
- `.aiknowledge/**`
- 其他 `openspec/changes/**`

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `codex/2026-06-30-completion-audit`
- Base Branch: `codex/2026-06-30-macos-readiness`
- Merge Target: user-owned integration branch
- Workspace Rationale:
  - dirty 分类：创建 change 前工作区 clean；创建后 dirty 仅包含 `openspec/changes/2026-06-30-completion-audit/**`，归属本 change。
  - 链式判断：上一轮 `macos-readiness` 已归档提交；本 change 是其后的 completion audit，不改产品代码。
  - isolated 触发：本 change 只写当前 OpenSpec audit artifacts，不需要 isolated worktree。
  - 结论：`current-worktree`。

## 验证计划

- `opsx changes list`：预期显示本 change，阶段推进清晰。
- `rg` / `Get-Content` 读取归档 changes、codemap 和关键源码，形成 audit matrix。
- `npm test` and `npm run build` in `src/DesktopLoudnessMeter.Web`：预期通过。
- `dotnet test tests/DesktopLoudnessMeter.Tests/DesktopLoudnessMeter.Tests.csproj`：预期通过或记录 allowed failure。
- 可用时检查 native exports/build evidence；不可用工具必须记录为 weak/missing evidence，而不是 pass。
- `git status --short --branch`：预期 dirty 仅包含当前 change artifacts。

## 稳定化 / 人工平台不确定性

- 是否需要 `[stabilization-first]`：yes，audit 可能发现目标完成性不确定项。
- 不确定性来源：macOS 真机/toolchain、CMake、capture/package/signing 环境，以及“Host 只保留 glue / C++ Core 承担核心状态机”的完成边界可能需要代码级证据。
- 人工 / 平台覆盖：macOS runtime/capture/package 仍可能只能标记为 follow-up，除非当前环境已有真实证据。
- 最终硬化策略：用 audit matrix 收敛，不把缺证项当作通过。

## 回退 / 升级

- 回退方式：删除或修订当前 audit change，不影响产品代码。
- 升级条件：如果 audit 发现需要产品实现，结束本 change 后创建后续 implementation change；不在 audit 内直接改产品。

## Bugfix Root Cause

none（feature/audit）。

## Rejected Attempts

- 不直接宣布 goal complete：目标要求 completion audit，并且 codemap 已显示 macOS runtime 与 Web-side WKWebView adapter 仍是 follow-up。
- 不在 audit change 内修复发现的问题：会混淆审计与实施边界。
- 不只依赖归档 change 列表：必须读取当前源码和命令输出。

## Readiness Notes

- readiness 需检查 audit 范围覆盖所有最终态要求。
- readiness 需检查每个 task 都是可验证审计工作，而非泛泛总结。
