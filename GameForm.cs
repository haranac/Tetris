using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MiniTetris;

public class GameForm : Form
{
    private const int BoardWidth = 10;
    private const int BoardHeight = 20;
    private const int CellSize = 28;

    private const int BoardOffsetX = 20;
    private const int BoardOffsetY = 20;

    private readonly int[,] board = new int[BoardWidth, BoardHeight];
    private readonly System.Windows.Forms.Timer gameTimer = new();
    private readonly Random random = new();

    private readonly Color[] pieceColors =
    {
        Color.Cyan, //Salva partidas, el elegido, el mítico, el palo
        Color.Gold,
        Color.MediumPurple,
        Color.Orange,
        Color.RoyalBlue,
        Color.LimeGreen,
        Color.Red
    };

    private Point[] currentBlocks = Array.Empty<Point>();

    private int currentPieceType;
    private int currentX;
    private int currentY;

    private int score;
    private int clearedLines;

    private bool gameOver;
    private bool paused;

    public GameForm()
    {
        Text = "TetriZZZ";
        ClientSize = new Size(520, 620);
        BackColor = Color.FromArgb(15, 23, 42);

        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        KeyPreview = true;
        DoubleBuffered = true;

        gameTimer.Interval = 450;
        gameTimer.Tick += GameTimer_Tick;

        NewGame();
    }

    private void NewGame()
    {
        Array.Clear(board);

        score = 0;
        clearedLines = 0;
        gameOver = false;
        paused = false;

        gameTimer.Interval = 450;

        CreateNewPiece();
        gameTimer.Start();

        Invalidate();
    }

    private void CreateNewPiece()
    {
        currentPieceType = random.Next(0, 7);
        currentBlocks = GetShape(currentPieceType);

        currentX = BoardWidth / 2;
        currentY = 1;

        if (!IsValidPosition(currentBlocks, currentX, currentY))
        {
            gameOver = true;
            gameTimer.Stop();
        }
    }

    private static Point[] GetShape(int pieceType)
    {
        return pieceType switch
        {
            // Pieza larga y estirada
            0 =>
            [
                new Point(-1, 0),
                new Point(0, 0),
                new Point(1, 0),
                new Point(2, 0)
            ],

            // Pieza cuadrada
            1 =>
            [
                new Point(0, 0),
                new Point(1, 0),
                new Point(0, 1),
                new Point(1, 1)
            ],

            // Pieza T
            2 =>
            [
                new Point(-1, 0),
                new Point(0, 0),
                new Point(1, 0),
                new Point(0, 1)
            ],

            // Pieza L
            3 =>
            [
                new Point(-1, 0),
                new Point(0, 0),
                new Point(1, 0),
                new Point(1, 1)
            ],

            // Pieza indescriptible
            4 =>
            [
                new Point(-1, 0),
                new Point(0, 0),
                new Point(1, 0),
                new Point(-1, 1)
            ],

            // Pieza roja medio inútil
            5 =>
            [
                new Point(0, 0),
                new Point(1, 0),
                new Point(-1, 1),
                new Point(0, 1)
            ],

            // Pieza verde medio inútil
            _ =>
            [
                new Point(-1, 0),
                new Point(0, 0),
                new Point(0, 1),
                new Point(1, 1)
            ]
        };
    }

    private void GameTimer_Tick(object? sender, EventArgs e)
    {
        if (paused || gameOver)
        {
            return;
        }

        MoveDown();
    }

    private void MoveDown()
    {
        if (IsValidPosition(currentBlocks, currentX, currentY + 1))
        {
            currentY++;
        }
        else
        {
            LockPiece();
            ClearCompletedLines();
            CreateNewPiece();
        }

        Invalidate();
    }

    private void MoveHorizontal(int direction)
    {
        int newX = currentX + direction;

        if (IsValidPosition(currentBlocks, newX, currentY))
        {
            currentX = newX;
            Invalidate();
        }
    }

    private void RotatePiece()
    {
        // La pieza cuadrada no gira pues porque los cuadrados no giran XD
        if (currentPieceType == 1)
        {
            return;
        }

        Point[] rotatedBlocks = currentBlocks
            .Select(block => new Point(-block.Y, block.X))
            .ToArray();

        if (IsValidPosition(rotatedBlocks, currentX, currentY))
        {
            currentBlocks = rotatedBlocks;
            Invalidate();
            return;
        }

        // Mover pieza si está pegada a la pared / borde
        if (IsValidPosition(rotatedBlocks, currentX - 1, currentY))
        {
            currentX--;
            currentBlocks = rotatedBlocks;
            Invalidate();
            return;
        }

        if (IsValidPosition(rotatedBlocks, currentX + 1, currentY))
        {
            currentX++;
            currentBlocks = rotatedBlocks;
            Invalidate();
        }
    }

