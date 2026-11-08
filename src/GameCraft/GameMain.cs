// -------------------------------------------------------------------------------------------------------------------------------------------------------------
// Author: 3dapi (https://github.com/3dapi)
// -------------------------------------------------------------------------------------------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Forms;
using Vortice.Direct2D1;
using Vortice.Mathematics;

class GameMain : G2AppBase
{
    public override System.Drawing.Size ScreenSize => GameGlobal.ScreenSize;
    public override string GameName => GameGlobal.GameName;

    private G2Texture? _background;
    private G2Texture? _spearTexture;
    private G2Audio? _battleBgm;
    private SpriteSet? _withSpear;
    private SpriteSet? _withoutSpear;
    private SpriteSet? _knightSprites;
    private G2Font? _font;
    private ID2D1SolidColorBrush? _hudBrush;
    private ID2D1SolidColorBrush? _warningBrush;
    private ID2D1SolidColorBrush? _groundBrush;

    private Player _player = new();
    private Spear _spear = new();
    private Knight _knight = new();
    private GamePhase _phase = GamePhase.Playing;
    private float _resultTimer;

    protected override void Initialize()
    {
        ClearColor = new Color4(0.08f, 0.10f, 0.14f, 1f);

        _background = new G2Texture("Assets/background/out_castle.jpg");
        _spearTexture = new G2Texture("Assets/spear_icon.png");
        _withSpear = SpriteSet.LoadPlayer("Assets/Characters/main_character/WithSpear", true);
        _withoutSpear = SpriteSet.LoadPlayer("Assets/Characters/main_character/WithoutSpear", false);
        _knightSprites = SpriteSet.LoadKnight("Assets/Characters/mob/knight");

        _font = new G2Font("Malgun Gothic", 17f);
        _hudBrush = RenderTarget.CreateSolidColorBrush(new Color4(0.02f, 0.03f, 0.05f, 0.75f));
        _warningBrush = RenderTarget.CreateSolidColorBrush(new Color4(1f, 0.17f, 0.10f, 0.42f));
        _groundBrush = RenderTarget.CreateSolidColorBrush(new Color4(0.93f, 0.70f, 0.34f, 0.58f));

        _battleBgm = new G2Audio("Assets/bgm/battle_bgm.wav");
        _battleBgm.Play(true);
        ResetDemo();
    }

    private void ResetDemo()
    {
        _player = new Player();
        _spear = new Spear();
        _knight = new Knight();
        _phase = GamePhase.Playing;
        _resultTimer = 0f;
        if (_battleBgm != null && !_battleBgm.IsPlaying()) _battleBgm.Play(true);
    }

    protected override void Update()
    {
        float dt = (float)Math.Clamp(DeltaTime, 0.0, 0.05);
        if (Input.IsKeyDown(Keys.Escape))
        {
            Close();
            return;
        }

        if (Input.IsKeyDown(Keys.R))
        {
            ResetDemo();
            return;
        }

        if (_phase != GamePhase.Playing)
        {
            _resultTimer += dt;
            if (_phase == GamePhase.GameOver) _player.UpdateDeath(dt);
            if (_phase == GamePhase.Clear) _knight.UpdateDeath(dt);
            return;
        }

        if (Input.IsButtonDown(MouseButtons.Left) && _spear.IsHeld)
            _player.BeginCharge();

        _player.Update(Input, dt, _spear.IsHeld);
        if (_player.IsCharging)
            _player.FaceToward(Input.MousePosition.X);

        if (Input.IsButtonUp(MouseButtons.Left) && _player.IsCharging && _spear.IsHeld)
        {
            float ratio = _player.ChargeRatio;
            _player.FaceToward(Input.MousePosition.X);
            _player.ReleaseCharge();
            _spear.Throw(_player, Input.MousePosition.X, Input.MousePosition.Y, ratio);
        }

        if (Input.IsKeyDown(Keys.E) && _spear.CanPickUp(_player))
            _spear.PickUp();

        if (Input.IsButtonDown(MouseButtons.Right) && _spear.IsHeld && _player.TryPush())
            _knight.TryGetPushed(_player.CenterX, _player.Facing, 70f);

        _knight.Update(_player, dt);

        _player.ResolveBodyCollision(_knight);

        _spear.Update(dt, _knight);

        if (_player.Health <= 0)
        {
            _phase = GamePhase.GameOver;
            _player.StartDeath();
        }
        else if (_knight.IsDead && _knight.DeathFinished)
        {
            _phase = GamePhase.Clear;
        }
    }

