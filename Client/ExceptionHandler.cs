using System;

namespace Client
{
    internal class ExceptionHandler
    {
        public static void Handle(Exception exception)
        {
            var foregroundColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(exception);
            Console.ForegroundColor = foregroundColor;
        }
    }
}
