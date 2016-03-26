using ColossalFramework;
using IOperateIt.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace IOperateIt.Manager
{
    public class Mission
    {
        Queue<Vector3> mObjectives;
        Queue<String> mNames;
        ItemClass.Service mVehicleTypeRequired;
        VehicleInfo mVehicleInfo;

        public Mission(List<Vector3> objectives, ItemClass.Service requiredVehicle)
        {
            mObjectives = new Queue<Vector3>();
            foreach (Vector3 objective in objectives)
            {
                mObjectives.Enqueue(objective);
            }
            mVehicleTypeRequired = requiredVehicle;
            mVehicleInfo = Singleton<VehicleManager>.instance.GetRandomVehicleInfo(ref Singleton<SimulationManager>.instance.m_randomizer, mVehicleTypeRequired, ItemClass.SubService.None, ItemClass.Level.None);
        }
    }


    public class MissionManager
    {
        public Mission mission;
        private static MissionManager mInstance;

        public static MissionManager getInstance()
        {
            if (mInstance == null)
            {
                mInstance = new MissionManager();

            }
            return mInstance;
        }

        public ushort GetRandomBuilding(ItemClass buildingType)
        {
            CitizenManager citizenManager = Singleton<CitizenManager>.instance;
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;

            int skip = UnityEngine.Random.Range(0, citizenManager.m_instanceCount - 1);

            for (uint i = 0; i < citizenManager.m_instances.m_buffer.Length; i++)
            {
                if ((citizenManager.m_instances.m_buffer[i].m_flags & (CitizenInstance.Flags.Created | CitizenInstance.Flags.Deleted)) != CitizenInstance.Flags.Created)
                {
                    continue;
                }

                if (skip > 0)
                {
                    skip--;
                    continue;
                }
                Vector3 pos = citizenManager.m_instances.m_buffer[i].GetSmoothPosition((ushort)(i));
                return buildingManager.FindBuilding(pos, 1000f, buildingType.m_service, buildingType.m_subService, Building.Flags.Created, Building.Flags.None);
            }

            //Fallback, find first citizen
            for (uint i = 0; i < citizenManager.m_instances.m_buffer.Length; i++)
            {
                if ((citizenManager.m_instances.m_buffer[i].m_flags & (CitizenInstance.Flags.Created | CitizenInstance.Flags.Deleted)) != CitizenInstance.Flags.Created)
                {
                    continue;
                }

                Vector3 pos = citizenManager.m_instances.m_buffer[i].GetSmoothPosition((ushort)(i));
                return buildingManager.FindBuilding(pos, 1000f, buildingType.m_service, buildingType.m_subService, Building.Flags.Created, Building.Flags.None);
            }

            return 0;
        }

        public Mission generateMission()
        {
            //Mission returnValue = new Mission();
            List<ItemClass.Service> possibleServiceTypes = new List<ItemClass.Service> { ItemClass.Service.Residential, ItemClass.Service.Commercial, ItemClass.Service.Industrial, ItemClass.Service.Office };
            ItemClass.Service serviceType = possibleServiceTypes[UnityEngine.Random.Range(0, possibleServiceTypes.Count)];
            //ushort startBuilding = GetRandomBuilding(serviceType);
            return null;
        }

    }
}