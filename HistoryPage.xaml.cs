using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace LoginApp
{
    public partial class HistoryPage : ContentPage
    {
        // Model cho lịch sử nhiệt độ/độ ẩm
        public class TempHumiHistory
        {
            public DateTime Time { get; set; }
            public double Temperature { get; set; }
            public double Humidity { get; set; }
            public string Display =>
                $"{Time:dd/MM/yyyy HH:mm} - Nhiệt độ: {Temperature:0.0}°C, Độ ẩm: {Humidity:0.0}%";
        }

        // Model cho lịch sử bật tắt thiết bị
        public class DeviceActionHistory
        {
            public DateTime Time { get; set; }
            public string Device { get; set; }
            public string Action { get; set; } // "Bật" hoặc "Tắt"
            public string Display => $"{Time:dd/MM/yyyy HH:mm} - {Device}: {Action}";
        }

        public ObservableCollection<TempHumiHistory> TempHumiHistories { get; set; } = new ObservableCollection<TempHumiHistory>();
        public ObservableCollection<DeviceActionHistory> DeviceActionHistories { get; set; } = new ObservableCollection<DeviceActionHistory>();

        public HistoryPage()
        {
            InitializeComponent();

            // Fake dữ liệu nhiệt độ/độ ẩm 3 ngày gần nhất (mỗi 30 phút 1 mẫu)
            var start = new DateTime(2025, 6, 17, 0, 0, 0);
            var end = new DateTime(2025, 6, 19, 23, 30, 0);
            var random = new Random(42);
            for (var t = start; t <= end; t = t.AddMinutes(30))
            {
                // Tạo biến động logic bằng chu kỳ ngày và giờ
                // Nhiệt độ cao nhất lúc 14h, thấp nhất lúc 4h sáng
                double baseTemp = 25 + 5 * Math.Sin((t.Hour - 4) / 24.0 * 2 * Math.PI);
                // Độ ẩm thấp nhất lúc 14h, cao nhất lúc 4h sáng
                double baseHumi = 70 - 10 * Math.Sin((t.Hour - 4) / 24.0 * 2 * Math.PI);

                TempHumiHistories.Add(new TempHumiHistory
                {
                    Time = t,
                    Temperature = baseTemp + random.NextDouble() * 1.2, // dao động nhẹ
                    Humidity = baseHumi + random.NextDouble() * 2
                });
            }

            // Fake lịch sử bật tắt thiết bị theo yêu cầu
            // Ngày 17/6
            DeviceActionHistories.Add(new DeviceActionHistory { Time = new DateTime(2025, 6, 17, 9, 0, 0), Device = "Van nước", Action = "Bật" });
            DeviceActionHistories.Add(new DeviceActionHistory { Time = new DateTime(2025, 6, 17, 9, 15, 0), Device = "Van nước", Action = "Tắt" });
            DeviceActionHistories.Add(new DeviceActionHistory { Time = new DateTime(2025, 6, 17, 9, 20, 0), Device = "Bơm", Action = "Bật" });
            DeviceActionHistories.Add(new DeviceActionHistory { Time = new DateTime(2025, 6, 17, 9, 35, 0), Device = "Bơm", Action = "Tắt" });
            DeviceActionHistories.Add(new DeviceActionHistory { Time = new DateTime(2025, 6, 17, 18, 0, 0), Device = "Đèn", Action = "Bật" });
            // Ngày 18/6
            DeviceActionHistories.Add(new DeviceActionHistory { Time = new DateTime(2025, 6, 18, 6, 0, 0), Device = "Đèn", Action = "Tắt" });
            DeviceActionHistories.Add(new DeviceActionHistory { Time = new DateTime(2025, 6, 18, 12, 30, 0), Device = "Van nước", Action = "Bật" });
            DeviceActionHistories.Add(new DeviceActionHistory { Time = new DateTime(2025, 6, 18, 12, 45, 0), Device = "Van nước", Action = "Tắt" });
            DeviceActionHistories.Add(new DeviceActionHistory { Time = new DateTime(2025, 6, 18, 13, 0, 0), Device = "Bơm", Action = "Bật" });
            DeviceActionHistories.Add(new DeviceActionHistory { Time = new DateTime(2025, 6, 18, 13, 14, 0), Device = "Bơm", Action = "Tắt" });
            DeviceActionHistories.Add(new DeviceActionHistory { Time = new DateTime(2025, 6, 18, 18, 0, 0), Device = "Đèn", Action = "Bật" });
            // Ngày 19/6 (giả lập logic: tắt đèn 6h, mở van 10h, tắt van 10h15, bật bơm 14h, tắt bơm 14h18, bật đèn 18h)
            DeviceActionHistories.Add(new DeviceActionHistory { Time = new DateTime(2025, 6, 19, 6, 0, 0), Device = "Đèn", Action = "Tắt" });
            DeviceActionHistories.Add(new DeviceActionHistory { Time = new DateTime(2025, 6, 19, 10, 0, 0), Device = "Van nước", Action = "Bật" });
            DeviceActionHistories.Add(new DeviceActionHistory { Time = new DateTime(2025, 6, 19, 10, 15, 0), Device = "Van nước", Action = "Tắt" });
            DeviceActionHistories.Add(new DeviceActionHistory { Time = new DateTime(2025, 6, 19, 10, 19, 0), Device = "Bơm", Action = "Bật" });
            DeviceActionHistories.Add(new DeviceActionHistory { Time = new DateTime(2025, 6, 19, 10, 37, 0), Device = "Bơm", Action = "Tắt" });
            DeviceActionHistories.Add(new DeviceActionHistory { Time = new DateTime(2025, 6, 19, 18, 0, 0), Device = "Đèn", Action = "Bật" });

            TempHumiListView.ItemsSource = TempHumiHistories;
            DeviceActionListView.ItemsSource = DeviceActionHistories;
        }
    }
}