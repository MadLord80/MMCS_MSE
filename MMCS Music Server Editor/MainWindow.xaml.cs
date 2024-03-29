﻿using System;
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
using System.Text.RegularExpressions;

namespace MMCS_MSE
{
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	delegate void UpdateProgressBarDelegate(DependencyProperty dp, object value);

	public partial class MainWindow : Window
	{
		private FolderBrowserDialog opendir = new FolderBrowserDialog();
		//private byte[] temp = new byte[0];

		private string codePage = "iso-8859-5";

		private help_functions hf = new help_functions();
		private MMCSServer mserver = new MMCSServer();
		private ObservableCollection<MSGroup> groups = new ObservableCollection<MSGroup>();
		private ObservableCollection<MSList> lists = new ObservableCollection<MSList>();
		private ObservableCollection<MSDisc> discs = new ObservableCollection<MSDisc>();

		private Dictionary<ElenmentId, List<int>> factTracks = new Dictionary<ElenmentId, List<int>>();
                
		System.ComponentModel.BackgroundWorker copyMoveworker;
        //System.ComponentModel.BackgroundWorker exportTracks1worker;

        internal string CodePage
		{
			get { return this.codePage; }
		}

		public MainWindow()
		{
			InitializeComponent();

            string version = System.Windows.Forms.Application.ProductVersion;
            this.Title = "MMCS Music Server Editor v." + version.Remove(version.Length - 2);// + " Beta";

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

            saveFButton.Visibility = Visibility.Hidden;
            createServer_Button.Visibility = Visibility.Hidden;
			copyMoveProgress.Visibility = Visibility.Hidden;
            export1Button.IsEnabled = false;

            editButtonTemplate.IsEnabledChanged += new DependencyPropertyChangedEventHandler(checkCodePageForIsEnabled);
            delButtonTemplate.IsEnabledChanged += new DependencyPropertyChangedEventHandler(checkCodePageForIsEnabled);
            addButtonTemplate.IsEnabledChanged += new DependencyPropertyChangedEventHandler(checkCodePageForIsEnabled);
            copyButtonTemplate.IsEnabledChanged += new DependencyPropertyChangedEventHandler(checkCodePageForIsEnabled);

            editTrackButton.IsEnabledChanged += new DependencyPropertyChangedEventHandler(checkCodePageForIsEnabled);
            delTrackButton.IsEnabledChanged += new DependencyPropertyChangedEventHandler(checkCodePageForIsEnabled);
            addTrackButton.IsEnabledChanged += new DependencyPropertyChangedEventHandler(checkCodePageForIsEnabled);
            copyTrackButton.IsEnabledChanged += new DependencyPropertyChangedEventHandler(checkCodePageForIsEnabled);

            editGroupButton.IsEnabledChanged += new DependencyPropertyChangedEventHandler(checkCodePageForIsEnabled);
            delGroupButton.IsEnabledChanged += new DependencyPropertyChangedEventHandler(checkCodePageForIsEnabled);
            addGroupButton.IsEnabledChanged += new DependencyPropertyChangedEventHandler(checkCodePageForIsEnabled);
            copyGroupButton.IsEnabledChanged += new DependencyPropertyChangedEventHandler(checkCodePageForIsEnabled);

#if !DEBUG
            hideButtons(true);
			reportButton.Visibility = Visibility.Hidden;
			testButton.Visibility = Visibility.Hidden;

            triggerGButtons(false);
            triggerLDButtons(false);
            triggerTButtons(false);
            //delGroupButton.IsEnabled = false;
            //addGroupButton.IsEnabled = false;
            //delButtonTemplate.IsEnabled = false;
            //addButtonTemplate.IsEnabled = false;
            //delTrackButton.IsEnabled = false;
            //addTrackButton.IsEnabled = false;
#endif
        }

        private bool checkFileForWrite(string file)
        {
            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
                {
                    byte[] test = new byte[2];
                    fs.Read(test, 0, test.Length);
                    fs.Position = 0;
                    fs.Write(test, 0, test.Length);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                return false;
            }
            return true;
        }

        private void checkCodePageForIsEnabled(object sender, DependencyPropertyChangedEventArgs e)
        {
            System.Windows.Controls.Button button = (sender as System.Windows.Controls.Button);
            if (codePage == "shift_jis") { button.IsEnabled = false; }
            if ((button.Name == "editGroupButton" || button.Name == "delGroupButton" || button.Name == "copyGroupButton") 
                && groups.Count < 3)
            {
                button.IsEnabled = false;
            }
            else if (button.Name == "addGroupButton" && groups.Count > 0)
            {
                button.IsEnabled = true;
            }
        }

        private void triggerLDButtons(bool onoff)
        {
            editButtonTemplate.IsEnabled = onoff;
			delButtonTemplate.IsEnabled = false;
			addButtonTemplate.IsEnabled = false;
			copyButtonTemplate.IsEnabled = onoff;
        }

        private void triggerTButtons(bool onoff)
        {
            editTrackButton.IsEnabled = onoff;
			delTrackButton.IsEnabled = false;
			addTrackButton.IsEnabled = false;
			copyTrackButton.IsEnabled = onoff;
        }
        private void triggerGButtons(bool onoff)
        {
            editGroupButton.IsEnabled = onoff;
            delGroupButton.IsEnabled = false;
            addGroupButton.IsEnabled = false;
            copyGroupButton.IsEnabled = onoff;
        }

		private void OpenAVDirButton_Click(object sender, RoutedEventArgs e)
		{
			if (opendir.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				string dir_path = opendir.SelectedPath;
				initServer(dir_path);
			}
		}

        private void clearCurrentData()
        {
            factTracks.Clear();
            discs.Clear();
            lists.Clear();
            groups.Clear();
            clearLDTTables();
            triggerGButtons(false);
            triggerLDButtons(false);
            triggerTButtons(false);

            createServer_Button.Visibility = Visibility.Hidden;
            export1Button.IsEnabled = false;
        }

        private void initServer(string path)
		{
            clearCurrentData();

            mserver.MainDir = path;

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

            // not export empty lists
            if (factTracks.Count > 0) { export1Button.IsEnabled = true; }
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
                    //int groupId = BitConverter.ToInt32(new ArraySegment<byte>(group_data, 0, 4).ToArray(), 0);
                    int groupId = (group_data[0] == 0xff)
                        ? BitConverter.ToInt32(new ArraySegment<byte>(group_data, 0, 4).ToArray(), 0)
                        : (int)group_data[0];
                    // standard groups has strange indexes
                    // may be: 00000000 (orig discs) and 01000000 (favs)
                    // may be: 01030000 (orig discs) and 00000000 (favs)
                    if (groupId > 1 && groupId != i && groupId != 0x010000ff)
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

                    byte[] gid = (groupId > 1)
                        ? new byte[] { group_data[0], 0, 0, 0 }
                        : new byte[] { (byte)i, 0, 0, 0 };
                    groups.Add(new MSGroup(
						//new ArraySegment<byte>(group_data, 0, 4).ToArray(),
                        //new byte[] {group_data[0], 0, 0, 0}, 
                        gid,
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
			}

            if (groups.Count > 0) { triggerGButtons(true); }
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
				}

				int header_and_listsizes_offset = mserver.album_header_size + mserver.album_length_size * mserver.album_max_lists;
				// check file size
				if (fs.Length != header_and_listsizes_offset + trackListSizes.Sum())
				{
					System.Windows.MessageBox.Show(fs.Name + ": incorrect data!");
					return;
				}

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
					// tracks_count - 1 byte!
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
					}
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

                byte[] disc_desc = new byte[mserver.org_discdata_size];
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
						}

