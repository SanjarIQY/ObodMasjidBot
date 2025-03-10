using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObodMasjidBot.Data
{
    public enum RegistrationState
    {
        NotStarted,
        AwaitingGender,
        AwaitingPhoneNumber,
        Complete
    }

}
