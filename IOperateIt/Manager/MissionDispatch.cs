using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using IOperateIt.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using UnityEngine;

namespace IOperateIt.Manager
{

    public class MissionDispatch : ChirperExtensionBase
    {
        private Timer timer = new Timer();
        private MissionMessage lastMissionMessage;
        private CitizenMessage lastCitizenMessage;

        public override void OnCreated(IChirper threading)
        {
            this.timer.AutoReset = true;
            this.timer.Elapsed += (ElapsedEventHandler)((sender, e) => MaybeGenerateMission());
            this.timer.Interval = 60000.0;
            //this.timer.Start();
            LoggerUtils.Log("Mission Dispatch loaded");
        }

        public override void OnReleased()
        {
            //this.timer.Stop();
            this.timer.Dispose();
        }

        private void MaybeGenerateMission()
        {
            MissionMessage m = new MissionMessage("Test message!", 100);
            Singleton<MessageManager>.instance.QueueMessage(m);
        }

        public override void OnNewMessage(IChirperMessage message)
        {
            if (message is CitizenMessage)
            {
                CitizenMessage cm = message as CitizenMessage;
                ChirpPanel.instance.m_NotificationSound = null;
                lastCitizenMessage = cm;
            }
            else if (message is MissionMessage)
            {
                lastMissionMessage = message as MissionMessage;
            }
        }

        public override void OnUpdate()
        {
            if (lastCitizenMessage == null && lastMissionMessage == null)
                return;

            // This code is roughly based on the work by Juuso "Zuppi" Hietala.
            var container = ChirpPanel.instance.transform.FindChild("Chirps").FindChild("Clipper").FindChild("Container").gameObject.transform;
            for (int i = 0; i < container.childCount; ++i)
            {
                var elem = container.GetChild(i);
                var label = elem.GetComponentInChildren<UILabel>();
                if (lastMissionMessage != null)
                {
                    if (label.text.Equals(lastMissionMessage.GetText()) && string.IsNullOrEmpty(label.stringUserData))
                    {
                        label.stringUserData = lastMissionMessage.mMissionId.ToString();
                        label.eventClick += ClickMissionMessage;

                        lastMissionMessage = null;
                    }
                }
            }
        }

        private void ClickMissionMessage(UIComponent component, UIMouseEventParameter eventParam)
        {
            ItemClass buildingType = ScriptableObject.CreateInstance<ItemClass>();
            buildingType.m_service = ItemClass.Service.Residential;
            buildingType.m_service = ItemClass.Service.None;
            UILabel label = component as UILabel;
            LoggerUtils.Log(label.stringUserData);
            ushort buildingId = MissionManager.getInstance().GetRandomBuilding(buildingType);
            LoggerUtils.Log(Singleton<BuildingManager>.instance.GetBuildingName(buildingId, InstanceID.Empty));
        }

    }
}
