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
        private readonly string _secretKey; // Секретный ключ для проверки токена
        private readonly string _loginPath; // Путь к странице авторизации

        public JwtAuthMiddleware(ILogger logger, string secretKey)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _secretKey = secretKey ?? throw new ArgumentNullException(nameof(secretKey));
            _loginPath = "/adminpanel/"; // Путь к странице авторизации
        }

        public async Task InvokeAsync(HttpListenerContext context, Func<Task> next)
        {
            var request = context.Request;
            var response = context.Response;

            // Пропускаем проверку для страницы авторизации
            if (request.Url.AbsolutePath.Equals(_loginPath, StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            // Проверяем наличие токена в заголовках
            string token = request.Headers["Authorization"]?.Replace("Bearer ", "");

            if (!string.IsNullOrEmpty(token) && ValidateJwtToken(token))
            {
                // Пользователь авторизован, продолжаем выполнение
                await next();
            }
            else
            {
                // Пользователь не авторизован, перенаправляем на страницу авторизации
                response.Redirect(_loginPath);
                response.Close();
            }
        }

        private bool ValidateJwtToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secretKey);

                // Настраиваем параметры валидации токена
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                // Пытаемся распарсить и валидировать токен
                SecurityToken validatedToken;
                tokenHandler.ValidateToken(token, validationParameters, out validatedToken);

                return true; // Токен валиден
            }
            catch (Exception ex)
            {
                _logger.LogError($"JWT validation failed: {ex.Message}");
                return false; // Токен невалиден
            }
        }
    }
}