    private void HardDrop()
    {
        while (IsValidPosition(currentBlocks, currentX, currentY + 1))
        {
            currentY++;
            score += 2;
        }

        LockPiece();
        ClearCompletedLines();
        CreateNewPiece();

        Invalidate();
    }

    private bool IsValidPosition(Point[] blocks, int positionX, int positionY)
    {
        foreach (Point block in blocks)
        {
            int boardX = positionX + block.X;
            int boardY = positionY + block.Y;

            if (boardX < 0 || boardX >= BoardWidth)
            {
                return false;
            }

            if (boardY < 0 || boardY >= BoardHeight)
            {
                return false;
            }

            if (board[boardX, boardY] != 0)
            {
                return false;
            }
        }

        return true;
    }

    private void LockPiece()
    {
        foreach (Point block in currentBlocks)
        {
            int boardX = currentX + block.X;
            int boardY = currentY + block.Y;

            if (
                boardX >= 0 &&
                boardX < BoardWidth &&
                boardY >= 0 &&
                boardY < BoardHeight
            )
            {
                board[boardX, boardY] = currentPieceType + 1;
            }
        }
    }

    private void ClearCompletedLines()
    {
        int linesRemoved = 0;

        for (int row = BoardHeight - 1; row >= 0; row--)
        {
            bool isComplete = true;

            for (int column = 0; column < BoardWidth; column++)
            {
                if (board[column, row] == 0)
                {
                    isComplete = false;
                    break;
                }
            }

            if (!isComplete)
            {
                continue;
            }

            RemoveLine(row);
            linesRemoved++;
            row++;
        }

        if (linesRemoved == 0)
        {
            return;
        }

        clearedLines += linesRemoved;

        score += linesRemoved switch
        {
            1 => 100,
            2 => 300,
            3 => 500,
            4 => 800,
            _ => linesRemoved * 200
        };

        UpdateSpeed();
    }

    private void RemoveLine(int completedRow)
    {
        for (int row = completedRow; row > 0; row--)
        {
            for (int column = 0; column < BoardWidth; column++)
            {
                board[column, row] = board[column, row - 1];
            }
        }

        for (int column = 0; column < BoardWidth; column++)
        {
            board[column, 0] = 0;
        }
    }

    private void UpdateSpeed()
    {
        int newInterval = 450 - clearedLines * 10;
        gameTimer.Interval = Math.Max(100, newInterval);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.R)
        {
            NewGame();
            return;
        }

        if (e.KeyCode == Keys.P && !gameOver)
        {
            paused = !paused;
            Invalidate();
            return;
        }

        if (gameOver || paused)
        {
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Left:
            case Keys.A:
                MoveHorizontal(-1);
                break;

            case Keys.Right:
            case Keys.D:
                MoveHorizontal(1);
                break;

            case Keys.Down:
            case Keys.S:
                MoveDown();
                score++;
                break;

            case Keys.Up:
            case Keys.W:
                RotatePiece();
                break;

            case Keys.Space:
                HardDrop();
                break;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics graphics = e.Graphics;
        graphics.Clear(BackColor);

        DrawBoardBackground(graphics);
        DrawLockedBlocks(graphics);

        if (!gameOver)
        {
            DrawCurrentPiece(graphics);
        }

        DrawInformationPanel(graphics);

        if (paused)
        {
            DrawOverlay(graphics, "PAUSA", "Presiona P para continuar");
        }

        if (gameOver)
        {
            DrawOverlay(graphics, "FIN DEL JUEGO", "Presiona R para reiniciar");
        }
    }

    private void DrawBoardBackground(Graphics graphics)
    {
        using SolidBrush boardBrush =
            new(Color.FromArgb(30, 41, 59));

        using Pen gridPen =
            new(Color.FromArgb(51, 65, 85));

        graphics.FillRectangle(
            boardBrush,
            BoardOffsetX,
            BoardOffsetY,
            BoardWidth * CellSize,
            BoardHeight * CellSize
        );

        for (int column = 0; column <= BoardWidth; column++)
        {
            int x = BoardOffsetX + column * CellSize;

            graphics.DrawLine(
                gridPen,
                x,
                BoardOffsetY,
                x,
                BoardOffsetY + BoardHeight * CellSize
            );
        }

        for (int row = 0; row <= BoardHeight; row++)
        {
            int y = BoardOffsetY + row * CellSize;

            graphics.DrawLine(
                gridPen,
                BoardOffsetX,
                y,
                BoardOffsetX + BoardWidth * CellSize,
                y
            );
        }
    }

