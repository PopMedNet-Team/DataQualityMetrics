using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASPE.DQM.Models
{
    public class PMNErrorResponse
    {
        public IEnumerable<PMNError> Errors { get; set; }
    }

    public class PMNError
    {
        public int? ErrorNumber { get; set; }
        public string Description { get; set; }
        public string ErrorType { get; set; }
    }
}
