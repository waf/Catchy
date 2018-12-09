using Catchy.UI;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Catchy.Tests.UI
{
    public class OutputQueueTests
    {
        [Fact]
        public void WriteLine_EmptyQueue_AddsToQueue()
        {
            OutputQueue.WriteLine("Hello");
            OutputQueue.WriteLine("World", ConsoleColor.Blue);
            Assert.Equal(2, OutputQueue.Count);
        }

        [Fact]
        public async Task StartOutputQueue_MessagesOnQueue_WritesOutput()
        {
            OutputQueue.WriteLine("Hello");
            OutputQueue.WriteLine("World", ConsoleColor.Blue);

            var output = new StringBuilder();
            using(var stdout = new StringWriter(output))
            {
                Console.SetOut(stdout);
                var tokenSource = new CancellationTokenSource();
                // system under test
                var queueJob = OutputQueue.StartOutputThread(tokenSource.Token);
                // wait for queue to empty, then gracefully stop the thread using the token
                SpinWait.SpinUntil(() => OutputQueue.Count == 0);
                tokenSource.Cancel();
                await queueJob;
            }

            Assert.Equal(0, OutputQueue.Count);
            Assert.Equal("Hello" + Environment.NewLine + "World" + Environment.NewLine, output.ToString());
        }
    }
}