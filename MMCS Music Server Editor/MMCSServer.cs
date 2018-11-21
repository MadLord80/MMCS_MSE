using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace MMCS_MSE
{
	class MMCSServer
	{
		//paths
		private string main_dir;
		private string INFO_path = "\\INFO";
		private string ALBUM_path = "\\ALBUM";
		private string TITLE_path = "\\TITLE";
		private string DATA_path = "\\DATA";
		//private string CUSTOM_path = "\\CUSTOM";		

		//Settings
		private string defALBUM_ID = "0000001";

		public int NameDesc_length = 0x80;
		public int NameLocDesc_length = 0x40;
		//INDEX
		public int header_size = 40;
		public int cnt_disks_offset = 0x24;
		public int max_groups = 100;
		public int groups_offset = 0x28;
		public int group_length = 0x84;
		public int groupName_offset = 4;
		//public int groupName_length = 0x80;
		public int lists_offset;
		public int list_length = 8;
		//ALBUM
		public int max_lists = 100;
		public int lists_size_offset = 0x24;
		public int list_size_length = 4;
		public int alists_offset;
		public int fav_listId = 1;
		//first 4 byte in list desc - unknown
		public int a_unknown_length = 4;
		public int listId_length = 4;
		public int listsongs_length = 4;
		public int listName_length = 0x180;
		public int list_desc_length = 0x194;
		public int listName_offset = 0x14;
		public int asong_data_length = 16;
		//ORG_ARRAY
		public int org_header_size = 8;
		public int org_discdata_size = 224;
		public int discs_cnt_offset = 4;
		public int discs_cnt_length = 4;
		public int disc_desc_length = 0xe0;
		public int discId_offset = 1;
		public int discId_length = 4;
		public int discName_offset = 12;
		//public int discName_length = 0x80;
		//public int discNameLoc_length = 0x40;
		public int disc_songscnt_length = 4;
		//public int disc_enddesc_length = 16;
		//TITLE
		public int max_dtracks = 100;
		public int dtrack_size_offset = 0x24;
		public int dtrack_size_length = 4;
		public int dtracks_offset;
		public int dtId_offset = 4;
		public int songs_cnt_offset = 8;
		public int dtName_offset = 0x1c;
		//public int dtName_length = 0x80;
		//public int dtNameLoc_length = 0x40;
		//public int dtArtist_length = 0x80;
		//public int dtArtistLoc_length = 0x40;

		//for report
		public byte[] INDEXstart = new byte[4];
		public byte[] ALBUMstart = new byte[4];

		public string MainDir
		{
			set { this.main_dir = value; }
			get { return this.main_dir; }
		}

		public MMCSServer()
		{
			//INDEX
			lists_offset = groups_offset + max_groups * group_length;
			//ALBUM
			alists_offset = lists_size_offset + max_lists * list_size_length;
			//TITLE
			dtracks_offset = dtrack_size_offset + max_dtracks * dtrack_size_length;
		}

		public string get_DATApath()
		{
			string path = main_dir + DATA_path;
			return path;
		}

		public string get_ALBUMpath()
		{
			string path = main_dir + INFO_path + ALBUM_path + ALBUM_path + defALBUM_ID + "\\ALBUM" + defALBUM_ID + ".lst";
			return path;
		}

		public string get_INDEXpath()
		{
			string path = main_dir + INFO_path + "\\INDEX.lst";
			return path;
		}

		public string get_ORGpath()
		{
			string path = main_dir + INFO_path + "\\ORG_ARRAY";
			return path;
		}

		public string get_TITLEpath(ElenmentId disc)
		{
			string id = BitConverter.ToString(new byte[1] { (byte)disc.Id });
			string path = main_dir + INFO_path + TITLE_path + TITLE_path + id + "\\TITLE" + id + "00001.lst";
			return path;
		}

		public string get_SCpath(ElenmentId disc, int track_id)
		{
			string disc_id = String.Format("{0,2:00}", BitConverter.ToString(new byte[1] { (byte)disc.Id }));
			string track_file = String.Format("{0,3:000}.sc", track_id);
			string path = main_dir + DATA_path + DATA_path + disc_id + "\\" + disc.FullId + "\\" + track_file;
			return path;
		}

		public string get_TBLdata(uint id)
		{
			string data = "";
			if (id == 171) return "[171] Оригинальные компакт-диски";
			if (id == 172) return "[172] Мои лучшие файлы";
			if (id == 173) return "[173] Часто воспроизводимые файлы";
			if (id == 174) return "[174] Любимые файлы";
			return data;
		}
	}

	class MSGroup : INotifyPropertyChanged
	{
		private int id;
		private byte[] name_bytes = new byte[128];
		private List<MSList> lists = new List<MSList>();
		private List<MSDisc> discs = new List<MSDisc>();
		private bool name_changed = false;
		private bool added = false;
		private bool deleted = false;

		public event PropertyChangedEventHandler PropertyChanged;

		public bool NameChanged
		{
			get { return this.name_changed; }
			set { this.name_changed = value; }
		}
		public bool Added
		{
			get { return this.added; }
			set { this.added = value; }
		}
		public bool Deleted
		{
			get { return this.deleted; }
			set { this.deleted = value; }
		}

		public int Id
		{
			get { return this.id; }
			set { this.id = value; }
		}
		public string Name
		{
			get
			{
				string name = "";
				//←[tbl:NNN]
				if (this.name_bytes[0] == 0x1b && this.name_bytes[1] == 0x5b)
				{
					string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
					uint str_id = Convert.ToUInt32(Encoding.GetEncoding(codePage).GetString(new byte[3] { this.name_bytes[6], this.name_bytes[7], this.name_bytes[8] }));
					MMCSServer ms = new MMCSServer();
					name = ms.get_TBLdata(str_id);
				}
				else
				{
					string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
					name = new string(Encoding.GetEncoding(codePage).GetChars(this.name_bytes));
					//\x00 - end string
					int null_offset = name.IndexOf('\x00');
					name = (null_offset != -1) ? name.Substring(0, null_offset) : name;
				}
				return name;
			}
			set
			{
				if (this.Name == value) return;

				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				byte[] new_name = Encoding.GetEncoding(codePage).GetBytes(value);
				if (new_name.Length > this.name_bytes.Length) Array.Resize(ref new_name, this.name_bytes.Length);
				this.name_bytes = Enumerable.Repeat((byte)0x00, this.name_bytes.Length).ToArray();
				Array.Copy(new_name, 0, this.name_bytes, 0, new_name.Length);
				this.name_changed = true;
				OnPropertyChanged("Name");
			}
		}
		public byte[] NameBytes
		{
			get { return this.name_bytes; }
		}
		public List<MSList> Lists
		{
			get { return this.lists; }
		}
		public List<MSDisc> Discs
		{
			get { return this.discs; }
		}
		public int Items
		{
			get
			{
				return (this.discs.Count > 0) ? this.discs.Count : this.lists.Count;
			}
		}

		public MSGroup(int gid, byte[] gnbytes)
		{
			this.id = gid;
			this.name_bytes = gnbytes;
		}
		public MSGroup(int gid, byte[] gnbytes, List<MSList> glists)
		{
			this.id = gid;
			this.name_bytes = gnbytes;
			this.lists = glists;
		}
		public MSGroup(int gid, byte[] gnbytes, List<MSDisc> gdiscs)
		{
			this.id = gid;
			this.name_bytes = gnbytes;
			this.discs = gdiscs;
		}
		public MSGroup(int gid, byte[] gnbytes, MSList glist)
		{
			this.id = gid;
			this.name_bytes = gnbytes;
			this.lists.Add(glist);
		}
		public MSGroup(int gid, byte[] gnbytes, MSDisc gdisc)
		{
			this.id = gid;
			this.name_bytes = gnbytes;
			this.discs.Add(gdisc);
		}

		protected void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(name));
			}
		}
	}

	class MSList : INotifyPropertyChanged
	{
		private int id;
		private byte[] name_bytes = new byte[384];
		private List<MSTrack> songs = new List<MSTrack>();
		private string errors = "";
		//for report
		private byte[] lstart = new byte[4];
		private byte lid_cnt = new byte();
		private byte[] ldelimeter = new byte[4];
		private byte[] lcode = new byte[4];

		public event PropertyChangedEventHandler PropertyChanged;

		public byte[] LStart
		{
			get { return this.lstart; }
			set { this.lstart = value; }
		}
		public byte LId_cnt
		{
			get { return this.lid_cnt; }
			set { this.lid_cnt = value; }
		}
		public byte[] LDelim
		{
			get { return this.ldelimeter; }
			set { this.ldelimeter = value; }
		}
		public byte[] LCode
		{
			get { return this.lcode; }
			set { this.lcode = value; }
		}

		public int Id
		{
			get { return this.id; }
		}
		public string Name
		{
			get
			{
				string name = "";
				//←[tbl:NNN]
				if (this.name_bytes[0] == 0x1b && this.name_bytes[1] == 0x5b)
				{
					string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
					uint str_id = Convert.ToUInt32(Encoding.GetEncoding(codePage).GetString(new byte[3] { this.name_bytes[6], this.name_bytes[7], this.name_bytes[8] }));
					MMCSServer ms = new MMCSServer();
					name = ms.get_TBLdata(str_id);
				}
				else
				{
					string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
					name = new string(Encoding.GetEncoding(codePage).GetChars(this.name_bytes));
					//\x00 - end string
					int null_offset = name.IndexOf('\x00');
					name = (null_offset != -1) ? name.Substring(0, null_offset) : name;
				}
				return name;
			}
			set
			{
				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				byte[] new_name = Encoding.GetEncoding(codePage).GetBytes(value);
				if (new_name.Length > this.name_bytes.Length) Array.Resize(ref new_name, this.name_bytes.Length);
				this.name_bytes = Enumerable.Repeat((byte)0x00, this.name_bytes.Length).ToArray();
				Array.Copy(new_name, 0, this.name_bytes, 0, new_name.Length);
				OnPropertyChanged("Name");
			}
		}
		public List<MSTrack> Songs
		{
			get { return this.songs; }
		}
		public string Errors
		{
			get { return this.errors; }
			set
			{
				this.errors = value;
				//OnPropertyChanged("Errors");
			}
		}

		public MSList(int lid, byte[] lname)
		{
			this.id = lid;

			this.name_bytes = Enumerable.Repeat((byte)0x00, this.name_bytes.Length).ToArray();
			if (lname.Length > this.name_bytes.Length) Array.Resize(ref lname, this.name_bytes.Length);
			Array.Copy(lname, 0, this.name_bytes, 0, lname.Length);
		}
		public MSList(int lid, byte[] lname, List<MSTrack> lsongs)
		{
			this.id = lid;

			this.name_bytes = Enumerable.Repeat((byte)0x00, this.name_bytes.Length).ToArray();
			if (lname.Length > this.name_bytes.Length) Array.Resize(ref lname, this.name_bytes.Length);
			Array.Copy(lname, 0, this.name_bytes, 0, lname.Length);

			this.songs = lsongs;
		}
		public MSList(int lid, byte[] lname, MSTrack lsong)
		{
			this.id = lid;

			this.name_bytes = Enumerable.Repeat((byte)0x00, this.name_bytes.Length).ToArray();
			if (lname.Length > this.name_bytes.Length) Array.Resize(ref lname, this.name_bytes.Length);
			Array.Copy(lname, 0, this.name_bytes, 0, lname.Length);

			this.songs.Add(lsong);
		}

		protected void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(name));
			}
		}
	}

	class MSDisc : INotifyPropertyChanged
	{
		private ElenmentId id;
		private TrackDiscDesc name;
		private TrackDiscDesc artist;
		//private byte[] name_bytes = new byte[80];
		//private byte[] nameLoc_bytes = new byte[40];
		//private byte[] artist_bytes = new byte[80];
		//private byte[] artistLoc_bytes = new byte[40];
		private List<MSTrack> tracks = new List<MSTrack>();
		private string errors = "";
		//for report
		private byte[] end_desc = new byte[16];
		private byte[] st_title = new byte[4];
		private byte[] title = new byte[16];

		public event PropertyChangedEventHandler PropertyChanged;

		public byte[] EndDesc
		{
			get { return this.end_desc; }
			set { this.end_desc = value; }
		}
		public byte[] StartTitle
		{
			get { return this.st_title; }
			set { this.st_title = value; }
		}
		public byte[] Title
		{
			get { return this.title; }
			set { this.title = value; }
		}

		public ElenmentId Id
		{
			get { return this.id; }
		}
		public string Name
		{
			get
			{
				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				string name = new string(Encoding.GetEncoding(codePage).GetChars(this.name_bytes));
				//\x00 - end string
				int null_offset = name.IndexOf('\x00');
				return (null_offset != -1) ? name.Substring(0, null_offset) : name;
			}
			set
			{
				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				byte[] new_name = Encoding.GetEncoding(codePage).GetBytes(value);
				if (new_name.Length > this.name_bytes.Length) Array.Resize(ref new_name, this.name_bytes.Length);
				this.name_bytes = Enumerable.Repeat((byte)0x00, this.name_bytes.Length).ToArray();
				Array.Copy(new_name, 0, this.name_bytes, 0, new_name.Length);
				OnPropertyChanged("Name");
			}
		}
		public string NameLoc
		{
			get
			{
				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				string name = new string(Encoding.GetEncoding(codePage).GetChars(this.nameLoc_bytes));
				//\x00 - end string
				int null_offset = name.IndexOf('\x00');
				return (null_offset != -1) ? name.Substring(0, null_offset) : name;
			}
			set
			{
				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				byte[] new_nameLoc = Encoding.GetEncoding(codePage).GetBytes(value);
				if (new_nameLoc.Length > this.nameLoc_bytes.Length) Array.Resize(ref new_nameLoc, this.nameLoc_bytes.Length);
				this.nameLoc_bytes = Enumerable.Repeat((byte)0x00, this.nameLoc_bytes.Length).ToArray();
				Array.Copy(new_nameLoc, 0, this.nameLoc_bytes, 0, new_nameLoc.Length);
				OnPropertyChanged("NameLoc");
			}
		}
		public string Artist
		{
			get
			{
				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				string name = new string(Encoding.GetEncoding(codePage).GetChars(this.artist_bytes));
				//\x00 - end string
				int null_offset = name.IndexOf('\x00');
				return (null_offset != -1) ? name.Substring(0, null_offset) : name;
			}
			set
			{
				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				byte[] new_artist = Encoding.GetEncoding(codePage).GetBytes(value);
				if (new_artist.Length > this.artist_bytes.Length) Array.Resize(ref new_artist, this.artist_bytes.Length);
				this.artist_bytes = Enumerable.Repeat((byte)0x00, this.artist_bytes.Length).ToArray();
				Array.Copy(new_artist, 0, this.artist_bytes, 0, new_artist.Length);
				OnPropertyChanged("Artist");
			}
		}
		public string ArtistLoc
		{
			get
			{
				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				string name = new string(Encoding.GetEncoding(codePage).GetChars(this.artistLoc_bytes));
				//\x00 - end string
				int null_offset = name.IndexOf('\x00');
				return (null_offset != -1) ? name.Substring(0, null_offset) : name;
			}
			set
			{
				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				byte[] new_artistLoc = Encoding.GetEncoding(codePage).GetBytes(value);
				if (new_artistLoc.Length > this.artistLoc_bytes.Length) Array.Resize(ref new_artistLoc, this.artistLoc_bytes.Length);
				this.artistLoc_bytes = Enumerable.Repeat((byte)0x00, this.artistLoc_bytes.Length).ToArray();
				Array.Copy(new_artistLoc, 0, this.artistLoc_bytes, 0, new_artistLoc.Length);
				OnPropertyChanged("ArtistLoc");
			}
		}
		public List<MSTrack> Tracks
		{
			get { return this.tracks; }
		}
		public string Errors
		{
			get { return this.errors; }
			set
			{
				this.errors += value + "\n";
				//OnPropertyChanged("Errors");
			}
		}

		public MSDisc(ElenmentId did, byte[] dname)
		{
			this.id = did;

			this.name_bytes = Enumerable.Repeat((byte)0x00, this.name_bytes.Length).ToArray();
			if (dname.Length > this.name_bytes.Length) Array.Resize(ref dname, this.name_bytes.Length);
			Array.Copy(dname, 0, this.name_bytes, 0, dname.Length);
		}
		public MSDisc(ElenmentId did, byte[] dname, byte[] dartist)
		{
			this.id = did;

			this.name_bytes = Enumerable.Repeat((byte)0x00, this.name_bytes.Length).ToArray();
			if (dname.Length > this.name_bytes.Length) Array.Resize(ref dname, this.name_bytes.Length);
			Array.Copy(dname, 0, this.name_bytes, 0, dname.Length);

			this.artist_bytes = Enumerable.Repeat((byte)0x00, this.artist_bytes.Length).ToArray();
			if (dartist.Length > this.artist_bytes.Length) Array.Resize(ref dartist, this.artist_bytes.Length);
			Array.Copy(dartist, 0, this.artist_bytes, 0, dartist.Length);
		}
		public MSDisc(ElenmentId did, byte[] dname, List<MSTrack> dtracks)
		{
			this.id = did;

			this.name_bytes = Enumerable.Repeat((byte)0x00, this.name_bytes.Length).ToArray();
			if (dname.Length > this.name_bytes.Length) Array.Resize(ref dname, this.name_bytes.Length);
			Array.Copy(dname, 0, this.name_bytes, 0, dname.Length);

			this.tracks = dtracks;
		}
		public MSDisc(ElenmentId did, byte[] dname, MSTrack dtrack)
		{
			this.id = did;

			this.name_bytes = Enumerable.Repeat((byte)0x00, this.name_bytes.Length).ToArray();
			if (dname.Length > this.name_bytes.Length) Array.Resize(ref dname, this.name_bytes.Length);
			Array.Copy(dname, 0, this.name_bytes, 0, dname.Length);

			this.tracks.Add(dtrack);
		}
		public MSDisc(ElenmentId did, byte[] dname, byte[] dnameLoc, List<MSTrack> dtracks)
		{
			this.id = did;

			this.name_bytes = Enumerable.Repeat((byte)0x00, this.name_bytes.Length).ToArray();
			if (dname.Length > this.name_bytes.Length) Array.Resize(ref dname, this.name_bytes.Length);
			Array.Copy(dname, 0, this.name_bytes, 0, dname.Length);
			this.nameLoc_bytes = Enumerable.Repeat((byte)0x00, this.nameLoc_bytes.Length).ToArray();
			if (dnameLoc.Length > this.nameLoc_bytes.Length) Array.Resize(ref dnameLoc, this.nameLoc_bytes.Length);
			Array.Copy(dnameLoc, 0, this.nameLoc_bytes, 0, dnameLoc.Length);

			this.tracks = dtracks;
		}

		protected void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(name));
			}
		}
	}

	class MSTrack : INotifyPropertyChanged
	{
		private int id;
		private byte[] name_bytes = new byte[80];
		private byte[] nameLoc_bytes = new byte[40];
		private byte[] artist_bytes = new byte[80];
		private byte[] artistLoc_bytes = new byte[40];
		private bool is_exist = true;
		private bool name_changed = false;
		private bool added = false;
		private bool deleted = false;
		//for report
		//private byte[] ldelim = new byte[8];

		public event PropertyChangedEventHandler PropertyChanged;

		public bool NameChanged
		{
			get { return this.name_changed; }
			set { this.name_changed = value; }
		}
		public bool Added
		{
			get { return this.added; }
			set { this.added = value; }
		}
		public bool Deleted
		{
			get { return this.deleted; }
			set { this.deleted = value; }
		}

		//public byte[] ListDelim
		//{
		//	get { return this.ldelim; }
		//	set { this.ldelim = value; }
		//}

		public int Id
		{
			get { return this.id; }
		}

		public string File
		{
			get { return String.Format("{0,3:000}.sc", this.id); }
		}

		public string Name
		{
			get
			{
				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				string name = new string(Encoding.GetEncoding(codePage).GetChars(this.name_bytes));
				//\x00 - end string
				int null_offset = name.IndexOf('\x00');
				return (null_offset != -1) ? name.Substring(0, null_offset) : name;
			}
			set
			{
				if (this.Name == value) return;

				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				byte[] new_name = Encoding.GetEncoding(codePage).GetBytes(value);
				if (new_name.Length > this.name_bytes.Length) Array.Resize(ref new_name, this.name_bytes.Length);
				this.name_bytes = Enumerable.Repeat((byte)0x00, this.name_bytes.Length).ToArray();
				Array.Copy(new_name, 0, this.name_bytes, 0, new_name.Length);
				this.name_changed = true;
				OnPropertyChanged("Name");
			}
		}
		public byte[] NameBytes
		{
			get { return this.name_bytes; }
		}
		public string Artist
		{
			get
			{
				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				string name = new string(Encoding.GetEncoding(codePage).GetChars(this.artist_bytes));
				//\x00 - end string
				int null_offset = name.IndexOf('\x00');
				return (null_offset != -1) ? name.Substring(0, null_offset) : name;
			}
			set
			{
				if (this.Artist == value) return;

				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				byte[] new_artist = Encoding.GetEncoding(codePage).GetBytes(value);
				if (new_artist.Length > this.artist_bytes.Length) Array.Resize(ref new_artist, this.artist_bytes.Length);
				this.artist_bytes = Enumerable.Repeat((byte)0x00, this.artist_bytes.Length).ToArray();
				Array.Copy(new_artist, 0, this.artist_bytes, 0, new_artist.Length);
				this.name_changed = true;
				OnPropertyChanged("Artist");
			}
		}
		public byte[] ArtistBytes
		{
			get { return this.artist_bytes; }
		}
		public string NameLoc
		{
			get
			{
				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				string name = new string(Encoding.GetEncoding(codePage).GetChars(this.nameLoc_bytes));
				//\x00 - end string
				int null_offset = name.IndexOf('\x00');
				return (null_offset != -1) ? name.Substring(0, null_offset) : name;
			}
			set
			{
				if (this.NameLoc == value) return;

				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				byte[] new_nameLoc = Encoding.GetEncoding(codePage).GetBytes(value);
				if (new_nameLoc.Length > this.nameLoc_bytes.Length) Array.Resize(ref new_nameLoc, this.nameLoc_bytes.Length);
				this.nameLoc_bytes = Enumerable.Repeat((byte)0x00, this.nameLoc_bytes.Length).ToArray();
				Array.Copy(new_nameLoc, 0, this.nameLoc_bytes, 0, new_nameLoc.Length);
				this.name_changed = true;
				OnPropertyChanged("NameLoc");
			}
		}
		public string ArtistLoc
		{
			get
			{
				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				string name = new string(Encoding.GetEncoding(codePage).GetChars(this.artistLoc_bytes));
				//\x00 - end string
				int null_offset = name.IndexOf('\x00');
				return (null_offset != -1) ? name.Substring(0, null_offset) : name;
			}
			set
			{
				if (this.ArtistLoc == value) return;

				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				byte[] new_artistLoc = Encoding.GetEncoding(codePage).GetBytes(value);
				if (new_artistLoc.Length > this.artistLoc_bytes.Length) Array.Resize(ref new_artistLoc, this.artistLoc_bytes.Length);
				this.artistLoc_bytes = Enumerable.Repeat((byte)0x00, this.artistLoc_bytes.Length).ToArray();
				Array.Copy(new_artistLoc, 0, this.artistLoc_bytes, 0, new_artistLoc.Length);
				this.name_changed = true;
				OnPropertyChanged("ArtistLoc");
			}
		}
		public bool Exists
		{
			get { return this.is_exist; }
			set { this.is_exist = value; }
		}

		public MSTrack(int tid, byte[] tname, byte[] tartist)
		{
			this.id = tid;

			this.name_bytes = Enumerable.Repeat((byte)0x00, this.name_bytes.Length).ToArray();
			if (tname.Length > this.name_bytes.Length) Array.Resize(ref tname, this.name_bytes.Length);
			Array.Copy(tname, 0, this.name_bytes, 0, tname.Length);

			this.artist_bytes = Enumerable.Repeat((byte)0x00, this.artist_bytes.Length).ToArray();
			if (tartist.Length > this.artist_bytes.Length) Array.Resize(ref tartist, this.artist_bytes.Length);
			Array.Copy(tartist, 0, this.artist_bytes, 0, tartist.Length);
		}
		public MSTrack(int tid, byte[] tname, byte[] tnameLoc, byte[] tartist, byte[] tartistLoc)
		{
			this.id = tid;

			this.name_bytes = Enumerable.Repeat((byte)0x00, this.name_bytes.Length).ToArray();
			if (tname.Length > this.name_bytes.Length) Array.Resize(ref tname, this.name_bytes.Length);
			Array.Copy(tname, 0, this.name_bytes, 0, tname.Length);
			this.nameLoc_bytes = Enumerable.Repeat((byte)0x00, this.nameLoc_bytes.Length).ToArray();
			if (tnameLoc.Length > this.nameLoc_bytes.Length) Array.Resize(ref tnameLoc, this.nameLoc_bytes.Length);
			Array.Copy(tnameLoc, 0, this.nameLoc_bytes, 0, tnameLoc.Length);

			this.artist_bytes = Enumerable.Repeat((byte)0x00, this.artist_bytes.Length).ToArray();
			if (tartist.Length > this.artist_bytes.Length) Array.Resize(ref tartist, this.artist_bytes.Length);
			Array.Copy(tartist, 0, this.artist_bytes, 0, tartist.Length);
			this.artistLoc_bytes = Enumerable.Repeat((byte)0x00, this.artistLoc_bytes.Length).ToArray();
			if (tartistLoc.Length > this.artistLoc_bytes.Length) Array.Resize(ref tartistLoc, this.artistLoc_bytes.Length);
			Array.Copy(tartistLoc, 0, this.artistLoc_bytes, 0, tartistLoc.Length);
		}

		protected void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(name));
			}
		}
	}

	//class MSFactTrack
	//{
	//	private help_functions hf = new help_functions();

	//	private List<int> id = new List<int> { };
	//	private ElenmentId discid;

	//	public MSFactTrack(string discid)
	//	{
	//		this.discid = new ElenmentId(
	//			(int)hf.HexStringToByteArray(discid.Substring(0, 2))[0],
	//			(int)hf.HexStringToByteArray(discid.Substring(discid.Length - 2))[0]
	//		);
	//	}

	//	public void AddId (string id)
	//	{
	//		this.id.Add(Convert.ToInt32(id));
	//	}
	//}

	class ElenmentId
	{
		private int id;
		private int prefix;

		public int Id
		{
			get { return this.id; }
		}
		public int Prefix
		{
			get { return this.prefix; }
		}
		public string FullId
		{
			get
			{
				return
					BitConverter.ToString(new byte[1] { (byte)this.id })
					+ "0000"
					+ BitConverter.ToString(new byte[1] { (byte)this.prefix });
			}
		}

		public ElenmentId(int eid, int eprefix)
		{
			this.id = eid;
			this.prefix = eprefix;
		}
	}

	class TrackDiscDesc
	{
		private byte[] name = new byte[0x80];
		private byte[] nameLoc = new byte[0x40];

		public TrackDiscDesc(byte[] name, byte[] nameLoc)
		{
			this.name = name;
			this.nameLoc = nameLoc;
		}
	}
}
