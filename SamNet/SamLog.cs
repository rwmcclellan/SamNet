// Copyright (c) 2026 Robert W. McClellan, Matthew J. McClellan
// Licensed under the GNU General Public License v3.0. See LICENSE in the repository root.

using Open.IP;

namespace SamNet
{
    public static class SamLog
    {

        public static List<LogEntry> Logger = new List<LogEntry>();


        public static void AddEntry(string caller, string message)
        {
            Logger.Add(new LogEntry(caller, message));
        }
        public static void AddEntry(string caller, OpenSafeResultBase osrb)
        {
            Logger.Add(new LogEntry(caller, osrb));
        }

    }

    public class LogEntry
    {
        public string Time = string.Empty;
        public string Caller = string.Empty;
        public string Message = string.Empty;
        public int LineCount = 0;

        public LogEntry()
        { }

        public LogEntry(string caller, string message)
        {
            Time = DateTime.Now.ToShortTimeString();
            Message = message;
            Caller = caller;
            LineCount = message.Length / 60 + 1;
        }

        public LogEntry(string caller, OpenSafeResultBase osrb)
        {
            Time = DateTime.Now.ToString("hh:mm:ss tt");
            Caller = caller;
            Message = osrb.Message;
            LineCount = Message.Length / 60 + 1;
            if (osrb.IsException)
            {
                Message += "\r\n";
                Message += osrb.ExMessage;
                LineCount += osrb.ExMessage.Length / 60 + 1;
            }
        }
    }

}
