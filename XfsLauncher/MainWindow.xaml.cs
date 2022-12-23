using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace XfsLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadAsync();
        }

        private static string ExeDir => System.AppContext.BaseDirectory;

        private static string GocqDir => Path.Combine(ExeDir, "go-cqhttp");

        public string Selected => (string)VersionCombo.SelectedItem;

        public string QQ => QQInput.Text;

        private async void LoadAsync()
        {
            await Task.Yield();
            try
            {
                ExecuteInfo.Text = $@"当前目录：{ExeDir}
查找目录：{GocqDir}";
                VersionCombo.ItemsSource = Directory.GetDirectories(GocqDir)
                    .Select(fp => Path.GetFileName(fp))
                    .Prepend("请选择 go-cqhttp 版本。");
                VersionCombo.SelectedIndex = 0;
                VersionCombo.IsSynchronizedWithCurrentItem = true;
            }
            catch (Exception)
            {
                MessageBox.Show($"加载内置的 go-cqhttp 失败。{VersionCombo.SelectedIndex}");
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !long.TryParse(e.Text, out _);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var gocqDir = Path.Combine(GocqDir, Selected);
            var gocqExe = Path.Combine(gocqDir, "go-cqhttp.exe");
            try
            {
                const string templateSuffix = ".template";
                var templateFile = Directory.GetFiles(gocqDir).FirstOrDefault(f => Path.GetExtension(f).Equals(templateSuffix, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(templateFile))
                {
                    MessageBox.Show("无法找到配置模板文件，启动失败。");
                    return;
                }
                ConnectionStackPanel.IsEnabled = false;
                var text = await File.ReadAllTextAsync(Path.Combine(gocqDir, templateFile));
                text = text.Replace("@qq", QQ, StringComparison.Ordinal)
                    .Replace("@password", System.Text.Json.JsonSerializer.Serialize(Password.Password), StringComparison.Ordinal);
                await File.WriteAllTextAsync(Path.Combine(gocqDir, templateFile[..^templateSuffix.Length]), text);
            }
            catch (Exception)
            {
                MessageBox.Show(this, "读取文件失败。");
                ConnectionStackPanel.IsEnabled = true;
                return;
            }

            try
            {
                var startInfo = new ProcessStartInfo("cmd", "/K \"" + gocqExe + "\"");
                startInfo.WorkingDirectory = gocqDir;
                startInfo.UseShellExecute = true;
                using var process = Process.Start(startInfo);
                if (process is null)
                {
                    MessageBox.Show(this, "go-cqhttp 未正常启动。");
                    return;
                }
                await process.WaitForExitAsync().ConfigureAwait(true);
                if (process.ExitCode != 0)
                {
                    MessageBox.Show(this, "go-cqhttp 已被非正常终止。");
                }
            }
            catch (Exception)
            {
                MessageBox.Show(this, "go-cqhttp 已被非正常终止。");
            }
            finally
            {
                ConnectionStackPanel.IsEnabled = true;
            }
        }
    }
}
