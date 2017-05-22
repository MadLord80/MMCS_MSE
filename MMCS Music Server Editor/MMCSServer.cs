using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
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

		//private string DATA_path = "\\DATA";
		//private string CUSTOM_path = "\\CUSTOM";

		//Settings
		private string defALBUM_ID = "0000001";
        //INDEX
        public int cnt_disks_offset = 0x24;
        public int cnt_groups = 100;
        public int groups_offset = 0x28;
        public int group_length = 0x84;
        public int lists_offset;
        public int list_length = 8;
		//ALBUM
		public int max_lists = 100;
		public int lists_size_offset = 0x24;
		public int list_size_length = 4;
		public int alists_offset;
		//first 4 byte in list desc - unknown
		public int a_unknown_length = 4;
		public int listId_length = 4;
		public int listsongs_length = 4;
		public int listName_length = 0x180;
		public int list_desc_length = 0x194;
		public int listName_offset = 0x14;
		public int asong_data_length = 16;
		//ORG_ARRAY
		public int discs_cnt_offset = 4;
		public int discs_cnt_length = 4;
		public int disc_desc_length = 0xe0;
		public int discId_offset = 1;
		public int discId_length = 4;
		public int discName_offset = 12;
		public int discName_length = 0x80;
		public int discArtist_length = 0x40;
		public int disc_songscnt_length = 4;
		//TITLE
		public int max_dtracks = 100;
		public int dtrack_size_offset = 0x24;
		public int dtrack_size_length = 4;
		public int dtracks_offset;
		public int dtId_offset = 4;
		public int songs_cnt_offset = 8;
		public int dtName_offset = 0x1c;
		public int dtName_length = 0x80;
		public int dtNameLoc_length = 0x40;
		public int dtArtist_length = 0x80;
		public int dtArtistLoc_length = 0x40;
		
		public string MainDir
		{
			set { this.main_dir = value; }
		}

        public MMCSServer()
        {
            //INDEX
            lists_offset = groups_offset + cnt_groups * group_length;
			//ALBUM
			alists_offset = lists_size_offset + max_lists * list_size_length;
			//TITLE
			dtracks_offset = dtrack_size_offset + max_dtracks * dtrack_size_length;
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
			string path = main_dir + INFO_path + TITLE_path + TITLE_path + id +  "\\TITLE" + id + "00001.lst";
			return path;
		}

		public string get_TBLdata(uint id)
        {
            string data = "";
            if (id == 171) return "Оригинальные компакт-диски";
            if (id == 172) return "Мои лучшие файлы";
            if (id == 173) return "Часто воспроизводимые файлы";
            if (id == 174) return "Любимые файлы";
            return data;
        }
    }

    class MSGroup : INotifyPropertyChanged
    {
        private int id;
        private string name = "";
		private byte[] name_bytes = new byte[128];
		private ElenmentId[] lists;

		private bool changing = false;
        
        public event PropertyChangedEventHandler PropertyChanged;

		public bool Changing
		{
			get { return this.changing; }
			set
			{
				this.changing = value;
				OnPropertyChanged("Changed");
			}
		}

        public int Id
        {
            get { return this.id; }
            set
            {
                this.id = value;
                OnPropertyChanged("Id");
            }
        }
        public string Name
		{
			get
			{
				if (this.name != "") return this.name;

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
				Array.Resize(ref new_name, this.name_bytes.Length);
				Array.ForEach(this.name_bytes, b => b = 0x00);
				Array.Copy(new_name, 0, this.name_bytes, 0, new_name.Length);
			}
		}
		public ElenmentId[] Lists
        {
            get { return this.lists; }
            set
            {
                this.lists = value;
                OnPropertyChanged("Lists");
            }
        }

		public MSGroup(int gid, byte[] gnbytes, ElenmentId[] glists)
		{
			this.id = gid;
			this.name_bytes = gnbytes;
			this.lists = glists;
			//this.codePage = MMCS_MSE.MMCSServer
		}

		public MSGroup(int gid, string gname, ElenmentId[] glists)
        {
            this.id = gid;
            //\x00 - end string
            int null_offset = gname.IndexOf('\x00');
            this.name = (null_offset != -1) ? gname.Substring(0, null_offset) : gname;
            this.lists = glists;
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
		private string name = "";
		private byte[] name_bytes = new byte[0];
		private MSTrack[] songs;
		private string errors = "";

		public event PropertyChangedEventHandler PropertyChanged;

		public int Id
		{
			get { return this.id; }
			set
			{
				this.id = value;
				OnPropertyChanged("Id");
			}
		}
		public string Name
		{
			get
			{
				if (this.name_bytes.Length == 0) return this.name;

				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				string name = new string(Encoding.GetEncoding(codePage).GetChars(this.name_bytes));
				//\x00 - end string
				int null_offset = name.IndexOf('\x00');
				return (null_offset != -1) ? name.Substring(0, null_offset) : name;
			}
		}
		public MSTrack[] Songs
		{
			get { return this.songs; }
			set
			{
				this.songs = value;
				OnPropertyChanged("Songs");
			}
		}
		public string Errors
		{
			get { return this.errors; }
			set
			{
				this.errors = value;
				OnPropertyChanged("Errors");
			}
		}

		public MSList(int lid, string lname, MSTrack[] lsongs)
		{
			this.id = lid;
			//\x00 - end string
			int null_offset = lname.IndexOf('\x00');
			this.name = (null_offset != -1) ? lname.Substring(0, null_offset) : lname;
			this.songs = lsongs;
		}

		public MSList(int lid, byte[] lname, MSTrack[] lsongs)
		{
			this.id = lid;
			this.name_bytes = lname;
			this.songs = lsongs;
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
		private string name;
		private byte[] name_bytes = new byte[0];
		private string artist;
		private byte[] artist_bytes = new byte[0];
		private int songs_cnt;
		private string errors;

		public event PropertyChangedEventHandler PropertyChanged;
		
		public ElenmentId Id
		{
			get { return this.id; }
			set
			{
				this.id = value;
				OnPropertyChanged("Id");
			}
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
		}
		public int SongsCnt
		{
			get { return this.songs_cnt; }
			set
			{
				this.songs_cnt = value;
				OnPropertyChanged("SongsCnt");
			}
		}
		public string Errors
		{
			get { return this.errors; }
			set
			{
				this.errors = value;
				OnPropertyChanged("Errors");
			}
		}

		public MSDisc(ElenmentId did, byte[] dname, byte[] dartist, int dsongs)
		{
			this.id = did;
			this.name_bytes = dname;
			this.artist_bytes = dartist;
			this.songs_cnt = dsongs;

			this.errors = "";
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
		private ElenmentId disc_id;
		//private string disc_name;
		//private string disc_name_loc;
		private string disc_artist;
		private byte[] disc_artist_bytes = new byte[0];
		//private string disc_artist_loc;
		private int id;
		private string name;
		private byte[] name_bytes = new byte[0];
		private string artist;
		private byte[] artist_bytes = new byte[0];

		public event PropertyChangedEventHandler PropertyChanged;

		public ElenmentId DiskId
		{
			get { return this.disc_id; }
			set
			{
				this.disc_id = value;
				OnPropertyChanged("DiskId");
			}
		}
		public string DiscArtist
		{
			get
			{
				string codePage = ((MainWindow)System.Windows.Application.Current.MainWindow).CodePage;
				string name = new string(Encoding.GetEncoding(codePage).GetChars(this.disc_artist_bytes));
				//\x00 - end string
				int null_offset = name.IndexOf('\x00');
				return (null_offset != -1) ? name.Substring(0, null_offset) : name;
			}
		}
		public int Id
		{
			get { return this.id; }
			set
			{
				this.id = value;
				OnPropertyChanged("Id");
			}
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
		}

		public MSTrack(ElenmentId tdid, byte[] tdartist, int tid, byte[] tname, byte[] tartist)
		{
			this.disc_id = tdid;
			this.id = tid;
			this.disc_artist_bytes = tdartist;
			this.name_bytes = tname;
			this.artist_bytes = tartist;
		}

		protected void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(name));
			}
		}
	}

	class ElenmentId
	{
		private int id;
		private int prefix;

		public int Id
		{
			get { return this.id; }
			set { this.id = value; }
		}
		public int Prefix
		{
			get { return this.prefix; }
			set { this.prefix = value; }
		}
		public string FullId
		{
			get {
				return BitConverter.ToString(new byte[1] { (byte)this.id }) + "0000" + String.Format("{0,2:00}", this.prefix);
			}
		}

		public ElenmentId(int eid, int epr)
		{
			this.id = eid;
			this.prefix = epr;
		}
	}
}
