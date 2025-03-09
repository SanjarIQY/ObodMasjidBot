﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObodMasjidBot.Data
{
    class UserState
    {
        public RegistrationState RegistrationState { get; set; } = RegistrationState.NotStarted;
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
        public bool HasSeenWelcome { get; set; } = false;
        public bool IsSubscribed { get; set; } = false;
    }
}
