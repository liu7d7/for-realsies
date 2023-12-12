using Penki.Client.Engine;

namespace Penki.Client.Game;

public abstract class Entity
{
  public abstract Vec3 Pos { get; set; }
  public abstract Vec3 Vel { get; set; }
  
  public abstract void Draw(Mat4 model, RenderSource source);
  public abstract void Tick(float dt);
}