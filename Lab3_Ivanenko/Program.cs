using System;
using System.Text;

namespace LabAllInOne
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Русская консоль в UTF-8 (достаточно для cmd/PowerShell/Windows Terminal)
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            while (true)
            {
                Console.WriteLine("\n=== МЕНЮ ===");
                Console.WriteLine("1) ЛР1 — простая симуляция предметов");
                Console.WriteLine("2) ЛР2 — менеджер коллекции (CSV)");
                Console.WriteLine("3) ЛР3 — сервер");
                Console.WriteLine("4) ЛР3 — клиент");
                Console.WriteLine("0) Выход");
                Console.Write("> ");

                var k = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(k)) continue;
                k = k.Trim();

                if (k == "0") return;

                try
                {
                    switch (k)
                    {
                        case "1": Lab1Module.Run(); break;
                        case "2": Lab2Module.Run(); break;
                        case "3": Lab3Module.RunServer(); break;
                        case "4": Lab3Module.RunClient(); break;
                        default: Console.WriteLine("Неизвестный пункт меню."); break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ОШИБКА] " + ex.Message);
                }
            }
        }
    }
}
