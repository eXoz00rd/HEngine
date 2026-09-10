using HEngine.Platform.Input;
using SilkKey = Silk.NET.Input.Key;

namespace HEngine.Platform.Windows.Input;

public static class SilkKeyMapper
{
    public static Key Map(SilkKey key) => key switch
    {
        SilkKey.A => Key.A,
        SilkKey.B => Key.B,
        SilkKey.C => Key.C,
        SilkKey.D => Key.D,
        SilkKey.E => Key.E,
        SilkKey.F => Key.F,
        SilkKey.G => Key.G,
        SilkKey.H => Key.H,
        SilkKey.I => Key.I,
        SilkKey.J => Key.J,
        SilkKey.K => Key.K,
        SilkKey.L => Key.L,
        SilkKey.M => Key.M,
        SilkKey.N => Key.N,
        SilkKey.O => Key.O,
        SilkKey.P => Key.P,
        SilkKey.Q => Key.Q,
        SilkKey.R => Key.R,
        SilkKey.S => Key.S,
        SilkKey.T => Key.T,
        SilkKey.U => Key.U,
        SilkKey.V => Key.V,
        SilkKey.W => Key.W,
        SilkKey.X => Key.X,
        SilkKey.Y => Key.Y,
        SilkKey.Z => Key.Z,

        SilkKey.Number0 => Key.Number0,
        SilkKey.Number1 => Key.Number1,
        SilkKey.Number2 => Key.Number2,
        SilkKey.Number3 => Key.Number3,
        SilkKey.Number4 => Key.Number4,
        SilkKey.Number5 => Key.Number5,
        SilkKey.Number6 => Key.Number6,
        SilkKey.Number7 => Key.Number7,
        SilkKey.Number8 => Key.Number8,
        SilkKey.Number9 => Key.Number9,

        SilkKey.F1 => Key.F1,
        SilkKey.F2 => Key.F2,
        SilkKey.F3 => Key.F3,
        SilkKey.F4 => Key.F4,
        SilkKey.F5 => Key.F5,
        SilkKey.F6 => Key.F6,
        SilkKey.F7 => Key.F7,
        SilkKey.F8 => Key.F8,
        SilkKey.F9 => Key.F9,
        SilkKey.F10 => Key.F10,
        SilkKey.F11 => Key.F11,
        SilkKey.F12 => Key.F12,

        SilkKey.Up => Key.Up,
        SilkKey.Down => Key.Down,
        SilkKey.Left => Key.Left,
        SilkKey.Right => Key.Right,

        SilkKey.Space => Key.Space,
        SilkKey.Enter => Key.Enter,
        SilkKey.Escape => Key.Escape,
        SilkKey.Tab => Key.Tab,
        SilkKey.Backspace => Key.Backspace,
        SilkKey.Delete => Key.Delete,
        SilkKey.Insert => Key.Insert,
        SilkKey.Home => Key.Home,
        SilkKey.End => Key.End,
        SilkKey.PageUp => Key.PageUp,
        SilkKey.PageDown => Key.PageDown,

        SilkKey.ShiftLeft => Key.ShiftLeft,
        SilkKey.ShiftRight => Key.ShiftRight,
        SilkKey.ControlLeft => Key.ControlLeft,
        SilkKey.ControlRight => Key.ControlRight,
        SilkKey.AltLeft => Key.AltLeft,
        SilkKey.AltRight => Key.AltRight,

        SilkKey.Comma => Key.Comma,
        SilkKey.Period => Key.Period,
        SilkKey.Semicolon => Key.Semicolon,
        SilkKey.Apostrophe => Key.Apostrophe,
        SilkKey.Minus => Key.Minus,
        SilkKey.Equal => Key.Equal,
        SilkKey.LeftBracket => Key.LeftBracket,
        SilkKey.RightBracket => Key.RightBracket,
        SilkKey.BackSlash => Key.Backslash,
        SilkKey.GraveAccent => Key.GraveAccent,

        _ => Key.Unknown
    };
}
