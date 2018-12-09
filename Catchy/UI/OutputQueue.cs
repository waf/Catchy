using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Catchy.UI
{
    /// <summary>
    /// We have multiple threads that want to write output to the console.
    /// This does not work well with the mutable, static Console.Foreground API.
    /// The threads interfere with each others' color output.
    ///
    /// One solution to this is to lock on Console.Out, but I'd rather not block the
    /// request handling threads. Instead, the threads can call this class's WriteLine
    /// functions to dump output messages on a queue, and a single background task
    /// consumes the queue and outputs the messages. This way the various Console
    /// functions are never called from multiple threads.
    /// </summary>
    public static class OutputQueue
    {
        private static readonly BlockingCollection<Message> messages =
            new BlockingCollection<Message>(new ConcurrentQueue<Message>());

        public static void WriteLine(string text) =>
            messages.Add(new Message(text, null));

        public static void WriteLine(string text, ConsoleColor color) =>
            messages.Add(new Message(text, color));

        public static int Count => messages.Count;

        public static Task StartOutputThread(CancellationToken token = default)
        {
            return Task.Run(() =>
            {
                while(!token.IsCancellationRequested)
                {
                    var message = messages.Take(token); // blocks
                    if(message.Color is null)
                    {
                        Console.WriteLine(message.Text);
                    }
                    else
                    {
                        Console.ForegroundColor = message.Color.Value;
                        Console.WriteLine(message.Text);
                        Console.ResetColor();
                    }
                }
            }, token);
        }

        private readonly struct Message
        {
            public Message(string text, ConsoleColor? color) : this()
            {
                Text = text;
                Color = color;
            }

            public string Text { get; }
            public ConsoleColor? Color { get; }
        }
    }
}
