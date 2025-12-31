namespace Automated_Employee_Attendance_System.Models
{
    public class Device
    {
        public string Name { get; set; } = "ESP Device";
        public string IpAddress { get; set; } = "";
        public string DeviceId { get; set; } = ""; // Unique device identifier
        public string Mode { get; set; } = "STA"; // AP or STA
        public bool IsConnected { get; set; } = false;
    }
}
