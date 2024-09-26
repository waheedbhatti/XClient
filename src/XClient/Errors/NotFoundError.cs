using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XClient.Errors
{
    public class NotFoundError : Error
    {
        public NotFoundError() : base("Not found")
        {
        }

        public NotFoundError(string message) : base(message)
        {
        }
    }
}
