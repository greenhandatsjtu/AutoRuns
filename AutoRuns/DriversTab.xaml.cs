using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;

namespace AutoRuns
{
    public partial class DriversTab : UserControl
    {
        public ObservableCollection<DriverRegistry> drivers = new ObservableCollection<DriverRegistry>();
        public CollectionView filterView;

        public DriversTab()
        {
            InitializeComponent();
            ItemList.ItemsSource = drivers;
            filterView = (CollectionView) CollectionViewSource.GetDefaultView(ItemList.ItemsSource);
        }

        private void SearchDrivers(string entry, ref ObservableCollection<DriverRegistry> drivers)
        {
            using (var key =
                Registry.LocalMachine.OpenSubKey(entry))
            {
                var keys = key.GetSubKeyNames();
                foreach (var subkeyName in keys)
                {
                    var p = new DriverRegistry();
                    using (var subKey = key.OpenSubKey(subkeyName))
                    {
                        try
                        {
                            var valueNames = subKey.GetValueNames();
                            p.Entry = subkeyName;
                            string displayName = null;
                            if (valueNames.Contains("ImagePath")) p.ImagePath = (string) subKey.GetValue("ImagePath");
                            else
                                continue;
                            //如果不以.sys结尾，说明不是驱动，跳过即可
                            if (!p.ImagePath.ToLower().EndsWith("sys")) continue;
                            p.ImagePath =
                                Environment.ExpandEnvironmentVariables(
                                    @"%SystemRoot%\" + p.ImagePath.Replace(@"\SystemRoot\", ""));

                            if (valueNames.Contains("Description"))
                                p.Description = (string) subKey.GetValue("Description");

                            if (valueNames.Contains("DisplayName"))
                                displayName = (string) subKey.GetValue("DisplayName");
                            var fileVersionInfo = Utils.FetchInfo(p.ImagePath);
                            try
                            {
                                //从签名获取publisher
                                p.Publisher = Utils.GetPublisher(Utils.GetFilePath(p.ImagePath));
                                // Console.WriteLine(Utils.GetFilePath(p.ImagePath)+" 有数字签名");
                            }
                            catch (Exception)
                            {
                            }
                            finally
                            {
                                // 如果没有成功读取Publisher，尝试用文件信息的公司名替代
                                p.Publisher ??= fileVersionInfo.CompanyName;
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
                                p.Description = fileVersionInfo.FileDescription;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(subkeyName);
                            Console.WriteLine(e);
                            continue;
                        }
                    }

                    drivers.Add(p);
                }
            }
        }

        private void _IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue &&drivers.Count == 0)
            {
                drivers.Clear();
                SearchDrivers(@"SYSTEM\CurrentControlSet\Services", ref drivers);
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

        public class DriverRegistry : Record
        {
        }
    }
}