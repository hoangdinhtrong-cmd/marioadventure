using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Media;

namespace MarioAdventure;

internal record ScoreEntry(string Name, int Score, int Level, DateTime PlayedAt);

internal static class ScoreStore
{
    private static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MarioAdventureWinForms");
    private static string FilePath => Path.Combine(Folder, "scores.tsv");
    public static string CleanName(string name) => string.IsNullOrWhiteSpace(name) ? "Player" : new string(name.Trim().Where(c => c != '\t' && c != '\r' && c != '\n' && !char.IsControl(c)).Take(20).ToArray());
    public static List<ScoreEntry> Read()
    {
        var scores = new List<ScoreEntry>();
        if (!File.Exists(FilePath)) return scores;
        foreach (string line in File.ReadLines(FilePath))
        {
            var p = line.Split('\t');
            if (p.Length == 4 && int.TryParse(p[1], out int score) && int.TryParse(p[2], out int level) && DateTime.TryParse(p[3], System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var when))
                scores.Add(new ScoreEntry(p[0], score, level, when));
        }
        return scores.OrderByDescending(x => x.Score).ThenBy(x => x.PlayedAt).Take(10).ToList();
    }
    public static void Save(ScoreEntry entry)
    {
        Directory.CreateDirectory(Folder);
        File.AppendAllText(FilePath, $"{CleanName(entry.Name)}\t{entry.Score}\t{entry.Level}\t{entry.PlayedAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture)}{Environment.NewLine}");
    }
    public static string Location => FilePath;
}

internal static class Audio
{
    [DllImport("winmm.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool PlaySound(string? sound, IntPtr module, uint flags);
    private const uint Async = 0x0001, FileName = 0x00020000, Loop = 0x0008, NoDefault = 0x0002;
    public static bool Enabled { get; private set; } = true;
    private static string Music => Path.Combine(AppContext.BaseDirectory, "Assets", "music.wav");
    public static void Toggle(bool playMusic = true) { Enabled = !Enabled; if (Enabled && playMusic) StartMusic(); else StopMusic(); }
    public static void StartMusic()
    {
        if (Enabled && File.Exists(Music)) PlaySound(Music, IntPtr.Zero, Async | FileName | Loop | NoDefault);
    }
    public static void StopMusic() => PlaySound(null, IntPtr.Zero, 0);
    public static void Effect(bool good)
    {
        if (!Enabled) return;
        // SystemSounds are short OS-provided effects; restart the background loop afterward.
        if (good) SystemSounds.Asterisk.Play(); else SystemSounds.Hand.Play();
    }
}
