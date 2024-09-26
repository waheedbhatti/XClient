using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XClient.Errors
{
    public class UnauthorizedError : Error
    {
        public UnauthorizedError() : base("Authorization failed")
        { }

        public UnauthorizedError(string errorMessage) : base(errorMessage)
        { }
    }
}
