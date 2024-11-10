using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OrderPayment.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(256, ErrorMessage = "Password cannot be longer than 256 characters.")]
        public string PasswordHash { get; set; } = string.Empty;  // Şifreyi düz metin yerine hash'li olarak saklayacağız

        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string? PhoneNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public bool IsActive { get; set; } = false;  // Kullanıcının aktif olup olmadığını belirten alan

        // Verification Code için ilişki
        [JsonIgnore]
        public List<VerificationCode> VerificationCodes { get; set; } = new List<VerificationCode>();

        // Foreign Key - One-to-Many relation with Orders
        public List<Order> Orders { get; set; } = new List<Order>();
    }
}
