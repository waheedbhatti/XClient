using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XClient.Errors
{
    public class ArgumentError : Error
    {
        public ArgumentError(string errorMessage) : base(errorMessage)
        {
            LogLevel = ErrorLogLevel.Warning;
        }

    }
}
