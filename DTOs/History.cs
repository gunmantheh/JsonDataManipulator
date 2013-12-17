using System.ComponentModel;

namespace JsonDataManipulator.DTOs
{
    public class History
    {
        public string status { get; set; } // <status>Queued</status>
        public int id { get; set; } // <id>605</id>
        public string eta { get; set; } // <eta>unknown</eta>
        public int missing { get; set; } // <missing>0</missing>
        public string avg_age { get; set; } // <avg_age>2h</avg_age>
        public string script { get; set; } // <script>sabToAniDB.py</script>
        public int? msgid { get; set; } // <msgid/> TODO verify possible value of the variable
        public string verbosity { get; set; } // <verbosity/> TODO verify possible value of the variable
        public decimal mb { get; set; } // <mb>246.92</mb>
        public string sizeleft { get; set; } // <sizeleft>192 MB</sizeleft>
        public string category { get; set; } // <category>Movies</category>
        public string nzo_id { get; set; } // <nzo_id>SABnzbd_nzo_uiuq_v</nzo_id>
        public string size { get; set; } // <size>247 MB</size> 
        public long downloaded { get; set; } // <downloaded>815878352</downloaded>
        public long download_time { get; set; } // <download_time>567</download_time>
        public string name { get; set; } // <name>Ubuntu</name>
    }
}