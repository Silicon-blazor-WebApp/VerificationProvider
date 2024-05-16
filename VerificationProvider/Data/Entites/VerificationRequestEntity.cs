
using System.ComponentModel.DataAnnotations;

namespace VerificationProvider.Data.Entites
{
    public class VerificationRequestEntity
    {
        [Key]
        public string Email { get; set; } = null!;
        public string Code { get; set; } = null!;
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddMinutes(5);
    }
}