    protected override void Render()
    {
        if (_background == null || _spearTexture == null ||
            _withSpear == null || _withoutSpear == null || _knightSprites == null ||
            _font == null || _hudBrush == null || _warningBrush == null || _groundBrush == null)
            return;

        _background.Draw(new Rect(0, 0, ScreenSize.Width, ScreenSize.Height),
                         new Rect(0, 0, 960, 640));

        RenderTarget.FillRectangle(new Rect(0, CombatSettings.GroundY, 960, 2), _groundBrush);

        if (_knight.IsWarning && !_knight.IsDead)
        {
            float left = _knight.Facing > 0 ? _knight.CenterX : _knight.CenterX - CombatSettings.KnightAttackRange;
            RenderTarget.FillRectangle(new Rect(left, CombatSettings.GroundY - CombatSettings.KnightHitHeight,
                CombatSettings.KnightAttackRange, CombatSettings.KnightHitHeight), _warningBrush);
        }

        _knight.Draw(_knightSprites, RenderTarget);
        _player.Draw(_withSpear, _withoutSpear, _spear.IsHeld, RenderTarget);
        _spear.Draw(_spearTexture, RenderTarget);

        DrawHud(_font, _hudBrush);
    }

    private void DrawHud(G2Font font, ID2D1SolidColorBrush hudBrush)
    {
        Color4 white = new(0.98f, 0.97f, 0.92f, 1f);
        Color4 soft = new(0.81f, 0.88f, 0.93f, 1f);
        Color4 yellow = new(1f, 0.81f, 0.38f, 1f);

        RenderTarget.FillRectangle(new Rect(0, 0, 960, 109), hudBrush);
        font.DrawText("RE:SPEAR  |  외성 전투", new Rect(17, 7, 560, 27), white);
        font.DrawText($"HP {_player.Health}/100    기사 HP {_knight.Health}/{CombatSettings.KnightMaxHealth}    창: {_spear.StatusText}",
                      new Rect(17, 37, 900, 28), soft);
        font.DrawText("A/D 이동  Space 점프  Shift 대시  좌클릭 차징/투척  우클릭 밀치기  E 회수  R 재시작",
                      new Rect(17, 71, 925, 28), soft);

        if (_phase == GamePhase.Playing)
        {
            if (_player.IsCharging)
            {
                font.DrawText($"차징: {_player.ChargeRatio * 100f:0}%    피해 {40f + 60f * _player.ChargeRatio:0}    최대 거리 {96f + 144f * _player.ChargeRatio:0}px",
                              new Rect(18, 120, 660, 30), yellow);
            }
            else if (_spear.CanPickUp(_player))
                font.DrawText("E 키로 창 회수", new Rect(18, 120, 400, 30), yellow);
        }
        else
        {
            RenderTarget.FillRectangle(new Rect(225, 237, 510, 160), hudBrush);
            font.DrawText(_phase == GamePhase.Clear ? "전투 성공!" : "게임 오버",
                          new Rect(286, 267, 465, 42), yellow);
            font.DrawText("R 키: 외성부터 다시 시작     ESC: 종료",
                          new Rect(266, 332, 465, 36), white);
        }
    }

