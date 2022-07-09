using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanShot.Models
{
    public class SavedFile
    {
        public string ContentDisposition { get; set; }

        public string ContentType { get; set; }

        public string FileName { get; set; }

        public long FileSize { get; set; }
        public Guid Id { get; set; }
        public DateTimeOffset UploadedAt { get; set; }
    }
}
