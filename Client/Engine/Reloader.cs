namespace Penki.Client.Engine;

public interface IReloadable
{
  public int ReloadId { get; set; }
  
  public void Reload();
}

public static class Reloader
{
  private static readonly List<IReloadable> _reloadables = new();
  private static readonly List<IReloadable> _queueAdd = new();
  private static readonly List<IReloadable> _queueDel = new();
  private static int _idCounter = 0;
  private static bool _reloading = false;
  
  public static void Register(IReloadable reloadable)
  {
    reloadable.ReloadId = _idCounter++;
    
    if (_reloading)
    {
      _queueAdd.Add(reloadable);
    }
    else
    {
      _reloadables.Add(reloadable);
    }
  }

  public static void Deregister(IReloadable reloadable)
  {
    if (_reloading)
    {
      _queueDel.Add(reloadable);
    }
    else
    {
      _reloadables.RemoveAt(
        _reloadables.FindIndex(it => it.ReloadId == reloadable.ReloadId));
    }
  }

  public static void Load()
  {
    _reloading = true;
    _reloadables.ForEach(it =>
    {
      try
      {
        it.Reload();
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
      }
    });
    _reloading = false;
    _reloadables.AddRange(_queueAdd);
    _queueAdd.Clear();
    foreach (var toRemove in _queueDel)
    {
      _reloadables.RemoveAt(
        _reloadables.FindIndex(it => it.ReloadId == toRemove.ReloadId));
    }
    _queueDel.Clear();
  }
}