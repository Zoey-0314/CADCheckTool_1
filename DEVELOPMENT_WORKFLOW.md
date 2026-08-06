# Correct_test1 Development Workflow

> 本文档规定 Correct_test1 项目的分支管理、提交规范、Pull Request、版本发布、AutoCAD 二次开发约束以及代码审查要求。  
> 所有参与开发、维护、测试和发布的人员都应遵循本规范。

---

## 1. 项目开发模式

本项目采用**简化版 Git Flow**。日常开发不直接修改 `master`，而是从 `master` 创建独立分支，完成开发与测试后通过 Pull Request 合并。

```text
master
│
├── feature/*    新功能或较大范围重构
├── bugfix/*     常规问题修复
├── hotfix/*     已发布版本的紧急修复
└── docs/*       文档更新
```

### 1.1 分支职责

| 分支类型 | 用途 | 稳定性要求 | 示例 |
|---|---|---:|---|
| `master` | 保存已经合并并通过验证的主线代码 | 稳定 | `master` |
| `feature/*` | 新功能、架构调整、较大范围重构 | 开发中 | `feature/v1.4-exception-handler` |
| `bugfix/*` | 非紧急问题修复 | 合并前必须验证 | `bugfix/batch-save-error` |
| `hotfix/*` | 已发布版本的紧急修复 | 必须优先验证 | `hotfix/v1.3.1-dwg-save` |
| `docs/*` | README、架构说明、配置指南等文档维护 | 不修改业务逻辑 | `docs/update-project-docs` |

### 1.2 分支命名规则

分支名称使用小写英文和连字符，表达清楚本次工作的目标。

```text
feature/marker-refactor
feature/v1.4-exception-handler
bugfix/titleblock-empty-value
hotfix/v1.3.1-save-crash
docs/update-project-docs
```

不要使用含义不清的名称：

```text
test
new
temp
fix1
branch2
```

---

## 2. `master` 分支规范

`master` 只保存已经完成、经过检查并允许作为主线继续开发的代码。

### 2.1 禁止事项

禁止直接在 `master` 上进行以下操作：

- 修改业务代码；
- 添加未经验证的新功能；
- 直接提交临时调试代码；
- 提交测试 DWG、日志、编译输出或个人配置；
- 在没有 Pull Request 的情况下合入较大修改。

错误流程：

```text
master
  ↓
直接修改代码
  ↓
commit
  ↓
push
```

正确流程：

```text
master
  ↓
拉取最新代码
  ↓
创建独立分支
  ↓
开发或维护
  ↓
本地测试
  ↓
Push
  ↓
Pull Request
  ↓
Review / 验证
  ↓
Merge 到 master
```

### 2.2 开始工作前同步主线

```bash
git switch master
git pull origin master
```

确认本地没有未提交内容：

```bash
git status
```

正常状态应为：

```text
nothing to commit, working tree clean
```

---

## 3. 版本管理规范

项目使用 **Semantic Versioning（语义化版本）**。

版本格式：

```text
MAJOR.MINOR.PATCH
```

示例：

```text
v1.3.1
```

含义：

```text
1       3       1
│       │       │
│       │       └── PATCH：问题修复、小范围维护或兼容性补丁
│       └────────── MINOR：向下兼容的新功能或明显能力提升
└────────────────── MAJOR：存在重大架构或不兼容变化
```

### 3.1 PATCH

适用于不改变主要使用方式的小范围修复和维护。

```text
v1.3.0
  ↓
v1.3.1
```

示例：

- 修复日志异常；
- 修复按钮失效；
- 修复批处理保存错误；
- 调整配置参数；
- 补充必要的兼容性处理。

单纯文档更新通常不强制发布新版本；只有在项目需要对外形成正式发布记录时，才将其纳入 PATCH Release。

### 3.2 MINOR

适用于新增向下兼容的功能或完成一个明确的功能阶段。

```text
v1.3.1
  ↓
v1.4.0
```

示例：

- 新增统一异常处理；
- 新增新的检查类型；
- 新增 BOM 读取能力；
- 新增完整的规则配置机制。

### 3.3 MAJOR

适用于重大架构变化或存在不兼容升级。

```text
v1.x.x
  ↓
v2.0.0
```

示例：

