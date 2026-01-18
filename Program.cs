using Raycaster;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;
using System.Windows.Forms;

class Program : Form
{
    const int ScreenWidth = 960;
    const int ScreenHeight = 640;
    Random rng = new Random();


    bool attacking;
    int attackCooldown = 0;
    const int AttackCooldownFrames = 5; // ~0.3s

    Bitmap framebuffer = new Bitmap(ScreenWidth, ScreenHeight);
    System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

    // Generate Random Map
    int[,] map = Extensions.BuildRaycasterMap(new Maze(5, 5), 5, 5);

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
        //Random rng = new Random();
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
        if (key == Keys.Space) attacking = down;
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

        if (attacking && attackCooldown == 0)
        {
            PerformMeleeAttack();
            attackCooldown = AttackCooldownFrames;

            // Start swing animation
            player.AttackFrame = 1; // start first frame
        }

        // Advance axe swing animation
        if (player.AttackFrame > 0)
        {
            player.AttackFrame++;
            if (player.AttackFrame > Player.MaxAttackFrames)
                player.AttackFrame = 0;
        }


        if (attackCooldown > 0)
            attackCooldown--;

        if (attacking && attackCooldown == 0)
        {
            PerformMeleeAttack();
            attackCooldown = AttackCooldownFrames;
        }

