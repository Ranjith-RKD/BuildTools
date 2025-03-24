using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;
using Border = Microsoft.Maui.Controls.Border;
using Syncfusion.Maui.Toolkit.Popup;



#if WINDOWS
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Microsoft.UI.Xaml.Controls;
#endif


namespace BasicTool
{
    public partial class MainPage : ContentPage
    {
        SfPopup popup;
        public MainPage()
        {
            InitializeComponent();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new Cleaner(viewModel));
        }

        private async void DeleteSyncfusionFolders(object sender, EventArgs e)
        {
#if WINDOWS
            string keyName = String.IsNullOrEmpty((helper)?.Text) ? (helper as Entry).Placeholder : (helper)?.Text;
            string userName = Environment.UserName;
            string rootPath = $@"C:\Users\{userName}\.nuget\packages";
            var folders = Directory.GetDirectories(rootPath, "Syncfusion.*");
            if (folders.Length > 0)
            {
                foreach (var folder in folders)
                {
                    try
                    {
                        Directory.Delete(folder, true); // true => delete all contents
                        ShowAutoCloseToast($"Folder {folder} deleted.", true);
                    }
                    catch (Exception ex)
                    {
                        ShowAutoCloseToast($"Failed to delete {folder}.", false);
                    }
                }
            }
            else
            {
                ShowAutoCloseToast("Nothing to Clean", false);
            }
            var packages = viewModel?.AppsInfo.Where(x => x.package != null && x.package.DisplayName.StartsWith($"{keyName}", StringComparison.OrdinalIgnoreCase));
            if (packages.Count() > 0)
            {
                foreach (var item in packages)
                {
                    string appName = (item as AppInfo)?.package?.DisplayName;
                    if (!string.IsNullOrWhiteSpace(appName) && appName.StartsWith($"{keyName}", StringComparison.OrdinalIgnoreCase))
                    {
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/c winget uninstall {appName}",
                                UseShellExecute = true,
                                Verb = "runas",
                            },
                            EnableRaisingEvents = true
                        };
                        process.Exited += (s, e) =>
                        {
                            if ((s as Process).ExitCode == 0)
                            {
                                ShowAutoCloseToast($"{appName} has been uninstalled.", true);
                            }
                            else
                            {
                                ShowAutoCloseToast($" Error in uninstallling {appName}", false);
                            }
                        };
                        process.Start();
                        process.WaitForExit();
                    }
                }
            }
            else
            {
                ShowAutoCloseToast("No sample browsers were found", false);
            }

            // For android emulator

            var label = new Label
            {
                Text = "Started the process on Android!",
                FontSize = 16,
                HorizontalTextAlignment = TextAlignment.Start,
                TextColor = Colors.Black,
                HorizontalOptions = LayoutOptions.Start
            };
            // Get all running emulators
            SfPopup popup = new SfPopup
            {
                ContentTemplate = new DataTemplate(() =>
                {
                    return new Border
                    {
                        StrokeThickness = 2,
                        Stroke = Colors.LightGray,
                        BackgroundColor = Colors.White,
                        WidthRequest = 250,
                        Padding = 20,
                        StrokeShape = new RoundRectangle
                        {
                            CornerRadius = new CornerRadius(12)
                        },
                        Content = new VerticalStackLayout
                        {
                            Spacing = 12,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            Children =
                         {
                                   new Label
                                   {
                                       WidthRequest = 30,
                                       HeightRequest = 30,
                                       FontFamily = "appicons.ttf#",
                                       Text = "\uf00c",
                                       FontSize = 20,
                                       TextColor = Colors.Green,
                                       VerticalOptions = LayoutOptions.Center,
                                       HorizontalOptions = LayoutOptions.Center,
                                       BackgroundColor = Colors.Transparent,
                                       HorizontalTextAlignment = TextAlignment.Center,
                                       VerticalTextAlignment = TextAlignment.Center
                                   },
                                  label
                        }
                        }
                    };
                }),
                PopupStyle = new PopupStyle { PopupBackground = Colors.Black.MultiplyAlpha(0.4f) },
                HeightRequest = 500,
                WidthRequest = 400,
                OverlayMode = PopupOverlayMode.Blur
            };
            popup.Show();
            string adbPath = $@"C:\Users\{Environment.UserName}\AppData\Local\Android\Sdk\platform-tools\adb.exe";
            label.Text = $"Selecting path............ {adbPath}";
            await Task.Delay(500);
            var devicesProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = "devices",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            devicesProcess.Start();
            string devicesOutput = devicesProcess.StandardOutput.ReadToEnd();
            devicesProcess.WaitForExit();

            // Extract emulator IDs
            var lines = devicesOutput.Split('\n');
            var emulatorList = lines.Where(x => x.Contains("emulator"));

            if (emulatorList.Count() > 0)
            {
                label.Text = $"Device List............ \n {emulatorList.ToList<string>()}";
            }
            else
            {
                label.Text = $"{"\uf00d"} Oops No Device found running";
                await Task.Delay(2500);
                popup.Dismiss();
                return;
            }

            foreach (var line in emulatorList)
            {
                var emulatorId = line.Split('\t')[0].Trim();
                label.Text = $"Processing {emulatorId}............";
                await Task.Delay(2000);

                // List user-installed apps
                var listProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = adbPath,
                        Arguments = $"-s {emulatorId} shell pm list packages -3",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                listProcess.Start();
                string appList = listProcess.StandardOutput.ReadToEnd();
                listProcess.WaitForExit();

                var packagesArr = appList.Split('\n');
                var packageslist = packagesArr.Where(x => x.Contains($"{keyName}"));
                if (packageslist.Count() > 0)
                {
                    label.Text = $"Processing {emulatorId}............{Environment.NewLine}Package List............{Environment.NewLine}{string.Join(Environment.NewLine, packageslist)}";

                    await Task.Delay(2000);
                }
                else
                {
                    label.Text = $"Processing {emulatorId}............{Environment.NewLine}  {"\uf00d"} Oops! No packages found";

                    await Task.Delay(2500);
                    popup.Dismiss();
                    break;
                }

                foreach (var pkg in packageslist)
                {
                    var packageName = pkg.Replace("package:", "").Trim();
                    label.Text = $"Processing {emulatorId}............{Environment.NewLine}Uninstalling {packageName}";

                    await Task.Delay(2000);


                    // Uninstall the app
                    var uninstallProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = adbPath,
                            Arguments = $"-s {emulatorId} uninstall {packageName}",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    uninstallProcess.Start();
                    string uninstallOutput = uninstallProcess.StandardOutput.ReadToEnd();
                    uninstallProcess.WaitForExit();
                    label.Text = $"Processing {emulatorId}............{Environment.NewLine}Result.... {uninstallOutput}";

                    await Task.Delay(2000);
                }
            }

            label.Text = $"{"\uf00c"}Process Complete";
            // Auto-close after the specified duration
            await Task.Delay(2000);
            popup.Dismiss();
