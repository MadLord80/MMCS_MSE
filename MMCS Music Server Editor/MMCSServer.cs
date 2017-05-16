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
		public int songs_cnt_offset = 0x1bc;
		public int tdiscName_offset = 0x1d0;
		public int tdiscName_length = 0xc0;
		public int tdiscArtist_length = 0xc0;

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

		public string get_TITLEpath(string id)
		{
			string path = main_dir + INFO_path + TITLE_path + TITLE_path + id.ToString() +  "\\TITLE" + id + "00001.lst";
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
        private string name;
		private int[] lists;
        
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
            get { return this.name; }
            set
            {
                //\x00 - end string
                int null_offset = value.IndexOf('\x00');
                this.name = (null_offset != -1) ? value.Substring(0, null_offset) : value;
                OnPropertyChanged("Name");
            }
        }
        public int[] Lists
        {
            get { return this.lists; }
            set
            {
                this.lists = value;
                OnPropertyChanged("Lists");
            }
        }

        public MSGroup(int gid, string gname, int[] glists)
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
		private string name;
		private Dictionary<int, int[]> songs = new Dictionary<int, int[]>();
		private int songs_count;

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
			get { return this.name; }
			set
			{
				//\x00 - end string
				int null_offset = value.IndexOf('\x00');
				this.name = (null_offset != -1) ? value.Substring(0, null_offset) : value;
				OnPropertyChanged("Name");
			}
		}
		public Dictionary<int, int[]> Songs
		{
			get { return this.songs; }
			set
			{
				this.songs = value;
				OnPropertyChanged("Songs");
			}
		}
		public int SongsCnt
		{
			get { return this.songs_count; }
			set
			{
				this.songs_count = value;
				OnPropertyChanged("SongsCnt");
			}
		}

		public MSList(int lid, string lname, Dictionary<int, int[]> lsongs)
		{
			this.id = lid;
			//\x00 - end string
			int null_offset = lname.IndexOf('\x00');
			this.name = (null_offset != -1) ? lname.Substring(0, null_offset) : lname;
			this.songs = lsongs;
			this.songs_count = 0;
			foreach (KeyValuePair<int, int[]> disk in lsongs)
			{
				this.songs_count += disk.Value.Length;
			}
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
		private byte id;
		private string name;
		private string artist;
		private int songs_cnt;

		public event PropertyChangedEventHandler PropertyChanged;

		public byte byteId
		{
			get { return this.id; }
			set
			{
				this.id = value;
				OnPropertyChanged("Id");
			}
		}
		public string Id
		{
			get { return BitConverter.ToString(new byte[1] { this.id }); }
		}
		public string Name
		{
			get { return this.name; }
			set
			{
				//\x00 - end string
				int null_offset = value.IndexOf('\x00');
				this.name = (null_offset != -1) ? value.Substring(0, null_offset) : value;
				OnPropertyChanged("Name");
			}
		}
		public string Artist
		{
			get { return this.artist; }
			set
			{
				//\x00 - end string
				int null_offset = value.IndexOf('\x00');
				this.artist = (null_offset != -1) ? value.Substring(0, null_offset) : value;
				OnPropertyChanged("Artist");
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

		public MSDisc(byte did, string dname, string dartist, int dsongs)
		{
			this.id = did;
			//\x00 - end string
			int null_offset = dname.IndexOf('\x00');
			this.name = (null_offset != -1) ? dname.Substring(0, null_offset) : dname;
			//\x00 - end string
			null_offset = dartist.IndexOf('\x00');
			this.artist = (null_offset != -1) ? dartist.Substring(0, null_offset) : dartist;
			this.songs_cnt = dsongs;
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
		private string name;
		private string artist;

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
			get { return this.name; }
			set
			{
				//\x00 - end string
				int null_offset = value.IndexOf('\x00');
				this.name = (null_offset != -1) ? value.Substring(0, null_offset) : value;
				OnPropertyChanged("Name");
			}
		}
		public string Artist
		{
			get { return this.artist; }
			set
			{
				//\x00 - end string
				int null_offset = value.IndexOf('\x00');
				this.artist = (null_offset != -1) ? value.Substring(0, null_offset) : value;
				OnPropertyChanged("Artist");
			}
		}

		public MSTrack(int tid, string tname, string tartist)
		{
			this.id = tid;
			//\x00 - end string
			int null_offset = tname.IndexOf('\x00');
			this.name = (null_offset != -1) ? tname.Substring(0, null_offset) : tname;
			//\x00 - end string
			null_offset = tartist.IndexOf('\x00');
			this.artist = (null_offset != -1) ? tartist.Substring(0, null_offset) : tartist;
		}

		protected void OnPropertyChanged(string name)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(name));
			}
		}
	}
}
