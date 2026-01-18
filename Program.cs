using Raycaster;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

class Program : Form
{
    const int ScreenWidth = 960;
    const int ScreenHeight = 640;

    Bitmap framebuffer = new Bitmap(ScreenWidth, ScreenHeight);
    System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

    // Generate Random Map
    int[,] map = Extensions.BuildRaycasterMap(new Maze(15, 15), 15, 15);

    HudRenderer hud = new HudRenderer(ScreenWidth, ScreenHeight, 100);

    Player player = new Player
    {
        posX = 0.5,
        posY = 0.5,
        dirX = 1,
        dirY = 0,
        planeX = 0,
        planeY = 0.66 // standard FOV
    };

    bool left, right, forward, backward;

    [STAThread]
    static void Main()
    {
        Application.Run(new Program());
    }

    public Program()
    {
        Text = "Minimal Raycaster - Synthwave";
        ClientSize = new Size(ScreenWidth, ScreenHeight);
        DoubleBuffered = true;

        timer.Interval = 16;
        timer.Tick += (s, e) => { UpdateGame(); Render(); Invalidate(); };
        timer.Start();

        KeyDown += (s, e) => SetKey(e.KeyCode, true);
        KeyUp += (s, e) => SetKey(e.KeyCode, false);

        // Player start
        for (int y = 0; y < map.GetLength(0); y++)
        {
            for (int x = 0; x < map.GetLength(1); x++)
            {
                if (map[y, x] == 0)
                {
                    player.posX = x + 0.5;
                    player.posY = y + 0.5;
                    break;
                }
            }
        }

        // Add 5 enemies in walkable tiles
        Random rng = new Random();
        int enemyCount = 15;
        for (int i = 0; i < enemyCount; i++)
        {
            int ex, ey;
            do
            {
                ex = rng.Next(map.GetLength(1));
                ey = rng.Next(map.GetLength(0));
            } while (map[ey, ex] != 0);
            player.enemies.Add(new SkeletonEnemy(ex + 0.5, ey + 0.5));
        }
    }

    void SetKey(Keys key, bool down)
    {
        if (key == Keys.A) left = down;
        if (key == Keys.D) right = down;
        if (key == Keys.W) forward = down;
        if (key == Keys.S) backward = down;
    }