#endif
        }

        private void Process_Exited(object? sender, EventArgs e)
        {

        }

        private async void ShowAutoCloseToast(string message, bool isSucess)
        {
            SfPopup popup = new SfPopup
            {
                ContentTemplate = new DataTemplate(() =>
                {
                    return new Border
                    {
                        StrokeThickness = 2,
                        Stroke = Colors.LightGray,
                        BackgroundColor = Colors.White,
                        WidthRequest = 250,
                        Padding = 20,
                        StrokeShape = new RoundRectangle
                        {
                            CornerRadius = new CornerRadius(12)
                        },
                        Content = new VerticalStackLayout
                        {
                            Spacing = 12,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            Children =
                         {
                                   new Label
                                   {
                                       WidthRequest = 30,
                                       HeightRequest = 30,
                                       FontFamily = "appicons.ttf#",
                                       Text = "\uf00c",
                                       FontSize = 20,
                                       TextColor = Colors.Green,
                                       VerticalOptions = LayoutOptions.Center,
                                       HorizontalOptions = LayoutOptions.Center,
                                       BackgroundColor = Colors.Transparent,
                                       HorizontalTextAlignment = TextAlignment.Center,
                                       VerticalTextAlignment = TextAlignment.Center
                                   },
                                  new Label
                              {
                                  Text = message,
                                  FontSize = 16,
                                  TextColor = Colors.Black,
                                  HorizontalOptions = LayoutOptions.Center
                              }
                        }
                        }
                    };
                }),
                PopupStyle = new PopupStyle { PopupBackground = Colors.Black.MultiplyAlpha(0.4f) },
                HeightRequest = 500,
                WidthRequest = 400,
                OverlayMode = PopupOverlayMode.Blur
            };
            popup.AutoCloseDuration = 2000;
            popup.Show();
        }
    }
}
