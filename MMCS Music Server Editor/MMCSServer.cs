using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MMCS_MSE
{
    class MMCSServer
    {
        //paths
        private string main_dir;
        public string INFO_path = "\\INFO";
        public string ALBUM_path, DISCID_path, HIST_path, RECORD_path, TITLE_path;

        public string DATA_path = "\\DATA";
        //private string CUSTOM_path = "\\CUSTOM";

        //Settings
        public string defALBUM_ID = "0000001";
        //INDEX
        public int cnt_disks_offset = 0x24;
        public int cnt_groups = 100;
        public int groups_offset = 0x28;
        public int groups_length = 0x84;

        public MMCSServer(string dir)
        {
            main_dir = dir;

            ALBUM_path = INFO_path + "\\ALBUM";
            DISCID_path = INFO_path + "\\DISCID";
            HIST_path = INFO_path + "\\HIST";
            RECORD_path = INFO_path + "\\RECORD";
            TITLE_path = INFO_path + "\\TITLE";
        }

        public string get_ALBUMpath()
        {
            string path = main_dir + INFO_path + ALBUM_path + defALBUM_ID + "\\ALBUM" + defALBUM_ID + ".lst";
            return path;
        }

        public string get_INDEXpath()
        {
            string path = main_dir + INFO_path + "\\INDEX.lst";
            return path;
        }
    }
}
