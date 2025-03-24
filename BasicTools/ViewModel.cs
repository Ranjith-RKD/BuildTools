using Microsoft.Maui.Controls.PlatformConfiguration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if WINDOWS
using Windows.Management.Deployment;
#endif

namespace BasicTool
{
    public class ViewModel : INotifyPropertyChanged
    {
        private List<AppInfo> appsInfo;
        private ObservableCollection<AndroidDeviceInfo> andriodDeviceInfo;
        private bool isAscending = true;
        private bool isAndroidAscending = true;

        public List<AppInfo> AppsInfo
        {
            get => appsInfo;
            set
            {
                appsInfo = value;
                OnPropertyChanged(nameof(AppsInfo));
            }
        }

        public ObservableCollection<AndroidDeviceInfo> AndriodDeviceInfo
        {
            get => andriodDeviceInfo;
            set
            {
                andriodDeviceInfo = value;
                OnPropertyChanged(nameof(AndriodDeviceInfo));
            }
        }

        public ViewModel()
        {
            LoadApps();
        }

        internal void LoadApps()
        {
            RefreshWindowsApps();
            RefreshAndroidApps();
        }

        public async Task RefreshAndroidApps()
        {
            var TempAppInfo = new List<AndroidDeviceInfo>();
#if WINDOWS
            string adbPath = $@"C:\Users\{Environment.UserName}\AppData\Local\Android\Sdk\platform-tools\adb.exe";

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
            string devicesOutput = await devicesProcess.StandardOutput.ReadToEndAsync();
            devicesProcess.WaitForExit();

            var lines = devicesOutput.Split('\n');
            var emulatorList = lines.Where(x => x.Contains("emulator"));

            if (!emulatorList.Any())
            {
                TempAppInfo.Add(new AndroidDeviceInfo { DisplayName = "No emulators found running" });
                return;
            }

            foreach (var line in emulatorList)
            {
                var emulatorId = line.Split('\t')[0].Trim();

                AndroidDeviceInfo emulatorInfo = new AndroidDeviceInfo
                {
                    DisplayName = $"Emulator: {emulatorId}",
                    EmulatorID = emulatorId
                };

                // List installed apps
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
                string appList = await listProcess.StandardOutput.ReadToEndAsync();
                listProcess.WaitForExit();

                var packagesArr = appList.Split('\n').Where(x => x.Contains("package:"));

                if (!packagesArr.Any())
                {
                    emulatorInfo.AppsInfo.Add(new AppInfo { DisplayName = "No apps found" });
                    TempAppInfo.Add(emulatorInfo);
                    continue;
                }

                foreach (var pkg in packagesArr)
                {
                    var packageName = pkg.Replace("package:", "").Trim();

                    // Get app size
                    var sizeProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = adbPath,
                            Arguments = $"-s {emulatorId} shell du -h /data/app/{packageName} | tail -n 1",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    sizeProcess.Start();
                    string sizeOutput = await sizeProcess.StandardOutput.ReadToEndAsync();
                    sizeProcess.WaitForExit();

                    var appSize = sizeOutput.Contains("\t") ? sizeOutput.Split('\t')[0].Trim() : "Unknown size";

                    // Get version info
                    var versionProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = adbPath,
                            Arguments = $"-s {emulatorId} shell dumpsys package {packageName} | grep versionName",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    versionProcess.Start();
                    string versionOutput = await versionProcess.StandardOutput.ReadToEndAsync();
                    versionProcess.WaitForExit();

                    var versionDate = versionOutput.Contains("=") ? versionOutput.Split('=')[1].Trim() : "Unknown version";

                    emulatorInfo.AppsInfo.Add(new AppInfo
                    {
                        DisplayName = packageName,
                        PackageName = packageName,
                        AppSize = appSize,
                        VersionDate = versionDate
                    });
                }

                TempAppInfo.Add(emulatorInfo);
            }
#endif
            AndriodDeviceInfo = new ObservableCollection<AndroidDeviceInfo>(TempAppInfo);
        }


