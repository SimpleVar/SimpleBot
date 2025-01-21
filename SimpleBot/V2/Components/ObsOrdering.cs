namespace SimpleBot.v2
{
    /// <summary>
    /// An ordering of a bunch of ObsItems, all below some "anchor" item:
    /// - anchor item
    /// - item 1
    /// - item 2
    /// - item 3
    /// </summary>
    class ObsOrdering
    {
        public readonly ObsItem AnchorItem;
        public readonly ObsItem[] Items;

        public ObsOrdering(string sceneName, string baseItemName, string[] itemsNames)
            : this(new ObsItem(baseItemName, sceneName), itemsNames?.Select(name => new ObsItem(name, sceneName)).ToArray())
        { }

        public ObsOrdering(ObsItem baseItem, ObsItem[] items)
        {
            AnchorItem = baseItem;
            Items = items;
            ArgumentNullException.ThrowIfNull(baseItem, nameof(baseItem));
            ArgumentNullException.ThrowIfNull(items, nameof(items));
        }

        public void SetOrder(params int[] indices)
        {
            if (indices == null || indices.Length != Items.Length)
                throw new ArgumentException("Must match in length to the number of items", nameof(indices));
#if DEBUG
            if (!indices.OrderBy(x => x).SequenceEqual(Items.Select((x, i) => i)))
                throw new ArgumentException("Must be a valid permutation of indices", nameof(indices));
#endif
            var baseIdx = AnchorItem.Index;
            if (baseIdx == -1)
                return;
            baseIdx--;
            try
            {
                for (int i = indices.Length - 1; i >= 0; i--)
                    v2.Bot._obs.SetSceneItemIndex(AnchorItem.SceneName, Items[indices[i]].ItemId, baseIdx);
            }
            catch { }
        }
    }
}