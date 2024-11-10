using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OrderPayment.Models;

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
    public string PasswordHash { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number format.")]
    public string? PhoneNumber { get; set; } = string.Empty;


    [Required]
    [DataType(DataType.Date)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(6, ErrorMessage = "Verification code must be 6 characters.")]
    public string? VerificationCode { get; set; } // Verification code for user validation

    [Required]
    [DataType(DataType.Date)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Key - One-to-Many relation with Orders
    public List<Order> Orders { get; set; } = new List<Order>();
}
