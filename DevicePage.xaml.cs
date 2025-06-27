using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using LoginApp.Service;
using Microsoft.Maui.Controls;

namespace LoginApp
{
    public partial class DevicePage : ContentPage
    {
        // Model cho một lịch trình thiết bị
        public class DeviceSchedule : INotifyPropertyChanged
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Device { get; set; } // "PUMP", "LIGHT", "VALVE"
            public string Value { get; set; }  // "on"/"off"
            public DateTime Time { get; set; }
            public string Action { get; set; } = "schedule";
            public string Status { get; set; } = "scheduled";

            public string DisplayText =>
                $"{ToVietnamese(Device)} {(Value == "on" ? "bật" : "tắt")} lúc {Time:yyyy-MM-dd HH:mm:ss}";

            // Command xoá cho CollectionView
            public Command DeleteCommand { get; set; }

            private string ToVietnamese(string device) =>
                device switch
                {
                    "PUMP" => "Bơm",
                    "LIGHT" => "Đèn",
                    "VALVE" => "Van nước",
                    _ => device
                };

            public event PropertyChangedEventHandler PropertyChanged;
        }

        private readonly MQTTControlService _mqttService;
        private ObservableCollection<DeviceSchedule> _scheduleList = new();

        // Trạng thái hiện tại của các thiết bị (true: bật, false: tắt)
        private bool isPumpOn = false;
        private bool isLightOn = false;
        private bool isValveOn = false;

        public DevicePage()
        {
            InitializeComponent();
            _mqttService = new MQTTControlService();
            ScheduleListView.ItemsSource = _scheduleList;
            UpdateDeviceButtonStates();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!_mqttService.IsConnected)
                await _mqttService.StartAsync();
        }

        // ====== 1. Bật/tắt thiết bị ngay lập tức ======

        private async void OnPumpButtonClicked(object sender, EventArgs e)
        {
            if (!isPumpOn)
            {
                await _mqttService.SendDeviceCommandAsync("PUMP", true, "", "", "");
                isPumpOn = true;
            }
            else
            {
                await _mqttService.SendDeviceCommandAsync("PUMP", false, "", "", "");
                isPumpOn = false;
            }
            UpdateDeviceButtonStates();
        }

        private async void OnLightButtonClicked(object sender, EventArgs e)
        {
            if (!isLightOn)
            {
                await _mqttService.SendDeviceCommandAsync("LIGHT", true, "", "", "");
                isLightOn = true;
            }
            else
            {
                await _mqttService.SendDeviceCommandAsync("LIGHT", false, "", "", "");
                isLightOn = false;
            }
            UpdateDeviceButtonStates();
        }

        private async void OnValveButtonClicked(object sender, EventArgs e)
        {
            if (!isValveOn)
            {
                await _mqttService.SendDeviceCommandAsync("VALVE", true, "", "", "");
                isValveOn = true;
            }
            else
            {
                await _mqttService.SendDeviceCommandAsync("VALVE", false, "", "", "");
                isValveOn = false;
            }
            UpdateDeviceButtonStates();
        }

        // Cập nhật trạng thái nút bật/tắt theo trạng thái thiết bị
        private void UpdateDeviceButtonStates()
        {
            PumpButton.Text = isPumpOn ? "Tắt Bơm" : "Bật Bơm";
            PumpButton.BackgroundColor = isPumpOn ? Colors.OrangeRed : Colors.LightGreen;

            LightButton.Text = isLightOn ? "Tắt Đèn" : "Bật Đèn";
            LightButton.BackgroundColor = isLightOn ? Colors.OrangeRed : Colors.LightBlue;

            ValveButton.Text = isValveOn ? "Tắt Van Nước" : "Bật Van Nước";
            ValveButton.BackgroundColor = isValveOn ? Colors.OrangeRed : Colors.LightSkyBlue;
        }

        // ====== 2. Lập lịch bật/tắt thiết bị ======

        private async void OnSaveScheduleClicked(object sender, EventArgs e)
        {
            // Lấy thiết bị từ Picker
            string device = DevicePicker.SelectedIndex switch
            {
                0 => "PUMP",
                1 => "LIGHT",
                2 => "VALVE",
                _ => "PUMP"
            };

            // Mặc định value là "on" (có thể sửa tùy UI)
            string value = "on";

            // Lấy ngày giờ bật
            DateTime date = SpecificDatePicker.Date;
            TimeSpan time = StartTimePicker.Time;
            DateTime scheduleTime = date.Date + time;

            // Gửi bản tin lập lịch bật
            await _mqttService.SendDeviceCommandAsync(
                device,
                value == "on",
                "schedule",
                scheduleTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                "scheduled");

            // Lưu vào danh sách hiển thị (RAM)
            var schedule = new DeviceSchedule
            {
                Device = device,
                Value = value,
                Time = scheduleTime,
                Action = "schedule",
                Status = "scheduled"
            };
            schedule.DeleteCommand = new Command(async () => await DeleteSchedule(schedule));
            _scheduleList.Add(schedule);

            // Nếu là Đèn và có giờ tắt, thì lập thêm bản tin tắt
            if (device == "LIGHT" && EndTimePanel.IsVisible)
            {
                TimeSpan endTime = EndTimePicker.Time;
                DateTime endScheduleTime = date.Date + endTime;

                // Gửi bản tin lập lịch tắt đèn
                await _mqttService.SendDeviceCommandAsync(
                    device,
                    false, // value == "off"
                    "schedule",
                    endScheduleTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    "scheduled");

                // Thêm lịch tắt đèn vào danh sách hiển thị
                var scheduleOff = new DeviceSchedule
                {
                    Device = device,
                    Value = "off",
                    Time = endScheduleTime,
                    Action = "schedule",
                    Status = "scheduled"
                };
                scheduleOff.DeleteCommand = new Command(async () => await DeleteSchedule(scheduleOff));
                _scheduleList.Add(scheduleOff);
            }
        }

        // ====== 3. Xoá lịch ======

        private async Task DeleteSchedule(DeviceSchedule schedule)
        {
            // Gửi bản tin xóa lịch (status = "deleted")
            await _mqttService.SendDeviceCommandAsync(
                schedule.Device,
                schedule.Value == "on",
                "schedule",
                schedule.Time.ToString("yyyy-MM-ddTHH:mm:ss"),
                "deleted");

            // Xoá khỏi danh sách RAM
            _scheduleList.Remove(schedule);
        }

        // ====== 4. Các hàm bổ sung cho UI ======

        private void DevicePicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Ẩn/hiện EndTimePanel nếu chọn Đèn thì hiện, còn lại ẩn
            if (DevicePicker.SelectedIndex == 1) // Đèn
            {
                EndTimePanel.IsVisible = true;
            }
            else
            {
                EndTimePanel.IsVisible = false;
            }
        }

        private void AutoPumpSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            // TODO: Bổ sung logic điều khiển tự động bơm khi nước cạn nếu cần
        }

        private void AutoLightSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            // TODO: Bổ sung logic điều khiển tự động bật đèn khi trời tối nếu cần
        }

        private void AutoTempSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            // TODO: Bổ sung logic tự động bật đèn khi nhiệt độ dưới mức đặt nếu cần
        }

        private void TemperatureEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            // TODO: Xử lý nhập nhiệt độ tự động nếu cần
        }
    }
}