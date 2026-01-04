using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WeatherApp
{
    public partial class MainWindow : Window
    {
        private const string ApiKey = "dd8eb228cee10b874079ce3949d5e16d";
        private const string CurrentUrl = "https://api.openweathermap.org/data/2.5/weather";
        private const string ForecastUrl = "https://api.openweathermap.org/data/2.5/forecast";

        public MainWindow()
        {
            InitializeComponent();
            this.SizeChanged += (s, e) => DrawCityscape(); // динамическое масштабирование
        }

        private async void GetWeather_Click(object sender, RoutedEventArgs e)
        {
            string city = CityInput.Text.Trim();
            if (string.IsNullOrEmpty(city))
            {
                MessageBox.Show("Введите город!");
                return;
            }

            await LoadCurrentWeather(city);
            await LoadForecast(city);
            DrawCityscape();
        }

        #region Weather
        private async Task LoadCurrentWeather(string city)
        {
            try
            {
                using HttpClient client = new HttpClient();
                string url = $"{CurrentUrl}?q={city}&appid={ApiKey}&units=metric&lang=ru";
                string response = await client.GetStringAsync(url);

                using JsonDocument doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                double temp = root.GetProperty("main").GetProperty("temp").GetDouble();
                double feels = root.GetProperty("main").GetProperty("feels_like").GetDouble();
                int humidity = root.GetProperty("main").GetProperty("humidity").GetInt32();
                int pressure = root.GetProperty("main").GetProperty("pressure").GetInt32();
                double wind = root.GetProperty("wind").GetProperty("speed").GetDouble();

                string description = root.GetProperty("weather")[0].GetProperty("description").GetString() ?? "";
                string icon = root.GetProperty("weather")[0].GetProperty("icon").GetString() ?? "";

                long sunriseUnix = root.GetProperty("sys").GetProperty("sunrise").GetInt64();
                long sunsetUnix = root.GetProperty("sys").GetProperty("sunset").GetInt64();
                DateTime sunrise = DateTimeOffset.FromUnixTimeSeconds(sunriseUnix).LocalDateTime;
                DateTime sunset = DateTimeOffset.FromUnixTimeSeconds(sunsetUnix).LocalDateTime;

                TemperatureText.Text = $"{temp:0}°C";
                FeelsLikeText.Text = $"Ощущается как: {feels:0}°C";
                HumidityText.Text = $"Влажность: {humidity}%";
                PressureText.Text = $"Давление: {pressure} hPa";
                WindText.Text = $"Ветер: {wind} м/с";
                DescriptionText.Text = description;
                SunText.Text = $"Восход: {sunrise:HH:mm} | Закат: {sunset:HH:mm}";

                WeatherIcon.Source = new BitmapImage(new Uri($"http://openweathermap.org/img/wn/{icon}@2x.png"));

                AnimateWeather(description);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при получении текущей погоды: " + ex.Message);
            }
        }

        private async Task LoadForecast(string city)
        {
            try
            {
                using HttpClient client = new HttpClient();
                string url = $"{ForecastUrl}?q={city}&appid={ApiKey}&units=metric&lang=ru";
                string response = await client.GetStringAsync(url);

                using JsonDocument doc = JsonDocument.Parse(response);
                var root = doc.RootElement.GetProperty("list");

                ForecastPanel.Children.Clear();

                for (int i = 0; i < root.GetArrayLength(); i += 8)
                {
                    var item = root[i];
                    string date = item.GetProperty("dt_txt").GetString() ?? "";
                    double temp = item.GetProperty("main").GetProperty("temp").GetDouble();
                    string icon = item.GetProperty("weather")[0].GetProperty("icon").GetString() ?? "";

                    Border cardBorder = new Border
                    {
                        Width = 100,
                        Margin = new Thickness(5),
                        Background = new SolidColorBrush(Color.FromArgb(120, 45, 45, 64)),
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(5),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    StackPanel cardPanel = new StackPanel();
                    cardPanel.Children.Add(new TextBlock
                    {
                        Text = DateTime.Parse(date).ToString("ddd dd"),
                        FontSize = 14,
                        TextAlignment = TextAlignment.Center
                    });
                    cardPanel.Children.Add(new Image
                    {
                        Source = new BitmapImage(new Uri($"http://openweathermap.org/img/wn/{icon}@2x.png")),
                        Width = 50,
                        Height = 50,
                        Margin = new Thickness(0, 5, 0, 5)
                    });
                    cardPanel.Children.Add(new TextBlock
                    {
                        Text = $"{temp:0}°C",
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center
                    });

                    cardBorder.Child = cardPanel;
                    ForecastPanel.Children.Add(cardBorder);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при получении прогноза: " + ex.Message);
            }
        }
        #endregion

        #region Weather Animation
        private void AnimateWeather(string description)
        {
            WeatherAnimationCanvas.Children.Clear();
            if (string.IsNullOrEmpty(description)) return;
            Random rand = new Random();

            // дождь
            if (description.Contains("дождь"))
            {
                for (int i = 0; i < 50; i++)
                {
                    Line drop = new Line
                    {
                        Stroke = Brushes.LightBlue,
                        StrokeThickness = 2
                    };
                    WeatherAnimationCanvas.Children.Add(drop);
                    AnimateRainDrop(drop, rand);
                }
            }

            // облака
            for (int i = 0; i < 3; i++)
            {
                Ellipse cloud = new Ellipse
                {
                    Width = 150,
                    Height = 60,
                    Fill = Brushes.LightGray,
                    Opacity = 0.6
                };
                WeatherAnimationCanvas.Children.Add(cloud);
                AnimateParallax(cloud, -150 * i, WeatherAnimationCanvas.ActualWidth + 50, 30 + rand.NextDouble() * 15);
                Canvas.SetTop(cloud, 30 + i * 50);
            }
        }

        private void AnimateRainDrop(Line drop, Random rand)
        {
            double canvasWidth = WeatherAnimationCanvas.ActualWidth;
            double canvasHeight = WeatherAnimationCanvas.ActualHeight;
            drop.X1 = rand.NextDouble() * canvasWidth;
            drop.X2 = drop.X1;
            drop.Y1 = rand.NextDouble() * canvasHeight;
            drop.Y2 = drop.Y1 + 10;

            DoubleAnimation anim = new DoubleAnimation
            {
                From = drop.Y1,
                To = canvasHeight + 10,
                Duration = TimeSpan.FromSeconds(1 + rand.NextDouble()),
                RepeatBehavior = RepeatBehavior.Forever
            };
            drop.BeginAnimation(Line.Y1Property, anim);
            drop.BeginAnimation(Line.Y2Property, anim);
        }
        #endregion

        #region Cityscape
        private void DrawCityscape()
        {
            GroundCanvas.Children.Clear();
            SkyCanvas.Children.Clear();
            Random rand = new Random();

            double width = WeatherAnimationCanvas.ActualWidth;
            double height = WeatherAnimationCanvas.ActualHeight;

            if (width == 0 || height == 0) return;

            // фон неба
            Rectangle sky = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = new SolidColorBrush(Color.FromRgb(30, 30, 60))
            };
            SkyCanvas.Children.Add(sky);

            // трава
            Rectangle grass = new Rectangle
            {
                Width = width,
                Height = height * 0.1,
                Fill = new SolidColorBrush(Color.FromRgb(20, 120, 20))
            };
            GroundCanvas.Children.Add(grass);
            Canvas.SetTop(grass, height * 0.9);

            // дорога
            Rectangle road = new Rectangle
            {
                Width = width,
                Height = height * 0.05,
                Fill = new SolidColorBrush(Color.FromRgb(50, 50, 50))
            };
            GroundCanvas.Children.Add(road);
            Canvas.SetTop(road, height * 0.85);

            // здания
            for (int i = 0; i < 10; i++)
            {
                double buildingWidth = width * 0.08 + rand.NextDouble() * 30;
                double buildingHeight = height * 0.2 + rand.NextDouble() * 80;
                Rectangle building = new Rectangle
                {
                    Width = buildingWidth,
                    Height = buildingHeight,
                    Fill = new SolidColorBrush(Color.FromRgb(30, 30, 40))
                };
                GroundCanvas.Children.Add(building);
                Canvas.SetLeft(building, i * (width / 10));
                Canvas.SetTop(building, height * 0.85 - buildingHeight);

                // окна
                int rows = (int)(buildingHeight / 10);
                int cols = (int)(buildingWidth / 10);
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        if (rand.NextDouble() < 0.3)
                        {
                            Rectangle window = new Rectangle
                            {
                                Width = 6,
                                Height = 6,
                                Fill = Brushes.Yellow
                            };
                            GroundCanvas.Children.Add(window);
                            Canvas.SetLeft(window, i * (width / 10) + c * 10);
                            Canvas.SetTop(window, height * 0.85 - buildingHeight + r * 10);
                        }
            }
        }

        private void AnimateParallax(FrameworkElement element, double from, double to, double seconds)
        {
            DoubleAnimation anim = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(seconds),
                RepeatBehavior = RepeatBehavior.Forever
            };
            element.BeginAnimation(Canvas.LeftProperty, anim);
        }
        #endregion
    }
}
