using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanShot.Models
{
    public class Settings
    {
        public static Settings Current { get; set; } = new Settings();

        public string SaveFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\";
        public bool SaveToDisk { get; set; } = true;
        public bool CopyToClipboard { get; set; } = true;
        public bool AlwaysOnTop { get; set; } = true;
        public bool Uninstalled { get; set; } = false;
    }
}