    public override void Dispose()
    {
        _battleBgm?.Stop();
        _battleBgm?.Dispose();
        _groundBrush?.Dispose();
        _warningBrush?.Dispose();
        _hudBrush?.Dispose();
        _font?.Dispose();
        _withSpear?.Dispose();
        _withoutSpear?.Dispose();
        _knightSprites?.Dispose();
        _spearTexture?.Dispose();
        _background?.Dispose();
        base.Dispose();
    }
}

enum GamePhase { Playing, Clear, GameOver }
enum SpearState { Held, Flying, Falling, Dropped }
enum KnightState { Idle, Chase, Warning, Attack, Finish, Cooldown, Dead }

static class CombatSettings
{
    public const float GroundY = 450f;

    public const float SourceSpriteSize = 64f;
    public const float DisplaySpriteSize = 128f;
    public const float LogicalSpriteSize = 64f;
    public const float SpriteHorizontalOffset = (DisplaySpriteSize - LogicalSpriteSize) / 2f;

    public const float PlayerSpriteFootPadding = 10f;
    public const float KnightSpriteFootPadding = 6f;

    public const float PlayerBodyHalfWidth = 28f;
    public const float KnightBodyHalfWidth = 30f;
    public const float PlayerBodyHeight = 96f;
    public const float KnightHitHeight = 108f;

    public const float SpearDisplayWidth = 104f;
    public const float SpearDisplayHeight = 20f;

    public const float PlayerSpeed = 108f;
    public const int KnightMaxHealth = 250;
    public const int KnightAttackDamage = 30;
    public const float KnightSpeed = 48f;
    public const float KnightDetectRange = 300f;
    public const float KnightAttackRange = 72f;
    public const float KnightWarning = 0.5f;
    public const float KnightActive = 0.125f;
    public const float KnightFinish = 0.25f;
    public const float KnightCooldown = 1.5f;

    public const int ArcherMaxHealth = 200;
    public const int ArcherAttackDamage = 45;
    public const float ArcherSpeed = 72f;
    public const float ArcherDetectRange = 360f;
    public const float ArcherAttackRange = 180f;
    public const float ArcherWarning = 0.6f;
    public const float ArcherActive = 0.2f;
    public const float ArcherCooldown = 2f;

    public const int TankMaxHealth = 500;
    public const int TankAttackDamage = 25;
    public const float TankSpeed = 36f;
    public const float TankDetectRange = 280f;
    public const float TankAttackRange = 72f;
    public const float TankWarning = 0.8f;
    public const float TankActive = 0.2f;
    public const float TankCooldown = 3f;
    public const float TankDamageReduction = 0.4f;

    public const float ChargeMaxSeconds = 1.3f;
    public const float ThrowSpeed = 420f;
    public const float MinThrowRange = 96f;
    public const float MaxThrowRange = 240f;
    public const float MinThrowDamage = 40f;
    public const float MaxThrowDamage = 100f;
    public const float SpearPickUpRange = 48f;
    public const float FallSpeed = 360f;
    public const float JumpSpeed = 520f;
    public const float Gravity = 900f;
    public const float DashSpeed = 660f;
    public const float DashSeconds = 0.18f;
    public const float DashCooldown = 1.0f;
    public const float PushCooldown = 1.1f;
}

class Player
{
    public float X { get; private set; } = 98f;
    public float Y { get; private set; } = CombatSettings.GroundY - 64f;
    public float CenterX => X + 32f;
    public float CenterY => Y + 30f;
    public float FeetY => Y + CombatSettings.LogicalSpriteSize;
    public float Facing { get; private set; } = 1f;

    public void FaceToward(float mouseX)
    {
        Facing = mouseX >= CenterX ? 1f : -1f;
    }
    public int Health { get; private set; } = 100;
    public bool IsCharging { get; private set; }
    public float ChargeSeconds { get; private set; }
    public float ChargeRatio => Math.Clamp(ChargeSeconds / CombatSettings.ChargeMaxSeconds, 0f, 1f);