        internal void RefreshWindowsApps()
        {
            try
            {
#if WINDOWS
                var appList = new List<AppInfo>();
                var manager = new PackageManager();
                var installedApps = manager.FindPackages().ToList().Where(p => !p.IsFramework && !p.IsResourcePackage && !p.IsBundle && !IsSystemApp(p) && p.DisplayName != "BasicTool");
                if (installedApps.Count() > 0)
                {
                    foreach (var app in installedApps)
                    {
                        string appName = app.DisplayName;
                        ImageSource icon = GetAppLogo(app);
                        string installTime = GetVersionDate(app);
                        string appSize = GetPackageSize(app);
                        appList.Add(new AppInfo
                        {
                            DisplayName = appName,
                            VersionDate = installTime,
                            Logo = icon,
                            package = app
                        });
                    }
                    if (appList.Count > 0)
                    {
                        AppsInfo = appList;
                    }
                    else
                    {
                        AppsInfo = new List<AppInfo>
                    {
                        new AppInfo { DisplayName = "No apps found" }
                    };
                    }
                }
                else
                {
                    AppsInfo = new List<AppInfo>
                    {
                        new AppInfo { DisplayName = "No apps found" }
                    };
                }
#endif
            }
            catch
            {
                AppsInfo = new List<AppInfo>
               {
                   new AppInfo { DisplayName = " !oops Unable to fetch Run in Administrator mode" }
               };
            }
        }

#if WINDOWS
        private string GetVersionDate(Windows.ApplicationModel.Package package)
        {
            try
            {
                return package?.Id?.Version != null && package?.InstalledDate != null
                    ? $"{package.Id.Version.Major}.{package.Id.Version.Minor}.{package.Id.Version.Build} || {package.InstalledDate.LocalDateTime:dd-MM-yyyy}"
                    : "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private ImageSource GetAppLogo(Windows.ApplicationModel.Package package)
        {
            try
            {
                var logoPath = package?.Logo?.AbsolutePath;
                if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
                {
                    return ImageSource.FromFile(logoPath);
                }
            }
            catch
            {
                // Fallback to default icon if logo extraction fails
            }
            return ImageSource.FromFile("default_icon.png");
        }

        private string GetPackageSize(Windows.ApplicationModel.Package package)
        {
            try
            {
                var folder = package?.InstalledLocation;
                var properties = folder?.GetBasicPropertiesAsync().AsTask().Result;
                var size = properties?.Size / (1024 * 1024); // Convert to MB
                return $"{size} MB";
            }
            catch
            {
                return "Unknown";
            }
        }

        private bool IsSystemApp(Windows.ApplicationModel.Package package)
        {
            try
            {
                var publisher = package?.Id?.Publisher;
                var installLocation = package?.InstalledLocation?.Path;
                var systemPublishers = new[] { "Microsoft Corporation", "Windows" };
                var systemPaths = new[] { @"C:\Windows", @"C:\Program Files\WindowsApps" };

                return systemPublishers.Any(p => publisher?.Contains(p) == true) ||
                       systemPaths.Any(path => installLocation?.StartsWith(path, StringComparison.OrdinalIgnoreCase) == true);
            }
            catch
            {
                return true; // Treat as system app if exception occurs
            }
        }
#endif
        public void SortApps()
        {
            AppsInfo = isAscending
                ? AppsInfo.OrderBy(app => app.DisplayName).ToList()
                : AppsInfo.OrderByDescending(app => app.DisplayName).ToList();
            isAscending = !isAscending;
        }

        public void SortAndroidApps()
        {
            foreach(var device in AndriodDeviceInfo)
            {
                device.AppsInfo = new ObservableCollection<AppInfo>(isAndroidAscending
                    ? device.AppsInfo.OrderBy(app => app.DisplayName)
                    : device.AppsInfo.OrderByDescending(app => app.DisplayName));
            }
            isAndroidAscending = !isAndroidAscending;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AppInfo : INotifyPropertyChanged
    {
        public string DisplayName { get; set; }
        public string PackageName { get; set; }
        public string VersionDate { get; set; }
        public string AppSize { get; set; }
#if WINDOWS
        public Windows.ApplicationModel.Package package;
#elif Android
#endif
        public ImageSource Logo { get; set; }

        private bool selected;
        public bool Selected
        {
            get => selected;
            set { selected = value; OnPropertyChanged(nameof(Selected)); }
        }

        private bool showSelected;
        public bool ShowSelected
        {
            get => showSelected;
            set { showSelected = value; OnPropertyChanged(nameof(ShowSelected)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AndroidDeviceInfo : INotifyPropertyChanged
    {
        private ObservableCollection<AppInfo> appsInfo;
        private ObservableCollection<AppInfo> selectedApps;

        public string DisplayName { get; set; }
        public string EmulatorID { get; set; }
        public ObservableCollection<AppInfo> AppsInfo
        {
            get => appsInfo;
            set
            {
                appsInfo = value;
                OnPropertyChanged(nameof(AppsInfo));
            }
        }

        public ObservableCollection<AppInfo> SelectedApps
        {
            get => selectedApps;
            set
            {
                selectedApps = value;
                OnPropertyChanged(nameof(SelectedApps));
            }
        }

        public AndroidDeviceInfo()
        {
            this.AppsInfo = new ObservableCollection<AppInfo>();
            this.SelectedApps = new ObservableCollection<AppInfo>();
            this.SelectedApps.CollectionChanged += SelectedApps_CollectionChanged;
        }

        private void SelectedApps_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.Action);
            //throw new NotImplementedException();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
