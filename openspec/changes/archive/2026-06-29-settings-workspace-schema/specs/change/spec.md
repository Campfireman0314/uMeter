# Spec: 2026-06-29-settings-workspace-schema

正文默认使用中文；路径、命令、配置键名、frontmatter key 和状态枚举保留原文。

## 新增需求

### 需求：versioned workspace schema
**Trace**: R1
**Slice**: settings/schema-version

系统必须为用户 workspace/settings 持久化模型提供显式 schema version，并在保存时写出当前版本。

#### 场景：新设置保存
- **当** `UserWorkspaceSettings.Save()` 写出当前 settings JSON
- **那么** JSON 必须包含当前 schema version 字段
- **并且** theme、recent event、event output folder、source dictionary、enum 和 detail layout 字段必须仍按现有兼容格式保存

#### 场景：内存 settings 默认值
- **当** 代码创建新的 `UserWorkspaceSettings`
- **那么** schema version 必须规范化为当前版本

### 需求：legacy settings compatibility
**Trace**: R2
**Slice**: settings/legacy-compatibility

系统必须读取旧版 `settings.json`，并把缺失或旧字段规范化为当前 schema，不丢失已有 workspace 语义。

#### 场景：缺少 schema version 的旧文件
- **当** settings JSON 不包含 schema version
- **那么** load/normalize 必须把它视为 legacy 输入并升级到当前 schema
- **并且** source alias、hidden state、order index、analysis display mode、detail layout 和 loudness clock settings 必须保留

#### 场景：旧 collision threshold 字段
- **当** source workspace 只包含 `CollisionAbsoluteThresholdDb`
- **那么** normalize 必须把有效旧值迁移为 `CollisionLevelFloorDb`
- **并且** 保存后的当前 schema 不应继续保留旧字段值

#### 场景：无效或越界 workspace 值
- **当** legacy JSON 包含空 source key、空 alias、负 order index、越界 recent event limit 或非有限 loudness clock values
- **那么** normalize 必须按现有约束裁剪或清空这些值

### 需求：HostBridge settings DTO
**Trace**: R3
**Slice**: hostbridge/settings-dto

HostBridge v1 settings DTO 必须暴露 schema version 和稳定 workspace fields，供 Web UI 作为 settings/workspace consumption boundary。

#### 场景：snapshot settings projection
- **当** host snapshot 投影 workspace settings
- **那么** `HostSettingsSnapshot` 必须包含 schema version
- **并且** source settings projection 必须继续以 stable source key 作为 identity

#### 场景：WebView JSON envelope
- **当** HostBridge snapshot response 序列化为 JSON
- **那么** `snapshot.settings.schemaVersion` 必须以 camel-case JSON 字段出现
- **并且** 现有 settings fields 和 source workspace fields 必须保持可消费的 JSON shape

### 需求：TypeScript DTO alignment
**Trace**: R4
**Slice**: web/settings-types

Web frontend TypeScript DTO 和 mock snapshot 必须与 HostBridge settings JSON contract 对齐。

#### 场景：frontend bridge types
- **当** Web UI 编译 `HostSettingsSnapshotDto`
- **那么** TypeScript type 必须包含 `schemaVersion`
- **并且** existing mock transport 必须提供该字段，避免非 WebView 环境隐藏 DTO 漂移

#### 场景：frontend bridge tests
- **当** `npm test` 和 `npm run build` 执行
- **那么** HostBridge client/mock/settings DTO usage 必须通过测试和 TypeScript build

### 需求：change boundary guard
**Trace**: R5
**Slice**: scope/boundary

本 change 必须保持 settings schema/type/compatibility 边界，不得实现完整 settings UI 或改变无关 runtime/native/source ingest 行为。

#### 场景：实现范围
- **当** implementation 修改代码
- **那么** 修改必须限制在 settings schema、host/bridge DTO、frontend DTO/mock/tests、docs 和必要 WPF regression 适配内

#### 场景：后续能力
- **当** 发现需要完整 settings editor、source ingest migration、startup/tray 主窗口切换或 chart parity
- **那么** 必须停止扩大本 change，并转入对应后续 OpenSpec change

## 修改需求

无。

## 移除需求

无。
