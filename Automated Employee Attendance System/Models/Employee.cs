using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automated_Employee_Attendance_System.Models
{
    public class Employee
    {
        public string emp_id { get; set; }
        public string name { get; set; }     // First + Last combined
        public string email { get; set; }
        public int finger_id { get; set; }
    }

}
