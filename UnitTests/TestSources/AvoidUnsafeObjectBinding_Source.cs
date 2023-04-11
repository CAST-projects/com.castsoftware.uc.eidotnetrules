using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Data.Entity;
using System.Web.ModelBinding;
using System.ComponentModel;

namespace UnitTests.UnitTest.Sources
{
    public class AvoidUnsafeObjectBinding_Source
    {

    }

    public class UserDbcontext : DbContext
    {

        public DbSet<User> Users { get; set; }
        public DbSet<string> Names { get; set; }

    }

    public class UserController : Controller
    {
        private UserDbcontext _db = new UserDbcontext();
        [HttpPost]
        public ActionResult Create(User user) // Violation
        {
            if (ModelState.IsValid)
            {
                _db.Users.Add(user);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View();
        }

        public ActionResult UpdateUser(User user) // No Violation
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateUserAccount(User user) //Violation
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            try
            {
                using (UserDbcontext db = new UserDbcontext())
                {
                    db.Users.Attach(user);
                    await db.SaveChangesAsync();
                    return View("Index");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error when saving data", ex);
                return View();
            }
        }

        [HttpPost]
        public ActionResult Create2(UserDto userDto) // No Violation
        {
            if (ModelState.IsValid)
            {
                User user = new User();
                user.UserName = userDto.UserName;
                user.UserAge = userDto.UserAge;
                _db.Users.Add(user);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public ActionResult Edit()  //Violation
        {
            if (ModelState.IsValid)
            {
                var user = new User();
                TryUpdateModel(user);
                _db.Users.Add(user);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public ActionResult Edit2()  //Violation
        {
            if (ModelState.IsValid)
            {
                var user = new User();
                UpdateModel(user);
                _db.Users.Add(user);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View();
        }
    }

    public class User
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public int UserAge { get; set; }
        public int UserId { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class UserDto
    {
        public string UserName { get; set; }
        public int UserAge { get; set; }
    }

    public class EmployeeController : Controller
    {
        private EmployeeDbcontext _db = new EmployeeDbcontext();

        [HttpPost]
        [ActionName("Edit")]
        public ActionResult Edit_Post(int id)
        {
            var employee = _db.Employees.Where(_ => _.ID == id).FirstOrDefault();
            if (employee != null)
            {
                UpdateModel<IEmployee>(employee); // No violation
                if (ModelState.IsValid)
                {
                    _db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            return View();
        }

        [HttpPost]
        [ActionName("Create")]
        public ActionResult Create_Post([Bind(Include = "Gender, City, Salary, DateOfBirth")] Employee employee) // No violation
        {
            if (ModelState.IsValid)
            {
                _db.Employees.Add(employee);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        [ActionName("Create2")]
        public ActionResult Create2_Post() // No violation
        {
            if (ModelState.IsValid)
            {
                var employee = new Employee();
                TryUpdateModel(employee, includeProperties: new[] { "Gender, City, Salary, DateOfBirth" });
                _db.Employees.Add(employee);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        [ActionName("Create3")]
        public ActionResult Create3_Post() // No violation
        {
            if (ModelState.IsValid)
            {
                var employee = new Employee();
                TryUpdateModel(employee, new[] { "Gender, City, Salary, DateOfBirth" });
                _db.Employees.Add(employee);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        [ActionName("Create4")]
        public ActionResult Create4_Post() // No violation
        {
            if (ModelState.IsValid)
            {
                var employee = new Employee();
                var includeProperties = new[] { "Gender, City, Salary, DateOfBirth" };
                TryUpdateModel(employee, includeProperties);
                _db.Employees.Add(employee);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View();
        }
    }

    public class EmployeeDbcontext : DbContext
    {
        public DbSet<Employee> Employees { get; set; }
    }

    public interface IEmployee
    {
        int ID { get; set; }
        string Gender { get; set; }
        string City { get; set; }
        decimal Salary { get; set; }
        DateTime DateOfBirth { get; set; }
    }
    // Step 2: Make "Employee" class inherit from IEmployee interface
    public class Employee : IEmployee
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public string City { get; set; }
        public decimal Salary { get; set; }
        public DateTime DateOfBirth { get; set; }
    }



    public class CustomerController : Controller
    {
        private CustomerDbcontext _db = new CustomerDbcontext();

        [HttpPost]
        [ActionName("Edit")]
        public ActionResult Edit_Post(int id) // No violation because data model is protected by attribute BindNever
        {
            var customer = _db.Customers.Where(_ => _.Id == id).FirstOrDefault();
            if (customer != null)
            {
                UpdateModel(customer); 
                if (ModelState.IsValid)
                {
                    _db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            return View();
        }

        [HttpPost]
        [ActionName("Create")]
        public ActionResult Create_Post( Customer customer) // No violation because data model is protected by attribute BindNever
        {
            if (ModelState.IsValid)
            {
                _db.Customers.Add(customer);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View();
        }

    }

    public class CustomerDbcontext : DbContext
    {
        public DbSet<Customer> Customers { get; set; }
    }

    
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime? Birthdate { get; set; }
        [BindNever]
        public string Password { get; set; }
        [ReadOnly(true)]
        public bool IsAdmin { get; set; }
    }

}
