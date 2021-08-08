using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Am
{
    class Program
    {
        private static Logger _logger;

        static void Main(string[] args)
        {
            var config = new LoggingConfiguration();
            var rootFolder = Directory.GetCurrentDirectory();
            var logFolder = Path.Combine(rootFolder, "logs");

            InternalLogger.LogFile = Path.Combine(logFolder, "am.internal.log");
            LogManager.ThrowConfigExceptions = true;

            var fileTarget = new FileTarget("file")
            {
                FileName = Path.Combine(logFolder, "log-${shortdate}.log"),
                ArchiveEvery = FileArchivePeriod.Day,
                MaxArchiveFiles = 7,
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}",
            };

            var wrapper = new BufferingTargetWrapper(fileTarget)
            {
                BufferSize = 1000,
                FlushTimeout = 100,
                OverflowAction = BufferingTargetWrapperOverflowAction.Flush,
            };

            var consoleTarget = new ConsoleTarget("console")
            {
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}",
            };

            config.AddRuleForAllLevels(wrapper);
            config.AddRuleForAllLevels(consoleTarget);
            LogManager.Configuration = config;

            _logger = LogManager.GetLogger("am");
            _logger.Info("Process start");

            try
            {
                Run(args);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Process exception");
            }
            finally
            {
                _logger.Info("Process end");
            }

            LogManager.Shutdown();
        }

        private static void Run(string[] args)
        {
            if (args.Length < 1)
                throw new Exception($"Usage: Am.exe <url to monitor> (supplied args={string.Join(", ", args)})");

            var url = args[0];
            var cts = new CancellationTokenSource();

            var threads = new List<Thread>
            {
                new Thread(() => MonitorAsync(1, cts.Token, url).GetAwaiter().GetResult()),
                new Thread(() => MonitorAsync(2, cts.Token, url).GetAwaiter().GetResult()),
                new Thread(() => MonitorAsync(3, cts.Token, url).GetAwaiter().GetResult()),
            };

            threads.ForEach(t => t.Start());

            _logger.Info("Press enter to stop ...");
            Console.ReadLine();

            cts.Cancel();
            threads.ForEach(t => t.Join());
        }

        private static async Task MonitorAsync(int id, CancellationToken cancellationToken, string url)
        {
            try
            {
                while (true)
                {
                    await Task.Delay(1000);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Info($"Monitor {id} exit");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error in monitor {id}");
            }
        }
    }
}
