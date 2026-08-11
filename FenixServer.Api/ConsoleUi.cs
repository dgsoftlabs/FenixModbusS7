using System;
using System.IO;
using System.Linq;

namespace FenixServer.Api
{
    /// <summary>
    /// Lightweight, colorful console UI helpers used by the FenixServer.Api console host.
    /// </summary>
    internal static class ConsoleUi
    {
        public static void Title(string text) => WriteLine(text, ConsoleColor.Cyan);
        public static void Info(string text) => WriteLine(text, ConsoleColor.Gray);
        public static void Ok(string text) => WriteLine(text, ConsoleColor.Green);
        public static void Warn(string text) => WriteLine(text, ConsoleColor.Yellow);
        public static void Error(string text) => WriteLine(text, ConsoleColor.Red, Console.Error);
        public static void App(string text) => WriteLine(text, ConsoleColor.DarkCyan);

        /// <summary>
        /// Writes a clickable (OSC 8 hyperlink) address line, e.g. "Open in browser: http://...".
        /// Renders as an underlined, clickable link in Windows Terminal and recent conhost.
        /// </summary>
        public static void Link(string label, string url)
        {
            var old = Console.ForegroundColor;
            try
            {
                Console.Write("  ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(label + " ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("\x1b[4m\x1b]8;;" + url + "\x1b\\" + url + "\x1b]8;;\x1b\\\x1b[0m");
                Console.WriteLine();
            }
            finally
            {
                Console.ForegroundColor = old;
            }
        }

        public static void Rule(char ch = '─', int width = 72) => Console.WriteLine(new string(ch, width));

        /// <summary>
        /// Draws a boxed, centered banner with a title and subtitle.
        /// </summary>
        public static void Banner(string title, string subtitle)
        {
            int width = Math.Max(title.Length, subtitle.Length) + 8;

            WriteLine("╔" + new string('═', width) + "╗", ConsoleColor.Cyan);
            WriteLine("║" + Center(title, width) + "║", ConsoleColor.Cyan);
            WriteLine("║" + Center(subtitle, width) + "║", ConsoleColor.Cyan);
            WriteLine("╚" + new string('═', width) + "╝", ConsoleColor.Cyan);
        }

        /// <summary>
        /// Draws a left-aligned info box from a set of lines.
        /// </summary>
        public static void Box(string[] lines)
        {
            if (lines == null || lines.Length == 0)
                return;

            int inner = lines.Max(l => l.Length);

            WriteLine("╔" + new string('═', inner + 4) + "╗", ConsoleColor.DarkCyan);
            foreach (var line in lines)
                WriteLine("║  " + line.PadRight(inner) + "  ║", ConsoleColor.DarkCyan);
            WriteLine("╚" + new string('═', inner + 4) + "╝", ConsoleColor.DarkCyan);
        }

        /// <summary>
        /// Writes a single timestamped line built from colored segments.
        /// </summary>
        public static void WriteLine(params (string Text, ConsoleColor Color)[] segments)
        {
            var old = Console.ForegroundColor;
            try
            {
                Console.Write("  " + DateTime.Now.ToString("HH:mm:ss") + " ");
                foreach (var (text, color) in segments)
                {
                    Console.ForegroundColor = color;
                    Console.Write(text);
                }
                Console.WriteLine();
            }
            finally
            {
                Console.ForegroundColor = old;
            }
        }

        private static void WriteLine(string text, ConsoleColor color) => WriteLine(text, color, Console.Out);

        private static void WriteLine(string text, ConsoleColor color, TextWriter writer)
        {
            var old = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                writer.WriteLine(text);
            }
            finally
            {
                Console.ForegroundColor = old;
            }
        }

        private static string Center(string text, int width)
        {
            int pad = (width - text.Length) / 2;
            return new string(' ', pad) + text + new string(' ', width - text.Length - pad);
        }
    }
}
