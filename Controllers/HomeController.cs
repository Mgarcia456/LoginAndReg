using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LoginAndReg.Models;
// This is needed for the password hasher
using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Mvc.Filters;

namespace LoginAndReg.Controllers;

public class HomeController : Controller
{
    private MyContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger, MyContext context)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost("users/create")]
    public IActionResult CreateUser(User newUser)
    {
        if(ModelState.IsValid)
        {
            // Hash the password
            // Initializing a PasswordHasher object, providing our User class as its type
            PasswordHasher<User> Hashbrown = new PasswordHasher<User>();
            // Updating our newUser's password to a hashed version
            newUser.Password = Hashbrown.HashPassword(newUser, newUser.Password);
            //Save your user object to the database 
            _context.Add(newUser);
            _context.SaveChanges();
            // put UserId into session
            HttpContext.Session.SetInt32("UserId", newUser.UserId);

            return RedirectToAction("Success");
        } else {
            return View("Index");
        }
    }

    [SessionCheck]
    [HttpGet(("success"))]
    public IActionResult Success()
    {
        return View();
    }

    [HttpPost("users/login")]
    public IActionResult LoginUser(LogUser loginUser)
    {
        if(ModelState.IsValid)
        {
            // lookup user in db
            User? userInDb = _context.Users.FirstOrDefault(u => u.Email == loginUser.LEmail);
            // verify user exists
            if(userInDb == null)
            {
                ModelState.AddModelError("LEmail", "Invalid Email/Password");
                return View("Index");
            }
            // verify the password matches db
            PasswordHasher<LogUser> hashbrown = new PasswordHasher<LogUser>();
            var result = hashbrown.VerifyHashedPassword(loginUser, userInDb.Password, loginUser.LPassword);
            if(result == 0)
            {
                // failure, incorrect password used
                ModelState.AddModelError("LEmail", "Invalid Email/Password");
                return View("Index");
            } else {
                // set session and route to success
                HttpContext.Session.SetInt32("UserId", userInDb.UserId);
                return RedirectToAction("Success");
            }
        } else {
            return View("Index");
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

// Name this anything you want with the word "Attribute" at the end
public class SessionCheckAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Find the session, but remember it may be null so we need int?
        int? userId = context.HttpContext.Session.GetInt32("UserId");
        // Check to see if we got back null
        if(userId == null)
        {
            // Redirect to the Index page if there was nothing in session
            // "Home" here is referring to "HomeController", you can use any controller that is appropriate here
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }
}