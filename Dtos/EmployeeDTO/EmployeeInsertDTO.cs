using System.ComponentModel.DataAnnotations;

namespace Integration_System.Dtos.EmployeeDTO
{
    public class EmployeeInsertDTO
    {
        ////////////////////
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string FullName { get; set; }
        ////////////////////
        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        [DataType(DataType.Date)]
        public DateTime DateofBirth { get; set; }
        ////////////////////
        public string? Gender { get; set; }
        ////////////////////
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(10, ErrorMessage = "Số điện thoại không được vượt quá 10 ký tự")]
        public string? PhoneNumber { get; set; } = "null";
        ////////////////////
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string? Email { get; set; } = "null";

        ////////////////////
        [Required(ErrorMessage = "Ngày vào làm là bắt buộc")]
        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; }
        ////////////////////
        [Range(1, int.MaxValue, ErrorMessage = "ID phòng ban không hợp lệ")]
        public int DepartmentId { get; set; }
        ////////////////////
        [Range(1, int.MaxValue, ErrorMessage = "ID chức vụ không hợp lệ")]
        public int? PositionId { get; set; }
        ////////////////////
        [StringLength(50, ErrorMessage = "Trạng thái không được vượt quá 50 ký tự")]
        public string? Status { get; set; } = "null";
    }
}
