using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SaveGamblingContactXAM.InterfaceServices
{
    public interface ISignalRServices
    {
        void StartConnection();

        Task StopConnection();

        bool IsForegroundServiceRunning();
    }
}
