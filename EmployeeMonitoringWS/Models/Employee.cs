using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace EmployeeMonitoringWS.Models
{
    public class Employee
    {
        public int id
        {
            get;
            set;
        }

        [Required(ErrorMessage = "Name is required")]
        [Display(Name = "Employee Name")]
        public string name
        {
            get;
            set;
        }

        [Required(ErrorMessage = "Surname is required")]
        [Display(Name = "Employee Surname")]
        public string surname
        {
            get;
            set;
        }
    }
}