    private float _velocityY;
    private float _dashRemaining;
    private float _dashCooldown;
    private float _pushCooldown;
    private float _pushAnimationRemaining;
    private float _throwAnimationRemaining;
    private float _immuneRemaining;
    private float _motionTime;
    private string _motion = "Idle";
    private bool _moving;
    private float _deathTime;

    public void BeginCharge()
    {
        if (Health <= 0 || IsCharging || _dashRemaining > 0f || _throwAnimationRemaining > 0f) return;
        IsCharging = true;
        ChargeSeconds = 0f;
    }

    public void ReleaseCharge()
    {
        IsCharging = false;
        ChargeSeconds = 0f;
        _throwAnimationRemaining = 0.45f;
    }

    public bool TryPush()
    {
        if (Health <= 0 || IsCharging || _pushCooldown > 0f || _dashRemaining > 0f) return false;
        _pushCooldown = CombatSettings.PushCooldown;
        _pushAnimationRemaining = 0.35f;
        return true;
    }

    public void Update(G2InputContext input, float dt, bool hasSpear)
    {
        if (Health <= 0) return;
        _dashCooldown = MathF.Max(0f, _dashCooldown - dt);
        _pushCooldown = MathF.Max(0f, _pushCooldown - dt);
        _immuneRemaining = MathF.Max(0f, _immuneRemaining - dt);
        _pushAnimationRemaining = MathF.Max(0f, _pushAnimationRemaining - dt);
        _throwAnimationRemaining = MathF.Max(0f, _throwAnimationRemaining - dt);

        if (!hasSpear) IsCharging = false;
        if (IsCharging)
            ChargeSeconds = MathF.Min(CombatSettings.ChargeMaxSeconds, ChargeSeconds + dt);

        int horizontal = 0;
        if (input.IsKeyDown(Keys.A) || input.IsKeyPress(Keys.A)) horizontal--;
        if (input.IsKeyDown(Keys.D) || input.IsKeyPress(Keys.D)) horizontal++;
        if (horizontal != 0) Facing = horizontal;
        _moving = horizontal != 0;

        if (!IsCharging && _dashCooldown <= 0f && input.IsKeyDown(Keys.ShiftKey))
        {
            _dashRemaining = CombatSettings.DashSeconds;
            _dashCooldown = CombatSettings.DashCooldown;
        }

        if (_dashRemaining > 0f)
        {
            X += Facing * CombatSettings.DashSpeed * dt;
            _dashRemaining = MathF.Max(0f, _dashRemaining - dt);
        }
        else
        {
            float speed = CombatSettings.PlayerSpeed * (IsCharging ? 0.45f : 1f);
            X += horizontal * speed * dt;
        }

        X = Math.Clamp(X, 0f, 960f - 64f);

        bool grounded = Y >= CombatSettings.GroundY - 64.01f;
        if (grounded && !IsCharging && input.IsKeyDown(Keys.Space))
            _velocityY = -CombatSettings.JumpSpeed;
        _velocityY += CombatSettings.Gravity * dt;
        Y += _velocityY * dt;
        if (Y >= CombatSettings.GroundY - 64f)
        {
            Y = CombatSettings.GroundY - 64f;
            _velocityY = 0f;
        }

        string action = ChooseMotion();
        if (action != _motion)
        {
            _motion = action;
            _motionTime = 0f;
        }
        else _motionTime += dt;
    }

    private string ChooseMotion()
    {
        if (Health <= 0) return "Death";
        if (_throwAnimationRemaining > 0f) return "Throw";
        if (_pushAnimationRemaining > 0f) return "Push";
        if (IsCharging) return "Charge";
        if (_dashRemaining > 0f) return "Dash";
        if (_velocityY < -35f) return "Jump";
        if (_velocityY > 35f && FeetY < CombatSettings.GroundY - 1f) return "Fall";
        return _moving ? "Run" : "Idle";
    }

