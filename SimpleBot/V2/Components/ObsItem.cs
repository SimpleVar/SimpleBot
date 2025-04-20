using Newtonsoft.Json.Linq;

namespace SimpleBot.v2
{
    class ObsItem
    {
        public readonly string SceneName;
        public readonly string ItemName;
        private int? _itemId;
        public int ItemId
        {
            get
            {
                if (_itemId == null)
                {
                    try
                    {
                        _itemId = Bot._obs?.GetSceneItemId(SceneName, ItemName, 0);
                    }
                    catch { }
                }
                return _itemId ?? -1;
            }
        }

        public ObsItem(string itemName, string sceneName)
        {
            SceneName = sceneName;
            ItemName = itemName;
        }

        public bool IsVisible
        {
            get
            {
                if (Bot._obs != null && Bot._obs.IsConnected)
                {
                    try
                    {
                        return Bot._obs.GetSceneItemEnabled(SceneName, ItemId);
                    }
                    catch { }
                }
                return false;
            }
            set
            {
                if (Bot._obs != null && Bot._obs.IsConnected)
                {
                    try
                    {
                        Bot._obs.SetSceneItemEnabled(SceneName, ItemId, value);
                    }
                    catch { }
                }
            }
        }

        public int Index
        {
            get
            {
                if (Bot._obs != null && Bot._obs.IsConnected)
                {
                    try
                    {
                        return Bot._obs.GetSceneItemIndex(SceneName, ItemId);
                    }
                    catch { }
                }
                return -1;
            }
        }

        /// <summary></summary>
        /// <param name="source">In the format of title:className:execName.exe</param>
        public void SetWindowSource(string windowSource)
        {
            if (Bot._obs != null && Bot._obs.IsConnected)
            {
                try
                {
                    Bot._obs.SetInputSettings(ItemName, new JObject { ["window"] = windowSource });
                }
                catch { }
            }
        }
    }
}