    void UpdateGame()
    {
        double moveSpeed = 0.45;
        double rotSpeed = 0.25;

        if (forward)
        {
            if (map[(int)player.posY, (int)(player.posX + player.dirX * moveSpeed)] == 0)
                player.posX += player.dirX * moveSpeed;
            if (map[(int)(player.posY + player.dirY * moveSpeed), (int)player.posX] == 0)
                player.posY += player.dirY * moveSpeed;
        }

        if (backward)
        {
            if (map[(int)player.posY, (int)(player.posX - player.dirX * moveSpeed)] == 0)
                player.posX -= player.dirX * moveSpeed;
            if (map[(int)(player.posY - player.dirY * moveSpeed), (int)player.posX] == 0)
                player.posY -= player.dirY * moveSpeed;
        }

        if (left || right)
        {
            double rot = 0;
            if (left) rot -= rotSpeed;
            if (right) rot += rotSpeed;

            double oldDirX = player.dirX;
            player.dirX = player.dirX * Math.Cos(rot) - player.dirY * Math.Sin(rot);
            player.dirY = oldDirX * Math.Sin(rot) + player.dirY * Math.Cos(rot);

            double oldPlaneX = player.planeX;
            player.planeX = player.planeX * Math.Cos(rot) - player.planeY * Math.Sin(rot);
            player.planeY = oldPlaneX * Math.Sin(rot) + player.planeY * Math.Cos(rot);

            // Normalize to prevent drift
            double len = Math.Sqrt(player.dirX * player.dirX + player.dirY * player.dirY);
            player.dirX /= len;
            player.dirY /= len;
            len = Math.Sqrt(player.planeX * player.planeX + player.planeY * player.planeY);
            player.planeX /= len;
            player.planeY /= len;
        }
    }
    void RenderSprites(Graphics g, double[] zBuffer)
    {
        foreach (var enemy in player.enemies)
        {
            if (!enemy.Alive) continue;
            int mx = (int)enemy.X;
            int my = (int)enemy.Y;
            if (map[my, mx] != 0) continue;

            double spriteX = enemy.X - player.posX;
            double spriteY = enemy.Y - player.posY;

            double invDet = 1.0 / (player.planeX * player.dirY - player.dirX * player.planeY);
            double transformX = invDet * (player.dirY * spriteX - player.dirX * spriteY);
            double transformY = invDet * (-player.planeY * spriteX + player.planeX * spriteY);

            if (transformY <= 0.5) continue;

            int spriteScreenX = (int)((ScreenWidth / 2) * (1 + transformX / transformY));
            if (spriteScreenX < 0 || spriteScreenX >= ScreenWidth) continue;
            if (transformY >= zBuffer[spriteScreenX]) continue;

            if (enemy is SkeletonEnemy skeleton)
            {
                SkeletonRenderer.Render(g, skeleton, player, zBuffer, ScreenWidth, ScreenHeight);
            }
        }
    }
    void DrawFloor(Graphics g)
    {
        // Base fill: dark purple
        g.FillRectangle(new SolidBrush(Color.FromArgb(20, 0, 40)), 0, 0, ScreenWidth, ScreenHeight);

        // Neon cyan grid
        using var gridPen = new Pen(Color.FromArgb(150, 0, 255, 255), 1); // cyan with some glow

        int gridSpacing = 30;

        // Vanishing point at top-right
        float horizonY = 50;
        float vanishingX = ScreenWidth - 20;

        // Horizontal lines (bottom to horizon, left to right covering more)
        for (float y = ScreenHeight; y >= horizonY; y -= gridSpacing)
        {
            float t = (ScreenHeight - y) / (ScreenHeight - horizonY); // 0 = bottom, 1 = horizon
                                                                      // Extend left beyond screen (twice the distance to left)
            float leftX = vanishingX - 2 * vanishingX * (1 - t);
            float rightX = ScreenWidth;
            g.DrawLine(gridPen, leftX, y, rightX, y);
        }

        // Vertical lines converging to vanishing point
        int numLines = ScreenWidth / gridSpacing;
        for (int i = -numLines; i <= numLines; i++)
        {
            float startX = i * gridSpacing;
            float startY = ScreenHeight;
            g.DrawLine(gridPen, startX, startY, vanishingX, horizonY);
        }
    }

    void DrawCeiling(Graphics g)
    {
        // Base gradient for ceiling
        using var brush = new LinearGradientBrush(
            new Point(0, 0),
            new Point(0, ScreenHeight / 2),
            Color.FromArgb(255, 20, 0, 50),
            Color.FromArgb(255, 255, 20, 120)
        );
        g.FillRectangle(brush, 0, 0, ScreenWidth, ScreenHeight / 2);

        // Golden city skyline
        using var buildingPen = new Pen(Color.FromArgb(200, 255, 215, 0), 1); // golden
        using var windowPen = new Pen(Color.FromArgb(150, 255, 255, 180), 1);  // lighter windows

        int numBuildings = 20; // total buildings
        float horizonY = ScreenHeight / 2;

        Random rand = new Random(123); // deterministic skyline

        for (int i = 0; i < numBuildings; i++)
        {
            // Building position and size
            float baseX = rand.Next(0, ScreenWidth);
            float width = rand.Next(20, 50);
            float height = rand.Next(120, 300); // taller buildings
            float topY = horizonY - height;
            float leftX = baseX;
            float rightX = baseX + width;

            // Draw building rectangle
            g.DrawRectangle(buildingPen, leftX, topY, width, height);

            // Draw windows grid proportional to building height
            int rows = Math.Max(3, (int)(height / 20));  // vertical divisions
            int cols = Math.Max(2, (int)(width / 10));   // horizontal divisions
            float rowHeight = height / rows;
            float colWidth = width / cols;

            for (int r = 1; r < rows; r++)
            {
                float y = topY + r * rowHeight;
                g.DrawLine(windowPen, leftX, y, rightX, y); // horizontal window line
            }
            for (int c = 1; c < cols; c++)
            {
                float x = leftX + c * colWidth;
                g.DrawLine(windowPen, x, topY, x, topY + height); // vertical window line
            }
        }
    }


