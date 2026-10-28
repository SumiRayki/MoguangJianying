namespace MoguangJianying;

internal sealed class Typer
{
    private const int MaxAttempts = 3;
    private readonly Dictionary<string, int> attempts = [];
    private readonly Random random = new();
    private string pending = string.Empty;
    private long nextAt;

    internal string Status { get; private set; } = "等待";

    internal string LastSubmitted { get; private set; } = string.Empty;

    internal void Tick(IReadOnlyList<SeenPrompt> prompts, bool enabled, bool inDuty, bool typingLocked, Configuration configuration)
    {
        var now = Environment.TickCount64;
        if (!enabled)
        {
            Status = "自动输入已关闭";
            return;
        }

        if (configuration.OnlyInKeybound && !inDuty)
        {
            Status = "不在魔光键影副本";
            return;
        }

        if (typingLocked)
        {
            Status = "禁止打字，已暂停";
            return;
        }

        if (!KeyboardInput.GameIsForeground())
        {
            Status = "游戏不在前台，已暂停";
            return;
        }

        if (now < nextAt)
            return;

        ForgetMissing(prompts);
        var next = PromptSelector.Choose(prompts, attempts, MaxAttempts);
        if (next == null)
        {
            Status = prompts.Count == 0 ? "没有词条" : "当前词条已发送过";
            return;
        }

        var prompt = next.Value;
        KeyboardInput.Type(prompt.Text, pressEnter: true);
        pending = prompt.Text;
        LastSubmitted = prompt.Text;
        var key = PromptSelector.Key(prompt);
        attempts[key] = attempts.GetValueOrDefault(key) + 1;
        var min = Math.Clamp(configuration.MinIntervalMs, 0, 5000);
        var max = Math.Clamp(Math.Max(configuration.MaxIntervalMs, min), min, 5000);
        nextAt = now + random.Next(min, max + 1);
        Status = $"已发送 {prompt.Text}";
    }

    private void ForgetMissing(IReadOnlyList<SeenPrompt> prompts)
    {
        var live = new HashSet<string>(prompts.Select(PromptSelector.Key));
        foreach (var key in attempts.Keys.ToArray())
        {
            if (!live.Contains(key))
                attempts.Remove(key);
        }

        if (pending.Length > 0 && prompts.All(prompt => prompt.Text != pending))
            pending = string.Empty;
    }
}
