using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ASPE.DQM.Model
{
    public class Visualization : EntityWithID
    {
        public Visualization()
        {
            RequiresAuth = false;
            Published = false;
        }

        public string Title { get; set; }
        public string Description { get; set; }
        public string AppID { get; set; }
        public string SheetID { get; set; }
        public bool RequiresAuth { get; set; }
        public bool Published { get; set; }
    }
}
