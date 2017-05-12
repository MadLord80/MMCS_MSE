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
		private string DISCID_path, HIST_path, RECORD_path, TITLE_path;

		private string DATA_path = "\\DATA";
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
		//first 4 byte in list desc
		public int a_unknown_length = 4;
		public int listId_length = 4;
		public int listsongs_length = 4;
		public int listName_offset = 0x14;
		public int listName_length = 0x180;

		public string MainDir
		{
			set { this.main_dir = value; }
		}

        public MMCSServer()
        {
            DISCID_path = INFO_path + "\\DISCID";
            HIST_path = INFO_path + "\\HIST";
            RECORD_path = INFO_path + "\\RECORD";
            TITLE_path = INFO_path + "\\TITLE";

            //INDEX
            lists_offset = groups_offset + cnt_groups * group_length;
			//ALBUM
			alists_offset = lists_offset + max_lists * list_size_length;
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
        private uint id;
        private string name;
		private uint[] lists;
        
        public event PropertyChangedEventHandler PropertyChanged;

        public uint Id
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
        public uint[] Lists
        {
            get { return this.lists; }
            set
            {
                this.lists = value;
                OnPropertyChanged("Lists");
            }
        }

        public MSGroup(uint gid, string gname, uint[] glists)
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
		private uint id;
		private string name;
		private uint songs_cnt;
		private byte[,] songs;

		public event PropertyChangedEventHandler PropertyChanged;

		public uint Id
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
		public uint SongsCnt
		{
			get { return this.songs_cnt; }
			set
			{
				this.songs_cnt = value;
				OnPropertyChanged("SongsCnt");
			}
		}
		public byte[,] Songs
		{
			get { return this.songs; }
			set
			{
				this.songs = value;
				OnPropertyChanged("Songs");
			}
		}

		public MSList(uint lid, string lname, uint lscnt, byte[,] lsongs)
		{
			this.id = lid;
			//\x00 - end string
			int null_offset = lname.IndexOf('\x00');
			this.name = (null_offset != -1) ? lname.Substring(0, null_offset) : lname;
			this.songs_cnt = lscnt;
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
}
