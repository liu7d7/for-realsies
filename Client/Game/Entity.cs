namespace Penki.Game;

public abstract class Entity
{
  public Vec3 Pos;
  
  public abstract void Draw(Mat4 model);
  public abstract void Tick();
}