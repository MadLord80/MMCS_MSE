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
using System.Windows.Forms;
using System.IO;

namespace MMCS_Music_Server_Editor
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FolderBrowserDialog opendir = new FolderBrowserDialog();

        //paths
        private string INFO_path = "\\INFO";
        private string ALBUM_path, DISCID_path, HIST_path, RECORD_path, TITLE_path;

        private string DATA_path = "\\DATA";
        //private string CUSTOM_path = "\\CUSTOM";

        //Settings
        private string defALBUM_ID = "0000001";

        public MainWindow()
        {
            InitializeComponent();

            ALBUM_path = INFO_path + "\\ALBUM";
            DISCID_path = INFO_path + "\\DISCID";
            HIST_path = INFO_path + "\\HIST";
            RECORD_path = INFO_path + "\\RECORD";
            TITLE_path = INFO_path + "\\TITLE";
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (opendir.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_from_n04_with_rus\\AVUNIT";
                opendir.SelectedPath = path;

                string dir_path = opendir.SelectedPath;
                if (! Directory.Exists(dir_path + "\\INFO\\ALBUM\\ALBUM0000001") || ! File.Exists(dir_path + "\\INFO\\ALBUM\\ALBUM0000001\\ALBUM0000001.lst"))
                {
                    System.Windows.MessageBox.Show("No ALBUM0000001.lst!");
                }
            }
        }
    }
}
