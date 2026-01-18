using Raycaster;
using System;
using System.Drawing;
using System.Numerics;

public static class SkeletonRenderer
{
    public static void Render(
        Graphics g,
        SkeletonEnemy enemy,
        Player player,
        double[] zBuffer,
        int screenWidth,
        int screenHeight)
    {
        double spriteX = enemy.X - player.posX;
        double spriteY = enemy.Y - player.posY;

        double invDet = 1.0 /
            (player.planeX * player.dirY - player.dirX * player.planeY);

        double transformX =
            invDet * (player.dirY * spriteX - player.dirX * spriteY);
        double transformY =
            invDet * (-player.planeY * spriteX + player.planeX * spriteY);

        if (transformY <= 0.5) return;

        int screenX = (int)((screenWidth / 2) * (1 + transformX / transformY));
        if (screenX < 0 || screenX >= screenWidth) return;
        if (transformY >= zBuffer[screenX]) return;

        foreach (var b in enemy.Bones)
        {
            Vector2 ws = b.Broken ? b.WorldStart : b.LocalStart + enemy.Position;
            Vector2 we = b.Broken ? b.WorldEnd : b.LocalEnd + enemy.Position;

            PointF a = new(
                screenX + (float)(ws.X / transformY),
                screenHeight / 2 + (float)(ws.Y / transformY));

            PointF e = new(
                screenX + (float)(we.X / transformY),
                screenHeight / 2 + (float)(we.Y / transformY));

            DrawGlowLine(g, a, e, b.Thickness, Color.Red);
        }
    }

    static void DrawGlowLine(
        Graphics g,
        PointF a,
        PointF b,
        float width,
        Color c)
    {
        for (int i = 3; i >= 1; i--)
        {
            using var glow =
                new Pen(Color.FromArgb(30, c), width + i * 2);
            g.DrawLine(glow, a, b);
        }

        using var pen = new Pen(c, width);
        g.DrawLine(pen, a, b);
    }
}