    public void ResolveBodyCollision(Knight knight)
    {
        if (knight.IsDead) return;

        float playerTop = FeetY - CombatSettings.PlayerBodyHeight;
        float knightTop = CombatSettings.GroundY - CombatSettings.KnightHitHeight;
        if (FeetY <= knightTop || playerTop >= CombatSettings.GroundY) return;

        float pLeft = CenterX - CombatSettings.PlayerBodyHalfWidth;
        float pRight = CenterX + CombatSettings.PlayerBodyHalfWidth;
        float eLeft = knight.CenterX - CombatSettings.KnightBodyHalfWidth;
        float eRight = knight.CenterX + CombatSettings.KnightBodyHalfWidth;

        if (pRight > eLeft && pLeft < eRight)
        {
            float targetCenter = CenterX < knight.CenterX
                ? eLeft - CombatSettings.PlayerBodyHalfWidth
                : eRight + CombatSettings.PlayerBodyHalfWidth;
            X = Math.Clamp(targetCenter - CombatSettings.LogicalSpriteSize / 2f,
                           0f, 960f - CombatSettings.LogicalSpriteSize);
        }
    }

    public void TakeDamage(int damage)
    {
        if (Health <= 0 || _immuneRemaining > 0f) return;
        Health = Math.Max(0, Health - damage);
        _immuneRemaining = 0.45f;
    }

    public void StartDeath()
    {
        IsCharging = false;
        _motionTime = 0f;
        _motion = "Death";
        _deathTime = 0f;
    }

    public void UpdateDeath(float dt) => _deathTime += dt;

    public void Draw(SpriteSet withSpear, SpriteSet withoutSpear, bool isHoldingSpear,
                     ID2D1HwndRenderTarget target)
    {
        bool forceWithSpear = _motion == "Charge" || _motion == "Throw" || _motion == "Push";
        SpriteSet set = (isHoldingSpear || forceWithSpear) ? withSpear : withoutSpear;
        string action = Health <= 0 ? "Death" : _motion;
        if (!set.Has(action)) action = "Idle";
        float time = Health <= 0 ? _deathTime : _motionTime;
        if (action == "Charge") set.DrawFraction(action, ChargeRatio, X, Y, Facing > 0f, target);
        else set.Draw(action, time, action == "Run" ? 11f : action == "Dash" ? 16f : 10f,
                      X, Y, Facing > 0f, target, action != "Death" && action != "Throw" && action != "Push");
    }
}

class Spear
{
    public SpearState State { get; private set; } = SpearState.Held;
    public bool IsHeld => State == SpearState.Held;
    public float X { get; private set; }
    public float Y { get; private set; }
    public string StatusText => State switch
    {
        SpearState.Held => "보유",
        SpearState.Flying => "비행 중",
        SpearState.Falling => "낙하 중",
        _ => "바닥 (E 회수)"
    };

    private float _vx, _vy;
    private float _distance, _maxDistance;
    private float _angle;
    private int _damage;

