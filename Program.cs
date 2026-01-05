using System;
using System.Drawing;
using System.Windows.Forms;
using Raycaster;

class Program : Form
{
    const int ScreenWidth = 640;
    const int ScreenHeight = 480;

    Bitmap framebuffer = new Bitmap(ScreenWidth, ScreenHeight);
    System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

    //Generte Random Map
    int[,] map = Extensions.BuildRaycasterMap(new Maze(5, 5), 5, 5);

    HudRenderer hud = new HudRenderer(ScreenWidth, ScreenHeight, 100);

    Player player = new Player
    {
        posX = 0.5,
        posY = 0.5,
        dirX = 1,
        dirY = 0
    };

    bool left, right, forward, backward;

    Bitmap enemySprite = new Bitmap
    (
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "enemy_idle.png")
    );

    [STAThread]
    static void Main()
    {
        Application.Run(new Program());
    }

    public Program()
    {
        Text = "Minimal Raycaster";
        ClientSize = new Size(ScreenWidth, ScreenHeight);
        DoubleBuffered = true;

        timer.Interval = 16;
        timer.Tick += (s, e) => { UpdateGame(); Render(); Invalidate(); };
        timer.Start();

        KeyDown += (s, e) => SetKey(e.KeyCode, true);
        KeyUp += (s, e) => SetKey(e.KeyCode, false);

        //Dont start in a wall
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
        double moveSpeed = 0.08;
        double rotSpeed = 0.05;

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
        }
    }

    public void RenderSprites(Graphics g, double[] zBuffer)
    {
        foreach (var enemy in player.enemies)
        {
            if (!enemy.Alive) continue;

            // Enemy must be in an empty tile
            int mx = (int)enemy.X;
            int my = (int)enemy.Y;
            if (map[my, mx] != 0) continue;

            // Translate to camera space
            double spriteX = enemy.X - player.posX;
            double spriteY = enemy.Y - player.posY;

            // Inverse camera matrix
            double invDet = 1.0 / (player.planeX * player.dirY - player.dirX * player.planeY);

            double transformX = invDet * (player.dirY * spriteX - player.dirX * spriteY);
            double transformY = invDet * (-player.planeY * spriteX + player.planeX * spriteY);

            if (transformY <= 0) continue; // Behind camera

            int spriteScreenX = (int)((ScreenWidth / 2) * (1 + transformX / transformY));

            int spriteHeight = Math.Abs((int)(ScreenHeight / transformY));
            int drawStartY = -spriteHeight / 2 + ScreenHeight / 2; 
            int drawEndY = spriteHeight / 2 + ScreenHeight / 2;

            drawStartY = Math.Max(0, drawStartY);
            drawEndY = Math.Min(ScreenHeight - 1, drawEndY);

            int spriteWidth = spriteHeight;
            int drawStartX = -spriteWidth / 2 + spriteScreenX;
            int drawEndX = spriteWidth / 2 + spriteScreenX;

            Bitmap sprite = enemySprite;

            for (int stripe = drawStartX; stripe < drawEndX; stripe++)
            {
                if (stripe < 0 || stripe >= ScreenWidth) continue;
                if (transformY >= zBuffer[stripe]) continue; // Wall in front

                int texX = (int)((stripe - drawStartX) * sprite.Width / (double)spriteWidth);
                if (texX < 0 || texX >= sprite.Width) continue;

                for (int y = drawStartY; y < drawEndY; y++)
                {
                    int d = y * 256 - ScreenHeight * 128 + spriteHeight * 128;
                    int texY = ((d * sprite.Height) / spriteHeight) / 256;

                    if (texY < 0 || texY >= sprite.Height) continue;

                    spriteHeight = Math.Min(spriteHeight, ScreenHeight * 2);
                    spriteWidth = Math.Min(spriteWidth, ScreenWidth);

                    Color c = sprite.GetPixel(texX, texY);

                    // Skip transparent pixels
                    if (c.A < 128) continue;

                    framebuffer.SetPixel(stripe, y, c);
                }
            }



        }
    }

    void Render()
    {
        using (Graphics g = Graphics.FromImage(framebuffer))
        {
            double[] zBuffer = new double[ScreenWidth];

            g.Clear(Color.Black);

            // Ceiling
            g.FillRectangle(
                Brushes.DarkSlateBlue,
                0, 0,
                ScreenWidth, ScreenHeight / 2
            );

            // Floor
            g.FillRectangle(
                Brushes.DimGray,
                0, ScreenHeight / 2,
                ScreenWidth, ScreenHeight / 2
            );

            for (int x = 0; x < ScreenWidth; x++)
            {
                double cameraX = 2 * x / (double)ScreenWidth - 1;
                double rayDirX = player.dirX + player.planeX * cameraX;
                double rayDirY = player.dirY + player.planeY * cameraX;

                int mapX = (int)player.posX;
                int mapY = (int)player.posY;

                double deltaDistX = Math.Abs(1 / rayDirX);
                double deltaDistY = Math.Abs(1 / rayDirY);

                int stepX, stepY;
                double sideDistX, sideDistY;

                if (rayDirX < 0)
                {
                    stepX = -1;
                    sideDistX = (player.posX - mapX) * deltaDistX;
                }
                else
                {
                    stepX = 1;
                    sideDistX = (mapX + 1.0 - player.posX) * deltaDistX;
                }

                if (rayDirY < 0)
                {
                    stepY = -1;
                    sideDistY = (player.posY - mapY) * deltaDistY;
                }
                else
                {
                    stepY = 1;
                    sideDistY = (mapY + 1.0 - player.posY) * deltaDistY;
                }

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

                    if (mapX < 0 || mapY < 0 ||
                        mapX >= map.GetLength(1) ||
                        mapY >= map.GetLength(0) ||
                        map[mapY, mapX] > 0)
                    {
                        hit = true;
                    }

                }

                double perpWallDist =
                    side == 0
                        ? (mapX - player.posX + (1 - stepX) / 2) / rayDirX
                        : (mapY - player.posY + (1 - stepY) / 2) / rayDirY;

                zBuffer[x] = perpWallDist;

                int lineHeight = (int)(ScreenHeight / perpWallDist);
                int drawStart = -lineHeight / 2 + ScreenHeight / 2;
                int drawEnd = lineHeight / 2 + ScreenHeight / 2;

                drawStart = Math.Max(0, drawStart);
                drawEnd = Math.Min(ScreenHeight - 1, drawEnd);

                Color wallColor = side == 1 ? Color.DarkRed : Color.Red;
                using (Pen p = new Pen(wallColor))
                    g.DrawLine(p, x, drawStart, x, drawEnd);               
            }

            RenderSprites(g, zBuffer);

            hud.Render(g, map, player);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.DrawImage(framebuffer, 0, 0);
    }
}
