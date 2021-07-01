using System;
using System.Collections.Generic;
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
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using WK.Libraries.BetterFolderBrowserNS;
using Path = System.IO.Path;

namespace ReposCleaner
{
    public partial class MainWindow : Window
    {
        string projectFolderPath = null;
        string projectPath = null;
        string clearedProjectFolderPath = null;
        string zipPath = null;
        string mainDirectoryPath = null;
        string projectName = null;
        string projectFileName = null;
        string choosenDirectoryPath = null;
        public MainWindow()
        {
            InitializeComponent();
        }
        #region Buttons Methods
        private void pathButton_Click(object sender, RoutedEventArgs e)
        {
            string path = null;
            using (var dialog = new BetterFolderBrowser())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    path = dialog.SelectedPath;
                pathTextBox.Text = path;
            }
        }
        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(pathTextBox.Text))
            {
                MessageBox.Show("Выберите папку");
                return;
            }
            pathTextBox.IsEnabled = false;
            pathButton.IsEnabled = false;
            ExecuteButton.IsEnabled = false;
            AcceptButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
            string[] csprojPaths = Directory.GetFiles(pathTextBox.Text, "*.csproj", SearchOption.AllDirectories);
            if (csprojPaths.Length != 1)
            {
                MessageBox.Show("Выбрана некорректная папка");
                return;
            }
            CreateFolder("..\\..\\Temp");
            choosenDirectoryPath = "..\\..\\Temp\\" + pathTextBox.Text.Split('\\').Last();
            try
            {
                MoveDirectory(pathTextBox.Text, choosenDirectoryPath);
            }
            catch
            {
                MessageBox.Show("Выключите лишние приложения которые могут мешать работать с выбранным каталогом");
                while (true)
                {
                    try
                    {
                        MoveDirectory(pathTextBox.Text, choosenDirectoryPath);
                        break;
                    }
                    catch
                    {
                        MessageBox.Show("Выключите лишние приложения которые могут мешать работать с выбранным каталогом");
                    }
                }
            }
            try
            {
                mainDirectoryPath = Directory.GetParent(pathTextBox.Text).FullName;
                csprojPaths = Directory.GetFiles(choosenDirectoryPath, "*.csproj", SearchOption.AllDirectories);
                projectPath = csprojPaths[0];
                projectFileName = Path.GetFileName(projectPath);
                projectName = projectFileName.Split('.')[0];
                projectFolderPath = Path.GetDirectoryName(projectPath);
                clearedProjectFolderPath = Path.Combine(mainDirectoryPath, projectName);
                zipPath = Path.Combine(mainDirectoryPath, projectName + ".zip");
                StreamReader sr = new StreamReader(projectPath);
                List<string> notDelFiles = new List<string>();
                while (!sr.EndOfStream)
                {
                    string csprojStr = sr.ReadLine();
                    if (csprojStr.Trim() == "<ItemGroup>")
                    {
                        while (csprojStr.Trim() != "</ItemGroup>")
                        {
                            csprojStr = sr.ReadLine();
                            int q = csprojStr.IndexOf("Include");
                            int w = csprojStr.IndexOf("Reference");
                            int z = csprojStr.IndexOf("BootstrapperPackage");
                            if (q != -1 && w == -1 && z == -1)
                            {
                                notDelFiles.Add(csprojStr.Split('\"')[1]);
                            }
                        }
                    }
                }
                sr.Close();
                CreateFolder(clearedProjectFolderPath);
                foreach (var v in notDelFiles)
                {
                    if (v.Split('\\').Length > 0)
                    {
                        string NewCombinedPath = clearedProjectFolderPath;
                        for (int i = 0; i < v.Split('\\').Length - 1; i++)
                        {
                            NewCombinedPath = Path.Combine(NewCombinedPath, v.Split('\\')[i]);
                            CreateFolder(NewCombinedPath);
                        }
                    }
                    if (!File.Exists(System.IO.Path.Combine(clearedProjectFolderPath, v)))
                        File.Copy(System.IO.Path.Combine(projectFolderPath, v), System.IO.Path.Combine(clearedProjectFolderPath, v));
                }
                if (!File.Exists(System.IO.Path.Combine(clearedProjectFolderPath, projectFileName)))
                    File.Copy(projectPath, System.IO.Path.Combine(clearedProjectFolderPath, projectFileName));
                CreateAcriveOrFolder();
                WriteChangedSize();
                MessageBox.Show("Work done successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка, возвращаем все обратно! \n" + ex.Message);
                Return();
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Return();
            pathTextBox.IsEnabled = true;
            pathTextBox.Text = "";
            ChangeSizeLabel.Content = "Было:             Стало:";
            pathButton.IsEnabled = true;
            ExecuteButton.IsEnabled = true;
            AcceptButton.IsEnabled = false;
            CancelButton.IsEnabled = false;

        }
        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            pathTextBox.IsEnabled = true;
            pathTextBox.Text = "";
            ChangeSizeLabel.Content = "Было:             Стало:";
            pathButton.IsEnabled = true;
            ExecuteButton.IsEnabled = true;
            AcceptButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            while (true)
            {
                try
                {
                    if (Directory.Exists(choosenDirectoryPath))
                        Directory.Delete(choosenDirectoryPath, true);
                    break;
                }
                catch (Exception ex)
                {

                    MessageBox.Show("Ошибка, выключите все лишние приложения, иначе мы не сможем все вернуть обратно :( \n" + ex.Message);
                }
            }
        }
        #endregion
        #region Tech Methods
        void Return()
        {
            while (true)
            {
                try
                {
                    //удаляет все лишнее, только, если есть оригинальная папка в Temp
                    if (Directory.Exists(choosenDirectoryPath))
                    {
                        if (Directory.Exists(clearedProjectFolderPath))
                            Directory.Delete(clearedProjectFolderPath, true);
                        if (File.Exists(zipPath))
                            File.Delete(zipPath);
                        MoveDirectory(choosenDirectoryPath, pathTextBox.Text);
                        break;
                    }
                    else
                        MessageBox.Show("Ошибка в void Return()");
                }
                catch (Exception ex)
                {

                    MessageBox.Show("Ошибка, выключите все лишние приложения, иначе мы не сможем все вернуть обратно :( \n" + ex.Message);
                }
            }
        }
        void CreateAcriveOrFolder()
        {
            if (ArchiveCheckBox.IsChecked == true)
            {
                ZipFile.CreateFromDirectory(clearedProjectFolderPath, zipPath);
                if (Directory.Exists(clearedProjectFolderPath))
                    Directory.Delete(clearedProjectFolderPath, true);
            }
        }
        void WriteChangedSize()
        {
            if (ArchiveCheckBox.IsChecked == true)
                ChangeSizeLabel.Content = $"Было: {GetDirectorySize(choosenDirectoryPath)} байт \t Стало:{new FileInfo(zipPath).Length} байт";
            else
                ChangeSizeLabel.Content = $"Было: {GetDirectorySize(choosenDirectoryPath)} байт \t Стало:{GetDirectorySize(clearedProjectFolderPath)} байт";
        }
        static void CreateFolder(string Path)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(Path);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }
        }
        private static long GetDirectorySize(string folderPath)
        {
            DirectoryInfo di = new DirectoryInfo(folderPath);
            return di.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length);
        }
        void MoveDirectory(string sourcePath, string targetPath)
        {
            if (Directory.Exists(sourcePath))
            {
                CopyFilesRecursively(sourcePath, targetPath);
                Directory.Delete(sourcePath, true);
            }
        }
        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }
        #endregion
    }
}
