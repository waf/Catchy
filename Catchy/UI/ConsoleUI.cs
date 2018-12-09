using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Environment;
using static Catchy.UI.OutputQueue;

namespace Catchy.UI
{
    public static class ConsoleUI
    {
        static ConsoleUI()
        {
            Console.OutputEncoding = Encoding.UTF8;
            StartOutputThread();
        }

        public static void WelcomeMessage() =>
            WriteLine("Catchy - Caching Proxy - starting..." + NewLine);

        public static void DescribeHandledHosts(IReadOnlyCollection<string> handledUrls)
        {
            var handledUrlsMessage = handledUrls
                .Select(url => $" • {url}")
                .Prepend("Currently monitoring requests to:");
            WriteLine(string.Join(NewLine, handledUrlsMessage) + NewLine);
        }

        internal static void ReadyMessage() =>
            WriteLine("Ready! Press any key to exit" + NewLine);

        internal static void CachedResponseMessage(string url) =>
            WriteLine("← returning cached response for " + url, ConsoleColor.Green);

        internal static void CapturingResponseMessage(string url) =>
            WriteLine("→ allowing request and caching response for " + url, ConsoleColor.Blue);

        internal static void ErrorMessage(Exception exception)
        {
            var message = exception.GetBaseException().Message;
            WriteLine("× encountered error: " + message, ConsoleColor.Red);
        }
    }
}
