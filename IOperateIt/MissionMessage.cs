using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IOperateIt
{
    class MissionMessage : MessageBase
    {
        public string mAuthor;
        public string mMessage;
        public uint mMissionId;

        public MissionMessage(string message, uint id)
        {
            mMissionId = id;
            mAuthor = "IOperateIt Dispatch";
            mMessage = message;
        }

        public override uint GetSenderID()
        {
            return 0U;
        }

        public override string GetSenderName()
        {
            return this.mAuthor;
        }

        public override string GetText()
        {
            return this.mMessage;
        }

        public override bool IsSimilarMessage(MessageBase other)
        {
            // Don't care, you can't even click on the message anyways
            return false;
        }

        public override void Serialize(ColossalFramework.IO.DataSerializer s)
        {
            s.WriteSharedString(mAuthor);
            s.WriteSharedString(mMessage);
            s.WriteUInt32(mMissionId);
        }

        public override void Deserialize(ColossalFramework.IO.DataSerializer s)
        {
            mAuthor = s.ReadSharedString();
            mMessage = s.ReadSharedString();
            mMissionId = s.ReadUInt32();

        }

        public override void AfterDeserialize(ColossalFramework.IO.DataSerializer s)
        {
        }
    }
}
