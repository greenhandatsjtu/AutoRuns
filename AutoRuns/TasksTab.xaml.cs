using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using System.Xml;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace AutoRuns
{
    public partial class TasksTab : UserControl
    {
        public ObservableCollection<MyTask> Tasks = new ObservableCollection<MyTask>();
        public CollectionView filterView;

        public TasksTab()
        {
            InitializeComponent();
            ItemList.ItemsSource = Tasks;
            filterView = (CollectionView) CollectionViewSource.GetDefaultView(ItemList.ItemsSource);
        }

        /// <summary>
        ///     Find tasks using DFS, and parse XML
        /// </summary>
        /// <param name="rootPath">root path</param>
        /// <param name="tasks">scheduled tasks list</param>
        private void SearchTasks(ref ObservableCollection<MyTask> tasks)
        {
            var rootPath = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\Tasks");
            var queue = new Queue<string>();
            string[] files = { };
            string[] directories = { };
            queue.Enqueue(rootPath); //根节点入队
            //DFS
            do
            {
                var path = queue.Dequeue();
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (UnauthorizedAccessException) //权限问题
                {
                    continue;
                }

                foreach (var file in files)
                {
                    var newTask = new MyTask();
                    try
                    {
                        var doc = new XmlDocument();
                        doc.Load(file);
                        // var description = doc.DocumentElement.GetElementsByTagName("Description");
                        // if (description.Count > 0)
                        // {
                        //     newTask.Description = description[0].InnerText;
                        // }
                        try
                        {
                            newTask.ImagePath = doc.DocumentElement.GetElementsByTagName("Command")[0].InnerText;
                        }
                        catch (Exception)
                        {
                        }

                        if (newTask.ImagePath != null)
                        {
                            newTask.ImagePath = Environment.ExpandEnvironmentVariables(newTask.ImagePath);
                            try
                            {
                                var fileVersionInfo = Utils.FetchInfo(newTask.ImagePath);
                                newTask.Publisher = fileVersionInfo.CompanyName;
                                newTask.Description = fileVersionInfo.FileDescription;
                            }
                            catch (FileNotFoundException e)
                            {
                            }
                        }
                    }
                    catch (Exception e)
                    {
                    }
                    newTask.Entry = file;
                    newTask.Description ??= Path.GetFileName(file);
                    tasks.Add(newTask);
                }

                try
                {
                    directories = Directory.GetDirectories(path);
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                foreach (var directory in directories) queue.Enqueue(directory);
            } while (queue.Count != 0);
        }
        
        private void _IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue&&Tasks.Count == 0)
            {
                Tasks.Clear();
                SearchTasks(ref Tasks);
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

        public class MyTask : Record
        {
        }
    }
}