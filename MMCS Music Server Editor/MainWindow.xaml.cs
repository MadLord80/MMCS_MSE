using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
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

		private Dictionary<ElenmentId, List<int>> factTracks = new Dictionary<ElenmentId, List<int>>();

		private Dictionary<string, bool> fileBackuped = new Dictionary<string, bool>();

		internal string CodePage
		{
			get { return this.codePage; }
		}

		internal bool is_favTrack(MSTrack track)
		{
			//bool is_fav = this.lists.Where(l => l.Id == mserver.fav_listId).First().Tracks.Contains(track);
			//return is_fav;
			return false;
		}

		public MainWindow()
		{
			InitializeComponent();

			GridView lview = new GridView();
			lview.Columns.Add(new GridViewColumn() { Header = "Id", Width = 30, DisplayMemberBinding = new System.Windows.Data.Binding("Id") });
			lview.Columns.Add(new GridViewColumn() { Header = "Name", Width = 273, DisplayMemberBinding = new System.Windows.Data.Binding("Name") });
			lview.Columns.Add(new GridViewColumn() { Header = "Items", Width = 50, DisplayMemberBinding = new System.Windows.Data.Binding("Items") });
			GroupsListView.View = lview;
			GroupsListView.ItemContainerStyleSelector = new LVStyleSelector();
			GroupsListView.ItemsSource = groups;
			System.Windows.Data.CollectionView gview = (System.Windows.Data.CollectionView)System.Windows.Data.CollectionViewSource.GetDefaultView(GroupsListView.ItemsSource);
			gview.Filter = hiddenItems;

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

		private void OpenAVDirButton_Click(object sender, RoutedEventArgs e)
		{
			if (opendir.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				string dir_path = opendir.SelectedPath;
				initServer(dir_path);
			}
		}

		private void initServer(string path)
		{
			clearLDTTables();

			mserver.MainDir = path;
			factTracks.Clear();
			discs.Clear();
			lists.Clear();
			groups.Clear();

			fill_fact_tracks();
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

		private void fill_fact_tracks()
		{
			string data_path = mserver.get_DATApath();
			if (!Directory.Exists(data_path))
			{
				return;
			}
			DirectoryInfo[] dirsID = new DirectoryInfo(data_path).GetDirectories();
			foreach (DirectoryInfo dirID in dirsID)
			{
				DirectoryInfo[] trackdirs = dirID.GetDirectories();
				foreach (DirectoryInfo trackdir in trackdirs)
				{
					FileInfo[] tracks = trackdir.GetFiles("*.sc");
					List<int> tids = new List<int> { };
					foreach (FileInfo track in tracks)
					{
						tids.Add(Convert.ToInt32(track.Name.Substring(0, 3)));
					}
					factTracks.Add(new ElenmentId(trackdir.Name), tids);
				}
			}
		}

		private void fill_groups_table()
		{
			//clearLDTTables();

			string info_path = mserver.get_INDEXpath();
			if (!File.Exists(info_path))
			{
				System.Windows.MessageBox.Show(info_path + " not found!");
				return;
			}

			using (FileStream fs = new FileStream(info_path, FileMode.Open, FileAccess.Read))
			{
				byte[] index_header = new byte[mserver.index_header_size];
				fs.Read(index_header, 0, index_header.Length);

				int list_count = BitConverter.ToInt32(new ArraySegment<byte>(index_header, 36, 4).ToArray(), 0);

				int file_size = mserver.index_header_size + mserver.index_max_groups * (4 + mserver.NameDesc_length) 
					+ list_count * mserver.index_list_data_size;
				// check file size
				if (fs.Length != file_size)
				{
					System.Windows.MessageBox.Show(fs.Name + ": incorrect data!");
					return;
				}

				for (int i = 0; i < mserver.index_max_groups; i++)
				{
					byte[] group_data = new byte[4 + mserver.NameDesc_length];
					fs.Read(group_data, 0, group_data.Length);
					int groupId = BitConverter.ToInt32(new ArraySegment<byte>(group_data, 0, 4).ToArray(), 0);
					if (groupId != i && groupId != 0x010000ff)
					{
						string groupName = new TrackDiscDesc(new ArraySegment<byte>(group_data, 4, mserver.NameDesc_length).ToArray()).Name;
						System.Windows.MessageBox.Show(groupId + ": fail group id for group " + groupName + "!");
						return;
					}
					if (groupId == 0x010000ff)
					{
						fs.Position += (mserver.index_max_groups - i - 1) * group_data.Length;
						break;
					}

					groups.Add(new MSGroup(
						new ArraySegment<byte>(group_data, 0, 4).ToArray(),
						new ArraySegment<byte>(group_data, 4, mserver.NameDesc_length).ToArray()
					));
				}

				for (int i = 0; i < list_count; i++)
				{
					byte[] list_data = new byte[mserver.index_list_data_size];
					fs.Read(list_data, 0, list_data.Length);

					byte[] gid = new ArraySegment<byte>(list_data, 4, 4).ToArray();
					// group id is last byte of data
					gid[0] = 0;
					Array.Reverse(gid);
					int groupId = BitConverter.ToInt32(gid, 0);
					if (groupId > 0)
					{
						int listId = BitConverter.ToInt32(new ArraySegment<byte>(list_data, 0, 4).ToArray(), 0);
						groups.Where((gr) => gr.Id == groupId).First().Lists.Add(
							lists.Where((lst) => lst.Id == listId).First()
						);
					}
					else
					{
						ElenmentId discid = new ElenmentId(new ArraySegment<byte>(list_data, 0, 4).ToArray());
						// all disc is in group with id 0!
						groups.Where((gr) => gr.Id == 0).First().Discs.Add(
							discs.Where((dsc) => dsc.Id.FullId == discid.FullId).First()
						);
					}
				}
				//	fs.Position = mserver.cnt_disks_offset;
				//	int lists_count = fs.ReadByte();

				//	fs.Position = mserver.groups_offset;
				//	byte[] group_desc = new byte[mserver.group_length];
				//	for (int i = 1; i <= mserver.index_max_groups; i++)
				//	{
				//		fs.Read(group_desc, 0, group_desc.Length);
				//		hf.spliceByteArray(group_desc, ref temp, 0, 4);
				//		if (BitConverter.ToInt32(temp, 0) == 0x010000ff) break;

				//		int group_id = temp[0];

				//		//byte[] group_name_bytes = new byte[mserver.groupName_length];
				//		//hf.spliceByteArray(group_desc, ref temp, mserver.groupName_offset, mserver.groupName_length);
				//		//temp.CopyTo(group_name_bytes, 0);

				//		//groups.Add(new MSGroup(group_id, group_name_bytes));
				//	}

				//	fs.Position = mserver.lists_offset;
				//	byte[] list_data = new byte[mserver.list_length];
				//	for (int i = 0; i < lists_count; i++)
				//	{
				//		fs.Read(list_data, 0, list_data.Length);
				//		int gid = list_data[7];
				//		List<MSGroup> fgroups = groups.Where(g => g.Id == gid).ToList();
				//		if (fgroups.Count == 0)
				//		{
				//			//error
				//			Console.Write("Group id " + gid + " not found!\n");
				//			continue;
				//		}
				//		else if (fgroups.Count > 1)
				//		{
				//			//error
				//			Console.Write("Group id " + gid + " not uniq!\n");
				//			continue;
				//		}

				//		if (list_data[4] == 0)
				//		{
				//			//ElenmentId disc_id = new ElenmentId(list_data[3], list_data[0]);
				//			//List<MSDisc> fdiscs = discs.Where(d => d.Id.FullId == disc_id.FullId).ToList();
				//			//if (fdiscs.Count == 0)
				//			//{
				//			//	//error
				//			//	Console.Write("Disc id " + disc_id.FullId + " not found!\n");
				//			//	continue;
				//			//}
				//			//else if (fdiscs.Count > 1)
				//			//{
				//			//	//error
				//			//	Console.Write("Disc id " + disc_id.FullId + " not uniq!\n");
				//			//	continue;
				//			//}
				//			//fgroups[0].Discs.Add(fdiscs[0]);
				//		}
				//		else
				//		{
				//			int list_id = list_data[0];
				//			List<MSList> flists = lists.Where(l => l.Id == list_id).ToList();
				//			if (flists.Count == 0)
				//			{
				//				//error
				//				Console.Write("List id " + list_id + " not found!\n");
				//				continue;
				//			}
				//			else if (flists.Count > 1)
				//			{
				//				//error
				//				Console.Write("List id " + list_id + " not uniq!\n");
				//				continue;
				//			}
				//			fgroups[0].Lists.Add(flists[0]);
				//		}
				//	}
				//}
				//fs.Close();
			}
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
				List<int> trackListSizes = new List<int>();

				fs.Position = mserver.title_header_size;
				//tracks size maybe 0!
				for (int i = 0; i < mserver.album_max_lists; i++)
				{
					byte[] trackListSize = new byte[mserver.album_length_size];
					fs.Read(trackListSize, 0, trackListSize.Length);
					int tls = BitConverter.ToInt32(trackListSize, 0);
					if (tls == 0) { continue; }
					trackListSizes.Add(tls);
					//dtracks_sizes[i] = BitConverter.ToInt32(dtrack_size, 0);
					//st -= dtracks_sizes[i];
				}
				//disc.StartTitle = BitConverter.GetBytes(st);

				int header_and_listsizes_offset = mserver.album_header_size + mserver.album_length_size * mserver.album_max_lists;
				// check file size
				if (fs.Length != header_and_listsizes_offset + trackListSizes.Sum())
				{
					System.Windows.MessageBox.Show(fs.Name + ": incorrect data!");
					return;
				}

				//int discPrefixTracks_offset = mserver.dtracks_offset;
				int track_desc_size = 2 * (mserver.NameDesc_length + mserver.NameLocDesc_length);
				fs.Position = header_and_listsizes_offset;
				for (int i = 0; i < trackListSizes.Count; i++)
				{
					byte[] list_header = new byte[mserver.album_list_header_size];
					fs.Read(list_header, 0, list_header.Length);
					MSList list = new MSList(
						BitConverter.ToInt32(new ArraySegment<byte>(list_header, 4, 4).ToArray(), 0),
						new ArraySegment<byte>(list_header, 20, mserver.NameDesc_length).ToArray()
					);

					byte[] tc = new ArraySegment<byte>(list_header, 9, 4).ToArray();
					// BitConverter.ToInt32 need 4 bytes
					tc[3] = 0;
					int tracks_count = BitConverter.ToInt32(tc, 0);
					for (int k = 0; k < tracks_count; k++)
					{
						byte[] track_data = new byte[mserver.album_track_data_size];
						fs.Read(track_data, 0, track_data.Length);

						ElenmentId discid = new ElenmentId(
							new ArraySegment<byte>(track_data, 0, 4).ToArray()
						);
						int tracknum = BitConverter.ToInt32(new ArraySegment<byte>(track_data, 4, 4).ToArray(), 0);

						list.AddTrack(
							discs.Where((dsc) => dsc.Id.FullId == discid.FullId).First()
							.Tracks.Where((trk) => trk.Id == tracknum).First()
						);

						//list.AddTrackRef(new MSTrackRef(
						//	new ArraySegment<byte>(track_data, 0, 4).ToArray(),
						//	new ArraySegment<byte>(track_data, 4, 4).ToArray()
						//));
					}

					lists.Add(list);
				}
				//	fs.Read(mserver.ALBUMstart, 0, mserver.ALBUMstart.Length);

				//	int[] lists_sizes = new int[mserver.album_max_lists];

				//	fs.Position = mserver.album_header_size;
				//	for (int i = 0; i < lists_sizes.Length; i++)
				//	{
				//		byte[] list_size = new byte[mserver.list_size_length];
				//		fs.Read(list_size, 0, list_size.Length);
				//		int size = BitConverter.ToInt32(list_size, 0);
				//		if (size == 0)
				//		{
				//			Array.Resize(ref lists_sizes, i);
				//			break;
				//		}
				//		lists_sizes[i] = size;
				//	}

				//	fs.Position = mserver.alists_offset;
				//	foreach (int size in lists_sizes)
				//	{
				//		byte[] list_data = new byte[size];
				//		fs.Read(list_data, 0, list_data.Length);

				//		hf.spliceByteArray(list_data, ref temp, 0, mserver.a_unknown_length);
				//		byte[] lst = new byte[mserver.a_unknown_length];
				//		temp.CopyTo(lst, 0);

				//		hf.spliceByteArray(list_data, ref temp, mserver.a_unknown_length, 1);
				//		int lid = temp[0];
				//		hf.spliceByteArray(list_data, ref temp, mserver.a_unknown_length + mserver.listId_length, 1);
				//		byte lid_cnt = temp[0];
				//		hf.spliceByteArray(list_data, ref temp, mserver.a_unknown_length + mserver.listId_length + 1, 1);
				//		int songs_cnt = temp[0];

				//		hf.spliceByteArray(list_data, ref temp, mserver.a_unknown_length + mserver.listId_length + 4, 4);
				//		byte[] ldelim = new byte[4];
				//		temp.CopyTo(ldelim, 0);
				//		hf.spliceByteArray(list_data, ref temp, mserver.a_unknown_length + mserver.listId_length + 8, 4);
				//		byte[] lcode = new byte[4];
				//		temp.CopyTo(lcode, 0);

				//		byte[] list_name_bytes = new byte[mserver.listName_length];
				//		hf.spliceByteArray(list_data, ref temp, mserver.listName_offset, mserver.listName_length);
				//		temp.CopyTo(list_name_bytes, 0);

				//		MSList list = new MSList(lid, list_name_bytes);
				//		list.LStart = lst;
				//		list.LId_cnt = lid_cnt;
				//		list.LDelim = ldelim;
				//		list.LCode = lcode;

				//		string errors = "";
				//		for (int i = 0; i < songs_cnt; i++)
				//		{
				//			hf.spliceByteArray(list_data, ref temp, mserver.list_desc_length + i * mserver.asong_data_length, mserver.asong_data_length);
				//			//ElenmentId disc_id = new ElenmentId(temp[3], temp[0]);
				//			//List<MSDisc> ldiscs = discs.Where(d => d.Id.FullId == disc_id.FullId).ToList();
				//			//if (ldiscs.Count == 0)
				//			//{
				//			//	errors += "Disc " + disc_id.FullId + " not found!\n";
				//			//	continue;
				//			//}
				//			//else if (ldiscs.Count > 1)
				//			//{
				//			//	errors += "Disc " + disc_id.FullId + " not uniq!\n";
				//			//	continue;
				//			//}
				//			//List<MSTrack> dtracks = ldiscs[0].Tracks.Where(tr => tr.Id == temp[4]).ToList();
				//			//if (dtracks.Count == 0)
				//			//{
				//			//	errors += "Track " + String.Format("{0,3:000}", temp[4]) + ".sc for disc " + disc_id.FullId + " not found!\n";
				//			//	continue;
				//			//}
				//			//else if (dtracks.Count > 1)
				//			//{
				//			//	errors += "Track " + String.Format("{0,3:000}", temp[4]) + ".sc for disc " + disc_id.FullId + " not uniq!\n";
				//			//	continue;
				//			//}

				//			//dtracks[0].ListDelim = new byte[8] {
				//			//	temp[8], temp[9], temp[10], temp[11],
				//			//	temp[12], temp[13], temp[14], temp[15],
				//			//};

				//			//list.Songs.Add(dtracks[0]);
				//		}
				//		list.Errors = errors;
				//		lists.Add(list);
				//	}
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
				byte[] discs_cnt = new byte[mserver.org_discs_cnt_length];
				fs.Position = mserver.org_discs_cnt_offset;
				fs.Read(discs_cnt, 0, discs_cnt.Length);
				int discs_count = BitConverter.ToInt32(discs_cnt, 0);
				// check file size
				if (fs.Length != mserver.org_header_size + discs_count * mserver.org_discdata_size)
				{
					System.Windows.MessageBox.Show("ORG_ARRAY: incorrect data!");
					return;
				}

				byte[] disc_desc = new byte[mserver.org_disc_desc_length];
				for (int i = 0; i < discs_count; i++)
				{
					fs.Read(disc_desc, 0, disc_desc.Length);
					MSDisc disc = new MSDisc(
						new ArraySegment<byte>(disc_desc, 1, 4).ToArray(),
						new ArraySegment<byte>(disc_desc, 12, 128).ToArray()
					);
					if (factTracks.Where((kvp) => kvp.Key.FullId == disc.Id.FullId).ToArray().Length > 0)
					{
						disc.Exists = true;
					}
					discs.Add(disc);
				}
			}
		}

		private void fill_discs_tracks()
		{
			//foreach (MSDisc disc in discs)
			//{
			string title_path = mserver.get_TITLEpath();
			if (!Directory.Exists(title_path))
			{
				return;
			}
			DirectoryInfo[] title_dirs = new DirectoryInfo(mserver.get_TITLEpath()).GetDirectories();
			foreach (DirectoryInfo title_dir in title_dirs)
			{
				FileInfo[] title_files = title_dir.GetFiles("*.lst");
				foreach (FileInfo title_file in title_files)
				{
					using (FileStream fs = new FileStream(title_file.FullName, FileMode.Open, FileAccess.Read))
					{
						//fs.Read(disc.StartTitle, 0, disc.StartTitle.Length);
						//int st = BitConverter.ToInt32(disc.StartTitle, 0);
						//fs.Position = mserver.title_discid_offset;
						//byte[] discid_bytes = new byte[2];
						//fs.Read(discid_bytes, 0, discid_bytes.Length);
						//int discid = Convert.ToInt32(new String(Encoding.UTF8.GetChars(discid_bytes).ToArray()), 16);

						//1 TITLE - n discs
						//int[] dtracks_sizes = new int[disc.Id.Prefix];
						List<int> trackListSizes = new List<int>();

						fs.Position = mserver.title_header_size;
						//tracks size maybe 0!
						for (int i = 0; i < mserver.title_max_lengths; i++)
						{
							byte[] trackListSize = new byte[mserver.title_length_size];
							fs.Read(trackListSize, 0, trackListSize.Length);
							int tls = BitConverter.ToInt32(trackListSize, 0);
							if (tls == 0) { continue; }
							trackListSizes.Add(tls);
							//dtracks_sizes[i] = BitConverter.ToInt32(dtrack_size, 0);
							//st -= dtracks_sizes[i];
						}
						//disc.StartTitle = BitConverter.GetBytes(st);

						int header_and_listsizes = mserver.title_header_size + mserver.title_length_size * mserver.title_max_lengths;
						// check file size
						if (fs.Length != header_and_listsizes + trackListSizes.Sum())
						{
							System.Windows.MessageBox.Show(fs.Name + ": incorrect data!");
							return;
						}

						//int discPrefixTracks_offset = mserver.dtracks_offset;
						int track_desc_size = 2 * (mserver.NameDesc_length + mserver.NameLocDesc_length);
						fs.Position = header_and_listsizes;
						for (int i = 0; i < trackListSizes.Count; i++)
						{
							byte[] list_header = new byte[mserver.title_list_header_size];
							fs.Read(list_header, 0, list_header.Length);

							ElenmentId discid = new ElenmentId(new ArraySegment<byte>(list_header, 4, 4).ToArray());
							MSDisc curDisc = discs.Where((dsc) => dsc.Id.FullId == discid.FullId).FirstOrDefault();
							int tracks_count = new ArraySegment<byte>(list_header, 8, 1).ToArray()[0];
							if (curDisc == null)
							{
								fs.Position += tracks_count * track_desc_size + track_desc_size;
								continue;
							}

							// disc desc
							byte[] disc_artist = new byte[track_desc_size];
							fs.Read(disc_artist, 0, disc_artist.Length);
							curDisc.SetArtist(new ArraySegment<byte>(disc_artist, mserver.NameDesc_length + mserver.NameLocDesc_length, mserver.NameDesc_length).ToArray());
							//fs.Position += track_desc_size;

							for (int tid = 0; tid < tracks_count; tid++)
							{
								byte[] track_data = new byte[track_desc_size];
								fs.Read(track_data, 0, track_data.Length);
								MSTrack track = new MSTrack(
									curDisc.Id,
									tid + 1,
									new ArraySegment<byte>(track_data, 0, mserver.NameDesc_length).ToArray(),
									new ArraySegment<byte>(track_data, mserver.NameDesc_length + mserver.NameLocDesc_length, mserver.NameDesc_length).ToArray()
								);
								if (factTracks.Where((kvp) => kvp.Key.FullId == discid.FullId && kvp.Value.Contains(tid + 1)).ToArray().Length > 0)
								{
									track.Exists = true;
								}
								curDisc.AddTrack(track);
							}
						}
						//fs.Position = discPrefixTracks_offset;

						//byte[] dtracks_data = new byte[dtracks_sizes[disc.Id.Prefix - 1]];
						//fs.Read(dtracks_data, 0, dtracks_data.Length);

						//int songs_count = dtracks_data[mserver.songs_cnt_offset];

						//hf.spliceByteArray(dtracks_data, ref temp, mserver.dtName_offset - 16, 16);
						//temp.CopyTo(disc.Title, 0);

						//hf.spliceByteArray(dtracks_data, ref temp, mserver.dtName_offset + mserver.dtName_length + mserver.dtNameLoc_length, mserver.dtArtist_length);
						//disc.Artist = hf.ByteArrayToString(temp, codePage);

						//int tracks_desc_offset = mserver.dtName_length + mserver.dtNameLoc_length + mserver.dtArtist_length + mserver.dtArtistLoc_length;
						//for (int i = 0; i < songs_count; i++)
						//{
						//	int cur_offset = i * (mserver.dtName_length + mserver.dtNameLoc_length + mserver.dtArtist_length + mserver.dtArtistLoc_length);

						//	hf.spliceByteArray(dtracks_data, ref temp, mserver.dtName_offset + tracks_desc_offset + cur_offset, mserver.dtName_length);
						//	byte[] tname = new byte[temp.Length];
						//	temp.CopyTo(tname, 0);

						//	hf.spliceByteArray(dtracks_data, ref temp, mserver.dtName_offset + tracks_desc_offset + cur_offset + mserver.dtName_length + mserver.dtNameLoc_length, mserver.dtArtist_length);
						//	byte[] tart = new byte[temp.Length];
						//	temp.CopyTo(tart, 0);

						//	MSTrack tr = new MSTrack(i + 1, tname, tart);
						//	if (!File.Exists(mserver.get_SCpath(disc.Id, i + 1))) tr.Exists = false;
						//	disc.Tracks.Add(tr);
						//}
					}

				}
			}
			//}
		}

		private void fill_lists_table()
		{
			clearLDTTables();
			tableLableTemplate.Content = "Lists (max 100)";

			listViewTemplate.ItemContainerStyleSelector = new LVStyleSelector();

			GridView lview = new GridView();
			listViewTemplate.View = lview;
			lview.Columns.Add(new GridViewColumn() { Header = "Id", Width = 64, DisplayMemberBinding = new System.Windows.Data.Binding("Id") });
			lview.Columns.Add(new GridViewColumn() { Header = "Name", Width = 280, DisplayMemberBinding = new System.Windows.Data.Binding("Name") });
			lview.Columns.Add(new GridViewColumn() { Header = "Tracks", Width = 45, DisplayMemberBinding = new System.Windows.Data.Binding("Tracks.Count") });
			copyButtonTemplate.ToolTip = "Copy Name to clipboard";

			MSGroup group = (GroupsListView.SelectedItem as MSGroup);
			listViewTemplate.ItemsSource = group.Lists;
		}

		private void fill_disks_table()
		{
			clearLDTTables();
			tableLableTemplate.Content = "Discs";

			listViewTemplate.ItemContainerStyleSelector = new LVStyleSelector();

			GridView lview = new GridView();
			listViewTemplate.View = lview;
			lview.Columns.Add(new GridViewColumn() { Header = "Id", Width = 64, DisplayMemberBinding = new System.Windows.Data.Binding("Id.FullId") });
			lview.Columns.Add(new GridViewColumn() { Header = "Name", Width = 140, DisplayMemberBinding = new System.Windows.Data.Binding("Name") });
			lview.Columns.Add(new GridViewColumn() { Header = "Artist", Width = 140, DisplayMemberBinding = new System.Windows.Data.Binding("Artist") });
			lview.Columns.Add(new GridViewColumn() { Header = "Tracks", Width = 45, DisplayMemberBinding = new System.Windows.Data.Binding("Tracks.Count") });
			MSGroup group = (GroupsListView.SelectedItem as MSGroup);
			// absent discs folders in ORG_ARRAY
			//List<MSDisc> discs = group.Discs;
			//if (group.Id == 0 && factTracks.Count > group.Discs.Count)
			//{

			//}
			listViewTemplate.ItemsSource = group.Discs;
			copyButtonTemplate.ToolTip = "Copy Name-Artist to clipboard";
		}

		private void fill_tracks_table()
		{
			Type itemType = listViewTemplate.SelectedItem.GetType();

			TrackslistView.ItemContainerStyleSelector = new LVStyleSelector();
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
				System.Windows.Data.CollectionView tview = (System.Windows.Data.CollectionView)System.Windows.Data.CollectionViewSource.GetDefaultView(TrackslistView.ItemsSource);
				tview.Filter = hiddenItems;
			}
			else
			{
				tracksLabelTemplate.Content = "Tracks";
				GridView lview = new GridView();
				TrackslistView.View = lview;
				//lview.Columns.Add(new GridViewColumn() { Header = "File", Width = 45, DisplayMemberBinding = new System.Windows.Data.Binding("Key.File") });
				//lview.Columns.Add(new GridViewColumn() { Header = "Name", Width = 140, DisplayMemberBinding = new System.Windows.Data.Binding("Key.Name") });
				//lview.Columns.Add(new GridViewColumn() { Header = "Artist", Width = 140, DisplayMemberBinding = new System.Windows.Data.Binding("Key.Artist") });
				//lview.Columns.Add(new GridViewColumn() { Header = "Disc", Width = 64, DisplayMemberBinding = new System.Windows.Data.Binding("Value.Id.FullId") });
				lview.Columns.Add(new GridViewColumn() { Header = "File", Width = 45, DisplayMemberBinding = new System.Windows.Data.Binding("File") });
				lview.Columns.Add(new GridViewColumn() { Header = "Name", Width = 140, DisplayMemberBinding = new System.Windows.Data.Binding("Name") });
				lview.Columns.Add(new GridViewColumn() { Header = "Artist", Width = 140, DisplayMemberBinding = new System.Windows.Data.Binding("Artist") });
				lview.Columns.Add(new GridViewColumn() { Header = "Disc", Width = 64, DisplayMemberBinding = new System.Windows.Data.Binding("DiscID.FullId") });
				MSList list = (listViewTemplate.SelectedItem as MSList);
				//Dictionary<MSTrack, MSDisc> ls = new Dictionary<MSTrack, MSDisc>();
				//foreach (MSTrack lt in list.Tracks)
				//{
				//	MSDisc td = discs.Where(d => d.Tracks.Contains(lt)).First();
				//	ls.Add(lt, td);
				//}
				//TrackslistView.ItemsSource = ls;
				TrackslistView.ItemsSource = list.Tracks;
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
						//gr.Lists.ForEach(l => l.Tracks.Remove(track));
					}
					foreach (MSList ls in lists)
					{
						//ls.Tracks.Remove(track);
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
				//byte[] newName = new byte[mserver.listName_length];
				//Array.ForEach(newName, b => b = 0x00);
				//byte[] new_name = Encoding.GetEncoding(codePage).GetBytes("New list");
				//Array.Copy(new_name, 0, newName, 0, new_name.Length);
				//MSList nlist = new MSList(newListId, new_name);
				//lists.Add(nlist);
				//group.Lists.Add(nlist);
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

				//byte[] newName = new byte[mserver.discName_length];
				//byte[] newArtist = new byte[mserver.dtArtist_length];
				//Array.ForEach(newName, b => b = 0x00);
				//Array.ForEach(newArtist, b => b = 0x00);
				byte[] new_name = Encoding.GetEncoding(codePage).GetBytes("New disc");
				byte[] new_artist = Encoding.GetEncoding(codePage).GetBytes("New artist");
				//Array.Copy(new_name, 0, newName, 0, new_name.Length);
				//Array.Copy(new_artist, 0, newArtist, 0, new_artist.Length);
				//MSDisc ndisc = new MSDisc(new ElenmentId(newDiscId, 1), newName, newArtist);
				//discs.Add(ndisc);
				//group.Discs.Add(ndisc);

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
				//string na = (disc.Artist == "") ? disc.Name : disc.Name + " - " + disc.Artist;
				//System.Windows.Clipboard.SetText(na);
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
				KeyValuePair<MSTrack, MSDisc> ls = (KeyValuePair<MSTrack, MSDisc>)TrackslistView.SelectedItem;
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
			//int newId = disc.Tracks.Max(t => t.Id) + 1;
			//if (newId > mserver.max_dtracks - 1)
			//{
			//	System.Windows.MessageBox.Show("Max tracks per disc: " + (mserver.max_dtracks - 1));
			//	return;
			//}
			//TODO: Add track from file

			//byte[] newName = new byte[mserver.dtName_length];
			//byte[] newArtist = new byte[mserver.dtArtist_length];
			//Array.ForEach(newName, b => b = 0x00);
			//Array.ForEach(newArtist, b => b = 0x00);
			byte[] new_name = Encoding.GetEncoding(codePage).GetBytes("New track");
			byte[] new_artist = Encoding.GetEncoding(codePage).GetBytes("New artist");
			//Array.Copy(new_name, 0, newName, 0, new_name.Length);
			//Array.Copy(new_artist, 0, newArtist, 0, new_artist.Length);
			//MSTrack track = new MSTrack(newId, newName, newArtist);
			//track.Added = true;
			//disc.Tracks.Add(track);

			listViewTemplate.Items.Refresh();
			//TrackslistView.Items.Refresh();
			System.Windows.Data.CollectionViewSource.GetDefaultView(TrackslistView.ItemsSource).Refresh();

			saveFButton.IsEnabled = true;
		}

		private void on_delTrack(object sender, RoutedEventArgs args)
		{
			if (TrackslistView.SelectedItem == null) return;

			Type itemType = TrackslistView.SelectedItem.GetType();
			if (itemType.Name == "MSTrack")
			{
				MSTrack track = (TrackslistView.SelectedItem as MSTrack);
				track.Exists = false;
				//track.Deleted = true;
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
				//list.Tracks.Remove(ls.Key as MSTrack);
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
				case "r-01":
					dir = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_from_r01\\AVUNIT";
					break;
				case "babatu":
					dir = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_from_r03_babatu\\AVUNIT";
					break;
				case "igorkaana":
					dir = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_from_r03_igorkaanna\\AVUNIT";
					break;
				case "my current":
					dir = "D:\\Cloud\\домашнее\\outlander\\files\\sound\\music_server_mycurrent\\AVUNIT";
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
			//int newId = 2;
			int newId = groups.Max((grp) => grp.Id) + 1;
			//foreach (MSGroup group in groups)
			//{
			//	if (group.Id == newId) newId++;
			//}
			//byte[] newName = new byte[mserver.groupName_length];
			//Array.ForEach(newName, b => b = 0x00);
			//byte[] new_name = Encoding.GetEncoding(codePage).GetBytes("New group");
			//Array.Copy(new_name, 0, newName, 0, new_name.Length);
			MSGroup ngroup = new MSGroup(newId, "New group");
			//ngroup.Added = true;
			groups.Add(ngroup);

			saveFButton.IsEnabled = true;
		}

		private void delGroupButton_Click(object sender, RoutedEventArgs e)
		{
			if (GroupsListView.SelectedItem == null) return;
			MSGroup group = (GroupsListView.SelectedItem as MSGroup);
			if (group.Id < 2) return;
			groups.Remove(group);
			//group.Deleted = true;
			//foreach (MSGroup cg in groups)
			//{
			//if (cg.Id > group.Id) cg.Id--;
			//}
			System.Windows.Data.CollectionViewSource.GetDefaultView(GroupsListView.ItemsSource).Refresh();

			saveFButton.IsEnabled = true;
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

		private void ListView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			bool tButtons = (listViewTemplate.SelectedItem == null) ? false : true;
			bool ldButtons = (GroupsListView.SelectedItem == null) ? false : true;
			triggerLDButtons(ldButtons);
			triggerTButtons(tButtons);
		}

		private void saveGroupsButton_Click(object sender, RoutedEventArgs e)
		{
			GridView gv = (GroupsListView.View as GridView);
			gv.Columns[1].DisplayMemberBinding = new System.Windows.Data.Binding("Name");
			gv.Columns[1].CellTemplateSelector = null;

			GroupsListView.Items.Refresh();
			saveGroupsButton.IsEnabled = false;
			//if (groups.Where(g => g.NameChanged == true).ToList().Count > 0) saveFButton.IsEnabled = true;
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
			//if (discs.Where(d => d.Tracks.Where(t => t.NameChanged).ToList().Count > 0).ToList().Count > 0) saveFButton.IsEnabled = true;
		}

		private void TrackslistView_MouseUp(object sender, MouseButtonEventArgs e)
		{
			DependencyObject dep = (DependencyObject)e.OriginalSource;
			if (!(dep is System.Windows.Controls.Image)) return;

			System.Windows.Controls.Image img = (dep as System.Windows.Controls.Image);
			MSTrack track = (img.DataContext as MSTrack);
			if (is_favTrack(track))
			{
				//lists.Where(l => l.Id == mserver.fav_listId).First().Tracks.Remove(track);
			}
			else
			{
				//lists.Where(l => l.Id == mserver.fav_listId).First().Tracks.Add(track);
			}

			TrackslistView.SelectedItem = null;
			TrackslistView.Items.Refresh();
		}

		private void reportButton_Click(object sender, RoutedEventArgs e)
		{
			if (groups.Count == 0) return;

			string file = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\" + "Report.txt";
			using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write))
			{
				string SCPTD = "AVSCPlayTrackData.dat: ";
				SCPTD += (File.Exists(mserver.MainDir + "\\AVSCPlayTrackData.dat")) ? "exist\n" : "not exist\n";
				byte[] str = hf.StringToByteArray(SCPTD, codePage);
				fs.Write(str, 0, str.Length);

				string INDEXst = "INDEX start (w\\o discs id): ";
				uint ist = BitConverter.ToUInt32(mserver.INDEXstart, 0);
				string discsEnd = ""; string titleStart = "";
				string discstitle = "";
				List<int> discIds = new List<int>();
				foreach (MSDisc disc in discs.OrderBy(d => d.Tracks.Count))
				{
					byte[] did = new byte[4] { 0x03, 0x00, 0x00, BitConverter.GetBytes(disc.Id.Id)[0] };
					uint idid = BitConverter.ToUInt32(did, 0);
					ist -= idid;

					//discsEnd += disc.Id.FullId + ": " + disc.Tracks.Count + " : " + BitConverter.ToString(disc.EndDesc) + "\n";
					//discstitle += disc.Id.FullId + ": " + disc.Tracks.Count + " : " + BitConverter.ToString(disc.Title) + "\n";

					if (discIds.Contains(disc.Id.Id)) continue;
					//titleStart += disc.Id.Id + ": " + BitConverter.ToString(disc.StartTitle) + "\n";
					discIds.Add(disc.Id.Id);
				}
				byte[] bist = BitConverter.GetBytes(ist);
				INDEXst += BitConverter.ToString(bist) + "\n";
				str = hf.StringToByteArray(INDEXst, codePage);
				fs.Write(str, 0, str.Length);

				str = hf.StringToByteArray("ORG_ARRAY discs ends:\n" + discsEnd, codePage);
				fs.Write(str, 0, str.Length);

				str = hf.StringToByteArray("TITLE disc data:\n" + discstitle, codePage);
				fs.Write(str, 0, str.Length);

				str = hf.StringToByteArray("TITLE starts (w\\o size of track desc):\n" + titleStart, codePage);
				fs.Write(str, 0, str.Length);

				string ALBUMst = "ALBUM start: " + BitConverter.ToString(mserver.ALBUMstart) + "\n";
				str = hf.StringToByteArray(ALBUMst, codePage);
				fs.Write(str, 0, str.Length);

				//string ldata = "";
				//foreach (MSList list in lists)
				//{
				//	ldata += list.Id + " (" + list.Songs.Count + "): "
				//		+ BitConverter.ToString(list.LStart) + " : "
				//		+ BitConverter.ToString(new byte[1] { list.LId_cnt }) + " : "
				//		+ BitConverter.ToString(list.LDelim) + " : "
				//		+ BitConverter.ToString(list.LCode) + "\n";

				//	foreach (MSTrack track in list.Songs)
				//	{
				//		MSDisc disc = discs.Where(d => d.Tracks.Contains(track)).First();
				//		ldata += disc.Id.FullId + "(" + track.Id + "): " + BitConverter.ToString(track.ListDelim) + "\n";
				//	}
				//}
				//string listsData = "ALBUM0000001.lst data:\n" + ldata;
				//str = hf.StringToByteArray(listsData, codePage);
				//fs.Write(str, 0, str.Length);

				System.Windows.MessageBox.Show("File " + fs.Name + " saved!");
			}
		}

		private bool hiddenItems(object item)
		{
			Type itemType = item.GetType();
			bool show_item = true;
			if (itemType.Name == "MSGroup")
			{
				//if ((item as MSGroup).Deleted) show_item = false;
			}
			else if (itemType.Name == "MSTrack")
			{
				//if ((item as MSTrack).Deleted) show_item = false;
			}
			return show_item;
		}

		private void saveFButton_Click(object sender, RoutedEventArgs e)
		{
			//if (discs.Where(d => d.Tracks.Where(t => t.Added).ToList().Count > 0).ToList().Count > 0) add_Tracks();

			//if (groups.Where(g => g.Added).ToList().Count > 0) add_Groups();
			//if (groups.Where(g => g.Deleted).ToList().Count > 0) del_Groups();
			//if (groups.Where(g => g.NameChanged).ToList().Count > 0) change_GroupNames();

			saveFButton.IsEnabled = false;
			System.Windows.MessageBox.Show("Music Server files updated!");
			fileBackuped.Clear();
			initServer(mserver.MainDir);
		}

		private bool makeFilecopy(string file)
		{
			if (fileBackuped.ContainsKey(file) && fileBackuped[file]) return true;
			if (File.Exists(file + ".old"))
			{
				MessageBoxResult res = System.Windows.MessageBox.Show("File " + file + ".old exists! Do you want overwrite it?", "Save backup file", MessageBoxButton.YesNo);
				if (res == MessageBoxResult.No) return false;
			}
			File.Copy(file, file + ".old", true);
			if (!fileBackuped.ContainsKey(file)) fileBackuped.Add(file, true);
			fileBackuped[file] = true;
			return true;
		}

		private void change_GroupNames()
		{
			string info_path = mserver.get_INDEXpath();
			if (!File.Exists(info_path))
			{
				System.Windows.MessageBox.Show(info_path + " not found!");
				return;
			}

			if (!makeFilecopy(info_path))
			{
				System.Windows.MessageBox.Show("File " + info_path + " not backuped! Changes is not done!");
				return;
			}

			using (FileStream fs = new FileStream(info_path, FileMode.Open, FileAccess.ReadWrite))
			{
				foreach (MSGroup group in groups)
				{
					//if (group.Added || group.Deleted || !group.NameChanged) continue;
					//group.NameChanged = false;

					//fs.Position = mserver.groups_offset;
					//byte[] group_desc = new byte[mserver.group_length];
					for (int i = 1; i <= mserver.index_max_groups; i++)
					{
						//fs.Read(group_desc, 0, group_desc.Length);
						//hf.spliceByteArray(group_desc, ref temp, 0, 4);
						//if (BitConverter.ToInt32(temp, 0) == 0x010000ff) break;

						//if (temp[0] == group.Id)
						//{
						//	//fs.Position -= mserver.groupName_length;
						//	fs.Write(group.NameBytes, 0, group.NameBytes.Length);
						//}
					}
				}
			}
		}
		private void add_Groups()
		{
			string info_path = mserver.get_INDEXpath();
			string album_path = mserver.get_ALBUMpath();
			if (!File.Exists(info_path) || !File.Exists(album_path))
			{
				System.Windows.MessageBox.Show(info_path + " or " + album_path + " not found!");
				return;
			}

			if (!makeFilecopy(info_path) || !makeFilecopy(album_path))
			{
				System.Windows.MessageBox.Show("File " + info_path + " or " + album_path + " not backuped! Changes is not done!");
				return;
			}

			using (FileStream fs = new FileStream(info_path, FileMode.Open, FileAccess.ReadWrite))
			{
				foreach (MSGroup group in groups.OrderBy(g => g.Id))
				{
					//if (!group.Added || group.Deleted) continue;
					//group.Added = false;

					//fs.Position = mserver.groups_offset;
					//byte[] group_desc = new byte[mserver.group_length];
					for (int i = 1; i <= mserver.index_max_groups; i++)
					{
						//fs.Read(group_desc, 0, group_desc.Length);
						//hf.spliceByteArray(group_desc, ref temp, 0, 4);
						//if (BitConverter.ToInt32(temp, 0) == 0x010000ff)
						//{
						//	fs.Position -= mserver.group_length;
						//	fs.Write(new byte[4] { (byte)group.Id, 0x00, 0x00, 0x00 }, 0, 4);
						//	fs.Write(group.NameBytes, 0, group.NameBytes.Length);
						//	if (group.Lists.Count > 0)
						//	{
						//		//need increase size of file???
						//		fs.Position = fs.Length;
						//		foreach (MSList list in group.Lists)
						//		{
						//			fs.Write(new byte[8] { (byte)list.Id, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, (byte)group.Id }, 0, 8);
						//			long cur_offset = fs.Position;

						//			fs.Position = mserver.cnt_disks_offset;
						//			int cur_cnt = fs.ReadByte();
						//			fs.Position -= 1;
						//			fs.WriteByte((byte)(cur_cnt + 1));
						//			fs.Position = cur_offset;
						//		}
						//	}
						//	break;
						//}
					}
				}
			}
		}
		private void add_Tracks()
		{
			// just add 00x.sc
			//
			//foreach (MSDisc disc in discs.Where(d => d.Tracks.Where(t => t.Added).ToList().Count > 0).ToList())
			//{
			//	string title_path = mserver.get_TITLEpath(disc.Id);
			//	if (!File.Exists(title_path))
			//	{
			//		System.Windows.MessageBox.Show(title_path + " not found!");
			//		return;
			//	}

			//	if (!makeFilecopy(title_path))
			//	{
			//		System.Windows.MessageBox.Show("File " + title_path + " not backuped! Changes is not done!");
			//		return;
			//	}

			//	using (FileStream fs = new FileStream(title_path, FileMode.Open, FileAccess.ReadWrite))
			//	{
			//		//What if disc in CDDB?
			//		//TITLE:
			//		//- add NNN.sc
			//		//DISCID:
			//		//MM0000TTDISCID.lst - not changed if del. If add?
			//		//DISCIDMM.lst - not changed if del. If add?
			//		//- increase num of tracks
			//		//- increase tracks offsets
			//		//RECORD: need to change?
			//		//ORG_ARRAY - change only end_desc if del. If add?

			//		List<KeyValuePair<int, byte[]>> dtracks = new List<KeyValuePair<int, byte[]>>();
			//		//tracks size maybe 0!
			//		int dtrack_data_offset = mserver.dtracks_offset;
			//		for (int i = 0; i < mserver.max_dtracks; i++)
			//		{
			//			fs.Position = mserver.dtrack_size_offset + i * mserver.dtrack_size_length;
			//			byte[] dtrack_size = new byte[mserver.dtrack_size_length];
			//			fs.Read(dtrack_size, 0, dtrack_size.Length);
			//			int dts = BitConverter.ToInt32(dtrack_size, 0);
			//			byte[] dtrack_data = new byte[dts];
			//			fs.Position = dtrack_data_offset;
			//			dtrack_data_offset += dts;
			//			fs.Read(dtrack_data, 0, dtrack_data.Length);
			//			if (i + 1 == disc.Id.Prefix)
			//			{
			//				foreach (MSTrack track in disc.Tracks.Where(t => t.Added).ToList())
			//				{
			//					if (track.Deleted) continue;
			//					//- increase tracks length
			//					dts += 0x180;
			//					//- increase sum
			//					dtrack_data[0]++;
			//					//- increase num of tracks
			//					dtrack_data[8]++;
			//					//- add track name and artist
			//					Array.Resize(ref dtrack_data, dtrack_data.Length + 0x180);
			//					Array.Copy(Enumerable.Repeat((byte)0x00, 0x180).ToArray(), 0, dtrack_data, dtrack_data.Length - 0x180, 0x180);
			//					Array.Copy(track.NameBytes, 0, dtrack_data, dtrack_data.Length - 0x180, track.NameBytes.Length);
			//					//Array.Copy(track.ArtistBytes, 0, dtrack_data, dtrack_data.Length - 0x180 + mserver.dtName_length + mserver.dtNameLoc_length, track.ArtistBytes.Length);
			//				}
			//			}
			//			dtracks.Add(new KeyValuePair<int, byte[]>(dts, dtrack_data));
			//		}

			//		fs.Position = mserver.dtrack_size_offset;
			//		//int st = BitConverter.ToInt32(disc.StartTitle, 0);
			//		foreach (KeyValuePair<int, byte[]> dt_data in dtracks)
			//		{
			//			//st += dt_data.Key;
			//			fs.Write(BitConverter.GetBytes(dt_data.Key), 0, 4);
			//		}
			//		foreach (KeyValuePair<int, byte[]> dt_data in dtracks)
			//		{
			//			fs.Write(dt_data.Value, 0, dt_data.Key);
			//		}
			//		//- increase start bytes
			//		fs.Position = 0;
			//		//fs.Write(BitConverter.GetBytes(st), 0, 4);
			//	}
			//}
		}

		private void TestButton_Click(object sender, RoutedEventArgs e)
		{
			//string discId = "2d00000a";
			//string trackId = "032";
			//int tid = Convert.ToInt32(trackId);
			//string tid2 = Convert.ToString(tid);
			//int dpf = (int)hf.HexStringToByteArray(discId.Substring(0, 2))[0];
			//int did = (int)hf.HexStringToByteArray(discId.Substring(discId.Length - 2))[0];
			//string discId2 = 
			//	BitConverter.ToString(new byte[1] { (byte)did }) 
			//	+ "0000" 
			//	+ BitConverter.ToString(new byte[1] { (byte)dpf });

			//ObservableCollection<testItem> list1 = new ObservableCollection<testItem> {
			//	new testItem(1), new testItem(2), new testItem(3)
			//};
			//testClass obj = new testClass(list1.Where((l) => l.Id == 1).First());
			//obj.AddString(list1.Where((l) => l.Id == 2).First());
			//obj.AddString(list1.Where((l) => l.Id == 3).First());
			//list1.Remove(list1.Where((l) => l.Id == 2).First());
		}

		//class testItem
		//{
		//	private int id;
		//	public int Id { get { return this.id; } }
		//	public testItem(int id)
		//	{
		//		this.id = id;
		//	}
		//}
		//class testClass
		//{
		//	private List<testItem> strings;
		//	public void AddString (testItem str)
		//	{
		//		this.strings.Add(str);
		//	}
		//	public testClass(testItem str)
		//	{
		//		this.strings = new List<testItem>() { str };
		//	}
		//}
	}

	public class LVStyleSelector : StyleSelector
	{
		public override Style SelectStyle(object item, DependencyObject container)
		{
			System.Windows.Controls.ListViewItem LVitem = (container as System.Windows.Controls.ListViewItem);

			System.Windows.Media.SolidColorBrush brush = System.Windows.Media.Brushes.Red;
			System.Windows.Media.SolidColorBrush green_brush = System.Windows.Media.Brushes.GreenYellow;
			System.Windows.Media.SolidColorBrush yellow_brush = System.Windows.Media.Brushes.Yellow;

			Type itemType = item.GetType();
			if (itemType.Name == "MSDisc")
			{
				MSDisc el = (item as MSDisc);
				if (el.Errors != "")
				{
					LVitem.ToolTip = el.Errors;
				}				
				if (!el.Exists)
				{
					LVitem.ToolTip += "\nDisc folder not exist!\n";
				}
				else
				{
					return LVitem.Style;
				}
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
				if (!el.Exists)
				{
					LVitem.ToolTip = "Track file not exist!\n";
				}
				//else if (el.Added)
				//{
				//	LVitem.ToolTip = "Track added!";
				//	brush = yellow_brush;
				//}
				//else if (el.NameChanged)
				//{
				//	LVitem.ToolTip = "Track name changed!";
				//	brush = green_brush;
				//}
				else
				{
					return LVitem.Style;
				}
			}
			else if (itemType.Name == "MSGroup")
			{
				MSGroup el = (item as MSGroup);
				//if (el.Added)
				//{
				//	LVitem.ToolTip = "Group added!";
				//	brush = yellow_brush;
				//}
				//else if (el.NameChanged)
				//{
				//	LVitem.ToolTip = "Group name changed!";
				//	brush = green_brush;
				//}
				//else
				//{
				return LVitem.Style;
				//}
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
					? new Setter(stb.Property, brush, stb.TargetName)
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
