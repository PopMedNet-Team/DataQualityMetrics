using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ASPE.DQM.Controllers
{
    [Route("api/[controller]")]
    public class AuthenticationController : Controller
    {

        readonly UserManager<Identity.IdentityUser> _userManager;
        readonly SignInManager<Identity.IdentityUser> _signInManager;
        readonly ILogger<AuthenticationController> _logger;
        readonly IConfiguration _config;

        public AuthenticationController(UserManager<Identity.IdentityUser> userManager, SignInManager<Identity.IdentityUser> signInManager, ILogger<AuthenticationController> logger, IConfiguration config)
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._logger = logger;
            this._config = config;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AuthenticationRequest user)
        {
            if (string.IsNullOrEmpty(user.username))
            {
                ModelState.AddModelError("username", "Username is required.");
            }

            if (string.IsNullOrEmpty(user.password))
            {
                ModelState.AddModelError("password", "Password is required.");
            }

            if(ModelState.ErrorCount > 0)
            {
                _logger.LogDebug("Authentication failed for user: {0}", string.IsNullOrEmpty(user.username) ? "<unknown>" : user.username);
                return BadRequest(ModelState);
            }

            var result = await _signInManager.PasswordSignInAsync(user.username, user.password, false, false);

            if (!result.Succeeded)
            {
                _logger.LogDebug("Authentication failed for user: {0}", string.IsNullOrEmpty(user.username) ? "<unknown>" : user.username);
                ModelState.AddModelError("error", "Authentication failed, please reconfirm your username and password.");
                return BadRequest(ModelState);
            }

            _logger.LogDebug("Authentication successful for user: {0}", user.username);

            var cookie = Crypto.EncryptStringAES(string.Format("{0}:{1}:{2}", user.username, user.password, Guid.NewGuid()), _config["PMNoAuthHash"], _config["PMNoAuthKey"]);
            var cookieOptions = new CookieOptions()
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = true                
            };
            Response.Cookies.Append("DQM-User", cookie, cookieOptions);

            return Ok();
        }

        [Authorize]
        [HttpPost("signout")]
        public async Task<IActionResult> SignOut()
        {
            if (User.Identity.IsAuthenticated)
            {
                _logger.LogDebug("Signing out for user: {0}", User.Identity.Name);
            }

            await _signInManager.SignOutAsync();

            var cookieOptions = new CookieOptions()
            {
                Expires = DateTimeOffset.Now.AddDays(-1)
            };

            Response.Cookies.Append("DQM-User", "", cookieOptions);

            return Ok();
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequest registration)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_config["PMNApiUrl"]);

                    var contentString = JsonConvert.SerializeObject(registration);
                    var sContent = new StringContent(contentString, Encoding.UTF8, "application/json");

                    var res = await client.PostAsync("/Users/UserRegistration", sContent);

                    if (res.IsSuccessStatusCode)
                    {
                        return Ok();
                    }
                    else
                    {
                        var errorMessage = JsonConvert.DeserializeObject<Models.PMNErrorResponse>(await res.Content.ReadAsStringAsync());

                        if (errorMessage.Errors.Count() > 0)
                        {
                            ModelState.AddModelError("error", errorMessage.Errors.FirstOrDefault().Description);
                            return BadRequest(ModelState);
                        }
                        else
                        {
                            ModelState.AddModelError("error", "An Error Occured");
                            return BadRequest();
                        }                        
                    }
                    
                }
            }
            catch (HttpRequestException hex)
            {
                if(hex.InnerException is SocketException)
                {
                    ModelState.AddModelError("error", "PopMedNet could not be reached to submit the registration.");
                    return BadRequest(ModelState);
                }
                else
                {
                    return BadRequest(hex);
                }
                
            }
            catch (Exception ex)
            {

                return BadRequest(ex);
            }
        }

        [HttpGet("Profile")]
        public async Task<IActionResult> Profile()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);

                var claims = (await _userManager.GetClaimsAsync(user)).ToDictionary(cl => cl.Type, cl => cl.Value);

                return new JsonResult(new UserProfileResponse(user, claims));
            }
            else
            {
                return new JsonResult(new UserProfileResponse());
            }
        }

        public class UserProfileResponse
        {
            public UserProfileResponse() { }
            public UserProfileResponse(Identity.IdentityUser user, Dictionary<string,string> claims)
            {
                id = user.Id;
                username = user.UserName;
                email = user.Email;
                phonenumber = user.PhoneNumber;
                firstname = claims.GetValueOrDefault(DQM.Identity.Claims.FirstName_Key);
                lastname = claims.GetValueOrDefault(DQM.Identity.Claims.LastName_Key);
                organization = claims.GetValueOrDefault(DQM.Identity.Claims.Organization_Key);
                canAuthorMetric = claims.ContainsKey(DQM.Identity.Claims.AuthorMetric_Key);
                canSubmitMeasures = claims.ContainsKey(DQM.Identity.Claims.SubmitMeasure_Key);
                isSystemAdministrator = claims.ContainsKey(DQM.Identity.Claims.SystemAdministrator_Key);
            }

            public Guid id { get; set; }
            public string firstname { get; set; }
            public string lastname { get; set; }
            public string username { get; set; }
            public string email { get; set; }
            public string phonenumber { get; set; }
            public string organization { get; set; }

            public bool canAuthorMetric { get; set; }

            public bool canSubmitMeasures { get; set; }

            public bool isSystemAdministrator { get; set; }
        }

        public class AuthenticationRequest
        {
            public string username { get; set; }

            public string password { get; set; }
        }

        public class UserRegistrationRequest
        {
            public string firstName { get; set; }
            public string middleName { get; set; }
            public string lastName { get; set; }
            public string title { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
            public string fax { get; set; }
            public string organizationRequested { get; set; }
            public string roleRequested { get; set; }
            public string username { get; set; }
            public string password { get; set; }
            public string confirmPassword { get; set; }
        }
    }
}
