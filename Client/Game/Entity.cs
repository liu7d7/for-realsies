namespace Penki.Game;

public abstract class Entity
{
  public abstract Vec3 Pos { get; set; }
  
  public abstract void Draw(Mat4 model);
  public abstract void Tick();
}