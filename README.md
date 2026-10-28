# 魔光键影

进入用途为魔光键影的副本后，读取副本导演上的词条并输入。优先打断正在咏唱的敌人，其次是屏障类词条，其余打最近的敌人。每句之间默认间隔 2000 毫秒。禁止或沉默时暂停。

默认关闭。`/mjy` 打开窗口，`/mjy on` 开始，`/mjy off` 停止。

这个版本还没有实机确认。

## 卫月自定义仓库

在卫月的自定义插件仓库中添加：

```text
https://raw.githubusercontent.com/SumiRayki/MoguangJianying/main/pluginmaster.json
```

这是订阅文件本身，不是 GitHub 仓库页面。当前版本是测试版，需要在卫月里允许测试版插件后才能看到 **魔光键影**。订阅里只有这一个插件。

## 支持范围

- 国服客户端 `2026.10.06.0000.0000`
- Dalamud API 15
- 发布包由 GitHub Actions 按 `ottercorp/dalamud-distrib` 的 Dalamud `15.0.3.8`（git `ecd08fafd6092c51a011b23fe322442baaecf5ce`）编译

## 本地构建

需要 .NET 10 SDK，以及 Dalamud API 15 运行时。把 `DALAMUD_HOME` 指到该运行时目录（里面要有 `Dalamud.dll`）。不要把游戏目录或本机路径写进仓库。

```powershell
$env:DALAMUD_HOME = "<Dalamud Hooks\dev>"
dotnet build MoguangJianying.csproj -c Release
```

Release 构建会在 `bin/Release/MoguangJianying/latest.zip` 生成卫月安装包。
