using GigaImageWPF.Clasess;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GigaImageWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static string ClientId = "019b07a5-4f7a-7433-aa68-20315fe3fcd3";
        static string AutoriazationKey = "MDE5YjA3YTUtNGY3YS03NDMzLWFhNjgtMjAzMTVmZTNmY2QzOjVmMzE5NjJkLTkzYTgtNDk0OS05M2I3LTBmOTk3MjA2YjdmMw==";

        private string Token;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Token = await GetToken();

            if (Token == null)
            {
                MessageBox.Show(
                    "Не удалось получить токен GigaChat",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void Calendar_Click(object sender, RoutedEventArgs e)
        {
            if (StyleCombo.SelectedItem == null ||ColorCombo.SelectedItem == null ||AspectCombo.SelectedItem == null)
            {
                MessageBox.Show(
                    "Выберите стиль, цвет и формат перед использованием календаря",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string style = ((ComboBoxItem)StyleCombo.SelectedItem).Content.ToString();
            string color = ((ComboBoxItem)ColorCombo.SelectedItem).Content.ToString();
            string aspect = ((ComboBoxItem)AspectCombo.SelectedItem).Content.ToString();

            string holiday = GetNearestHoliday(DateTime.Now);

            PromptBox.Text =$@"Тематические обои к празднику «{holiday}». Стиль: {style} Цветовая палитра: {color} Соотношение сторон: {aspect} Обои для рабочего стола Без текста и логотипов Высокое качество, 4K";

            MessageBox.Show(
                $"Сформирован промпт для ближайшего праздника:\n{holiday}",
                "Календарная тема",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void GeneratePrompt_Click(object sender, RoutedEventArgs e)
        {
            if (StyleCombo.SelectedItem == null ||
                ColorCombo.SelectedItem == null ||
                AspectCombo.SelectedItem == null)
            {
                MessageBox.Show(
                    "Выберите стиль, цвет и формат",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            string userText = PromptBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(userText))
            {
                MessageBox.Show(
                    "Введите текстовое описание",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string style = ((ComboBoxItem)StyleCombo.SelectedItem).Content.ToString();
            string color = ((ComboBoxItem)ColorCombo.SelectedItem).Content.ToString();
            string aspect = ((ComboBoxItem)AspectCombo.SelectedItem).Content.ToString();

            

            PromptBox.Text =$@"{userText} Стиль: {style} Цветовая палитра: {color} Соотношение сторон: {aspect} Обои для рабочего стола Без текста и логотипов Высокое качество";

            MessageBox.Show(
                "Промпт сформирован",
                "Готово",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        private async void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (Token == null)
                return;

            string prompt = PromptBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(prompt))
            {
                MessageBox.Show(
                    "Сначала сформируйте промпт",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(
                "Генерация изображения запущена",
                "GigaChat",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            string path = await GenerateImage(prompt);

            if (path == null)
                return;

            var res = MessageBox.Show(
                $"Изображение сохранено на рабочем столе.\n\nУстановить как обои?",
                "Готово",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                WallpaperSetter.Set(path);
                MessageBox.Show(
                    "Обои установлены",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        private async Task<string> GenerateImage(string prompt)
        {
            string baseUrl = "https://gigachat.devices.sberbank.ru/api/v1";

            using var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

            using var http = new HttpClient(handler);
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {Token}");
            http.DefaultRequestHeaders.Add("X-Client-ID", ClientId);
            http.DefaultRequestHeaders.Add("Accept", "application/json");


            string style = ((ComboBoxItem)StyleCombo.SelectedItem).Content.ToString();
            string color = ((ComboBoxItem)ColorCombo.SelectedItem).Content.ToString();
            string aspect = ((ComboBoxItem)AspectCombo.SelectedItem).Content.ToString();



            var pr = $@"{prompt} Стиль: {style} Цветовая палитра: {color} Соотношение сторон: {aspect} Обои для рабочего стола Без текста и логотипов Высокое качество";

            var payload = new
            {
                model = "GigaChat",
                messages = new[]
                {
                    new { role = "user", content = pr }
                },
                function_call = "auto"
            };

            var json = JsonConvert.SerializeObject(payload);
            MessageBox.Show("Подождите пару секунд, запрос был отправлен");
            var resp = await http.PostAsync(
                $"{baseUrl}/chat/completions",
                new StringContent(json, Encoding.UTF8, "application/json"));


            if (!resp.IsSuccessStatusCode)
            {
                MessageBox.Show(
                    $"Ошибка генерации: {resp.StatusCode}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }

            var respJson = await resp.Content.ReadAsStringAsync();
            var html = JsonObject.Parse(respJson)["choices"]?[0]?["message"]?["content"]?.ToString();

            if (string.IsNullOrWhiteSpace(html))
                return null;

            var match = Regex.Match(html, "<img\\s+[^>]*src\\s*=\\s*\"([^\"]+)\"");
            if (!match.Success)
            {
                MessageBox.Show(
                    "GigaChat вернул текст вместо изображения",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }

            string fileId = match.Groups[1].Value.Replace("files/", "").Trim();

            using var fileRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{baseUrl}/files/{fileId}/content");

            fileRequest.Headers.Add("X-Client-ID", ClientId);
            fileRequest.Headers.Add("Accept", "application/octet-stream");

            var fileResp = await http.SendAsync(fileRequest);
            if (!fileResp.IsSuccessStatusCode)
                return null;

            byte[] bytes = await fileResp.Content.ReadAsByteArrayAsync();

            string path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"gigachat_{fileId}.jpg");

            File.WriteAllBytes(path, bytes);
            return path;
        }
        private async Task<string> GetToken()
        {
            string url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

            using var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("RqUID", ClientId);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {AutoriazationKey}");

            var resp = await client.PostAsync(
                url,
                new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
                }));

            if (!resp.IsSuccessStatusCode)
                return null;

            var json = await resp.Content.ReadAsStringAsync();
            return JsonObject.Parse(json)["access_token"]?.ToString();
        }

        private string GetNearestHoliday(DateTime today)
        {
            var holidays = new List<(DateTime date, string name)>
            {
                (new DateTime(today.Year, 1, 1),  "Новый год"),
                (new DateTime(today.Year, 2, 14), "День всех влюблённых"),
                (new DateTime(today.Year, 2, 23), "День защитника Отечества"),
                (new DateTime(today.Year, 3, 8),  "8 Марта"),
                (new DateTime(today.Year, 5, 9),  "День Победы"),
                (new DateTime(today.Year, 6, 12), "День России"),
                (new DateTime(today.Year, 11, 4), "День народного единства"),
                (new DateTime(today.Year, 12, 31),"Новый год (канун)")
            };

            foreach (var h in holidays)
            {
                if (h.date >= today.Date)
                    return h.name;
            }
            return "Новый год";
        }
    }
}