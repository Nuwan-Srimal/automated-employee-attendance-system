using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Automated_Employee_Attendance_System.Services;
using Automated_Employee_Attendance_System.Models;

namespace Automated_Employee_Attendance_System
{
    public partial class AttendanceView : UserControl, INotifyPropertyChanged
    {
        private ESP_Services _espServices;
        private List<Attendance> _allAttendanceRecords;

        private int _totalEmployees;
        public int TotalEmployees
        {
            get => _totalEmployees;
            set
            {
                _totalEmployees = value;
                OnPropertyChanged();
            }
        }

        private int _presentToday;
        public int PresentToday
        {
            get => _presentToday;
            set
            {
                _presentToday = value;
                OnPropertyChanged();
            }
        }

        private int _absentToday;
        public int AbsentToday
        {
            get => _absentToday;
            set
            {
                _absentToday = value;
                OnPropertyChanged();
            }
        }

        public AttendanceView()
        {
            InitializeComponent();
            DataContext = this;

            _espServices = new ESP_Services();
            _allAttendanceRecords = new List<Attendance>();

            Loaded += AttendanceView_Loaded;
        }

        private async void AttendanceView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAttendanceData();
        }

        private async Task LoadAttendanceData()
        {
            try
            {
                List<Attendance> attendanceRecords = null;

                await _espServices.ConnectToSavedDevice();

                // ✅ TRY TO LOAD FROM ESP FIRST
                if (!string.IsNullOrEmpty(_espServices.espBaseUrl))
                {
                    try
                    {
                        attendanceRecords = await _espServices.GetAttendanceRecords();

                        // ✅ SYNC ESP DATA TO DATABASE
                        if (attendanceRecords != null && attendanceRecords.Count > 0)
                        {
                            DatabaseService.SaveAttendanceBulk(attendanceRecords);
                            SystemServices.Log($"Loaded {attendanceRecords.Count} attendance records from ESP and synced to database");
                        }
                    }
                    catch (Exception ex)
                    {
                        SystemServices.Log($"ESP attendance load failed, loading from database: {ex.Message}");
                    }
                }

                // ✅ FALLBACK TO DATABASE IF ESP FAILED OR NOT CONNECTED
                if (attendanceRecords == null || attendanceRecords.Count == 0)
                {
                    attendanceRecords = DatabaseService.GetAllAttendance();
                    SystemServices.Log($"Loaded {attendanceRecords.Count} attendance records from database (ESP not available)");
                }

                _allAttendanceRecords = attendanceRecords;
                AttendanceGrid.ItemsSource = _allAttendanceRecords;

                // ✅ UPDATE STATISTICS
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                SystemServices.Log($"Load attendance error: {ex.Message}");
                _allAttendanceRecords = new List<Attendance>();
                AttendanceGrid.ItemsSource = _allAttendanceRecords;
                UpdateStatistics();
            }
        }

        private void UpdateStatistics()
        {
            try
            {
                // Get all employees
                var allEmployees = DatabaseService.GetAllEmployees();
                TotalEmployees = allEmployees.Count;

                // Get today's date in the same format as your attendance records
                string today = DateTime.Now.ToString("yyyy-MM-dd");

                // Get attendance for today
                var todayAttendance = DatabaseService.GetAttendanceByDate(today);

                // Get unique employees who attended today
                var presentEmployeeIds = todayAttendance
                    .Select(a => a.emp_id)
                    .Distinct()
                    .ToList();

                PresentToday = presentEmployeeIds.Count;
                AbsentToday = TotalEmployees - PresentToday;

                SystemServices.Log($"Stats updated - Total: {TotalEmployees}, Present: {PresentToday}, Absent: {AbsentToday}");
            }
            catch (Exception ex)
            {
                SystemServices.Log($"Update statistics error: {ex.Message}");
                TotalEmployees = 0;
                PresentToday = 0;
                AbsentToday = 0;
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filteredRecords = _allAttendanceRecords.AsEnumerable();

                // Filter by date range
                if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
                {
                    DateTime startDate = StartDatePicker.SelectedDate.Value;
                    DateTime endDate = EndDatePicker.SelectedDate.Value;

                    filteredRecords = filteredRecords.Where(a =>
                    {
                        if (DateTime.TryParse(a.date, out DateTime recordDate))
                        {
                            return recordDate >= startDate && recordDate <= endDate;
                        }
                        return false;
                    });
                }
                else if (StartDatePicker.SelectedDate.HasValue)
                {
                    DateTime startDate = StartDatePicker.SelectedDate.Value;
                    filteredRecords = filteredRecords.Where(a =>
                    {
                        if (DateTime.TryParse(a.date, out DateTime recordDate))
                        {
                            return recordDate >= startDate;
                        }
                        return false;
                    });
                }
                else if (EndDatePicker.SelectedDate.HasValue)
                {
                    DateTime endDate = EndDatePicker.SelectedDate.Value;
                    filteredRecords = filteredRecords.Where(a =>
                    {
                        if (DateTime.TryParse(a.date, out DateTime recordDate))
                        {
                            return recordDate <= endDate;
                        }
                        return false;
                    });
                }

                // Filter by search text
                string searchText = TxtSearch.Text?.Trim().ToLower();
                if (!string.IsNullOrEmpty(searchText))
                {
                    filteredRecords = filteredRecords.Where(a =>
                        a.emp_id.ToString().Contains(searchText) ||
                        a.finger_id.ToString().Contains(searchText) ||
                        a.date.ToLower().Contains(searchText) ||
                        a.time.ToLower().Contains(searchText)
                    );
                }

                AttendanceGrid.ItemsSource = filteredRecords.ToList();
                SystemServices.Log($"Filters applied - {filteredRecords.Count()} records found");
            }
            catch (Exception ex)
            {
                SystemServices.Log($"Apply filters error: {ex.Message}");
            }
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SearchAttendance_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            // Clear all filter controls
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;
            TxtSearch.Text = string.Empty;

            // Reset to show all records
            AttendanceGrid.ItemsSource = _allAttendanceRecords;
            SystemServices.Log("All filters cleared");
        }

        private void DatePicker_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private async void RefreshAttendance_Click(object sender, RoutedEventArgs e)
        {
            // Clear filters before refreshing
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;
            TxtSearch.Text = string.Empty;

            await LoadAttendanceData();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}