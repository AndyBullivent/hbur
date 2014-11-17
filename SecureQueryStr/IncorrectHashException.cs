using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Collabco.Security
{
    [Serializable]
    public class IncorrectHashException: Exception
    {
        readonly Int32 data;
        public IncorrectHashException() { }

        public IncorrectHashException(Int32 data): base(FormatMessage(data))
        {
            this.data = data;
        }

        public IncorrectHashException(string message):base(message)
        { }

        public IncorrectHashException(Int32 data, Exception inner)
            : base(FormatMessage(data), inner)
        {
            this.data = data;
        }

        public IncorrectHashException(string message, Exception inner):base(message,inner)
        { }

        protected IncorrectHashException(SerializationInfo info, StreamingContext context):base(info, context)
        {
            if(info == null)
            {
                throw new ArgumentNullException("info");
            }
            this.data = info.GetInt32("data");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if(info==null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("data", this.data);
            base.GetObjectData(info, context);
        }

        public Int32 Data { get { return this.data; } }

        static string FormatMessage(int data)
        {
            return String.Format("Invalid Hash Exception with data {0}.", data);
        }
    }
}
