using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Serialization;

namespace LabAllInOne
{
    // ======== ЛР1 (простая заглушка, чтобы меню не ругалось) ========
    public static class Lab1Module
    {
        public static void Run()
        {
            Console.WriteLine("ЛР1: простая демонстрация. Здесь может быть ваша симуляция из ЛР1.");
            Console.WriteLine("Нажмите Enter, чтобы вернуться в меню...");
            Console.ReadLine();
        }
    }

    // ======== Утилиты для русских enum и ввода ========
    static class Ui
    {
        public static Rarity ReadRarity()
        {
            Console.Write("Редкость (1-Обычная, 2-Редкая, 3-Эпическая, 4-Легендарная): ");
            while (true)
            {
                var s = (Console.ReadLine() ?? "").Trim().ToLower();
                int n;
                if (int.TryParse(s, out n) && n >= 1 && n <= 4) return (Rarity)n;

                if (s == "обычная" || s == "common") return Rarity.Common;
                if (s == "редкая" || s == "rare") return Rarity.Rare;
                if (s == "эпическая" || s == "epic") return Rarity.Epic;
                if (s == "легендарная" || s == "legendary") return Rarity.Legendary;

                Console.Write("Введите 1..4 или слово (Обычная/Редкая/Эпическая/Легендарная): ");
            }
        }

        public static ItemKind ReadKind()
        {
            Console.Write("Тип (1-Оружие, 2-Броня, 3-Зелье, 4-Другое): ");
            while (true)
            {
                var s = (Console.ReadLine() ?? "").Trim().ToLower();
                int n;
                if (int.TryParse(s, out n) && n >= 1 && n <= 4) return (ItemKind)n;

                if (s == "оружие" || s == "weapon") return ItemKind.Weapon;
                if (s == "броня" || s == "armor") return ItemKind.Armor;
                if (s == "зелье" || s == "potion") return ItemKind.Potion;
                if (s == "другое" || s == "misc") return ItemKind.Misc;

                Console.Write("Введите 1..4 или слово (Оружие/Броня/Зелье/Другое): ");
            }
        }

        public static int ReadIntRange(int min, int max, string prompt)
        {
            Console.Write(prompt);
            while (true)
            {
                var s = Console.ReadLine();
                int v;
                if (int.TryParse(s, out v) && v >= min && v <= max) return v;
                Console.Write("Введите число в диапазоне " + min + ".." + max + ": ");
            }
        }

