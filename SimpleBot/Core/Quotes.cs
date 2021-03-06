﻿namespace SimpleBot
{
  static class Quotes
  {
    static List<string> _quotes = new();
    static string _quotesFile;

    public static void Load(string quotesFile)
    {
      _quotesFile = quotesFile;
      if (string.IsNullOrEmpty(_quotesFile))
        return;
      try
      {
        _quotes = new List<string>(File.ReadAllLines(_quotesFile));
      }
      catch { }
    }

    public static void Save()
    {
      if (string.IsNullOrEmpty(_quotesFile))
        return;
      File.WriteAllLines(_quotesFile, _quotes);
    }

    public static string GetRandom() => _quotes.Count == 0 ? "No quotes D:" : GetQuote(Rand.R.Next(_quotes.Count));
    public static string GetQuote(int i) => i >= 0 && i < _quotes.Count ? $"{i + 1}. {_quotes[i]}" : $"{_quotes.Count}. {_quotes[^1]}";
    public static string FindQuote(string query)
    {
      var candidates = _quotes
        .Select((q, i) => new { Q = q, I = i })
        .Where(q => q.Q.Contains(query, StringComparison.InvariantCultureIgnoreCase))
        .Select(q => q.I)
        .ToArray();
      if (candidates.Length == 0)
        return null;
      return GetQuote(candidates.AtRand());
    }
    public static int AddQuote(string quote)
    {
      _quotes.Add(quote);
      return _quotes.Count;
    }
    public static bool DelQuote(int i)
    {
      if (i <= 0 || i >= _quotes.Count)
        return false;
      _quotes.RemoveAt(i);
      return true;
    }
  }
}