- 重写核心检查架构；
- 更换主要数据存储方式；
- 重新设计插件加载与扩展机制；
- 原有配置或接口无法继续兼容。

---

## 4. 新功能开发流程

以下以 `feature/v1.4-exception-handler` 为例。

### Step 1：同步 `master`

```bash
git switch master
git pull origin master
```

### Step 2：创建开发分支

```bash
git switch -c feature/v1.4-exception-handler
```

### Step 3：确认当前分支

```bash
git branch --show-current
```

输出应为：

```text
feature/v1.4-exception-handler
```

### Step 4：进行开发

所有相关修改都在当前功能分支完成，不要中途切回 `master` 继续修改。

例如：

```text
Core/
├── ExceptionHelper.cs
└── TransactionHelper.cs
```

### Step 5：本地检查

```bash
git status
git diff
```

完成编译和 AutoCAD 功能验证后再提交。

### Step 6：暂存与提交

```bash
git add .
git commit -m "feat: add unified exception handling"
```

### Step 7：推送远程分支

第一次推送：

```bash
git push -u origin feature/v1.4-exception-handler
```

后续推送：

```bash
git push
```

### Step 8：创建 Pull Request

目标方向：

```text
feature/v1.4-exception-handler
                ↓
              master
```

---

## 5. Commit 规范

Commit 使用以下格式：

```text
type: description
```

### 5.1 Commit 类型

| Type | 用途 | 示例 |
|---|---|---|
| `feat` | 新增功能 | `feat: add revision table parser` |
| `fix` | 修复问题 | `fix: prevent invalid DWG overwrite` |
| `refactor` | 重构但不改变预期功能 | `refactor: unify marker architecture` |
| `docs` | 文档更新 | `docs: add configuration guide` |
| `test` | 测试相关 | `test: add title block test cases` |
| `style` | 代码格式，不影响逻辑 | `style: format marker classes` |
| `chore` | 构建、依赖、工程配置 | `chore: update project references` |

### 5.2 Commit 描述要求

Commit 应说明“做了什么”，不要只写：

```text
update
modify
fix
change code
```

推荐：

```text
feat: add unified logging system
refactor: centralize marker configuration
fix: handle empty title block drawing number
docs: update company-specific validation rules
```

### 5.3 一个 Commit 只处理一类事情

错误示例：

```text
feat: add logger, modify marker, update README and fix batch bug
```

正确拆分：

```text
feat: add unified logging system
refactor: unify marker architecture
fix: correct batch save handling
docs: update README
```

这样更方便：

- 回滚；
- 定位问题；
- 查看历史；
- Code Review；
- 生成版本说明。

### 5.4 提交前检查

```bash
git status
git diff
git diff --cached
```

确认没有误提交：

- `.vs/`
- `bin/`
- `obj/`
- 日志文件
- 测试 DWG
- 临时文件
- 本机绝对路径
- 密钥或密码

---

## 6. Pull Request 流程

开发完成后，通过 Pull Request 合并到 `master`。

### 6.1 PR 标题

PR 标题应与主要 Commit 风格一致。

示例：

```text
refactor: centralize marker configuration
docs: update project documentation
fix: prevent batch process from overwriting invalid DWG
```

### 6.2 PR 内容

PR 至少包含以下内容：

```markdown
## Summary

说明本次修改的目标和原因。

## Changes

- 新增了什么；
- 修改了什么；
- 删除了什么；
- 哪些模块受到影响。

## Affected Areas

- Core
- Markers
- Batch

## Test

- [x] Build success
- [x] AutoCAD plugin loading success
- [x] Single DWG check success
- [x] Batch DWG check success
- [x] Clear marker function success

## Risk

说明是否涉及：
- DWG 写入；
- 数据库事务；
- 图层；
- 批处理；
- 文件覆盖。
```

### 6.3 PR 中继续补充 Commit

PR 创建后发现遗漏，不需要关闭或重新创建 PR。

继续在原分支提交并推送：

```bash
git add .
git commit -m "fix: complete missing configuration comments"
git push
```

GitHub 上原有 PR 会自动更新。

---

## 7. Merge 规范

推荐使用 **Squash and merge**，把功能分支上的多个开发 Commit 合并为 `master` 上一个清晰的提交。

功能分支：

```text
commit 1
commit 2
commit 3
commit 4
```

Squash 合并后：