        public static double ReadDoubleMin(double min, string prompt)
        {
            Console.Write(prompt);
            while (true)
            {
                var s = Console.ReadLine();
                double v;
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && v >= min) return v;
                Console.Write("Введите число >= " + min + ": ");
            }
        }
    }

    // ======== ЛР2 — менеджер CSV локально ========
    public static class Lab2Module
    {
        public static void Run()
        {
            var path = Csv.DefaultFile;
            int nextId;
            var items = Csv.Load(path, out nextId);

            Console.WriteLine("Файл: " + path);
            Console.WriteLine("Команды: help, info, show, insert, update <id>, remove_key <id>, clear, save, exit");

            while (true)
            {
                Console.Write("\n> ");
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Trim().Split(new[] { ' ' }, 2);
                var cmd = parts[0].ToLowerInvariant();
                var arg = parts.Length > 1 ? parts[1].Trim() : "";

                if (cmd == "exit") break;

                try
                {
                    if (cmd == "help")
                    {
                        Console.WriteLine("help, info, show, insert, update <id>, remove_key <id>, clear, save, exit");
                    }
                    else if (cmd == "info")
                    {
                        Console.WriteLine("Dictionary<int, GameItem>, элементов: " + items.Count);
                    }
                    else if (cmd == "show")
                    {
                        foreach (var it in items.Values.OrderBy(x => x.Size.Slots).ThenBy(x => x.Price))
                            Console.WriteLine(it);
                    }
                    else if (cmd == "insert")
                    {
                        var it = ReadItem(false, 0);
                        it.Id = nextId++;
                        it.CreatedAt = DateTime.Now;
                        items[it.Id] = it;
                        Console.WriteLine("Добавлено: " + it.Id);
                    }
                    else if (cmd == "update")
                    {
                        int id;
                        if (!int.TryParse(arg, out id) || !items.ContainsKey(id))
                        {
                            Console.WriteLine("update <id>");
                            continue;
                        }
                        var it = ReadItem(true, id);
                        it.Id = id;
                        items[id] = it;
                        Console.WriteLine("Обновлено: " + id);
                    }
                    else if (cmd == "remove_key")
                    {
                        int id;
                        if (!int.TryParse(arg, out id))
                        {
                            Console.WriteLine("remove_key <id>");
                            continue;
                        }
                        Console.WriteLine(items.Remove(id) ? "Удалено." : "Нет такого id.");
                    }
                    else if (cmd == "clear")
                    {
                        items.Clear();
                        Console.WriteLine("Очищено.");
                    }
                    else if (cmd == "save")
                    {
                        Csv.Save(items, path);
                        try { GitAuto.Publish(path, "auto: save (lab2)"); } catch { }
                        Console.WriteLine("Сохранено: " + path);
                    }
                    else
                    {
                        Console.WriteLine("Неизвестная команда.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ОШИБКА] " + ex.Message);
                }
            }
        }

        static GameItem ReadItem(bool isUpdate, int id)
        {
            var it = new GameItem();
            Console.Write("Название: ");
            it.Name = Console.ReadLine();
            Console.Write("Описание (можно пусто): ");
            it.Description = Console.ReadLine();

            it.Rarity = Ui.ReadRarity();
            it.Kind = Ui.ReadKind();

            Console.Write("Создатель: имя: ");
            var makerName = Console.ReadLine();
            Console.Write("Город (можно пусто): ");
            var makerCity = Console.ReadLine();
            it.Maker = new Crafter { Name = makerName, City = makerCity };

            it.RequiredLevel = Ui.ReadIntRange(1, 60, "Уровень (1..60): ");
            it.Price = Ui.ReadDoubleMin(0, "Цена (>=0): ");
            it.Durability = Ui.ReadIntRange(0, 100, "Прочность (0..100): ");

            var w = Ui.ReadDoubleMin(0, "Вес (>=0): ");
            var l = Ui.ReadDoubleMin(0, "Длина (>=0): ");
            var slots = Ui.ReadIntRange(0, 999, "Слоты (>=0): ");
            it.Size = new ItemSize { Weight = w, Length = l, Slots = slots };

            if (!isUpdate)
            {
                it.CreatedAt = DateTime.Now;
            }
            return it;
        }
    }

    // ======== ЛР3 — сервер/клиент (TCP + XML) ========
    public static class Lab3Module
    {
        public const string HOST = "127.0.0.1";
        public const int PORT = 5555;

        // ----- Команды -----
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
        public class GroupByRarityCommand : Command { }
        public class CountCommand : Command { }

        public class InsertCommand : Command
        {
            public GameItem Item;
        }

        public class UpdateCommand : Command
        {
            public int Id;
            public GameItem Item;
        }

        public class RemoveKeyCommand : Command
        {
            public int Id;
        }

        public class FilterKindCommand : Command
        {
            public ItemKind Kind;
        }

        public class Request
        {
            public Command Cmd;
        }

        // Группа для сериализации (вместо Dictionary!)
        public class Group
        {
            public string Name;
            public List<GameItem> Items;
        }

        public class Response
        {
            public string Message;
            public List<GameItem> Items;
            public List<Group> Groups;
            public int Count;
            public string Error;
        }

        // ----- Хранилище на сервере -----
        static Dictionary<int, GameItem> _items;
        static int _nextId;

        static void EnsureLoaded()
        {
            if (_items != null) return;
            _items = Csv.Load(Csv.DefaultFile, out _nextId);
        }

        static void Save()
        {
            Csv.Save(_items, Csv.DefaultFile);
            try { GitAuto.Publish(Csv.DefaultFile, "auto: save (server)"); } catch { }
        }

        // ----- Сервер -----
        public static void RunServer()
        {
            EnsureLoaded();
            Console.WriteLine("[ЛР3/Server] Запускаю TCP " + HOST + ":" + PORT);

            var listener = new TcpListener(IPAddress.Parse(HOST), PORT);
            listener.Start();

            Console.WriteLine("[ЛР3/Server] Слушаю... (Ctrl+C чтобы убить процесс)");
            while (true)
            {
                var client = listener.AcceptTcpClient(); // блокирующий режим
                Console.WriteLine("[ЛР3/Server] Клиент подключился.");

                try
                {
                    using (client)
                    using (var ns = client.GetStream())
                    {
                        while (client.Connected)
                        {
                            var req = ReceiveXml<Request>(ns);
                            if (req == null) break;
                            var resp = Exec(req.Cmd);
                            SendXml(ns, resp);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ЛР3/Server] Ошибка клиента: " + ex.Message);
                }

                Console.WriteLine("[ЛР3/Server] Клиент отключился.");
            }
        }

        static Response Exec(Command c)
        {
            var resp = new Response();
            try
            {
                if (c is HelpCommand)
                {
                    resp.Message = "Команды: help, info, show, insert, update <id>, remove_key <id>, clear, filter_kind <тип>, group_by_rarity, count";
                }
                else if (c is InfoCommand)
                {
                    resp.Message = "Dictionary<int, GameItem>, элементов: " + _items.Count;
                }
                else if (c is ShowCommand)
                {
                    resp.Items = _items.Values
                        .OrderBy(x => x.Size.Slots)
                        .ThenBy(x => x.Price)
                        .ToList();
                }
                else if (c is InsertCommand)
                {
                    var ic = (InsertCommand)c;
                    var it = ic.Item;
                    it.Id = _nextId++;
                    it.CreatedAt = DateTime.Now;
                    _items[it.Id] = it;
                    Save();
                    resp.Message = "Добавлено id=" + it.Id;
                }
                else if (c is UpdateCommand)
                {
                    var uc = (UpdateCommand)c;
                    if (!_items.ContainsKey(uc.Id))
                    {
                        resp.Error = "Нет такого id.";
                    }
                    else
                    {
                        var it = uc.Item;
                        it.Id = uc.Id;
                        if (it.CreatedAt.Ticks == 0) it.CreatedAt = DateTime.Now;
                        _items[uc.Id] = it;
                        Save();
                        resp.Message = "Обновлено id=" + uc.Id;
                    }
                }
                else if (c is RemoveKeyCommand)
                {
                    var rc = (RemoveKeyCommand)c;
                    if (_items.Remove(rc.Id)) { Save(); resp.Message = "Удалено id=" + rc.Id; }
                    else resp.Error = "Нет такого id.";
                }
                else if (c is ClearCommand)
                {
                    _items.Clear();
                    Save();
                    resp.Message = "Очищено.";
                }
                else if (c is FilterKindCommand)
                {
                    var fk = (FilterKindCommand)c;
                    resp.Items = _items.Values.Where(x => x.Kind == fk.Kind)
                        .OrderBy(x => x.Size.Slots)
                        .ThenBy(x => x.Price)
                        .ToList();
                }
                else if (c is GroupByRarityCommand)
                {
                    resp.Groups = _items.Values
                        .GroupBy(x => x.Rarity)
                        .Select(g => new Group
                        {
                            Name = g.Key.ToString(),
                            Items = g.OrderBy(x => x.Price).ToList()
                        })
                        .ToList();
                }
                else if (c is CountCommand)
                {
                    resp.Count = _items.Count;
                }
                else
                {
                    resp.Error = "Неизвестная команда.";
                }
            }
            catch (Exception ex)
            {
                resp.Error = ex.Message;
            }
            return resp;
        }

        // ----- Клиент -----
        public static void RunClient()
        {
            Console.WriteLine("Клиент. Команды: help, info, show, insert, update <id>, remove_key <id>, clear, filter_kind <тип>, group_by_rarity, count, exit (только у клиента)");

            TcpClient client = null;
            try
            {
                client = new TcpClient();
                client.Connect(HOST, PORT);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Сеть] Сервер недоступен: " + ex.Message);
                return;
            }

            using (client)
            using (var ns = client.GetStream())
            {
                while (true)
                {
                    Console.Write("\n> ");
                    var line = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Trim().Split(new[] { ' ' }, 2);
                    var cmd = parts[0].ToLowerInvariant();
                    var arg = parts.Length > 1 ? parts[1].Trim() : "";

                    if (cmd == "exit") break;

                    try
                    {
                        Request req = null;

                        if (cmd == "help") req = new Request { Cmd = new HelpCommand() };
                        else if (cmd == "info") req = new Request { Cmd = new InfoCommand() };
                        else if (cmd == "show") req = new Request { Cmd = new ShowCommand() };
                        else if (cmd == "clear") req = new Request { Cmd = new ClearCommand() };
                        else if (cmd == "group_by_rarity") req = new Request { Cmd = new GroupByRarityCommand() };
                        else if (cmd == "count") req = new Request { Cmd = new CountCommand() };
                        else if (cmd == "insert")
                        {
                            var it = ReadItem();
                            req = new Request { Cmd = new InsertCommand { Item = it } };
                        }
                        else if (cmd == "update")
                        {
                            int id;
                            if (!int.TryParse(arg, out id)) { Console.WriteLine("update <id>"); continue; }
                            var it = ReadItem();
                            req = new Request { Cmd = new UpdateCommand { Id = id, Item = it } };
                        }
                        else if (cmd == "remove_key")
                        {
                            int id;
                            if (!int.TryParse(arg, out id)) { Console.WriteLine("remove_key <id>"); continue; }
                            req = new Request { Cmd = new RemoveKeyCommand { Id = id } };
                        }
                        else if (cmd == "filter_kind")
                        {
                            if (string.IsNullOrWhiteSpace(arg)) { Console.WriteLine("filter_kind <Weapon|Armor|Potion|Misc> или по-русски."); continue; }
                            ItemKind kind;
                            if (!TryParseKind(arg, out kind)) { Console.WriteLine("Неверный тип."); continue; }
                            req = new Request { Cmd = new FilterKindCommand { Kind = kind } };
                        }
                        else
                        {
                            Console.WriteLine("Неизвестная команда.");
                            continue;
                        }

                        SendXml(ns, req);
                        var resp = ReceiveXml<Response>(ns);
                        PrintResponse(resp);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Сеть] " + ex.Message);
                    }
                }
            }
        }

        static bool TryParseKind(string s, out ItemKind kind)
        {
            s = (s ?? "").Trim().ToLower();
            if (Enum.TryParse<ItemKind>(s, true, out kind)) return true;
            if (s == "оружие") { kind = ItemKind.Weapon; return true; }
            if (s == "броня") { kind = ItemKind.Armor; return true; }
            if (s == "зелье") { kind = ItemKind.Potion; return true; }
            if (s == "другое") { kind = ItemKind.Misc; return true; }
            return false;
        }

        static GameItem ReadItem()
        {
            var it = new GameItem();
            Console.Write("Название: ");
            it.Name = Console.ReadLine();
            Console.Write("Описание (можно пусто): ");
            it.Description = Console.ReadLine();

            it.Rarity = Ui.ReadRarity();
            it.Kind = Ui.ReadKind();

            Console.Write("Создатель: имя: ");
            var makerName = Console.ReadLine();
            Console.Write("Город (можно пусто): ");
            var makerCity = Console.ReadLine();
            it.Maker = new Crafter { Name = makerName, City = makerCity };

            it.RequiredLevel = Ui.ReadIntRange(1, 60, "Уровень (1..60): ");
            it.Price = Ui.ReadDoubleMin(0, "Цена (>=0): ");
            it.Durability = Ui.ReadIntRange(0, 100, "Прочность (0..100): ");

            var w = Ui.ReadDoubleMin(0, "Вес (>=0): ");
            var l = Ui.ReadDoubleMin(0, "Длина (>=0): ");
            var slots = Ui.ReadIntRange(0, 999, "Слоты (>=0): ");
            it.Size = new ItemSize { Weight = w, Length = l, Slots = slots };

            // CreatedAt/Id выставляет сервер
            return it;
        }

        static void PrintResponse(Response r)
        {
            if (r == null) { Console.WriteLine("[Клиент] Пустой ответ."); return; }
            if (!string.IsNullOrWhiteSpace(r.Error)) { Console.WriteLine("[ОШИБКА] " + r.Error); return; }
            if (!string.IsNullOrWhiteSpace(r.Message)) Console.WriteLine(r.Message);
            if (r.Count != 0) Console.WriteLine("Количество: " + r.Count);
            if (r.Items != null && r.Items.Count > 0)
                foreach (var it in r.Items) Console.WriteLine(it);
            if (r.Groups != null && r.Groups.Count > 0)
            {
                foreach (var g in r.Groups)
                {
                    Console.WriteLine(g.Name + ":");
                    foreach (var it in g.Items) Console.WriteLine("  " + it);
                }
            }
        }

        // ----- Сериализация по сети (длина + UTF8 XML) -----
        static void SendXml<T>(NetworkStream ns, T obj)
        {
            var ser = new XmlSerializer(typeof(T));
            using (var ms = new MemoryStream())
            {
                ser.Serialize(ms, obj);
                var data = ms.ToArray();
                var len = BitConverter.GetBytes(data.Length);
                ns.Write(len, 0, len.Length);
                ns.Write(data, 0, data.Length);
                ns.Flush();
            }
        }

        static T ReceiveXml<T>(NetworkStream ns) where T : class
        {
            var lenBuf = new byte[4];
            int read = ReadFull(ns, lenBuf, 0, 4);
            if (read == 0) return null; // клиент закрылся
            if (read < 4) throw new IOException("Не удалось прочитать длину.");
            int len = BitConverter.ToInt32(lenBuf, 0);
            var data = new byte[len];
            read = ReadFull(ns, data, 0, len);
            if (read < len) throw new IOException("Данные усечены.");

            var ser = new XmlSerializer(typeof(T));
            using (var ms = new MemoryStream(data))
                return (T)ser.Deserialize(ms);
        }

        static int ReadFull(NetworkStream ns, byte[] buf, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int r = ns.Read(buf, offset + total, count - total);
                if (r == 0) break;
                total += r;
            }
            return total;
        }
    }
}
