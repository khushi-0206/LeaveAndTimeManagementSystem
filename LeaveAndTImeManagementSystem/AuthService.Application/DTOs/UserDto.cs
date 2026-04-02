using System;
using System.Collections.Generic;
using System.Text;

namespace AuthService.Application.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public int? ManagerId { get; set; }
        public string EmploymentType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? ProfilePhoto { get; set; }
        public string? Phone { get; set; }
        public DateTime DateOfJoining { get; set; }
    }
}