    void DrawHorizonGlow(Graphics g)
    {
        using var brush = new LinearGradientBrush(
            new Point(0, ScreenHeight / 2 - 20),
            new Point(0, ScreenHeight / 2 + 20),
            Color.FromArgb(100, 255, 0, 255),
            Color.FromArgb(0, 255, 0, 255)
        );
        g.FillRectangle(brush, 0, ScreenHeight / 2 - 20, ScreenWidth, 40);
    }

    void Render()
    {
        using (Graphics g = Graphics.FromImage(framebuffer))
        {
            double[] zBuffer = new double[ScreenWidth];
            g.Clear(Color.Black);
            
            DrawFloor(g);
            DrawCeiling(g);
            DrawHorizonGlow(g);

            for (int x = 0; x < ScreenWidth; x++)
            {
                double cameraX = 2 * x / (double)ScreenWidth - 1;
                double rayDirX = player.dirX + player.planeX * cameraX;
                double rayDirY = player.dirY + player.planeY * cameraX;

                int mapX = (int)player.posX;
                int mapY = (int)player.posY;

                double deltaDistX = rayDirX == 0 ? 1e30 : Math.Abs(1 / rayDirX);
                double deltaDistY = rayDirY == 0 ? 1e30 : Math.Abs(1 / rayDirY);

                int stepX, stepY;
                double sideDistX, sideDistY;

                if (rayDirX < 0) { stepX = -1; sideDistX = (player.posX - mapX) * deltaDistX; }
                else { stepX = 1; sideDistX = (mapX + 1.0 - player.posX) * deltaDistX; }

                if (rayDirY < 0) { stepY = -1; sideDistY = (player.posY - mapY) * deltaDistY; }
                else { stepY = 1; sideDistY = (mapY + 1.0 - player.posY) * deltaDistY; }

                bool hit = false;
                int side = 0;

                while (!hit)
                {
                    if (sideDistX < sideDistY)
                    {
                        sideDistX += deltaDistX;
                        mapX += stepX;
                        side = 0;
                    }
                    else
                    {
                        sideDistY += deltaDistY;
                        mapY += stepY;
                        side = 1;
                    }

                    if (mapX < 0 || mapY < 0 || mapX >= map.GetLength(1) || mapY >= map.GetLength(0) || map[mapY, mapX] > 0)
                        hit = true;
                }

                double perpWallDist = side == 0
                    ? (mapX - player.posX + (1 - stepX) / 2) / rayDirX
                    : (mapY - player.posY + (1 - stepY) / 2) / rayDirY;

                zBuffer[x] = perpWallDist;
                int lineHeight = (int)(ScreenHeight / perpWallDist);
                int drawStart = Math.Max(0, -lineHeight / 2 + ScreenHeight / 2);
                int drawEnd = Math.Min(ScreenHeight - 1, lineHeight / 2 + ScreenHeight / 2);

                // Hellish wall colors
                float depthFade = (float)Math.Clamp(1.0 / perpWallDist, 0.2f, 1.0f);

                // Dark hellish palette
                Color baseColor = side == 1
                    ? Color.FromArgb(255, 120, 20, 0)   // dark red / ember
                    : Color.FromArgb(255, 180, 80, 20); // molten orange / lava

                // subtle flicker (mostly solid, barely transparent)
                float flicker = 0.95f + 0.05f * (float)Math.Sin(Environment.TickCount * 0.006f);

                // Apply depth fade to RGB only (keep alpha mostly solid)
                baseColor = Color.FromArgb(
                    255, // alpha stays solid
                    (int)(baseColor.R * depthFade * flicker),
                    (int)(baseColor.G * depthFade * flicker),
                    (int)(baseColor.B * depthFade * flicker)
                );

                DrawGlowColumn(g, x, drawStart, drawEnd, baseColor);
            }

            RenderSprites(g, zBuffer);
            hud.Render(g, map, player);
        }
    }

    void DrawGlowColumn(Graphics g, int x, int y1, int y2, Color c)
    {
        for (int i = 4; i >= 1; i--)
        {
            using var glow = new Pen(Color.FromArgb(20, c), i * 2);
            g.DrawLine(glow, x, y1, x, y2);
        }
        using var pen = new Pen(c, 1);
        g.DrawLine(pen, x, y1, x, y2);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.DrawImage(framebuffer, 0, 0);
    }
}
