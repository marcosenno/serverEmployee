using EmployeeMonitoringWS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EmployeeMonitoringWS.Controllers
{
    public class EmployeeController : Controller
    {
        private EmployeeContext employeeContext = new EmployeeContext();

        // GET: Employee
        public ActionResult ListAll()
        {
            var employees = employeeContext.Employees.ToList();
            return View(employees);
        }

        // GET: Employee/Details/5
        public ActionResult Details(int id)
        {
            Employee employee = employeeContext.Employees.Find(id);
            if (employee == null)
                return HttpNotFound();

            return View(employee);
        }

        // GET: Employee/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Employee/Create
        [HttpPost]
        public ActionResult Create(Employee employee)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    employeeContext.Employees.Add(employee);
                    employeeContext.SaveChanges();
                    return RedirectToAction("ListAll");
                }
                return View(employee);
            }
            catch
            {
                return View();
            }
        }

        // GET: Employee/Edit/5
        public ActionResult Edit(int id)
        {
            Employee employee = employeeContext.Employees.Find(id);
            if (employee == null)
                return HttpNotFound();

            return View(employee);
        }

        // POST: Employee/Edit/5
        [HttpPost]
        public ActionResult Edit(Employee employee)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    employeeContext.Entry(employee).State = System.Data.Entity.EntityState.Modified;
                    employeeContext.SaveChanges();
                    return RedirectToAction("ListAll");
                }
                return View(employee);
            }
            catch
            {
                return View();
            }
        }

        // GET: Employee/Delete/5
        public ActionResult Delete(int id)
        {
            Employee employee = employeeContext.Employees.Find(id);
            if (employee == null)
                return HttpNotFound();

            return View(employee);
        }

        // POST: Employee/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    Employee employee = employeeContext.Employees.Find(id);
                    if (employee == null)
                        return HttpNotFound();

                    employeeContext.Employees.Remove(employee);
                    employeeContext.SaveChanges();
                    return RedirectToAction("ListAll");
                }
                return View();
            }
            catch
            {
                return View();
            }
        }
    }
}
