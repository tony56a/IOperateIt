using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IOperateIt.Configs
{
    class MissionType
    {
        ItemClass.Service srcService;
        List<ItemClass.Service> dstService;
        TransportLine transportLine;
        string missionName;
        string missionDescription;
        public MissionType( ItemClass.Service srcService, List<ItemClass.Service> dstService, string missionName, string missionDescription)
        {
            this.srcService = srcService;
            this.dstService = dstService;
            this.missionName = missionName;
            this.missionDescription = missionDescription;
        }
    }
    class MissionTypes
    {
        public static readonly List<MissionType> MISSION_DEFAULTS = new List<MissionType>
        {
            new MissionType(ItemClass.Service.Residential, new List<ItemClass.Service> { ItemClass.Service.Commercial }, "Shopping Trip","Gotta run some errands, get to {0}!"),
            new MissionType(ItemClass.Service.Residential, new List<ItemClass.Service> { ItemClass.Service.Industrial }, "Plain ol' commute","Gotta get to work at {0}!"),
            new MissionType(ItemClass.Service.Residential, new List<ItemClass.Service> { ItemClass.Service.Office }, "Plain ol' commute","Gotta get to work at {0}!"),
            new MissionType(ItemClass.Service.Industrial, new List<ItemClass.Service> { ItemClass.Service.Commercial }, "Delivery","We gotta get these widgets to {0}!"),

        };
    }
}