    private void DrawLockedBlocks(Graphics graphics)
    {
        for (int x = 0; x < BoardWidth; x++)
        {
            for (int y = 0; y < BoardHeight; y++)
            {
                int cellValue = board[x, y];

                if (cellValue > 0)
                {
                    DrawCell(
                        graphics,
                        x,
                        y,
                        pieceColors[cellValue - 1]
                    );
                }
            }
        }
    }

    private void DrawCurrentPiece(Graphics graphics)
    {
        Color currentColor = pieceColors[currentPieceType];

        foreach (Point block in currentBlocks)
        {
            int boardX = currentX + block.X;
            int boardY = currentY + block.Y;

            DrawCell(graphics, boardX, boardY, currentColor);
        }
    }

    private static void DrawCell(
        Graphics graphics,
        int boardX,
        int boardY,
        Color color
    )
    {
        int pixelX = BoardOffsetX + boardX * CellSize;
        int pixelY = BoardOffsetY + boardY * CellSize;

        Rectangle rectangle = new(
            pixelX + 2,
            pixelY + 2,
            CellSize - 4,
            CellSize - 4
        );

        using SolidBrush brush = new(color);
        using Pen borderPen = new(Color.FromArgb(180, Color.White));

        graphics.FillRectangle(brush, rectangle);
        graphics.DrawRectangle(borderPen, rectangle);
    }

    private void DrawInformationPanel(Graphics graphics)
    {
        int panelX = BoardOffsetX + BoardWidth * CellSize + 25;

        using Font titleFont =
            new("Segoe UI", 20, FontStyle.Bold);

        using Font textFont =
            new("Segoe UI", 11, FontStyle.Regular);

        using Font scoreFont =
            new("Segoe UI", 15, FontStyle.Bold);

        using SolidBrush titleBrush =
            new(Color.DeepSkyBlue);

        using SolidBrush textBrush =
            new(Color.Gainsboro);

        graphics.DrawString(
            "TETRIS",
            titleFont,
            titleBrush,
            panelX,
            25
        );

        graphics.DrawString(
            $"Puntuación\n{score}",
            scoreFont,
            textBrush,
            panelX,
            135
        );

        graphics.DrawString(
            $"Líneas\n{clearedLines}",
            scoreFont,
            textBrush,
            panelX,
            210
        );

        string controls =
            "Controles\n\n" +
            "← →  Mover\n" +
            "↓     Bajar\n" +
            "↑     Rotar\n" +
            "Espacio\nCaída rápida\n\n" +
            "P  Pausa\n" +
            "R  Reiniciar";

        graphics.DrawString(
            controls,
            textFont,
            textBrush,
            panelX,
            295
        );
    }

    private void DrawOverlay(
        Graphics graphics,
        string title,
        string subtitle
    )
    {
        Rectangle overlayArea = new(
            BoardOffsetX,
            BoardOffsetY + 200,
            BoardWidth * CellSize,
            150
        );

        using SolidBrush overlayBrush =
            new(Color.FromArgb(220, 15, 23, 42));

        using SolidBrush titleBrush =
            new(Color.White);

        using SolidBrush subtitleBrush =
            new(Color.LightGray);

        using Font titleFont =
            new("Segoe UI", 19, FontStyle.Bold);

        using Font subtitleFont =
            new("Segoe UI", 10, FontStyle.Regular);

        graphics.FillRectangle(overlayBrush, overlayArea);

        SizeF titleSize = graphics.MeasureString(title, titleFont);
        SizeF subtitleSize = graphics.MeasureString(subtitle, subtitleFont);

        float titleX =
            overlayArea.X + (overlayArea.Width - titleSize.Width) / 2;

        float subtitleX =
            overlayArea.X + (overlayArea.Width - subtitleSize.Width) / 2;

        graphics.DrawString(
            title,
            titleFont,
            titleBrush,
            titleX,
            overlayArea.Y + 38
        );

        graphics.DrawString(
            subtitle,
            subtitleFont,
            subtitleBrush,
            subtitleX,
            overlayArea.Y + 90
        );
    }
}
