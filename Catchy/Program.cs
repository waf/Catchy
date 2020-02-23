using Catchy.HttpProxy;
using Catchy.CacheStrategies;
using Catchy.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;

namespace Catchy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IReadOnlyCollection<Type> cacheStrategyTypes = DiscoverCacheStrategies();
            var app = ConfigureCommandLine(cacheStrategyTypes);
            try
            {
                app.Execute(args);
            }
            catch (CommandParsingException parseException)
            {
                app.ShowHelp();
                Console.ForegroundColor = ConsoleColor.Red;
                app.Error.WriteLine(parseException.Message);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Scan through this DLL and find every class that implements <see cref="ICacheStrategy"/>
        /// </summary>
        private static IReadOnlyCollection<Type> DiscoverCacheStrategies() =>
            Assembly.GetExecutingAssembly()
                .DefinedTypes
                .Select(typeInfo => typeInfo.AsType())
                .Where(type => type.IsClass && typeof(ICacheStrategy).IsAssignableFrom(type))
                .ToList();

        private static CommandLineApplication ConfigureCommandLine(IReadOnlyCollection<Type> cacheStrategyTypes)
        {
            const int DefaultPort = 9999;

            var app = new CommandLineApplication { Name = nameof(Catchy) };
            app.HelpOption();
            app.ExtendedHelpText = $"{Environment.NewLine}Example:{Environment.NewLine}  Catchy --CacheByRestRequest worldtimeapi.org --CacheByRestRequest news.ycombinator.com{Environment.NewLine}";

            var portOption = app.Option<int>(
                    template: "-p|--port <PORT>",
                    description: $"The network port to use for the local proxy. Default is {DefaultPort}",
                    optionType: CommandOptionType.SingleValue
                )
                .Accepts(opt => opt.Range(0, 65535));

            // configure a command line option per cache strategy
            var cacheStrategyOptions = cacheStrategyTypes
                .Select(strategy => app.Option(
                    template: $"--{strategy.Name} <HOST>",
                    description: $"use the {strategy.Name} strategy for requests to this host. Can be specified multiple times for additional hosts.",
                    optionType: CommandOptionType.MultipleValue)
                )
                .ToList();

            app.OnValidate(context =>
                cacheStrategyOptions.Any(option => option.HasValue())
                    ? ValidationResult.Success
                    : new ValidationResult("At least one caching strategy must be provided")
            );

            app.ValidationErrorHandler = err =>
            {
                app.ShowHelp();
                app.Error.WriteLine(err.ErrorMessage);
                return 1;
            };

            app.OnExecute(() =>
            {
                // instantiate a cache strategy for each strategy specified in the command line options
                var cacheStrategies = cacheStrategyOptions
                    .Where(o => o.HasValue())
                    .Join(
                        cacheStrategyTypes,
                        option => option.LongName,
                        strategy => strategy.Name,
                        (option, strategy) => InstantiateStrategy(strategy, option)
                    )
                    .ToList();

                int port = portOption.HasValue() ? portOption.ParsedValue : 9999;

                StartProxy(port, cacheStrategies);
            });

            return app;
        }

        /// <summary>
        /// Instantiates the provided caching strategy with the provided command line options
        /// </summary>
        private static ICacheStrategy InstantiateStrategy(Type cacheStrategy, CommandOption option) =>
            Activator.CreateInstance(cacheStrategy, new[] { option.Values }) is ICacheStrategy instantiatedStrategy
                ? instantiatedStrategy
                : throw new InvalidOperationException("Unable to instantiate strategy: " + cacheStrategy.GetType().Name);

        /// <summary>
        /// The main application flow. Starts and configures the proxy, and
        /// blocks until the user presses any key.
        /// </summary>
        private static void StartProxy(int port, IReadOnlyCollection<ICacheStrategy> cacheStrategies)
        {
            var handledHosts = cacheStrategies.SelectMany(handler => handler.HandledHosts).ToList();

            ConsoleUI.WelcomeMessage();
            ConsoleUI.DescribeHandledHosts(handledHosts);

            var interceptor = new RequestResponseInterceptor(cacheStrategies);
            using (var proxy = new Proxy(IPAddress.Any, port, ShouldDecryptUrl))
            {
                proxy.OnRequest += (_, session) => interceptor.InterceptRequest(session);
                proxy.OnResponse += (_, session) => interceptor.InterceptResponse(session);
                proxy.OnError += (_, exception) => ConsoleUI.ErrorMessage(exception);

                ConsoleUI.ReadyMessage();
                Console.ReadKey();
            }

            // only decrypt SSL traffic for urls we're handling. There are efficiency benefits, but
            // also firefox has its own root cert store, so if we unnecessarily decrypt everything
            // then users will see a lot of cert errors in firefox while browsing unrelated websites.
            bool ShouldDecryptUrl(Uri uri) =>
                handledHosts.Contains(uri.Host);
        }
    }
}
