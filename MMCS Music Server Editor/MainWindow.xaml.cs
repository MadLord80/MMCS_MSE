using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
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
using System.Collections.Specialized;
using System.Reflection;

namespace MMCS_MSE
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FolderBrowserDialog opendir = new FolderBrowserDialog();
		private byte[] temp = new byte[0];

		private string codePage = "iso-8859-5";

        private help_functions hf = new help_functions();
        private MMCSServer mserver = new MMCSServer();
		private ObservableCollection<MSGroup> groups = new ObservableCollection<MSGroup>();
		private ObservableCollection<MSList> lists = new ObservableCollection<MSList>();
		private ObservableCollection<MSDisc> discs = new ObservableCollection<MSDisc>();
		
		internal string CodePage
		{
			get { return this.codePage; }
		}
		
		internal bool is_favTrack(MSTrack track)
		{
			List<MSList> ls = this.lists.Where(l => l.Songs.Contains(track)).ToList();
			return (ls.Count > 0) ? true : false;
		}

		public MainWindow()
        {
            InitializeComponent();
			
			GridView lview = new GridView();
			lview.Columns.Add(new GridViewColumn() { Header = "Id", Width = 30, DisplayMemberBinding = new System.Windows.Data.Binding("Id") });
			lview.Columns.Add(new GridViewColumn() { Header = "Name", Width = 273, DisplayMemberBinding = new System.Windows.Data.Binding("Name") });
			lview.Columns.Add(new GridViewColumn() { Header = "Items", Width = 50, DisplayMemberBinding = new System.Windows.Data.Binding("Items") });
			GroupsListView.View = lview;
			GroupsListView.ItemsSource = groups;

			((INotifyCollectionChanged)listViewTemplate.Items).CollectionChanged += ListView_CollectionChanged;
			((INotifyCollectionChanged)TrackslistView.Items).CollectionChanged += ListView_CollectionChanged;

			//hideButtons(true);

			editButtonTemplate.Click += new RoutedEventHandler(on_editList);
			delButtonTemplate.Click += new RoutedEventHandler(on_delList);
			addButtonTemplate.Click += new RoutedEventHandler(on_addList);
			copyButtonTemplate.Click += new RoutedEventHandler(on_copyList);

			editTrackButton.Click += new RoutedEventHandler(on_editTrack);
			delTrackButton.Click += new RoutedEventHandler(on_delTrack);
			addTrackButton.Click += new RoutedEventHandler(on_addTrack);
			copyTrackButton.Click += new RoutedEventHandler(on_copyTrack);
		}

		private void hideButtons(bool hide)
		{
			Visibility vis = (hide) ? Visibility.Hidden : Visibility.Visible;
			radioButton.Visibility = vis;
			radioButton_Copy.Visibility = vis;
			radioButton_Copy1.Visibility = vis;
			radioButton_Copy2.Visibility = vis;
			radioButton_Copy3.Visibility = vis;
			radioButton_Copy4.Visibility = vis;
			radioButton_Copy5.Visibility = vis;
			radioButton_Copy6.Visibility = vis;
			radioButton_Copy7.Visibility = vis;
			radioButton_Copy8.Visibility = vis;
			radioButton_Copy9.Visibility = vis;
			radioButton_Copy10.Visibility = vis;
		}
		
		private void triggerLDButtons(bool onoff)
		{
			editButtonTemplate.IsEnabled = onoff;
			delButtonTemplate.IsEnabled = onoff;
			addButtonTemplate.IsEnabled = onoff;
			copyButtonTemplate.IsEnabled = onoff;
		}
		private void triggerTButtons(bool onoff)
		{
			editTrackButton.IsEnabled = onoff;
			delTrackButton.IsEnabled = onoff;
			addTrackButton.IsEnabled = onoff;
			copyTrackButton.IsEnabled = onoff;
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (opendir.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string dir_path = opendir.SelectedPath;
				initServer(dir_path);
			}
        }

		private void initServer(string path)
		{
			mserver.MainDir = path;
			discs.Clear();
			lists.Clear();
			groups.Clear();

			fill_discs_array();
			fill_discs_tracks();
			fill_lists_array();
			fill_groups_table();
		}

		private void clearLDTTables()
		{
			clearLDTTables("all");
		}
		private void clearLDTTables(string table)
		{
			if (table != "tracks")
			{
				listViewTemplate.View = null;
				listViewTemplate.ItemsSource = null;
			}

			tracksLabelTemplate.Content = "";
			TrackslistView.View = null;
			TrackslistView.ItemsSource = null;
		}

		private void fill_groups_table()
        {
			clearLDTTables();

			string info_path = mserver.get_INDEXpath();
			if (!File.Exists(info_path))
			{
				System.Windows.MessageBox.Show(info_path + " not found!");
				return;
			}

			using (FileStream fs = new FileStream(info_path, FileMode.Open, FileAccess.Read))
			{
				fs.Position = mserver.cnt_disks_offset;
				int lists_count = fs.ReadByte();

				fs.Position = mserver.groups_offset;
				byte[] group_desc = new byte[mserver.group_length];
				for (int i = 1; i <= mserver.max_groups; i++)
				{
					fs.Read(group_desc, 0, group_desc.Length);
					hf.spliceByteArray(group_desc, ref temp, 0, 4);
					if (BitConverter.ToInt32(temp, 0) == 0x010000ff) break;

					int group_id = temp[0];
					
					byte[] group_name_bytes = new byte[mserver.groupName_length];
					hf.spliceByteArray(group_desc, ref temp, mserver.groupName_offset, mserver.groupName_length);
					temp.CopyTo(group_name_bytes, 0);

					groups.Add(new MSGroup(group_id, group_name_bytes));
				}

				fs.Position = mserver.lists_offset;
				byte[] list_data = new byte[mserver.list_length];
				for (int i = 0; i < lists_count; i++)
				{
					fs.Read(list_data, 0, list_data.Length);
					int gid = list_data[7];
					List<MSGroup> fgroups = groups.Where(g => g.Id == gid).ToList();
					if (fgroups.Count == 0)
					{
						//error
						Console.Write("Group id " + gid + " not found!\n");
						continue;
					}
					else if (fgroups.Count > 1)
					{
						//error
						Console.Write("Group id " + gid + " not uniq!\n");
						continue;
					}

					if (list_data[4] == 0)
					{
						ElenmentId disc_id = new ElenmentId(list_data[3], list_data[0]);
						List<MSDisc> fdiscs = discs.Where(d => d.Id.FullId == disc_id.FullId).ToList();
						if (fdiscs.Count == 0)
						{
							//error
							Console.Write("Disc id " + disc_id.FullId + " not found!\n");
							continue;
						}
						else if (fdiscs.Count > 1)
						{
							//error
							Console.Write("Disc id " + disc_id.FullId + " not uniq!\n");
							continue;
						}
						fgroups[0].Discs.Add(fdiscs[0]);
					}
					else
					{
						int list_id = list_data[0];
						List<MSList> flists = lists.Where(l => l.Id == list_id).ToList();
						if (flists.Count == 0)
						{
							//error
							Console.Write("List id " + list_id + " not found!\n");
							continue;
						}
						else if (flists.Count > 1)
						{
							//error
							Console.Write("List id " + list_id + " not uniq!\n");
							continue;
						}
						fgroups[0].Lists.Add(flists[0]);
					}
				}
			}
			//fs.Close();
		}

		private void fill_lists_array()
		{
			string album_path = mserver.get_ALBUMpath();
			if (!File.Exists(album_path))
			{
				System.Windows.MessageBox.Show(album_path + " not found!");
				return;
			}

			using (FileStream fs = new FileStream(album_path, FileMode.Open, FileAccess.Read))
			{
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

				fs.Position = mserver.alists_offset;
				foreach (int size in lists_sizes)
				{
					byte[] list_data = new byte[size];
					fs.Read(list_data, 0, list_data.Length);

					hf.spliceByteArray(list_data, ref temp, mserver.a_unknown_length, 1);
					int lid = temp[0];
					hf.spliceByteArray(list_data, ref temp, mserver.a_unknown_length + mserver.listId_length + 1, 1);
					int songs_cnt = temp[0];
					
					byte[] list_name_bytes = new byte[mserver.listName_length];					
					hf.spliceByteArray(list_data, ref temp, mserver.listName_offset, mserver.listName_length);
					temp.CopyTo(list_name_bytes, 0);

					MSList list = new MSList(lid, list_name_bytes);
					
					string errors = "";
					for (int i = 0; i < songs_cnt; i++)
					{
						hf.spliceByteArray(list_data, ref temp, mserver.list_desc_length + i * mserver.asong_data_length, mserver.asong_data_length);
						ElenmentId disc_id = new ElenmentId(temp[3], temp[0]);
						List<MSDisc> ldiscs = discs.Where(d => d.Id.FullId == disc_id.FullId).ToList();
						if (ldiscs.Count == 0)
						{
							errors += "Disc " + disc_id.FullId + " not found!\n";
							continue;
						}
						else if (ldiscs.Count > 1)
						{
							errors += "Disc " + disc_id.FullId + " not uniq!\n";
							continue;
						}
						List<MSTrack> dtracks = ldiscs[0].Tracks.Where(tr => tr.Id == temp[4]).ToList();
						if (dtracks.Count == 0)
						{
							errors += "Track " + String.Format("{0,3:000}", temp[4]) + ".sc for disc " + disc_id.FullId + " not found!\n";
							continue;
						}
						else if (dtracks.Count > 1)
						{
							errors += "Track " + String.Format("{0,3:000}", temp[4]) + ".sc for disc " + disc_id.FullId + " not uniq!\n";
							continue;
						}

						list.Songs.Add(dtracks[0]);
					}
					list.Errors = errors;
					lists.Add(list);
				}
			}
		}

		private void fill_discs_array()
		{
			string org_path = mserver.get_ORGpath();
			if (!File.Exists(org_path))
			{
				System.Windows.MessageBox.Show(org_path + " not found!");
				return;
			}

			using (FileStream fs = new FileStream(org_path, FileMode.Open, FileAccess.Read))
			{
				byte[] discs_cnt = new byte[mserver.discs_cnt_length];
				fs.Position = mserver.discs_cnt_offset;
				fs.Read(discs_cnt, 0, discs_cnt.Length);
				int discs_count = BitConverter.ToInt32(discs_cnt, 0);

				byte[] disc_desc = new byte[mserver.disc_desc_length];
				for (int i = 0; i < discs_count; i++)
				{
					fs.Read(disc_desc, 0, disc_desc.Length);
					hf.spliceByteArray(disc_desc, ref temp, mserver.discId_offset, mserver.discId_length);
					ElenmentId disc_id = new ElenmentId(temp[3], temp[0]);

					hf.spliceByteArray(disc_desc, ref temp, mserver.discName_offset, mserver.discName_length);
					byte[] disc_name = new byte[temp.Length];
					temp.CopyTo(disc_name, 0);					

					discs.Add(new MSDisc(disc_id, disc_name));
				}
			}
		}

		private void fill_discs_tracks()
		{
			foreach (MSDisc disc in discs)
			{
				string title_path = mserver.get_TITLEpath(disc.Id);
				if (!File.Exists(title_path))
				{
					disc.Errors = title_path + " not found!";
					continue;
				}

				using (FileStream fs = new FileStream(title_path, FileMode.Open, FileAccess.Read))
				{
					//1 TITLE - n discs
					int[] dtracks_sizes = new int[disc.Id.Prefix];
					
					fs.Position = mserver.dtrack_size_offset;
					for (int i = 0; i < dtracks_sizes.Length; i++)
					{
						byte[] dtrack_size = new byte[mserver.dtrack_size_length];
						fs.Read(dtrack_size, 0, dtrack_size.Length);
						dtracks_sizes[i] = BitConverter.ToInt32(dtrack_size, 0);
					}

					int discPrefixTracks_offset = mserver.dtracks_offset;
					for (int i = 0; i < disc.Id.Prefix - 1; i++)
					{
						discPrefixTracks_offset += dtracks_sizes[i];
					}
					fs.Position = discPrefixTracks_offset;

					byte[] dtracks_data = new byte[dtracks_sizes[disc.Id.Prefix - 1]];
					fs.Read(dtracks_data, 0, dtracks_data.Length);

					int songs_count = dtracks_data[mserver.songs_cnt_offset];

					hf.spliceByteArray(dtracks_data, ref temp, mserver.dtName_offset + mserver.dtName_length + mserver.dtNameLoc_length, mserver.dtArtist_length);
					disc.Artist = hf.ByteArrayToString(temp, codePage);

					int tracks_desc_offset = mserver.dtName_length + mserver.dtNameLoc_length + mserver.dtArtist_length + mserver.dtArtistLoc_length;
					for (int i = 0; i < songs_count; i++)
					{
						int cur_offset = i * (mserver.dtName_length + mserver.dtNameLoc_length + mserver.dtArtist_length + mserver.dtArtistLoc_length);

						hf.spliceByteArray(dtracks_data, ref temp, mserver.dtName_offset + tracks_desc_offset + cur_offset, mserver.dtName_length);
						byte[] tname = new byte[temp.Length];
						temp.CopyTo(tname, 0);

						hf.spliceByteArray(dtracks_data, ref temp, mserver.dtName_offset + tracks_desc_offset + cur_offset + mserver.dtName_length + mserver.dtNameLoc_length, mserver.dtArtist_length);
						byte[] tart = new byte[temp.Length];
						temp.CopyTo(tart, 0);

						MSTrack tr = new MSTrack(i + 1, tname, tart);
						if (!File.Exists(mserver.get_SCpath(disc.Id, i + 1))) tr.Exists = false;
						disc.Tracks.Add(tr);
					}
				}
			}
		}

		private void fill_lists_table()
		{
			clearLDTTables();
			tableLableTemplate.Content = "Lists (max 100)";

			listViewTemplate.ItemContainerStyleSelector = new DiscListStyleSelector();

			GridView lview = new GridView();
			listViewTemplate.View = lview;
			lview.Columns.Add(new GridViewColumn() { Header = "Id", Width = 64, DisplayMemberBinding = new System.Windows.Data.Binding("Id") });
			lview.Columns.Add(new GridViewColumn() { Header = "Name", Width = 280, DisplayMemberBinding = new System.Windows.Data.Binding("Name") });
			lview.Columns.Add(new GridViewColumn() { Header = "Tracks", Width = 45, DisplayMemberBinding = new System.Windows.Data.Binding("Songs.Count") });
			copyButtonTemplate.ToolTip = "Copy Name to clipboard";
			
			MSGroup group = (GroupsListView.SelectedItem as MSGroup);
			listViewTemplate.ItemsSource = group.Lists;
		}

		private void fill_disks_table()
		{
			clearLDTTables();
			tableLableTemplate.Content = "Discs";
			
			listViewTemplate.ItemContainerStyleSelector = new DiscListStyleSelector();

			GridView lview = new GridView();
			listViewTemplate.View = lview;
			lview.Columns.Add(new GridViewColumn() { Header = "Id", Width = 64, DisplayMemberBinding = new System.Windows.Data.Binding("Id.FullId") });
			lview.Columns.Add(new GridViewColumn() { Header = "Name", Width = 140, DisplayMemberBinding = new System.Windows.Data.Binding("Name") });
			lview.Columns.Add(new GridViewColumn() { Header = "Artist", Width = 140, DisplayMemberBinding = new System.Windows.Data.Binding("Artist") });
			lview.Columns.Add(new GridViewColumn() { Header = "Tracks", Width = 45, DisplayMemberBinding = new System.Windows.Data.Binding("Tracks.Count") });
			MSGroup group = (GroupsListView.SelectedItem as MSGroup);
			listViewTemplate.ItemsSource = group.Discs;
			copyButtonTemplate.ToolTip = "Copy Name-Artist to clipboard";
		}

		private void fill_tracks_table()
		{
			Type itemType = listViewTemplate.SelectedItem.GetType();

			TrackslistView.ItemContainerStyleSelector = new DiscListStyleSelector();
			if (itemType.Name == "MSDisc")
			{
				tracksLabelTemplate.Content = "Tracks (max 99)";
				GridView lview = new GridView();
				TrackslistView.View = lview;
				lview.Columns.Add(new GridViewColumn() { Header = "File", Width = 45, DisplayMemberBinding = new System.Windows.Data.Binding("File") });
				lview.Columns.Add(new GridViewColumn() { Header = "Name", Width = 140, DisplayMemberBinding = new System.Windows.Data.Binding("Name") });
				lview.Columns.Add(new GridViewColumn() { Header = "Artist", Width = 140, DisplayMemberBinding = new System.Windows.Data.Binding("Artist") });
				lview.Columns.Add(new GridViewColumn() { Header = "Favorite", Width = 64, CellTemplateSelector = new FavorCellTemplateSelector() });
				MSDisc disc = (listViewTemplate.SelectedItem as MSDisc);
				TrackslistView.ItemsSource = disc.Tracks;
				copyTrackButton.ToolTip = "Copy Name-Artist to clipboard";
			}
			else
			{
				tracksLabelTemplate.Content = "Tracks";
				GridView lview = new GridView();
				TrackslistView.View = lview;
				lview.Columns.Add(new GridViewColumn() { Header = "File", Width = 45, DisplayMemberBinding = new System.Windows.Data.Binding("Key.File") });
				lview.Columns.Add(new GridViewColumn() { Header = "Name", Width = 140, DisplayMemberBinding = new System.Windows.Data.Binding("Key.Name") });
				lview.Columns.Add(new GridViewColumn() { Header = "Artist", Width = 140, DisplayMemberBinding = new System.Windows.Data.Binding("Key.Artist") });
				lview.Columns.Add(new GridViewColumn() { Header = "Disc", Width = 64, DisplayMemberBinding = new System.Windows.Data.Binding("Value.Id.FullId") });
				MSList list = (listViewTemplate.SelectedItem as MSList);
				Dictionary<MSTrack, MSDisc> ls = new Dictionary<MSTrack, MSDisc>();
				foreach (MSTrack lt in list.Songs)
				{
					MSDisc td = discs.Where(d => d.Tracks.Contains(lt)).First();
					ls.Add(lt, td);
				}
				TrackslistView.ItemsSource = ls;
				copyTrackButton.ToolTip = "Copy DiscId: Name-Artist to clipboard";
			}
		}
		
		private void on_editList(object sender, RoutedEventArgs args)
		{
			if (GroupsListView.SelectedItem == null) return;

			listViewTemplate.SelectedItem = null;
			clearLDTTables("tracks");

			MSGroup group = (GroupsListView.SelectedItem as MSGroup);
			GridView gv = (listViewTemplate.View as GridView);

			if (group.Id > 0)
			{
				gv.Columns[1].DisplayMemberBinding = null;
				gv.Columns[1].CellTemplateSelector = new EditCellTemplateSelector();
			}
			else
			{
				gv.Columns[1].DisplayMemberBinding = null;
				gv.Columns[1].CellTemplateSelector = new EditCellTemplateSelector();
				gv.Columns[2].DisplayMemberBinding = null;
				gv.Columns[2].CellTemplateSelector = new EditCellTemplateSelector("Artist");
			}

			listViewTemplate.Items.Refresh();
			saveLDButton.IsEnabled = true;
		}

		private void on_delList(object sender, RoutedEventArgs args)
		{
			if (listViewTemplate.SelectedItem == null || GroupsListView.SelectedItem == null) return;
			Type itemType = listViewTemplate.SelectedItem.GetType();
			MSGroup group = (GroupsListView.SelectedItem as MSGroup);

			if (itemType.Name == "MSList")
			{
				MSList list = (listViewTemplate.SelectedItem as MSList);
				if (list.Id < 3) return;
				lists.Remove(list);
				group.Lists.Remove(list);
			}
			else
			{
				MSDisc disc = (listViewTemplate.SelectedItem as MSDisc);
				foreach (MSTrack track in disc.Tracks)
				{
					foreach (MSGroup gr in groups)
					{
						if (gr.Id == group.Id) continue;
						gr.Lists.ForEach(l => l.Songs.Remove(track));
					}
					foreach (MSList ls in lists)
					{
						ls.Songs.Remove(track);
					}
				}
				group.Discs.Remove(disc);
			}
			listViewTemplate.Items.Refresh();
			GroupsListView.Items.Refresh();
			TrackslistView.ItemsSource = null;
		}

		private void delListTracks(int listId, MSTrack[] songs)
		{
			MSList[] clists;
			if (listId > 0)
			{
				clists = new MSList[1] { lists.Where(l => l.Id == listId).First() };
			}
			else
			{
				clists = lists.ToArray();
			}

			//foreach (MSList list in clists)
			//{
			//	MSTrack[] ntracks = new MSTrack[0];
			//	foreach (MSTrack track in list.Songs)
			//	{
			//		MSTrack[] ltrack = songs.Where(ls => ls.DiskId.Id == track.DiskId.Id && ls.DiskId.Prefix == track.DiskId.Prefix && ls.Id == track.Id).ToArray();
			//		if (ltrack.Length > 0) continue;
			//		Array.Resize(ref ntracks, ntracks.Length + 1);
			//		ntracks[ntracks.Length - 1] = ltrack[0];
			//	}
			//	list.Songs = ntracks;
			//}

			//if (listId > 0)
			//{
			//	foreach (MSList li in lists)
			//	{
			//		if (li.Id == clists[0].Id)
			//		{
			//			li.Songs = clists[0].Songs;
			//			break;
			//		}
			//	}
			//}
			//else
			//{
			//	lists.Clear();
			//	lists = new ObservableCollection<MSList>(clists);
			//}

		}

		private void on_addList(object sender, RoutedEventArgs args)
		{
			if (GroupsListView.SelectedItem == null) return;
			MSGroup group = (GroupsListView.SelectedItem as MSGroup);

			//Bad condition!!!
			if (group.Id > 0)
			{
				int newListId = 3;
				foreach (MSList list in lists)
				{
					if (list.Id == newListId) newListId++;
				}
				byte[] newName = new byte[mserver.listName_length];
				Array.ForEach(newName, b => b = 0x00);
				byte[] new_name = Encoding.GetEncoding(codePage).GetBytes("New list");
				Array.Copy(new_name, 0, newName, 0, new_name.Length);
				MSList nlist = new MSList(newListId, new_name);
				lists.Add(nlist);
				group.Lists.Add(nlist);
			}
			else
			{
				//неизвестно, как сервер выделяет новые индексы
				//бывает просто: XX000001, где XX - новый индекс
				//а бывает: YY000002, где YY - индекс существующего диска YY000001
				int newDiscId = 0;
				foreach (MSDisc disc in discs)
				{
					if (disc.Id.Id >= newDiscId) newDiscId = disc.Id.Id;
				}
				newDiscId++;

				byte[] newName = new byte[mserver.discName_length];
				byte[] newArtist = new byte[mserver.discArtist_length];
				Array.ForEach(newName, b => b = 0x00);
				Array.ForEach(newArtist, b => b = 0x00);
				byte[] new_name = Encoding.GetEncoding(codePage).GetBytes("New disc");
				byte[] new_artist = Encoding.GetEncoding(codePage).GetBytes("New artist");
				Array.Copy(new_name, 0, newName, 0, new_name.Length);
				Array.Copy(new_artist, 0, newArtist, 0, new_artist.Length);
				MSDisc ndisc = new MSDisc(new ElenmentId(newDiscId, 1), newName, newArtist);
				discs.Add(ndisc);
				group.Discs.Add(ndisc);

				//TODO: Add disc from directory
			}
			listViewTemplate.Items.Refresh();
			GroupsListView.Items.Refresh();
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

		private void on_copyTrack(object sender, RoutedEventArgs args)
		{
			if (TrackslistView.SelectedItem == null) return;
			Type itemType = TrackslistView.SelectedItem.GetType();
			if (itemType.Name == "MSTrack")
			{
				MSTrack track = (TrackslistView.SelectedItem as MSTrack);
				string na = (track.Artist == "") ? track.Name : track.Name + " - " + track.Artist;
				System.Windows.Clipboard.SetText(na);
			}
			else
			{
				KeyValuePair<MSTrack, MSDisc> ls =  (KeyValuePair<MSTrack, MSDisc>) TrackslistView.SelectedItem;
				string na = (ls.Key.Artist == "") ? ls.Value.Id.FullId + ": " + ls.Key.Name : ls.Value.Id.FullId + ": " + ls.Key.Name + " - " + ls.Key.Artist;
				System.Windows.Clipboard.SetText(na);
			}
		}

		private void on_addTrack(object sender, RoutedEventArgs args)
		{
			if (listViewTemplate.SelectedItem == null) return;

			Type itemType = listViewTemplate.SelectedItem.GetType();
			//TODO: Add to list!!!
			if (itemType.Name == "MSList") return;
			
			MSDisc disc = (listViewTemplate.SelectedItem as MSDisc);
			int newId = disc.Tracks.Max(t => t.Id) + 1;
			//TODO: Add track from file

			//byte[] newName = new byte[mserver.dtName_length];
			//byte[] newArtist = new byte[mserver.dtArtist_length];
			//Array.ForEach(newName, b => b = 0x00);
			//Array.ForEach(newArtist, b => b = 0x00);
			//byte[] new_name = Encoding.GetEncoding(codePage).GetBytes("New track");
			//byte[] new_artist = Encoding.GetEncoding(codePage).GetBytes("New artist");
			//Array.Copy(new_name, 0, newName, 0, new_name.Length);
			//Array.Copy(new_artist, 0, newArtist, 0, new_artist.Length);
			//disc.Tracks.Add(new MSTrack(newId, newName, newArtist));

			//listViewTemplate.Items.Refresh();
			//TrackslistView.Items.Refresh();
		}

		private void on_delTrack(object sender, RoutedEventArgs args)
		{
			if (TrackslistView.SelectedItem == null) return;

			Type itemType = TrackslistView.SelectedItem.GetType();
			if (itemType.Name == "MSTrack")
			{
				MSTrack track = (TrackslistView.SelectedItem as MSTrack);
				track.Exists = false;
				TrackslistView.Items.Refresh();
				//discs.Where(d => d.Tracks.Contains(track)).ToList().ForEach(d => d.Tracks.Remove(track));
				//lists.Where(l => l.Songs.Contains(track)).ToList().ForEach(l => l.Songs.Remove(track));

				//TODO: del track file
			}
			else
			{
				if (listViewTemplate.SelectedItem == null) return;
				KeyValuePair<MSTrack, MSDisc> ls = (KeyValuePair<MSTrack, MSDisc>)TrackslistView.SelectedItem;
				MSList list = (listViewTemplate.SelectedItem as MSList);
				list.Songs.Remove(ls.Key as MSTrack);
				fill_tracks_table();
			}

			listViewTemplate.Items.Refresh();
		}

		private void on_editTrack(object sender, RoutedEventArgs args)
		{
			if (listViewTemplate.SelectedItem == null) return;
			TrackslistView.SelectedItem = null;
			
			GridView gv = (TrackslistView.View as GridView);
			
			gv.Columns[1].DisplayMemberBinding = null;
			gv.Columns[1].CellTemplateSelector = new EditCellTemplateSelector();
			gv.Columns[2].DisplayMemberBinding = null;
			gv.Columns[2].CellTemplateSelector = new EditCellTemplateSelector("Artist");

			//TODO: edit tracks from source

			TrackslistView.Items.Refresh();
			saveTButton.IsEnabled = true;
		}

		private void GroupsListView_onclick(object sender, MouseButtonEventArgs e)
        {
			if (GroupsListView.SelectedItem == null) return;
			MSGroup group = (GroupsListView.SelectedItem as MSGroup);
			//Bad condition!!!
			if (group.Id == 0)
			{
				fill_disks_table();
			}
			else
			{
				fill_lists_table();
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
				case "donnnn1":
					dir = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\descr\\donnn1\\full\\AVUNIT";
					break;
				default:
					break;
			}

			initServer(dir);
		}
		
		private void copyGroupButton_Click(object sender, RoutedEventArgs e)
		{
			if (GroupsListView.SelectedItem == null) return;
			MSGroup group = (GroupsListView.SelectedItem as MSGroup);
			System.Windows.Clipboard.SetText(group.Name);
		}

		private void editGroupButton_Click(object sender, RoutedEventArgs e)
		{
			GroupsListView.SelectedItem = null;
			clearLDTTables();

			GridView gv = (GroupsListView.View as GridView);
			gv.Columns[1].DisplayMemberBinding = null;
			gv.Columns[1].CellTemplateSelector = new EditCellTemplateSelector();

			GroupsListView.Items.Refresh();
			saveGroupsButton.IsEnabled = true;
		}

		private void addGroupButton_Click(object sender, RoutedEventArgs e)
		{
			if (groups.Count == 0) return;
			int newId = 2;
			foreach (MSGroup group in groups)
			{
				if (group.Id == newId) newId++;
			}
			byte[] newName = new byte[mserver.groupName_length];
			Array.ForEach(newName, b => b = 0x00);
			byte[] new_name = Encoding.GetEncoding(codePage).GetBytes("New group");
			Array.Copy(new_name, 0, newName, 0, new_name.Length);
			groups.Add(new MSGroup(newId, newName));
		}

		private void delGroupButton_Click(object sender, RoutedEventArgs e)
		{
			if (GroupsListView.SelectedItem == null) return;
			MSGroup group = (GroupsListView.SelectedItem as MSGroup);
			if (group.Id < 2) return;
			groups.Remove(group);
		}

		private void listViewTemplate_onclick(object sender, MouseButtonEventArgs e)
		{
			if (listViewTemplate.SelectedItem == null) return;
			
			fill_tracks_table();
		}

		private void codepage_Click(object sender, RoutedEventArgs e)
		{
			System.Windows.Controls.MenuItem mi = (sender as System.Windows.Controls.MenuItem);

			if (!mi.IsChecked)
			{
				mi.IsChecked = true;
			}
			else
			{
				if (mi.Header.ToString() == "Cyrillic (ISO 8859-5)")
				{
					codePage = "iso-8859-5";
					jis_codepage.IsChecked = false;
				}
				else
				{
					codePage = "shift_jis";
					iso_codepage.IsChecked = false;
				}

				GroupsListView.Items.Refresh();
				listViewTemplate.Items.Refresh();
				TrackslistView.Items.Refresh();
			}
		}

		private void saveGroupsButton_Click(object sender, RoutedEventArgs e)
		{
			GridView gv = (GroupsListView.View as GridView);
			gv.Columns[1].DisplayMemberBinding = new System.Windows.Data.Binding("Name");
			gv.Columns[1].CellTemplateSelector = null;

			GroupsListView.Items.Refresh();
			saveGroupsButton.IsEnabled = false;
		}

		private void saveLDButton_Click(object sender, RoutedEventArgs e)
		{
			GridView gv = (listViewTemplate.View as GridView);
			if (gv == null || gv.Columns.Count == 0) return;

			gv.Columns[1].DisplayMemberBinding = new System.Windows.Data.Binding("Name");
			gv.Columns[1].CellTemplateSelector = null;
			if (gv.Columns.Count == 4)
			{
				gv.Columns[2].DisplayMemberBinding = new System.Windows.Data.Binding("Artist");
				gv.Columns[2].CellTemplateSelector = null;
			}

			listViewTemplate.Items.Refresh();
			saveLDButton.IsEnabled = false;
		}
		
		private void ListView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			bool tButtons = (listViewTemplate.SelectedItem == null) ? false : true;
			bool ldButtons = (GroupsListView.SelectedItem == null) ? false : true;
			triggerLDButtons(ldButtons);
			triggerTButtons(tButtons);
		}

		private void saveTButton_Click(object sender, RoutedEventArgs e)
		{
			GridView gv = (TrackslistView.View as GridView);
			if (gv == null || gv.Columns.Count == 0) return;

			gv.Columns[1].DisplayMemberBinding = new System.Windows.Data.Binding("Name");
			gv.Columns[1].CellTemplateSelector = null;
			gv.Columns[2].DisplayMemberBinding = new System.Windows.Data.Binding("Artist");
			gv.Columns[2].CellTemplateSelector = null;

			TrackslistView.Items.Refresh();
			saveTButton.IsEnabled = false;
		}

		private void TrackslistView_MouseUp(object sender, MouseButtonEventArgs e)
		{
			DependencyObject dep = (DependencyObject)e.OriginalSource;
			if (!(dep is System.Windows.Controls.Image)) return;

			System.Windows.Controls.Image img = (dep as System.Windows.Controls.Image);
			MSTrack track = (img.DataContext as MSTrack);
			if (is_favTrack(track))
			{
				lists.Where(l => l.Id == 1).First().Songs.Remove(track);
			}
			else
			{
				lists.Where(l => l.Id == 1).First().Songs.Add(track);
			}

			TrackslistView.SelectedItem = null;
			TrackslistView.Items.Refresh();
		}
	}

	public class DiscListStyleSelector : StyleSelector
	{
		public override Style SelectStyle(object item, DependencyObject container)
		{
			System.Windows.Controls.ListViewItem LVitem = (container as System.Windows.Controls.ListViewItem);

			Type itemType = item.GetType();
			if (itemType.Name == "MSDisc")
			{
				MSDisc el = (item as MSDisc);
				if (el.Errors == "") return LVitem.Style;
				LVitem.ToolTip = el.Errors;
			}
			else if (itemType.Name == "MSList")
			{
				MSList el = (item as MSList);
				if (el.Errors == "") return LVitem.Style;
				LVitem.ToolTip = el.Errors;
			}
			else if (itemType.Name == "MSTrack")
			{
				MSTrack el = (item as MSTrack);
				if (el.Exists) return LVitem.Style;
				LVitem.ToolTip = "Track file deleted!\n";
			}
			else
			{
				KeyValuePair<MSTrack, MSDisc> el = (KeyValuePair<MSTrack, MSDisc>)item;
				if (el.Key.Exists) return LVitem.Style;
				LVitem.ToolTip = "Track file deleted!\n";
			}

			Style st = new Style(LVitem.Style.TargetType, LVitem.Style);
			
			foreach (Setter stb in LVitem.Style.Setters)
			{
				Setter rrr = (stb.Property == System.Windows.Controls.ListViewItem.ForegroundProperty) 
					? new Setter(stb.Property, System.Windows.Media.Brushes.Red, stb.TargetName)
					: new Setter(stb.Property, stb.Value, stb.TargetName);
				st.Setters.Add(rrr);
			}
			return st;			
		}
	}

	public class EditCellTemplateSelector : DataTemplateSelector
	{
		private string bindString;

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			FrameworkElementFactory tb = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBlock));
			DependencyProperty tp = System.Windows.Controls.TextBlock.TextProperty;

			Type itemType = item.GetType();
			if (itemType.Name == "MSGroup")
			{
				MSGroup group = (item as MSGroup);
				if (group.Id > 1)
				{
					tb = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBox));
					tp = System.Windows.Controls.TextBox.TextProperty;
				}
			}
			else if (itemType.Name == "MSList")
			{
				MSList list = (item as MSList);
				if (list.Id > 2)
				{
					tb = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBox));
					tp = System.Windows.Controls.TextBox.TextProperty;
				}
			}
			else
			{
				tb = new FrameworkElementFactory(typeof(System.Windows.Controls.TextBox));
				tp = System.Windows.Controls.TextBox.TextProperty;
			}

			tb.SetBinding(tp, new System.Windows.Data.Binding(this.bindString));
			return new DataTemplate { VisualTree = tb };
		}

		public EditCellTemplateSelector(string bstring)
		{
			this.bindString = bstring;
		}
		public EditCellTemplateSelector()
		{
			this.bindString = "Name";
		}
	}

	public class FavorCellTemplateSelector : DataTemplateSelector
	{
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{			
			FrameworkElementFactory img = new FrameworkElementFactory(typeof(System.Windows.Controls.Image));

			MSTrack track = (item as MSTrack);
			string fav_img = (((MainWindow)System.Windows.Application.Current.MainWindow).is_favTrack(track)) ? "fav.png" : "nfav.png";
			BitmapImage bitmapImage = new BitmapImage(new Uri(@"pack://application:,,,/"
				+ Assembly.GetExecutingAssembly().GetName().Name
				+ ";component/"
				+ "Images/" + fav_img, UriKind.Absolute));
			img.SetValue(System.Windows.Controls.Image.SourceProperty, bitmapImage);
			img.SetValue(System.Windows.Controls.Image.WidthProperty, 20d);
			return new DataTemplate { VisualTree = img };
		}
	}
}