```text
master
└── refactor: centralize marker configuration
```

适用优点：

- 主线历史更清晰；
- 一个 PR 对应一个主线提交；
- 更容易回滚完整功能；
- 减少调试过程中的零散 Commit。

合并前必须确认：

- PR 目标分支为 `master`；
- 没有未解决冲突；
- 编译成功；
- 关键功能验证完成；
- Files changed 中没有无关文件。

---

## 8. 分支删除规范

已经合并且不再继续开发的 `feature/*`、`bugfix/*`、`hotfix/*` 和 `docs/*` 分支应及时删除。

### 8.1 删除远程分支

可在 GitHub PR 合并完成后点击：

```text
Delete branch
```

也可以使用：

```bash
git push origin --delete feature/marker-refactor
```

### 8.2 删除本地分支

先切换到 `master`：

```bash
git switch master
git pull origin master
```

安全删除：

```bash
git branch -d feature/marker-refactor
```

若 Git 提示分支未完全合并，不要立即使用 `-D` 强制删除，应先确认分支内容是否已经进入 `master`。

### 8.3 清理远程引用

```bash
git fetch --prune
```

---

## 9. Tag 与 Release 规范

### 9.1 Tag 只用于正式版本

不要为每个 Commit 或每个 PR 创建 Tag。

正确关系：

```text
开发分支
  ↓
Pull Request
  ↓
Merge 到 master
  ↓
回归测试
  ↓
创建 Tag
  ↓
创建 GitHub Release
```

### 9.2 创建带说明的 Tag

确认当前位于最新 `master`：

```bash
git switch master
git pull origin master
```

创建：

```bash
git tag -a v1.3.1 -m "Marker configuration and architecture refactor"
```

推送：

```bash
git push origin v1.3.1
```

### 9.3 Tag 与 Release 的区别

- **Tag**：标记某个 Git Commit 是一个版本节点；
- **Release**：GitHub 上基于 Tag 创建的发布页面，可包含版本说明和附件。

代码已经合并到 `master`，但未创建新 Tag 和 Release 时，GitHub 右侧仍可能显示旧版本为 `Latest`。这不会影响代码，只表示新的正式版本尚未发布。

### 9.4 Release 前检查

- `master` 已同步；
- 工作区干净；
- Release 模式编译成功；
- AutoCAD 插件加载正常；
- 单张检查通过；
- 批量检查通过；
- 标记生成和清除正常；
- DWG 保存安全机制验证通过；
- README 版本号和说明准确。

---

## 10. 完整开发与发布流程

```bash
# 1. 同步主线
git switch master
git pull origin master

# 2. 创建分支
git switch -c feature/xxx

# 3. 开发并检查
git status
git diff

# 4. 提交
git add .
git commit -m "feat: describe the change"

# 5. 推送
git push -u origin feature/xxx

# 6. 在 GitHub 创建 Pull Request
# 7. Review、测试并合并
# 8. 删除远程功能分支

# 9. 同步本地 master
git switch master
git pull origin master

# 10. 删除本地功能分支
git branch -d feature/xxx

# 11. 正式发布时创建 Tag
git tag -a vX.Y.Z -m "Release description"
git push origin vX.Y.Z
```

不是每个 PR 都必须立即创建 Tag。只有准备形成正式版本时才执行第 11 步。

---

## 11. 项目目录规范

当前推荐结构：

```text
Correct_test1
│
├── Command/                    AutoCAD 命令入口
├── Core/                       通用基础设施
├── Readers/                    DWG 数据读取
├── Checks/                     业务检查规则
├── Markers/                    CAD 标记生成
├── Batch/                      批量处理
├── Export/                     结果导出
├── Models/                     数据模型
├── Configs/                    可配置参数
│
├── README.md                   项目入口说明
├── ARCHITECTURE.md             架构说明
├── CONFIGURATION_GUIDE.md      企业规则与配置指南
├── DEVELOPMENT_WORKFLOW.md     开发与版本管理规范
└── Correct_test1.csproj
```

以下目录可在对应功能真正建立后再加入，不要仅为结构完整而创建空目录：

```text
Parsers/
Tests/
Docs/
```

---

## 12. AutoCAD 二次开发特殊规范

### 12.1 Transaction 规范

数据库对象的读取和修改必须在有效 Transaction 中完成。

