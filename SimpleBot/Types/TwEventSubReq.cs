namespace SimpleBot
{
  struct TwEventSubReq
  {
    public string type;
    public int version;
    public Dictionary<string, string> conditions = new();

    public TwEventSubReq(int version, string type)
    {
      this.version = version;
      this.type = type;
    }

    public TwEventSubReq Cond(string condition, string value)
    {
      conditions.Add(condition, value);
      return this;
    }
  }
}