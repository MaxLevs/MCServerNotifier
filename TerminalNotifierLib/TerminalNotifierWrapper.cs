using System;
using System.Diagnostics;

namespace TerminalNotifierLib
{
    public static class TerminalNotifierWrapper
    {
        private static readonly string Name = "terminal-notifier";
        
        public static void Notify(TerminalNotifierOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Message))
                throw new Exception();
            
            var args = BuildArgsByOptions(options);
            Process.Start(Name, args);
        }

        private static string BuildArgsByOptions(TerminalNotifierOptions options)
        {
            var args = $" -message \"{options.Message}\"";
            
            if (!string.IsNullOrEmpty(options.Title))
            {
                args += $" -title \"{options.Title}\"";
            }
            
            if (!string.IsNullOrEmpty(options.Subtitle))
            {
                args += $" -subtitle \"{options.Subtitle}\"";
            }
            
            if (!string.IsNullOrEmpty(options.ContentImage))
            {
                args += $" -contentImage \"{options.ContentImage}\"";
            }

            if (options.Sound != NotificationSound.Default)
            {
                args += $" -sound {options.Sound}";
            }
            
            return args;
        }
    }
}