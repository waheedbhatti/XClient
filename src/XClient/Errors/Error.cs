using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XClient.Errors
{
    public class Error
    {
        public string Message { get; init; }

        public ErrorLogLevel LogLevel { get; protected set; }
        public string? ClientMessage { get; private set; }

        public Error(string message)
        {
            Message = message;
        }

        public Error(string message, string clientMessage)
            : this(message)
        {
            ClientMessage = clientMessage;
        }

        public Error(Exception exception)
        {
            Message = exception.Message;
        }

        public Error SetClientMessage(string message)
        {
            ClientMessage = message;
            return this;
        }

    }


    public enum ErrorLogLevel
    {
        Default = 0,
        Information = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }
}
