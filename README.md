# 轻记账

Windows 本地多币种记账软件，使用 SQLite 保存账目。

运行 build.ps1 编译，随后运行 bin\轻记账.exe。需要 Windows 10 / 11 和 .NET Framework 4.x。

数据库位于程序旁的 data\ledger.db，已从 Git 中排除。详细操作见 使用说明.txt。

## 界面语言

顶部下拉框支持中文、日本語、English，即时切换并保存选择到 `data/language.txt`。
币种选择、筛选、账目列表、统计和 CSV 导出都显示当前语言的币种名称，例如 `JPY 日元`、`JPY 日本円`、`JPY Japanese Yen`。
数据库继续使用原有币种代码和收支类型，兼容已有账本；自定义分类与备注保留原文，内置分类随语言显示。
切换语言保留当前筛选、未保存的金额和备注，以及正在编辑的账目。
本次只更新源码，现有 EXE 未重新编译或替换。运行 build.ps1 后才会生成含此功能的新程序。

## Google 日历

选择一条账目后，点击“添加到 Google 日历”。程序会在默认浏览器中打开 Google 日历的新事件确认页，并自动填写账目日期、收支类型、金额、带本地语言名称的币种、分类和备注。事件按全天事件创建；用户在浏览器中选择目标日历并确认保存。

该功能使用 Google Calendar 的事件模板链接，不在本地保存 Google 账号、密码、令牌或 API 密钥。界面和事件内容支持中文、日本語、English。现有 EXE 不会因源码修改而自动更新；运行 `build.ps1` 后才会生成包含此功能的新程序。

## 源码结构

- `LedgerForm.cs`：Windows 界面和记账交互
- `LedgerDb.cs`：SQLite 数据访问与完整备份
- `Localization.cs`：中文、日文和英文文本及币种名称
- `CurrencyItem.cs`：币种选择项模型
- `GoogleDriveBackup.cs`：Google Drive 同步文件夹的配置与 SQLite 备份

`build.ps1` 会自动编译仓库根目录下的全部 `.cs` 文件。

## Google Drive 备份

点击“备份到 Google Drive”。首次使用时选择 Google Drive 桌面版正在同步的文件夹；以后可以直接备份，也可以在确认窗口中选择“否”更换文件夹。程序使用 SQLite 完整备份生成带时间戳的 `.db` 文件，同一秒内重复备份会自动增加序号。

程序把备份写入本地同步文件夹，Google Drive 桌面版负责上传和显示同步状态。软件不保存 Google 账号、密码、OAuth 令牌或 API 密钥。所选文件夹保存在 `data/google-drive-folder.txt`，该设置不纳入 Git。
