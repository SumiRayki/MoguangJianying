using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.System.String;

namespace MoguangJianying;

internal readonly record struct KeyboundSnapshot(int GamePhase, uint TailFlag, SeenPrompt[] Entries, bool TypingLocked);

internal static unsafe class KeyboundBoard
{
    private const int EntryOffset = 8088;
    private const int EntryCount = 5;
    private const int TailFlagOffset = 8172;
    private const int GamePhaseOffset = 8176;

    [StructLayout(LayoutKind.Explicit, Size = 12)]
    private struct ContentTextEntry
    {
        [FieldOffset(0)] public ushort TextRowId;
        [FieldOffset(2)] public ushort TextArgument;
        [FieldOffset(4)] public byte DisplayType;
        [FieldOffset(5)] public byte ConditionMask;
        [FieldOffset(6)] public byte ConditionIndex;
        [FieldOffset(7)] public byte ConditionValue;
        [FieldOffset(8)] public byte SecondaryConditionIndex;
        [FieldOffset(9)] public byte TextSource;
        [FieldOffset(10)] public ushort Flags;
    }

    internal static KeyboundSnapshot Read()
    {
        var director = FindInstanceDirector();
        if (director == null)
            return new KeyboundSnapshot(-1, 0, [], TypingLocked());

        var raw = (byte*)director;
        var phase = *(int*)(raw + GamePhaseOffset);
        var tail = *(uint*)(raw + TailFlagOffset);
        var entries = new List<SeenPrompt>(EntryCount);
        var enemies = ReadEnemies();
        var source = (ContentTextEntry*)(raw + EntryOffset);
        for (var i = 0; i < EntryCount; i++)
        {
            var entry = source[i];
            if (entry.TextRowId == 0)
                continue;
            var text = ReadText(director, entry.TextRowId);
            if (text.Length == 0)
                continue;

            var enemy = enemies.FirstOrDefault(candidate => candidate.Name.Equals(text, StringComparison.Ordinal));
            var matched = enemy.Name.Length > 0;
            var distance = matched ? enemy.Distance : 1000f + i;
            var rank = enemy.Casting ? 0 : entry.DisplayType == 2 || text.Contains("屏障", StringComparison.Ordinal) ? 1 : 2;
            entries.Add(new SeenPrompt(text, distance, entry.TextRowId, matched ? "敌人" : $"词条{i + 1}", rank, entry.DisplayType));
        }

        return new KeyboundSnapshot(phase, tail, entries.ToArray(), TypingLocked());
    }

    private static string ReadText(InstanceContentDirector* director, ushort rowId)
    {
            var utf = director->GetInstanceContentText(rowId);
        if (utf == null)
            return string.Empty;
        var text = utf->ToString();
        return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
    }

    private static InstanceContentDirector* FindInstanceDirector()
    {
        var framework = EventFramework.Instance();
        if (framework == null)
            return null;

        foreach (var entry in framework->DirectorModule.DirectorList)
        {
            var director = entry.Value;
            if (director == null || director->Info.EventId.ContentId != EventHandlerContent.InstanceContentDirector)
                continue;
            return (InstanceContentDirector*)director;
        }

        return null;
    }

    private readonly record struct Enemy(string Name, float Distance, bool Casting);

    private static List<Enemy> ReadEnemies()
    {
        var enemies = new List<Enemy>();
        var player = Service.Objects.LocalPlayer;
        if (player == null)
            return enemies;

        foreach (var obj in Service.Objects)
        {
            if (obj is not IBattleNpc npc || npc.IsDead || !npc.IsTargetable)
                continue;
            if (npc.BattleNpcKind != BattleNpcSubKind.Combatant)
                continue;
            var name = npc.Name.TextValue.Trim();
            if (name.Length == 0)
                continue;
            var casting = npc is IBattleChara chara && chara.IsCasting;
            enemies.Add(new Enemy(name, Vector3.Distance(player.Position, npc.Position), casting));
        }

        return enemies;
    }

    private static bool TypingLocked()
    {
        var player = Service.Objects.LocalPlayer;
        if (player == null)
            return false;
        foreach (var status in player.StatusList)
        {
            if (status is not { StatusId: > 0 })
                continue;
            var name = status.GameData.Value.Name.ExtractText();
            if (name.Contains("禁止", StringComparison.Ordinal) || name.Contains("沉默", StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
