using System.Runtime.InteropServices;

namespace MoguangJianying;

internal static class KeyboardInput
{
    private const uint InputKeyboard = 1;
    private const uint KeyUp = 0x0002;
    private const uint Unicode = 0x0004;
    private const ushort Enter = 0x0D;

    internal static bool GameIsForeground()
    {
        var hwnd = Service.Interface.UiBuilder.WindowHandlePtr;
        return hwnd != 0 && GetForegroundWindow() == hwnd;
    }

    internal static void Type(string text, bool pressEnter)
    {
        if (text.Length == 0 && !pressEnter)
            return;

        var hwnd = Service.Interface.UiBuilder.WindowHandlePtr;
        var previousIme = hwnd == 0 ? 0 : ImmAssociateContext(hwnd, 0);
        try
        {
            var inputs = new INPUT[text.Length * 2 + (pressEnter ? 2 : 0)];
            var index = 0;
            foreach (var character in text)
            {
                inputs[index++] = UnicodeKey(character, down: true);
                inputs[index++] = UnicodeKey(character, down: false);
            }

            if (pressEnter)
            {
                inputs[index++] = VirtualKey(Enter, down: true);
                inputs[index++] = VirtualKey(Enter, down: false);
            }

            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
            if (sent != inputs.Length)
                Service.Log.Warning($"SendInput sent {sent} of {inputs.Length}.");
        }
        finally
        {
            if (hwnd != 0)
                ImmAssociateContext(hwnd, previousIme);
        }
    }

    private static INPUT UnicodeKey(char character, bool down) => new()
    {
        Type = InputKeyboard,
        Scan = character,
        Flags = down ? Unicode : Unicode | KeyUp,
    };

    private static INPUT VirtualKey(ushort key, bool down) => new()
    {
        Type = InputKeyboard,
        VirtualKey = key,
        Flags = down ? 0 : KeyUp,
    };

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, INPUT[] inputs, int size);

    [DllImport("imm32.dll")]
    private static extern nint ImmAssociateContext(nint window, nint context);

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct INPUT
    {
        [FieldOffset(0)] public uint Type;
        [FieldOffset(8)] public ushort VirtualKey;
        [FieldOffset(10)] public ushort Scan;
        [FieldOffset(12)] public uint Flags;
        [FieldOffset(16)] public uint Time;
        [FieldOffset(24)] public nint Extra;
    }
}
