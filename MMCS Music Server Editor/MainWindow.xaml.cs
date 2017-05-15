using System;
using System.Collections;
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
using System.Collections.ObjectModel;

namespace MMCS_MSE
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FolderBrowserDialog opendir = new FolderBrowserDialog();

		private string codePage = "iso-8859-5";

        private help_functions hf = new help_functions();
        private MMCSServer mserver = new MMCSServer();
        private ObservableCollection<MSGroup> groups = new ObservableCollection<MSGroup>();
		private ObservableCollection<MSList> lists = new ObservableCollection<MSList>();

		public MainWindow()
        {
            InitializeComponent();
            GroupsListView.ItemsSource = groups;
			ListsListView.ItemsSource = lists;

			radioButton1.IsChecked = true;

			radioButton_Copy2.IsChecked = true;
		}

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (opendir.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_from_n04_with_rus\\AVUNIT";
                opendir.SelectedPath = path;

                string dir_path = opendir.SelectedPath;
				mserver.MainDir = dir_path;

                string info_path = mserver.get_INDEXpath();
                if (! File.Exists(info_path))
                {
                    System.Windows.MessageBox.Show(info_path + " not found!");
                    return;
                }
                fill_groups_table(info_path);                
            }
        }

        private void fill_groups_table(string path)
        {
			lists.Clear(); groups.Clear();
			FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            byte[] temp = new byte[0];

            fs.Position = mserver.cnt_disks_offset;
            int lists_count = fs.ReadByte();
            
			Dictionary<int, int[]> gl = new Dictionary<int, int[]>();
            for (int i = 0; i < lists_count; i++)
            {
                fs.Position = mserver.lists_offset + i * mserver.list_length + mserver.list_length - 4;
                int list_type = fs.ReadByte();
				fs.Position = mserver.lists_offset + i * mserver.list_length + mserver.list_length - 1;
				int gid = fs.ReadByte();

				int id_offset = (list_type == 0) ? 3 : 0;
				fs.Position = mserver.lists_offset + i * mserver.list_length + id_offset;
				int lid = fs.ReadByte();
				if (gl.ContainsKey(gid))
                {
					int[] lists = gl[gid];
					Array.Resize(ref lists, lists.Length + 1);
					lists[lists.Length - 1] = lid;
					gl[gid] = lists;

				}
                else
                {
					gl.Add(gid, new int[] {lid});
                }
            }

            fs.Position = mserver.groups_offset;
            byte[] group_desc = new byte[mserver.group_length];
            for (int i = 1; i <= mserver.cnt_groups; i++)
            {
                fs.Read(group_desc, 0, group_desc.Length);
                hf.spliceByteArray(group_desc, ref temp, 0, 4);
                int group_id = BitConverter.ToInt32(temp, 0);
                if (group_id == 0x010000ff) break;

                string group_name = "";
                hf.spliceByteArray(group_desc, ref temp, 4, 4);
                //←[tbl:NNN]
                if (BitConverter.ToUInt32(temp,0) == 0x62745b1b)
                {
                    string tbl_id = hf.ByteArrayToString(hf.spliceByteArray(group_desc, ref temp, 10, 3));
                    group_name = mserver.get_TBLdata(Convert.ToUInt32(tbl_id));
                }
                else
                {
                    group_name = hf.ByteArrayToString(hf.spliceByteArray(group_desc, ref temp, 4, group_desc.Length - 4), codePage);
                }

				int[] lists = (gl.ContainsKey((int)group_id)) ? gl[(int)group_id] : new int[0];
				groups.Add(new MSGroup(group_id, group_name, lists));
            }
			fs.Close();
        }

		private void fill_lists_table(int gid)
		{
			lists.Clear();

			string album_path = mserver.get_ALBUMpath();
			if (!File.Exists(album_path))
			{
				System.Windows.MessageBox.Show(album_path + " not found!");
				return;
			}
			FileStream fs = new FileStream(album_path, FileMode.Open, FileAccess.Read); ;
			
			int[] lists_sizes = new int[mserver.max_lists];

			fs.Position = mserver.lists_size_offset;
			for (int i = 0; i < lists_sizes.Length; i++)
			{
				byte[] list_size = new byte[mserver.list_size_length];
				fs.Read(list_size, 0, list_size.Length);
				int size = BitConverter.ToInt32(list_size, 0);
				if (size == 0)
				{
					Array.Resize(ref lists_sizes, i);
					break;
				}
				lists_sizes[i] = size;
			}

			//ПРОВЕРИТЬ ЛЮБИМЫЕ и 02!!!!
			MSGroup group = groups.Where(gr => gr.Id == gid).First();
			foreach (int lid in group.Lists)
			{
				byte[] temp = new byte[0];

				int list_offset = 0;
				for (int i = 0; i < lid - 1; i++)
				{
					list_offset += lists_sizes[i];
				}
				fs.Position = mserver.alists_offset + list_offset + mserver.a_unknown_length + mserver.listId_length + 1;
				int songs_cnt = fs.ReadByte();

				byte[] lname = new byte[mserver.listName_length];
				fs.Position = mserver.alists_offset + list_offset + mserver.listName_offset;
				fs.Read(lname, 0, lname.Length);
				string list_name = "";
				hf.spliceByteArray(lname, ref temp, 0, 4);
				//←[tbl:NNN]
				if (BitConverter.ToUInt32(temp, 0) == 0x62745b1b)
				{
					string tbl_id = hf.ByteArrayToString(hf.spliceByteArray(lname, ref temp, 6, 3));
					list_name = mserver.get_TBLdata(Convert.ToUInt32(tbl_id));
				}
				else
				{
					list_name = hf.ByteArrayToString(lname, codePage);
				}

				Dictionary<int, int[]> songs = new Dictionary<int, int[]>();
				byte[] song_data = new byte[mserver.asong_data_length];
				fs.Position = mserver.alists_offset + list_offset + mserver.list_desc_length;
				for (int i = 0; i < songs_cnt; i++)
				{
					fs.Read(song_data, 0, song_data.Length);
					if (songs.ContainsKey(song_data[3]))
					{
						int[] lsongs = songs[song_data[3]];
						Array.Resize(ref lsongs, lsongs.Length + 1);
						lsongs[lsongs.Length - 1] = song_data[4];
						songs[song_data[3]] = lsongs;
					}
					else
					{
						songs.Add(song_data[3], new int[] {song_data[4]});
					}
				}

				lists.Add(new MSList(lid, list_name, songs));
			}
			
			fs.Close();
		}

		private void GroupsListView_onclick(object sender, MouseButtonEventArgs e)
        {
            var listView = e.Source as System.Windows.Controls.ListView;
            if (listView != null)
            {
                if (listView.SelectedItem != null)
                {
                    MSGroup group = (listView.SelectedItem as MSGroup);
					if (group.Id == 0)
					{
						//disks - ORG_ARRAY
						//fill_disks_table();
					}
					else
					{
						//lists - ALBUM
						fill_lists_table(group.Id);
					}
					//System.Windows.MessageBox.Show(group.Name);                    
				}
            }
        }

		private void radioButton_Checked(object sender, RoutedEventArgs e)
		{
			System.Windows.Controls.RadioButton li = (sender as System.Windows.Controls.RadioButton);
			string dir = "";
			switch (li.Content.ToString())
			{
				case "J-01":
					dir = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_from_j01\\AVUNIT";
					break;
				case "J-03":
					dir = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_from_j03\\AVUNIT";
					break;
				case "N-04":
					dir = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_from_n04\\AVUNIT";
					break;
				case "R-03 empty":
					dir = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_from_r03_empty\\AVUNIT";
					break;
				case "N-04rus":
					dir = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_from_n04_with_rus\\AVUNIT";
					break;
				case "N-04rus empty":
					dir = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_from_n04_with_rus\\AVUNIT_empty";
					break;
				case "N-04rus - 2s":
					dir = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_from_n04_with_rus\\AVUNIT_minus2songs";
					break;
				case "N-04rus newcd":
					dir = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_from_n04_with_rus\\AVUNIT_newcd";
					break;
				case "N-04rus necd - 1d":
					dir = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_from_n04_with_rus\\AVUNIT_newcd_minus1disc";
					break;
				case "N-04rus newcd only":
					dir = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_from_n04_with_rus\\AVUNIT_newcd_only";
					break;
				case "N-04rus newcd delother":
					dir = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_from_n04_with_rus\\AVUNIT_newcdonly_delother";
					break;
				default:
					break;
			}

			mserver.MainDir = dir;
			string info_path = mserver.get_INDEXpath();
			if (!File.Exists(info_path))
			{
				lists.Clear(); groups.Clear();
				System.Windows.MessageBox.Show(info_path + " not found!");				
				return;
			}
			fill_groups_table(info_path);
		}

		private void radioButton1_Checked(object sender, RoutedEventArgs e)
		{
			System.Windows.Controls.RadioButton li = (sender as System.Windows.Controls.RadioButton);
			codePage = li.Content.ToString();
		}
	}
}