    public void Throw(Player player, float mouseX, float mouseY, float charge)
    {
        if (!IsHeld) return;
        X = player.CenterX + player.Facing * 32f;
        Y = player.FeetY - 65f;
        float dx = mouseX - X, dy = mouseY - Y;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length < 8f)
        {
            dx = player.Facing;
            dy = 0f;
            length = 1f;
        }
        dx /= length;
        dy /= length;
        _vx = dx * CombatSettings.ThrowSpeed;
        _vy = dy * CombatSettings.ThrowSpeed;
        _angle = MathF.Atan2(dy, dx);
        _distance = 0f;
        _maxDistance = CombatSettings.MinThrowRange +
            (CombatSettings.MaxThrowRange - CombatSettings.MinThrowRange) * charge;
        _damage = (int)MathF.Round(CombatSettings.MinThrowDamage +
            (CombatSettings.MaxThrowDamage - CombatSettings.MinThrowDamage) * charge);
        State = SpearState.Flying;
    }

    public void Update(float dt, Knight enemy)
    {
        if (State == SpearState.Flying)
        {
            float stepDistance = CombatSettings.ThrowSpeed * dt;
            int steps = Math.Max(1, (int)MathF.Ceiling(stepDistance / 5f));
            float step = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                float prevX = X, prevY = Y;
                X += _vx * step;
                Y += _vy * step;
                _distance += CombatSettings.ThrowSpeed * step;
                float tipX = X + (_vx / CombatSettings.ThrowSpeed) *
                             (CombatSettings.SpearDisplayWidth / 2f);
                float tipY = Y + (_vy / CombatSettings.ThrowSpeed) *
                             (CombatSettings.SpearDisplayWidth / 2f);
                if (!enemy.IsDead &&
                    (enemy.HitTestSpear(tipX, tipY) || enemy.HitTestSpear(X, Y)))
                {
                    enemy.ReceiveDamage(_damage);
                    State = SpearState.Falling;
                    X = (prevX + X) / 2f;
                    Y = (prevY + Y) / 2f;
                    break;
                }
                if (_distance >= _maxDistance || X <= 12f || X >= 948f ||
                    Y >= CombatSettings.GroundY - CombatSettings.SpearDisplayHeight / 2f || Y < 115f)
                {
                    State = SpearState.Falling;
                    X = Math.Clamp(X, 18f, 942f);
                    break;
                }
            }
        }
        else if (State == SpearState.Falling)
        {
            Y += CombatSettings.FallSpeed * dt;
            if (Y >= CombatSettings.GroundY - CombatSettings.SpearDisplayHeight / 2f)
            {
                Y = CombatSettings.GroundY - CombatSettings.SpearDisplayHeight / 2f;
                State = SpearState.Dropped;
            }
        }
    }

    public bool CanPickUp(Player player)
    {
        return State == SpearState.Dropped &&
               MathF.Abs(player.CenterX - X) <= CombatSettings.SpearPickUpRange &&
               player.FeetY > CombatSettings.GroundY - 65f;
    }

    public void PickUp()
    {
        if (State == SpearState.Dropped) State = SpearState.Held;
    }

    public void Draw(G2Texture texture, ID2D1HwndRenderTarget target)
    {
        if (IsHeld) return;
        float angle = State == SpearState.Dropped ? 0f : _angle;
        float width = CombatSettings.SpearDisplayWidth;
        float height = CombatSettings.SpearDisplayHeight;
        var saved = target.Transform;
        target.Transform = Matrix3x2.CreateRotation(angle + MathF.PI, new Vector2(X, Y)) * saved;
        texture.Draw(new Rect(X - width * .5f, Y - height * .5f, width, height),
                     new Rect(0, 0, 512, 96), 1f, BitmapInterpolationMode.NearestNeighbor);
        target.Transform = saved;
    }
}

class Knight
{
    public float X { get; private set; } = 580f;
    public float Y => CombatSettings.GroundY - 64f;
    public float CenterX => X + 32f;
    public float Facing { get; private set; } = -1f;
    public int Health { get; private set; } = CombatSettings.KnightMaxHealth;
    public bool IsDead => Health <= 0;
    public bool DeathFinished => _deathTime >= 0.7f;
    public bool IsWarning => _state == KnightState.Warning;

    private KnightState _state = KnightState.Idle;
    private float _stateTimer;
    private float _attackElapsed;
    private float _deathTime;
    private float _walkTime;
    private bool _attackApplied;

    public void ReceiveDamage(int damage)
    {
        if (IsDead) return;
        Health = Math.Max(0, Health - damage);
        if (IsDead)
        {
            _state = KnightState.Dead;
            _deathTime = 0f;
        }
    }

