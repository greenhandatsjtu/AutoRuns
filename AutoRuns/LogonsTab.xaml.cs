using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;

namespace AutoRuns
{
    public partial class LogonsTab : UserControl
    {
        //自启动目录
        private readonly List<string> _directories = new List<string>
        {
            @"%USERPROFILE%\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup",
            @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup"
        };

        //自启动相关注册表子键
        private readonly List<string> _registryEntries = new List<string>
        {
            @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
            @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
            @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run",
            @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run",
            @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
            @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
            @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnceEx",
            @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnceEx",
            @"HKLM\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run",
            @"HKCU\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run"
        };

        public ObservableCollection<Logon> Logons = new ObservableCollection<Logon>();
        private readonly CollectionView filterView;

        public LogonsTab()
        {
            InitializeComponent();
            ItemList.ItemsSource = Logons;
            filterView = (CollectionView) CollectionViewSource.GetDefaultView(ItemList.ItemsSource);
        }

        //在相关注册表查询logon
        private void SearchRegistryLogons(List<string> entries, ref ObservableCollection<Logon> registries)
        {
            foreach (var entry in entries)
            {
                RegistryKey root;
                var newEntry = entry.Substring(5);
                switch (entry.Substring(0, 4))
                {
                    case "HKLM":
                        root = Registry.LocalMachine;
                        break;
                    case "HKCU":
                        root = Registry.CurrentUser;
                        break;
                    default: return;
                }

                using (var key =
                    root.OpenSubKey(newEntry))
                {
                    if (key != null)
                    {
                        var valueNames = key.GetValueNames();
                        foreach (var valueName in valueNames)
                        {
                            var value = new Logon();
                            value.Path = entry;
                            value.Entry = valueName;
                            value.ImagePath = (string) key.GetValue(value.Entry);
                            var fileVersionInfo = Utils.FetchInfo(value.ImagePath);
                            value.Description = fileVersionInfo?.FileDescription;
                            try
                            {
                                //从签名获取publisher
                                value.Publisher = Utils.GetPublisher(Utils.GetFilePath(value.ImagePath));
                            }
                            catch (Exception)
                            {
                                Console.WriteLine(Utils.GetFilePath(value.ImagePath) + " 没有数字签名");
                            }
                            finally
                            {
                                // 如果没有成功读取Publisher，尝试用文件信息的公司名替代
                                value.Publisher ??= fileVersionInfo?.CompanyName;
                            }

                            registries.Add(value);
                        }
                    }
                }
            }
        }

        //搜索相关文件夹里的logon
        private void SearchDirectoryLogons(List<string> directories, ref ObservableCollection<Logon> executions)
        {
            //遍历相关文件夹
            foreach (var directory in directories)
            {
                //用来解析%USERPROFILE%这些环境变量
                var filePath = Environment.ExpandEnvironmentVariables(directory);
                var files = Directory.GetFiles(filePath);
                //读取每个logon的信息
                foreach (var file in files)
                {
                    var value = new Logon();
                    FileVersionInfo fileVersionInfo;
                    if (Path.GetExtension(file).Equals("lnk"))
                        fileVersionInfo = Utils.FetchInfo(file);
                    else
                        fileVersionInfo = Utils.FetchInfo(file);
                    value.Entry = Path.GetFileName(file);
                    value.Description = fileVersionInfo.FileDescription;
                    value.Path = directory;
                    value.Publisher = fileVersionInfo.CompanyName;
                    value.ImagePath = file;
                    executions.Add(value);
                }
            }
        }

        private void _IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool) e.NewValue&&Logons.Count==0)
            {
                Logons.Clear();
                var view = (CollectionView) CollectionViewSource.GetDefaultView(ItemList.ItemsSource);
                view.GroupDescriptions.Clear();
                SearchRegistryLogons(_registryEntries, ref Logons);
                SearchDirectoryLogons(_directories, ref Logons);
                var groupDescription = new PropertyGroupDescription("Path");
                view.GroupDescriptions.Add(groupDescription);
                addFilter();
            }
        }
        
        /// <summary>
        /// 添加过滤器
        /// </summary>
        private void addFilter()
        {
            if (ms.IsChecked ?? false)
            {
                if (win.IsChecked ?? false)
                    filterView.Filter = Utils.both_filter;
                else
                    filterView.Filter = Utils.MS_filter;
            }
            else
            {
                if (win.IsChecked ?? false)
                    filterView.Filter = Utils.win_filter;
                else
                    filterView.Filter = null;
            }
        }

        private void Toggle(object sender, RoutedEventArgs e)
        {
            addFilter();
        }

        public class Logon : Record
        {
            public string Path { get; set; }
        }
    }
}