namespace SimpleBot
{
  class RandomBag<T>
  {
    readonly Random _rand;
    readonly int[] _rands;
    readonly T[] _items;
    int _nextsUntilShuffle = 0;

    public RandomBag(T[] items) : this(items, new Random())
    { }

    public RandomBag(T[] items, Random rand)
    {
      _items = items;
      _rands = new int[items.Length];
      _rand = rand;
    }

    public T Next()
    {
      if (_nextsUntilShuffle == 0)
      {
        _nextsUntilShuffle = _items.Length;
        for (int i = 0; i < _rands.Length; i++)
          _rands[i] = _rand.Next();
        Array.Sort(_rands, _items);
      }
      return _items[--_nextsUntilShuffle];
    }
  }
}
