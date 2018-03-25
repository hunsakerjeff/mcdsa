using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;


namespace DSA.Shell.Logging
{
    public class LogEvent
    {
        // //////////////////////////////////////////////////////////
        // Properties
        // //////////////////////////////////////////////////////////
        public LoggingLevel Level { get; set; }
        public string Message { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public Exception Exception { get; set; }


        // //////////////////////////////////////////////////////////
        // CTOR
        // //////////////////////////////////////////////////////////
        public LogEvent(LoggingLevel level, string message, Exception ex)
        {
            Level = level;
            Message = message;
            Exception = ex;
            TimeStamp = DateTimeOffset.UtcNow;
        }


        // //////////////////////////////////////////////////////////
        // Implementation Public Methods
        // //////////////////////////////////////////////////////////
        public string Serialize()
        {
            var builder = new StringBuilder();
            builder.Append(TimeStamp.ToString("o"));
            builder.Append("|");
            builder.Append(Level.ToString().ToUpper());
            builder.Append("|");
            builder.Append(Environment.CurrentManagedThreadId);
            if (!String.IsNullOrEmpty(Message))
            {
                builder.Append("|");
                builder.Append(Message);
            }
            if (Exception != null)
            {
                builder.Append("Exception:  ");
                builder.Append(Exception);
            }

            return builder.ToString();
        }
    }
}
