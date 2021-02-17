using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;

namespace AutoRuns
{
    public partial class KnownDLLsTab : UserControl
    {
        private readonly string _entry = @"SYSTEM\CurrentControlSet\Control\Session Manager\KnownDLLs";
        public ObservableCollection<MyDll> Dlls = new ObservableCollection<MyDll>();
        public CollectionView filterView;

        public KnownDLLsTab()
        {
            InitializeComponent();
            ItemList.ItemsSource = Dlls;
            filterView = (CollectionView) CollectionViewSource.GetDefaultView(ItemList.ItemsSource);
        }

        private void SearchDlls(string entry, ref ObservableCollection<MyDll> dlls)
        {
            RegistryKey root;
            root = Registry.LocalMachine;
            using (var key =
                root.OpenSubKey(entry))
            {
                if (key != null)
                {
                    var valueNames = key.GetValueNames();
                    foreach (var valueName in valueNames)
                    {
                        var value = new MyDll {Entry = valueName};
                        var another = new MyDll {Entry = valueName};
                        value.ImagePath = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\") +
                                          (string) key.GetValue(value.Entry);
                        another.ImagePath = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\syswow64\") +
                            (string) key.GetValue(value.Entry);
                        try
                        {
                            //从签名获取publisher
                            value.Publisher = Utils.GetPublisher(Utils.GetFilePath(value.ImagePath));
                            // Console.WriteLine(Utils.GetFilePath(p.ImagePath)+" 有数字签名");
                        }
                        catch (Exception)
                        {
                        }

                        try
                        {
                            var fileVersionInfo = Utils.FetchInfo(value.ImagePath);
                            value.Description = fileVersionInfo?.FileDescription;
                            value.Publisher ??= fileVersionInfo?.CompanyName;
                        }
                        catch (FileNotFoundException)
                        {
                        }

                        dlls.Add(value);
                        
                        try
                        {
                            //从签名获取publisher
                            another.Publisher = Utils.GetPublisher(Utils.GetFilePath(another.ImagePath));
                            // Console.WriteLine(Utils.GetFilePath(p.ImagePath)+" 有数字签名");
                        }
                        catch (Exception)
                        {
                        }

                        try
                        {
                            var fileVersionInfo = Utils.FetchInfo(another.ImagePath);
                            another.Description = fileVersionInfo?.FileDescription;
                            another.Publisher ??= fileVersionInfo?.CompanyName;
                        }
                        catch (FileNotFoundException)
                        {
                        }

                        dlls.Add(another);
                    }
                }
            }
        }

        private void _IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool) e.NewValue&&Dlls.Count==0)
            {
                Dlls.Clear();
                SearchDlls(_entry, ref Dlls);
                addFilter();
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
    }

    public class MyDll : Record
    {
    }
}