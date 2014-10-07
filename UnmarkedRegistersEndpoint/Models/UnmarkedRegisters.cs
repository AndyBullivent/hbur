using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UnmarkedRegistersEndpoint.Models
{
    public class UnmarkedRegisters
    {
        public string RegisterNo { get; set; }
        public string RegisterTitle { get; set; }
        public DateTime Date { get; set; }
        public DateTime SessionDateTime { get; set; }
        public string CollegeLevelCode { get; set; }

        public UnmarkedRegisters() { }
    }
}