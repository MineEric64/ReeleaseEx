using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using syntaxERROR.OPtion;

using Ionic.Zip;
using Ookii.Dialogs.Wpf;

using Path = System.IO.Path;

namespace ReeleaseEx
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Version AppVersion => Assembly.GetExecutingAssembly().GetName().Version;

        public static OPtion<JArray> Options { get; set; } = new OPtion<JArray>(Path.Combine(AppContext.BaseDirectory, "settings.json"));

        public static List<ToolConfig> Tools { get; set; } = new List<ToolConfig>();
        public static ToolConfig SelectedTool { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Options.FilePath))
            {
                Options.Load();

                LoadOption();
                AddToolsToControl();
            }
            else
            {
                Options.Data = new JArray();
            }

            this.Title = $"Reelease! v{AppVersion}";
        }

        private void ItemAdd_Click(object sender, RoutedEventArgs e)
        {
            var textOk = new ChooseText();

            textOk.TextAdded += (object s, string text) =>
            {
                var tool = ToolConfig.Empty;
                tool.Name = text;

                Tools.Add(tool);
                Items.Items.Add(text);

                Items.SelectedItem = text;
            };
            textOk.Show();
        }

        private void Items_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Items.SelectedIndex >= 0)
            {
                SelectedTool = Tools[Items.SelectedIndex];
                LoadToControl();
            }
        }

        private void ItemRemove_Click(object sender, RoutedEventArgs e)
        {
            if (Items.SelectedIndex >= 0)
            {
                int index = Items.SelectedIndex != 0 ? Items.SelectedIndex - 1 : 0;

                Tools.Remove(SelectedTool);
                Items.Items.Remove(SelectedTool.Name);

                SelectedTool = Tools[index];
            }
        }

        private void DirPathOK_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new VistaFolderBrowserDialog()
            {
                Multiselect = false,
                Description = "Please select the directory to add files"
            };

            bool? result = ofd.ShowDialog();

            if (result.GetValueOrDefault())
            {
                SelectedTool.DirectoryPathFrom = ofd.SelectedPath;
                DirPath.Text = ofd.SelectedPath;
            }
        }

        private void FileAdd_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new VistaOpenFileDialog()
            {
                AddExtension = true,
                CheckFileExists = false,
                CheckPathExists = false,
                Multiselect = true,
                Title = "Please select the files to add"
            };
            if (!string.IsNullOrWhiteSpace(SelectedTool.DirectoryPathFrom)) ofd.InitialDirectory = SelectedTool.DirectoryPathFrom;

            bool? result = ofd.ShowDialog();

            if (result.GetValueOrDefault())
            {
                foreach (string filePath in ofd.FileNames)
                {
                    string fileName = filePath.Replace($@"{SelectedTool.DirectoryPathFrom}\", string.Empty);

                    if (!SelectedTool.AddedFiles.Contains(fileName))
                    {
                        SelectedTool.AddedFiles.Add(fileName);
                        AddedList.Items.Add(fileName);
                    }
                }
            }
        }

        private void FileRemove_Click(object sender, RoutedEventArgs e)
        {
            int index = AddedList.SelectedIndex;

            if (index >= 0)
            {
                AddedList.Items.RemoveAt(index);
                SelectedTool.AddedFiles.RemoveAt(index);
            }
        }

        private void SavedDirOK_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new VistaFolderBrowserDialog()
            {
                Multiselect = false,
                Description = "Please select the directory to save compressed file"
            };

            bool? result = ofd.ShowDialog();

            if (result.GetValueOrDefault())
            {
                SelectedTool.DirectoryPathTo = ofd.SelectedPath;
                SavedDir.Text = ofd.SelectedPath;
            }
        }

        private void SavedDirOpen_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", SavedDir.Text);
        }

        private void Reelease_Click(object sender, RoutedEventArgs e)
        {
            SelectedTool.ZipName = ZipName.Text;
            SelectedTool.ZipNameParams = ZipNameParam.Text;

            SaveOption();
            Reelease();

            MessageBox.Show("Reeleased!", this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void LoadOption()
        {
            List<JObject> json = Options.Data.ToObject<List<JObject>>();

            foreach (var tool in json)
            {
                Tools.Add(tool.ToObject<ToolConfig>());
            }
        }

        public void SaveOption()
        {
            Options.Data = new JArray();

            foreach (var tool in Tools)
            {
                Options.Data.Add(JObject.FromObject(tool));
            }
            Options.Save();
        }
        
        public void Reelease()
        {
            string[] param = SelectedTool.ZipNameParams.Split(';');
            string name = string.Format(SelectedTool.ZipName, param);

            string path = Path.Combine(SelectedTool.DirectoryPathTo, name);

            using (var zip = new ZipFile())
            {
                foreach (var fileName in SelectedTool.AddedFiles)
                {
                    string filePath = Path.Combine(SelectedTool.DirectoryPathFrom, fileName);
                    string directoryName = "";

                    if (fileName.Contains(@"\")) directoryName = Directory.GetParent(filePath).FullName.Replace(SelectedTool.DirectoryPathFrom, "");
                    zip.AddFile(filePath, directoryName);
                }

                zip.Save($"{path}.zip");
            }
        }

        public void LoadToControl()
        {
            DirPath.Text = SelectedTool.DirectoryPathFrom;
            ZipName.Text = SelectedTool.ZipName;
            ZipNameParam.Text = SelectedTool.ZipNameParams;
            SavedDir.Text = SelectedTool.DirectoryPathTo;

            AddedList.Items.Clear();

            foreach (var fileName in SelectedTool.AddedFiles)
            {
                AddedList.Items.Add(fileName);
            }
        }

        public void AddToolsToControl()
        {
            foreach (var tool in Tools)
            {
                Items.Items.Add(tool.Name);
            }
        }
    }
}