        foreach (var enemy in player.enemies.OfType<SkeletonEnemy>())
        {
            // Trigger explosion if half bones are broken
            if (!enemy.Exploding && enemy.Bones.Count(b => b.Broken) >= enemy.Bones.Count / 5)
            {
                enemy.Exploding = true;
                enemy.ExplosionFrames = 0;
            }

            // Animate exploding bones
            if (enemy.Exploding)
            {
                enemy.ExplosionFrames++;

                foreach (var bone in enemy.Bones.Where(b => b.Broken))
                {
                    if (bone.Velocity == Vector2.Zero)
                    {
                        // Assign random outward velocity
                        double angle = rng.NextDouble() * Math.PI * 2;
                        float speed = 0.3f + (float)rng.NextDouble() * 0.3f;
                        bone.Velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);
                    }

                    // Move bone outward
                    bone.LocalStart += bone.Velocity;
                    bone.LocalEnd += bone.Velocity;

                    // Shrink over time
                    bone.Thickness *= 0.95f;
                }

                // Remove skeleton after animation completes
                if (enemy.ExplosionFrames > SkeletonEnemy.MaxExplosionFrames)
                {
                    enemy.Bones.Clear();
                }
            }
        }

    }

    void PerformMeleeAttack()
    {
        const double attackRange = 1.9; // axe reach
        const double attackAngle = 0.7; // radians (~20 degrees)

        SkeletonEnemy? closest = null;
        double closestDist = double.MaxValue;

        foreach (var enemy in player.enemies)
        {
            if (!enemy.Alive) continue;

            double dx = enemy.X - player.posX;
            double dy = enemy.Y - player.posY;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist > attackRange) continue;

            // Normalize vector to enemy
            double nx = dx / dist;
            double ny = dy / dist;

            // Dot product to check "in front"
            double dot = nx * player.dirX + ny * player.dirY;

            if (dot < Math.Cos(attackAngle)) continue;

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy as SkeletonEnemy;
            }
        }

        if (closest != null)
            DamageSkeleton(closest);
    }

    void DamageSkeleton(SkeletonEnemy skeleton)
    {
        // Prefer core bones first
        var targetBones = skeleton.Bones
            .Where(b => !b.Broken)
            .OrderByDescending(b => b.IsCore)
            .ToList();

        if (targetBones.Count == 0)
            return;

        Random rng = new Random();
        int breaks = rng.Next(5, 10); 

        foreach (var bone in targetBones.Take(breaks))
            bone.Broken = true;
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
        g.FillRectangle(new SolidBrush(Color.FromArgb(255, 20, 0, 40)), 0, 0, ScreenWidth, ScreenHeight);

        float horizonY = 50;
        float vanishingX = ScreenWidth - 20;

        int glowSteps = 3;
        Random rand = new Random(123);

        // Helper: draw a glowing line
        void DrawGlowLine(PointF a, PointF b, Color color, int steps = 3)
        {
            for (int i = steps; i >= 1; i--)
            {
                using var glowPen = new Pen(Color.FromArgb(color.A / (i + 1), color.R, color.G, color.B), i * 2);
                g.DrawLine(glowPen, a, b);
            }
            using var pen = new Pen(color, 1);
            g.DrawLine(pen, a, b);
        }

        Color baseColor = Color.FromArgb(180, 0, 200, 0); // 80s terminal green

        // Horizontal lines (bottom to horizon) with varying spacing
        float y = ScreenHeight;
        while (y >= horizonY)
        {
            float t = (ScreenHeight - y) / (ScreenHeight - horizonY);
            float leftX = vanishingX - 2 * vanishingX * (1 - t);
            float rightX = ScreenWidth;

            // Wavy effect using sine
            float waveAmplitude = 5f; // max horizontal offset
            float waveFrequency = 0.05f;
            PointF start = new PointF(leftX + (float)Math.Sin(y * waveFrequency) * waveAmplitude, y);
            PointF end = new PointF(rightX + (float)Math.Sin(y * waveFrequency + 1) * waveAmplitude, y);

            DrawGlowLine(start, end, baseColor, glowSteps);

            // Vary spacing between lines randomly
            y -= 10 + rand.Next(0, 10); // 10–20px spacing
        }

        // Vertical lines converging to vanishing point, also wavy
        float xSpacing = 15;
        for (float i = -ScreenWidth; i <= ScreenWidth * 2; i += xSpacing + rand.Next(0, 5))
        {
            float startX = i + (float)Math.Sin(i * 0.02f) * 5f; // wavy horizontal offset
            float startY = ScreenHeight;
            DrawGlowLine(new PointF(startX, startY), new PointF(vanishingX, horizonY), baseColor, glowSteps);
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

        Random rand = new Random(123); // deterministic skyline

        // Define layers: back, mid, front
        var layers = new (int count, float alpha, int minHeight, int maxHeight)[]
        {
        (10, 100, 80, 150),   // distant buildings: dimmer, small
        (12, 160, 120, 220),  // mid layer
        (8, 200, 180, 300)    // foreground: tall, bright
        };

        foreach (var layer in layers)
        {
            for (int i = 0; i < layer.count; i++)
            {
                // Random horizontal position
                float baseX = rand.Next(0, ScreenWidth);

                // Random width and height
                float width = rand.Next(20, 50);
                float height = rand.Next(layer.minHeight, layer.maxHeight);

                float horizonY = ScreenHeight / 2;
                float topY = horizonY - height;
                float leftX = baseX;
                float rightX = baseX + width;

                // Glow helper
                void DrawGlowLine(PointF a, PointF b, Color c, int glowWidth = 3)
                {
                    for (int gStep = glowWidth; gStep > 0; gStep--)
                    {
                        using var glowPen = new Pen(Color.FromArgb((c.A / (gStep + 1)), c.R, c.G, c.B), gStep * 2);
                        g.DrawLine(glowPen, a, b);
                    }
                    using var pen = new Pen(c, 1);
                    g.DrawLine(pen, a, b);
                }

                // Draw building outline with glow
                DrawGlowLine(new PointF(leftX, topY), new PointF(rightX, topY), Color.FromArgb((int)layer.alpha, 255, 215, 0));
                DrawGlowLine(new PointF(leftX, topY), new PointF(leftX, horizonY), Color.FromArgb((int)layer.alpha, 255, 215, 0));
                DrawGlowLine(new PointF(rightX, topY), new PointF(rightX, horizonY), Color.FromArgb((int)layer.alpha, 255, 215, 0));
                DrawGlowLine(new PointF(leftX, horizonY), new PointF(rightX, horizonY), Color.FromArgb((int)layer.alpha, 255, 215, 0));

                // Draw window grid with glow
                int rows = Math.Max(3, (int)(height / 20));
                int cols = Math.Max(2, (int)(width / 10));
                float rowHeight = height / rows;
                float colWidth = width / cols;

                for (int r = 1; r < rows; r++)
                {
                    float y = topY + r * rowHeight;
                    float flicker = 0.8f + 0.2f * (float)Math.Sin(Environment.TickCount * 0.005 + i);
                    Color windowColor = Color.FromArgb((int)(layer.alpha * flicker), 255, 255, 180);
                    DrawGlowLine(new PointF(leftX, y), new PointF(rightX, y), windowColor);
                }

                for (int c = 1; c < cols; c++)
                {
                    float x = leftX + c * colWidth;
                    float flicker = 0.8f + 0.2f * (float)Math.Sin(Environment.TickCount * 0.005 + i * 2);
                    Color windowColor = Color.FromArgb((int)(layer.alpha * flicker), 255, 255, 180);
                    DrawGlowLine(new PointF(x, topY), new PointF(x, topY + height), windowColor);
                }
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

    void DrawPlayerAxe(Graphics g)
    {
        if (player.AttackFrame == 0) return;

        int w = framebuffer.Width;
        int h = framebuffer.Height;

        float t = player.AttackFrame / (float)Player.MaxAttackFrames;

        // Big brutal swing arc
        float angle = -1.2f + 2.4f * t;

        // Pivot at bottom center
        PointF pivot = new(w / 2, h - 40);

        float haftLength = 340;
        float haftWidth = 18;

        // Shaft direction
        float dx = (float)Math.Sin(angle);
        float dy = -(float)Math.Cos(angle);

        PointF head = new(
            pivot.X + dx * haftLength,
            pivot.Y + dy * haftLength
        );

        // Draw wooden haft
        using (var pen = new Pen(Color.SaddleBrown, haftWidth)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        })
        {
            g.DrawLine(pen, pivot, head);
        }

        // Perpendicular for blade width
        float px = -dy;
        float py = dx;

        // ---- MAIN BLADE (crescent) ----
        PointF[] frontBlade =
        {
        new(head.X + px * 90 - dx * 20, head.Y + py * 90 - dy * 20),
        new(head.X + px * 50 - dx * 70, head.Y + py * 50 - dy * 70),
        new(head.X - px * 50 - dx * 70, head.Y - py * 50 - dy * 70),
        new(head.X - px * 90 - dx * 20, head.Y - py * 90 - dy * 20),
        new(head.X - px * 20,            head.Y - py * 20),
        new(head.X + px * 20,            head.Y + py * 20)
    };

        // ---- REAR SPIKE / HOOK ----
        PointF[] rearBlade =
        {
        new(head.X + px * 35 + dx * 20, head.Y + py * 35 + dy * 20),
        new(head.X + dx * 90,           head.Y + dy * 90),
        new(head.X - px * 35 + dx * 20, head.Y - py * 35 + dy * 20)
    };

        // Glow underlayer
        using (var glow = new SolidBrush(Color.FromArgb(80, 255, 120, 0)))
        {
            g.FillPolygon(glow, frontBlade);
            g.FillPolygon(glow, rearBlade);
        }

        // Blade metal
        using (var steel = new SolidBrush(Color.Orange))
        {
            g.FillPolygon(steel, frontBlade);
            g.FillPolygon(steel, rearBlade);
        }

        // Blade edge highlight
        using (var edge = new Pen(Color.Yellow, 6))
        {
            g.DrawPolygon(edge, frontBlade);
            g.DrawPolygon(edge, rearBlade);
        }

        // Impact arc
        using var arc = new Pen(Color.FromArgb(80, 255, 140, 0), 22);
        g.DrawArc(
            arc,
            pivot.X - 400,
            pivot.Y - 400,
            800,
            800,
            angle * 180 / (float)Math.PI - 25,
            50
        );
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

            DrawPlayerAxe(g);
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
