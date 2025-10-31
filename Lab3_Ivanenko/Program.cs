using System;

namespace LabAllInOne
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // быстрые аргументы: server / client для ЛР3
            if (args != null && args.Length > 0)
            {
                var a = args[0].Trim().ToLowerInvariant();
                if (a.StartsWith("serv")) { Lab3Module.RunServer(); return; }
                if (a.StartsWith("cli")) { Lab3Module.RunClient(); return; }
            }

            while (true)
            {
                Console.WriteLine("\n=== МЕНЮ ===");
                Console.WriteLine("1) ЛР1 — простая симуляция предметов");
                Console.WriteLine("2) ЛР2 — менеджер коллекции (CSV)");
                Console.WriteLine("3) ЛР3 — сервер");
                Console.WriteLine("4) ЛР3 — клиент");
                Console.WriteLine("0) Выход");
                Console.Write("> ");
                var key = Console.ReadLine();

                if (key == "1") Lab1Module.Run();
                else if (key == "2") Lab2Module.Run();
                else if (key == "3") Lab3Module.RunServer();
                else if (key == "4") Lab3Module.RunClient();
                else if (key == "0" || string.Equals(key, "exit", StringComparison.OrdinalIgnoreCase)) return;
                else Console.WriteLine("Не понял. Введите 0..4.");
            }
        }
    }
}
