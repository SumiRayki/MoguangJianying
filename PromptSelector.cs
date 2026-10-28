namespace MoguangJianying;

internal readonly record struct SeenPrompt(
    string Text,
    float Distance,
    ulong ObjectId,
    string Source,
    int Rank,
    byte DisplayType);

internal static class PromptSelector
{
    internal static string Key(SeenPrompt prompt) => $"{prompt.ObjectId:X}\n{prompt.Text}";

    internal static SeenPrompt? Choose(IReadOnlyList<SeenPrompt> prompts, IReadOnlyDictionary<string, int> failures, int maxFailures)
    {
        SeenPrompt? best = null;
        foreach (var prompt in prompts)
        {
            if (failures.TryGetValue(Key(prompt), out var count) && count >= maxFailures)
                continue;
            if (best == null
                || prompt.Rank < best.Value.Rank
                || (prompt.Rank == best.Value.Rank && prompt.Distance < best.Value.Distance))
                best = prompt;
        }

        return best;
    }
}