```csharp
using (Transaction tr = db.TransactionManager.StartTransaction())
{
    // Read or modify database objects.

    tr.Commit();
}
```

禁止：

- 在事务外修改数据库对象；
- 事务提交后继续访问仅在该事务中打开的对象；
- 跨 Database 复用 `ObjectId`；
- SaveAs 前仍保留未释放事务。

### 12.2 Entity 初始化规范

新建 Entity 后，应设置数据库默认属性：

```csharp
entity.SetDatabaseDefaults(db);
```

然后再设置项目需要的图层、颜色、线型等属性。

### 12.3 Layer 规范

统一通过 `LayerId` 绑定图层。

推荐：

```csharp
entity.LayerId = layerId;
```

项目中应通过统一方法确保图层存在，例如：

```csharp
ObjectId layerId = EnsureLayer(db, tr, layerName);
```

不推荐在不同 Marker 中重复编写图层创建代码。

### 12.4 Marker 规范

Marker 类只负责：

- 创建检查框；
- 创建文字；
- 设置 Marker 图层与显示参数；
- 将实体加入目标 BlockTableRecord。

Marker 不负责：

- 解析标题栏；
- 判断图号规则；
- 执行批量目录遍历；
- 决定企业业务标准。

通用能力优先放入：

```text
MarkerBase
```

参数优先放入：

```text
Configs/
```

### 12.5 日志规范

禁止直接使用固定路径写日志：

```csharp
File.AppendAllText(@"D:\log.txt", message);
```

统一使用：

```csharp
AppLogger.Info(...);
AppLogger.Warn(...);
AppLogger.Error(...);
AppLogger.Exception(...);
```

日志中不得记录密码、密钥或不必要的敏感图纸内容。

### 12.6 DWG 保存规范

禁止在缺少保护的情况下直接覆盖原始 DWG。

应优先使用项目统一的安全保存机制：

```text
SafeDwgSaver
```

推荐流程：

```text
保存临时文件
  ↓
确认保存成功
  ↓
替换目标文件
  ↓
异常时保留原始文件
```

### 12.7 企业规则配置规范

以下内容可能因企业、项目或个人图纸模板不同而变化：

- 图号格式；
- 项目号格式；
- 文件名解析方式；
- 标题栏坐标；
- 修订栏区域；
- 标记尺寸；
- 图层名称；
- 文字高度；
- 判断容差。

这些规则应：

1. 优先集中到 `Configs/`；
2. 在规则代码附近添加“企业定制规则”注释；
3. 在 `CONFIGURATION_GUIDE.md` 中说明修改位置；
4. 避免散落的魔法数字和硬编码字符串。

示例：

```csharp
/*
 * Company-specific rule:
 *
 * Update this validation when the drawing-number format,
 * prefix, separator, suffix, or title-block template changes.
 */
```

---

## 13. Code Review 检查表

### 13.1 功能

- [ ] 本次功能或维护目标已经实现；
- [ ] 原有功能未被破坏；
- [ ] 空值、异常输入和不存在对象已处理；
- [ ] 单张检查验证通过；
- [ ] 批量检查验证通过；
- [ ] 标记生成和清除验证通过。

### 13.2 AutoCAD API

- [ ] Transaction 生命周期正确；
- [ ] `ObjectId` 属于当前 Database；
- [ ] 新 Entity 已调用 `SetDatabaseDefaults`；
- [ ] 图层通过统一逻辑创建；
- [ ] 使用正确的 `LayerId`；
- [ ] 没有事务外修改数据库；
- [ ] SaveAs 前资源已经释放。

### 13.3 架构

- [ ] Reader 只负责读取；
- [ ] Check 只负责规则判断；
- [ ] Marker 只负责绘制；
- [ ] 企业参数已放入 Config；
- [ ] 没有新增不必要的魔法数字；
- [ ] 没有重复实现已有公共功能。

### 13.4 Git

- [ ] 当前分支正确；
- [ ] Commit 信息清晰；
- [ ] 一个 Commit 只处理一类问题；
- [ ] PR 描述完整；
- [ ] 没有无关文件；
- [ ] 已合并分支准备删除。

### 13.5 安全

- [ ] 没有提交 DWG 文件；
- [ ] 没有提交日志文件；
- [ ] 没有提交个人绝对路径；
- [ ] 没有提交账号、密码或密钥；
- [ ] 不会在异常情况下破坏原始 DWG。

