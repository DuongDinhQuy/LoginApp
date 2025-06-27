using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace LoginApp
{
    public partial class LoginPage : ContentPage
    {
        private readonly HttpClient _httpClient;

        public static string GlobalUserId { get; private set; } // Biến toàn cục cho UserId

        public LoginPage()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text?.Trim();
            string password = PasswordEntry.Text?.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.", "OK");
                return;
            }

            try
            {
                var apiUrl = "https://1ab3-2001-ee0-4161-2458-c96a-c69c-38b9-add3.ngrok-free.app/api/Auth/login";
                var loginRequest = new LoginRequestModel
                {
                    Username = username,
                    Password = password
                };
                var jsonRequest = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var loginResult = JsonSerializer.Deserialize<LoginResultModel>(json);

                    // Lưu UserId làm biến toàn cục
                    Globals.GlobalUserId = loginResult.id;
                    // Ngoài ra có thể lưu vào SecureStorage hoặc Preferences nếu cần

                    await DisplayAlert("Thành công", "Đăng nhập thành công!", "OK");
                    await Navigation.PushAsync(new MenuPage());
                }
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    string errorMsg = "Sai tên đăng nhập hoặc mật khẩu.";
                    try
                    {
                        var errObj = JsonSerializer.Deserialize<ErrorResponse>(errorJson);
                        if (!string.IsNullOrWhiteSpace(errObj?.message))
                            errorMsg = errObj.message;
                    }
                    catch { }
                    await DisplayAlert("Lỗi", errorMsg, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Lỗi kết nối: {ex.Message}", "OK");
            }
        }

        private async void OnRegisterRedirectTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//RegisterPage");
        }
    }

    // Model gửi lên server
    public class LoginRequestModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    // Model nhận kết quả đăng nhập
    public class LoginResultModel
    {
        public string id { get; set; }
        public string username { get; set; }
        public string role { get; set; }
    }

    // Model nhận lỗi trả về
    public class ErrorResponse
    {
        public string message { get; set; }
    }

    // Nếu vẫn cần ánh xạ UserModel cho các chức năng khác

}