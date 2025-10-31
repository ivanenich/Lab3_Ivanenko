using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Globalization;

namespace LabAllInOne
{
    // ===================== ЛР1 — простая симуляция =====================
    static class Lab1Module
    {
        public static void Run()
        {
            Console.WriteLine("\n== ЛР1: мини-сценарий ==");
            // персонаж
            var rnd = new Random();
            int hp = 10 + rnd.Next(1, 6);
            int atk = 2 + rnd.Next(0, 3);
            Console.WriteLine($"Игрок: HP {hp}, ATK {atk}");

            // лут (3 предмета)
            var items = new List<GameItem>();
            for (int i = 0; i < 3; i++)
            {
                items.Add(new GameItem
                {
                    Id = i + 1,
                    Name = "Item_" + (i + 1),
                    Price = Math.Round(rnd.NextDouble() * 50, 2),
                    Rarity = (Rarity)rnd.Next(1, 5),
                    Kind = (ItemKind)rnd.Next(1, 5),
                    RequiredLevel = rnd.Next(1, 10),
                    Durability = rnd.Next(50, 101),
                    Size = new ItemSize { Weight = Math.Round(rnd.NextDouble() * 3, 2), Length = Math.Round(rnd.NextDouble() * 2, 2), Slots = rnd.Next(0, 3) },
                    Maker = new Crafter { Name = "NPC" + rnd.Next(1, 4), City = "Town" + rnd.Next(1, 3) },
                    CreatedAt = DateTime.Now
                });
            }

            Console.WriteLine("Лут:");
            foreach (var it in items) Console.WriteLine("  " + it);

            // выбор лучшего по цене/весу
            var best = items.OrderBy(x => x.Price / Math.Max(0.1, x.Size.Weight)).First();
            Console.WriteLine("\nЛучший по цене/весу: " + best.Name);
            Console.WriteLine("Сценарий пройден ;)");
        }
    }

    // ===================== ЛР2 — менеджер коллекции (CSV) =====================
    static class Lab2Module
    {
        static Dictionary<int, GameItem> _items;
        static int _nextId;
        static string _path;

