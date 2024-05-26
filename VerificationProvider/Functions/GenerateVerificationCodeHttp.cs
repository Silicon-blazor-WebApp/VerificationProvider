using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VerificationProvider.Models;
using VerificationProvider.Services;

namespace VerificationProvider.Functions
{
    public class GenerateVerificationCodeHttp(ILogger<GenerateVerificationCodeHttp> logger, IVerificationService verificationService, ServiceBusClient serviceBusClient)
    {
        private readonly ILogger<GenerateVerificationCodeHttp> _logger = logger;
        private readonly IVerificationService _verificationService = verificationService;
        private readonly ServiceBusClient _serviceBusClient = serviceBusClient;
        private readonly string _queueName = "email_request";

        [Function("GenerateVerificationCodeHttp")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var verificationRequest = JsonConvert.DeserializeObject<VerificationRequest>(requestBody);

                if (verificationRequest == null || string.IsNullOrEmpty(verificationRequest.Email))
                {
                    return new BadRequestObjectResult("Please pass a valid verification request with an email.");
                }

                var code = _verificationService.GenerateCode();
                if (!string.IsNullOrEmpty(code))
                {
                    var result = await _verificationService.SaveVerificationRequest(verificationRequest, code);
                    if (result)
                    {
                        var emailRequest = _verificationService.GenerateEmailRequest(verificationRequest, code);
                        if (emailRequest != null)
                        {
                            var payload = _verificationService.GenerateServiceBusEmailRequest(emailRequest);
                            if (!string.IsNullOrEmpty(payload))
                            {
                                await SendMessageToServiceBus(payload);
                                return new OkObjectResult($"Verification code sent to {verificationRequest.Email}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR : GenerateVerificationCodeHttp.Run() :: {ex.Message}");

            }

            return null!;
        }

        private async Task SendMessageToServiceBus(string payload)
        {
            ServiceBusSender sender = _serviceBusClient.CreateSender(_queueName);
            ServiceBusMessage message = new ServiceBusMessage(payload);
            await sender.SendMessageAsync(message);
        }
    }
}
