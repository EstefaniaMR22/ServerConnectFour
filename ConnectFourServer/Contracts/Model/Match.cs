using Services;
using Services.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Model
{
    [DataContract]
    public class Match
    {
        [DataMember]
        public int GameID { get; set; }

        [DataMember]
        public int WinnerID { get; set; }

        [DataMember]
        public DateTime Start { get; set; }

        [DataMember]
        public DateTime End { get; set; }

        [DataMember]
        public Dictionary<ColorToken, IMatchBoardMgtServiceCallback> tokens { get; set; }
    }
}