        public static void Run()
        {
            Console.WriteLine("\n== ЛР2: коллекция (Dictionary<int,GameItem>) ==");
            _path = Csv.DefaultFile;
            _items = Csv.Load(_path, out _nextId);
            Log.Info("Файл: " + _path + ", элементов: " + _items.Count);

            Console.WriteLine("Команды: help, info, show, insert, update <id>, remove_key <id>, clear, save, load,");
            Console.WriteLine("         filter_kind, group_by_rarity, count, exit");

            while (true)
            {
                Console.Write("\n> ");
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                var cmd = parts[0].ToLowerInvariant();
                var arg = parts.Length > 1 ? parts[1].Trim() : "";

                try
                {
                    if (cmd == "help") PrintHelp();
                    else if (cmd == "info") Info();
                    else if (cmd == "show") Show();
                    else if (cmd == "insert") Insert();
                    else if (cmd == "update") UpdateCmd(arg);
                    else if (cmd == "remove_key") RemoveCmd(arg);
                    else if (cmd == "clear") { _items.Clear(); Console.WriteLine("Ок."); }
                    else if (cmd == "save") { Csv.Save(_items, _path); Console.WriteLine("Сохранено."); }
                    else if (cmd == "load") { _items = Csv.Load(_path, out _nextId); Console.WriteLine("Перезагружено."); }
                    else if (cmd == "filter_kind") FilterKind();
                    else if (cmd == "group_by_rarity") GroupByRarity();
                    else if (cmd == "count") Console.WriteLine(_items.Count);
                    else if (cmd == "exit") return;
                    else Console.WriteLine("Неизвестная команда.");
                }
                catch (Exception ex) { Console.WriteLine("[ОШИБКА] " + ex.Message); }
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("help, info, show, insert, update <id>, remove_key <id>, clear, save, load,");
            Console.WriteLine("filter_kind, group_by_rarity, count, exit");
        }
        static void Info()
        {
            Console.WriteLine("Тип: Dictionary<int,GameItem>");
            Console.WriteLine("Файл: " + _path);
            Console.WriteLine("Элементов: " + _items.Count);
        }
        static void Show()
        {
            if (_items.Count == 0) { Console.WriteLine("Пусто."); return; }
            foreach (var x in _items.Values.OrderBy(v => v.Id)) Console.WriteLine(x);
        }

        static void Insert()
        {
            var it = ReadItem();
            it.Id = _nextId++;
            it.CreatedAt = DateTime.Now;
            _items[it.Id] = it;
            Console.WriteLine("Добавлено id=" + it.Id);
        }
        static void UpdateCmd(string arg)
        {
            int id; if (!int.TryParse(arg, out id)) { Console.WriteLine("update <id>"); return; }
            if (!_items.ContainsKey(id)) { Console.WriteLine("Нет такого id."); return; }
            var it = ReadItem();
            it.Id = id; it.CreatedAt = _items[id].CreatedAt;
            _items[id] = it; Console.WriteLine("Обновлено.");
        }
        static void RemoveCmd(string arg)
        {
            int id; if (!int.TryParse(arg, out id)) { Console.WriteLine("remove_key <id>"); return; }
            Console.WriteLine(_items.Remove(id) ? "Удалено." : "Нет такого id.");
        }

        static void FilterKind()
        {
            Console.WriteLine("Тип: 1-Weapon, 2-Armor, 3-Potion, 4-Misc");
            int v = ReadInt(1, 4);
            var res = _items.Values.Where(x => x.Kind == (ItemKind)v).OrderBy(x => x.Id);
            foreach (var it in res) Console.WriteLine(it);
        }
        static void GroupByRarity()
        {
            var g = _items.Values.GroupBy(x => x.Rarity).OrderBy(x => x.Key).Select(x => x.Key + "=" + x.Count());
            Console.WriteLine(string.Join(", ", g));
        }

        // ввод предмета
        static GameItem ReadItem()
        {
            var it = new GameItem();

            while (true)
            {
                Console.Write("Название (обяз.): ");
                var s = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(s)) { it.Name = s; break; }
                Console.WriteLine("Обязательно.");
            }
            Console.Write("Описание (можно пусто): ");
            it.Description = Console.ReadLine();

            Console.WriteLine("Редкость: 1-Common, 2-Rare, 3-Epic, 4-Legendary");
            it.Rarity = (Rarity)ReadInt(1, 4);
            Console.WriteLine("Тип: 1-Weapon, 2-Armor, 3-Potion, 4-Misc");
            it.Kind = (ItemKind)ReadInt(1, 4);

            Console.Write("Создатель (имя, обяз.): ");
            string maker;
            while (true)
            {
                maker = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(maker)) break;
                Console.WriteLine("Имя обяз.");
            }
            Console.Write("Город (можно пусто): ");
            it.Maker = new Crafter { Name = maker, City = Console.ReadLine() };

            Console.Write("Требуемый уровень (1..60): ");
            it.RequiredLevel = ReadInt(1, 60);
            Console.Write("Цена (>=0): ");
            it.Price = ReadDouble(0);
            Console.Write("Прочность (0..100): ");
            it.Durability = ReadInt(0, 100);

            Console.Write("Вес (>=0): ");
            var w = ReadDouble(0);
            Console.Write("Длина (>=0): ");
            var l = ReadDouble(0);
            Console.Write("Слоты (>=0): ");
            var sslots = ReadInt(0, 999);
            it.Size = new ItemSize { Weight = w, Length = l, Slots = sslots };

            return it;
        }

