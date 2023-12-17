using Penki.Client.Engine;

namespace Penki.Client.Game;

public abstract class Entity
{
  public World World;
  public bool WasViewed;
  
  public Entity(World world)
  {
    World = world;
  }
  
  public abstract Vec3 Pos { get; set; }
  public abstract Vec3 Vel { get; set; }
  
  public abstract void Draw(Mat4 model, RenderSource source);
  public abstract void Tick(float dt);

  public virtual void OnCollide(Entity other)
  {
    
  }

  public virtual void EnterView()
  {
    
  }

  public virtual void ExitView()
  {
    
  }
}