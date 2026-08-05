using System;
using System.Collections.Generic;

namespace Luminang.UI.Announcements
{
    public enum AnnouncementType
    {
        System,
        Update,
        Maintenance
    }

    public enum AnnouncementState
    {
        Unread,
        Read,
        Archived
    }

    [Serializable]
    public class AnnouncementModel
    {
        public string Id;               // user_notifications.id (used for DB updates)
        public string NotificationId;   // admin_notifications.id (the source content)
        public AnnouncementType Type;
        public string Title;
        public string Details;
        public string DateString;       // ISO 8601 string from JSON/Supabase
        public AnnouncementState State;
        public int AttachedCoins;       // 0 if no reward
        public bool IsClaimed;          // true if the coin reward was already collected

        public DateTime ParsedDate
        {
            get
            {
                if (DateTime.TryParse(DateString, out DateTime result))
                    return result;
                return DateTime.MinValue;
            }
        }
    }

    [Serializable]
    public class AnnouncementDataList
    {
        public List<AnnouncementModel> announcements;
    }
}
