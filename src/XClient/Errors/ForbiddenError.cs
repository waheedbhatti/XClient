using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XClient.Errors
{
    public class ForbiddenError : Error
    {
        public ForbiddenError() : base("You do not have permission to access this resource")
        {
        }

        public ForbiddenError(string errorMessage) : base(errorMessage)
        {
            
        }
    }
}
