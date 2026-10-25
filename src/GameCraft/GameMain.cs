// -------------------------------------------------------------------------------------------------------------------------------------------------------------
// Author: 3dapi (https://github.com/3dapi)
// -------------------------------------------------------------------------------------------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Vortice.Direct2D1;
using Vortice.Mathematics;

class GameMain : G2AppBase
{
    public override System.Drawing.Size ScreenSize => GameGlobal.ScreenSize;
    public override string GameName => GameGlobal.GameName;

    private G2Texture? _playerImage;
    private G2Texture? _spearImage;
    private G2Texture? _meleeImage;
    private G2Texture? _archerImage;
    private G2Texture? _tankImage;

    private G2Font? _font;
    private ID2D1SolidColorBrush? _floorBrush;
    private ID2D1SolidColorBrush? _platformBrush;

    private Player? _player;
    private Spear? _spear;
    private Stage? _stage;

    protected override void Initialize()
    {
        ClearColor = new Color4(0.08f, 0.10f, 0.14f, 1f);

        _playerImage = new G2Texture("Assets/player_idle.png");
        _spearImage = new G2Texture("Assets/spear_icon.png");
        _meleeImage = new G2Texture("Assets/enemy_melee.png");
        _archerImage = new G2Texture("Assets/enemy_archer.png");
        _tankImage = new G2Texture("Assets/enemy_tank.png");

        _font = new G2Font("Malgun Gothic", 18f);
        _floorBrush = RenderTarget.CreateSolidColorBrush(new Color4(0.22f, 0.23f, 0.27f, 1f));
        _platformBrush = RenderTarget.CreateSolidColorBrush(new Color4(0.34f, 0.29f, 0.26f, 1f));

        _player = new Player(95f, Stage.GroundY - 158f, 110f, 158f);
        _spear = new Spear();
        _stage = new Stage();
        _stage.Load(1, _meleeImage, _archerImage, _tankImage);
    }

    protected override void Update()
    {
        float dt = (float)Math.Min(DeltaTime, 0.05);

        if (Input.IsKeyDown(Keys.Escape))
        {
            Close();
            return;
        }

        if (Input.IsKeyDown(Keys.D1) || Input.IsKeyDown(Keys.NumPad1))
            _stage?.Load(1, _meleeImage!, _archerImage!, _tankImage!);
        if (Input.IsKeyDown(Keys.D2) || Input.IsKeyDown(Keys.NumPad2))
            _stage?.Load(2, _meleeImage!, _archerImage!, _tankImage!);
        if (Input.IsKeyDown(Keys.D3) || Input.IsKeyDown(Keys.NumPad3))
            _stage?.Load(3, _meleeImage!, _archerImage!, _tankImage!);

        _player?.Update(Input, dt, ScreenSize.Width);
        _stage?.Update(dt);
        _spear?.Update(dt);
    }

    protected override void Render()
    {
        if (_playerImage == null || _spearImage == null || _stage == null ||
            _player == null || _spear == null || _font == null ||
            _floorBrush == null || _platformBrush == null)
            return;

        _stage.DrawFloor(RenderTarget, _floorBrush, _platformBrush);
        _stage.DrawEnemies();
        _player.Draw(_playerImage);
        _spear.DrawIcon(_spearImage);

        Color4 white = new(0.95f, 0.95f, 0.91f, 1f);
        Color4 muted = new(0.68f, 0.73f, 0.78f, 1f);

        _font.DrawText("RE:SPEAR  |  2주차 기본 구조", new Rect(22, 16, 520, 34), white);
        _font.DrawText($"맵: {_stage.Name}    HP: {_player.Health}/{_player.MaxHealth}",
                       new Rect(22, 53, 700, 34), muted);
        _font.DrawText("A / D: 이동     1 / 2 / 3: 맵 보기     ESC: 종료",
                       new Rect(22, 94, 800, 34), muted);
        _font.DrawText("창 소지 상태: " + (_spear.IsHeld ? "소지 중" : "회수 필요"),
                       new Rect(716, 72, 230, 32), white);

        if (_stage.Number == 3)
            _font.DrawText("보스 이미지 준비 후 이 위치에 배치", new Rect(325, 255, 560, 44), white);
    }

