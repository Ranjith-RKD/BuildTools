using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;
using Syncfusion.Maui.Toolkit.Popup;

#if WINDOWS
using Windows.ApplicationModel;
#endif

namespace BasicTool;
public partial class Cleaner : ContentPage
{
    private bool _allSelected = false;
    private bool _andAllSelected = false;
    private ObservableCollection<AppInfo> packages;
    private int currentIndex = 0;
    private ViewModel viewModel;

    public Cleaner(ViewModel viewModel)
    {
        this.viewModel = viewModel;
        this.BindingContext = this.viewModel;
        InitializeComponent();
    }


    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower();
        AppCollection.ItemsSource = string.IsNullOrWhiteSpace(searchText) ? viewModel.AppsInfo : viewModel.AppsInfo.Where(a => a.DisplayName.ToLower().Contains(searchText)).ToList();
    }

    private void OnSortClicked(object sender, EventArgs e)
    {
        if (viewModel.AppsInfo.Count > 0)
        {
            viewModel?.SortApps();
        }
        else
        {
            ShowAutoCloseToast($"Item Cout is zero", false);
        }
    }

    private void CollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AppCollection?.SelectedItems?.Count > 0)
        {
            foreach (var item in viewModel?.AppsInfo ?? Enumerable.Empty<AppInfo>())
            {
                (item as AppInfo).ShowSelected = true;
            }
        }
        else
        {
            foreach (var item in viewModel?.AppsInfo ?? Enumerable.Empty<AppInfo>())
            {
                (item as AppInfo).ShowSelected = false;
            }
        }

        foreach (var item in e?.PreviousSelection ?? Enumerable.Empty<object>())
        {
            (item as AppInfo).Selected = false;
        }

        foreach (var item in e?.CurrentSelection ?? Enumerable.Empty<object>())
        {
            (item as AppInfo).Selected = true;
        }
    }


    private void SelectAll(object sender, EventArgs e)
    {
        _allSelected = !_allSelected;
        AppCollection?.SelectedItems?.Clear();
        if (_allSelected)
        {
            foreach (var item in viewModel?.AppsInfo ?? Enumerable.Empty<AppInfo>())
            {
                AppCollection?.SelectedItems?.Add(item);
            }
        }
    }

    private async void Delete(object sender, EventArgs e)
    {
#if WINDOWS
        // Convert IList to IEnumerable before creating ObservableCollection
        if (AppCollection.SelectedItems.Count > 0)
        {
            packages = new ObservableCollection<AppInfo>(AppCollection.SelectedItems.Cast<AppInfo>());
            if (packages.Count > 0)
            {
                UninstallNext();
            }
        }
#endif
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
                                   Text = isSucess? "\uf00c" : "\uf00d",
                                  FontSize = 20,
                                  TextColor =isSucess? Colors.Green: Colors.Red,
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

        popup.Show();
        await Task.Delay(2000);
        popup.Dismiss();
    }

    private void UninstallNext()
    {
        if (currentIndex >= packages.Count)
        {
            // All apps uninstalled, refresh the list
            MainThread.BeginInvokeOnMainThread(() =>
            {
                viewModel.LoadApps();
                ShowAutoCloseToast($"All apps have been uninstalled in Windows", true);
            });
            currentIndex = 0;  // Reset for the next run
            return;
        }

        var app = packages[currentIndex];
#if WINDOWS
        string appName = (app as AppInfo)?.package?.DisplayName;

        if (!string.IsNullOrWhiteSpace(appName))
        {
            // WinUI Uninstall
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c winget uninstall {appName}",
                    UseShellExecute = true,
                    Verb = "runas"
                },
                EnableRaisingEvents = true
            };

            process.Exited += (s, e) =>
            {
                var exitCode = (s as Process)?.ExitCode ?? -1;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (exitCode == 0)
                    {
                        ShowAutoCloseToast($"{appName} has been uninstalled.", true);
                    }
                    else
                    {
                        ShowAutoCloseToast($"Error in uninstalling {appName}", false);
                    }
                    currentIndex++;
                    UninstallNext();
                });
            };

            process.Start();
            process.WaitForExit();
        }
        else
        {
            // Move to the next app if the name is invalid
            currentIndex++;
            UninstallNext();
        }
