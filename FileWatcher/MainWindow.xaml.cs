using Microsoft.Win32;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;

namespace FileWatcher
{
    public partial class MainWindow
    {
        private readonly Regex _regex = new Regex("(public|private) (\\w+ )?(?<Type>[\\w?]+) (?<PropertyName>\\w+) { get; set; }", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void WatchFileClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "cs classes (*.cs)|*.cs"
            };
            if (dialog.ShowDialog() != true) return;

            LoadProperties(dialog.FileName);

            var watcher = new FileSystemWatcher(Path.GetDirectoryName(dialog.FileName))
            {
                Filter = Path.GetFileName(dialog.FileName),
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                EnableRaisingEvents = true,
            };

            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                LogEditor.Text += $"File: {e.FullPath} {e.ChangeType}";
            }));

            LoadProperties(e.FullPath);
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                LogEditor.Text += $"File: {e.OldFullPath} renamed to {e.FullPath}";
            }));
        }

        private void LoadProperties(string fullpath)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                using (var fileStream = new FileStream(fullpath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var textReader = new StreamReader(fileStream))
                {
                    var text = textReader.ReadToEnd();
                    var matches = _regex.Matches(text);

                    ListView.Items.Clear();
                    foreach (Match match in matches)
                    {
                        var type = match.Groups["Type"].Value;
                        var propertyName = match.Groups["PropertyName"].Value;

                        ListView.Items.Add($"{propertyName}: {type}");
                    }
                }
            }));
        }
    }
}
