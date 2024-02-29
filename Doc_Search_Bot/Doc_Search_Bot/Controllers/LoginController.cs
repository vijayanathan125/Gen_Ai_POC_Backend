using Microsoft.AspNetCore.Mvc;
using Query_Quasar_Bot_API.Models;
using System;

namespace Query_Quasar_Bot_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        [HttpPost]
        public IActionResult Login(LoginRequest request)
        {
            try
            {
                // Hardcoded credentials
                const string adminEmail = "admin@example.com";
                const string adminPassword = "adminpassword";
                const string userEmail = "user@example.com";
                const string userPassword = "userpassword";

                // Validate request
                if (string.IsNullOrEmpty(request.UserMail) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "UserMail and Password are required." });
                }

                // Check if the provided credentials match the hardcoded values
                bool isAdmin = false;

                if (request.UserMail == adminEmail && request.Password == adminPassword)
                {
                    isAdmin = true;
                }
                else if (request.UserMail == userEmail && request.Password == userPassword)
                {
                    // No need to set isAdmin = false here as it's already initialized to false
                }
                else
                {
                    // Return unauthorized if credentials do not match
                    return Unauthorized(new { message = "Invalid credentials." });
                }

                // Return the user's role and whether they are admin
                return Ok(new { message = "Login successful.", isAdmin });
            }
            catch (Exception ex)
            {
                // Return internal server error if an exception occurs
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
