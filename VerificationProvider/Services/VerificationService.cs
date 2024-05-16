using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VerificationProvider.Data.Context;
using VerificationProvider.Models;

namespace VerificationProvider.Services
{
    public class VerificationService(ILogger<VerificationService> logger, IServiceProvider serviceProvider) : IVerificationService
    {
        private readonly ILogger<VerificationService> _logger = logger;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public VerificationRequest UnpackVerificationRequest(ServiceBusReceivedMessage message)
        {
            try
            {
                var verificationRequest = JsonConvert.DeserializeObject<VerificationRequest>(message.Body.ToString());
                if (verificationRequest != null && !string.IsNullOrEmpty(verificationRequest.Email))
                {
                    return verificationRequest;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR : GenerateVerificationCode.UnpackVerificationRequest() :: {ex.Message}");
            }
            return null!;
        }

        public string GenerateCode()
        {
            try
            {
                var rnd = new Random();
                var code = rnd.Next(100000, 999999);
                return code.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR : GenerateVerificationCode.GenerateCode() :: {ex.Message}");
            }
            return null!;
        }

        public EmailRequest GenerateEmailRequest(VerificationRequest verificationRequest, string code)
        {
            try
            {
                if (!string.IsNullOrEmpty(verificationRequest.Email) && !string.IsNullOrEmpty(code))
                {
                    var emailRequest = new EmailRequest()
                    {
                        To = verificationRequest.Email,
                        Subject = $"Verification Code {code}",
                        HtmlBody = $@"
                       <html lang='en'>
                        <head>
                            <meta charset='UTF-8'>
                            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                            <title></title>
                        </head>
                        <body>
                          <div style='border: 1px solid #9397AD; width: 30rem;'>
                            <div style='background-color: #4C82F7; padding: 2rem;'>
                              <h1 style='color: #F3F6FF; display: flex; justify-content: center; align-items: center;' >Verification Code</h1>
                            </div>
                            <div style='padding: 3rem; display: flex; flex-direction: column; align-items: center;'>
                              <p style='text-align: center;'>We have received a request to sign in to your account using email {verificationRequest.Email} </p>
                              <h3 style='margin: 1rem;'>Verification Code:</h3>
                              <h1 style='margin: 0; letter-spacing: 10px; background-color: #9397AD; color: #ffffff; padding: 1rem; border-radius: 10px;'>{code}</h1>
                              <p style='margin-top: 2rem; text-align: center;'>To complete the verification process, simply enter this code when prompted. If you did not initiate this request, please contact our support team immediately.</p>
                              <p>&copy; 2024 Silicon. All rights reserverd.</p>
                            </div>
                          </div>
                        </body>
                       </html>",
                        PlainText = $"We have received a request to sign in to your account using email {verificationRequest.Email}. Please verify your account using this code: {code}",
                    };
                    return emailRequest;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR : GenerateVerificationCode.GenerateEmailRequest() :: {ex.Message}");
            }
            return null!;
        }

        public async Task<bool> SaveVerificationRequest(VerificationRequest verificationRequest, string code)
        {
            try
            {
                using var context = _serviceProvider.GetRequiredService<DataContext>();

                var existingRequest = await context.VerificationRequests.FirstOrDefaultAsync(x => x.Email == verificationRequest.Email);
                if (existingRequest != null)
                {
                    existingRequest.Code = code;
                    existingRequest.ExpiryDate = DateTime.Now.AddMinutes(5);
                    context.Entry(existingRequest).State = EntityState.Modified;
                }
                else
                {
                    context.VerificationRequests.Add(new Data.Entites.VerificationRequestEntity() { Email = verificationRequest.Email, Code = code });
                }
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR : GenerateVerificationCode.SaveVerificationRequest() :: {ex.Message}");
            }
            return false;
        }
        public string GenerateServiceBusEmailRequest(EmailRequest emailRequest)
        {
            try
            {
                var payload = JsonConvert.SerializeObject(emailRequest);
                if (!string.IsNullOrEmpty(payload))
                {
                    return payload;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR : GenerateVerificationCode.GenerateServiceBusEmailRequest() :: {ex.Message}");
            }
            return null!;
        }
    }
}
