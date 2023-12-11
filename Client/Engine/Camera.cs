using OpenTK.Windowing.Common;

namespace Penki.Client.Engine;

public class Camera
{
  public const float Fov = 45 * MathF.PI / 180f;
  public const float Near = 0.1f;
  public readonly Vec3 WorldUp = Vec3.UnitY;
  public Vec3 Front;
  public Vec3 Right;
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
    Right = Vec3.Cross(Front, WorldUp).Normalized();
    _up = Vec3.Cross(Right, Front).Normalized();
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

  public Vec3 Target => Pos + Vec3.UnitY * 2.4f;
  public Vec3 Eye => Target - Front * 10;

  public Mat4 View => Mat4.LookAt(Eye, Target, _up);

  public const float ZNear = 0.001f;
  public const float ZFar = 100.0f;

  public Mat4 Proj =>
    Mat4.CreatePerspectiveFieldOfView(Fov,
      Penki.SizeF.X / Penki.SizeF.Y, ZNear, ZFar);
}