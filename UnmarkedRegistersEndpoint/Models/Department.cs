using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UnmarkedRegistersEndpoint.Models
{
    public class Department
    {
        public string DeptName { get; set; }
        public int UnmarkedRegisters { get; set; }
        public Dictionary<string,int> Lecturers { get; set; }
    }
}