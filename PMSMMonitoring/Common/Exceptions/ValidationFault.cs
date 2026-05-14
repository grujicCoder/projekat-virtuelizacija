using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Common.Exceptions
{
    [DataContract]
    public class ValidationFault
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public string ExpectedRange { get; set; }

        public ValidationFault(string message, string fieldName, string expectedRange)
        {
            Message = message;
            FieldName = fieldName;
            ExpectedRange = expectedRange;
        }
    }
}