#endif
    }

    private void AndroidCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView coll && coll.BindingContext is AndroidDeviceInfo androidDeviceInfo)
        {
            foreach (var app in androidDeviceInfo.AppsInfo)
            {
                (app as AppInfo).ShowSelected = coll.SelectedItems?.Count > 0;
            }
        }

        foreach (var item in e?.PreviousSelection ?? Enumerable.Empty<object>())
        {
            (item as AppInfo).Selected = false;
        }

        foreach (var item in e?.CurrentSelection ?? Enumerable.Empty<object>())
        {
            (item as AppInfo).Selected = true;
        }
    }

    private void AndroidOnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        viewModel.RefreshAndroidApps();
        var searchText = e.NewTextValue?.ToLower();
        foreach (var device in viewModel.AndriodDeviceInfo)
        {
            device.AppsInfo = string.IsNullOrWhiteSpace(searchText) ? device.AppsInfo : new ObservableCollection<AppInfo>(device.AppsInfo.Where(a => a.DisplayName.ToLower().Contains(searchText)).ToList());
        }
    }

    private void AndroidSelectAll(object sender, EventArgs e)
    {
        _andAllSelected = !_andAllSelected;
        foreach (var item in viewModel?.AndriodDeviceInfo)
        {
            if (_andAllSelected)
            {
                foreach (var app in item.AppsInfo)
                {
                    app.Selected = true;
                    app.ShowSelected = true;
                }
            }
            else
            {
                foreach (var app in item.AppsInfo)
                {
                    app.Selected = false;
                    app.ShowSelected = false;
                }
            }
        }
    }

    private async void AndroidDelete(object sender, EventArgs e)
    {
#if WINDOWS
        var label = new Label
        {
            Text = "Started the process on Android!",
            FontSize = 16,
            HorizontalTextAlignment = TextAlignment.Start,
            TextColor = Colors.Black,
            HorizontalOptions = LayoutOptions.Start
        };
        // Get all running emulators
        string adbPath = $@"C:\Users\{Environment.UserName}\AppData\Local\Android\Sdk\platform-tools\adb.exe";
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

        if (viewModel.AndriodDeviceInfo.Count > 0)
        {
            foreach (var devicelist in viewModel.AndriodDeviceInfo)
            {
                label.Text = $"{"\uf00c"} Processing {devicelist.DisplayName}";
                if (devicelist.AppsInfo.Count > 0)
                {
                    foreach (var app in devicelist.AppsInfo.Where(x => x.Selected))
                    {
                        label.Text = $"Processing {devicelist.EmulatorID}............{Environment.NewLine}Uninstalling {app.PackageName}";
                        await Task.Delay(2000);
                        var uninstallProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = adbPath,
                                Arguments = $"-s {devicelist.EmulatorID} uninstall {app.PackageName}",
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };

                        uninstallProcess.Start();
                        string uninstallOutput = uninstallProcess.StandardOutput.ReadToEnd();
                        uninstallProcess.WaitForExit();
                        label.Text = $"Processing {devicelist.EmulatorID}............{Environment.NewLine}Result.... {uninstallOutput}";
                        await Task.Delay(2000);
                    }
                }
                else
                {
                    label.Text = $"{"\uf00d"} Oops No Apps found running on {devicelist.DisplayName}";
                    await Task.Delay(1250);
                    popup.Dismiss();
                    return;
                }

            }
        }
        else
        {
            label.Text = $"{"\uf00d"} Oops No Device found running";
            await Task.Delay(1250);
            popup.Dismiss();
            return;
        }

        label.Text = $"{"\uf00c"}Process Complete";
        await Task.Delay(2000);
        viewModel.RefreshAndroidApps();
        popup.Dismiss();
#endif
    }

    private void OnAndroidSortClicked(object sender, EventArgs e)
    {
        if (viewModel.AndriodDeviceInfo.Count > 0)
        {
            viewModel?.SortAndroidApps();
        }
        else
        {
            ShowAutoCloseToast($"Item Cout is zero", false);
        }
    }
}