---

## 14. 当前版本路线

### v1.2.0：基础检查能力

已完成：

- 标题栏读取；
- 图号检查；
- 绿色错误标记；
- 批量检查基础流程。

```text
v1.2.0
   ↓
```

### v1.3.0：工程化与稳定性优化

主要内容：

- `AppLogger` 统一日志；
- Marker 公共能力整理；
- DWG 安全保存机制；
- 代码职责进一步拆分。

```text
v1.3.0
   ↓
```

### v1.3.1：配置与架构重构

已完成或已合入主线的主要内容：

- 新增并完善 `Configs/`；
- 集中管理 Marker 参数；
- 减少魔法数字和硬编码；
- 更新 Marker 和 Reader 的配置引用；
- 增加 `ARCHITECTURE.md`；
- 规范 Git 分支与 Pull Request 流程。

```text
v1.3.1
   ↓
```

### 文档维护补丁

当前文档维护内容：

- 更新 `README.md`；
- 更新 `ARCHITECTURE.md`；
- 新增 `CONFIGURATION_GUIDE.md`；
- 明确图号、项目号、标题栏和修订栏等企业定制规则。

建议分支：

```text
docs/update-project-docs
```

该文档 PR 不必单独创建 Tag；可并入下一次正式 Release，也可在确认 v1.3.1 稳定后一起发布。

```text
文档补丁
   ↓
```

### v1.4.0：稳定性与维护能力增强（计划）

建议内容：

- `ExceptionHelper`；
- `TransactionHelper`；
- 统一异常信息与错误上下文；
- 完善测试图纸和回归检查清单；
- 进一步降低 AutoCAD API 使用风险。

```text
v1.4.0
   ↓
```

### 后续版本：智能检查能力（规划）

在基础稳定性完成后，再逐步加入：

- BOM 读取；
- 零件和组件识别；
- 新检查规则；
- 更灵活的企业规则配置。

最终方向：

```text
v2.0.0
智能 CAD 审核与规则配置平台
```

以上后续内容属于规划，具体版本号应根据实际开发范围确定。

---

## 15. 当前执行状态

截至当前阶段：

```text
master
└── 已包含 v1.3.1 配置与架构重构相关代码
```

GitHub 上的 `Latest Release` 仍可能显示为 `v1.2.0`，原因是：

- 代码已合并到 `master`；
- 但尚未为新版本创建并推送 Tag；
- 尚未基于 Tag 创建新的 GitHub Release。

当前建议先完成：

```text
docs/update-project-docs
          ↓
Pull Request
          ↓
master
```

然后进行回归验证：

- Release 模式编译；
- AutoCAD 加载；
- 单张 DWG 检查；
- 批量检查；
- Marker 生成；
- Marker 清除；
- 安全保存。

验证通过后，可发布：

```text
v1.3.1
```

发布完成后，再从最新 `master` 创建下一阶段分支：

```bash
git switch master
git pull origin master
git switch -c feature/v1.4-exception-handler
```

---

## 16. 文档维护规则

以下情况必须同步更新文档：

| 变化 | 需要更新的文档 |
|---|---|
| 新增或删除核心模块 | `ARCHITECTURE.md` |
| 修改图号、项目号等规则 | `CONFIGURATION_GUIDE.md` |
| 修改安装、使用或主要功能 | `README.md` |
| 修改分支、Commit 或发布流程 | `DEVELOPMENT_WORKFLOW.md` |
| 修改版本号或正式发布 | README、Tag、Release Notes |

文档更新也应走独立分支和 Pull Request，不要长期只在本地维护。

---

## 17. 常用 Git 命令速查

### 查看状态

```bash
git status
git branch --show-current
git log --oneline --decorate -10
```

### 创建分支

```bash
git switch master
git pull origin master
git switch -c feature/xxx
```

### 提交与推送

```bash
git add .
git commit -m "type: description"
git push -u origin feature/xxx
```

### 合并后同步

```bash
git switch master
git pull origin master
git fetch --prune
```

### 删除本地已合并分支

```bash
git branch -d feature/xxx
```

### 创建正式版本 Tag

```bash
git tag -a vX.Y.Z -m "Release description"
git push origin vX.Y.Z
```
