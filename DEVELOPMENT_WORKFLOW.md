Correct_test1 Development Workflow
1. 项目开发模式

本项目采用：

Git Flow 简化版开发流程

结构：

master
│
├── feature/*
│
├── bugfix/*
│
└── hotfix/*

分支职责：

分支	用途	是否稳定
master	正式发布版本	✅ 稳定
feature/*	新功能开发	❌ 开发中
bugfix/*	问题修复	部分稳定
hotfix/*	紧急线上修复	✅
2. Master 分支规范
master 只保存稳定版本

禁止：

直接在 master 修改代码。

错误：

master

修改代码

commit

正确：

master

    ↓

创建feature分支

    ↓

开发

    ↓

测试

    ↓

Merge回master
3. 版本管理规范

项目使用：

Semantic Versioning

版本格式：

MAJOR.MINOR.PATCH

例如：

v1.2.0

含义：

1     2     0
│     │     │
│     │     └ 修复版本
│     └──── 功能增加
└──────── 大版本升级
版本升级规则
PATCH

Bug修复：

例如：

v1.2.0

↓

v1.2.1

例：

修复日志异常
修复按钮失效
MINOR

新增功能：

例如：

v1.2.0

↓

v1.3.0

例：

新增Logger
新增BOM读取
新增检查类型
MAJOR

架构重大变化：

例如：

v1.x

↓

v2.0

例：

重写核心架构
更换数据库
重构插件体系
4. 新功能开发流程
Step 1 创建需求

例如：

需求：

统一Marker体系

创建：

feature/marker-refactor

命令：

git switch master

git pull

git switch -c feature/marker-refactor
Step 2 开发

所有修改只在：

feature/marker-refactor

进行。

例如：

feature/marker-refactor

修改：

Markers/
 ├── MarkerBase.cs
 ├── RevisionMarker.cs
 └── TitleBlockMarker.cs
Step 3 Commit

Commit必须描述：

修改内容。

格式：

type: description

类型：

type	用途
feat	新增功能
fix	修复问题
refactor	重构
docs	文档
test	测试
style	格式
chore	工具配置

例如：

新增Logger：

git commit -m "feat: add unified logging system"

重构Marker：

git commit -m "refactor: unify marker architecture"

修改README：

git commit -m "docs: update project documentation"
5. Commit 原则
一个commit只做一件事情

错误：

feat:
add logger
modify marker
update README
fix batch bug

正确：

commit1:
feat: add logger


commit2:
refactor: unify marker


commit3:
docs: update README

原因：

方便：

回滚
查看历史
Code Review
6. Pull Request流程

开发完成：

feature

↓

Pull Request

↓

master

PR必须包含：

修改说明

例如：

Added AppLogger class.

Replaced:
- File.AppendAllText
- Debug.WriteLine

Affected:
- BatchCheckerManager
- TitleBlockChecker
测试结果

必须写：

Test:

[x] Build success

[x] AutoCAD loading success

[x] Single DWG check success

[x] Batch check success
7. Merge规范

推荐：

Squash Merge

原因：

保持master干净。

例如：

feature：

commit1
commit2
commit3
commit4

合并后：

master：

feat: marker refactor
8. Tag规范

Tag只用于正式版本。

禁止：

每次commit打tag

正确：

master

稳定

↓

tag

例如：

master

commit A

commit B

v1.2.0

commit C

commit D

v1.3.0

创建tag：

git tag -a v1.3.0 -m "Marker architecture refactor"

git push origin v1.3.0
9. Feature开发完成流程

完整流程：

1.

git switch master


2.

git pull


3.

git switch -c feature/xxx


4.

开发


5.

git add .


6.

git commit


7.

git push origin feature/xxx


8.

Create Pull Request


9.

Merge master


10.

git switch master


11.

git pull


12.

git tag


13.

git push tag
10. 项目目录规范

推荐：

Correct_test1

├── Command
│
├── Core
│
├── Readers
│
├── Parsers
│
├── Checks
│
├── Markers
│
├── Batch
│
├── Export
│
├── Models
│
├── Configs
│
├── Tests
│
├── Docs
│
└── README.md

11. AutoCAD二次开发特殊规范
所有数据库操作必须：

统一：

using(Transaction tr)
{

}

禁止：

事务外修改数据库。

所有新增Entity：

必须：

entity.SetDatabaseDefaults(db);
所有Layer：

必须：

LayerId

禁止：

entity.Layer="xxx";
所有日志：

禁止：

File.AppendAllText()

统一：

AppLogger
12. Code Review检查表

提交前：

功能
 功能实现
 原功能未破坏
AutoCAD API
 Transaction正确
 LayerId正确
 Entity初始化正确
Git
 分支正确
 Commit清晰
 无临时文件
安全
 无DWG文件
 无个人路径
 无密码
13. Correct_test1当前版本路线
v1.2.0

标题栏检查
图号检查
绿色标记
批量检查


        ↓


v1.3.0

工程化优化

├── AppLogger
├── Marker统一
└── 配置化


        ↓


v1.4.0

智能检查

├── BOM读取
├── 零件识别
└── 组件识别


        ↓


v2.0.0

智能CAD审核平台

14. 当前执行状态

当前：

master

v1.2.0

下一阶段：

创建：

feature/marker-refactor

目标：

v1.3.0