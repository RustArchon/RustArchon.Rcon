using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustArchon.Rcon.Entities
{
    public class MessageLogBase : EntityBase
    {
        public string Message { get; set; } = string.Empty;
        public int Time { 
            get => _time; 
            set
            {
                _time = value;
            }
        }
        public DateTime DateTime
        {
            get
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(_time);
                return dateTimeOffset.DateTime;
            }
        }
        private int _time { get; set; }
    }
}
