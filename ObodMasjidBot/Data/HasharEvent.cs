using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObodMasjidBot.Data
{
    class HasharEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // Generate a unique ID
        public DateTime Date { get; set; }
        public string Time { get; set; }
        public string Masjid { get; set; }
    }
}