    public bool HitTestSpear(float x, float y)
    {
        return MathF.Abs(x - CenterX) <= CombatSettings.KnightBodyHalfWidth + 4f &&
               y >= CombatSettings.GroundY - CombatSettings.KnightHitHeight &&
               y <= CombatSettings.GroundY;
    }

    public void TryGetPushed(float playerCenter, float playerFacing, float distance)
    {
        if (IsDead) return;
        float dx = CenterX - playerCenter;
        if (MathF.Abs(dx) > 82f || dx * playerFacing < 0f) return;
        X = Math.Clamp(X + playerFacing * distance, 0f, 896f);
        if (_state == KnightState.Warning)
        {
            _state = KnightState.Cooldown;
            _stateTimer = 0f;
        }
    }

    public void Update(Player player, float dt)
    {
        if (IsDead)
        {
            UpdateDeath(dt);
            return;
        }

        _stateTimer += dt;
        float dx = player.CenterX - CenterX;
        float absDistance = MathF.Abs(dx);
        if (_state != KnightState.Warning && _state != KnightState.Attack && _state != KnightState.Finish)
            Facing = dx >= 0f ? 1f : -1f;

        switch (_state)
        {
            case KnightState.Idle:
            case KnightState.Chase:
                if (absDistance > CombatSettings.KnightDetectRange)
                {
                    _state = KnightState.Idle;
                }
                else if (absDistance <= CombatSettings.KnightAttackRange)
                {
                    ChangeState(KnightState.Warning);
                    _attackElapsed = 0f;
                    _attackApplied = false;
                }
                else
                {
                    _state = KnightState.Chase;
                    X += Facing * CombatSettings.KnightSpeed * dt;
                    X = Math.Clamp(X, 0f, 896f);
                    _walkTime += dt;
                }
                break;

            case KnightState.Warning:
                _attackElapsed += dt;
                if (_stateTimer >= CombatSettings.KnightWarning)
                    ChangeState(KnightState.Attack);
                break;

            case KnightState.Attack:
                _attackElapsed += dt;
                if (!_attackApplied)
                {
                    float directionalDistance = (player.CenterX - CenterX) * Facing;
                    bool verticalOverlap =
                        player.FeetY > CombatSettings.GroundY - CombatSettings.KnightHitHeight &&
                        player.FeetY - CombatSettings.PlayerBodyHeight < CombatSettings.GroundY;
                    if (directionalDistance >= 0f && directionalDistance <= CombatSettings.KnightAttackRange &&
                        verticalOverlap)
                        player.TakeDamage(CombatSettings.KnightAttackDamage);
                    _attackApplied = true;
                }
                if (_stateTimer >= CombatSettings.KnightActive)
                    ChangeState(KnightState.Finish);
                break;

            case KnightState.Finish:
                _attackElapsed += dt;
                if (_stateTimer >= CombatSettings.KnightFinish)
                    ChangeState(KnightState.Cooldown);
                break;

            case KnightState.Cooldown:
                if (_stateTimer >= CombatSettings.KnightCooldown)
                    ChangeState(KnightState.Idle);
                break;
        }
    }

    private void ChangeState(KnightState state)
    {
        _state = state;
        _stateTimer = 0f;
    }

    public void UpdateDeath(float dt)
    {
        if (IsDead) _deathTime += dt;
    }

    public void Draw(SpriteSet sprites, ID2D1HwndRenderTarget target)
    {
        string motion;
        float time;
        bool looping;
        float fps;

        if (IsDead)
        {
            motion = "Death"; time = _deathTime; looping = false; fps = 10f;
        }
        else if (_state == KnightState.Warning || _state == KnightState.Attack || _state == KnightState.Finish)
        {
            motion = "Attack"; time = _attackElapsed; looping = false; fps = 8f;
        }
        else if (_state == KnightState.Chase)
        {
            motion = "Run"; time = _walkTime; looping = true; fps = 10f;
        }
        else
        {
            motion = "Idle"; time = _walkTime; looping = true; fps = 8f;
        }
        sprites.Draw(motion, time, fps, X, Y, Facing > 0f, target, looping);
    }
}

