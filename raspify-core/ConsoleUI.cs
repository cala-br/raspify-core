using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaspifyCore
{
    class ConsoleUI
    {
        private static ConsoleUI _instance;
        public static ConsoleUI GetInstance()
        {
            _instance ??= new();
            return _instance;
        }


        private List<string> _logMessages = new();


        private ConsoleUI() { }


        public void PushLogMessage(string message)
        {
            _logMessages.Add(message);
            Draw();
        }


        public void Draw()
        {
            Console.Clear();
            DrawMenu();
            DrawDivider();
            DrawLogMessages();
            Console.SetCursorPosition(4, 2);
        }


        private static void DrawMenu()
        {
            Console.Write(
                "d. Disconnect all\n" +
                "e. Exit\n" +
                " >> "
            );
        }

        private void DrawDivider()
        {
            var halfWidth = Console.WindowWidth / 2;
            for (int i = 0; i < Console.WindowHeight; i++)
            {
                Console.SetCursorPosition(halfWidth, i);
                Console.Write('|');
            }
        }

        private void DrawLogMessages()
        {
            var left = Console.WindowWidth / 2 + 2;
            int i = 0;

            _logMessages.ForEach(log =>
            {
                Console.SetCursorPosition(left, i++);
                Console.Write(log);
            });
        }
    }
}
