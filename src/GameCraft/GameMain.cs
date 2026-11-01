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

    private G2Texture? _outerBackground;
    private G2Texture? _innerBackground;
    private G2Texture? _throneBackground;

    private Rect _outerSource;
    private Rect _innerSource;
    private Rect _throneSource;

    private G2Audio? _battleBgm;
    private G2Audio? _bossBgm;

    private int _playingMusicKind;

    private G2Font? _font;
    private ID2D1SolidColorBrush? _hudBrush;

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

        (_outerBackground, _outerSource) =
            LoadBackground("Assets/background/out_castle.jpg");

        (_innerBackground, _innerSource) =
            LoadBackground("Assets/background/in_castle.jpg");

        (_throneBackground, _throneSource) =
            LoadBackground("Assets/background/throne.jpg");

        _battleBgm = new G2Audio("Assets/bgm/battle_bgm.wav");
        _bossBgm = new G2Audio("Assets/bgm/boss_battle_bgm.wav");

        _font = new G2Font("Malgun Gothic", 18f);
        _hudBrush = RenderTarget.CreateSolidColorBrush(
            new Color4(0f, 0f, 0f, 0.65f));

        _player = new Player(
            95f, Stage.GroundY - 158f, 110f, 158f);

        _spear = new Spear();
        _stage = new Stage();

        ChangeStage(1);
    }

    private static (G2Texture Texture, Rect Source)
        LoadBackground(string path)
    {
        using System.Drawing.Image info =
            System.Drawing.Image.FromFile(
                G2Util.FindFilePath(path));

        return (
            new G2Texture(path),
            new Rect(0, 0, info.Width, info.Height)
        );
    }

    private void ChangeStage(int number)
    {
        if (_stage == null ||
            _meleeImage == null ||
            _archerImage == null ||
            _tankImage == null)
            return;

        if (number < 1 || number > 3)
            return;

        _stage.Load(
            number,
            _meleeImage,
            _archerImage,
            _tankImage
        );

        int neededMusic = number == 3 ? 2 : 1;

        if (_playingMusicKind != neededMusic)
        {
            _battleBgm?.Stop();
            _bossBgm?.Stop();

            if (neededMusic == 1)
            {
                _battleBgm?.Play(true);
            }
            else
            {
                _bossBgm?.Play(true);
            }

            _playingMusicKind = neededMusic;
        }
    }

    protected override void Update()
    {
        float dt = (float)Math.Min(DeltaTime, 0.05);

        if (Input.IsKeyDown(Keys.Escape))
        {
            Close();
            return;
        }

        if (Input.IsKeyDown(Keys.D1) ||
            Input.IsKeyDown(Keys.NumPad1))
        {
            ChangeStage(1);
        }
        else if (Input.IsKeyDown(Keys.D2) ||
                 Input.IsKeyDown(Keys.NumPad2))
        {
            ChangeStage(2);
        }
        else if (Input.IsKeyDown(Keys.D3) ||
                 Input.IsKeyDown(Keys.NumPad3))
        {
            ChangeStage(3);
        }

        _player?.Update(Input, dt, ScreenSize.Width);
        _stage?.Update(dt);
        _spear?.Update(dt);
    }

    protected override void Render()
    {
        if (_playerImage == null ||
            _spearImage == null ||
            _stage == null ||
            _player == null ||
            _spear == null ||
            _font == null ||
            _hudBrush == null)
        {
            return;
        }

        G2Texture? background = _stage.Number switch
        {
            1 => _outerBackground,
            2 => _innerBackground,
            3 => _throneBackground,
            _ => null
        };

        Rect source = _stage.Number switch
        {
            1 => _outerSource,
            2 => _innerSource,
            3 => _throneSource,
            _ => new Rect(0, 0, 960, 640)
        };

        background?.Draw(
            new Rect(
                0,
                0,
                ScreenSize.Width,
                ScreenSize.Height
            ),
            source
        );

        _stage.DrawEnemies();

        _player.Draw(_playerImage);

        _spear.DrawIcon(_spearImage);

        RenderTarget.FillRectangle(
            new Rect(0, 0, 960, 120),
            _hudBrush
        );

        Color4 white =
            new Color4(0.96f, 0.96f, 0.93f, 1f);

        Color4 muted =
            new Color4(0.76f, 0.82f, 0.88f, 1f);

        _font.DrawText(
            "RE:SPEAR  |  3차 과제",
            new Rect(22, 12, 520, 32),
            white
        );

        _font.DrawText(
            $"맵: {_stage.Name}    HP: {_player.Health}/{_player.MaxHealth}",
            new Rect(22, 47, 720, 32),
            muted
        );

        _font.DrawText(
            "A / D: 이동    1 / 2 / 3: 맵 전환    ESC: 종료",
            new Rect(22, 82, 800, 30),
            muted
        );

        string bgmName = _stage.Number == 3
            ? "BGM: 보스 전투"
            : "BGM: 일반 전투";

        _font.DrawText(
            bgmName,
            new Rect(740, 15, 210, 30),
            white
        );
    }

    public override void Dispose()
    {
        _battleBgm?.Stop();
        _bossBgm?.Stop();

        _battleBgm?.Dispose();
        _bossBgm?.Dispose();

        _hudBrush?.Dispose();
        _font?.Dispose();

        _outerBackground?.Dispose();
        _innerBackground?.Dispose();
        _throneBackground?.Dispose();

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

    public Player(
        float x,
        float y,
        float width,
        float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public void Update(
        G2InputContext input,
        float deltaTime,
        int screenWidth)
    {
        bool left =
            input.IsKeyDown(Keys.A) ||
            input.IsKeyPress(Keys.A);

        bool right =
            input.IsKeyDown(Keys.D) ||
            input.IsKeyPress(Keys.D);

        if (left)
            X -= Speed * deltaTime;

        if (right)
            X += Speed * deltaTime;

        X = Math.Clamp(
            X,
            0f,
            screenWidth - Width
        );
    }

    public void Draw(G2Texture image)
    {
        image.Draw(
            new Rect(X, Y, Width, Height),
            new Rect(0, 0, 192, 192)
        );
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
        image.Draw(
            new Rect(716, 125, 225, 42),
            new Rect(0, 0, 512, 96)
        );
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

    public Enemy(
        EnemyKind kind,
        G2Texture image,
        float x,
        float y,
        float width,
        float height,
        int health)
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
        Image.Draw(
            new Rect(X, Y, Width, Height),
            new Rect(0, 0, 192, 192)
        );
    }
}

class Stage
{
    public const float GroundY = 528f;

    private readonly List<Enemy> _enemies = new();

    public int Number { get; private set; }

    public string Name => Number switch
    {
        1 => "외성",
        2 => "내성",
        3 => "왕좌의 방",
        _ => "알 수 없음"
    };

    public void Load(
        int number,
        G2Texture melee,
        G2Texture archer,
        G2Texture tank)
    {
        Number = number;
        _enemies.Clear();

        if (number == 1)
        {
            _enemies.Add(new Enemy(
                EnemyKind.Melee,
                melee,
                500,
                GroundY - 147,
                110,
                147,
                50
            ));

            _enemies.Add(new Enemy(
                EnemyKind.Archer,
                archer,
                740,
                GroundY - 147,
                110,
                147,
                35
            ));
        }
        else if (number == 2)
        {
            _enemies.Add(new Enemy(
                EnemyKind.Melee,
                melee,
                400,
                GroundY - 145,
                108,
                145,
                50
            ));

            _enemies.Add(new Enemy(
                EnemyKind.Archer,
                archer,
                595,
                GroundY - 145,
                108,
                145,
                35
            ));

            _enemies.Add(new Enemy(
                EnemyKind.Tank,
                tank,
                777,
                GroundY - 155,
                119,
                155,
                125
            ));
        }

    }

    public void Update(float deltaTime)
    {
        foreach (Enemy enemy in _enemies)
        {
            enemy.Update(deltaTime);
        }
    }

    public void DrawEnemies()
    {
        foreach (Enemy enemy in _enemies)
        {
            enemy.Draw();
        }
    }
}
