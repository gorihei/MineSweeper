using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MineSweeper
{
    public partial class MainForm : Form
    {
        /// <summary>番兵セル</summary>
        private const int CELL_NONE = -3;
        /// <summary>地雷セル</summary>
        private const int CELL_MINE = -2;
        /// <summary>空セル</summary>
        private const int CELL_BLANK = -1;

        /// <summary>地雷セルイメージインデックス</summary>
        private const int CELL_IMAGE_MINE_IDX = 9;
        /// <summary>空セルイメージインデックス</summary>
        private const int CELL_IMAGE_BLANK_IDX = 10;
        /// <summary>旗セルイメージインデックス</summary>
        private const int CELL_IMAGE_FLAG_IDX = 11;
        /// <summary>セルイメージ最大数</summary>
        private const int CELL_IMAGE_MAX = 12;

        /// <summary>セルサイズ(画像の1セル毎のサイズ。変更する場合は画像も変更必要)</summary>
        private const int CELL_SIZE = 50;
        /// <summary>1辺当たりのセル数</summary>
        private const int LINE_CELL_COUNT = 10;
        /// <summary>盤面サイズ</summary>
        private const int BOARD_SIZE = CELL_SIZE * LINE_CELL_COUNT;
        /// <summary>地雷セルの数</summary>
        private const int MINE_COUNT = 10;

        /// <summary>
        /// セルイメージ配列
        /// </summary>
        private Bitmap[] CellImages { get; set; } = new Bitmap[CELL_IMAGE_MAX];

        /// <summary>
        /// 盤面
        /// </summary>
        private Cell[,] Board { get; set; }

        /// <summary>
        /// ゲーム終了しているかどうか
        /// </summary>
        private bool IsGameEnd { get; set; }

        /// <summary>
        /// 初回のセル公開かどうか
        /// </summary>
        private bool IsFirstOpen { get; set; } = true;

        /// <summary>
        /// セルクラス
        /// </summary>
        private class Cell
        {
            /// <summary>
            /// セル状態
            /// </summary>
            public int State { get; set; } = CELL_BLANK;
            /// <summary>
            /// 旗が立っているかどうか
            /// </summary>
            public bool Flag { get; set; } = false;
        }

        /// <summary>
        /// メインフォームコンストラクタ。
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            // フォーム初期化
            InitializeForm();

            // セルイメージ初期化
            InitializeCellImages();

            // 盤面の初期化
            InitializeBoard();

            // 盤面の描画
            DrawBoard();
        }

        /// <summary>
        /// フォーム初期化処理。
        /// </summary>
        private void InitializeForm()
        {
            // フォームサイズ変更不可に設定
            FormBorderStyle = FormBorderStyle.FixedDialog;
            // フォームサイズ設定
            ClientSize = new Size(BOARD_SIZE, BOARD_SIZE);
            // フォームキーダウンイベント設定
            KeyDown += Form_KeyDown;
            // ピクチャーボックスサイズ設定
            picBoard.Dock = DockStyle.Fill;
            // ピクチャーボックスイメージをフォームサイズで初期化
            picBoard.Image = new Bitmap(BOARD_SIZE, BOARD_SIZE);
            // ピクチャーボックスマウスクリックイベント設定
            picBoard.MouseClick += Board_Click;
        }

        /// <summary>
        /// セルイメージ初期化処理。
        /// </summary>
        private void InitializeCellImages()
        {
            // Resourceからセルイメージを取得
            var images = Properties.Resources.minesweeper;

            for (var i = 0; i < CELL_IMAGE_MAX; i++)
            {
                // X=(i*100), Y=0, 横幅100, 高さ100でイメージを分割して配列に設定
                CellImages[i] = images.Clone(new Rectangle(i * 100, 0, 100, 100), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }
        }

        /// <summary>
        /// 盤面初期化処理。
        /// </summary>
        private void InitializeBoard()
        {
            // 盤面初期化
            Board = new Cell[LINE_CELL_COUNT + 2, LINE_CELL_COUNT + 2];

            for (var y = 0; y < Board.GetLength(0); y++)
            {
                for (var x = 0; x < Board.GetLength(1); x++)
                {
                    Board[y, x] = new Cell();

                    // 盤面の外周の辺の場合
                    if (x == 0 || x == Board.GetLength(1) - 1 || y == 0 || y == Board.GetLength(0) - 1)
                    {
                        // 番兵
                        Board[y, x].State = CELL_NONE;
                    }
                }
            }

            // 地雷を配置
            int cnt = 0;
            var rand = new Random();

            if (MINE_COUNT >= LINE_CELL_COUNT * 2)
            {
                throw new Exception("地雷数の設定が盤面セル数を超えています。");
            }

            while (MINE_COUNT > cnt)
            {
                // 盤面内側のセル座標をランダムで取得
                var x = rand.Next(1, Board.GetLength(0) - 1);
                var y = rand.Next(1, Board.GetLength(1) - 1);

                // 地雷セルではない場合
                if (Board[y, x].State != CELL_MINE)
                {
                    Board[y, x].State = CELL_MINE;
                    cnt++;
                }
            }
        }

        /// <summary>
        /// 盤面描画処理。
        /// </summary>
        private void DrawBoard()
        {
            using (var g = Graphics.FromImage(picBoard.Image))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;

                for (var y = 0; y < LINE_CELL_COUNT; y++)
                {
                    for (var x = 0; x < LINE_CELL_COUNT; x++)
                    {
                        int imageIdx;
                        // 公開済みセル
                        if (Board[y + 1, x + 1].State > -1)
                        {
                            imageIdx = Board[y + 1, x + 1].State;
                        }
                        // 未公開セル
                        else
                        {
                            // フラグON
                            if (Board[y + 1, x + 1].Flag)
                            {
                                imageIdx = CELL_IMAGE_FLAG_IDX;
                            }
                            // フラグOFF
                            else
                            {
                                imageIdx = CELL_IMAGE_BLANK_IDX;
                            }

                            // ゲームが終了しているかつ、地雷セル
                            if (IsGameEnd && Board[y + 1, x + 1].State == CELL_MINE)
                            {
                                // 地雷セルを表示する
                                imageIdx = CELL_IMAGE_MINE_IDX;
                            }
                        }

                        // セル描画
                        g.DrawImage(CellImages[imageIdx], x * CELL_SIZE, y * CELL_SIZE, CELL_SIZE, CELL_SIZE);
                    }
                }
            }

            // 描画更新
            picBoard.Invalidate();
        }

        /// <summary>
        /// フォームKeyDownイベント。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            // Escapeキーが押下された場合
            if (e.KeyCode == Keys.Escape)
            {
                // ゲームを初期化する
                IsGameEnd = false;
                IsFirstOpen = true;
                InitializeBoard();
                DrawBoard();
            }
        }

        /// <summary>
        /// ピクチャーボックスClickイベント。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Board_Click(object sender, MouseEventArgs e)
        {
            // ゲーム終了している場合は何もしない
            if (IsGameEnd)
            {
                return;
            }

            var clickPoint = GetCellPointFromMousePoint(e.Location);

            // 左クリック
            if (e.Button == MouseButtons.Left)
            {
                // フラグONの場合
                if (Board[clickPoint.Y, clickPoint.X].State == CELL_BLANK &&
                    Board[clickPoint.Y, clickPoint.X].Flag)
                {
                    // 何もしない
                    return;
                }
                else
                {
                    // セルを公開する
                    CellOpen(clickPoint);
                }
            }
            // 右クリック
            else if (e.Button == MouseButtons.Right)
            {
                // 未公開のセルの場合(地雷or空)
                if (Board[clickPoint.Y, clickPoint.X].State < 0)
                {
                    // フラグを反転する
                    Board[clickPoint.Y, clickPoint.X].Flag = !Board[clickPoint.Y, clickPoint.X].Flag;

                    // 盤面の描画
                    DrawBoard();
                }
            }
        }

        /// <summary>
        /// マウス座標から盤面セル座標を取得する。
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private Point GetCellPointFromMousePoint(Point p)
        {
            var cellPoint = p;

            for (var y = 0; y < LINE_CELL_COUNT; y++)
            {
                for (var x = 0; x < LINE_CELL_COUNT; x++)
                {
                    // クリック座標がX,Y位置のセル領域内の場合
                    if (x * CELL_SIZE <= p.X &&
                        p.X <= x * CELL_SIZE + CELL_SIZE &&
                        y * CELL_SIZE <= p.Y &&
                        p.Y <= y * CELL_SIZE + CELL_SIZE)
                    {
                        // セル座標を設定
                        cellPoint = new Point(x + 1, y + 1);
                    }
                }
            }

            return cellPoint;
        }

        /// <summary>
        /// セルを公開する。
        /// </summary>
        /// <param name="p"></param>
        private void CellOpen(Point p)
        {
            // 未公開セルの場合
            if (Board[p.Y, p.X].State == CELL_BLANK)
            {
                // 周囲に地雷のない隣接セルを全て公開済みにする
                OpenThatCanbeOpend(p.X, p.Y, new List<Point>());

                // 地雷数を設定する
                Board[p.Y, p.X].State = CountMines(p.X, p.Y);

                // 盤面の描画
                DrawBoard();

                // ゲームクリア判定
                if (IsGameClear())
                {
                    // ゲームクリアでゲーム終了
                    GameEnd(true);
                }
            }
            // 地雷セルの場合
            else if (Board[p.Y, p.X].State == CELL_MINE)
            {
                // 初回選択の場合
                if (IsFirstOpen)
                {
                    // 地雷をなかったものとして処理する
                    Board[p.Y, p.X].State = CELL_BLANK;
                    CellOpen(p);
                    return;
                }

                // ゲームオーバーでゲーム終了
                GameEnd(false);
            }
            // 公開済みのセル
            else
            {
                // 何もしない
            }

            IsFirstOpen = false;
        }

        /// <summary>
        /// ゲームクリアしたか判定する。
        /// </summary>
        /// <returns></returns>
        private bool IsGameClear()
        {
            for (var y = 0; y < Board.GetLength(0); y++)
            {
                for (var x = 0; x < Board.GetLength(1); x++)
                {
                    if (Board[y, x].State == CELL_BLANK)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// ゲーム終了処理。
        /// </summary>
        /// <param name="clear"></param>
        private void GameEnd(bool clear)
        {
            IsGameEnd = true;

            // 盤面の描画
            DrawBoard();

            string text;
            if (clear)
            {
                text = "GameClear";
            }
            else
            {
                text = "GameOver";
            }

            // 文字列を中央に描画
            using (var g = Graphics.FromImage(picBoard.Image))
            using (var font = new Font("Meiryo UI", 50, FontStyle.Bold))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var textSize = g.MeasureString(text, font).ToSize();
                g.DrawString(text, font, Brushes.Red, new Point((BOARD_SIZE / 2) - (textSize.Width / 2), (BOARD_SIZE / 2) - (textSize.Height / 2)));
            }
        }

        /// <summary>
        /// 指定した座標を起点として公開できるセルをすべて公開する。
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        private int OpenThatCanbeOpend(int x, int y, List<Point> list)
        {
            // 探索範囲外の場合
            if (x == 0 || x == Board.GetLength(1) - 1 || y == 0 || y == Board.GetLength(0) - 1)
            {
                return 1;
            }

            // 探索済みの場合
            if (list.Contains(new Point(x, y)))
            {
                return 1;
            }
            // 未探索の場合
            else
            {
                list.Add(new Point(x, y));
            }

            // 周囲に地雷があるセルの場合
            if (CountMines(x, y) != 0)
            {
                return 1;
            }
            // 周囲に地雷がないセルの場合
            else
            {
                // 自身のセルと周囲8セルを公開済みにする
                if (CountMines(x, y) > -1) Board[y, x].State = CountMines(x, y);
                if (CountMines(x, y - 1) > -1) Board[y - 1, x].State = CountMines(x, y - 1);
                if (CountMines(x + 1, y - 1) > -1) Board[y - 1, x + 1].State = CountMines(x + 1, y - 1);
                if (CountMines(x + 1, y) > -1) Board[y, x + 1].State = CountMines(x + 1, y);
                if (CountMines(x + 1, y + 1) > -1) Board[y + 1, x + 1].State = CountMines(x + 1, y + 1);
                if (CountMines(x, y + 1) > -1) Board[y + 1, x].State = CountMines(x, y + 1);
                if (CountMines(x - 1, y + 1) > -1) Board[y + 1, x - 1].State = CountMines(x - 1, y + 1);
                if (CountMines(x - 1, y) > -1) Board[y, x - 1].State = CountMines(x - 1, y);
                if (CountMines(x - 1, y - 1) > -1) Board[y - 1, x - 1].State = CountMines(x - 1, y - 1);

                // ８方向に再帰探索する
                if (OpenThatCanbeOpend(x, y - 1, list) == -1) return -1;
                if (OpenThatCanbeOpend(x + 1, y - 1, list) == -1) return -1;
                if (OpenThatCanbeOpend(x + 1, y, list) == -1) return -1;
                if (OpenThatCanbeOpend(x + 1, y + 1, list) == -1) return -1;
                if (OpenThatCanbeOpend(x, y + 1, list) == -1) return -1;
                if (OpenThatCanbeOpend(x - 1, y + 1, list) == -1) return -1;
                if (OpenThatCanbeOpend(x - 1, y, list) == -1) return -1;
                if (OpenThatCanbeOpend(x - 1, y - 1, list) == -1) return -1;
            }

            return 0;
        }

        /// <summary>
        /// 指定座標の周囲8セルの地雷をカウントする。
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private int CountMines(int x, int y)
        {
            // 範囲外まで探索した場合は終了
            if (x == 0 || x == Board.GetLength(1) - 1 || y == 0 || y == Board.GetLength(0) - 1)
            {
                return -1;
            }

            var mineCnt = 0;

            if (Board[y - 1, x].State == CELL_MINE) mineCnt++;
            if (Board[y + 1, x].State == CELL_MINE) mineCnt++;
            if (Board[y, x - 1].State == CELL_MINE) mineCnt++;
            if (Board[y, x + 1].State == CELL_MINE) mineCnt++;
            if (Board[y - 1, x - 1].State == CELL_MINE) mineCnt++;
            if (Board[y - 1, x + 1].State == CELL_MINE) mineCnt++;
            if (Board[y + 1, x - 1].State == CELL_MINE) mineCnt++;
            if (Board[y + 1, x + 1].State == CELL_MINE) mineCnt++;

            return mineCnt;
        }
    }
}
