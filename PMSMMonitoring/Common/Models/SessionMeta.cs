using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Common.Models
{
    [DataContract]
    public class SessionMeta
    {
        [DataMember]
        public double U_q { get; set; }

        [DataMember]
        public double U_d { get; set; }

        [DataMember]
        public double Motor_Speed { get; set; }

        [DataMember]
        public int Profile_Id { get; set; }

        [DataMember]
        public double Ambient { get; set; }

        [DataMember]
        public double Torque { get; set; }
    }
}
