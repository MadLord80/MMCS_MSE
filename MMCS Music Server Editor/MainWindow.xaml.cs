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
		private ObservableCollection<MSDisc> discs = new ObservableCollection<MSDisc>();
		private ObservableCollection<MSTrack> tracks = new ObservableCollection<MSTrack>();

		public MainWindow()
        {
            InitializeComponent();
            GroupsListView.ItemsSource = groups;
			TrackslistView.ItemsSource = tracks;

			radioButton1.IsChecked = true;

			radioButton_Copy2.IsChecked = true;

			editButtonTemplate.Click += new RoutedEventHandler(on_editList);
			delButtonTemplate.Click += new RoutedEventHandler(on_delList);
			addButtonTemplate.Click += new RoutedEventHandler(on_addList);
			copyButtonTemplate.Click += new RoutedEventHandler(on_copyList);
		}

		private void triggerButtons(bool onoff)
		{
			editButtonTemplate.IsEnabled = onoff;
			delButtonTemplate.IsEnabled = onoff;
			addButtonTemplate.IsEnabled = onoff;
			copyButtonTemplate.IsEnabled = onoff;
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
			lists.Clear(); groups.Clear(); discs.Clear(); tracks.Clear();
			artistTitlelabel.Content = "";
			tableLableTemplate.Content = "";
			listViewTemplate.View = null;
			listViewTemplate.ItemsSource = null;
			triggerButtons(false);

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
			tableLableTemplate.Content = "Lists (max 100)";
			GridView lview = new GridView();
			listViewTemplate.View = lview;
			lview.Columns.Add(new GridViewColumn() { Header = "Id", Width = 40, DisplayMemberBinding = new System.Windows.Data.Binding("Id") });
			lview.Columns.Add(new GridViewColumn() { Header = "Name", Width = 240, DisplayMemberBinding = new System.Windows.Data.Binding("Name") });
			lview.Columns.Add(new GridViewColumn() { Header = "Tracks", Width = 50, DisplayMemberBinding = new System.Windows.Data.Binding("SongsCnt") });
			listViewTemplate.ItemsSource = lists;
			triggerButtons(true);
			copyButtonTemplate.ToolTip = "Copy Name to clipboard";

			string album_path = mserver.get_ALBUMpath();
			if (!File.Exists(album_path))
			{
				System.Windows.MessageBox.Show(album_path + " not found!");
				return;
			}
			FileStream fs = new FileStream(album_path, FileMode.Open, FileAccess.Read);
			
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
		
		private void fill_disks_table()
		{
			discs.Clear();
			tableLableTemplate.Content = "Discs";
			GridView lview = new GridView();
			listViewTemplate.View = lview;
			lview.Columns.Add(new GridViewColumn() { Header = "hexId", Width = 40, DisplayMemberBinding = new System.Windows.Data.Binding("Id") });
			lview.Columns.Add(new GridViewColumn() { Header = "Name", Width = 150, DisplayMemberBinding = new System.Windows.Data.Binding("Name") });
			lview.Columns.Add(new GridViewColumn() { Header = "Artist", Width = 150, DisplayMemberBinding = new System.Windows.Data.Binding("Artist") });
			lview.Columns.Add(new GridViewColumn() { Header = "Tracks", Width = 50, DisplayMemberBinding = new System.Windows.Data.Binding("SongsCnt") });
			listViewTemplate.ItemsSource = discs;
			triggerButtons(true);
			copyButtonTemplate.ToolTip = "Copy Name-Artist to clipboard";

			string org_path = mserver.get_ORGpath();
			if (!File.Exists(org_path))
			{
				System.Windows.MessageBox.Show(org_path + " not found!");
				return;
			}
			FileStream fs = new FileStream(org_path, FileMode.Open, FileAccess.Read);

			byte[] temp = new byte[0];

			byte[] discs_cnt = new byte[mserver.discs_cnt_length];
			fs.Position = mserver.discs_cnt_offset;
			fs.Read(discs_cnt, 0, discs_cnt.Length);
			int discs_count = BitConverter.ToInt32(discs_cnt, 0);

			byte[] disc_desc = new byte[mserver.disc_desc_length];
			for (int i = 0; i < discs_count; i++)
			{
				fs.Read(disc_desc, 0, disc_desc.Length);
				hf.spliceByteArray(disc_desc, ref temp, mserver.discId_offset + 3, 1);
				byte disc_id = temp[0];
				hf.spliceByteArray(disc_desc, ref temp, mserver.discName_offset, mserver.discName_length);
				string disc_name = hf.ByteArrayToString(temp, codePage);
				//Artist может не быть в ORG_ARRAY, но быть в TITLEXX000001.lst!
				hf.spliceByteArray(disc_desc, ref temp, mserver.discName_offset + mserver.discName_length, mserver.discArtist_length);
				string disc_artist = hf.ByteArrayToString(temp, codePage);
				hf.spliceByteArray(disc_desc, ref temp, mserver.discName_offset + mserver.discName_length + mserver.discArtist_length, mserver.disc_songscnt_length);
				int songs = BitConverter.ToInt32(temp, 0);

				discs.Add(new MSDisc(disc_id, disc_name, disc_artist, songs));
			}
		}

		private void fill_tracks_table()
		{
			tracks.Clear();
			artistTitlelabel.Content = "";

			Type itemType = listViewTemplate.SelectedItem.GetType();

			if (itemType.Name == "MSDisc")
			{
				MSDisc disc = (listViewTemplate.SelectedItem as MSDisc);
				int disc_id = disc.byteId;
				fill_tracks(disc_id, new int[0]);
			}
			else
			{
				MSList list = (listViewTemplate.SelectedItem as MSList);
				foreach (KeyValuePair<int, int[]> disc in list.Songs)
				{
					fill_tracks(disc.Key, disc.Value);
				}
			}
			
		}

		private void fill_tracks(int disc_id, int[] tracks_arr)
		{
			string did = BitConverter.ToString(new byte[1] { (byte)disc_id });
			string title_path = mserver.get_TITLEpath(did);
			if (!File.Exists(title_path))
			{
				System.Windows.MessageBox.Show(title_path + " not found!");
				return;
			}
			FileStream fs = new FileStream(title_path, FileMode.Open, FileAccess.Read);

			byte[] temp = new byte[0];

			fs.Position = mserver.songs_cnt_offset;
			int songs_count = fs.ReadByte();

			byte[] track_data = new byte[mserver.tdiscName_length + mserver.tdiscArtist_length];
			fs.Position = mserver.tdiscName_offset;
			fs.Read(track_data, 0, track_data.Length);
			hf.spliceByteArray(track_data, ref temp, mserver.tdiscName_length, mserver.tdiscArtist_length);
			string disc_name = hf.ByteArrayToString(temp, codePage);
			//\x00 - end string
			int null_offset = disc_name.IndexOf('\x00');
			disc_name = (null_offset != -1) ? disc_name.Substring(0, null_offset) : disc_name;
			if (disc_name != "") artistTitlelabel.Content = ": " + disc_name;

			ObservableCollection<MSTrack> alltracks = new ObservableCollection<MSTrack>();
			for (int i = 0; i < songs_count; i++)
			{
				fs.Read(track_data, 0, track_data.Length);
				hf.spliceByteArray(track_data, ref temp, 0, mserver.tdiscName_length);
				string tname = hf.ByteArrayToString(temp, codePage);
				hf.spliceByteArray(track_data, ref temp, mserver.tdiscName_length, mserver.tdiscArtist_length);
				string tart = hf.ByteArrayToString(temp, codePage);
				alltracks.Add(new MSTrack(i + 1, tname, tart));
			}
			if (tracks_arr.Length > 0)
			{
				for (int i = 0; i < tracks_arr.Length; i++)
				{
					IEnumerable<MSTrack> track = alltracks.Where(t => t.Id == tracks_arr[i]);
					if (track.Count() > 0) tracks.Add(track.First());
				}
			}
			else
			{
				tracks = alltracks;
			}
			TrackslistView.ItemsSource = tracks;

			fs.Close();
		}

		private void on_editList(object sender, RoutedEventArgs args)
		{

		}

		private void on_delList(object sender, RoutedEventArgs args)
		{

		}

		private void on_addList(object sender, RoutedEventArgs args)
		{

		}

		private void on_copyList(object sender, RoutedEventArgs args)
		{
			if (listViewTemplate.SelectedItem == null) return;
			Type itemType = listViewTemplate.SelectedItem.GetType();
			if (itemType.Name == "MSList")
			{
				MSList list = (listViewTemplate.SelectedItem as MSList);
				System.Windows.Clipboard.SetText(list.Name);
			}
			else
			{
				MSDisc disc = (listViewTemplate.SelectedItem as MSDisc);
				string na = (disc.Artist == "") ? disc.Name : disc.Name + " - " + disc.Artist;
				System.Windows.Clipboard.SetText(na);
			}
		}

		private void GroupsListView_onclick(object sender, MouseButtonEventArgs e)
        {
			if (GroupsListView.SelectedItem == null) return;
			MSGroup group = (GroupsListView.SelectedItem as MSGroup);
			if (group.Id == 0)
			{
				//disks - ORG_ARRAY
				fill_disks_table();
			}
			else
			{
				//lists - ALBUM
				fill_lists_table(group.Id);
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

		private void button_Click(object sender, RoutedEventArgs e)
		{
			MSGroup group = (GroupsListView.SelectedItem as MSGroup);
			System.Windows.Clipboard.SetText(group.Name);
		}
				
		private void listViewTemplate_onclick(object sender, MouseButtonEventArgs e)
		{
			if (listViewTemplate.SelectedItem == null) return;
			
			fill_tracks_table();
		}
	}
}