						int header_and_listsizes = mserver.title_header_size + mserver.title_length_size * mserver.title_max_lengths;
						// check file size
						if (fs.Length != header_and_listsizes + trackListSizes.Sum())
						{
							System.Windows.MessageBox.Show(fs.Name + ": incorrect data!");
							return;
						}

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
					}
				}
			}
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
            editButtonTemplate.IsEnabled = true;
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

			listViewTemplate.ItemsSource = group.Discs;
			copyButtonTemplate.ToolTip = "Copy Name-Artist to clipboard";
            editButtonTemplate.IsEnabled = true;
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

                editTrackButton.IsEnabled = true;
			}
			else
			{
				tracksLabelTemplate.Content = "Tracks";
				GridView lview = new GridView();
				TrackslistView.View = lview;
				lview.Columns.Add(new GridViewColumn() { Header = "File", Width = 45, DisplayMemberBinding = new System.Windows.Data.Binding("File") });
				lview.Columns.Add(new GridViewColumn() { Header = "Name", Width = 140, DisplayMemberBinding = new System.Windows.Data.Binding("Name") });
				lview.Columns.Add(new GridViewColumn() { Header = "Artist", Width = 140, DisplayMemberBinding = new System.Windows.Data.Binding("Artist") });
				lview.Columns.Add(new GridViewColumn() { Header = "Disc", Width = 64, DisplayMemberBinding = new System.Windows.Data.Binding("DiscID.FullId") });
				MSList list = (listViewTemplate.SelectedItem as MSList);
				TrackslistView.ItemsSource = list.Tracks;
				copyTrackButton.ToolTip = "Copy DiscId: Name-Artist to clipboard";
			}

			if (TrackslistView.Items.Count > 0 && createServer_Button.Visibility == Visibility.Visible) { changeNATrackButton.IsEnabled = true; }
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
			if (group.Id > 1)
			{
				int newListId = 3;
				foreach (MSGroup grp in groups)
				{
					if (grp.Id < 2 || grp.Lists.Count < 1) { continue; }
					int ListId = grp.Lists.Max((lst) => lst.Id) + 1;
					if (ListId > newListId) { newListId = ListId; }
				}

				group.AddList(new MSList(newListId, "New list"));
				//foreach (MSList list in lists)
				//{
				//	if (list.Id == newListId) newListId++;
				//}
				//byte[] newName = new byte[mserver.listName_length];
				//Array.ForEach(newName, b => b = 0x00);
				//byte[] new_name = Encoding.GetEncoding(codePage).GetBytes("New list");
				//Array.Copy(new_name, 0, newName, 0, new_name.Length);
				//MSList nlist = new MSList(newListId, new_name);
				//lists.Add(nlist);
				//group.Lists.Add(nlist);
			}
			else if (group.Id == 0)
			{
				if (group.Discs.Count >= 0x3b * 100)
				{
					System.Windows.MessageBox.Show("Maximum number of drives reached!");
					return;
				}

				// NN0000MM
				// ищем максимальный NN + 1
				// если максимальный NN + 1 > 0x3B, то начиная с NN = 0
				// ищем MM + 1 < 100
				ElenmentId newDiscId = (group.Discs.Count > 0)
					? new ElenmentId(group.Discs.Max((dsc) => dsc.Id.Id) + 1, 1)
					: new ElenmentId(0, 1);
				bool founded = false;
				if (newDiscId.Id > 0x3b)
				{
					for (int i = 0; i < 0x3c; i++)
					{
						if (founded) { break; }
						for (int k = 1; k < 101; k++)
						{
							newDiscId.Id = i; newDiscId.Prefix = k;
							if (group.Discs.Where((dsc) => dsc.Id.FullId == newDiscId.FullId).ToArray().Length > 0)
							{ continue; }

							founded = true;
							break;
						}
					}
				}
				if (!founded)
				{
					System.Windows.MessageBox.Show("Can`t find new disc id!");
					return;
				}
				group.AddDisc(new MSDisc(newDiscId, "New disc"));

				//foreach (MSDisc disc in discs)
				//{
				//	if (disc.Id.Id >= newDiscId) newDiscId = disc.Id.Id;
				//}
				//newDiscId++;

				//byte[] newName = new byte[mserver.discName_length];
				//byte[] newArtist = new byte[mserver.dtArtist_length];
				//Array.ForEach(newName, b => b = 0x00);
				//Array.ForEach(newArtist, b => b = 0x00);
				//byte[] new_name = Encoding.GetEncoding(codePage).GetBytes("New disc");
				//byte[] new_artist = Encoding.GetEncoding(codePage).GetBytes("New artist");
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

#if DEBUG
			saveFButton.IsEnabled = true;
#endif
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
			changeNATrackButton.IsEnabled = false;
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
			if (groups.Count < 2) return;
			int newId = groups.Max((grp) => grp.Id) + 1;
			MSGroup ngroup = new MSGroup(newId, "New group");
			//ngroup.Added = true;
			groups.Add(ngroup);
            triggerGButtons(true);

#if DEBUG
			saveFButton.IsEnabled = true;
#endif
		}

		private void delGroupButton_Click(object sender, RoutedEventArgs e)
		{
			if (GroupsListView.SelectedItem == null) return;
			MSGroup group = (GroupsListView.SelectedItem as MSGroup);
			if (group.Id < 2) return;
			groups.Remove(group);
			//group.Deleted = true;
			System.Windows.Data.CollectionViewSource.GetDefaultView(GroupsListView.ItemsSource).Refresh();
            if (groups.Count < 3) { triggerGButtons(false); }

#if DEBUG
			saveFButton.IsEnabled = true;
#endif
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
                if (mi.Name == "iso_codepage")
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
			//triggerLDButtons(ldButtons);
			//triggerTButtons(tButtons);
		}

		private void saveGroupsButton_Click(object sender, RoutedEventArgs e)
		{
			GridView gv = (GroupsListView.View as GridView);
			gv.Columns[1].DisplayMemberBinding = new System.Windows.Data.Binding("Name");
			gv.Columns[1].CellTemplateSelector = null;

			GroupsListView.Items.Refresh();
			saveGroupsButton.IsEnabled = false;
            updateINDEXlst();
            //if (groups.Where(g => g.NameChanged == true).ToList().Count > 0) saveFButton.IsEnabled = true;
        }

        // update names of groups
        private void updateINDEXlst()
        {
            if (groups.Count > 2)
            {
                string info_path = mserver.get_INDEXpath();
                if (!File.Exists(info_path))
                {
                    System.Windows.MessageBox.Show(info_path + " not found!");
                    return;
                }

                if (!checkFileForWrite(info_path)) { return; }
                using (FileStream fs = new FileStream(info_path, FileMode.Open, FileAccess.ReadWrite))
                {
                    foreach (MSGroup group in groups)
                    {
                        if (group.Id < 2) { continue; }

                        fs.Position = mserver.index_header_size + group.Id * (4 + mserver.NameDesc_length) + 4;
                        byte[] g_name = new byte[mserver.NameDesc_length];
                        Encoding.GetEncoding(codePage).GetBytes(group.Name).CopyTo(g_name, 0);
                        fs.Write(g_name, 0, g_name.Length);
                    }

                    // update checksum
                    byte[] fileData = new byte[fs.Length - 4];
                    fs.Position = 4;
                    fs.Read(fileData, 0, fileData.Length);
                    UInt32 checksum = BitConverter.ToUInt32(hf.checksum32bit(fileData), 0);
                    checksum += BitConverter.ToUInt32(new ArraySegment<byte>(fileData, 32, 4).ToArray(), 0);
                    fs.Position = 0;
                    fs.Write(BitConverter.GetBytes(checksum), 0, 4);
                }
            }
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
            updateDiscsNames();
            saveLDButton.IsEnabled = false;
		}

        private void updateDiscsNames()
        {
            //update ORG_ARRAY
            string org_path = mserver.get_ORGpath();
            if (!File.Exists(org_path))
            {
                System.Windows.MessageBox.Show(org_path + " not found!");
                return;
            }

            if (!checkFileForWrite(org_path)) { return; }
            using (FileStream fs = new FileStream(org_path, FileMode.Open, FileAccess.ReadWrite))
            {
                byte[] discs_cnt = new byte[mserver.org_discs_cnt_length];
                fs.Position = mserver.org_discs_cnt_offset;
                fs.Read(discs_cnt, 0, discs_cnt.Length);
                int discs_count = BitConverter.ToInt32(discs_cnt, 0);

                byte[] disc_desc = new byte[mserver.org_discdata_size];
                for (int i = 0; i < discs_count; i++)
                {
                    fs.Read(disc_desc, 0, disc_desc.Length);
                    ElenmentId discid = new ElenmentId(new ArraySegment<byte>(disc_desc, 1, 4).ToArray());
                    MSDisc discData = discs.Where(d => d.Id.Id == discid.Id && d.Id.Prefix == discid.Prefix).First();
                    // to start of disc name
                    fs.Position -= 212;
                    byte[] d_name = new byte[mserver.NameDesc_length];
                    Encoding.GetEncoding(codePage).GetBytes(discData.Name).CopyTo(d_name, 0);
                    fs.Write(d_name, 0, d_name.Length);
                    // to end of disc data
                    fs.Position += 84;
                }
            }

            //update TITLEs
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
                    if (!checkFileForWrite(title_file.FullName)) { continue; }
                    using (FileStream fs = new FileStream(title_file.FullName, FileMode.Open, FileAccess.ReadWrite))
                    {
                        //1 TITLE - n discs
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
                        }

                        int header_and_listsizes = mserver.title_header_size + mserver.title_length_size * mserver.title_max_lengths;

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

                            // update disc desc
                            byte[] d_name = new byte[mserver.NameDesc_length];
                            Encoding.GetEncoding(codePage).GetBytes(curDisc.Name).CopyTo(d_name, 0);
                            fs.Write(d_name, 0, d_name.Length);
                            byte[] d_artist = new byte[mserver.NameDesc_length];
                            Encoding.GetEncoding(codePage).GetBytes(curDisc.Artist).CopyTo(d_artist, 0);
                            fs.Position += 128 + 64 - d_name.Length;
                            fs.Write(d_artist, 0, d_artist.Length);

                            // update disc checksum
                            byte[] tracksData = new byte[tracks_count * track_desc_size + track_desc_size + 8];
                            fs.Position -= 128 + 64 + d_artist.Length + 8;
                            fs.Read(tracksData, 0, tracksData.Length);
                            byte[] checksum = hf.checksum32bit(tracksData);
                            fs.Position -= tracksData.Length + 4;
                            fs.Write(checksum, 0, checksum.Length);
                            fs.Position += tracksData.Length;
                        }
                    }
                }
            }
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
            updateTrackNames();
			saveTButton.IsEnabled = false;
			//if (discs.Where(d => d.Tracks.Where(t => t.NameChanged).ToList().Count > 0).ToList().Count > 0) saveFButton.IsEnabled = true;
		}

        private void updateTrackNames()
        {
            //update TITLEs
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
                    if (!checkFileForWrite(title_file.FullName)) { continue; }
                    using (FileStream fs = new FileStream(title_file.FullName, FileMode.Open, FileAccess.ReadWrite))
                    {
                        //1 TITLE - n discs
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
                        }

                        int header_and_listsizes = mserver.title_header_size + mserver.title_length_size * mserver.title_max_lengths;

                        int track_desc_size = 2 * (mserver.NameDesc_length + mserver.NameLocDesc_length);
                        fs.Position = header_and_listsizes;
                        for (int i = 0; i < trackListSizes.Count; i++)
                        {
                            byte[] list_header = new byte[mserver.title_list_header_size];
                            fs.Read(list_header, 0, list_header.Length);

                            ElenmentId discid = new ElenmentId(new ArraySegment<byte>(list_header, 4, 4).ToArray());
                            int tracks_count = new ArraySegment<byte>(list_header, 8, 1).ToArray()[0];
                            MSDisc curDisc = discs.Where((dsc) => dsc.Id.FullId == discid.FullId).FirstOrDefault();
                            if (curDisc == null)
                            {
                                fs.Position += tracks_count * track_desc_size + track_desc_size;
                                continue;
                            }

                            // skip disc data
                            fs.Position += track_desc_size;

                            for (int tid = 0; tid < tracks_count; tid++)
                            {
                                MSTrack trackData = curDisc.Tracks.Where(tr => tr.Id == tid + 1).First();
                                // update track name
                                byte[] t_name = new byte[mserver.NameDesc_length];
                                byte[] t_artist = new byte[mserver.NameDesc_length];
                                Encoding.GetEncoding(codePage).GetBytes(trackData.Name).CopyTo(t_name, 0);
                                Encoding.GetEncoding(codePage).GetBytes(trackData.Artist).CopyTo(t_artist, 0);
                                fs.Write(t_name, 0, t_name.Length);
                                fs.Position += mserver.NameLocDesc_length;
                                fs.Write(t_artist, 0, t_artist.Length);
                                fs.Position += mserver.NameLocDesc_length;
                            }

                            // update disc checksum
                            byte[] tracksData = new byte[tracks_count * track_desc_size + track_desc_size + 8];
                            fs.Position -= tracksData.Length;
                            fs.Read(tracksData, 0, tracksData.Length);
                            byte[] checksum = hf.checksum32bit(tracksData);
                            fs.Position -= tracksData.Length + 4;
                            fs.Write(checksum, 0, checksum.Length);
                            fs.Position += tracksData.Length;
                        }
                    }
                }
            }
        }


        private void TrackslistView_MouseUp(object sender, MouseButtonEventArgs e)
		{
			DependencyObject dep = (DependencyObject)e.OriginalSource;
			if (!(dep is System.Windows.Controls.Image)) return;

			System.Windows.Controls.Image img = (dep as System.Windows.Controls.Image);
			MSTrack track = (img.DataContext as MSTrack);
			//if (is_favTrack(track))
			//{
				//lists.Where(l => l.Id == mserver.fav_listId).First().Tracks.Remove(track);
			//}
			//else
			//{
				//lists.Where(l => l.Id == mserver.fav_listId).First().Tracks.Add(track);
			//}

			TrackslistView.SelectedItem = null;
			TrackslistView.Items.Refresh();
		}

		private void reportButton_Click(object sender, RoutedEventArgs e)
		{
			if (groups.Count == 0) return;

			string file = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\" + "Report.txt";
            //if (!checkFileForWrite(file)) { return; }
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

			//saveFButton.IsEnabled = false;
			//System.Windows.MessageBox.Show("Music Server files updated!");
			//fileBackuped.Clear();
			//initServer(mserver.MainDir);
		}

		private bool makeFilecopy(string file)
		{
            //if (fileBackuped.ContainsKey(file) && fileBackuped[file]) return true;
            //if (File.Exists(file + ".old"))
            //{
            //	MessageBoxResult res = System.Windows.MessageBox.Show("File " + file + ".old exists! Do you want overwrite it?", "Save backup file", MessageBoxButton.YesNo);
            //	if (res == MessageBoxResult.No) return false;
            //}
            //File.Copy(file, file + ".old", true);
            //if (!fileBackuped.ContainsKey(file)) fileBackuped.Add(file, true);
            //fileBackuped[file] = true;
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

            if (!checkFileForWrite(info_path)) { return; }
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

            if (!checkFileForWrite(info_path)) { return; }
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
            parseTrackName(new ElenmentId(new byte[] { 0x09, 0x00, 0x00, 0x02 }), "");
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

		private void ServerFromDir_Button_Click(object sender, RoutedEventArgs e)
		{
			if (opendir.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				string sc_path = opendir.SelectedPath;
                //string sc_path = "D:\\tmp\\testmusic_oma";
                //string sc_path = "D:\\id3vtest\\!! музыкаoma_dirs";

                clearCurrentData();

				List<MSDisc> origDiscs = createDirTracksFromDir(sc_path);
				if (origDiscs.Count == 0)
				{
					System.Windows.MessageBox.Show("Valid tracks not found!");
					return;
				}
				createDefaultGroups(origDiscs);
				GroupsListView.Items.Refresh();
				listViewTemplate.Items.Refresh();
				TrackslistView.Items.Refresh();
				mserver.MainDir = sc_path;
				createServer_Button.Visibility = Visibility.Visible;
			}
		}

		private List<MSDisc> createDirTracksFromDir(string sc_path)
		{
			List<MSDisc> discs = new List<MSDisc>();
			DirectoryInfo[] discsDirs = new DirectoryInfo(sc_path).GetDirectories();
			if (discsDirs.Length == 0)
			{
				DirectoryInfo dir = new DirectoryInfo(sc_path);
				if (!File.Exists(dir.FullName + "//TITLE.lst")) { return discs; }
				FileInfo[] sc_files = dir.GetFiles("???.sc").Where((f) => Regex.IsMatch(f.Name, @"^[0-9]{3}\.sc$")).ToArray();
				if (sc_files.Length == 0 || sc_files.Length > 99) { return discs; }

				ElenmentId discId = getNexDiscId(discs);
				if (discId == null)
				{
					System.Windows.MessageBox.Show("Can`t find new disc id for " + dir.Name + "!");
					return null;
				}

				// may be use disc name from TITLE.lst?
				MSDisc disc = new MSDisc(discId, dir.Name);
				disc.Exists = true;
				disc.OrigDirFullPath = dir.FullName;
				int track_desc_size = 2 * (mserver.NameDesc_length + mserver.NameLocDesc_length);
				using (FileStream fs = new FileStream(dir.FullName + "//TITLE.lst", FileMode.Open, FileAccess.Read))
				{
					fs.Position = mserver.title_header_size + mserver.title_max_lengths * mserver.title_length_size;

					byte[] list_header = new byte[mserver.title_list_header_size];
					fs.Read(list_header, 0, list_header.Length);
					int tracks_count = new ArraySegment<byte>(list_header, 8, 1).ToArray()[0];

					// disc desc
					//byte[] disc_artist = new byte[track_desc_size];
					//fs.Read(disc_artist, 0, disc_artist.Length);
					fs.Position += track_desc_size;

					for (int tid = 0; tid < tracks_count; tid++)
					{
						byte[] track_data = new byte[track_desc_size];
						fs.Read(track_data, 0, track_data.Length);
						MSTrack track = new MSTrack(
							disc.Id,
							tid + 1,
							new ArraySegment<byte>(track_data, 0, mserver.NameDesc_length).ToArray(),
							new ArraySegment<byte>(track_data, mserver.NameDesc_length + mserver.NameLocDesc_length, mserver.NameDesc_length).ToArray()
						);
						if (sc_files.Where((sc) => sc.Name == track.File).ToArray().Length == 0) { continue; }
						track.Exists = true;
						disc.AddTrack(track);
					}
				}
				discs.Add(disc);
				return discs;
			}

			foreach (DirectoryInfo dir in discsDirs)
			{
				if (!File.Exists(dir.FullName + "//TITLE.lst")) { continue; }
				FileInfo[] sc_files = dir.GetFiles("???.sc").Where((f) => Regex.IsMatch(f.Name, @"^[0-9]{3}\.sc$")).ToArray();
				if (sc_files.Length == 0 || sc_files.Length > 99) { continue; }

				ElenmentId discId = getNexDiscId(discs);
				if (discId == null)
				{
					System.Windows.MessageBox.Show("Can`t find new disc id for " + dir.Name + "!");
					return null;
				}

				// may be use disc name from TITLE.lst?
				MSDisc disc = new MSDisc(discId, dir.Name);
				disc.Exists = true;
				disc.OrigDirFullPath = dir.FullName;
				int track_desc_size = 2 * (mserver.NameDesc_length + mserver.NameLocDesc_length);
				using (FileStream fs = new FileStream(dir.FullName + "//TITLE.lst", FileMode.Open, FileAccess.Read))
				{
					fs.Position = mserver.title_header_size + mserver.title_max_lengths * mserver.title_length_size;

					byte[] list_header = new byte[mserver.title_list_header_size];
					fs.Read(list_header, 0, list_header.Length);
					int tracks_count = new ArraySegment<byte>(list_header, 8, 1).ToArray()[0];

					// disc desc
					//byte[] disc_artist = new byte[track_desc_size];
					//fs.Read(disc_artist, 0, disc_artist.Length);
					fs.Position += track_desc_size;

					for (int tid = 0; tid < tracks_count; tid++)
					{
						byte[] track_data = new byte[track_desc_size];
						fs.Read(track_data, 0, track_data.Length);
						MSTrack track = new MSTrack(
							disc.Id,
							tid + 1,
							new ArraySegment<byte>(track_data, 0, mserver.NameDesc_length).ToArray(),
							new ArraySegment<byte>(track_data, mserver.NameDesc_length + mserver.NameLocDesc_length, mserver.NameDesc_length).ToArray()
						);
						if (sc_files.Where((sc) => sc.Name == track.File).ToArray().Length == 0) { continue; }
						track.Exists = true;
						disc.AddTrack(track);
					}
				}
				discs.Add(disc);
			}
			return discs;
		}

		private void createDefaultGroups(List<MSDisc> discs)
		{
			MSGroup origDiscs = new MSGroup(0, 171);
			origDiscs.Discs.AddRange(discs);
			groups.Add(origDiscs);

			MSGroup bestFiles = new MSGroup(1, 172);
			bestFiles.Lists.Add(new MSList(1, 174));
			bestFiles.Lists.Add(new MSList(2, 173));
			groups.Add(bestFiles);
		}

		private ElenmentId getNexDiscId(List<MSDisc> discs)
		{
			ElenmentId newDiscId = new ElenmentId(0, 1);
			if (discs.Count == 0) { return newDiscId; }

			bool founded = false;
			for (int i = 0; i < 0x3c; i++)
			{
				if (founded) { break; }
				for (int k = 1; k < 101; k++)
				{
					newDiscId.Id = i; newDiscId.Prefix = k;
					if (discs.Where((dsc) => dsc.Id.FullId == newDiscId.FullId).ToArray().Length > 0)
					{
						continue;
					}
					founded = true;
					break;
				}
			}
			if (!founded) { return null; }
			return newDiscId;
		}

		private void CreateServer_Button_Click(object sender, RoutedEventArgs e)
		{
			Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            createServer();
            copyMoveFromDir();
            //Mouse.OverrideCursor = null;
            //System.Windows.MessageBox.Show("Done!");

            //// need copy/move process!!!
            //copyMoveProgress.Visibility = Visibility.Visible;
            //int tracksCount = groups.Where((grp) => grp.Id == 0).First().Discs.Sum((dsc) => dsc.Tracks.Count);
            //copyMoveProgress.Maximum = tracksCount;
            //copyMoveProgress.Value = 0;
            //// need copy/move input!!!
            //MessageBoxResult copy_move_select = System.Windows.MessageBox.Show(
            //	"Copy track files to DATA directory or move? (Press 'Yes' to copy)",
            //	"Create DATA directory",
            //	MessageBoxButton.YesNo,
            //	MessageBoxImage.Warning
            //);
            //copyMoveworker = new System.ComponentModel.BackgroundWorker();
            //copyMoveworker.DoWork += new System.ComponentModel.DoWorkEventHandler(copyMoveWork);
            //copyMoveworker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(copyMoveWorkComplete);
            //copyMoveworker.RunWorkerAsync(copy_move_select == MessageBoxResult.Yes);

            //createServer_Button.IsEnabled = false;
            createServer_Button.Visibility = Visibility.Hidden;
        }

        private void createServer()
        {
            string AVunitDir = mserver.MainDir + "\\AVUNIT";
            if (Directory.Exists(AVunitDir)) { Directory.Delete(AVunitDir, true); }
            Directory.CreateDirectory(AVunitDir);
            mserver.MainDir = AVunitDir;

            // create ALBUM
            Directory.CreateDirectory(mserver.MainDir + mserver.INFO_path + mserver.ALBUM_path + mserver.ALBUM_path + mserver.defALBUM_ID);
            //if (!checkFileForWrite(mserver.get_ALBUMpath())) { return; }
            using (FileStream fs = new FileStream(mserver.get_ALBUMpath(), FileMode.Create, FileAccess.Write))
            {
                // header
                byte[] album_header = new byte[mserver.album_header_size];
                string header_text = "SLJA_ALBUM:1.3 " + mserver.defALBUM_ID;
                Encoding.UTF8.GetBytes(header_text).CopyTo(album_header, 4);
                // lengths
                byte[] lengths = new byte[mserver.album_max_lists * mserver.album_length_size];
                // fav list header
                byte[] fl_header = new byte[mserver.album_list_header_size];
                new byte[] { 0x01 }.CopyTo(fl_header, 4);
                new byte[] { 0x01 }.CopyTo(fl_header, 8);
                mserver.defALBUM_delim.CopyTo(fl_header, 12);
                new byte[] { 0x1b }.CopyTo(fl_header, 20);
                Encoding.UTF8.GetBytes("[tbl:174]").CopyTo(fl_header, 21);
                hf.checksum32bit(fl_header).CopyTo(fl_header, 0);
                // offen list header
                byte[] ol_header = new byte[mserver.album_list_header_size];
                new byte[] { 0x02 }.CopyTo(ol_header, 4);
                new byte[] { 0x01 }.CopyTo(ol_header, 8);
                mserver.defALBUM_delim.CopyTo(ol_header, 12);
                new byte[] { 0x1b }.CopyTo(ol_header, 20);
                Encoding.UTF8.GetBytes("[tbl:173]").CopyTo(ol_header, 21);
                hf.checksum32bit(ol_header).CopyTo(ol_header, 0);
                // lengths of lists
                BitConverter.GetBytes(fl_header.Length).CopyTo(lengths, 0);
                BitConverter.GetBytes(ol_header.Length).CopyTo(lengths, 4);
                // checksum of header
                hf.checksum32bit(album_header, lengths).CopyTo(album_header, 0);
                // write header
                fs.Write(album_header, 0, album_header.Length);
                // write lengths
                fs.Write(lengths, 0, lengths.Length);
                // write fav list header
                fs.Write(fl_header, 0, fl_header.Length);
                // write offen list header
                fs.Write(ol_header, 0, ol_header.Length);
            }

            // create DISCID
            string DISCID_path = mserver.MainDir + mserver.INFO_path + mserver.DISCID_path;
            Directory.CreateDirectory(DISCID_path);
            foreach (MSDisc disc in groups.Where((grp) => grp.Id == 0).First().Discs)
            {
                string sDiscId = hf.ByteArrayToHexString(new byte[] { (byte)disc.Id.Id });
                //if (!checkFileForWrite(DISCID_path + "\\DISCID" + sDiscId + ".lst")) { continue; }
                using (FileStream fs = new FileStream(DISCID_path + "\\DISCID" + sDiscId + ".lst", FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    if (fs.Length == 0)
                    {
                        byte[] header = new byte[mserver.discid_header_size];
                        string header_text = "SLJA_DISCID:1.3 " + sDiscId;
                        Encoding.UTF8.GetBytes(header_text).CopyTo(header, 0);
                        fs.Write(header, 0, header.Length);
                    }
                    else
                    {
                        fs.Position = fs.Length;
                    }

                    byte[] dataDisc_header = new byte[mserver.discid_datadisc_header_size];
                    hf.HexStringToByteArray(disc.Id.FullId)
                        .Reverse().ToArray()
                        .CopyTo(dataDisc_header, 4);
                    new byte[] { (byte)disc.Tracks.Count }.CopyTo(dataDisc_header, 8);
                    mserver.discid_dataheader_unknown.CopyTo(dataDisc_header, 12);
                    mserver.discid_datadisc_96.CopyTo(dataDisc_header, 16);

                    byte[] dataDisc = new byte[mserver.discid_datadisc_size];
                    for (int i = 0; i < disc.Tracks.Count - 1; i++)
                    {
                        mserver.discid_datadisc_offset.CopyTo(dataDisc, i * 4);
                    }

                    hf.checksum32bit(dataDisc_header, dataDisc).CopyTo(dataDisc_header, 0);
                    // write header
                    fs.Write(dataDisc_header, 0, dataDisc_header.Length);
                    // write data
                    fs.Write(dataDisc, 0, dataDisc.Length);
                }
            }

            // create RECORD
            string RECORD_path = mserver.MainDir + mserver.INFO_path + mserver.RECORD_path;
            Directory.CreateDirectory(RECORD_path);
            foreach (MSDisc disc in groups.Where((grp) => grp.Id == 0).First().Discs.OrderBy((dsc) => dsc.Id.Prefix))
            {
                string sDiscId = hf.ByteArrayToHexString(new byte[] { (byte)disc.Id.Id });
                Directory.CreateDirectory(RECORD_path + "\\RECORD" + sDiscId);
                using (FileStream fs = new FileStream(RECORD_path + "\\RECORD" + sDiscId + "\\RECORD" + sDiscId + "00001.lst", FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    byte[] header = new byte[mserver.record_header_size];
                    byte[] lengths = new byte[mserver.record_lengths_size];
                    if (fs.Length == 0)
                    {
                        string header_text = "SLJA_RECORD:1.3 " + sDiscId + "00001";
                        Encoding.UTF8.GetBytes(header_text).CopyTo(header, 4);
                    }
                    else
                    {
                        fs.Read(header, 0, header.Length);
                        // for update checksum
                        new byte[] { 0, 0, 0, 0 }.CopyTo(header, 0);
                        fs.Read(lengths, 0, lengths.Length);
                        fs.Position = fs.Length;
                    }

                    byte[] data_header = new byte[mserver.record_datadisc_header_size];
                    hf.HexStringToByteArray(disc.Id.FullId)
                        .Reverse().ToArray()
                        .CopyTo(data_header, 4);
                    new byte[] { (byte)disc.Tracks.Count }.CopyTo(data_header, 8);
                    mserver.record_unknown_delim.CopyTo(data_header, 10);

                    byte[] tracks_data = new byte[mserver.record_datadisc_trackdata_size * disc.Tracks.Count];
                    byte[] track_data = new byte[mserver.record_datadisc_trackdata_size];
                    new byte[] { 0x01 }.CopyTo(track_data, 0);
                    mserver.record_unknown_delim.CopyTo(track_data, 4);
                    for (int i = 0; i < disc.Tracks.Count; i++)
                    {
                        track_data.CopyTo(tracks_data, i * mserver.record_datadisc_trackdata_size);
                    }
                    hf.checksum32bit(data_header, tracks_data).CopyTo(data_header, 0);

                    // update lengths
                    BitConverter.GetBytes(data_header.Length + tracks_data.Length).CopyTo(lengths, 4 * (disc.Id.Prefix - 1));
                    // update header
                    hf.checksum32bit(header, lengths).CopyTo(header, 0);

                    if (fs.Length == 0)
                    {
                        fs.Write(header, 0, header.Length);
                        fs.Write(lengths, 0, lengths.Length);
                        fs.Write(data_header, 0, data_header.Length);
                        fs.Write(tracks_data, 0, tracks_data.Length);
                    }
                    else
                    {
                        fs.Write(data_header, 0, data_header.Length);
                        fs.Write(tracks_data, 0, tracks_data.Length);
                        fs.Position = 0;
                        fs.Write(header, 0, header.Length);
                        fs.Write(lengths, 0, lengths.Length);
                    }
                }
            }

            // create TITLE
            string TITLE_path = mserver.MainDir + mserver.INFO_path + mserver.TITLE_path;
            Directory.CreateDirectory(TITLE_path);
            foreach (MSDisc disc in groups.Where((grp) => grp.Id == 0).First().Discs.OrderBy((dsc) => dsc.Id.Prefix))
            {
                string sDiscId = hf.ByteArrayToHexString(new byte[] { (byte)disc.Id.Id });
                Directory.CreateDirectory(TITLE_path + "\\TITLE" + sDiscId);
                using (FileStream fs = new FileStream(TITLE_path + "\\TITLE" + sDiscId + "\\TITLE" + sDiscId + "00001.lst", FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    byte[] header = new byte[mserver.title_header_size];
                    byte[] lengths = new byte[mserver.title_length_size * mserver.title_max_lengths];
                    if (fs.Length == 0)
                    {
                        string header_text = "SLJA_TITLE:1.3 " + sDiscId + "00001";
                        Encoding.UTF8.GetBytes(header_text).CopyTo(header, 4);
                    }
                    else
                    {
                        fs.Read(header, 0, header.Length);
                        // for update checksum
                        new byte[] { 0, 0, 0, 0 }.CopyTo(header, 0);
                        fs.Read(lengths, 0, lengths.Length);
                        fs.Position = fs.Length;
                    }

                    byte[] disc_header = new byte[mserver.title_list_header_size];
                    hf.HexStringToByteArray(disc.Id.FullId)
                        .Reverse().ToArray()
                        .CopyTo(disc_header, 4);
                    new byte[] { (byte)disc.Tracks.Count, 1 }.CopyTo(disc_header, 8);
                    hf.checksum32bit(disc_header).CopyTo(disc_header, 0);
                    mserver.title_unknown.CopyTo(disc_header, 20);

                    byte[] disc_desc = new byte[2 * (mserver.NameDesc_length + mserver.NameLocDesc_length)];
                    Encoding.GetEncoding(codePage).GetBytes(disc.Name).CopyTo(disc_desc, 0);
                    Encoding.GetEncoding(codePage).GetBytes(disc.Artist).CopyTo(disc_desc, mserver.NameDesc_length + mserver.NameLocDesc_length);

                    byte[] tracks_descs = new byte[disc.Tracks.Count * disc_desc.Length];
                    foreach (MSTrack track in disc.Tracks.OrderBy((trk) => trk.Id))
                    {
                        Encoding.GetEncoding(codePage).GetBytes(track.Name).CopyTo(tracks_descs, (track.Id - 1) * disc_desc.Length);
                        Encoding.GetEncoding(codePage).GetBytes(track.Artist).CopyTo(tracks_descs, (track.Id - 1) * disc_desc.Length + mserver.NameDesc_length + mserver.NameLocDesc_length);
                    }
                    byte[] disc_cs = hf.checksum32bit(disc_desc, tracks_descs);
                    hf.checksum32bit(disc_cs, new ArraySegment<byte>(disc_header, 20, 8).ToArray()).CopyTo(disc_header, 16);

                    // update lengths
                    BitConverter.GetBytes(disc_header.Length + disc_desc.Length + tracks_descs.Length).CopyTo(lengths, 4 * (disc.Id.Prefix - 1));
                    // update header
                    hf.checksum32bit(header, lengths).CopyTo(header, 0);

                    if (fs.Length == 0)
                    {
                        fs.Write(header, 0, header.Length);
                        fs.Write(lengths, 0, lengths.Length);
                        fs.Write(disc_header, 0, disc_header.Length);
                        fs.Write(disc_desc, 0, disc_desc.Length);
                        fs.Write(tracks_descs, 0, tracks_descs.Length);
                    }
                    else
                    {
                        fs.Write(disc_header, 0, disc_header.Length);
                        fs.Write(disc_desc, 0, disc_desc.Length);
                        fs.Write(tracks_descs, 0, tracks_descs.Length);
                        fs.Position = 0;
                        fs.Write(header, 0, header.Length);
                        fs.Write(lengths, 0, lengths.Length);
                    }
                }
            }

            // create INDEX
            using (FileStream fs = new FileStream(mserver.get_INDEXpath(), FileMode.Create, FileAccess.Write))
            {
                byte[] header = new byte[mserver.index_header_size];
                string header_text = "SLJA_INDEX:1.5";
                Encoding.UTF8.GetBytes(header_text).CopyTo(header, 4);
                int ld_count = groups.Where((grp) => grp.Id == 0).First().Discs.Count + groups.Where((grp) => grp.Id == 1).First().Lists.Count;
                new byte[] { (byte)ld_count }.CopyTo(header, 36);

                byte[] groups_data = new byte[mserver.index_max_groups * (mserver.NameDesc_length + 4)];
                byte[] discs_lists_data = { };
                foreach (MSGroup group in groups)
                {
                    byte[] group_data = new byte[mserver.NameDesc_length + 4];

                    new byte[] { (byte)group.Id }.CopyTo(group_data, 0);
                    if (group.Id == 0)
                    {
                        new byte[] { 0x1b }.CopyTo(group_data, 4);
                        Encoding.UTF8.GetBytes("[tbl:171]").CopyTo(group_data, 5);

                        foreach (MSDisc disc in group.Discs)
                        {
                            byte[] disc_data = new byte[8];
                            hf.HexStringToByteArray(disc.Id.FullId)
                                .Reverse().ToArray()
                                .CopyTo(disc_data, 0);
                            Array.Resize(ref discs_lists_data, discs_lists_data.Length + disc_data.Length);
                            disc_data.CopyTo(discs_lists_data, discs_lists_data.Length - disc_data.Length);
                        }
                    }
                    else if (group.Id == 1)
                    {
                        new byte[] { 0x1b }.CopyTo(group_data, 4);
                        Encoding.UTF8.GetBytes("[tbl:172]").CopyTo(group_data, 5);

                        foreach (MSList list in group.Lists)
                        {
                            byte[] list_data = new byte[8];
                            new byte[] { (byte)list.Id }.CopyTo(list_data, 0);
                            new byte[] { 1 }.CopyTo(list_data, 4);
                            new byte[] { (byte)group.Id }.CopyTo(list_data, 7);
                            Array.Resize(ref discs_lists_data, discs_lists_data.Length + list_data.Length);
                            list_data.CopyTo(discs_lists_data, discs_lists_data.Length - list_data.Length);
                        }
                    }
                    else
                    {
                        Encoding.GetEncoding(codePage).GetBytes(group.Name).CopyTo(group_data, 4);

                        foreach (MSList list in group.Lists)
                        {
                            byte[] list_data = new byte[8];
                            new byte[] { (byte)list.Id }.CopyTo(list_data, 0);
                            new byte[] { 1 }.CopyTo(list_data, 4);
                            new byte[] { (byte)group.Id }.CopyTo(list_data, 8);
                            Array.Resize(ref discs_lists_data, discs_lists_data.Length + list_data.Length);
                            list_data.CopyTo(discs_lists_data, discs_lists_data.Length - list_data.Length);
                        }
                    }
                    group_data.CopyTo(groups_data, group.Id * group_data.Length);
                }
                // empty groups
                for (int i = 0; i < mserver.index_max_groups - groups.Count; i++)
                {
                    byte[] group_data = new byte[mserver.NameDesc_length + 4];
                    new byte[] { 0xff, 0, 0, 1 }.CopyTo(group_data, 0);
                    group_data.CopyTo(groups_data, (i + groups.Count) * group_data.Length);
                }

                byte[] checksum1 = hf.checksum32bit(header, groups_data);
                UInt32 checksum2 = BitConverter.ToUInt32(hf.checksum32bit(checksum1, discs_lists_data), 0);
                checksum2 += (UInt32)ld_count;
                BitConverter.GetBytes(checksum2).CopyTo(header, 0);

                fs.Write(header, 0, header.Length);
                fs.Write(groups_data, 0, groups_data.Length);
                fs.Write(discs_lists_data, 0, discs_lists_data.Length);
            }

            // create ORG_ARRAY
            using (FileStream fs = new FileStream(mserver.get_ORGpath(), FileMode.Create, FileAccess.Write))
            {
                byte[] header = new byte[mserver.org_header_size];
                string header_text = "DATE";
                int discsCount = groups.Where((grp) => grp.Id == 0).First().Discs.Count;
                Encoding.UTF8.GetBytes(header_text).CopyTo(header, 0);
                new byte[] { (byte)discsCount }.CopyTo(header, 4);
                fs.Write(header, 0, header.Length);

                foreach (MSDisc disc in groups.Where((grp) => grp.Id == 0).First().Discs)
                {
                    byte[] disc_data = new byte[mserver.org_discdata_size];
                    hf.HexStringToByteArray(disc.Id.FullId)
                        .Reverse().ToArray()
                        .CopyTo(disc_data, 1);
                    Encoding.GetEncoding(codePage).GetBytes(disc.Name).CopyTo(disc_data, 12);
                    new byte[] { (byte)disc.Tracks.Count }.CopyTo(disc_data, 204);
                    mserver.org_unknown_end.CopyTo(disc_data, 208);
                    fs.Write(disc_data, 0, disc_data.Length);
                }
            }

            // create others (AVVRALBUMARTIST.lst etc.) ???

            // create DATA (copy/move files, add DISCID files)
            Directory.CreateDirectory(mserver.MainDir + "\\DATA");
        }

        private void copyMoveFromDir()
        {
            // need copy/move process!!!
            copyMoveProgress.Visibility = Visibility.Visible;
            int tracksCount = groups.Where((grp) => grp.Id == 0).First().Discs.Sum((dsc) => dsc.Tracks.Count);
            copyMoveProgress.Maximum = tracksCount;
            copyMoveProgress.Value = 0;
            // need copy/move input!!!
            MessageBoxResult copy_move_select = System.Windows.MessageBox.Show(
                "Copy track files to DATA directory or move? (Press 'Yes' to copy)",
                "Create DATA directory",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );
            copyMoveworker = new System.ComponentModel.BackgroundWorker();
            copyMoveworker.DoWork += new System.ComponentModel.DoWorkEventHandler(copyMoveWork);
            copyMoveworker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(copyMoveWorkComplete);
            copyMoveworker.RunWorkerAsync(copy_move_select == MessageBoxResult.Yes);
        }

		private void copyMoveWork(object sender, System.ComponentModel.DoWorkEventArgs arg)
		{
			copyMoveTracks2Server((bool)arg.Argument);
		}

		private void copyMoveWorkComplete(object sender, System.ComponentModel.RunWorkerCompletedEventArgs arg)
		{
			Mouse.OverrideCursor = null;
			System.Windows.MessageBox.Show("Done!");
            if (arg.Error != null) { System.Windows.MessageBox.Show("Error: " + arg.Error.ToString()); }
			copyMoveProgress.Visibility = Visibility.Hidden;
		}

		private void copyMoveTracks2Server(bool toCopy)
		{
			UpdateProgressBarDelegate updProgress = new UpdateProgressBarDelegate(copyMoveProgress.SetValue);
			double value = 0;
            foreach (MSDisc disc in groups.Where((grp) => grp.Id == 0).First().Discs)
			{
				string sDiscId = hf.ByteArrayToHexString(new byte[] { (byte)disc.Id.Id });
				DirectoryInfo data_disc_dir = Directory.CreateDirectory(mserver.MainDir + "\\DATA" + "\\DATA" + sDiscId + "\\" + disc.Id.FullId);
				foreach (FileInfo track in new DirectoryInfo(disc.OrigDirFullPath).GetFiles("*.sc"))
				{
                    string trackName = track.Name;
                    if (! Regex.IsMatch(trackName, @"^[0-9][0-9][0-9]\.sc$"))
                    {
                        MSTrack otrack = disc.Tracks.Where(trk => trk.OrigTrackName == trackName).FirstOrDefault();
                        if (otrack == null)
                        {
                            Dispatcher.Invoke(updProgress, new object[] { System.Windows.Controls.ProgressBar.ValueProperty, ++value });
                            continue;
                        }
                        trackName = string.Format("{0,3:D3}", otrack.Id) + ".sc";
                    }

                    if (toCopy)
					{
						File.Copy(track.FullName, data_disc_dir.FullName + "\\" + trackName);
					}
					else
					{
						File.Move(track.FullName, data_disc_dir.FullName + "\\" + trackName);
					}
					
					Dispatcher.Invoke(updProgress, new object[] { System.Windows.Controls.ProgressBar.ValueProperty, ++value });
				}
				using (FileStream fs = new FileStream(data_disc_dir.FullName + "\\" + disc.Id.FullId + "DISCID.lst", FileMode.Create, FileAccess.Write))
				{
					byte[] header = new byte[mserver.discid_header_size];
					string header_text = "SLJA_DISCID:1.3 " + sDiscId;
					Encoding.UTF8.GetBytes(header_text).CopyTo(header, 0);
					fs.Write(header, 0, header.Length);

					byte[] dataDisc_header = new byte[mserver.discid_datadisc_header_size];
					new byte[] { (byte)disc.Id.Id }.CopyTo(dataDisc_header, 4);
					new byte[] { (byte)disc.Tracks.Count }.CopyTo(dataDisc_header, 8);
					mserver.discid_dataheader_unknown.CopyTo(dataDisc_header, 12);
					mserver.discid_datadisc_96.CopyTo(dataDisc_header, 16);

					byte[] dataDisc = new byte[mserver.discid_datadisc_size];
					for (int i = 0; i < disc.Tracks.Count - 1; i++)
					{
						mserver.discid_datadisc_offset.CopyTo(dataDisc, i * 4);
					}

					// no need checksum
					//hf.checksum32bit(dataDisc_header, dataDisc).CopyTo(dataDisc_header, 0);
					// write header
					fs.Write(dataDisc_header, 0, dataDisc_header.Length);
					// write data
					fs.Write(dataDisc, 0, dataDisc.Length);
				}
			}
		}

		private void ChangeNATrackButton_Click(object sender, RoutedEventArgs e)
		{
			if (TrackslistView.SelectedItem == null) { return; }

			MSTrack track = (TrackslistView.SelectedItem as MSTrack);
			string Artist = track.Artist;
			track.Artist = track.Name;
			track.Name = Artist;

			TrackslistView.Items.Refresh();
			listViewTemplate.Items.Refresh();
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
            radioButton_Copy11.Visibility = vis;
            radioButton_Copy12.Visibility = vis;
            radioButton_Copy13.Visibility = vis;
            radioButton_Copy14.Visibility = vis;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MSTrack track = TrackslistView.SelectedItem as MSTrack;
            if (!track.Exists)
            {
                System.Windows.MessageBox.Show("Track file not exist!");
                return;
            }
            string dataPath = mserver.get_DATApath();
            string discId = "DATA" + track.DiscID.FullId.Remove(2);
            string dirName = track.DiscID.FullId;
            string filePath = dataPath + "\\" + discId + "\\" + dirName + "\\" + track.File;
            System.Diagnostics.Process.Start(@filePath);
        }

        private void export1Button_Click(object sender, RoutedEventArgs e)
        {
            if (groups.Where((grp) => grp.Id == 0).First().Discs.Count < 1) { return; }

            if (opendir.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string export_path = opendir.SelectedPath;
                                
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                copyMoveProgress.Visibility = Visibility.Visible;
                int tracksCount = groups.Where((grp) => grp.Id == 0).First().Discs.Sum((dsc) => dsc.Tracks.Count);
                copyMoveProgress.Maximum = tracksCount;
                copyMoveProgress.Value = 0;
                //exportTracks1worker = new System.ComponentModel.BackgroundWorker();
                //exportTracks1worker.DoWork += new System.ComponentModel.DoWorkEventHandler(ExportTracks1Work);
                //exportTracks1worker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(copyMoveWorkComplete);
                //exportTracks1worker.RunWorkerAsync(export_path);
                ExportTracks1(export_path);
                Mouse.OverrideCursor = null;
                copyMoveProgress.Visibility = Visibility.Hidden;
                System.Windows.MessageBox.Show("Done!");
            }
        }

        //private void ExportTracks1Work(object sender, System.ComponentModel.DoWorkEventArgs arg)
        //{
        //    ExportTracks1((string)arg.Argument);
        //}

        private void ExportTracks1(string export_path)
        {
            UpdateProgressBarDelegate updProgress = new UpdateProgressBarDelegate(copyMoveProgress.SetValue);
            double value = 0;
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex invalidChars = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            foreach (MSDisc disc in groups.Where((grp) => grp.Id == 0).First().Discs)
            {
                string dirName = (disc.Artist == "" && disc.Name == "")
                    ? disc.Id.FullId
                    : disc.Artist + " == " + disc.Name;
                //string dirName = disc.Id.FullId;
                dirName = invalidChars.Replace(dirName, "");
                if (Directory.Exists(export_path + "\\" + dirName)) { Directory.Delete(export_path + "\\" + dirName, true); }
                DirectoryInfo discDir = Directory.CreateDirectory(export_path + "\\" + dirName);

                foreach (MSTrack track in disc.Tracks)
                {
                    string sc_file = mserver.get_DATApath() + "\\DATA" + track.DiscID.FullId.Remove(2)
                        + "\\" + track.DiscID.FullId + "\\" + track.File;
                    string trackName = (track.Artist == "" && track.Name == "")
                        ? track.File
                        : track.File.Remove(3) + "." + track.Artist + " == " + track.Name + ".sc";
                    trackName = invalidChars.Replace(trackName, "");
                    string exp_file = discDir.FullName + "\\" + trackName;

                    if (File.Exists(sc_file))
                    {
                        File.Copy(sc_file, exp_file);
                        Dispatcher.Invoke(updProgress, new object[] { System.Windows.Controls.ProgressBar.ValueProperty, ++value });
                    }
                }
            }
        }

        private void ServerFromExp1Button_Click(object sender, RoutedEventArgs e)
        {
            if (opendir.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string sc_path = opendir.SelectedPath;
                //string sc_path = "D:\\tmp\\testmusic_oma";
                //string sc_path = "D:\\id3vtest\\!! музыкаoma_dirs";

                clearCurrentData();

                List<MSDisc> origDiscs = createDirTracksFromExp1Dir(sc_path);
                if (origDiscs.Count == 0)
                {
                    System.Windows.MessageBox.Show("Valid tracks not found!");
                    return;
                }
                createDefaultGroups(origDiscs);
                GroupsListView.Items.Refresh();
                listViewTemplate.Items.Refresh();
                TrackslistView.Items.Refresh();
                mserver.MainDir = sc_path;
                createServer_Button.Visibility = Visibility.Visible;
            }
        }

        private MSTrack parseTrackName(ElenmentId discId, string trackName)
        {
            int tid = 0; string artist = ""; string name = "";
            Regex reTrackName = new Regex(@"^([0-9][0-9][0-9])\.(.*) == (.*)\.sc$");
            Regex reTrackName1 = new Regex(@"^([0-9][0-9][0-9])\.(.*)\.sc$");

            // 012.sc
            if (Regex.IsMatch(trackName, @"^[0-9][0-9][0-9]\.sc$"))
            {
                name = trackName.Remove(trackName.Length - 3, 3);
                tid = Convert.ToInt32(name);
            }
            // 001.Eagles == Peaceful Easy Feeling.sc
            // 008. == $0-D0 (-B 00).sc
            // 003.0 == .sc
            else
            {
                Match tname = reTrackName.Match(trackName);
                Match tname1 = reTrackName1.Match(trackName);
                if (tname.Groups.Count == 4)
                {
                    tid = Convert.ToInt32(tname.Groups[1].Value);
                    artist = tname.Groups[2].Value;
                    name = tname.Groups[3].Value;
                }
                else if (tname1.Groups.Count == 3)
                {
                    tid = Convert.ToInt32(tname.Groups[1].Value);
                    name = tname.Groups[2].Value;
                }
            }

            if (tid == 0)
            {
                System.Windows.MessageBox.Show("Can`t parse id for " + trackName + "!");
                return null;
            }

            return new MSTrack(discId, tid,
                Encoding.GetEncoding(codePage).GetBytes(name),
                Encoding.GetEncoding(codePage).GetBytes(artist)
            );
        }

        private MSDisc parseDirName(ElenmentId discId, string dirName)
        {
            string artist = ""; string name = "";
            Regex reDirName = new Regex(@"^(.*) == (.*)$");
            // 0F000001
            if (Regex.IsMatch(dirName, @"^[0-9A-F][0-9A-F]0000[0-9A-F][0-9A-F]$"))
            {
                name = dirName;
            }
            // ATB == ATB - FUTURE MEMORIES CD2
            //  == ATB - FUTURE MEMORIES CD2
            // ATB == 
            else
            {
                Match tname = reDirName.Match(dirName);
                if (tname.Groups.Count == 3)
                {
                    artist = tname.Groups[1].Value;
                    name = tname.Groups[2].Value;
                }
                // ATB
                else
                {
                    name = dirName;
                }
            }

            if (artist == "" && name == "")
            {
                System.Windows.MessageBox.Show("Can`t parse artist and title for " + dirName + "!");
                return null;
            }

            MSDisc disc = new MSDisc(discId, Encoding.GetEncoding(codePage).GetBytes(name));
            disc.SetArtist(Encoding.GetEncoding(codePage).GetBytes(artist));
            return disc;
        }

        private void fixTracksId(MSDisc disc)
        {
            int tid = 1;
            foreach (MSTrack track in disc.Tracks.OrderBy((trk) => trk.Id))
            {
                track.Id = tid++;
            }
        }

        private List<MSDisc> createDirTracksFromExp1Dir(string sc_path)
        {
            List<MSDisc> discs = new List<MSDisc>();
            DirectoryInfo[] discsDirs = new DirectoryInfo(sc_path).GetDirectories();

            if (discsDirs.Length == 0)
            {
                DirectoryInfo dir = new DirectoryInfo(sc_path);
                FileInfo[] sc_files = dir.GetFiles("*.sc");
                if (sc_files.Length == 0 || sc_files.Length > 99) { return discs; }

                ElenmentId discId = getNexDiscId(discs);
                if (discId == null)
                {
                    System.Windows.MessageBox.Show("Can`t find new disc id for " + dir.Name + "!");
                    return null;
                }

                MSDisc disc = parseDirName(discId, dir.Name);
                if (disc == null) { return null; }                
                disc.Exists = true;
                disc.OrigDirFullPath = dir.FullName;

                foreach (FileInfo trackFile in sc_files)
                {
                    MSTrack track = parseTrackName(disc.Id, trackFile.Name);
                    if (track == null)
                    {
                        MessageBoxResult continue_select = System.Windows.MessageBox.Show(
                            "Continue?",
                            "Parse " + dir.Name + " directory",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                        );
                        if (continue_select == MessageBoxResult.No) { return null; }
                        continue;
                    }                    
                    track.Exists = true;
                    track.OrigTrackName = trackFile.Name;
                    disc.AddTrack(track);
                }

                discs.Add(disc);
                return discs;
            }

            foreach (DirectoryInfo dir in discsDirs)
            {
                FileInfo[] sc_files = dir.GetFiles("*.sc");
                if (sc_files.Length == 0 || sc_files.Length > 99) { return discs; }

                ElenmentId discId = getNexDiscId(discs);
                if (discId == null)
                {
                    System.Windows.MessageBox.Show("Can`t find new disc id for " + dir.Name + "!");
                    return null;
                }

                string[] artName = dir.Name.Split(new string[] { " == " }, StringSplitOptions.None);
                MSDisc disc = parseDirName(discId, dir.Name);
                if (disc == null)
                {
                    MessageBoxResult continue_select = System.Windows.MessageBox.Show(
                            "Continue?",
                            "Parse " + dir.Name + " directory",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                        );
                    if (continue_select == MessageBoxResult.No) { return null; }
                    continue;
                }
                disc.Exists = true;
                disc.OrigDirFullPath = dir.FullName;

                foreach (FileInfo trackFile in sc_files)
                {
                    MSTrack track = parseTrackName(disc.Id, trackFile.Name);
                    if (track == null)
                    {
                        MessageBoxResult continue_select = System.Windows.MessageBox.Show(
                            "Continue?",
                            "Parse " + dir.Name + " directory",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                        );
                        if (continue_select == MessageBoxResult.No) { return null; }
                        continue;
                    }
                    track.Exists = true;
                    track.OrigTrackName = trackFile.Name;
                    disc.AddTrack(track);
                }
                fixTracksId(disc);
                discs.Add(disc);
            }
            return discs;
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
				//MSTrack el = (item as MSTrack);
				//if (!el.Exists)
				//{
				//	LVitem.ToolTip = "Track file not exist!\n";
				//}
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
				//else
				//{
				//	return LVitem.Style;
				//}
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
            //string fav_img = (((MainWindow)System.Windows.Application.Current.MainWindow).is_favTrack(track)) ? "fav.png" : "nfav.png";
            string fav_img = "nfav.png";
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
