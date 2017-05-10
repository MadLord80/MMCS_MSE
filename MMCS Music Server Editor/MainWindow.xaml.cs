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

namespace MMCS_MSE
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FolderBrowserDialog opendir = new FolderBrowserDialog();

        private help_functions hf = new help_functions();
        private MMCSServer mserver;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (opendir.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_from_n04_with_rus\\AVUNIT";
                opendir.SelectedPath = path;

                string dir_path = opendir.SelectedPath;
                mserver = new MMCSServer(dir_path);

                string info_path = mserver.get_INDEXpath();
                if (! File.Exists(info_path))
                {
                    System.Windows.MessageBox.Show(info_path + " not found!");
                    return;
                }
                fill_groups_table(info_path);

                //string album_path = mserver.get_ALBUMpath();
                //if (! File.Exists(album_path))
                //{
                //    System.Windows.MessageBox.Show(album_path + " not found!");
                //    return;
                //}
            }
        }

        private void fill_groups_table(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            byte[] temp = new byte[0];

            //fs.Position = mserver.cnt_disks_offset;
            //int disks_count = fs.ReadByte();

            ListBoxItem row = new ListBoxItem();

            fs.Position = mserver.groups_offset;
            byte[] group_desc = new byte[mserver.groups_length];
            for (int i = 1; i <= mserver.cnt_groups; i++)
            {
                fs.Read(group_desc, 0, group_desc.Length);
                hf.spliceByteArray(group_desc, ref temp, 0, 4);
                if (BitConverter.ToUInt32(temp, 0) == 0x010000ff) break;

                hf.spliceByteArray(group_desc, ref temp, 4, 4);
                //←[tbl:NNN]
                if (BitConverter.ToUInt32(temp,0) == 0x62745b1b)
                {
                    hf.spliceByteArray(group_desc, ref temp, 10, 3);
                    if (hf.ByteArrayToString(temp) == "171")
                    {
                        //row.ad
                    }
                }
            }
        }
    }
}
