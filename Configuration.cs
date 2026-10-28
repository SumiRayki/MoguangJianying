using Dalamud.Configuration;

namespace MoguangJianying;

public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool AutoType { get; set; }

    public bool OnlyInKeybound { get; set; } = true;

    public bool IncludeNonEnemies { get; set; }

    public int MinIntervalMs { get; set; } = 2000;

    public int MaxIntervalMs { get; set; } = 2000;

    public string BlockList { get; set; } = "解说员\n金碟点兑换员\n猫型魔法人偶\n瓦·札赞";

    public void Save() => Service.Interface.SavePluginConfig(this);
}
