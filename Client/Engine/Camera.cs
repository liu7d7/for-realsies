using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Penki.Client.Engine;

public class Camera
  {
    public const float Fov = 45 * MathF.PI / 180f;
    public const float Near = 0.1f;
    private readonly Vec3 _worldUp = Vec3.UnitY;
    private Vec3 _front;
    private Vec3 _right;
    private Vec3 _up;
    private float _lastX;
    private float _lastY;
    public Vec3 Pos;
    private float _yaw;
    private float _pitch;

    private bool _first = true;

    public void Tick()
    {
      _front = 
        new Vec3(
          MathF.Cos(_pitch.Rad()) * MathF.Cos(_yaw.Rad()),
          MathF.Sin(_pitch.Rad()),
          MathF.Cos(_pitch.Rad()) * MathF.Sin(_yaw.Rad())).Normalized();
      _right = Vec3.Cross(_front, _worldUp).Normalized();
      _up = Vec3.Cross(_right, _front).Normalized();
      
      var dir = Vec3.Zero;
      if (Penki.IsDown(Keys.W)) dir.Z++;
      if (Penki.IsDown(Keys.S)) dir.Z--;
      if (Penki.IsDown(Keys.A)) dir.X--;
      if (Penki.IsDown(Keys.D)) dir.X++;
      if (Penki.IsDown(Keys.Space)) dir.Y++;
      if (Penki.IsDown(Keys.LeftShift)) dir.Y--;
      if (dir.Length > 0.0001) dir.Normalize();

      Pos += dir.X * _right * 0.01f;
      Pos += dir.Z * (_front * new Vec3(1, 0, 1)).Normalized() * 0.01f;
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

      _yaw += xOffset;
      _pitch += yOffset;

      if (_pitch > 89.0f)
        _pitch = 89.0f;
      if (_pitch < -89.0f)
        _pitch = -89.0f;
    }

    public Vec3 Eye => Pos;

    public Mat4 View
    {
      get
      {
        var target = Eye + _front;
        var lookAt = Mat4.LookAt(Eye, target, _up);
        return lookAt;
      }
    }

    public Mat4 Proj =>
      Mat4.CreatePerspectiveFieldOfView(Fov,
        Penki.SizeF.X / Penki.SizeF.Y, 0.001f, 100.0f);
  }