using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace MoguangJianying;

internal sealed class MainWindow : Window
{
    private readonly Configuration configuration;
    private readonly Typer typer;
    private readonly object gate = new();
    private ZoneState zone;
    private KeyboundSnapshot board;

    public MainWindow(Configuration configuration, Typer typer) : base("魔光键影")
    {
        this.configuration = configuration;
        this.typer = typer;
        RespectCloseHotkey = true;
        SizeCondition = ImGuiCond.FirstUseEver;
        Size = new Vector2(560, 420);
    }

    public void Publish(ZoneState current, KeyboundSnapshot snapshot)
    {
        lock (gate)
        {
            zone = current;
            board = snapshot;
        }
    }

    public override void Draw()
    {
        ZoneState current;
        KeyboundSnapshot snapshot;
        lock (gate)
        {
            current = zone;
            snapshot = board;
        }

        var auto = configuration.AutoType;
        if (ImGui.Checkbox("自动输入", ref auto))
        {
            configuration.AutoType = auto;
            configuration.Save();
        }

        ImGui.SameLine();
        var onlyZone = configuration.OnlyInKeybound;
        if (ImGui.Checkbox("只在魔光键影副本", ref onlyZone))
        {
            configuration.OnlyInKeybound = onlyZone;
            configuration.Save();
        }

        var place = string.IsNullOrEmpty(current.PlaceName) ? "未知" : current.PlaceName;
        ImGui.Text($"区域：{place}    用途：{current.IntendedUse}    阶段：{snapshot.GamePhase}");
        ImGui.TextWrapped(typer.Status);
        if (typer.LastSubmitted.Length > 0)
            ImGui.Text($"上一句：{typer.LastSubmitted}");

        var min = configuration.MinIntervalMs;
        var max = configuration.MaxIntervalMs;
        if (ImGui.SliderInt("最短间隔（毫秒）", ref min, 0, 5000))
        {
            configuration.MinIntervalMs = min;
            configuration.Save();
        }

        if (ImGui.SliderInt("最长间隔（毫秒）", ref max, 0, 5000))
        {
            configuration.MaxIntervalMs = Math.Max(max, min);
            configuration.Save();
        }

        ImGui.TextWrapped("每发出一句后，会等待这两个间隔之间的随机时间再发下一句。默认都是 2000 毫秒。正在咏唱的敌人优先，类型 2 和带“屏障”的词条其次，其余打最近的敌人。");
        if (snapshot.Entries.Length == 0)
        {
            ImGui.TextDisabled("当前没有词条");
            return;
        }

        foreach (var prompt in snapshot.Entries)
        {
            var kind = prompt.Rank switch
            {
                0 => "打断",
                1 => "屏障",
                _ => "攻击",
            };
            ImGui.BulletText($"{kind}  类型{prompt.DisplayType}  {prompt.Distance:0}  {prompt.Text}");
        }
    }
}
