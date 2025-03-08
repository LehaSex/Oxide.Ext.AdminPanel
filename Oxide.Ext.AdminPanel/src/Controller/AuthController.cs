using System;
using System.Threading.Tasks;
using System.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Oxide.Ext.AdminPanel
{
    public class AuthController : Controller
    {
        private readonly ILogger _logger;
        private readonly string _secretKey;

        public AuthController(IFileSystem fileSystem, string htmlPath, IResponseHelper responseHelper, ILogger logger, string secretKey)
            : base(fileSystem, htmlPath, responseHelper) // Передаем параметры в базовый класс
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _secretKey = secretKey ?? throw new ArgumentNullException(nameof(secretKey));
        }

        public async Task HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod == "POST")
            {
                string username = request.QueryString["username"];
                string password = request.QueryString["password"];

                if (AuthenticateUser(username, password))
                {
                    var token = GenerateJwtToken(username);
                    response.StatusCode = (int)HttpStatusCode.OK;
                    await Get_responseHelper().ServeContentAsync(response, Encoding.UTF8.GetBytes(token), "text/plain");
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await Get_responseHelper().ServeContentAsync(response, Encoding.UTF8.GetBytes("Invalid credentials"), "text/plain");
                }
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                await Get_responseHelper().ServeContentAsync(response, Encoding.UTF8.GetBytes("Method not allowed"), "text/plain");
            }
        }

        private bool AuthenticateUser(string username, string password)
        {
            // Пример простой аутентификации
            return username == "admin" && password == "password";
        }

        private string GenerateJwtToken(string username)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "your_issuer",
                audience: "your_audience",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
