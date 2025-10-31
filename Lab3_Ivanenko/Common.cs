using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace LabAllInOne
{
    // ===== ЛОГ =====
    static class Log
    {
        static readonly object _lock = new object();
        public static void Info(string m)
        {
            lock (_lock) Console.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + m);
        }
        public static void Err(string m)
        {
            lock (_lock)
            {
                var old = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] ERROR: " + m);
                Console.ForegroundColor = old;
            }
        }
    }

    // ===== МОДЕЛИ =====
    public enum Rarity { Common = 1, Rare = 2, Epic = 3, Legendary = 4 }
    public enum ItemKind { Weapon = 1, Armor = 2, Potion = 3, Misc = 4 }

    public struct ItemSize
    {
        public double Weight;  // >=0
        public double Length;  // >=0
        public int Slots;      // >=0
        public override string ToString()
        {
            return "[вес=" + Weight + ", длина=" + Length + ", слоты=" + Slots + "]";
        }
    }

    public class Crafter
    {
        public string Name = "Unknown";
        public string City;
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(City) ? Name : (Name + " (" + City + ")");
        }
    }

    public class GameItem : IComparable<GameItem>
    {
        public int Id;
        public string Name = "";
        public string Description;
        public Rarity Rarity = Rarity.Common;
        public ItemKind Kind = ItemKind.Misc;
        public ItemSize Size;
        public Crafter Maker = new Crafter();
        public int RequiredLevel;      // 1..60
        public double Price;           // >=0
        public int Durability;         // 0..100
        public DateTime CreatedAt;

        public int CompareTo(GameItem other) { return Price.CompareTo(other.Price); }
        public override string ToString()
        {
            return "#" + Id + ": " + Name + " (" + Kind + ", " + Rarity + ") | lvl " + RequiredLevel +
                   ", price " + Price + ", dur " + Durability + ", size " + Size + ", maker " + Maker;
        }
    }

    // ===== CSV =====
    static class Csv
    {
        public static string DataDir
        {
            get
            {
                string d = Path.Combine(AppContext.BaseDirectory, "data");
                Directory.CreateDirectory(d);
                return d;
            }
        }
        public static string DefaultFile { get { return Path.Combine(DataDir, "items.csv"); } }

        public static void Save(Dictionary<int, GameItem> dict, string path)
        {
            using (var sw = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                sw.WriteLine("Id;Name;Description;Rarity;Kind;Weight;Length;Slots;MakerName;MakerCity;RequiredLevel;Price;Durability;CreatedAt");
                foreach (var it in dict.Values)
                {
                    sw.WriteLine(string.Join(";", new[]
                    {
                        it.Id.ToString(),
                        Esc(it.Name),
                        Esc(it.Description),
                        it.Rarity.ToString(),
                        it.Kind.ToString(),
                        it.Size.Weight.ToString(CultureInfo.InvariantCulture),
                        it.Size.Length.ToString(CultureInfo.InvariantCulture),
                        it.Size.Slots.ToString(),
                        Esc(it.Maker!=null?it.Maker.Name:null),
                        Esc(it.Maker!=null?it.Maker.City:null),
                        it.RequiredLevel.ToString(),
                        it.Price.ToString(CultureInfo.InvariantCulture),
                        it.Durability.ToString(),
                        it.CreatedAt.ToString("o")
                    }));
                }
            }
        }

        public static Dictionary<int, GameItem> Load(string path, out int nextId)
        {
            var res = new Dictionary<int, GameItem>();
            nextId = 1;
            if (!File.Exists(path)) return res;

            using (var sr = new StreamReader(path, Encoding.UTF8))
            {
                string line = sr.ReadLine(); // header
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var cols = Split(line);
                    if (cols.Count < 14) continue;

                    var it = new GameItem();
                    int i = 0;
                    it.Id = PInt(cols[i++]);
                    it.Name = cols[i++];
                    it.Description = cols[i++];
                    it.Rarity = PEnum<Rarity>(cols[i++], Rarity.Common);
                    it.Kind = PEnum<ItemKind>(cols[i++], ItemKind.Misc);
                    it.Size = new ItemSize
                    {
                        Weight = PDouble(cols[i++]),
                        Length = PDouble(cols[i++]),
                        Slots = PInt(cols[i++])
                    };
                    it.Maker = new Crafter { Name = cols[i++], City = cols[i++] };
                    it.RequiredLevel = PInt(cols[i++]);
                    it.Price = PDouble(cols[i++]);
                    it.Durability = PInt(cols[i++]);
                    it.CreatedAt = PDate(cols[i++]);
                    res[it.Id] = it;
                }
            }
            if (res.Count > 0) nextId = res.Keys.Max() + 1;
            return res;
        }

        // helpers
        static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            bool q = s.IndexOf(';') >= 0 || s.IndexOf('"') >= 0 || s.IndexOf('\t') >= 0;
            s = s.Replace("\"", "\"\"");
            return q ? "\"" + s + "\"" : s;
        }
        static List<string> Split(string line)
        {
            var res = new List<string>();
            var sb = new StringBuilder();
            bool q = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (q)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                        else q = false;
                    }
                    else sb.Append(c);
                }
                else
                {
                    if (c == ';') { res.Add(sb.ToString()); sb.Length = 0; }
                    else if (c == '"') q = true;
                    else sb.Append(c);
                }
            }
            res.Add(sb.ToString());
            return res;
        }
        static int PInt(string s) { int v; return int.TryParse(s, out v) ? v : 0; }
        static double PDouble(string s) { double v; return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0.0; }
        static DateTime PDate(string s) { DateTime d; return DateTime.TryParse(s, null, DateTimeStyles.RoundtripKind, out d) ? d : DateTime.Now; }
        static T PEnum<T>(string s, T def) where T : struct { T v; return Enum.TryParse<T>(s, true, out v) ? v : def; }
    }

    // ===== АВТО-GIT =====
    static class GitAuto
    {
        public static void Publish(string filePath, string message)
        {
            try
            {
                string root = FindGitRoot(AppContext.BaseDirectory);
                if (root == null) { Log.Info("GitAuto: .git не найден (пропускаю)."); return; }

                string rel = MakeRelative(filePath, root);

                RunGit("add -f \"" + rel + "\"", root, true);

                string st = RunGit("status --porcelain \"" + rel + "\"", root, true).Trim();
                if (string.IsNullOrWhiteSpace(st))
                {
                    Log.Info("GitAuto: изменений нет (" + rel + ")");
                    return;
                }

                RunGit("commit -m \"" + message.Replace("\"", "'") + "\"", root, true);
                RunGit("push", root, true);

                Log.Info("GitAuto: отправлено " + rel);
            }
            catch (Exception ex)
            {
                Log.Err("GitAuto: " + ex.Message);
            }
        }

        static string FindGitRoot(string startDir)
        {
            var d = new DirectoryInfo(startDir);
            while (d != null)
            {
                if (Directory.Exists(Path.Combine(d.FullName, ".git")))
                    return d.FullName;
                d = d.Parent;
            }
            return null;
        }
        static string MakeRelative(string path, string root)
        {
            if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return path;
        }
        static string RunGit(string args, string workDir, bool ignoreExitCode)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo("git", args);
            p.StartInfo.WorkingDirectory = workDir;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (!ignoreExitCode && p.ExitCode != 0) Log.Info("git " + args + " => " + stderr.Trim());
            return string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
        }
    }
}
