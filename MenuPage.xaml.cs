using Microsoft.Maui.Controls;
using System;

namespace LoginApp
{
    public partial class MenuPage : ContentPage
    {
        public MenuPage()
        {
            InitializeComponent();
        }

        private async void OnEnvParamsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MainPage());
        }

        private async void OnDeviceControlClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new DevicePage());
        }

        private async void History_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new HistoryPage());
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            // Đăng xuất, quay về LoginPage và xóa stack
            await Navigation.PopToRootAsync();
            // hoặc dùng Shell nếu bạn dùng Shell navigation:
            // await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}