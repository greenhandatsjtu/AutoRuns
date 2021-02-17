using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;

namespace AutoRuns
{
    public partial class ServicesTab : UserControl
    {
        private readonly CollectionView filterView;
        public ObservableCollection<ServiceRegistry> Registries = new ObservableCollection<ServiceRegistry>();

        public ServicesTab()
        {
            InitializeComponent();
            ItemList.ItemsSource = Registries;
            filterView = (CollectionView) CollectionViewSource.GetDefaultView(ItemList.ItemsSource);
        }

        private void SearchRegistries(string entry, ObservableCollection<ServiceRegistry> registries)
        {
            Console.WriteLine("Searching services...");
            using (var key =
                Registry.LocalMachine.OpenSubKey(entry))
            {
                var keys = key.GetSubKeyNames();
                foreach (var subkeyName in keys)
                {
                    var p = new ServiceRegistry();
                    using (var subKey = key.OpenSubKey(subkeyName))
                    {
                        try
                        {
                            p.Entry = subkeyName;
                            string displayName = null;
                            var valueNames = subKey.GetValueNames();
                            if (valueNames.Contains("Start")) p.Start = (int) subKey.GetValue("Start");

                            if (valueNames.Contains("Type")) p.Type = (int) subKey.GetValue("Type");
                            if (valueNames.Contains("ImagePath"))
                                p.ImagePath = (string) subKey.GetValue("ImagePath");
                            else
                                continue; // 没有ImagePath就跳过

                            //跳过驱动
                            if (p.ImagePath != null && p.ImagePath.ToLower().EndsWith("sys")) continue;

                            if (valueNames.Contains("Description"))
                                p.Description = (string) subKey.GetValue("Description");

                            if (valueNames.Contains("DisplayName"))
                                displayName = (string) subKey.GetValue("DisplayName");

                            if (p.ImagePath != null)
                            {
                                //Svchost服务，需要读取子键Parameters的ServiceDll
                                if (p.ImagePath.Contains("svchost.exe"))
                                {
                                    if (!subKey.GetSubKeyNames().Contains("Parameters")) continue;
                                    using var subSubKey = subKey.OpenSubKey("Parameters");
                                    p.ImagePath = (string) subSubKey.GetValue("ServiceDll");
                                }

                                var fileVersionInfo = Utils.FetchInfo(p.ImagePath);
                                try
                                {
                                    //从签名获取publisher
                                    p.Publisher = Utils.GetPublisher(Utils.GetFilePath(p.ImagePath));
                                }
                                catch (Exception)
                                {
                                    // Console.WriteLine(Utils.GetFilePath(p.ImagePath)+" 没有数字签名");
                                }
                                finally
                                {
                                    // 如果没有成功读取Publisher，尝试用文件信息的公司名替代
                                    p.Publisher ??= fileVersionInfo?.CompanyName;
                                }

                                //读取重定向的DisplayName和Description
                                if (Utils.IsRedirected(p.Description))
                                    p.Description = Utils.LoadMuiStringValue(subKey, "Description");
                                if (Utils.IsRedirected(displayName))
                                    displayName = Utils.LoadMuiStringValue(subKey, "DisplayName");

                                if (displayName != null || p.Description != null)
                                {
                                    if (displayName != null && p.Description != null)
                                        p.Description = displayName + ": " + p.Description;
                                    else
                                        p.Description = displayName ?? p.Description;
                                }
                                else
                                {
                                    p.Description = fileVersionInfo?.FileDescription;
                                }
                            }
                        }
                        //没有读取子键的权限，以管理员身份运行即可
                        catch (SecurityException)
                        {
                            continue;
                        }
                        //文件不存在
                        catch (FileNotFoundException)
                        {
                            Console.WriteLine(p.ImagePath + " 文件不存在");
                        }
                    }

                    registries.Add(p);
                }
            }
        }

        /// <summary>
        ///     添加过滤器
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

        private void _IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool) e.NewValue && Registries.Count == 0)
            {
                Registries.Clear();
                SearchRegistries(@"SYSTEM\CurrentControlSet\Services", Registries);
                addFilter();
            }
        }
        
    }

    public class ServiceRegistry : Record
    {
        public ServiceRegistry()
        {
            Start = -1;
            Type = -1;
        }

        public int Start { get; set; }
        public int Type { get; set; }
    }
}