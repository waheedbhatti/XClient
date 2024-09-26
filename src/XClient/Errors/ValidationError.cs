using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XClient.Errors
{
    public class ValidationError : Error
    {
        public IDictionary<string, string[]> Errors { get; private init; }

        public ValidationError(IDictionary<string, string[]> errors) : base("One or more validation errors occurred")
        {
            Errors = errors;
        }

        public ValidationError(IDictionary<string, List<string>> errors) : base("One or more validation errors occurred")
        {
            Errors = errors.ToDictionary(e => e.Key, e => e.Value.ToArray());
        }



        public ValidationError(string propertyName, string errorMessage) : base("One or more validation errors occurred")
        {
            Errors = new Dictionary<string, string[]>
            {
                { propertyName, new string[] { errorMessage} }
            };
        }
    }
}