sealed class SpriteSet : IDisposable
{
    private readonly Dictionary<string, G2Texture[]> _animations = new();
    private readonly float _footPadding;

    private SpriteSet(float footPadding)
    {
        _footPadding = footPadding;
    }

    private void Load(string root, string action, int count)
    {
        var frames = new G2Texture[count];
        for (int i = 0; i < count; i++)
        {
            string file = $"{root}/{action}/{action.ToLowerInvariant()}_{i + 1:00}.png";
            frames[i] = new G2Texture(file);
        }
        _animations[action] = frames;
    }

    public static SpriteSet LoadPlayer(string root, bool hasSpear)
    {
        SpriteSet result = new(CombatSettings.PlayerSpriteFootPadding);
        result.Load(root, "Idle", 8);
        result.Load(root, "Run", 8);
        result.Load(root, "Jump", 8);
        result.Load(root, "Fall", 6);
        result.Load(root, "Dash", 8);
        result.Load(root, "Death", 8);
        if (hasSpear)
        {
            result.Load(root, "Charge", 9);
            result.Load(root, "Throw", 9);
            result.Load(root, "Push", 8);
        }
        return result;
    }

    public static SpriteSet LoadKnight(string root)
    {
        SpriteSet result = new(CombatSettings.KnightSpriteFootPadding);
        result.Load(root, "Idle", 8);
        result.Load(root, "Run", 8);
        result.Load(root, "Attack", 7);
        result.Load(root, "Death", 7);
        return result;
    }

    public bool Has(string action) => _animations.ContainsKey(action);

    public void Draw(string action, float elapsed, float fps, float x, float y,
                     bool faceRight, ID2D1HwndRenderTarget target, bool loop)
    {
        var frames = _animations[action];
        int index = (int)MathF.Floor(elapsed * fps);
        index = loop ? index % frames.Length : Math.Clamp(index, 0, frames.Length - 1);
        DrawFrame(frames[index], x, y, faceRight, target);
    }

    public void DrawFraction(string action, float fraction, float x, float y,
                             bool faceRight, ID2D1HwndRenderTarget target)
    {
        var frames = _animations[action];
        int index = Math.Clamp((int)(fraction * (frames.Length - 1)), 0, frames.Length - 1);
        DrawFrame(frames[index], x, y, faceRight, target);
    }

    private void DrawFrame(G2Texture frame, float x, float y, bool faceRight,
                           ID2D1HwndRenderTarget target)
    {
        float drawX = x - CombatSettings.SpriteHorizontalOffset;
        float drawY = y + CombatSettings.LogicalSpriteSize -
                      CombatSettings.DisplaySpriteSize + _footPadding;

        var saved = target.Transform;
        try
        {
            if (!faceRight)
            {
                float pivotX = drawX + CombatSettings.DisplaySpriteSize / 2f;
                float pivotY = drawY + CombatSettings.DisplaySpriteSize / 2f;
                target.Transform = Matrix3x2.CreateScale(-1f, 1f,
                    new Vector2(pivotX, pivotY)) * saved;
            }

            frame.Draw(
                new Rect(drawX, drawY, CombatSettings.DisplaySpriteSize,
                         CombatSettings.DisplaySpriteSize),
                new Rect(0, 0, CombatSettings.SourceSpriteSize,
                         CombatSettings.SourceSpriteSize),
                1f, BitmapInterpolationMode.NearestNeighbor);
        }
        finally
        {
            target.Transform = saved;
        }
    }

    public void Dispose()
    {
        foreach (var frames in _animations.Values)
            foreach (var frame in frames)
                frame.Dispose();
        _animations.Clear();
    }
}