        static int ReadInt(int min, int max)
        {
            while (true)
            {
                var s = Console.ReadLine();
                int v; if (int.TryParse(s, out v) && v >= min && v <= max) return v;
                Console.Write("Введите число [" + min + ".." + max + "]: ");
            }
        }
        static double ReadDouble(double min)
        {
            while (true)
            {
                var s = Console.ReadLine();
                double v; if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && v >= min) return v;
                Console.Write("Введите число >= " + min + ": ");
            }
        }
    }

    // ===================== ЛР3 — сеть (Вариант 9) =====================
    // Команды/ответ (только классы — без “просто строк”)
    [XmlInclude(typeof(HelpCommand))]
    [XmlInclude(typeof(InfoCommand))]
    [XmlInclude(typeof(ShowCommand))]
    [XmlInclude(typeof(InsertCommand))]
    [XmlInclude(typeof(UpdateCommand))]
    [XmlInclude(typeof(RemoveKeyCommand))]
    [XmlInclude(typeof(ClearCommand))]
    [XmlInclude(typeof(FilterKindCommand))]
    [XmlInclude(typeof(GroupByRarityCommand))]
    [XmlInclude(typeof(CountCommand))]
    public abstract class Command { }
    public class HelpCommand : Command { }
    public class InfoCommand : Command { }
    public class ShowCommand : Command { }
    public class ClearCommand : Command { }
    public class CountCommand : Command { }
    public class InsertCommand : Command { public GameItem Item; }
    public class UpdateCommand : Command { public int Id; public GameItem Item; }
    public class RemoveKeyCommand : Command { public int Id; }
    public class FilterKindCommand : Command { public ItemKind Kind; }
    public class GroupByRarityCommand : Command { }
    public class Response
    {
        public bool Ok = true;
        public string Message = "";
        public List<GameItem> Items = new List<GameItem>();
        public int Count;
    }

    static class Lab3Module
    {
        const int PORT = 5555;
        static readonly System.Net.IPAddress HOST_IP = System.Net.IPAddress.Loopback;

        // ========== SERVER ==========
        public static void RunServer()
        {
            Log.Info("ЛР3/Server запускается…");

            string path = Csv.DefaultFile;
            int nextId;
            var items = Csv.Load(path, out nextId);

            // сохраняем при выходе
            AppDomain.CurrentDomain.ProcessExit += (_, __) =>
            {
                Csv.Save(items, path);
                Log.Info("CSV сохранён: " + path);
            };

            var listener = new TcpListener(new IPEndPoint(HOST_IP, PORT));
            listener.Start();
            Log.Info("Слушаю TCP " + PORT);

            while (true)
            {
                try
                {
                    var client = listener.AcceptTcpClient();
                    Log.Info("Клиент подключен.");
                    HandleClient(client, items, ref nextId, path);
                }
                catch (Exception ex) { Log.Err("Accept: " + ex.Message); }
            }
        }

        static void HandleClient(TcpClient c, Dictionary<int, GameItem> items, ref int nextId, string path)
        {
            using (c)
            using (var s = c.GetStream())
            {
                var cmdSer = new XmlSerializer(typeof(Command));
                var respSer = new XmlSerializer(typeof(Response));

                while (c.Connected)
                {
                    try
                    {
                        string xml = ReadPacket(s);
                        if (xml == null) break;

                        Command cmd;
                        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml))) cmd = (Command)cmdSer.Deserialize(ms);

                        var resp = Exec(cmd, items, ref nextId);
                        using (var ms = new MemoryStream())
                        {
                            respSer.Serialize(ms, resp);
                            WritePacket(s, Encoding.UTF8.GetString(ms.ToArray()));
                        }
                        Log.Info("Ответ отправлен.");
                    }
                    catch (Exception ex) { Log.Err("Handle: " + ex.Message); break; }
                }
                Log.Info("Клиент отключён.");
            }
        }

        static Response Exec(Command c, Dictionary<int, GameItem> items, ref int nextId)
        {
            var resp = new Response();

            if (c is HelpCommand)
            {
                resp.Message = "help, info, show, insert, update <id>, remove_key <id>, clear, filter_kind, group_by_rarity, count (exit только у клиента)";
            }
            else if (c is InfoCommand)
            {
                resp.Message = "Тип: Dictionary<int,GameItem>; файл: " + Csv.DefaultFile + "; кол-во: " + items.Count;
            }
            else if (c is ShowCommand)
            {
                resp.Items = items.Values
                    .OrderBy(x => x.Size.Weight + x.Size.Length + x.Size.Slots).ToList();
                resp.Count = resp.Items.Count;
            }
            else if (c is ClearCommand)
            {
                items.Clear(); resp.Message = "Коллекция очищена.";
            }
            else if (c is CountCommand)
            {
                resp.Count = items.Count; resp.Message = "Количество: " + resp.Count;
            }
            else if (c is RemoveKeyCommand)
            {
                var rk = (RemoveKeyCommand)c;
                resp.Ok = items.Remove(rk.Id);
                resp.Message = resp.Ok ? "Удалено." : "Нет такого id.";
            }
            else if (c is InsertCommand)
            {
                var ins = (InsertCommand)c;
                var it = ins.Item ?? new GameItem();
                it.Id = nextId++;
                it.CreatedAt = DateTime.Now;
                items[it.Id] = it;
                resp.Message = "Добавлено id=" + it.Id;
            }
            else if (c is UpdateCommand)
            {
                var up = (UpdateCommand)c;
                if (!items.ContainsKey(up.Id)) { resp.Ok = false; resp.Message = "Нет такого id."; }
                else
                {
                    var it = up.Item ?? new GameItem();
                    it.Id = up.Id;
                    it.CreatedAt = items[up.Id].CreatedAt;
                    items[up.Id] = it;
                    resp.Message = "Обновлено.";
                }
            }
            else if (c is FilterKindCommand)
            {
                var fk = (FilterKindCommand)c;
                resp.Items = items.Values.Where(x => x.Kind == fk.Kind)
                    .OrderBy(x => x.Size.Weight + x.Size.Length + x.Size.Slots).ToList();
                resp.Count = resp.Items.Count;
                resp.Message = "Отфильтровано: " + resp.Count;
            }
            else if (c is GroupByRarityCommand)
            {
                var g = items.Values.GroupBy(x => x.Rarity).OrderBy(x => x.Key).Select(x => x.Key + "=" + x.Count());
                resp.Message = "Группы: " + string.Join(", ", g);
                resp.Items = items.Values.OrderBy(x => x.Size.Weight + x.Size.Length + x.Size.Slots).ToList();
                resp.Count = resp.Items.Count;
            }
            else
            {
                resp.Ok = false; resp.Message = "Неизвестная команда.";
            }

            return resp;
        }

        // ========== CLIENT ==========
        public static void RunClient()
        {
            Console.WriteLine("Клиент. Команды: help, info, show, insert, update <id>, remove_key <id>, clear, filter_kind, group_by_rarity, count, exit");

            while (true)
            {
                try
                {
                    using (var tcp = new TcpClient())
                    {
                        tcp.Connect(new IPEndPoint(HOST_IP, PORT));
                        using (var s = tcp.GetStream())
                        {
                            var cmd = ReadCommand();
                            if (cmd == null) return;

                            var cmdSer = new XmlSerializer(typeof(Command));
                            using (var ms = new MemoryStream()) { cmdSer.Serialize(ms, cmd); WritePacket(s, Encoding.UTF8.GetString(ms.ToArray())); }

                            var xml = ReadPacket(s);
                            var respSer = new XmlSerializer(typeof(Response));
                            Response resp;
                            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml))) resp = (Response)respSer.Deserialize(ms);

                            Print(resp);
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine("[Сеть] Сервер недоступен: " + ex.Message); }
            }
        }

        static void Print(Response r)
        {
            if (!string.IsNullOrWhiteSpace(r.Message))
                Console.WriteLine(r.Ok ? r.Message : "[Ошибка] " + r.Message);
            if (r.Items != null && r.Items.Count > 0)
                foreach (var it in r.Items) Console.WriteLine(it);
            else if (r.Count != 0) Console.WriteLine("Количество: " + r.Count);
        }

        static Command ReadCommand()
        {
            Console.Write("\n> ");
            var line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) return new HelpCommand();
            var parts = line.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLowerInvariant();
            var arg = parts.Length > 1 ? parts[1].Trim() : "";

            if (cmd == "help") return new HelpCommand();
            if (cmd == "info") return new InfoCommand();
            if (cmd == "show") return new ShowCommand();
            if (cmd == "clear") return new ClearCommand();
            if (cmd == "count") return new CountCommand();
            if (cmd == "remove_key")
            {
                int id; if (!int.TryParse(arg, out id)) { Console.WriteLine("remove_key <id>"); return null; }
                return new RemoveKeyCommand { Id = id };
            }
            if (cmd == "insert") return new InsertCommand { Item = ReadItem() };
            if (cmd == "update")
            {
                int id; if (!int.TryParse(arg, out id)) { Console.WriteLine("update <id>"); return null; }
                return new UpdateCommand { Id = id, Item = ReadItem() };
            }
            if (cmd == "filter_kind")
            {
                Console.WriteLine("Тип: 1-Weapon, 2-Armor, 3-Potion, 4-Misc");
                int v = ReadInt(1, 4);
                return new FilterKindCommand { Kind = (ItemKind)v };
            }
            if (cmd == "group_by_rarity") return new GroupByRarityCommand();
            if (cmd == "exit") return null;

            Console.WriteLine("Неизвестная команда."); return null;
        }

        static GameItem ReadItem()
        {
            var it = new GameItem();

            while (true)
            {
                Console.Write("Название (обяз.): ");
                var s = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(s)) { it.Name = s; break; }
                Console.WriteLine("Обязательно.");
            }
            Console.Write("Описание (можно пусто): ");
            it.Description = Console.ReadLine();

            Console.WriteLine("Редкость: 1-Common,2-Rare,3-Epic,4-Legendary");
            it.Rarity = (Rarity)ReadInt(1, 4);

            Console.WriteLine("Тип: 1-Weapon,2-Armor,3-Potion,4-Misc");
            it.Kind = (ItemKind)ReadInt(1, 4);

            Console.Write("Создатель (имя, обяз.): ");
            string maker;
            while (true) { maker = Console.ReadLine(); if (!string.IsNullOrWhiteSpace(maker)) break; Console.WriteLine("Имя обяз."); }
            Console.Write("Город (можно пусто): ");
            it.Maker = new Crafter { Name = maker, City = Console.ReadLine() };

            Console.Write("Требуемый уровень (1..60): ");
            it.RequiredLevel = ReadInt(1, 60);
            Console.Write("Цена (>=0): ");
            it.Price = ReadDouble(0);
            Console.Write("Прочность (0..100): ");
            it.Durability = ReadInt(0, 100);

            Console.Write("Вес (>=0): ");
            var w = ReadDouble(0);
            Console.Write("Длина (>=0): ");
            var l = ReadDouble(0);
            Console.Write("Слоты (>=0): ");
            var sslots = ReadInt(0, 999);
            it.Size = new ItemSize { Weight = w, Length = l, Slots = sslots };

            return it; // Id/CreatedAt поставит сервер
        }

        // ===== сетевые фреймы (длина + xml) =====
        static void WritePacket(NetworkStream s, string xml)
        {
            var data = Encoding.UTF8.GetBytes(xml);
            var len = BitConverter.GetBytes(data.Length);
            s.Write(len, 0, 4);
            s.Write(data, 0, data.Length);
        }
        static string ReadPacket(NetworkStream s)
        {
            var lenBuf = new byte[4];
            int r = ReadExact(s, lenBuf, 0, 4);
            if (r == 0) return null;
            int len = BitConverter.ToInt32(lenBuf, 0);
            var data = new byte[len];
            ReadExact(s, data, 0, len);
            return Encoding.UTF8.GetString(data);
        }
        static int ReadExact(NetworkStream s, byte[] buf, int off, int count)
        {
            int got = 0;
            while (got < count)
            {
                int n = s.Read(buf, off + got, count - got);
                if (n == 0) return 0;
                got += n;
            }
            return got;
        }

        // ввод чисел
        static int ReadInt(int min, int max)
        {
            while (true)
            {
                var s = Console.ReadLine();
                int v; if (int.TryParse(s, out v) && v >= min && v <= max) return v;
                Console.Write("Введите число [" + min + ".." + max + "]: ");
            }
        }
        static double ReadDouble(double min)
        {
            while (true)
            {
                var s = Console.ReadLine();
                double v; if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && v >= min) return v;
                Console.Write("Введите число >= " + min + ": ");
            }
        }
    }
}
