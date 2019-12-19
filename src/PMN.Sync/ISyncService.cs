using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PMN.Sync
{
    public interface ISyncService
    {
        Task Run();

        Task Stop();
    }
}
