using System;
using System.IO;
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

        static async Task Main(string[] args)
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
                await RunAsync(args);
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

        private static async Task RunAsync(string[] args)
        {
            if (args.Length < 1)
                throw new Exception($"Usage: Am.exe <url to monitor> (supplied args={string.Join(", ", args)})");

            _logger.Info("Press enter to stop ...");
            Console.ReadLine();
            await Task.CompletedTask;
        }
    }
}
