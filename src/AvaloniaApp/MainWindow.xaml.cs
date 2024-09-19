using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Avalonia.Controls;  // Correct namespace for 'Window'

namespace ProxyControlApp
{
    public partial class MainWindow : Window
    {
        private JObject _env;
        private readonly HttpClient _httpClient;

        public MainWindow()
        {
            InitializeComponent();
            LoadEnvironment();
            _httpClient = new HttpClient();
        }

        // Load environment variables from env.json
        private void LoadEnvironment()
        {
            var envPath = Path.Combine(Directory.GetCurrentDirectory(), "env.json");
            if (File.Exists(envPath))
            {
                _env = JObject.Parse(File.ReadAllText(envPath));
            }
        }

        // Helper method to get variables from env.json
        private string GetEnvVariable(string key)
        {
            return _env[key]?.ToString();
        }

        // Triggered when the "Connect" button is clicked
        private async void ConnectToProxyButton_Click(object sender, EventArgs e)
        {
            string apiUrl = GetEnvVariable("api_base_url");
            string apiKey = GetEnvVariable("api_key");
            string proxyPort = GetEnvVariable("proxy_port");

            Console.WriteLine($"Connecting to proxy on port {proxyPort}...");

            // Fetch the list of available proxies and connect
            var proxyList = await GetProxyList(apiUrl, apiKey);
            if (proxyList != null && proxyList.Count > 0)
            {
                string proxyIP = proxyList[0]["proxy_address"].ToString();
                string username = proxyList[0]["username"].ToString();
                string password = proxyList[0]["password"].ToString();
                await ConnectToProxy(proxyIP, proxyPort, username, password);
            }
            else
            {
                Console.WriteLine("No proxies available.");
            }
        }

        private async Task<JArray> GetProxyList(string apiUrl, string apiKey)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(jsonResponse);
                    return (JArray)jsonObject["data"];
                }
                else
                {
                    Console.WriteLine("Failed to fetch proxies from Webshare API.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching proxy list: {ex.Message}");
            }

            return null;
        }

        private async Task ConnectToProxy(string proxyIP, string proxyPort, string username, string password)
        {
            try
            {
                Console.WriteLine($"Attempting to connect to proxy at {proxyIP}:{proxyPort}...");

                var proxy = new WebProxy($"{proxyIP}:{proxyPort}", true)
                {
                    Credentials = new NetworkCredential(username, password)
                };

                WebRequest.DefaultWebProxy = proxy;

                _httpClient.DefaultProxy = proxy;
                var testUrl = "https://api.ipify.org?format=json";
                HttpResponseMessage response = await _httpClient.GetAsync(testUrl);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Connected to proxy! Your IP through proxy: {responseBody}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to proxy: {ex.Message}");
            }
        }
    }
}