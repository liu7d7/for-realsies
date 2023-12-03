using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Penki.Client.Engine;

public class Camera
{
  public const float Fov = 45 * MathF.PI / 180f;
  public const float Near = 0.1f;
  private readonly Vec3 _worldUp = Vec3.UnitY;
  public Vec3 Front;
  private Vec3 _right;
  private Vec3 _up;
  private float _lastX;
  private float _lastY;
  public Vec3 Pos;
  public float Yaw;
  public float Pitch;

  private bool _first = true;

  public void Tick()
  {
    Front =
      new Vec3(
        MathF.Cos(Pitch.Rad()) * MathF.Cos(Yaw.Rad()),
        MathF.Sin(Pitch.Rad()),
        MathF.Cos(Pitch.Rad()) * MathF.Sin(Yaw.Rad())).Normalized();
    _right = Vec3.Cross(Front, _worldUp).Normalized();
    _up = Vec3.Cross(_right, Front).Normalized();

    var dir = Vec3.Zero;
    if (Penki.IsDown(Keys.W)) dir.Z++;
    if (Penki.IsDown(Keys.S)) dir.Z--;
    if (Penki.IsDown(Keys.A)) dir.X--;
    if (Penki.IsDown(Keys.D)) dir.X++;
    if (Penki.IsDown(Keys.Space)) dir.Y++;
    if (Penki.IsDown(Keys.LeftShift)) dir.Y--;
    if (dir.Length > 0.0001) dir.Normalize();

    Pos += dir.X * _right * 0.01f;
    Pos += dir.Z * (Front * new Vec3(1, 0, 1)).Normalized() * 0.01f;
    Pos += dir.Y * _worldUp * 0.01f;
  }

  private CursorState _lastCursorState = CursorState.Grabbed;

  public void Look()
  {
    if (_lastCursorState == CursorState.Normal &&
        Penki.Cursor == CursorState.Grabbed)
    {
      _first = true;
    }

    _lastCursorState = Penki.Cursor;
    if (Penki.Cursor != CursorState.Grabbed || !Penki.InFocus)
      return;
    float xPos = Penki.Mouse.X;
    float yPos = Penki.Mouse.Y;

    if (_first)
    {
      _lastX = xPos;
      _lastY = yPos;
      _first = false;
    }

    float xOffset = xPos - _lastX;
    float yOffset = _lastY - yPos;
    _lastX = xPos;
    _lastY = yPos;

    const float sensitivity = 0.1f;
    xOffset *= sensitivity;
    yOffset *= sensitivity;

    Yaw += xOffset;
    Pitch += yOffset;

    if (Pitch > 89.0f)
      Pitch = 89.0f;
    if (Pitch < -89.0f)
      Pitch = -89.0f;
  }

  public Vec3 Target => Pos + Vec3.UnitY * 1.8f;
  public Vec3 Eye => Target - Front * 10;

  public Mat4 View => Mat4.LookAt(Eye, Target, _up);

  public const float ZNear = 0.001f;
  public const float ZFar = 100.0f;

  public Mat4 Proj =>
    Mat4.CreatePerspectiveFieldOfView(Fov,
      Penki.SizeF.X / Penki.SizeF.Y, ZNear, ZFar);
}