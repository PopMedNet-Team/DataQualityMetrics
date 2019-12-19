using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASPE.DQM.Models
{
    public class VisualizationViewModel
    {
        public Guid ID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string AppID { get; set; }
        public string SheetID { get; set; }
        public bool RequiresAuthentication { get; set; }
        public string EmbedUrl { get; set; }
        public bool Bookmarked { get; set; }
    }
}
