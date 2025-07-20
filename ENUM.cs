namespace Integration_System
{
    public class ENUM
    {
        public enum InsertEmployeeResult
        {
            Success,
            EmailAlreadyExists,
            InvalidDepartment,
            InvalidPosition,
            Failed
        } //enum trong C# là một kiểu dữ liệu đặc biệt dùng để định nghĩa tập hợp các hằng số có tên,

        public enum Role
        {
            HR,
            PayrollManagement,
            Employee
        } 
    }
}
