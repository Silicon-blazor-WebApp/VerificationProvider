using Microsoft.AspNetCore.Http;
using VerificationProvider.Models;

namespace VerificationProvider.Services
{
    public interface IValidateVerificationCodeService
    {
        Task<ValidateRequest> UnpackValidateRequest(HttpRequest req);
        Task<bool> ValdateCodeAsync(ValidateRequest validateRequest);
    }
}