    public override void Dispose()
    {
        _floorBrush?.Dispose();
        _platformBrush?.Dispose();
        _font?.Dispose();
        _playerImage?.Dispose();
        _spearImage?.Dispose();
        _meleeImage?.Dispose();
        _archerImage?.Dispose();
        _tankImage?.Dispose();
        base.Dispose();
    }
}

class Player
{
    public float X { get; private set; }
    public float Y { get; private set; }
    public float Width { get; }
    public float Height { get; }
    public int MaxHealth { get; } = 100;
    public int Health { get; private set; } = 100;
    public float Speed { get; } = 220f;

    public Player(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public void Update(G2InputContext input, float deltaTime, int screenWidth)
    {
        bool left = input.IsKeyDown(Keys.A) || input.IsKeyPress(Keys.A);
        bool right = input.IsKeyDown(Keys.D) || input.IsKeyPress(Keys.D);
        if (left) X -= Speed * deltaTime;
        if (right) X += Speed * deltaTime;
        X = Math.Clamp(X, 0f, screenWidth - Width);
    }

    public void Draw(G2Texture image)
    {
        image.Draw(new Rect(X, Y, Width, Height), new Rect(0, 0, 192, 192));
    }
}

class Spear
{
    public bool IsHeld { get; private set; } = true;
    public float X { get; private set; }
    public float Y { get; private set; }

    public void Update(float deltaTime)
    {

    }

    public void DrawIcon(G2Texture image)
    {
        image.Draw(new Rect(716, 25, 225, 42), new Rect(0, 0, 512, 96));
    }
}

enum EnemyKind
{
    Melee,
    Archer,
    Tank
}

class Enemy
{
    public EnemyKind Kind { get; }
    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }
    public int Health { get; private set; }
    public G2Texture Image { get; }

    public Enemy(EnemyKind kind, G2Texture image, float x, float y,
                 float width, float height, int health)
    {
        Kind = kind;
        Image = image;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Health = health;
    }

    public void Update(float deltaTime)
    {

    }

    public void Draw()
    {
        Image.Draw(new Rect(X, Y, Width, Height), new Rect(0, 0, 192, 192));
    }
}

class Stage
{
    public const float GroundY = 528f;
    private readonly List<Enemy> _enemies = new();

    public int Number { get; private set; } = 1;
    public string Name => Number switch
    {
        1 => "일반 맵 1",
        2 => "일반 맵 2",
        3 => "보스 맵 (임시)",
        _ => "알 수 없음"
    };

    public void Load(int number, G2Texture melee, G2Texture archer, G2Texture tank)
    {
        Number = number;
        _enemies.Clear();
        if (number == 1)
        {
            _enemies.Add(new Enemy(EnemyKind.Melee, melee, 500, GroundY - 147, 110, 147, 50));
            _enemies.Add(new Enemy(EnemyKind.Archer, archer, 740, GroundY - 147, 110, 147, 35));
        }
        else if (number == 2)
        {
            _enemies.Add(new Enemy(EnemyKind.Melee, melee, 400, GroundY - 145, 108, 145, 50));
            _enemies.Add(new Enemy(EnemyKind.Archer, archer, 595, GroundY - 145, 108, 145, 35));
            _enemies.Add(new Enemy(EnemyKind.Tank, tank, 777, GroundY - 155, 119, 155, 125));
        }
    }

    public void Update(float deltaTime)
    {
        foreach (Enemy enemy in _enemies)
            enemy.Update(deltaTime);
    }

    public void DrawFloor(ID2D1HwndRenderTarget target,
                          ID2D1SolidColorBrush floorBrush,
                          ID2D1SolidColorBrush platformBrush)
    {
        target.FillRectangle(new Rect(0, GroundY, 960, 112), floorBrush);
        target.FillRectangle(new Rect(0, GroundY, 960, 9), platformBrush);
    }

    public void DrawEnemies()
    {
        foreach (Enemy enemy in _enemies)
            enemy.Draw();
    }
}
