using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;

namespace MoguangJianying;

public sealed class Plugin : IDalamudPlugin
{
    private const string Command = "/mjy";
    private readonly Configuration configuration;
    private readonly Typer typer = new();
    private readonly WindowSystem windows = new("MoguangJianying");
    private readonly MainWindow window;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        window = new MainWindow(configuration, typer);
        windows.AddWindow(window);

        Service.CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
        {
            HelpMessage = "打开魔光键影窗口。/mjy on 开始自动输入，/mjy off 停止。",
            ShowInHelp = true,
        });
        Service.Interface.UiBuilder.Draw += windows.Draw;
        Service.Interface.UiBuilder.OpenConfigUi += window.Toggle;
        Service.Framework.Update += OnUpdate;
    }

    public void Dispose()
    {
        Service.Framework.Update -= OnUpdate;
        Service.Interface.UiBuilder.Draw -= windows.Draw;
        Service.Interface.UiBuilder.OpenConfigUi -= window.Toggle;
        Service.CommandManager.RemoveHandler(Command);
        windows.RemoveAllWindows();
    }

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "on":
                configuration.AutoType = true;
                configuration.Save();
                Service.Chat.Print("魔光键影：自动输入已打开。");
                break;
            case "off":
                configuration.AutoType = false;
                configuration.Save();
                Service.Chat.Print("魔光键影：自动输入已关闭。");
                break;
            default:
                window.Toggle();
                break;
        }
    }

    private void OnUpdate(object framework)
    {
        var zone = ZoneReader.Read();
        var board = zone.IsKeybound ? KeyboundBoard.Read() : new KeyboundSnapshot(-1, 0, [], false);
        window.Publish(zone, board);
        typer.Tick(board.Entries, configuration.AutoType, zone.IsKeybound, board.TypingLocked, configuration);
    }
}
