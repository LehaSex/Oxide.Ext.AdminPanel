using System;
using System.Threading.Tasks;
using System.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Oxide.Ext.AdminPanel
{
    public class JwtAuthMiddleware : IMiddleware
    {
        private readonly ILogger _logger;
        private readonly string _secretKey; 
        private readonly string _loginPath; 

        public JwtAuthMiddleware(ILogger logger, string secretKey)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _secretKey = secretKey ?? throw new ArgumentNullException(nameof(secretKey));
            _loginPath = "/adminpanel/"; 
        }

        public async Task InvokeAsync(HttpListenerContext context, Func<Task> next)
        {
            var request = context.Request;
            var response = context.Response;

            // skip check for auth page
            if (request.Url.AbsolutePath.Equals(_loginPath, StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            // check token in Headers
            string? token = request.Headers["Authorization"]?.Replace("Bearer ", "");

            if (!string.IsNullOrEmpty(token) && ValidateJwtToken(token))
            {
                // auth confirmed
                await next();
            }
            else
            {
                // redirect to auth
                response.Redirect(_loginPath);
                response.Close();
            }
        }

        private bool ValidateJwtToken(string? token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                SecurityToken validatedToken;
                tokenHandler.ValidateToken(token, validationParameters, out validatedToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"JWT validation failed: {ex.Message}");
                return false;
            }
        }
    }
}
