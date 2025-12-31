using Newtonsoft.Json;
using System.IO;
using Automated_Employee_Attendance_System.Models;

namespace Automated_Employee_Attendance_System.Services
{
    public static class DeviceService
    {
        private static string file = "Savers/device.json";

        public static Device? Load()
        {
            if (!File.Exists(file))
                return null;

            string json = File.ReadAllText(file);
            return JsonConvert.DeserializeObject<Device>(json);
        }

        public static void Save(Device device)
        {
            Directory.CreateDirectory("Savers");
            string json = JsonConvert.SerializeObject(device, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(file, json);
        }

        public static void Delete()
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }
}
